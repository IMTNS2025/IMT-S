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
    private bool doOnce;

    [BurstCompile]
    public void OnCreate(ref SystemState pSystemState)
    {
        pSystemState.RequireForUpdate<AirflowSimSettings>();
        pSystemState.RequireForUpdate<Particle>();
        doOnce = true;

        EntityQueryBuilder entityQueryDesc = new(Allocator.Temp);
        entityQueryDesc.WithAll<LocalTransform, PhysicsVelocity, Particle>();
        query = pSystemState.GetEntityQuery(entityQueryDesc);
        entityQueryDesc.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
        Init();
        
        AirflowSimCalculationJob airflowSimCalculationJob = new()
        {
            allParticles = query.ToComponentDataArray<Particle>(Allocator.TempJob),
            allParticleLTs = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob),
            allParticlePVs = query.ToComponentDataArray<PhysicsVelocity>(Allocator.TempJob),
            collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld,
            airflowSimSettings = airflowSimSettings,
            deltaTime = SystemAPI.Time.DeltaTime,
            spikyPow2ScalingFactor = spikyPow2ScalingFactor,
            spikyPow3ScalingFactor = spikyPow3ScalingFactor,
            spikyPow2DerivativeScalingFactor = spikyPow2DerivativeScalingFactor,
            spikyPow3DerivativeScalingFactor = spikyPow3DerivativeScalingFactor,
        };
        pSystemState.Dependency = airflowSimCalculationJob.ScheduleParallel(pSystemState.Dependency);
    }

    private void Init()
    {
        if (doOnce)
        {
            airflowSimSettings = SystemAPI.GetSingleton<AirflowSimSettings>();

            spikyPow2ScalingFactor = 6 / (math.PI * math.pow(airflowSimSettings.smoothingRadius, 4));
            spikyPow3ScalingFactor = 10 / (math.PI * math.pow(airflowSimSettings.smoothingRadius, 5));

            spikyPow2DerivativeScalingFactor = 12 / (math.pow(airflowSimSettings.smoothingRadius, 4) * math.PI);
            spikyPow3DerivativeScalingFactor = 30 / (math.pow(airflowSimSettings.smoothingRadius, 5) * math.PI);

            doOnce = false;
        }
    }
}

