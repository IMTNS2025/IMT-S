using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;

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
    public void Execute(ref Particle pParticleA, ref LocalTransform pLocalTransformA, ref URPMaterialPropertyBaseColor color)
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

        // Iterate through neighboring cells (3x3 grid around center cell)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int2 neighborCell = new int2(centerCell.x + dx, centerCell.y + dy);
                int hash = HashCell(neighborCell);

                // Iterate through all particles in this cell
                if (spatialHashMap.TryGetFirstValue(hash, out int neighborIndex, out var iterator))
                {
                    do
                    {
                        Particle pB = allParticles[neighborIndex];
                        if (pB.id == pParticleA.id)
                            continue;

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

                        float denomDensity = math.max(neighbourDensity, kEpsilon);
                        float denomNearDensity = math.max(neighbourNearDensity, kEpsilon);

                        float invDst = dst > 0f ? math.rcp(dst) : 0f;
                        float2 dirToNeighbour = dst > 0f ? offset * invDst : new float2(0f, 1f);

                        totalPressureForce += dirToNeighbour * DerivativeSpikyPow2(dst, radius) * sharedPressure / denomDensity;
                        totalPressureForce += dirToNeighbour * DerivativeSpikyPow3(dst, radius) * sharedNearPressure / denomNearDensity;

                        // Viscosity
                        float2 neighbourVelocity = pB.velocity;
                        totalViscosityForce += (neighbourVelocity - pParticleA.velocity) * SmoothingKernelPoly6(dst, radius);

                    } while (spatialHashMap.TryGetNextValue(out neighborIndex, ref iterator));
                }
            }
        }

        // Apply accumulated effects
        pParticleA.density = totalDensity;
        pParticleA.densityNear = totalNearDensity;

        float invDensity = math.rcp(math.max(pParticleA.density, 1e-6f));
        float2 pressureAcceleration = totalPressureForce * invDensity;
        pParticleA.velocity += pressureAcceleration * deltaTime;

        float2 viscosityVel = airflowSimSettings.viscosityStrength * totalViscosityForce;
        pParticleA.velocity += viscosityVel * deltaTime;

        // Clamp maximum velocity to prevent unrealistic speeds
        float maxSpeed = airflowSimSettings.maxParticleSpeed;
        float speedSq = math.lengthsq(pParticleA.velocity);
        if (speedSq > maxSpeed * maxSpeed)
        {
            float speed = math.sqrt(speedSq);
            pParticleA.velocity = pParticleA.velocity * (maxSpeed / speed);
        }

        // Integrate position
        pos2 += pParticleA.velocity * deltaTime;
        pLocalTransformA.Position = new float3(pos2.x, pos2.y, pLocalTransformA.Position.z);
        pParticleA.predictedPosition = pos2;

        // Collisions
        HandleCollisions(ref pLocalTransformA, ref pParticleA);

        // Color
        ApplySpeedColor(ref color, pParticleA.velocity);
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
        // Use prime number hash with addition to avoid XOR symmetry issues
        // Adding a large prime offset prevents (0,0) from hashing to 0
        // and reduces collisions for symmetric cell coordinates
        // Same constants as BuildSpatialHashMapJob for consistency
        const int p1 = 73856093;
        const int p2 = 19349663;
        const int offset = 83492791;
        return ((cell.x * p1) + (cell.y * p2) + offset);
    }

    private float SpikyKernelPow2(float dst, float radius)
    {
        if (dst < radius)
        {
            float v = radius - dst;
            return v * v * spikyPow2ScalingFactor;
        }
        return 0f;
    }

    private float SpikyKernelPow3(float dst, float radius)
    {
        if (dst < radius)
        {
            float v = radius - dst;
            return v * v * v * spikyPow3ScalingFactor;
        }
        return 0f;
    }

    private float DerivativeSpikyPow2(float dst, float radius)
    {
        if (dst <= radius)
        {
            float v = radius - dst;
            return -v * spikyPow2DerivativeScalingFactor;
        }
        return 0f;
    }

    private float DerivativeSpikyPow3(float dst, float radius)
    {
        if (dst <= radius)
        {
            float v = radius - dst;
            return -v * v * spikyPow3DerivativeScalingFactor;
        }
        return 0f;
    }

    private float PressureFromDensity(float density)
    {
        return (density - airflowSimSettings.targetDensity) * airflowSimSettings.pressureMultiplier;
    }

    private float NearPressureFromDensity(float nearDensity)
    {
        return airflowSimSettings.nearPressureMultiplier * nearDensity;
    }

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

    private float2 ExternalForces(float3 pos, float2 velocity)
    {
        float2 gravityAccel = new float2(0f, airflowSimSettings.gravity);

        if (!input.isActive)
            return gravityAccel;

        float2 particlePos = new float2(pos.x, pos.y);
        float obstacleRadius = airflowSimSettings.interactionInputRadius;
        float2 obstaclePos = input.lineEnd; // Current obstacle position

        // Simple distance from current obstacle position
        float2 toParticle = particlePos - obstaclePos;
        float dist = math.length(toParticle);

        // Influence radius
        float influenceRadius = obstacleRadius * 2.5f;

        // Outside influence - no effect
        if (dist > influenceRadius)
            return gravityAccel;

        // Direction from obstacle to particle
        float2 radialDir = (dist > 1e-5f) ? (toParticle / dist) : new float2(0f, 1f);

        float2 totalAccel = float2.zero;
        float dt = math.max(deltaTime, 1e-5f);
        float baseStrength = airflowSimSettings.interactionInputStrength;

        // Normalized distance (0 at center, 1 at influence edge)
        float normalizedDist = dist / influenceRadius;
        float proximity = 1f - normalizedDist;
        proximity = proximity * proximity; // Quadratic falloff

        // === PHASE 1: HARD BOUNDARY - Push particles out of the obstacle ===
        if (dist < obstacleRadius)
        {
            float penetration = obstacleRadius - dist;
            // Strong push outward
            float pushStrength = (penetration / obstacleRadius) * baseStrength * 3f;
            totalAccel += radialDir * pushStrength;

            // Cancel velocity into obstacle
            float velInward = -math.dot(velocity, radialDir);
            if (velInward > 0f)
            {
                totalAccel += radialDir * velInward / dt;
            }
        }

        // === PHASE 2: MOVING OBSTACLE - Push particles in movement direction ===
        float2 obstacleVel = input.velocity;
        float obstacleSpeed = math.length(obstacleVel);

        if (obstacleSpeed > 0.5f)
        {
            float2 moveDir = obstacleVel / obstacleSpeed;

            // Clamp speed using maxObstacleSpeed for stability
            float maxSpeed = airflowSimSettings.maxObstacleSpeed;
            float clampedSpeed = math.min(obstacleSpeed, maxSpeed);
            float speedFactor = clampedSpeed / maxSpeed;

            // How much is this particle in the direction of movement?
            // dotProduct > 0 means particle is ahead, < 0 means behind
            float dotProduct = math.dot(radialDir, moveDir);

            // FORWARD PUSH: All particles get pushed in movement direction
            // Stronger for particles ahead, weaker for particles behind
            float forwardFactor = math.saturate(0.5f + dotProduct * 0.5f); // 0 to 1, with 0.5 at perpendicular
            float forwardStrength = proximity * forwardFactor * speedFactor * baseStrength * 1.5f;
            totalAccel += moveDir * forwardStrength;

            // RADIAL PUSH: Push particles outward (around the obstacle)
            // Stronger for particles perpendicular to movement
            float perpFactor = 1f - math.abs(dotProduct); // Strongest when perpendicular
            float radialStrength = proximity * perpFactor * speedFactor * baseStrength * 0.8f;
            totalAccel += radialDir * radialStrength;

            // EXTRA FORWARD PUSH for particles directly ahead
            if (dotProduct > 0.5f) // Particle is in front
            {
                float aheadBonus = (dotProduct - 0.5f) * 2f; // 0 to 1 for particles ahead
                float bonusStrength = proximity * aheadBonus * speedFactor * baseStrength * 2f;
                totalAccel += moveDir * bonusStrength;
            }
        }

        // Clamp acceleration
        float maxAccel = baseStrength * 5f;
        float accelMag = math.length(totalAccel);
        if (accelMag > maxAccel)
        {
            totalAccel *= maxAccel / accelMag;
        }

        return gravityAccel + totalAccel;
    }

    private void ApplySpeedColor(ref URPMaterialPropertyBaseColor color, float2 velocity)
    {
        float speedSqr = math.lengthsq(velocity);
        float colorMaxSpeed = airflowSimSettings.colorGradientMaxSpeed;
        float invMaxSpeed = 1f / math.max(colorMaxSpeed, 0.001f); // Avoid division by zero
        float t = math.saturate(math.sqrt(speedSqr) * invMaxSpeed);

        float3 slowColor = airflowSimSettings.slowParticleColor;
        float3 fastColor = airflowSimSettings.fastParticleColor;
        float3 rgb = math.lerp(slowColor, fastColor, t);

        color.Value = new float4(rgb, 1f);
    }
}