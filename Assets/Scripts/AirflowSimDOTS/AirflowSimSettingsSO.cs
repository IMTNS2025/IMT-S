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
    public Vector2 boundsSize = new (16f, 8f);
    
    [Header("Visualization")]
    public Color particleCol;
    public Color negativePressureCol;
    public Color positivePressureCol;
    public Color zeroPressureCol;

    /// <summary>
    /// Draws debug lines for the rectangle defined by <see cref="boundsSize"/>.
    /// The rectangle center is at the provided <paramref name="center"/> (default 0,0).
    /// Uses Debug.DrawLine; call from a MonoBehaviour (e.g. in Update or OnDrawGizmos)
    /// to visualize in the Scene/Game view.
    /// </summary>
    /// <param name="color">Line color.</param>
    /// <param name="duration">How long each line should be visible (0 draws for one frame).</param>
    /// <param name="drawCross">Also draw crosshairs through the center and optional diagonals.</param>
    /// <param name="drawDiagonals">Draw diagonals from corners if true.</param>
    /// <param name="center">Center of the bounds in world space (z set to 0 automatically).</param>
    public void DrawBounds(Color color, float duration = 0f, bool drawCross = true, bool drawDiagonals = false, Vector2? center = null)
    {
        Vector2 c2 = center ?? Vector2.zero;
        Vector3 c = new Vector3(c2.x, c2.y, 0f);

        float halfW = boundsSize.x * 0.5f;
        float halfH = boundsSize.y * 0.5f;

        Vector3 bl = new Vector3(c.x - halfW, c.y - halfH, 0f); // bottom-left
        Vector3 br = new Vector3(c.x + halfW, c.y - halfH, 0f); // bottom-right
        Vector3 tl = new Vector3(c.x - halfW, c.y + halfH, 0f); // top-left
        Vector3 tr = new Vector3(c.x + halfW, c.y + halfH, 0f); // top-right

        Debug.DrawLine(bl, br, color, duration);
        Debug.DrawLine(br, tr, color, duration);
        Debug.DrawLine(tr, tl, color, duration);
        Debug.DrawLine(tl, bl, color, duration);

        if (drawDiagonals)
        {
            Debug.DrawLine(bl, tr, color, duration);
            Debug.DrawLine(br, tl, color, duration);
        }

        if (drawCross)
        {
            Debug.DrawLine(new Vector3(c.x - halfW, c.y, 0f), new Vector3(c.x + halfW, c.y, 0f), color, duration);
            Debug.DrawLine(new Vector3(c.x, c.y - halfH, 0f), new Vector3(c.x, c.y + halfH, 0f), color, duration);
        }
    }
}