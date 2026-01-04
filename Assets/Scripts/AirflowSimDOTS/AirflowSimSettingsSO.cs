using UnityEngine;

[CreateAssetMenu(fileName = "AirflowSimSettingsSO", menuName = "Scriptable Objects/AirflowSimSettingsSO")]
public class AirflowSimSettingsSO : ScriptableObject
{
    // Smoothing radius (kernel support). For typical particle spacing ~0.4 this value
    // yields ~15-25 neighbors which is a stable range for SPH-style airflow sims.
    public float smoothingRadius = 1.0f;

    // Target density for pressure calculations — tuned to the initial rest density
    // produced by the default spawn settings (order of 10-30).
    public float targetDensity = 20f;

    // Pressure multipliers: air is relatively compressible vs liquids, so keep these small
    // to avoid large pressure forces and instability.
    public float pressureMultiplier = 0.1f;
    public float nearPressureMultiplier = 0.05f; 

    // No gravity by default for horizontal airflow simulation
    public float gravity = 0f;

    [Range(0f, 1f)] public float collisionDampening = 0.9f;

    // Low viscosity for air; increase if simulation appears too noisy.
    public float viscosityStrength = 0.001f;

    // Simulation bounds (world units)
    public Vector2 boundsSize = new(16f, 8f);

    [Header("User Input")]
    public float interactionInputStrength = 90f;
    public float interactionInputRadius = 2f;
}