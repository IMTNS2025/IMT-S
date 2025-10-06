using UnityEngine;

public interface IDropTarget
{
    bool IsTrashbin();

    bool CanAccept(DragAndDrop dragger);

    Vector3 GetSnapWorldPosition();

    void ApplyDrop(DragAndDrop dragger);

    void ClearDrop(DragAndDrop dragger);
}
