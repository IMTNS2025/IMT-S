using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class ParticleSpawnSettingsAuthoring : MonoBehaviour
{
    [SerializeField] private ParticleSpawnSettingsSO particleSpawnSettingsSO;

    private class Baker : Baker<ParticleSpawnSettingsAuthoring>
    {
        public override void Bake(ParticleSpawnSettingsAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ParticleSpawnSettings
            {
                particlePrefab = GetEntity(pAuthoring.particleSpawnSettingsSO.particlePrefab, TransformUsageFlags.None),
                particleCount = pAuthoring.particleSpawnSettingsSO.particleCount,   
                initialVelocity = pAuthoring.particleSpawnSettingsSO.initialVelocity,
                spawnCenter = pAuthoring.particleSpawnSettingsSO.spawnCenter,
                spawnSize = pAuthoring.particleSpawnSettingsSO.spawnSize,
                jitterStrength = pAuthoring.particleSpawnSettingsSO.jitterStrength,
                doSpawn = true,
            });
        }
    }
}

[BurstCompile]
public struct ParticleSpawnSettings : IComponentData
{
    public Entity particlePrefab;
    public int particleCount;
    public float2 initialVelocity;
    public float2 spawnCenter;
    public float2 spawnSize;
    public float jitterStrength;
    public bool doSpawn;
}
