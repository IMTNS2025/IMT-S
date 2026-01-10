using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class Theta_Star : MonoBehaviour
{
    public static Theta_Star Instance { get; private set; }

    private Vector3Int[] directions;

    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap[] walkableTiles;
    [SerializeField] private Tilemap[] obstaclesTiles;

    [SerializeField] private GameObject pathGameObject;
    [SerializeField] private bool useEuclidean = false;

    private void Awake()
    {
        Application.targetFrameRate = -1;
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (grid == null) return;

        directions = useEuclidean
            ? new Vector3Int[] {
                new(-1, 0, 0), new(1, 0, 0), new(0, 1, 0), new(0, -1, 0),
                new(-1, 1, 0), new(1, 1, 0), new(-1, -1, 0), new(1, -1, 0)
              }
            : new Vector3Int[] {
                new(-1, 0, 0), new(1, 0, 0), new(0, 1, 0), new(0, -1, 0)
              };
    }

    private void OnEnable()
    {
        EventManager.OnPathRequested.AddListener(HandlePathRequest);
    }

    private void OnDisable()
    {
        EventManager.OnPathRequested.RemoveListener(HandlePathRequest);
    }

    public bool IsWalkable(Vector3Int cellPosition)
    {
        foreach (var obstacleTilemap in obstaclesTiles)
        {
            if (obstacleTilemap == null) continue;
            if (obstacleTilemap.HasTile(cellPosition)) return false;
        }
        return true;
    }

    public List<Vector3Int> FindPathCells(Vector3Int startCell, Vector3Int goalCell, out int stepsTaken)
    {
        stepsTaken = 0;
        List<Vector3Int> openList = new();
        openList.Add(startCell);

        var closedSet = new HashSet<Vector3Int>();

        Dictionary<Vector3Int, Vector3Int> cameFrom = new() { [startCell] = startCell };
        Dictionary<Vector3Int, float> gScore = new() { [startCell] = 0 };
        Dictionary<Vector3Int, float> fScore = new()
        {
            [startCell] = useEuclidean ? EuclideanCostEstimate(startCell, goalCell)
                                       : ManhattanCostEstimate(startCell, goalCell)
        };

        while (openList.Count > 0)
        {
            Vector3Int current = default;
            float smallestF = float.MaxValue;

            foreach (var n in openList)
            {
                float f = fScore.TryGetValue(n, out float fv) ? fv : smallestF;
                if (f < smallestF) { smallestF = f; current = n; }
            }
            stepsTaken++;
            if (current == goalCell) return ReconstructPath(cameFrom, current);

            openList.Remove(current);
            closedSet.Add(current);

            var parentOfCurrent = cameFrom[current];

            for (int i = 0; i < directions.Length; i++)
            {
                Vector3Int neighborCell = current + directions[i];
                if (!IsWalkable(neighborCell) || closedSet.Contains(neighborCell)) continue;

                Vector3Int potentialParent;
                float tempGScore;

                if (LineOfSight(parentOfCurrent, neighborCell))
                {
                    potentialParent = parentOfCurrent;
                    tempGScore = gScore[parentOfCurrent] +
                                 (useEuclidean ? EuclideanCostEstimate(parentOfCurrent, neighborCell)
                                               : ManhattanCostEstimate(parentOfCurrent, neighborCell));
                }
                else
                {
                    potentialParent = current;
                    tempGScore = gScore[current] +
                                 (useEuclidean ? EuclideanCostEstimate(current, neighborCell)
                                               : ManhattanCostEstimate(current, neighborCell));
                }

                float neighborGScore = gScore.TryGetValue(neighborCell, out float gv) ? gv : float.MaxValue;

                if (tempGScore < neighborGScore)
                {
                    cameFrom[neighborCell] = potentialParent;
                    gScore[neighborCell] = tempGScore;
                    fScore[neighborCell] = tempGScore +
                        (useEuclidean ? EuclideanCostEstimate(neighborCell, goalCell)
                                      : ManhattanCostEstimate(neighborCell, goalCell));

                    if (!openList.Contains(neighborCell)) openList.Add(neighborCell);
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
        return Mathf.Sqrt(Mathf.Pow(startCell.x - endCell.x, 2) + Mathf.Pow(startCell.y - endCell.y, 2));
    }

    private float ManhattanCostEstimate(Vector3Int startCell, Vector3Int endCell)
    {
        return Mathf.Abs(startCell.x - endCell.x) + Mathf.Abs(startCell.y - endCell.y);
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

    bool LineOfSight(Vector3Int start, Vector3Int end)
    {
        int currentX = start.x, currentY = start.y;
        int targetX = end.x, targetY = end.y;
        int deltaX = Mathf.Abs(targetX - currentX);
        int deltaY = Mathf.Abs(targetY - currentY);
        int stepX = (currentX < targetX) ? 1 : -1;
        int stepY = (currentY < targetY) ? 1 : -1;
        int error = deltaX - deltaY;

        while (true)
        {
            if (!IsWalkable(new Vector3Int(currentX, currentY, 0))) return false;
            if (currentX == targetX && currentY == targetY) return true;

            int doubledError = error * 2;
            if (doubledError > -deltaY) { error -= deltaY; currentX += stepX; }
            if (doubledError < deltaX) { error += deltaX; currentY += stepY; }
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

             var sw = Stopwatch.StartNew();
        var cells = FindPathCells(startCell, endCell, out int stepsTaken);
           sw.Stop();


        TestManager.Save(new ComputationAndLengthData
        {
            agent = requester.name,
            computationTimeMs = (float)(sw.ElapsedTicks * 1000f / Stopwatch.Frequency),
            pathNodesCount = cells != null ? cells.Count : 0,
            //pathWorldDistance = worldDistance,
            stepsTaken = stepsTaken
        });

        if (cells == null)
        {
            EventManager.OnPathCalculatedFor?.Invoke(requester, null);
            return;
        }

        var path = new List<Vector3>(cells.Count);
        foreach (var c in cells) path.Add(grid.GetCellCenterWorld(c));
        EventManager.OnPathCalculatedFor?.Invoke(requester, path);
    }
}
