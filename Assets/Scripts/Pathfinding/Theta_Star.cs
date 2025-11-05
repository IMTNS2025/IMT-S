using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Theta_Star : MonoBehaviour
{
    public static Theta_Star Instance { get; private set; }

    private readonly Vector3Int[] directions = {
        new(-1, 0, 0),  //left
        new(1, 0, 0),   //right
        new(0, 1, 0),   //up
        new(0, -1, 0),  //down
        new(-1, 1, 0),  //left up
        new(1, 1, 0),   //right up
        new(-1, -1, 0), //left down
        new(1, -1, 0)   //right down
    };

    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap[] walkableTiles;
    [SerializeField] private Tilemap[] obstaclesTiles;

    // Legacy single-agent fields (kept for backward compatibility / optional debug)
    [SerializeField] private Transform playerPosition;
    [SerializeField] private Transform endPosition;
    [SerializeField] private GameObject pathGameObject;

    private List<Vector3> closedList = new();

    [Space]

    [SerializeField] private bool recalculateOnEndTargetChanged = true;
    [SerializeField] private float recalculationInterval = 0.01f;

    private List<GameObject> lastSpawnPathObjects = new();
    private Vector3 _lastStartCell;
    private Vector3 _lastEndCell;

    private void Start()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);

        if (grid == null)
        {
            Debug.LogWarning("Theta_Star: Missing grid reference.");
            return;
        }

        if (playerPosition != null && endPosition != null)
        {
            _lastStartCell = grid.WorldToCell(playerPosition.position);
            _lastEndCell = grid.WorldToCell(endPosition.position);
            GeneratePath();
        }
    }

    private void OnEnable()
    {
        EventManager.OnEndTargetPathChanged.AddListener(SetEndGoal);

        EventManager.OnPathRequested.AddListener(HandlePathRequest);
    }

    private void OnDisable()
    {
        EventManager.OnEndTargetPathChanged.RemoveListener(SetEndGoal);
        EventManager.OnPathRequested.RemoveListener(HandlePathRequest);
    }
    
    public bool IsWalkable(Vector3Int cellPosition)
    {
        foreach (var obstacleTilemap in obstaclesTiles)
        {
            if (obstacleTilemap == null) continue;
            if (obstacleTilemap.HasTile(cellPosition))
            {
                return false;
            }
        }
        return true;
    }

    private void DynamicPathRecalculation()
    {
        if (!recalculateOnEndTargetChanged || grid == null || playerPosition == null || endPosition == null)
            return;

        if (Time.time < recalculationInterval) return;

        var currentStartCell = grid.WorldToCell(playerPosition.position);
        var currentEndCell = grid.WorldToCell(endPosition.position);

        if (!IsWalkable(currentStartCell) || !IsWalkable(currentEndCell)) return;

        if (currentStartCell != _lastStartCell || currentEndCell != _lastEndCell)
        {
            _lastStartCell = currentStartCell;
            _lastEndCell = currentEndCell;

            foreach (var go in lastSpawnPathObjects)
                Destroy(go);

            lastSpawnPathObjects.Clear();
            closedList.Clear();
            GeneratePath();
        }
    }

    private void GeneratePath()
    {
        if (grid == null || playerPosition == null || endPosition == null)
        {
            Debug.LogWarning("Theta_Star: Missing grid/start/end references.");
            return;
        }

        var path = FindPathWorld();
        if (path == null)
        {
            Debug.Log("Theta_Star: No path found.");
            return;
        }

        EventManager.OnPathCalculated?.Invoke(path);

        if (pathGameObject != null)
        {
            foreach (var p in path)
            {
                var go = Instantiate(pathGameObject, p, Quaternion.identity);
                lastSpawnPathObjects.Add(go);
            }
        }
    }

    public List<Vector3> FindPathWorld()
    {
        var cells = FindPathCells();
        if (cells == null) return null;

        var result = new List<Vector3>();
        for (int i = 0; i < cells.Count; i++)
            result.Add(grid.GetCellCenterWorld(cells[i]));
        return result;
    }

    public List<Vector3Int> FindPathCells()
    {
        var startCell = grid.WorldToCell(playerPosition.position);
        var goalCell = grid.WorldToCell(endPosition.position);
        return FindPathCells(startCell, goalCell);
    }

    public List<Vector3Int> FindPathCells(Vector3Int startCell, Vector3Int goalCell)
    {
        List<Vector3Int> openList = new();
        openList.Clear();
        openList.Add(startCell);

        var closedSet = new HashSet<Vector3Int>();

        Dictionary<Vector3Int, Vector3Int> cameFrom = new();
        cameFrom[startCell] = startCell;

        Dictionary<Vector3Int, float> gScore = new() { [startCell] = 0 };
        Dictionary<Vector3Int, float> fScore = new() { [startCell] = EuclideanCostEstimate(startCell, goalCell) };

        while (openList.Count > 0)
        {
            Vector3Int current = default;

            float smallestF = float.MaxValue;

            foreach (var n in openList)
            {
                float f = fScore.TryGetValue(n, out float fValue) ? fValue : smallestF;

                if (f < smallestF)
                {
                    smallestF = f;
                    current = n;
                }
            }

            if (current == goalCell)
                return ReconstructPath(cameFrom, current);

            openList.Remove(current);
            closedSet.Add(current);

            var parentOfCurrent = cameFrom[current];

            for (int i = 0; i < directions.Length; i++)
            {
                Vector3Int neighborCell = current + directions[i];

                if (!IsWalkable(neighborCell) || closedSet.Contains(neighborCell)) continue;

                Vector3Int potentialParent;
                float tempGScore;

                if (LineOfSight(grid.CellToWorld(parentOfCurrent), grid.CellToWorld(neighborCell)))
                {
                    potentialParent = parentOfCurrent;
                    tempGScore = gScore[parentOfCurrent] + EuclideanCostEstimate(parentOfCurrent, neighborCell);
                }
                else
                {
                    potentialParent = current;
                    tempGScore = gScore[current] + EuclideanCostEstimate(current, neighborCell);
                }

                float neighborGScore =
                    gScore.TryGetValue(neighborCell, out float gValue) ? gValue : float.MaxValue;

                if (tempGScore < neighborGScore)
                {
                    cameFrom[neighborCell] = potentialParent;
                    gScore[neighborCell] = tempGScore;
                    fScore[neighborCell] = tempGScore + EuclideanCostEstimate(neighborCell, goalCell);
                    if (!openList.Contains(neighborCell))
                        openList.Add(neighborCell);
                }
            }
        }

        return null;
    }

    private List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int currentCellPosition)
    {
        var path = new List<Vector3Int> { currentCellPosition };
        while (cameFrom.TryGetValue(currentCellPosition, out var parent))
        {
            if (parent == currentCellPosition) break;
            currentCellPosition = parent;
            path.Add(currentCellPosition);
        }

        path.Reverse();
        return path;
    }

    private float EuclideanCostEstimate(Vector3Int startCell, Vector3Int endCell)
    {
        return Mathf.Sqrt(Mathf.Pow((startCell.x - endCell.x), 2) + Mathf.Pow((startCell.y - endCell.y), 2));
    }
   
    private void OnDrawGizmos()
    {
        if (grid == null || obstaclesTiles == null) return;

        Gizmos.color = Color.red;

        foreach (var obstacleTilemap in obstaclesTiles)
        {
            if (obstacleTilemap == null) continue;

            BoundsInt bounds = obstacleTilemap.cellBounds;
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                if (obstacleTilemap.HasTile(pos))
                {
                    Vector3 worldPos = grid.GetCellCenterWorld(pos);
                    Gizmos.DrawCube(worldPos, Vector3.one * 0.5f);
                }
            }
        }
    }

    public void SetEndGoal(Vector3Int cellPos)
    {
        if (endPosition == null || grid == null) return;
        endPosition.position = grid.GetCellCenterWorld(cellPos);
        DynamicPathRecalculation();
    }

    //Bresenham's line algorithm
    bool LineOfSight(Vector3 start, Vector3 end)
    {
        Vector3Int startPos = grid.WorldToCell(start);
        Vector3Int endPos = grid.WorldToCell(end);

        int x0 = startPos.x;
        int y0 = startPos.y;

        int x1 = endPos.x;
        int y1 = endPos.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        int sx = (x0 < x1) ? 1 : -1;
        int sy = (y0 < y1) ? 1 : -1;

        int err = dx - dy;

        while (true)
        {
            if (!IsWalkable(new Vector3Int(x0, y0, 0))) return false;
            if (x0 == x1 && y0 == y1) return true;

            int e2 = err * 2;

            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;

                if (!IsWalkable(new Vector3Int(x0, y0, 0))) return false;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;

                if (!IsWalkable(new Vector3Int(x0, y0, 0))) return false;
            }
        }
    }

    private void HandlePathRequest(Transform requester, Vector3Int endCell)
    {
        if (grid == null || requester == null) return;

        var startCell = grid.WorldToCell(requester.position);

        if (!IsWalkable(startCell) || !IsWalkable(endCell))
        {
            EventManager.OnPathCalculatedFor?.Invoke(requester, null);
            return;
        }

        var cells = FindPathCells(startCell, endCell);
        if (cells == null)
        {
            EventManager.OnPathCalculatedFor?.Invoke(requester, null);
            return;
        }

        var path = new List<Vector3>(cells.Count);
        foreach (var c in cells)
            path.Add(grid.GetCellCenterWorld(c));

        EventManager.OnPathCalculatedFor?.Invoke(requester, path);

        if (pathGameObject != null)
        {
            foreach (var p in path)
            {
                var go = Instantiate(pathGameObject, p, Quaternion.identity);
                lastSpawnPathObjects.Add(go);
            }
        }
    }
}
