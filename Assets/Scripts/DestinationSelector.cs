using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class DestinationSelector : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [SerializeField] private GameObject target;

    private Camera cam;

    private Tilemap[] occupiedTiles;

    private InputAction pointAction;

    private void Awake()
    {
        cam = Camera.main;

        if (occupiedTiles == null || occupiedTiles.Length == 0)
        {
            occupiedTiles = grid.GetComponentsInChildren<Tilemap>(includeInactive: false);
            
            for (int i = 0; i < occupiedTiles.Length; i++)
            {
                occupiedTiles[i].transform.position = new Vector3(0, 0, 0);
            }
        }
    }

    private void OnEnable()
    {
        pointAction = new InputAction("Point", InputActionType.PassThrough, "<Pointer>/position");
        pointAction.Enable();
    }

    private void OnDisable()
    {
        if (pointAction != null)
        {
            pointAction.Disable();
            pointAction.Dispose();
            pointAction = null;
        }
    }

    private void Update()
    {
        if (cam == null || grid == null || target == null || pointAction == null)
            return;

        if (Touchscreen.current != null)
        {
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                ProcessPointerPosition();
            }
        }
        else if(Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ProcessPointerPosition();
        }
    }

    private void ProcessPointerPosition()
    {
        Vector2 screenPos = pointAction.ReadValue<Vector2>();
        Ray ray = cam.ScreenPointToRay(screenPos);

        Plane plane = new Plane(grid.transform.forward, grid.transform.position);

        if (!plane.Raycast(ray, out var enter)) return;

        Vector3 worldPoint = ray.GetPoint(enter);
        Vector3Int cellToCheck = grid.WorldToCell(worldPoint);

        if (!IsCellOccupied(cellToCheck)) return;
       
        target.transform.position = grid.GetCellCenterWorld(cellToCheck);

        EventManager.OnEndTargetPathChanged?.Invoke(cellToCheck);
    }

    private bool IsCellOccupied(Vector3Int cell)
    {
        foreach (var tilemap in occupiedTiles)
        {
            if (tilemap == null) continue;
            if (tilemap.HasTile(cell))
            {
                return true;
            }
        }
        return false;
    }
}
