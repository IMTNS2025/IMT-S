using UnityEngine;
using Unity.Entities;

/// <summary>
/// Authoring component for ParticleReloadRequest.
/// This creates a singleton entity that can be used to request particle reloads.
/// </summary>
public class ParticleReloadRequestAuthoring : MonoBehaviour
{
    private class Baker : Baker<ParticleReloadRequestAuthoring>
    {
        public override void Bake(ParticleReloadRequestAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ParticleReloadRequest
            {
                shouldReload = false,
                newParticleCount = -1
            });
        }
    }
}
