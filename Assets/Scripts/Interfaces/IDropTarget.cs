using UnityEngine;

public interface IDropTarget
{
    bool IsTrashbin();

    bool IsLocker();

    bool CanAccept(DragAndDrop dragger);

    Vector3 GetSnapWorldPosition();

    void ApplyDrop(DragAndDrop dragger);

    void ClearDrop(DragAndDrop dragger);

    Vector2 GetDropSize();
}
