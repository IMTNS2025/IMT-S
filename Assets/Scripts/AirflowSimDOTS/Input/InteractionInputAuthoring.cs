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
