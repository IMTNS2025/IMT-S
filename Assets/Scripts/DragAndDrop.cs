using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDrop : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IDragHandler
{
    [Header("Draggable")]
    [Tooltip("What Transform to move. If not set, uses this GameObject's Transform.")]
    private Transform objectToDrag;

    [Header("Drop Targets")]
    [Tooltip("Auto find all DropTarget targets in the scene")]
    [SerializeField] private bool autoFindTargets = true;

    [Tooltip("Leave empty if autoFindTargets == true")]
    [SerializeField] private DropTarget[] targets;

    private Vector3 originalPosition;
    private bool dragging;

    // UI helpers
    private RectTransform rect;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Vector2 pointerToAnchorOffset;
    private float snapDist = 250f;

    private void Start()
    {
        if (objectToDrag == null)
        {
            var objToDrag = transform.GetChild(0);

            if (objToDrag == null)
                objectToDrag = transform;
            else
                objectToDrag = objToDrag;
        }

        rect = objectToDrag as RectTransform;
        if (rect != null)
        {
            canvas = rect.GetComponentInParent<Canvas>();
            canvasRect = canvas ? (RectTransform)canvas.transform : null;
        }

        AutoFindTargets();
    }

    private void AutoFindTargets()
    {
        if (autoFindTargets == true)
        {
            targets = FindObjectsByType<DropTarget>(FindObjectsSortMode.None);
        }
    }


    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || objectToDrag == null) return;

        if (rect != null && canvas != null)
        {
            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out localPointerPosition))
            {
                objectToDrag.position = canvas.transform.TransformPoint(localPointerPosition - pointerToAnchorOffset);
            }
        }
        else
        {
            Vector3 screenPoint = new Vector3(eventData.position.x, eventData.position.y, Camera.main.WorldToScreenPoint(objectToDrag.position).z);
            objectToDrag.position = Camera.main.ScreenToWorldPoint(screenPoint);
        }

        var closestTarget = GetClosestTarget(objectToDrag.position, targets);
        if (closestTarget != null)
        {
            if (closestTarget.GetSnapWorldPosition().magnitude < snapDist)
            {
               Debug.DrawLine(objectToDrag.position, closestTarget.GetSnapWorldPosition(), Color.green);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (objectToDrag == null) return;

        dragging = true;
        originalPosition = objectToDrag.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;

        var closestTarget = GetClosestTarget(objectToDrag.position, targets);
        if (closestTarget != null)
        {
            if (closestTarget.GetSnapWorldPosition().magnitude < snapDist)
            {
                objectToDrag.position = closestTarget.GetSnapWorldPosition();
            }
            else
                objectToDrag.position = originalPosition;
        }
        else
        {
            objectToDrag.position = originalPosition;
        }
    }

    private DropTarget GetClosestTarget(Vector3 position, DropTarget[] targets)
    {
        if (targets == null || targets.Length == 0) return null;

        DropTarget closest = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 p = objectToDrag.position;

        for (int i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            if (target == null) continue;

            var snapPos = target.GetSnapWorldPosition();
            float dSqr = (snapPos - p).sqrMagnitude;

            if (dSqr <= closestDistanceSqr)
            {
                closestDistanceSqr = dSqr;
                closest = target;
            }
        }

        return closest;
    }
}
