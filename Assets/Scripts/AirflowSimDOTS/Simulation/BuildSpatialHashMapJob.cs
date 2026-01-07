using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;

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

    private readonly int2 GetCell(float2 position)
    {
        return new int2(
            (int)math.floor(position.x / cellSize),
            (int)math.floor(position.y / cellSize)
        );
    }

    private readonly int HashCell(int2 cell)
    {
        // Use prime number hash with addition to avoid XOR symmetry issues
        // Adding a large prime offset prevents (0,0) from hashing to 0
        // and reduces collisions for symmetric cell coordinates
        const int p1 = 73856093;
        const int p2 = 19349663;
        const int offset = 83492791;
        return ((cell.x * p1) + (cell.y * p2) + offset);
    }
}