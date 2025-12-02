using UnityEngine;

[ExecuteAlways]
public class DrawDebugLines : MonoBehaviour
{
    [SerializeField] private AirflowSimSettingsSO airflowSimSettingsSO;

    private void OnDrawGizmos()
    {
        if (airflowSimSettingsSO == null) return;

        // Use the SO's visualization color (fallback to white) and draw the rect centered at 0,0.
        Color col = airflowSimSettingsSO.zeroPressureCol;
        airflowSimSettingsSO.DrawBounds(col, 0f, drawCross: true, drawDiagonals: false, center: Vector2.zero);
    }
}