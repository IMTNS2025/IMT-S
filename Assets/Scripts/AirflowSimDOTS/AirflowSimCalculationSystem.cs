using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System.Diagnostics;

[BurstCompile]
public partial struct AirflowSimCalculationSystem : ISystem
{
    private EntityQuery query;
    private AirflowSimSettings airflowSimSettings;
    private float spikyPow2ScalingFactor;
    private float spikyPow3ScalingFactor; 
    private float spikyPow2DerivativeScalingFactor;
    private float spikyPow3DerivativeScalingFactor;
    private float poly6ScalingFactor;
    private bool doOnce;

    [BurstCompile]
    public void OnCreate(ref SystemState pSystemState)
    {
        pSystemState.RequireForUpdate<AirflowSimSettings>();
        pSystemState.RequireForUpdate<Particle>();
        doOnce = true;

        EntityQueryBuilder entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAll<LocalTransform, Particle>();
        query = pSystemState.GetEntityQuery(entityQueryDesc);
        entityQueryDesc.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
        Init(ref pSystemState);      

        AirflowSimCalculationJob airflowSimCalculationJob = new()
        {
            allParticles = query.ToComponentDataArray<Particle>(Allocator.TempJob),
            allParticleLTs = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob),
            collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld,
            airflowSimSettings = airflowSimSettings,
            deltaTime = SystemAPI.Time.DeltaTime,
            spikyPow2ScalingFactor = spikyPow2ScalingFactor,
            spikyPow3ScalingFactor = spikyPow3ScalingFactor,
            spikyPow2DerivativeScalingFactor = spikyPow2DerivativeScalingFactor,
            spikyPow3DerivativeScalingFactor = spikyPow3DerivativeScalingFactor,
            poly6ScalingFactor = poly6ScalingFactor,
        };
        pSystemState.Dependency = airflowSimCalculationJob.ScheduleParallel(pSystemState.Dependency);
    }

    private void Init(ref SystemState pSystemState)
    {
        if (doOnce)
        {
            airflowSimSettings = SystemAPI.GetSingleton<AirflowSimSettings>();

            spikyPow2ScalingFactor = 6 / (math.PI * math.pow(airflowSimSettings.smoothingRadius, 4));
            spikyPow3ScalingFactor = 10 / (math.PI * math.pow(airflowSimSettings.smoothingRadius, 5));

            spikyPow2DerivativeScalingFactor = 12 / (math.pow(airflowSimSettings.smoothingRadius, 4) * math.PI);
            spikyPow3DerivativeScalingFactor = 30 / (math.pow(airflowSimSettings.smoothingRadius, 5) * math.PI);

            poly6ScalingFactor = 4 / (math.PI * math.pow(airflowSimSettings.smoothingRadius, 8));

            // Initialize density fields for all particles (self-contribution using kernels at dst = 0)
            // This ensures particles have sensible starting density values before the job runs.
            var entities = query.ToEntityArray(Allocator.Temp);
            var particles = query.ToComponentDataArray<Particle>(Allocator.Temp);
            var localtransforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            for (int i = 0; i < particles.Length; i++)
            {
                // self-distance = 0
                float dst = 0f;
                float radius = airflowSimSettings.smoothingRadius;

                float density = 0f;
                float nearDensity = 0f;

                if (dst < radius)
                {
                    float v = radius - dst; // == radius
                    density = v * v * spikyPow2ScalingFactor;
                    nearDensity = v * v * v * spikyPow3ScalingFactor;
                }

                Particle p = particles[i];
                p.density = density;
                p.densityNear = nearDensity;
                p.predictedPosition = new float2(localtransforms[i].Position.x, localtransforms[i].Position.y);
                p.velocity = float2.zero;

                // write back
                pSystemState.EntityManager.SetComponentData(entities[i], p);
            }
            entities.Dispose();
            particles.Dispose();
            localtransforms.Dispose();

            doOnce = false;
        }
    }
}

