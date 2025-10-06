using UnityEngine;

[DisallowMultipleComponent]
public class DropTarget : MonoBehaviour
{
    [Tooltip("If set, the draggable will snap to this transform instead of the target's transform.")]
    public Transform snapPoint;

    [SerializeField] private bool isOccupied = false;
    [SerializeField] private bool isTrashbin = false;

    [SerializeField] private DragAndDrop occupiedByObject = null;

    public bool IsOccupied() => isOccupied;
    
    public bool IsTrashbin() => isTrashbin;

    public DragAndDrop GetObjectOccupied() => occupiedByObject;


    public Vector3 GetSnapWorldPosition()
    {
        Transform t = snapPoint != null ? snapPoint : transform;
        Vector3 pos = t.position;
        return pos;
    }

    public void SetOccupied(bool occupied) => isOccupied = occupied;

    public void SetOccupiedByObject(DragAndDrop obj) => occupiedByObject = obj;

}