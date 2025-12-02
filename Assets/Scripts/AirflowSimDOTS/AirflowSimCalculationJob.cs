using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
public partial struct AirflowSimCalculationJob : IJobEntity
{
    [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Particle> allParticles;
    [DeallocateOnJobCompletion][ReadOnly] public NativeArray<LocalTransform> allParticleLTs;
    [DeallocateOnJobCompletion][ReadOnly] public NativeArray<PhysicsVelocity> allParticlePVs;
    [ReadOnly] public CollisionWorld collisionWorld;
    [ReadOnly] public AirflowSimSettings airflowSimSettings;
    [ReadOnly] public float deltaTime;
    [ReadOnly] public float spikyPow2ScalingFactor;
    [ReadOnly] public float spikyPow3ScalingFactor;
    [ReadOnly] public float spikyPow2DerivativeScalingFactor;
    [ReadOnly] public float spikyPow3DerivativeScalingFactor;

    private float2 offsetToNeighbor;
    private float sqrDstToNeighbor; 

    [BurstCompile]
    public void Execute(ref Particle pParticleA, ref LocalTransform pLocalTransformA, ref PhysicsVelocity pPhysicsVelocityA)
    {
        for (int i = 0; i < allParticles.Length; i++)
        {
            Particle particleB = allParticles[i];

            if (pParticleA.id == particleB.id)
                continue;

            LocalTransform localTransformB = allParticleLTs[i];

            offsetToNeighbor = new float2(localTransformB.Position.x, localTransformB.Position.y) - new float2(pLocalTransformA.Position.x, pLocalTransformA.Position.y);
            sqrDstToNeighbor = offsetToNeighbor.x * offsetToNeighbor.x + offsetToNeighbor.y * offsetToNeighbor.y;

            if (sqrDstToNeighbor >= airflowSimSettings.sqrRadius)
                continue;

            //ExternalForces();
            float2 density = CalculateDensity();
            pParticleA.density = density.x;
            pParticleA.densityNear = density.y;
            float3 pressure = CalculatePressureForce(pParticleA, particleB);
            pPhysicsVelocityA.Linear += pressure;
            //CalculateViscosity();
            //UpdatePositions();
            NativeArray<float2> collisionResults = HandleCollisions(pLocalTransformA, pPhysicsVelocityA);
            pLocalTransformA.Position = new float3(collisionResults[0].x, collisionResults[0].y, 0);
            pPhysicsVelocityA.Linear = new float3(collisionResults[1].x, collisionResults[1].y, 0);
        }
    }

    private readonly float2 CalculateDensity()
    {
        float density = 0;
        float nearDensity = 0;

        float sqrDstToNeighbour = math.dot(offsetToNeighbor, offsetToNeighbor);

        // Calculate density and near density
        float dst = math.sqrt(sqrDstToNeighbour);
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


    private readonly float3 CalculatePressureForce(Particle pParticleA, Particle pParticleB)
    {
        float density = pParticleA.density;
        float densityNear = pParticleA.densityNear;
        float pressure = PressureFromDensity(density);
        float nearPressure = NearPressureFromDensity(densityNear);
        float2 pressureForce = 0;

        float2 pos = pParticleA.predictedPosition;

        float2 neighbourPos = pParticleB.predictedPosition;

        // Calculate pressure force
        float dst = math.sqrt(sqrDstToNeighbor);
        float2 dirToNeighbour = dst > 0 ? offsetToNeighbor / dst : new float2(0, 1);

        float neighbourDensity = pParticleB.density == 0 ? 1 : pParticleB.density;
        float neighbourNearDensity = pParticleB.densityNear == 0 ? 1 : pParticleB.densityNear;
        float neighbourPressure = PressureFromDensity(neighbourDensity);
        float neighbourNearPressure = NearPressureFromDensity(neighbourNearDensity);

        float sharedPressure = (float)((pressure + neighbourPressure) * 0.5);
        float sharedNearPressure = (float)((nearPressure + neighbourNearPressure) * 0.5);

        pressureForce += dirToNeighbour * DerivativeSpikyPow2(dst, airflowSimSettings.smoothingRadius) * sharedPressure / neighbourDensity;
        pressureForce += dirToNeighbour * DerivativeSpikyPow3(dst, airflowSimSettings.smoothingRadius) * sharedNearPressure / neighbourNearDensity;

        float2 acceleration = pressureForce / density;
        return new float3(acceleration.x, acceleration.y, 0) * deltaTime;
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

    //private void UpdatePositions(uint3 id)
    //{
    //    Positions[id.x] += Velocities[id.x] * deltaTime;
    //    //HandleCollisions(id.x);
    //}

    //private float2 ExternalForces(float2 pos, float2 velocity)
    //{
    //    // Gravity
    //    float2 gravityAccel = float2(0, gravity);

    //    // Input interactions modify gravity
    //    if (interactionInputStrength != 0)
    //    {
    //        float2 inputPointOffset = interactionInputPoint - pos;
    //        float sqrDst = dot(inputPointOffset, inputPointOffset);
    //        if (sqrDst < interactionInputRadius * interactionInputRadius)
    //        {
    //            float dst = sqrt(sqrDst);
    //            float edgeT = (dst / interactionInputRadius);
    //            float centreT = 1 - edgeT;
    //            float2 dirToCentre = inputPointOffset / dst;

    //            float gravityWeight = 1 - (centreT * saturate(interactionInputStrength / 10));
    //            float2 accel = gravityAccel * gravityWeight + dirToCentre * centreT * interactionInputStrength;
    //            accel -= velocity * centreT;
    //            return accel;
    //        }
    //    }

    //    return gravityAccel;
    //}

    private readonly NativeArray<float2> HandleCollisions(LocalTransform localTransformA, PhysicsVelocity physicsVelocityA)
    {
        // Keep particle inside bounds
        float2 halfSize = airflowSimSettings.boundsSize * 0.5f;
        float2 pos = new (localTransformA.Position.x, localTransformA.Position.y);
        float2 vel = new (physicsVelocityA.Linear.x, physicsVelocityA.Linear.y);
        float2 edgeDst = halfSize - math.abs(pos);

        if (edgeDst.x <= 0)
        {
            pos.x = halfSize.x * math.sign(pos.x);
            vel.x *= -1 * airflowSimSettings.collisionDampening;
        }
        if (edgeDst.y <= 0)
        {
            pos.y = halfSize.y * math.sign(pos.y);
            vel.y *= -1 * airflowSimSettings.collisionDampening;
        }
        NativeArray<float2> res = new (2, Allocator.Temp);
        res[0] = pos;
        res[1] = vel;
        return res;
    }

    //private void CalculateViscosity(uint3 id : SV_DispatchThreadID)
    //{
    // if (id.x >= numParticles) return;


    //    float2 pos = PredictedPositions[id.x];
    //    int2 originCell = GetCell2D(pos, smoothingRadius);
    //    float sqrRadius = smoothingRadius * smoothingRadius;

    //    float2 viscosityForce = 0;
    //    float2 velocity = Velocities[id.x];

    //    for (int i = 0; i < 9; i++)
    //    {
    //        uint hash = HashCell2D(originCell + offsets2D[i]);
    //        uint key = KeyFromHash(hash, numParticles);
    //        uint currIndex = SpatialOffsets[key];

    //        while (currIndex < numParticles)
    //        {
    //            uint3 indexData = SpatialIndices[currIndex];
    //            currIndex++;
    //            // Exit if no longer looking at correct bin
    //            if (indexData[2] != key) break;
    //            // Skip if hash does not match
    //            if (indexData[1] != hash) continue;

    //            uint neighbourIndex = indexData[0];
    //            // Skip if looking at self
    //            if (neighbourIndex == id.x) continue;

    //            float2 neighbourPos = PredictedPositions[neighbourIndex];
    //            float2 offsetToNeighbour = neighbourPos - pos;
    //            float sqrDstToNeighbour = dot(offsetToNeighbour, offsetToNeighbour);

    //            // Skip if not within radius
    //            if (sqrDstToNeighbour > sqrRadius) continue;

    //            float dst = sqrt(sqrDstToNeighbour);
    //            float2 neighbourVelocity = Velocities[neighbourIndex];
    //            viscosityForce += (neighbourVelocity - velocity) * ViscosityKernel(ööödst, smoothingRadius);
    //        }

    //    }
    //    Velocities[id.x] += viscosityForce * viscosityStrength * deltaTime;
    //}

}
