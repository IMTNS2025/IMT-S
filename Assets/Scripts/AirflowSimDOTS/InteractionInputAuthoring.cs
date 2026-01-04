using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class InteractionInputAuthoring : MonoBehaviour
{
    private Entity cachedEntity = Entity.Null;
    private EntityManager entityManager;
    private Camera mainCamera; // Cache camera reference
    private float cachedCameraZ; // Cache camera Z position
    private float cachedZToPlane; // Cache the Z-to-plane distance

    void OnEnable()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
            return;

        entityManager = world.EntityManager;

        // Cache main camera for performance
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cachedCameraZ = mainCamera.transform.position.z;
            cachedZToPlane = -cachedCameraZ;
        }

        // Use EntityQuery to find existing InteractionInput singleton
        var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<InteractionInput>());
        if (query.IsEmpty)
        {
            cachedEntity = entityManager.CreateEntity(typeof(InteractionInput));
            entityManager.SetComponentData(cachedEntity, new InteractionInput { point = float2.zero, active = false });
        }
        else
        {
            cachedEntity = query.GetSingletonEntity();
        }
        query.Dispose();
    }

    void Update()
    {
        // Early exit checks - single condition check
        if (entityManager == null || cachedEntity == Entity.Null || mainCamera == null)
            return;

        // OPTIMIZATION: Direct input state evaluation without intermediate variables
        bool active = false;
        Vector3 screenPos = Vector3.zero;

        // Check touch input first (mobile) - optimized touch phase checking
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            TouchPhase phase = touch.phase;

            // OPTIMIZATION: Single comparison using bitwise logic for active phases
            // Began = 0, Moved = 1, Stationary = 2, Ended = 3, Canceled = 4
            active = phase <= TouchPhase.Stationary;

            if (active)
            {
                screenPos = touch.position;
            }
        }
        // Fallback to mouse input (single call optimization)
        else
        {
            active = Input.GetMouseButton(0);
            if (active)
            {
                screenPos = Input.mousePosition;
            }
        }

        // OPTIMIZATION: Single SetComponentData call with ternary operators
        if (active)
        {
            // Use cached zToPlane value
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cachedZToPlane));

            entityManager.SetComponentData(cachedEntity, new InteractionInput
            {
                point = new float2(worldPos.x, worldPos.y),
                active = true
            });
        }
        else
        {
            // OPTIMIZATION: Reuse static readonly zero value
            entityManager.SetComponentData(cachedEntity, new InteractionInput
            {
                point = float2.zero,
                active = false
            });
        }
    }

    void OnDisable()
    {
        // Clean up on disable
        mainCamera = null;
    }
}

public struct InteractionInput : IComponentData
{
    public float2 point;
    public bool active;
}
