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

    [BurstCompile]
    public void Execute(ref Particle pParticleA, ref LocalTransform pLocalTransformA, ref URPMaterialPropertyBaseColor color)
    {
        // Phase 0: predicted position (external forces)
        float2 pos2 = new float2(pLocalTransformA.Position.x, pLocalTransformA.Position.y);
        pParticleA.velocity += ExternalForces(pLocalTransformA.Position, pParticleA.velocity) * deltaTime;
        const float predictionFactor = 1f / 120f;
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

        for (int i = 0; i < allParticles.Length; i++)
        {
            Particle pB = allParticles[i];
            if (pB.id == pParticleA.id)
                continue;

            LocalTransform ltB = allParticleLTs[i];
            float2 bPos2 = new float2(ltB.Position.x, ltB.Position.y);
            float2 offset = bPos2 - predictedPos;
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
        }

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

    private float2 ExternalForces(float3 pos, float2 velocity)
    {
        float2 gravityAccel = new float2(0f, airflowSimSettings.gravity);

        float interactionStrength = airflowSimSettings.interactionInputStrength;
        if (!input.active || interactionStrength == 0f)
        {
            return gravityAccel;
        }

        float2 inputPointOffset = input.point - new float2(pos.x, pos.y);
        float sqrDst = math.dot(inputPointOffset, inputPointOffset);
        float interactionRadius = airflowSimSettings.interactionInputRadius;
        float sqrInteractionRadius = interactionRadius * interactionRadius;

        if (sqrDst >= sqrInteractionRadius)
        {
            return gravityAccel;
        }

        float dst = math.sqrt(sqrDst);
        float edgeT = dst / interactionRadius;
        float centreT = 1f - edgeT;

        float invDst = dst > 0f ? math.rcp(dst) : 0f;
        float2 dirToCentre = dst > 0f ? inputPointOffset * invDst : new float2(0f, 1f);

        float strengthNorm = interactionStrength * 0.1f;
        float gravityWeight = 1f - (centreT * math.saturate(strengthNorm));
        float2 accel = gravityAccel * gravityWeight + dirToCentre * centreT * interactionStrength;
        accel -= velocity * centreT;
        return accel;
    }

    private void ApplySpeedColor(ref URPMaterialPropertyBaseColor color, float2 velocity)
    {
        float speedSqr = math.lengthsq(velocity);
        const float maxSpeed = 1f;
        float invMaxSpeed = 1f / maxSpeed;
        float t = math.saturate(math.sqrt(speedSqr) * invMaxSpeed);

        float3 slowColor = new float3(0f, 0f, 1f);
        float3 fastColor = new float3(1f, 0f, 0f);
        float3 rgb = math.lerp(slowColor, fastColor, t);

        color.Value = new float4(rgb, 1f);
    }
}
