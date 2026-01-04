using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class InteractionInputAuthoring : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("Multiplier for velocity-based disturbance (higher = more impact from fast movement)")]
    [Range(0.1f, 10f)]
    public float velocityInfluenceMultiplier = 0.8f; // Reduced from 2f
    
    [Tooltip("Smoothing factor for velocity (0 = no smoothing, higher = smoother)")]
    [Range(0f, 0.95f)]
    public float velocitySmoothing = 0.85f; // Increased from 0.7f

    [Tooltip("Maximum input velocity (units/second) - clamps extremely fast movements")]
    [Range(5f, 100f)]
    public float maxInputVelocity = 30f;

    private Entity cachedEntity = Entity.Null;
    private EntityManager entityManager;
    private Camera mainCamera;
    private float cachedCameraZ;
    private float cachedZToPlane;
    
    // Track previous position and velocity for delta calculation
    private float2 previousPoint;
    private float2 smoothedVelocity;
    private bool wasActiveLastFrame;

    void OnEnable()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
            return;

        entityManager = world.EntityManager;

        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cachedCameraZ = mainCamera.transform.position.z;
            cachedZToPlane = -cachedCameraZ;
        }

        var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<InteractionInput>());
        if (query.IsEmpty)
        {
            cachedEntity = entityManager.CreateEntity(typeof(InteractionInput));
            entityManager.SetComponentData(cachedEntity, new InteractionInput 
            { 
                point = float2.zero,
                previousPoint = float2.zero,
                velocity = float2.zero,
                active = false 
            });
        }
        else
        {
            cachedEntity = query.GetSingletonEntity();
        }
        query.Dispose();
        
        // Initialize tracking variables
        previousPoint = float2.zero;
        smoothedVelocity = float2.zero;
        wasActiveLastFrame = false;
    }

    void Update()
    {
        if (entityManager == null || cachedEntity == Entity.Null || mainCamera == null)
            return;

        bool active = false;
        Vector3 screenPos = Vector3.zero;

        // Check touch input first
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            TouchPhase phase = touch.phase;
            active = phase <= TouchPhase.Stationary;

            if (active)
            {
                screenPos = touch.position;
            }
        }
        // Fallback to mouse input
        else
        {
            active = Input.GetMouseButton(0);
            if (active)
            {
                screenPos = Input.mousePosition;
            }
        }

        if (active)
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cachedZToPlane));
            float2 currentPoint = new float2(worldPos.x, worldPos.y);
            
            // Calculate velocity
            float2 instantVelocity = float2.zero;
            if (wasActiveLastFrame)
            {
                float deltaTime = Time.deltaTime;
                if (deltaTime > 0.0001f) // Avoid division by near-zero
                {
                    instantVelocity = (currentPoint - previousPoint) / deltaTime;
                    
                    // Clamp instant velocity to prevent extreme spikes
                    float instantSpeed = math.length(instantVelocity);
                    if (instantSpeed > maxInputVelocity)
                    {
                        instantVelocity = math.normalize(instantVelocity) * maxInputVelocity;
                    }
                }
            }
            
            // Apply smoothing to velocity for more stable interaction
            smoothedVelocity = math.lerp(instantVelocity, smoothedVelocity, velocitySmoothing);
            
            // Clamp smoothed velocity as well (safety measure)
            float smoothedSpeed = math.length(smoothedVelocity);
            if (smoothedSpeed > maxInputVelocity)
            {
                smoothedVelocity = math.normalize(smoothedVelocity) * maxInputVelocity;
            }
            
            // Store the position from the last frame for path calculation
            float2 storedPreviousPoint = wasActiveLastFrame ? previousPoint : currentPoint;
            
            entityManager.SetComponentData(cachedEntity, new InteractionInput
            {
                point = currentPoint,
                previousPoint = storedPreviousPoint,
                velocity = smoothedVelocity * velocityInfluenceMultiplier,
                active = true
            });
            
            previousPoint = currentPoint;
            wasActiveLastFrame = true;
        }
        else
        {
            entityManager.SetComponentData(cachedEntity, new InteractionInput
            {
                point = float2.zero,
                previousPoint = float2.zero,
                velocity = float2.zero,
                active = false
            });
            
            smoothedVelocity = float2.zero;
            wasActiveLastFrame = false;
        }
    }

    void OnDisable()
    {
        mainCamera = null;
    }
}

public struct InteractionInput : IComponentData
{
    public float2 point;
    public float2 previousPoint;
    public float2 velocity;
    public bool active;
}
