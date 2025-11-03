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

    private readonly Vector3Int[] directions = {
        new(-1, 0, 0),  //left
        new(1, 0, 0),   //right
        new(0, 1, 0),   //up
        new(0, -1, 0),  //down
    };

    public int PowerIndex => npcPower;
    public Tilemap obstacles;
    [SerializeField] private NPCState currentState = NPCState.Idle;

    [SerializeField] private Grid grid;
    [SerializeField] private List<Transform> workStationPositions;
    [SerializeField] private float walkingSpeed;
    [SerializeField] private float repathCooldown = 0.25f;

    [SerializeField] private int npcPower;

    private List<Vector3> path = new();

    private Coroutine movementCoroutine;
    private int currentPathIndex = 0;

    public bool isYielding;

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
        FindObstacleTilemaps(grid.transform);
    }

    void FindObstacleTilemaps(Transform grid)
    {
        if (grid == null) return;

        string targetLayer = "ObstacleTiles";
        int targetLayerIndex = LayerMask.NameToLayer(targetLayer);

        foreach (Transform child in grid)
        {
            Tilemap map = child.GetComponent<Tilemap>();
            if (map != null && map.gameObject.layer == targetLayerIndex)
            {
                obstacles = map;
                break;
            }
        }
    }

    private void Update()
    {
        var currentCell = grid.WorldToCell(transform.position);
        bool isCellChanged = currentCell != DynamicObstacles.GetAllObstacles()[this.transform];
        DynamicObstacles.AddOrUpdateObstacle(this.transform, grid.WorldToCell(transform.position));

        Detect();
    }

    void Detect()
    {
        var pos = grid.WorldToCell(transform.position);
        for (int i = 0; i < directions.Length; i++)
        {
            var r = pos + directions[i];
            if (!IsCellOccupiedByOther(r)) continue;
            Debug.Log($"detected npc at {r}");
            var n = DynamicObstacles.GetOwnerAtPosition(r);
            if (n == null) continue;

            if (n.TryGetComponent<NPCBehavior>(out NPCBehavior other))
            {
                Debug.Log($"npc {other}");

                if (this.PowerIndex <= other.PowerIndex)
                {
                    isYielding = true;
                    break;
                }
            }
        }
    }

    private bool IsCellFree(Vector3Int neighbourCell)
    {
        if (obstacles == null) return false;
        if (obstacles.HasTile(neighbourCell) || DynamicObstacles.IsPositionOccupied(neighbourCell)) return true;
        return false;
    }

    private bool IsCellOccupiedByOther(Vector3Int neighbourCell)
    {
        foreach (var kv in DynamicObstacles.GetAllObstacles())
        {
            if (kv.Key == this.transform) continue;
            if (kv.Value == neighbourCell) return true;
        }
        return false;
    }

    private void RequestPath()
    {
        if (currentState == NPCState.Idle && workStationPositions != null && workStationPositions.Count > 0)
        {
            int randomIndex = Random.Range(0, workStationPositions.Count);
            Vector3Int pos = Vector3Int.FloorToInt(workStationPositions[randomIndex].position);
            var v = grid.WorldToCell((Vector3)pos);

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
                //Request path to destination
                break;
            case NPCState.Working:
                //simulate working
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

        path = newPath;
        currentPathIndex = closestIndex;

        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(PlayerPositioning());
    }

    private void OnDrawGizmos()
    {
        if (path == null || path.Count < 2) return;
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

            // If yielding, wait here until Detect() clears it
            while (isYielding)
                yield return null;

            while (Vector3.Distance(transform.position, target) > 0.1f)
            {
                if (isYielding)
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
