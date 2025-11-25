using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragAndDrop : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IDragHandler
{
    [Header("Draggable")]
    [Tooltip("What Transform to move. If not set, uses this GameObject's Transform.")]
    private Transform objectToDrag;

    [SerializeField] private float snapDist = 250f;
    [SerializeField] private bool makeInvisible = true;

    [Header("Drop Targets")]
    [SerializeField] private bool autoFindTargets = true;
    [SerializeField] private bool resizeWithTarget = false;
    private IDropTarget[] targets;
    private Vector3 originalPosition;
    private bool dragging;
    private Vector3 lastPosition;

    // Scaling / state
    private Vector3 initialLocalScale;
    private IDropTarget currentTarget;

    // UI helpers
    private RectTransform rect;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Vector2 pointerToAnchorOffset;

    private void Start()
    {
        objectToDrag = transform.childCount > 0 ? transform.GetChild(0) : transform;

        rect = objectToDrag as RectTransform;
        if (rect != null)
        {
            canvas = rect.GetComponentInParent<Canvas>();
            canvasRect = canvas ? (RectTransform)canvas.transform : null;
        }

        if (autoFindTargets)
            targets = FindObjectsByType<DropTarget>(FindObjectsSortMode.None);

        originalPosition = transform.position;

        // Cache initial local scale and apply container scale
        if (objectToDrag != null)
            initialLocalScale = objectToDrag.localScale;
        else
            initialLocalScale = transform.localScale;

        currentTarget = null;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (objectToDrag == null) return;
        dragging = true;
        EventManager.OnItemDragStart?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || objectToDrag == null) return;

        if (rect != null && canvas != null)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out var localPointerPosition))
            {
                lastPosition = objectToDrag.position;
                objectToDrag.position = canvas.transform.TransformPoint(localPointerPosition - pointerToAnchorOffset);
            }
        }
        else
        {
            var screenPoint = new Vector3(eventData.position.x, eventData.position.y, Camera.main.WorldToScreenPoint(objectToDrag.position).z);
            objectToDrag.position = Camera.main.ScreenToWorldPoint(screenPoint);
        }

#if UNITY_EDITOR
        var closest = GetClosestTarget(objectToDrag.position);
        if (closest != null)
        {
            var snapPos = closest.GetSnapWorldPosition();
            if ((snapPos - objectToDrag.position).sqrMagnitude < GetSnapRadiusSqr())
            {
                Debug.DrawLine(objectToDrag.position, snapPos, Color.red);
            }
        }
#endif

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;
        EventManager.OnItemBeforeDragEnd?.Invoke(this);

        IDropTarget closest = GetClosestTarget(objectToDrag.position);
        if (closest == null)
        {
            objectToDrag.position = originalPosition;
            currentTarget = null;
            return;
        }

        var snapPos = closest.GetSnapWorldPosition();
        bool withinSnap = (snapPos - objectToDrag.position).sqrMagnitude <= GetSnapRadiusSqr();

        if (withinSnap && closest.CanAccept(this) && !objectToDrag.GetComponent<DecontaminationToolInfo>())
        {
            objectToDrag.position = snapPos;
            closest.ApplyDrop(this);
            currentTarget = closest;

            if (resizeWithTarget == true)
            {
                if (objectToDrag.TryGetComponent<RectTransform>(out RectTransform v))
                {
                    v.sizeDelta = closest.GetDropSize();

                    if (objectToDrag.TryGetComponent<RawImage>(out RawImage i))
                    {
                        i.color = new Color(i.color.r, i.color.g, i.color.b, 1f);
                    }
                }
            }
            EventManager.OnDragSuccessed?.Invoke(); // If it breaks, move this three lines up lol (stan approved this)

            EventManager.OnItemDragEnd?.Invoke(this);
        }
        else
        {
            objectToDrag.position = originalPosition;
            closest.ClearDrop(this);
            currentTarget = null;

            if (objectToDrag.TryGetComponent<RawImage>(out RawImage i))
            {
                i.color = new Color(i.color.r, i.color.g, i.color.b, makeInvisible ? 0f : 1f);
            }
            EventManager.OnDragFailed?.Invoke();

            EventManager.OnItemDragEnd?.Invoke(this);
        }
    }

    private float GetSnapRadiusSqr()
    {
        if (rect != null && canvas != null && canvas.renderMode != RenderMode.WorldSpace)
        {
            float scale = Mathf.Max(1f, canvas.scaleFactor);
            float r = snapDist / scale;
            return r * r;
        }
        return snapDist * snapDist;
    }

    private IDropTarget GetClosestTarget(Vector3 position)
    {
        if (targets == null || targets.Length == 0) return null;

        IDropTarget closest = null;
        float closestDistanceSqr = Mathf.Infinity;

        foreach (var target in targets)
        {
            if (target == null) continue;
            var snapPos = target.GetSnapWorldPosition();
            float dSqr = (snapPos - position).sqrMagnitude;

            if (dSqr < closestDistanceSqr)
            {
                closestDistanceSqr = dSqr;
                closest = target;
            }
        }

        return closest;
    }

    public float getSnapDistance()
    {
        return snapDist;
    }

    public Vector3 getLastPosition()
    {
        return lastPosition;
    }
}