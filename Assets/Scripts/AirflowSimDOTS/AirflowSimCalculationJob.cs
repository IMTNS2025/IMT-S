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
    [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Particle> allParticles;
    [DeallocateOnJobCompletion][ReadOnly] public NativeArray<LocalTransform> allParticleLTs;
    [ReadOnly] public CollisionWorld collisionWorld;
    [ReadOnly] public AirflowSimSettings airflowSimSettings;
    [ReadOnly] public float deltaTime;
    [ReadOnly] public float spikyPow2ScalingFactor;
    [ReadOnly] public float spikyPow3ScalingFactor;
    [ReadOnly] public float spikyPow2DerivativeScalingFactor;
    [ReadOnly] public float spikyPow3DerivativeScalingFactor;
    [ReadOnly] public float poly6ScalingFactor;
    [ReadOnly] public InteractionInput input;

    // local temporaries are now computed per-loop inside helper functions
    private float2 offsetToNeighbor;
    private float sqrDstToNeighbor;
    private float dstToNeighbor;

    [BurstCompile]
    public void Execute(ref Particle pParticleA, ref LocalTransform pLocalTransformA, ref URPMaterialPropertyBaseColor color)
    {
        // Phase 0: Calcualte predicted position
        pParticleA.velocity += ExternalForces(pLocalTransformA.Position, pParticleA.velocity) * deltaTime;
        const float predictionFactor = 1 / 120.0f;
        float2 integratedPos2 = new float2(pLocalTransformA.Position.x, pLocalTransformA.Position.y) + pParticleA.velocity * predictionFactor;
        pParticleA.predictedPosition = integratedPos2;

        // Phase 1: Calculate densities from all neighbors (moved into helper)
        float2 densities = ComputeDensities(in pParticleA, pParticleA.predictedPosition);
        pParticleA.density = densities.x;
        pParticleA.densityNear = densities.y;

        // Phase 2: Calculate pressure forces from all neighbors (moved into helper)
        float2 totalPressureForce = ComputeTotalPressureForce(in pParticleA, pParticleA.predictedPosition);
        float2 pressureAcceleration = totalPressureForce / math.max(pParticleA.density, 1e-6f);
        pParticleA.velocity += pressureAcceleration * deltaTime;

        // Phase 3: Calculate viscosity from all neighbors (moved into helper)
        float2 totalViscosityForce = ComputeTotalViscosityForce(in pParticleA, pParticleA.predictedPosition);
        float2 viscosityVel = airflowSimSettings.viscosityStrength * totalViscosityForce;
        pParticleA.velocity += viscosityVel * deltaTime;

        // Phase 4: Integrate position using particle.velocity (set position directly)
        integratedPos2 = new float2(pLocalTransformA.Position.x, pLocalTransformA.Position.y) + pParticleA.velocity * deltaTime;
        pLocalTransformA.Position = new float3(integratedPos2.x, integratedPos2.y, pLocalTransformA.Position.z);
        pParticleA.predictedPosition = integratedPos2;

        // Phase 5: Run collision handling (update velocity and position via Particle.velocity)
        HandleCollisions(ref pLocalTransformA, ref pParticleA);

        // Maps particle speed to a blue (slow) -> red (fast) gradient.
        ApplySpeedColor(ref color, pParticleA.velocity);
    }

    // Compute summed density and near-density for particle A from all neighbors
    private float2 ComputeDensities(in Particle pParticleA, float2 predictedPos)
    {
        // Include self-contribution so density is never zero
        float totalDensity = SpikyKernelPow2(0f, airflowSimSettings.smoothingRadius);
        float totalNearDensity = SpikyKernelPow3(0f, airflowSimSettings.smoothingRadius);

        for (int i = 0; i < allParticles.Length; i++)
        {
            Particle particleB = allParticles[i];
            if (pParticleA.id == particleB.id)
                continue;

            LocalTransform localTransformB = allParticleLTs[i];

            float2 offset = new float2(localTransformB.Position.x, localTransformB.Position.y) - predictedPos;
            float sqrDst = math.dot(offset, offset);

            if (sqrDst > airflowSimSettings.sqrRadius)
                continue;

            float dst = math.sqrt(sqrDst);
            totalDensity += SpikyKernelPow2(dst, airflowSimSettings.smoothingRadius);
            totalNearDensity += SpikyKernelPow3(dst, airflowSimSettings.smoothingRadius);
        }

        return new float2(totalDensity, totalNearDensity);
    }

    // Compute summed pressure force on particle A from all neighbors
    private float2 ComputeTotalPressureForce(in Particle pParticleA, float2 predictedPos)
    {
        float2 totalPressureForce = float2.zero;

        float pressureA = PressureFromDensity(pParticleA.density);
        float nearPressureA = NearPressureFromDensity(pParticleA.densityNear);

        for (int i = 0; i < allParticles.Length; i++)
        {
            Particle particleB = allParticles[i];
            if (pParticleA.id == particleB.id)
                continue;

            LocalTransform localTransformB = allParticleLTs[i];

            float2 offset = new float2(localTransformB.Position.x, localTransformB.Position.y) - predictedPos;
            float sqrDst = math.dot(offset, offset);

            if (sqrDst > airflowSimSettings.sqrRadius)
                continue;

            float dst = math.sqrt(sqrDst);
            float2 dirToNeighbour = dst > 0 ? offset / dst : new float2(0, 1);

            float neighbourDensity = particleB.density;
            float neighbourNearDensity = particleB.densityNear;
            float neighbourPressure = PressureFromDensity(neighbourDensity);
            float neighbourNearPressure = NearPressureFromDensity(neighbourNearDensity);

            float sharedPressure = (pressureA + neighbourPressure) * 0.5f;
            float sharedNearPressure = (nearPressureA + neighbourNearPressure) * 0.5f;

            const float kEpsilon = 1e-6f;
            float denomDensity = math.max(neighbourDensity, kEpsilon);
            float denomNearDensity = math.max(neighbourNearDensity, kEpsilon);

            totalPressureForce += dirToNeighbour * DerivativeSpikyPow2(dst, airflowSimSettings.smoothingRadius) * sharedPressure / denomDensity;
            totalPressureForce += dirToNeighbour * DerivativeSpikyPow3(dst, airflowSimSettings.smoothingRadius) * sharedNearPressure / denomNearDensity;
        }

        return totalPressureForce;
    }

    // Compute summed viscosity contribution from neighbors
    private float2 ComputeTotalViscosityForce(in Particle pParticleA, float2 predictedPos)
    {
        float2 totalViscosityForce = float2.zero;

        for (int i = 0; i < allParticles.Length; i++)
        {
            Particle particleB = allParticles[i];
            if (pParticleA.id == particleB.id)
                continue;

            LocalTransform localTransformB = allParticleLTs[i];

            float2 offset = new float2(localTransformB.Position.x, localTransformB.Position.y) - predictedPos;
            float sqrDst = math.dot(offset, offset);

            if (sqrDst > airflowSimSettings.sqrRadius)
                continue;

            float dst = math.sqrt(sqrDst);

            float2 neighbourVelocity = particleB.velocity;
            totalViscosityForce += (neighbourVelocity - pParticleA.velocity) * SmoothingKernelPoly6(dst, airflowSimSettings.smoothingRadius);
        }

        return totalViscosityForce;
    }

    private readonly float2 CalculateDensity()
    {
        float density = 0;
        float nearDensity = 0;

        // Calculate density and near density
        float dst = math.sqrt(sqrDstToNeighbor);
        density += SpikyKernelPow2(dst, airflowSimSettings.smoothingRadius);
        nearDensity += SpikyKernelPow3(dst, airflowSimSettings.smoothingRadius);

        return new float2(density, nearDensity);
    }

    private readonly float SpikyKernelPow2(float dst, float radius)
    {
        if (dst < radius)
        {
            float v = radius - dst;
            return v * v * spikyPow2ScalingFactor;
        }
        return 0;
    }

    private readonly float SpikyKernelPow3(float dst, float radius)
    {
        if (dst < radius)
        {
            float v = radius - dst;
            return v * v * v * spikyPow3ScalingFactor;
        }
        return 0;
    }

    private readonly float2 CalculatePressureForce(Particle pParticleA, Particle pParticleB)
    {
        float density = pParticleA.density;
        float densityNear = pParticleA.densityNear;
        float pressure = PressureFromDensity(density);
        float nearPressure = NearPressureFromDensity(densityNear);
        float2 pressureForce = 0;

        float2 pos = pParticleA.predictedPosition;

        float2 neighbourPos = pParticleB.predictedPosition;

        // Calculate pressure force
        float2 dirToNeighbour = dstToNeighbor > 0 ? offsetToNeighbor / dstToNeighbor : new float2(0, 1);

        float neighbourDensity = pParticleB.density;
        float neighbourNearDensity = pParticleB.densityNear;
        float neighbourPressure = PressureFromDensity(neighbourDensity);
        float neighbourNearPressure = NearPressureFromDensity(neighbourNearDensity);

        float sharedPressure = (pressure + neighbourPressure) * 0.5f;
        float sharedNearPressure = (nearPressure + neighbourNearPressure) * 0.5f;

        // Avoid division by zero or extremely small densities which can create huge forces.
        const float kEpsilon = 1e-6f;
        float denomDensity = math.max(neighbourDensity, kEpsilon);
        float denomNearDensity = math.max(neighbourNearDensity, kEpsilon);

        pressureForce += dirToNeighbour * DerivativeSpikyPow2(dstToNeighbor, airflowSimSettings.smoothingRadius) * sharedPressure / denomDensity;
        pressureForce += dirToNeighbour * DerivativeSpikyPow3(dstToNeighbor, airflowSimSettings.smoothingRadius) * sharedNearPressure / denomNearDensity;

        return pressureForce;
    }

    private readonly float DerivativeSpikyPow2(float dst, float radius)
    {
        if (dst <= radius)
        {
            float v = radius - dst;
            return -v * spikyPow2DerivativeScalingFactor;
        }
        return 0;
    }

    private readonly float DerivativeSpikyPow3(float dst, float radius)
    {
        if (dst <= radius)
        {
            float v = radius - dst;
            return -v * v * spikyPow3DerivativeScalingFactor;
        }
        return 0;
    }

    private readonly float PressureFromDensity(float density)
    {
        return (density - airflowSimSettings.targetDensity) * airflowSimSettings.pressureMultiplier;
    }

    private readonly float NearPressureFromDensity(float nearDensity)
    {
        return airflowSimSettings.nearPressureMultiplier * nearDensity;
    }

    private readonly float2 CalculateViscosity(Particle pParticleA, float2 velA, float2 velB)
    {
        return (velB - velA) * SmoothingKernelPoly6(dstToNeighbor, airflowSimSettings.smoothingRadius);
    }

    private readonly float SmoothingKernelPoly6(float dst, float radius)
    {
        if (dst < radius)
        {
            float v = radius * radius - dst * dst;
            return v * v * v * poly6ScalingFactor;
        }
        return 0;
    }

    private readonly void HandleCollisions(ref LocalTransform localTransformA, ref Particle particleA)
    {
        // Keep particle inside bounds. If your bounds have a center, replace boundsCenter with that value.
        float2 boundsCenter = float2.zero;
        float2 halfSize = airflowSimSettings.boundsSize * 0.5f;
        float2 pos = new (localTransformA.Position.x, localTransformA.Position.y); //TODOALEX test predicted pos???
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
            const float eps = 1e-4f;
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

    private readonly float2 ExternalForces(float3 pos, float2 velocity)
    {
        // Gravity
        float2 gravityAccel = new (0, airflowSimSettings.gravity);

        // Input interactions modify gravity
        if (airflowSimSettings.interactionInputStrength != 0 && input.active)
        {
            float2 inputPointOffset = input.point - new float2(pos.x, pos.y);
            float sqrDst = math.dot(inputPointOffset, inputPointOffset);
            if (sqrDst < airflowSimSettings.interactionInputRadius * airflowSimSettings.interactionInputRadius)
            {
                float dst = math.sqrt(sqrDst);
                float edgeT = (dst / airflowSimSettings.interactionInputRadius);
                float centreT = 1 - edgeT;
                float2 dirToCentre = inputPointOffset / dst;

                float gravityWeight = 1 - (centreT * math.saturate(airflowSimSettings.interactionInputStrength / 10));
                float2 accel = gravityAccel * gravityWeight + dirToCentre * centreT * airflowSimSettings.interactionInputStrength;
                accel -= velocity * centreT;
                return accel;
            }
        }

        return gravityAccel;
    }

    // Compute speed
    private void ApplySpeedColor(ref URPMaterialPropertyBaseColor color, float2 velocity)
    {
        // Compute speed
        float speed = math.length(velocity);

        // Choose a max speed at which color is fully red
        const float maxSpeed = 1f; // tune for your simulation
        float t = math.saturate(speed / maxSpeed);

        // Blue (slow) and Red (fast)
        float3 slowColor = new float3(0f, 0f, 1f); // blue
        float3 fastColor = new float3(1f, 0f, 0f); // red

        float3 rgb = math.lerp(slowColor, fastColor, t);

        // If URPMaterialPropertyBaseColor stores color in .Value (float4), set it here.
        color.Value = new float4(rgb, 1f);
    }
}
