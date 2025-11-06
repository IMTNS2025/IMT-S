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

    public int PowerIndex => npcPower;

    // Path following
    private readonly List<Vector3> path = new();
    private Coroutine movementCoroutine;
    private int currentPathIndex;

    // Side-step avoidance
    private Coroutine avoidCoroutine;
    private Vector3Int currentDestinationCell;
    private bool isAvoiding;
    private bool alreadyMovedAside;

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
        Detect();
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

    private void Detect()
    {
        if (alreadyMovedAside) return;
        var pos = grid.WorldToCell(transform.position);

        for (int i = 0; i < directions.Length; i++)
        {
            var r = pos + directions[i];

            if (!Theta_Star.Instance.IsWalkable(r)) continue;

            var n = DynamicObstacles.GetOwnerAtPosition(r);
            if (n == null) continue;

            if (n.TryGetComponent<NPCBehavior>(out var other))
            {
                Debug.Log($"npc {other}");

                if (PowerIndex <= other.PowerIndex)
                {
                    if (!TryStartAvoiding(pos, r))
                        break;
                }
            }
        }
    }

    private bool TryStartAvoiding(Vector3Int myCell, Vector3Int otherCell)
    {
        var dir = otherCell - myCell;

        Vector3Int sideA, sideB;
        if (dir.x != 0)
        {
            sideA = new Vector3Int(0, 1, 0);
            sideB = new Vector3Int(0, -1, 0);
        }
        else
        {
            sideA = new Vector3Int(1, 0, 0);
            sideB = new Vector3Int(-1, 0, 0);
        }

        var neighbourA = myCell + sideA;
        var neighbourB = myCell + sideB;

        bool walkableA = IsCellWalkable(neighbourA);
        bool walkableB = IsCellWalkable(neighbourB);

        if (walkableA)
        {
            StartAvoiding(myCell, neighbourA, dir);
            return true;
        }

        if (walkableB)
        {
            StartAvoiding(myCell, neighbourB, dir);
            return true;
        }

        return false;
    }

    private void StartAvoiding(Vector3Int originalCell, Vector3Int sideCell, Vector3Int dir)
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        if (!alreadyMovedAside && avoidCoroutine == null && isAvoiding == false)
            avoidCoroutine = StartCoroutine(AvoidingCoroutine(originalCell, sideCell, dir));
    }

    private IEnumerator AvoidingCoroutine(Vector3Int originalCell, Vector3Int sideCell, Vector3Int dir)
    {
        isAvoiding = true;
        alreadyMovedAside = true;
        var sideWorldPos = grid.GetCellCenterWorld(sideCell);
        while (Vector3.Distance(transform.position, sideWorldPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, sideWorldPos, walkingSpeed * 1.5f * Time.deltaTime);
            yield return null;
        }


        float elapsed = 0f;
        while (!IsLaneClear(originalCell, dir))
        {
            elapsed += Time.deltaTime;
            if (elapsed >= 3f) break;
            yield return null;
        }

        isAvoiding = false;
        avoidCoroutine = null;
        alreadyMovedAside = false;
        RequestPath();
        // if (movementCoroutine == null)
        //   movementCoroutine = StartCoroutine(PlayerPositioning());
    }

    private bool IsLaneClear(Vector3Int originCell, Vector3Int passDir)
    {
        if (!IsCellWalkable(originCell)) return false;
        if (!IsCellWalkable(originCell + passDir)) return false;

        return true;
    }

    private bool IsCellWalkable(Vector3Int cell)
    {
        if (obstacles == null) return true;
        if (obstacles.HasTile(cell) || DynamicObstacles.IsPositionOccupied(cell))
            return false;
        return true;
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

    private void UpdateState(NPCState state)
    {
        currentState = state;

        switch (currentState)
        {
            case NPCState.Idle:
                RequestPath();
                break;
            case NPCState.Walking:
                // Request path to destination
                break;
            case NPCState.Working:
                // Simulate working
                break;
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
        foreach (var pos in DynamicObstacles.GetAllObstacles().Values)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                var r = pos + directions[i];
                Vector3 worldPos = grid.GetCellCenterWorld(r);
                Gizmos.DrawCube(worldPos, Vector3.one * 0.5f);
            }
        }

        Gizmos.color = Color.cyan;
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
