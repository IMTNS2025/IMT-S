using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

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

        EntityQueryBuilder entityQueryDesc = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, Particle>();
        query = pSystemState.GetEntityQuery(entityQueryDesc);
        entityQueryDesc.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState pSystemState)
    {
        Init(ref pSystemState);

        InteractionInput input = SystemAPI.GetSingleton<InteractionInput>();

        AirflowSimCalculationJob airflowSimCalculationJob = new ()
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
            input = input,
        };
        pSystemState.Dependency = airflowSimCalculationJob.ScheduleParallel(pSystemState.Dependency);
    }

    private void Init(ref SystemState pSystemState)
    {
        if (!doOnce)
            return;

        airflowSimSettings = SystemAPI.GetSingleton<AirflowSimSettings>();

        float r = airflowSimSettings.smoothingRadius;
        float r2 = r * r;
        float r4 = r2 * r2;
        float r5 = r4 * r;
        float r8 = r4 * r4;

        float invPi = 1f / math.PI;
        spikyPow2ScalingFactor = 6f * invPi / r4;
        spikyPow3ScalingFactor = 10f * invPi / r5;

        spikyPow2DerivativeScalingFactor = 12f * invPi / r4;
        spikyPow3DerivativeScalingFactor = 30f * invPi / r5;

        poly6ScalingFactor = 4f * invPi / r8;

        var entities = query.ToEntityArray(Allocator.Temp);
        var particles = query.ToComponentDataArray<Particle>(Allocator.Temp);
        var localtransforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        float radius = airflowSimSettings.smoothingRadius;

        for (int i = 0; i < particles.Length; i++)
        {
            float density = radius * radius * spikyPow2ScalingFactor;
            float nearDensity = radius * radius * radius * spikyPow3ScalingFactor;

            Particle p = particles[i];
            p.density = density;
            p.densityNear = nearDensity;
            p.predictedPosition = new float2(localtransforms[i].Position.x, localtransforms[i].Position.y);
            p.velocity = float2.zero;

            pSystemState.EntityManager.SetComponentData(entities[i], p);
        }

        entities.Dispose();
        particles.Dispose();
        localtransforms.Dispose();

        doOnce = false;
    }
}

