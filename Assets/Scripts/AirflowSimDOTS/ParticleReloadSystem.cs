using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// System that handles reloading particles by destroying all existing particles and triggering a respawn.
/// </summary>
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(ParticleSpawnSystem))]
partial struct ParticleReloadSystem : ISystem
{
    private int framesSinceDestroy;
    private bool waitingToSpawn;
    private int pendingParticleCount;
    
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ParticleReloadRequest>();
        state.RequireForUpdate<ParticleSpawnSettings>();
        framesSinceDestroy = 0;
        waitingToSpawn = false;
        pendingParticleCount = -1;
    }

    public void OnUpdate(ref SystemState state)
    {
        // If we're waiting to spawn, check if enough frames have passed
        if (waitingToSpawn)
        {
            framesSinceDestroy++;
            
            // Wait 3 frames after destroying to ensure physics state is fully cleared
            if (framesSinceDestroy >= 3)
            {
                RefRW<ParticleSpawnSettings> spawnSettings = SystemAPI.GetSingletonRW<ParticleSpawnSettings>();
                
                if (pendingParticleCount > 0)
                {
                    spawnSettings.ValueRW.particleCount = pendingParticleCount;
                    Debug.Log($"[ParticleReloadSystem] Updated particle count to: {pendingParticleCount}");
                }
                
                // Trigger respawn
                spawnSettings.ValueRW.doSpawn = true;
                Debug.Log($"[ParticleReloadSystem] Triggered respawn with doSpawn = true (particle count: {spawnSettings.ValueRO.particleCount})");
                
                // Reset state
                waitingToSpawn = false;
                framesSinceDestroy = 0;
                pendingParticleCount = -1;
            }
            
            return;
        }
        
        // Check for new reload request
        RefRW<ParticleReloadRequest> reloadRequest = SystemAPI.GetSingletonRW<ParticleReloadRequest>();
        
        if (!reloadRequest.ValueRO.shouldReload)
            return;

        // Store values locally before structural changes
        int newParticleCount = reloadRequest.ValueRO.newParticleCount;
        
        Debug.Log($"[ParticleReloadSystem] Processing reload request for {newParticleCount} particles");

        // Get all particle entities
        EntityQuery particleQuery = SystemAPI.QueryBuilder().WithAll<Particle>().Build();
        
        int particleCount = particleQuery.CalculateEntityCount();
        Debug.Log($"[ParticleReloadSystem] Destroying {particleCount} existing particles");
        
        // Destroy all existing particles (this causes structural change)
        state.EntityManager.DestroyEntity(particleQuery);
        
        // Complete the entity command buffer to ensure all entities are destroyed
        state.EntityManager.CompleteAllTrackedJobs();
        
        // Get fresh reference to reload request after structural change
        RefRW<ParticleReloadRequest> reloadRequestAfter = SystemAPI.GetSingletonRW<ParticleReloadRequest>();
        
        // Reset the reload request
        reloadRequestAfter.ValueRW.shouldReload = false;
        reloadRequestAfter.ValueRW.newParticleCount = -1;
        
        // Set up delayed spawn
        waitingToSpawn = true;
        framesSinceDestroy = 0;
        pendingParticleCount = newParticleCount;
        
        Debug.Log($"[ParticleReloadSystem] Waiting 3 frames before respawning...");
    }
}

/// <summary>
/// Component data for requesting particle reload.
/// </summary>
public struct ParticleReloadRequest : IComponentData
{
    public bool shouldReload;
    public int newParticleCount; // -1 means use current particle count
}
