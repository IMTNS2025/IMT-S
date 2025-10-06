using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class A_Star : MonoBehaviour
{
    private readonly Vector3Int[] directions = { new(-1, 0, 0), new(1, 0, 0), new(0, 1, 0), new(0, -1, 0) };

    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap[] walkableTiles;
    [SerializeField] private Tilemap[] obstaclesTiles;

    [SerializeField] private Transform playerPosition;
    [SerializeField] private Transform endPosition;
    [SerializeField] private GameObject pathGameObject;

    private List<Vector3> closedList = new();
    private Vector3Int currentCellPosition;

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

    private void Update()
    {
        DynamicPathRecalculation();
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
        Dictionary<Vector3Int, Vector3Int> cameFrom = new();

        Dictionary<Vector3Int, float> gScore = new() { [startCell] = 0 };
        Dictionary<Vector3Int, float> fScore = new() { [startCell] = HeuristicCostEstimate(startCell, goalCell) };

        openList.Add(startCell);

        while (openList.Count > 0)
        {
            float smallestF = float.MaxValue;

            foreach (var n in openList)
            {
                float f = fScore.TryGetValue(n, out float fValue) ? fValue : smallestF;

                if (f < smallestF)
                {
                    smallestF = f;
                    currentCellPosition = n;
                }
            }

            //If we have a path to the end
            if (currentCellPosition == goalCell)
                return ReconstructPath(cameFrom, currentCellPosition);

            //if we dont have a path to the end
            openList.Remove(currentCellPosition);
            closedList.Add(currentCellPosition);

            //Check neighbors
            for (int i = 0; i < directions.Length; i++)
            {
                Vector3Int neighborCellPosition = currentCellPosition + directions[i];
                if (closedList.Contains(neighborCellPosition)) continue;
                if (!IsWalkable(neighborCellPosition)) continue;

                //temp g score
                float tempGScore = gScore[currentCellPosition] + 1f;

                if (!openList.Contains(neighborCellPosition))
                    openList.Add(neighborCellPosition);
                else if (tempGScore >= gScore[neighborCellPosition]) continue;

                cameFrom[neighborCellPosition] = currentCellPosition;
                gScore[neighborCellPosition] = tempGScore;
                fScore[neighborCellPosition] = gScore[neighborCellPosition] + HeuristicCostEstimate(neighborCellPosition, goalCell);
                openList.Add(neighborCellPosition);
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
            currentCellPosition = parent;
            path.Add(currentCellPosition);
        }
        path.Reverse();
        return path;
    }

    private int HeuristicCostEstimate(Vector3Int startCell, Vector3Int endCell)
    {
        return Mathf.Abs(startCell.x - endCell.x) + Mathf.Abs(startCell.y - endCell.y);
    }

    private bool IsWalkable(Vector3Int cellPosition)
    {
        foreach (var obstacleTilemap in obstaclesTiles)
        {
            if (obstacleTilemap == null) continue;
            if (obstacleTilemap.HasTile(cellPosition))
                return false; // Cell is blocked by an obstacle
        }
        return true; // Cell is walkable and not blocked
    }

    public void SetEndGoal(Vector3Int cellPos)
    {
        endPosition.position = grid.GetCellCenterWorld(cellPos);
    }
}