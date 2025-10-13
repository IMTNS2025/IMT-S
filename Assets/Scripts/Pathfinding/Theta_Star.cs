using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Theta_Star : MonoBehaviour
{
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
        if (grid == null || playerPosition == null || endPosition == null)
        {
            Debug.LogWarning("A_Star: Missing grid/start/end references.");
            return;
        }

        _lastStartCell = grid.WorldToCell(playerPosition.position);
        _lastEndCell = grid.WorldToCell(endPosition.position);

        GeneratePath();
    }

    private void OnEnable() => EventManager.OnEndTargetPathChanged.AddListener(SetEndGoal);
    private void OnDisable() => EventManager.OnEndTargetPathChanged.RemoveListener(SetEndGoal);

    private void Update()
    {
        //DynamicPathRecalculation();
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
            Debug.LogWarning("A_Star: Missing grid/start/end references.");
            return;
        }

        var path = FindPathWorld();
        if (path == null)
        {
            Debug.Log("A_Star: No path found.");
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
        //h cost from current node to end node
        //g cost from start to current node
        //f cost = g + h
        var startCell = grid.WorldToCell(playerPosition.position);
        var goalCell = grid.WorldToCell(endPosition.position);

        List<Vector3Int> openList = new();
        openList.Clear();
        openList.Add(startCell);

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

            //If we have a path to the end
            if (current == goalCell)
                return ReconstructPath(cameFrom, current);

            //if we dont have a path to the end
            openList.Remove(current);
            closedList.Add(current);

            var parentOfCurrent = cameFrom[current];

            //Check neighbors
            for (int i = 0; i < directions.Length; i++)
            {
                Vector3Int neighborCell = current + directions[i];

                if (!IsWalkable(neighborCell) || closedList.Contains(neighborCell)) continue;

                //temp g score

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
                    openList.Add(neighborCell);
                }
            }
        }

        return null;
    }

    private List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int currentCellPosition)
    {
        //      path = []
        //      while current is not null:
        //      add current to beginning of path
        //      current = current.parent
        //    return path
        var path = new List<Vector3Int> { currentCellPosition };
        while (cameFrom.TryGetValue(currentCellPosition, out var parent))
        {
            if (parent == currentCellPosition) break; // Prevent infinite loop
            currentCellPosition = parent;
            path.Add(currentCellPosition);
        }

        path.Reverse();
        return path;
    }

    private float EuclideanCostEstimate(Vector3Int startCell, Vector3Int endCell)
    {
        //D = √((x₂ - x₁)² + (y₂ - y₁)²
        return Mathf.Sqrt(Mathf.Pow((startCell.x - endCell.x), 2) + Mathf.Pow((startCell.y - endCell.y), 2));
    }

    private bool IsWalkable(Vector3Int cellPosition)
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

        //dx and dy stands for distance in x and y axis
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        //sx and sy stands for step in x and y axis
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
}
