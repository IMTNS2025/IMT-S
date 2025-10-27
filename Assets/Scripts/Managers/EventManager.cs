using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class EventManager
{
    // Existing global events (kept for backward compatibility)
    public static UnityEvent<List<Vector3>> OnPathCalculated = new();
    public static UnityEvent<Vector3Int> OnEndTargetPathChanged = new();

    // New per-request/per-owner events
    public static UnityEvent<Transform, Vector3Int> OnPathRequested = new();
    public static UnityEvent<Transform, List<Vector3>> OnPathCalculatedFor = new();

    public static UnityEvent<DragAndDrop> OnItemReturnedToPocket = new();
    public static UnityEvent<DragAndDrop> OnItemRemovedFromPocket = new();

    public static UnityEvent<DragAndDrop> OnItemDragStart = new();
    public static UnityEvent<DragAndDrop> OnItemDragEnd = new();
}
