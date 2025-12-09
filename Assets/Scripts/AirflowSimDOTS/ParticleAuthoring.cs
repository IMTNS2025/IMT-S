using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class ParticleAuthoring : MonoBehaviour
{
    private class Baker : Baker<ParticleAuthoring>
    {
        public override void Bake(ParticleAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Particle
            {
                density = 0f,
                densityNear = 0f,
                predictedPosition = float2.zero,
                velocity = float2.zero,
            });
        }
    }
}

[BurstCompile]
public struct Particle : IComponentData
{
    public int id;
    public float density;
    public float densityNear;
    public float2 predictedPosition;
    public float2 velocity;

}
