using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class EventManager
{
    public static UnityEvent<List<Vector3>> OnPathCalculated = new();
    public static UnityEvent<Vector3Int> OnEndTargetPathChanged = new();

    public static UnityEvent<Transform, Vector3Int> OnPathRequested = new();
    public static UnityEvent<Transform, List<Vector3>> OnPathCalculatedFor = new();

    public static UnityEvent<DragAndDrop> OnItemReturnedToPocket = new();
    public static UnityEvent<DragAndDrop> OnItemRemovedFromPocket = new();

    public static UnityEvent<DragAndDrop> OnItemDragStart = new();
    public static UnityEvent<DragAndDrop> OnItemDragEnd = new();
    public static UnityEvent<DragAndDrop> OnItemBeforeDragEnd = new();

    public static UnityEvent OnDragSuccessed = new();
    public static UnityEvent OnDragFailed = new();

    public static UnityEvent<Vector2> OnMovementDragStarted = new();

    public static UnityEvent OnSlowModeChanged = new();
}
