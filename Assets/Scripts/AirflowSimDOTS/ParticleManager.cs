using UnityEngine;
using Unity.Entities;

/// <summary>
/// MonoBehaviour bridge to communicate with the ECS ParticleReloadSystem.
/// Provides methods to reload particles and update particle count from UI.
/// </summary>
public class ParticleManager : MonoBehaviour
{
    private static ParticleManager instance;
    public static ParticleManager Instance => instance;

    [Header("Particle Count Settings")]
    [SerializeField] private int minParticleCount = 100;
    [SerializeField] private int maxParticleCount = 2000;
    [SerializeField] private int defaultParticleCount = 400;
    [SerializeField] private int particleCountStep = 50;

    [Header("Reload Settings")]
    [SerializeField] private bool reloadOnPlay = false;

    private int currentParticleCount;

    public int MinParticleCount => minParticleCount;
    public int MaxParticleCount => maxParticleCount;
    public int DefaultParticleCount => defaultParticleCount;
    public int ParticleCountStep => particleCountStep;
    public bool ReloadOnPlay { get => reloadOnPlay; set => reloadOnPlay = value; }
    public int CurrentParticleCount => currentParticleCount;

    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        currentParticleCount = defaultParticleCount;
        
        Debug.Log($"[ParticleManager] Initialized with count: {currentParticleCount}, min: {minParticleCount}, max: {maxParticleCount}, step: {particleCountStep}");
    }

    /// <summary>
    /// Reloads all particles with the current particle count.
    /// </summary>
    public void ReloadParticles()
    {
        ReloadParticles(currentParticleCount);
    }

    /// <summary>
    /// Reloads all particles with a specific particle count.
    /// </summary>
    /// <param name="particleCount">Number of particles to spawn.</param>
    public void ReloadParticles(int particleCount)
    {
        currentParticleCount = Mathf.Clamp(particleCount, minParticleCount, maxParticleCount);

        World defaultWorld = World.DefaultGameObjectInjectionWorld;
        if (defaultWorld == null)
        {
            Debug.LogError("[ParticleManager] Default world not found. Cannot reload particles.");
            return;
        }

        if (!defaultWorld.IsCreated)
        {
            Debug.LogError("[ParticleManager] Default world is not created. Cannot reload particles.");
            return;
        }

        EntityManager entityManager = defaultWorld.EntityManager;
        
        // Find the ParticleReloadRequest entity
        EntityQuery reloadQuery = entityManager.CreateEntityQuery(typeof(ParticleReloadRequest));
        
        if (reloadQuery.IsEmpty)
        {
            Debug.LogError("[ParticleManager] ParticleReloadRequest entity not found. Make sure ParticleReloadRequestAuthoring is in the scene.");
            reloadQuery.Dispose();
            return;
        }

        Entity reloadEntity = reloadQuery.GetSingletonEntity();
        
        // Set the reload request
        ParticleReloadRequest reloadRequest = new ParticleReloadRequest
        {
            shouldReload = true,
            newParticleCount = currentParticleCount
        };
        
        entityManager.SetComponentData(reloadEntity, reloadRequest);
        reloadQuery.Dispose();

        Debug.Log($"[ParticleManager] Reload requested with {currentParticleCount} particles.");
    }

    /// <summary>
    /// Sets the particle count for the next reload.
    /// </summary>
    /// <param name="count">Number of particles.</param>
    public void SetParticleCount(int count)
    {
        currentParticleCount = Mathf.Clamp(count, minParticleCount, maxParticleCount);
        Debug.Log($"[ParticleManager] Particle count set to: {currentParticleCount}");
    }

    /// <summary>
    /// Called when simulation or free mode starts.
    /// Triggers reload if ReloadOnPlay is enabled.
    /// </summary>
    public void OnModeStart()
    {
        Debug.Log($"[ParticleManager] OnModeStart called. ReloadOnPlay: {reloadOnPlay}");
        if (reloadOnPlay)
        {
            // Delay the reload slightly to ensure world is ready
            StartCoroutine(DelayedReload());
        }
    }

    private System.Collections.IEnumerator DelayedReload()
    {
        // Wait a frame to ensure everything is initialized
        yield return null;
        ReloadParticles();
    }
}
