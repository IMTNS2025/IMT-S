using UnityEngine;

[CreateAssetMenu(fileName = "AirflowSimSettingsSO", menuName = "Scriptable Objects/AirflowSimSettingsSO")]
public class AirflowSimSettingsSO : ScriptableObject
{
    [Header("Physics Core")]
    // Smoothing radius (kernel support). For typical particle spacing ~0.4 this value
    // yields ~15-25 neighbors which is a stable range for SPH-style airflow sims.
    public float smoothingRadius = 1.0f;

    // Target density for pressure calculations — LOW for gas behavior
    // Gases want to spread out, not clump together
    [Tooltip("Low values (1-3) create gas-like spreading. High values (15-30) create liquid-like cohesion.")]
    public float targetDensity = 1.5f; // Reduced from 3 to 1.5 for faster gap filling

    // Pressure multipliers: air is relatively compressible vs liquids, so keep these small
    // to avoid large pressure forces and instability.
    [Tooltip("Low values allow easy compression (gas-like). Recommended: 0.015-0.04")]
    public float pressureMultiplier = 0.04f; // Increased from 0.025 to 0.04 for stronger pressure response
    
    [Tooltip("Minimal particle repulsion for gas. Recommended: 0.005-0.015")]
    public float nearPressureMultiplier = 0.008f; // Reduced from 0.01 for less repulsion

    // No gravity by default for horizontal airflow simulation
    public float gravity = 0f;

    [Range(0f, 1f)]
    [Tooltip("High values (0.95-0.99) prevent bouncing and aid settling")]
    public float collisionDampening = 0.98f; // Increased from 0.97

    // Moderate viscosity for air - provides internal friction for stability
    [Tooltip("Low viscosity for air; increase if simulation appears too noisy. Recommended: 0.003-0.006")]
    public float viscosityStrength = 0.003f; // Reduced from 0.004 for less drag

    // Simulation bounds (world units)
    public Vector2 boundsSize = new(16f, 8f);

    [Header("User Input")]
    [Tooltip("Lower values create gentler disturbances. Recommended: 30-60 for air")]
    public float interactionInputStrength = 45f;
    public float interactionInputRadius = 2f;
    
    [Tooltip("Maximum speed the obstacle can move (units/second). Higher input speeds are clamped to this value.")]
    public float maxObstacleSpeed = 10f;

    [Header("Particle Behavior")]
    [Tooltip("Maximum velocity a particle can have (units/second)")]
    public float maxParticleSpeed = 15f;

    [Header("Visualization")]
    [Tooltip("Speed at which particles show the 'fast' color (should be lower than max speed)")]
    [Range(0.1f, 10f)]
    public float colorGradientMaxSpeed = 2f;
    
    [Tooltip("Color for slow-moving particles (speed = 0)")]
    public Color slowParticleColor = new Color(0.5f, 0.7f, 1f, 1f); // Light blue (calm air)
    
    [Tooltip("Color for fast-moving particles (at color gradient max speed)")]
    public Color fastParticleColor = new Color(1f, 0.3f, 0f, 1f); // Orange-red (turbulent air)

    [Header("Simulation Advanced")]
    [Tooltip("Prediction time factor for position prediction (1/120 = ~0.0083)")]
    [Range(0.001f, 0.1f)]
    public float predictionFactor = 0.00833f; // 1f / 120f
}