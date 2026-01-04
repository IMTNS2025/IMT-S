using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;

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

        // Get particle data for spatial hash map construction
        NativeArray<Particle> allParticles = query.ToComponentDataArray<Particle>(Allocator.TempJob);
        NativeArray<LocalTransform> allParticleLTs = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

        int particleCount = allParticles.Length;
        float cellSize = airflowSimSettings.smoothingRadius;

        // Create spatial hash map with capacity for expected number of entries
        // Using TempJob allocator for job lifetime
        NativeParallelMultiHashMap<int, int> spatialHashMap = new NativeParallelMultiHashMap<int, int>(
            particleCount * 4, // Account for particles potentially in multiple cells (border cases)
            Allocator.TempJob
        );

        // Schedule job to build spatial hash map
        BuildSpatialHashMapJob buildHashMapJob = new BuildSpatialHashMapJob
        {
            particles = allParticles,
            particleLTs = allParticleLTs,
            spatialHashMap = spatialHashMap.AsParallelWriter(),
            cellSize = cellSize,
            predictionFactor = airflowSimSettings.predictionFactor
        };

        JobHandle buildHashMapHandle = buildHashMapJob.Schedule(particleCount, 64, pSystemState.Dependency);

        // Schedule the main calculation job after hash map is built
        AirflowSimCalculationJob airflowSimCalculationJob = new()
        {
            allParticles = allParticles,
            allParticleLTs = allParticleLTs,
            spatialHashMap = spatialHashMap,
            collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld,
            airflowSimSettings = airflowSimSettings,
            deltaTime = SystemAPI.Time.DeltaTime,
            spikyPow2ScalingFactor = spikyPow2ScalingFactor,
            spikyPow3ScalingFactor = spikyPow3ScalingFactor,
            spikyPow2DerivativeScalingFactor = spikyPow2DerivativeScalingFactor,
            spikyPow3DerivativeScalingFactor = spikyPow3DerivativeScalingFactor,
            poly6ScalingFactor = poly6ScalingFactor,
            input = input,
            cellSize = cellSize
        };

        JobHandle calculationHandle = airflowSimCalculationJob.ScheduleParallel(buildHashMapHandle);

        // Use the built-in Dispose(JobHandle) method for proper deferred disposal
        // This schedules the disposal to occur after the calculation job completes
        JobHandle disposeParticlesHandle = allParticles.Dispose(calculationHandle);
        JobHandle disposeTransformsHandle = allParticleLTs.Dispose(calculationHandle);
        JobHandle disposeHashMapHandle = spatialHashMap.Dispose(calculationHandle);

        // Combine all dispose handles into the final dependency
        pSystemState.Dependency = JobHandle.CombineDependencies(
            disposeParticlesHandle,
            disposeTransformsHandle,
            disposeHashMapHandle
        );
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

/// <summary>
/// Job to build the spatial hash map from particle positions.
/// Uses predicted positions for consistency with neighbor lookups.
/// </summary>
[BurstCompile]
public struct BuildSpatialHashMapJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Particle> particles;
    [ReadOnly] public NativeArray<LocalTransform> particleLTs;
    public NativeParallelMultiHashMap<int, int>.ParallelWriter spatialHashMap;
    public float cellSize;
    public float predictionFactor;

    public void Execute(int index)
    {
        // Calculate predicted position for this particle
        float2 pos = new float2(particleLTs[index].Position.x, particleLTs[index].Position.y);
        float2 predictedPos = pos + particles[index].velocity * predictionFactor;

        // Calculate cell coordinates
        int2 cell = GetCell(predictedPos);

        // Hash the cell and add this particle index to the map
        int hash = HashCell(cell);
        spatialHashMap.Add(hash, index);
    }

    private int2 GetCell(float2 position)
    {
        return new int2(
            (int)math.floor(position.x / cellSize),
            (int)math.floor(position.y / cellSize)
        );
    }

    private int HashCell(int2 cell)
    {
        // Use prime number hash for better distribution
        // Constants chosen to minimize collisions for typical 2D grids
        const int p1 = 73856093;
        const int p2 = 19349663;
        return (cell.x * p1) ^ (cell.y * p2);
    }
}

