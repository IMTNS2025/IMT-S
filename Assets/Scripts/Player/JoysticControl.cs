using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class JoysticControl : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [SerializeField] private InputActionAsset inputActionAsset;

    [SerializeField] private Image outerCircle;
    [SerializeField] private Image innerCircle;
    [SerializeField] private Button slowButton;

    private Vector3 outerCircleCenter;
    private Vector3 innerCircleCenter;

    private void Start()
    {
        if (outerCircle == null)
            outerCircle = GetComponent<Image>();
        if (innerCircle == null && transform.childCount > 0)
            innerCircle = transform.GetChild(0).GetComponent<Image>();

        GetCenters();

        innerCircle.rectTransform.position = outerCircleCenter;
    }

    #region Joystic Control
    public void OnEndDrag(PointerEventData eventData)
    {
        innerCircle.rectTransform.position = outerCircleCenter;
        EventManager.OnMovementDragStarted?.Invoke(Vector3.zero);
    }

    public void OnDrag(PointerEventData eventData)
    {
        innerCircle.transform.position = eventData.position;

        Vector3 dir = (innerCircleCenter - outerCircleCenter).normalized;

        EventManager.OnMovementDragStarted?.Invoke(dir);
    }

    private void GetCenters()
    {
        RectTransform rtOuter = outerCircle.rectTransform;
        outerCircleCenter = rtOuter.TransformPoint(rtOuter.rect.center);

        RectTransform rtInner = innerCircle.rectTransform;
        innerCircleCenter = rtInner.TransformPoint(rtInner.rect.center);
    }

    private static float GetRadiusWorld(RectTransform rt)
    {
        float w = rt.rect.width * rt.lossyScale.x;
        float h = rt.rect.height * rt.lossyScale.y;
        return 0.5f * Mathf.Min(w, h);
    }
    private void PositionWithinRadius()
    {
        if (outerCircle == null || innerCircle == null) return;

        RectTransform rtOuter = outerCircle.rectTransform;
        RectTransform rtInner = innerCircle.rectTransform;

        Vector3 outerCenter = rtOuter.TransformPoint(rtOuter.rect.center);
        Vector3 innerCenter = rtInner.TransformPoint(rtInner.rect.center);

        float radius = GetRadius();

        Vector3 delta = innerCenter - outerCenter;
        float dist = delta.magnitude;

        if (dist > radius && dist > 0f)
        {
            Vector3 clampedCenter = outerCenter + delta.normalized * radius;
            Vector3 move = clampedCenter - innerCenter;

            rtInner.position += move;
            innerCenter = clampedCenter;
        }

        outerCircleCenter = outerCenter;
        innerCircleCenter = innerCenter;
    }

    private float GetRadius()
    {
        if (outerCircle == null) return 0f;
        return GetRadiusWorld(outerCircle.rectTransform);
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (outerCircle == null)
            outerCircle = GetComponent<Image>();
        if (innerCircle == null && transform.childCount > 0)
            innerCircle = transform.GetChild(0).GetComponent<Image>();
        if (outerCircle == null || innerCircle == null)
            return;

        RectTransform rtOuter = outerCircle.rectTransform;
        RectTransform rtInner = innerCircle.rectTransform;

        Vector3 worldCenterOuter = rtOuter.TransformPoint(rtOuter.rect.center);
        Vector3 worldCenterInner = rtInner.TransformPoint(rtInner.rect.center);

        float outerRadius = GetRadius();
        float innerRadius = 0.5f * Mathf.Min(rtInner.rect.width * rtInner.lossyScale.x,
                                             rtInner.rect.height * rtInner.lossyScale.y);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(worldCenterOuter, outerRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(worldCenterInner, innerRadius);

        Vector3 dir = worldCenterInner - worldCenterOuter;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(worldCenterOuter, worldCenterOuter + dir);

        Vector3 radiusVec = dir.normalized * outerRadius;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(worldCenterOuter, worldCenterOuter + radiusVec);
    }

    private void OnEnable()
    {
        //var joystickActionMap = inputActionAsset.FindActionMap("TouchScreen");
        //joystickActionMap.FindAction("Move").performed += OnMovePerformed;
        //joystickActionMap.FindAction("Move").canceled += OnMoveCanceled;
        //joystickActionMap.Enable();
    }

    private void Update()
    {
        PositionWithinRadius();
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        // Example: use OuterRadiusPixels to clamp inner knob movement in UI space.
        // Vector2 delta = (Vector2)pointerPos - (Vector2)outerCircleCenter;
        // Vector2 clamped = Vector2.ClampMagnitude(delta, OuterRadiusPixels);
    }
}