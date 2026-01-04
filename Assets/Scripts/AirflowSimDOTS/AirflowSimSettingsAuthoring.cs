using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class AirflowSimSettingsAuthoring : MonoBehaviour
{
    [SerializeField] private AirflowSimSettingsSO airflowSimSettingsSO;

    private class Baker : Baker<AirflowSimSettingsAuthoring>
    {
        public override void Bake(AirflowSimSettingsAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new AirflowSimSettings
            {
                smoothingRadius = pAuthoring.airflowSimSettingsSO.smoothingRadius,
                sqrRadius = pAuthoring.airflowSimSettingsSO.smoothingRadius * pAuthoring.airflowSimSettingsSO.smoothingRadius,
                targetDensity = pAuthoring.airflowSimSettingsSO.targetDensity,
                pressureMultiplier = pAuthoring.airflowSimSettingsSO.pressureMultiplier,
                nearPressureMultiplier = pAuthoring.airflowSimSettingsSO.nearPressureMultiplier,
                gravity = pAuthoring.airflowSimSettingsSO.gravity,
                collisionDampening = pAuthoring.airflowSimSettingsSO.collisionDampening,
                viscosityStrength = pAuthoring.airflowSimSettingsSO.viscosityStrength,
                boundsSize = pAuthoring.airflowSimSettingsSO.boundsSize,
                interactionInputStrength = pAuthoring.airflowSimSettingsSO.interactionInputStrength,
                interactionInputRadius = pAuthoring.airflowSimSettingsSO.interactionInputRadius
            });
        }
    }
}

[BurstCompile]
public struct AirflowSimSettings : IComponentData
{
    public float smoothingRadius;
    public float sqrRadius;
    public float targetDensity;
    public float pressureMultiplier;
    public float nearPressureMultiplier;
    public float gravity;
    public float collisionDampening;
    public float viscosityStrength;
    public float2 boundsSize;
    public float interactionInputStrength;
    public float interactionInputRadius;
}
