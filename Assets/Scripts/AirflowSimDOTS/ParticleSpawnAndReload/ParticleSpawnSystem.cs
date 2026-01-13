using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(ParticleReloadSystem))]
[BurstCompile]
partial struct ParticleSpawnSystem : ISystem
{
    private uint randomSeed;
    
    [BurstCompile]
    public void OnCreate(ref SystemState pSystemState)
    {
        pSystemState.RequireForUpdate<ParticleSpawnSettings>();
        randomSeed = 42; // Will be updated on first spawn
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
        RefRW<ParticleSpawnSettings> pss = SystemAPI.GetSingletonRW<ParticleSpawnSettings>();
        if (!pss.ValueRO.doSpawn) return;

        // Update random seed to ensure variety between spawns
        randomSeed = randomSeed * 1664525u + 1013904223u; // Linear congruential generator
        Random rng = new (randomSeed);

        float2 s = pss.ValueRO.spawnSize;
        int numX = (int)math.ceil(math.sqrt(s.x / s.y * pss.ValueRO.particleCount + (s.x - s.y) * (s.x - s.y) / (4 * s.y * s.y)) - (s.x - s.y) / (2 * s.y));
        int numY = (int)math.ceil(pss.ValueRO.particleCount / (float)numX);
        int i = 0;

        NativeArray<Entity> instantiatedEntities = pSystemState.EntityManager.Instantiate(pss.ValueRO.particlePrefab, pss.ValueRO.particleCount, Allocator.Temp);


        for (int y = 0; y < numY; y++)
        {
            for (int x = 0; x < numX; x++)
            {
                if (i >= pss.ValueRO.particleCount) break;
                Entity entity = instantiatedEntities[i];

                float tx = numX <= 1 ? 0.5f : x / (numX - 1f);
                float ty = numY <= 1 ? 0.5f : y / (numY - 1f);

                float angle = rng.NextFloat() * 3.14159265f * 2f;
                float2 dir = new (math.cos(angle), math.sin(angle));
                float2 jitter = (rng.NextFloat() - 0.5f) * pss.ValueRO.jitterStrength * dir;
                float2 position = new float2((tx - 0.5f) * pss.ValueRO.spawnSize.x, (ty - 0.5f) * pss.ValueRO.spawnSize.y) + jitter + pss.ValueRO.spawnCenter;
                float2 velocity = pss.ValueRO.initialVelocity;

                pSystemState.EntityManager.SetComponentData(entity, new LocalTransform
                {
                    Position = new float3(position.x, position.y, 0),
                    Scale = 1f,
                    Rotation = quaternion.identity,
                });
                
                // Initialize all Particle component fields to ensure clean state
                // Set density values to small non-zero values to prevent division by zero
                pSystemState.EntityManager.SetComponentData(entity, new Particle
                {
                    id = i,
                    velocity = velocity,
                    density = 1f, // Start with non-zero density to prevent instability
                    densityNear = 1f, // Start with non-zero near density
                    predictedPosition = position
                });

                i++;
            }
        }

        pss.ValueRW.doSpawn = false;
        instantiatedEntities.Dispose();
    }
}
