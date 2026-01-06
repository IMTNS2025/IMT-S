using UnityEngine;

/// <summary>
/// Quick test script to verify ParticleManager functionality.
/// Add this to any GameObject and press the keys to test.
/// Remove this script once everything is working.
/// </summary>
public class ParticleManagerDebugger : MonoBehaviour
{
    [Header("Test Controls")]
    [SerializeField] private KeyCode testReloadKey = KeyCode.F1;
    [SerializeField] private KeyCode testSetCountKey = KeyCode.F2;
    [SerializeField] private KeyCode testStatusKey = KeyCode.F3;
    [SerializeField] private int testParticleCount = 1000;

    private void Update()
    {
        // F1: Test reload with current count
        if (Input.GetKeyDown(testReloadKey))
        {
            Debug.Log("==================================================");
            Debug.Log($"[DEBUG] F1 pressed - Testing ReloadParticles()");
            
            if (ParticleManager.Instance == null)
            {
                Debug.LogError("[DEBUG] FAILED: ParticleManager.Instance is NULL!");
                Debug.LogError("[DEBUG] Make sure ParticleManager component exists in the scene");
                return;
            }
            
            Debug.Log($"[DEBUG] ParticleManager found!");
            Debug.Log($"[DEBUG] Current particle count: {ParticleManager.Instance.CurrentParticleCount}");
            
            ParticleManager.Instance.ReloadParticles();
            
            Debug.Log($"[DEBUG] ReloadParticles() called");
            Debug.Log("==================================================");
        }

        // F2: Test setting particle count
        if (Input.GetKeyDown(testSetCountKey))
        {
            Debug.Log("==================================================");
            Debug.Log($"[DEBUG] F2 pressed - Testing SetParticleCount({testParticleCount})");
            
            if (ParticleManager.Instance == null)
            {
                Debug.LogError("[DEBUG] FAILED: ParticleManager.Instance is NULL!");
                return;
            }
            
            Debug.Log($"[DEBUG] Before: {ParticleManager.Instance.CurrentParticleCount}");
            ParticleManager.Instance.SetParticleCount(testParticleCount);
            Debug.Log($"[DEBUG] After: {ParticleManager.Instance.CurrentParticleCount}");
            
            Debug.Log("==================================================");
        }

        // F3: Print status
        if (Input.GetKeyDown(testStatusKey))
        {
            Debug.Log("==================================================");
            Debug.Log($"[DEBUG] F3 pressed - Particle Manager Status");
            
            if (ParticleManager.Instance == null)
            {
                Debug.LogError("[DEBUG] ParticleManager.Instance is NULL!");
                Debug.LogError("[DEBUG] - Check if ParticleManager component exists in scene");
                Debug.LogError("[DEBUG] - Check if GameObject with ParticleManager is active");
                Debug.LogError("[DEBUG] - Check if ParticleManager Awake() was called");
                return;
            }
            
            Debug.Log($"[DEBUG] ? ParticleManager Instance: FOUND");
            Debug.Log($"[DEBUG] Min Particle Count: {ParticleManager.Instance.MinParticleCount}");
            Debug.Log($"[DEBUG] Max Particle Count: {ParticleManager.Instance.MaxParticleCount}");
            Debug.Log($"[DEBUG] Default Particle Count: {ParticleManager.Instance.DefaultParticleCount}");
            Debug.Log($"[DEBUG] Particle Count Step: {ParticleManager.Instance.ParticleCountStep}");
            Debug.Log($"[DEBUG] Current Particle Count: {ParticleManager.Instance.CurrentParticleCount}");
            Debug.Log($"[DEBUG] Reload On Play: {ParticleManager.Instance.ReloadOnPlay}");
            
            // Check world status
            if (Unity.Entities.World.DefaultGameObjectInjectionWorld != null)
            {
                Debug.Log($"[DEBUG] ? Default World: FOUND");
                Debug.Log($"[DEBUG] World Is Created: {Unity.Entities.World.DefaultGameObjectInjectionWorld.IsCreated}");
                
                var entityManager = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
                var reloadQuery = entityManager.CreateEntityQuery(typeof(ParticleReloadRequest));
                
                if (!reloadQuery.IsEmpty)
                {
                    Debug.Log($"[DEBUG] ? ParticleReloadRequest Entity: FOUND");
                    var entity = reloadQuery.GetSingletonEntity();
                    var request = entityManager.GetComponentData<ParticleReloadRequest>(entity);
                    Debug.Log($"[DEBUG] shouldReload: {request.shouldReload}");
                    Debug.Log($"[DEBUG] newParticleCount: {request.newParticleCount}");
                }
                else
                {
                    Debug.LogError("[DEBUG] ? ParticleReloadRequest Entity: NOT FOUND");
                    Debug.LogError("[DEBUG] - Add ParticleReloadRequestAuthoring to a GameObject");
                    Debug.LogError("[DEBUG] - Save the scene");
                    Debug.LogError("[DEBUG] - Restart play mode");
                }
                
                reloadQuery.Dispose();
                
                var spawnQuery = entityManager.CreateEntityQuery(typeof(ParticleSpawnSettings));
                if (!spawnQuery.IsEmpty)
                {
                    Debug.Log($"[DEBUG] ? ParticleSpawnSettings Entity: FOUND");
                    var spawnSettings = spawnQuery.GetSingleton<ParticleSpawnSettings>();
                    Debug.Log($"[DEBUG] Spawn Settings Particle Count: {spawnSettings.particleCount}");
                    Debug.Log($"[DEBUG] doSpawn: {spawnSettings.doSpawn}");
                }
                else
                {
                    Debug.LogError("[DEBUG] ? ParticleSpawnSettings Entity: NOT FOUND");
                }
                
                spawnQuery.Dispose();
            }
            else
            {
                Debug.LogError("[DEBUG] ? Default World: NOT FOUND");
                Debug.LogError("[DEBUG] - Make sure you're in Play Mode");
                Debug.LogError("[DEBUG] - Make sure ECS world is initialized");
            }
            
            Debug.Log("==================================================");
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        style.alignment = TextAnchor.MiddleLeft;
        style.normal.textColor = Color.white;
        
        GUILayout.BeginArea(new Rect(10, Screen.height - 120, 400, 110), "Particle Manager Debugger", style);
        
        GUILayout.Label($"Press {testReloadKey} - Reload particles");
        GUILayout.Label($"Press {testSetCountKey} - Set count to {testParticleCount}");
        GUILayout.Label($"Press {testStatusKey} - Print status");
        
        if (ParticleManager.Instance != null)
        {
            GUILayout.Label($"Current Count: {ParticleManager.Instance.CurrentParticleCount}", new GUIStyle(GUI.skin.label) { normal = new GUIStyleState { textColor = Color.green } });
        }
        else
        {
            GUILayout.Label("ParticleManager: NOT FOUND", new GUIStyle(GUI.skin.label) { normal = new GUIStyleState { textColor = Color.red } });
        }
        
        GUILayout.EndArea();
    }
}
