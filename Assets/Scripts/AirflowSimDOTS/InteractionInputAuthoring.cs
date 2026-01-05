using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class InteractionInputAuthoring : MonoBehaviour
{
    private class Baker : Baker<InteractionInputAuthoring>
    {
        public override void Bake(InteractionInputAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new InteractionInput());
        }
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class InteractionInputSystem : SystemBase
{
    private Camera mainCamera;
    private float2 previousPoint;
    private bool wasActiveLastFrame;
    
    protected override void OnCreate()
    {
        RequireForUpdate<InteractionInput>();
    }

    protected override void OnStartRunning()
    {
        mainCamera = Camera.main;
    }

    protected override void OnUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                SystemAPI.SetSingleton(new InteractionInput());
                return;
            }
        }

        bool inputPressed = Input.touchCount > 0
            ? Input.GetTouch(0).phase <= TouchPhase.Stationary
            : Input.GetMouseButton(0);

        if (!inputPressed)
        {
            wasActiveLastFrame = false;
            SystemAPI.SetSingleton(new InteractionInput());
            return;
        }

        Vector3 screenPos = Input.touchCount > 0
            ? (Vector3)Input.GetTouch(0).position
            : Input.mousePosition;

        float zDist = -mainCamera.transform.position.z;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDist));
        float2 currentPoint = new float2(worldPos.x, worldPos.y);

        float dt = SystemAPI.Time.DeltaTime;
        float2 velocity = float2.zero;
        float speed = 0f;
        
        // Calculate obstacle velocity from movement between frames
        if (wasActiveLastFrame && dt > 0f)
        {
            float2 delta = currentPoint - previousPoint;
            velocity = delta / dt;
            speed = math.length(velocity);
        }

        // Obstacle is always active at current position when input is pressed
        // For continuous collision, we pass the swept path from previous to current position
        InteractionInput newInput = new InteractionInput
        {
            position = currentPoint,
            velocity = velocity,
            speed = speed,
            lineStart = wasActiveLastFrame ? previousPoint : currentPoint,
            lineEnd = currentPoint,
            deltaTime = dt,
            isActive = true
        };

        previousPoint = currentPoint;
        wasActiveLastFrame = true;

        SystemAPI.SetSingleton(newInput);
    }
}

public struct InteractionInput : IComponentData
{
    public float2 position;      // Current obstacle position
    public float2 velocity;      // Obstacle velocity (units per second)
    public float speed;          // Cached speed magnitude
    public float2 lineStart;     // Previous frame position (for swept collision)
    public float2 lineEnd;       // Current frame position (for swept collision)
    public float deltaTime;      // Frame delta time for interpolation
    public bool isActive;
}
