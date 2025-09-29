using UnityEngine;

[DisallowMultipleComponent]
public class DropTarget : MonoBehaviour
{
    [Tooltip("If set, the draggable will snap to this transform instead of the target's transform.")]
    public Transform snapPoint;

    public Vector3 GetSnapWorldPosition()
    {
        Transform t = snapPoint != null ? snapPoint : transform.GetChild(0);
        Vector3 pos = t.position;
        return pos;
    }
}