using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using System.Runtime.CompilerServices;

[BurstCompile]
public partial struct AirflowSimCalculationJob : IJobEntity
{
    [ReadOnly] public NativeArray<Particle> allParticles;
    [ReadOnly] public NativeArray<LocalTransform> allParticleLTs;
    [ReadOnly] public NativeParallelMultiHashMap<int, int> spatialHashMap;

    [ReadOnly] public CollisionWorld collisionWorld;
    [ReadOnly] public AirflowSimSettings airflowSimSettings;
    [ReadOnly] public float deltaTime;
    [ReadOnly] public float spikyPow2ScalingFactor;
    [ReadOnly] public float spikyPow3ScalingFactor;
    [ReadOnly] public float spikyPow2DerivativeScalingFactor;
    [ReadOnly] public float spikyPow3DerivativeScalingFactor;
    [ReadOnly] public float poly6ScalingFactor;
    [ReadOnly] public InteractionInput input;
    [ReadOnly] public float cellSize;

    [BurstCompile]
    public void Execute(ref Particle pParticleA, ref LocalTransform pLocalTransformA, ref URPMaterialPropertyBaseColor color, [EntityIndexInQuery] int entityIndexInQuery)
    {
        // Phase 0: predicted position (external forces)
        float2 pos2 = new float2(pLocalTransformA.Position.x, pLocalTransformA.Position.y);
        pParticleA.velocity += ExternalForces(pLocalTransformA.Position, pParticleA.velocity) * deltaTime;
        float predictionFactor = airflowSimSettings.predictionFactor;
        float2 predictedPos = pos2 + pParticleA.velocity * predictionFactor;
        pParticleA.predictedPosition = predictedPos;

        // Precompute neighbor data once per particle
        float totalDensity = 0f;
        float totalNearDensity = 0f;
        float2 totalPressureForce = float2.zero;
        float2 totalViscosityForce = float2.zero;

        float radius = airflowSimSettings.smoothingRadius;
        float sqrRadius = airflowSimSettings.sqrRadius;

        // self contribution
        totalDensity += SpikyKernelPow2(0f, radius);
        totalNearDensity += SpikyKernelPow3(0f, radius);

        // Cache pressure terms for particle A once
        float basePressureA = PressureFromDensity(pParticleA.density);
        float baseNearPressureA = NearPressureFromDensity(pParticleA.densityNear);

        const float kEpsilon = 1e-6f;

        // Get the cell for this particle's predicted position
        int2 centerCell = GetCell(predictedPos);

        // Manually unroll 3x3 neighbor cell iteration for better performance
        // This eliminates loop overhead for a small, fixed iteration count
        ProcessNeighborCell(centerCell + new int2(-1, -1), predictedPos, predictionFactor, radius, sqrRadius, kEpsilon, basePressureA, baseNearPressureA, ref totalDensity, ref totalNearDensity, ref totalPressureForce, ref totalViscosityForce, entityIndexInQuery, pParticleA.velocity);
        ProcessNeighborCell(centerCell + new int2(-1,  0), predictedPos, predictionFactor, radius, sqrRadius, kEpsilon, basePressureA, baseNearPressureA, ref totalDensity, ref totalNearDensity, ref totalPressureForce, ref totalViscosityForce, entityIndexInQuery, pParticleA.velocity);
        ProcessNeighborCell(centerCell + new int2(-1,  1), predictedPos, predictionFactor, radius, sqrRadius, kEpsilon, basePressureA, baseNearPressureA, ref totalDensity, ref totalNearDensity, ref totalPressureForce, ref totalViscosityForce, entityIndexInQuery, pParticleA.velocity);
        ProcessNeighborCell(centerCell + new int2( 0, -1), predictedPos, predictionFactor, radius, sqrRadius, kEpsilon, basePressureA, baseNearPressureA, ref totalDensity, ref totalNearDensity, ref totalPressureForce, ref totalViscosityForce, entityIndexInQuery, pParticleA.velocity);
        ProcessNeighborCell(centerCell + new int2( 0,  0), predictedPos, predictionFactor, radius, sqrRadius, kEpsilon, basePressureA, baseNearPressureA, ref totalDensity, ref totalNearDensity, ref totalPressureForce, ref totalViscosityForce, entityIndexInQuery, pParticleA.velocity);
        ProcessNeighborCell(centerCell + new int2( 0,  1), predictedPos, predictionFactor, radius, sqrRadius, kEpsilon, basePressureA, baseNearPressureA, ref totalDensity, ref totalNearDensity, ref totalPressureForce, ref totalViscosityForce, entityIndexInQuery, pParticleA.velocity);
        ProcessNeighborCell(centerCell + new int2( 1, -1), predictedPos, predictionFactor, radius, sqrRadius, kEpsilon, basePressureA, baseNearPressureA, ref totalDensity, ref totalNearDensity, ref totalPressureForce, ref totalViscosityForce, entityIndexInQuery, pParticleA.velocity);
        ProcessNeighborCell(centerCell + new int2( 1,  0), predictedPos, predictionFactor, radius, sqrRadius, kEpsilon, basePressureA, baseNearPressureA, ref totalDensity, ref totalNearDensity, ref totalPressureForce, ref totalViscosityForce, entityIndexInQuery, pParticleA.velocity);
        ProcessNeighborCell(centerCell + new int2( 1,  1), predictedPos, predictionFactor, radius, sqrRadius, kEpsilon, basePressureA, baseNearPressureA, ref totalDensity, ref totalNearDensity, ref totalPressureForce, ref totalViscosityForce, entityIndexInQuery, pParticleA.velocity);

        // Apply accumulated effects
        pParticleA.density = totalDensity;
        pParticleA.densityNear = totalNearDensity;

        float invDensity = math.rcp(math.max(pParticleA.density, 1e-6f));
        float2 pressureAcceleration = totalPressureForce * invDensity;
        pParticleA.velocity += pressureAcceleration * deltaTime;

        float2 viscosityVel = airflowSimSettings.viscosityStrength * totalViscosityForce;
        pParticleA.velocity += viscosityVel * deltaTime;

        // Integrate position
        pos2 += pParticleA.velocity * deltaTime;
        pLocalTransformA.Position = new float3(pos2.x, pos2.y, pLocalTransformA.Position.z);
        pParticleA.predictedPosition = pos2;

        // Collisions
        HandleCollisions(ref pLocalTransformA, ref pParticleA);

        // Color
        ApplySpeedColor(ref color, pParticleA.velocity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int2 GetCell(float2 position)
    {
        return new int2(
            (int)math.floor(position.x / cellSize),
            (int)math.floor(position.y / cellSize)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int HashCell(int2 cell)
    {
        // Use prime number hash with addition to avoid XOR symmetry issues
        // Adding a large prime offset prevents (0,0) from hashing to 0
        // and reduces collisions for symmetric cell coordinates
        // Same constants as BuildSpatialHashMapJob for consistency
        const int p1 = 73856093;
        const int p2 = 19349663;
        const int offset = 83492791;
        return ((cell.x * p1) + (cell.y * p2) + offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessNeighborCell(int2 neighborCell, float2 predictedPos, float predictionFactor, float radius, float sqrRadius, 
        float kEpsilon, float basePressureA, float baseNearPressureA, 
        ref float totalDensity, ref float totalNearDensity, ref float2 totalPressureForce, ref float2 totalViscosityForce, 
        int entityIndexInQuery, float2 particleVelocity)
    {
        int hash = HashCell(neighborCell);

        if (spatialHashMap.TryGetFirstValue(hash, out int neighborIndex, out var iterator))
        {
            do
            {
                // Skip self-interaction using entity index
                if (neighborIndex == entityIndexInQuery)
                    continue;

                Particle pB = allParticles[neighborIndex];
                LocalTransform ltB = allParticleLTs[neighborIndex];
                float2 bPos2 = new float2(ltB.Position.x, ltB.Position.y);
                float2 bPredictedPos = bPos2 + pB.velocity * predictionFactor;
                float2 offset = bPredictedPos - predictedPos;
                float sqrDst = math.dot(offset, offset);
                if (sqrDst > sqrRadius)
                    continue;

                float dst = math.sqrt(sqrDst);

                // Densities
                float k2 = SpikyKernelPow2(dst, radius);
                float k3 = SpikyKernelPow3(dst, radius);
                totalDensity += k2;
                totalNearDensity += k3;

                // Pressure forces
                float neighbourDensity = pB.density;
                float neighbourNearDensity = pB.densityNear;
                float neighbourPressure = PressureFromDensity(neighbourDensity);
                float neighbourNearPressure = NearPressureFromDensity(neighbourNearDensity);

                float sharedPressure = (basePressureA + neighbourPressure) * 0.5f;
                float sharedNearPressure = (baseNearPressureA + neighbourNearPressure) * 0.5f;

                // Use reciprocal to avoid division
                float invDenomDensity = math.rcp(math.max(neighbourDensity, kEpsilon));
                float invDenomNearDensity = math.rcp(math.max(neighbourNearDensity, kEpsilon));

                float invDst = dst > 0f ? math.rcp(dst) : 0f;
                float2 dirToNeighbour = dst > 0f ? offset * invDst : new float2(0f, 1f);

                totalPressureForce += dirToNeighbour * DerivativeSpikyPow2(dst, radius) * sharedPressure * invDenomDensity;
                totalPressureForce += dirToNeighbour * DerivativeSpikyPow3(dst, radius) * sharedNearPressure * invDenomNearDensity;

                // Viscosity
                totalViscosityForce += (pB.velocity - particleVelocity) * SmoothingKernelPoly6(dst, radius);

            } while (spatialHashMap.TryGetNextValue(out neighborIndex, ref iterator));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float SpikyKernelPow2(float dst, float radius)
    {
        if (dst < radius)
        {
            float v = radius - dst;
            return v * v * spikyPow2ScalingFactor;
        }
        return 0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float SpikyKernelPow3(float dst, float radius)
    {
        if (dst < radius)
        {
            float v = radius - dst;
            return v * v * v * spikyPow3ScalingFactor;
        }
        return 0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float DerivativeSpikyPow2(float dst, float radius)
    {
        if (dst <= radius)
        {
            float v = radius - dst;
            return -v * spikyPow2DerivativeScalingFactor;
        }
        return 0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float DerivativeSpikyPow3(float dst, float radius)
    {
        if (dst <= radius)
        {
            float v = radius - dst;
            return -v * v * spikyPow3DerivativeScalingFactor;
        }
        return 0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float PressureFromDensity(float density)
    {
        return (density - airflowSimSettings.targetDensity) * airflowSimSettings.pressureMultiplier;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float NearPressureFromDensity(float nearDensity)
    {
        return airflowSimSettings.nearPressureMultiplier * nearDensity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float SmoothingKernelPoly6(float dst, float radius)
    {
        if (dst < radius)
        {
            float v = radius * radius - dst * dst;
            return v * v * v * poly6ScalingFactor;
        }
        return 0f;
    }

    private void HandleCollisions(ref LocalTransform localTransformA, ref Particle particleA)
    {
        float2 boundsCenter = float2.zero;
        float2 halfSize = airflowSimSettings.boundsSize * 0.5f;
        float2 pos = new float2(localTransformA.Position.x, localTransformA.Position.y);
        float2 rel = pos - boundsCenter;

        bool collidedX = false;
        bool collidedY = false;

        float2 clampedRel = rel;

        if (rel.x > halfSize.x)
        {
            clampedRel.x = halfSize.x;
            collidedX = true;
        }
        else if (rel.x < -halfSize.x)
        {
            clampedRel.x = -halfSize.x;
            collidedX = true;
        }

        if (rel.y > halfSize.y)
        {
            clampedRel.y = halfSize.y;
            collidedY = true;
        }
        else if (rel.y < -halfSize.y)
        {
            clampedRel.y = -halfSize.y;
            collidedY = true;
        }

        if (collidedX || collidedY)
        {
            float eps = 0.00001f;
            float pushX = collidedX ? math.sign(clampedRel.x) * eps : 0f;
            float pushY = collidedY ? math.sign(clampedRel.y) * eps : 0f;

            float2 newPos = boundsCenter + clampedRel + new float2(pushX, pushY);

            if (collidedX)
                particleA.velocity.x = -particleA.velocity.x * airflowSimSettings.collisionDampening;
            if (collidedY)
                particleA.velocity.y = -particleA.velocity.y * airflowSimSettings.collisionDampening;

            localTransformA.Position = new float3(newPos.x, newPos.y, localTransformA.Position.z);
            particleA.predictedPosition = newPos;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float2 ExternalForces(float3 pos, float2 velocity)
    {
        float2 gravityAccel = new float2(0f, airflowSimSettings.gravity);

        // Early exit if no interaction
        if (!input.isActive)
            return gravityAccel;

        float2 particlePos = new float2(pos.x, pos.y);
        float obstacleRadius = airflowSimSettings.interactionInputRadius;
        float2 obstaclePos = input.lineEnd;

        // Simple distance from current obstacle position
        float2 toParticle = particlePos - obstaclePos;
        float sqrDist = math.dot(toParticle, toParticle);

        // Influence radius
        float influenceRadius = obstacleRadius * 2.5f;
        float sqrInfluenceRadius = influenceRadius * influenceRadius;

        // Early exit if outside influence
        if (sqrDist > sqrInfluenceRadius)
            return gravityAccel;

        float dist = math.sqrt(sqrDist);
        float invDist = dist > 1e-5f ? math.rcp(dist) : 0f;
        
        // Direction from obstacle to particle
        float2 radialDir = math.select(new float2(0f, 1f), toParticle * invDist, dist > 1e-5f);

        float2 totalAccel = float2.zero;
        float dt = math.max(deltaTime, 1e-5f);
        float baseStrength = airflowSimSettings.interactionInputStrength;

        // Normalized distance (0 at center, 1 at influence edge)
        float normalizedDist = dist * math.rcp(influenceRadius);
        float proximity = 1f - normalizedDist;
        proximity = proximity * proximity; // Quadratic falloff

        // === PHASE 1: HARD BOUNDARY - Push particles out of the obstacle ===
        bool isInsideObstacle = dist < obstacleRadius;
        if (isInsideObstacle)
        {
            float penetration = obstacleRadius - dist;
            float invObstacleRadius = math.rcp(obstacleRadius);
            // Strong push outward
            float pushStrength = (penetration * invObstacleRadius) * baseStrength * 3f;
            totalAccel += radialDir * pushStrength;

            // Cancel velocity into obstacle
            float velInward = -math.dot(velocity, radialDir);
            totalAccel += radialDir * math.max(velInward * math.rcp(dt), 0f);
        }

        // === PHASE 2: MOVING OBSTACLE - Push particles in movement direction ===
        float2 obstacleVel = input.velocity;
        float obstacleSpeedSq = math.lengthsq(obstacleVel);

        if (obstacleSpeedSq > 0.25f) // 0.5^2 = 0.25
        {
            float obstacleSpeed = math.sqrt(obstacleSpeedSq);
            float2 moveDir = obstacleVel * math.rcp(obstacleSpeed);

            // Clamp speed using maxObstacleSpeed for stability
            float maxSpeed = airflowSimSettings.maxObstacleSpeed;
            float clampedSpeed = math.min(obstacleSpeed, maxSpeed);
            float speedFactor = clampedSpeed * math.rcp(maxSpeed);

            // How much is this particle in the direction of movement?
            float dotProduct = math.dot(radialDir, moveDir);

            // FORWARD PUSH: All particles get pushed in movement direction
            float forwardFactor = math.saturate(0.5f + dotProduct * 0.5f);
            float forwardStrength = proximity * forwardFactor * speedFactor * baseStrength * 1.5f;
            totalAccel += moveDir * forwardStrength;

            // RADIAL PUSH: Push particles outward (around the obstacle)
            float perpFactor = 1f - math.abs(dotProduct);
            float radialStrength = proximity * perpFactor * speedFactor * baseStrength * 0.8f;
            totalAccel += radialDir * radialStrength;

            // EXTRA FORWARD PUSH for particles directly ahead
            float aheadBonus = math.max(dotProduct - 0.5f, 0f) * 2f;
            float bonusStrength = proximity * aheadBonus * speedFactor * baseStrength * 2f;
            totalAccel += moveDir * bonusStrength;
        }

        // Clamp acceleration using rsqrt for efficiency
        float maxAccel = baseStrength * 5f;
        float accelMagSq = math.lengthsq(totalAccel);
        float maxAccelSq = maxAccel * maxAccel;
        
        if (accelMagSq > maxAccelSq)
        {
            float invAccelMag = math.rsqrt(accelMagSq);
            totalAccel *= maxAccel * invAccelMag;
        }

        return gravityAccel + totalAccel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplySpeedColor(ref URPMaterialPropertyBaseColor color, float2 velocity)
    {
        float speedSqr = math.lengthsq(velocity);
        float colorMaxSpeed = airflowSimSettings.colorGradientMaxSpeed;
        float colorMaxSpeedSqr = colorMaxSpeed * colorMaxSpeed;
        // Avoid sqrt by working with squared values
        float t = math.saturate(math.sqrt(speedSqr / math.max(colorMaxSpeedSqr, 0.001f)));

        float3 slowColor = airflowSimSettings.slowParticleColor;
        float3 fastColor = airflowSimSettings.fastParticleColor;
        float3 rgb = math.lerp(slowColor, fastColor, t);

        color.Value = new float4(rgb, 1f);
    }
}