using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NPCBehavior : MonoBehaviour
{
    public enum NPCState
    {
        Idle,
        Walking,
        Working
    }

    // Movement neighborhood (used for detection and gizmos)
    private readonly Vector3Int[] directions =
    {
        new(-1, 0, 0),  // left
        new(1, 0, 0),   // right
        new(0, 1, 0),   // up
        new(0, -1, 0),  // down
    };

    [Header("Config")]
    [SerializeField] private NPCState currentState = NPCState.Idle;
    [SerializeField] private Grid grid;
    [SerializeField] private List<Transform> workStationPositions;
    [SerializeField] private float walkingSpeed;
    [SerializeField] private int npcPower;

    [Header("Tiles")]
    [Tooltip("Obstacle tilemap used to prevent stepping into blocked cells.")]
    public Tilemap obstacles;

    // Path following
    private readonly List<Vector3> path = new();
    private Coroutine movementCoroutine;
    private int currentPathIndex;

    private Vector3Int currentDestinationCell;
    private bool isAvoiding;

    private void OnEnable()
    {
        EventManager.OnPathCalculatedFor.AddListener(OnPathCalculatedForOwner);
        DynamicObstacles.AddOrUpdateObstacle(transform, grid.WorldToCell(transform.position));
    }

    private void OnDisable()
    {
        EventManager.OnPathCalculatedFor.RemoveListener(OnPathCalculatedForOwner);
        DynamicObstacles.RemoveObstacle(transform);
    }

    private void Start()
    {
        RequestPath();
        FindObstacleTilemaps(grid != null ? grid.transform : null);
    }

    private void Update()
    {
        var currentCell = grid.WorldToCell(transform.position);
        DynamicObstacles.AddOrUpdateObstacle(transform, currentCell);
        //Detect();
    }

    private void FindObstacleTilemaps(Transform gridTransform)
    {
        if (gridTransform == null) return;

        const string targetLayer = "ObstacleTiles";
        int targetLayerIndex = LayerMask.NameToLayer(targetLayer);

        foreach (Transform child in gridTransform)
        {
            var map = child.GetComponent<Tilemap>();
            if (map != null && map.gameObject.layer == targetLayerIndex)
            {
                obstacles = map;
                break;
            }
        }
    }

    private void RequestPath()
    {
        if (/*currentState == NPCState.Idle && */workStationPositions != null && workStationPositions.Count > 0)
        {
            int randomIndex = Random.Range(0, workStationPositions.Count);
            Vector3Int pos = Vector3Int.FloorToInt(workStationPositions[randomIndex].position);
            var v = grid.WorldToCell((Vector3)pos);

            currentDestinationCell = v;
            EventManager.OnPathRequested?.Invoke(transform, v);
            currentState = NPCState.Walking;
        }
    }

    private void OnPathCalculatedForOwner(Transform owner, List<Vector3> newPath)
    {
        if (owner != transform) return;
        CalculateWalkablePath(newPath);
    }

    private void CalculateWalkablePath(List<Vector3> newPath)
    {
        if (newPath == null || newPath.Count == 0) return;

        float minDist = float.MaxValue;
        int closestIndex = 0;
        for (int i = 0; i < newPath.Count; i++)
        {
            float dist = Vector3.Distance(transform.position, newPath[i]);
            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
        }

        path.Clear();
        path.AddRange(newPath);
        currentPathIndex = closestIndex;

        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        movementCoroutine = StartCoroutine(PlayerPositioning());
    }

    private void OnDrawGizmos()
    {
        if (path == null || path.Count < 2 || grid == null || currentDestinationCell == null) return;

        Gizmos.color = Color.yellow;
        //foreach (var pos in DynamicObstacles.GetAllObstacles().Values)
        //{
        //    for (int i = 0; i < directions.Length; i++)
        //    {
        //        var r = pos + directions[i];
        //        Vector3 worldPos = grid.GetCellCenterWorld(r);
        //        Gizmos.DrawCube(worldPos, Vector3.one * 0.5f);
        //    }
        //}

        Gizmos.color = Color.red;
        for (int i = 0; i < path.Count - 1; i++)
        {
            Gizmos.DrawSphere(path[i], 0.1f);
            Gizmos.DrawLine(path[i], path[i + 1]);
        }
    }

    private IEnumerator PlayerPositioning()
    {
        while (currentPathIndex < path.Count)
        {
            Vector3 target = path[currentPathIndex];

            while (isAvoiding)
                yield return null;

            while (Vector3.Distance(transform.position, target) > 0.1f)
            {
                if (isAvoiding)
                {
                    yield return null;
                    continue;
                }

                transform.position = Vector3.MoveTowards(transform.position, target, walkingSpeed * Time.deltaTime);
                yield return null;
            }

            currentPathIndex++;
        }

        currentState = NPCState.Working;
    }
}
