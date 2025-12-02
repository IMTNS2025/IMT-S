using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial struct ParticleSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState pSystemState)
    {
        pSystemState.RequireForUpdate<ParticleSpawnSettings>();
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
        RefRW<ParticleSpawnSettings> pss = SystemAPI.GetSingletonRW<ParticleSpawnSettings>();
        if (!pss.ValueRO.doSpawn) return;

        Random rng = new(42);

        float2 s = pss.ValueRO.spawnSize;
        int numX = (int)math.ceil(math.sqrt(s.x / s.y * pss.ValueRO.particleCount + (s.x - s.y) * (s.x - s.y) / (4 * s.y * s.y)) - (s.x - s.y) / (2 * s.y));
        int numY = (int)math.ceil(pss.ValueRO.particleCount / (float)numX);
        int i = 0;

        NativeArray<Entity> instiatedEntities = pSystemState.EntityManager.Instantiate(pss.ValueRO.particlePrefab, pss.ValueRO.particleCount, Allocator.Temp);


        for (int y = 0; y < numY; y++)
        {
            for (int x = 0; x < numX; x++)
            {
                if (i >= pss.ValueRO.particleCount) break;
                Entity entity = instiatedEntities[i];

                float tx = numX <= 1 ? 0.5f : x / (numX - 1f);
                float ty = numY <= 1 ? 0.5f : y / (numY - 1f);

                float angle = rng.NextFloat() * 3.14f * 2;
                float2 dir = new(math.cos(angle), math.sin(angle));
                float2 jitter = (rng.NextFloat() - 0.5f) * pss.ValueRO.jitterStrength * dir;
                float2 position = new float2((tx - 0.5f) * pss.ValueRO.spawnSize.x, (ty - 0.5f) * pss.ValueRO.spawnSize.y) + jitter + pss.ValueRO.spawnCenter;
                float2 velocity = pss.ValueRO.initialVelocity;

                pSystemState.EntityManager.SetComponentData(entity, new LocalTransform
                {
                    Position = new float3(position.x, position.y, 0),
                    Scale = 1f,
                    Rotation = quaternion.identity,
                });
                pSystemState.EntityManager.SetComponentData(entity, new PhysicsVelocity
                {
                    Linear = new float3(velocity.x, velocity.y, 0),
                });
                pSystemState.EntityManager.SetComponentData(entity, new Particle
                {
                    id = i,
                });

                i++;
            }
        }

        pss.ValueRW.doSpawn = false;
        instiatedEntities.Dispose();

        //float deltaTime = 1 / 60f;
        //SystemAPI.Time.fixedDeltaTime = deltaTime; 

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState pSystemState)
    {
        
    }
}
