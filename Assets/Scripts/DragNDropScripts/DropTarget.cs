using UnityEngine;

[DisallowMultipleComponent]
public class DropTarget : MonoBehaviour, IDropTarget
{
    [Tooltip("If set, the draggable will snap to this transform instead of the target's transform.")]
    public Transform snapPoint;

    [Header("Capacity")]
    [SerializeField] private bool allowMultiple = false;

    [Header("State (single-capacity only)")]
    [SerializeField] private bool isOccupied = false;
    [SerializeField] private DragAndDrop occupiedByObject = null;

    [Header("Optional behavior")]
    [SerializeField] private bool isTrashbin = false;
    [SerializeField] private bool isLocker = false;

    public bool IsTrashbin() => isTrashbin;

    public bool IsLocker() => isLocker;

    public bool IsOccupied() => isOccupied;

    public DragAndDrop GetObjectOccupied() => occupiedByObject;

    public Vector3 GetSnapWorldPosition()
    {
        return snapPoint != null ? snapPoint.position : transform.position;
    }

    public bool CanAccept(DragAndDrop dragger)
    {
        return isTrashbin || allowMultiple || !isOccupied || occupiedByObject == dragger;
    }

    public void ApplyDrop(DragAndDrop dragger)
    {
        if(isLocker && dragger != null && dragger.gameObject != null)
        {
            EventManager.OnItemPutInLocker?.Invoke(dragger.gameObject);
            EventManager.OnItemRemovedFromPocket?.Invoke(dragger);
        }

        if (isTrashbin && dragger != null && dragger.gameObject != null)
        {
            EventManager.OnItemTrashed?.Invoke(dragger.gameObject);
            EventManager.OnItemRemovedFromPocket?.Invoke(dragger);
            
            Destroy(dragger.gameObject);
            return;
        }

        if (!allowMultiple)
        {
            isOccupied = true;
            occupiedByObject = dragger;
        }
    }

    public void ClearDrop(DragAndDrop dragger)
    {
        if (isOccupied && occupiedByObject == dragger)
        {
            isOccupied = false;
            occupiedByObject = null;
        }
    }

    public Vector2 GetDropSize()
    {
        return GetComponent<RectTransform>()?.sizeDelta ?? Vector2.zero;
    }
}