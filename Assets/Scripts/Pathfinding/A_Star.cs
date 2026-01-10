using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class A_Star : MonoBehaviour
{
    private Vector3Int[] directions;

    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap[] walkableTiles;
    [SerializeField] private Tilemap[] obstaclesTiles;

    [SerializeField] private GameObject pathGameObject;
    [SerializeField] private bool useEuclidean = true;

    List<long> avrgTime = new();

    private string displayText = "Theta* Pathfinding";
    [SerializeField] private bool displayAvarageComputationTime;

    private void Awake()
    {
        Application.targetFrameRate = -1;
        if (grid == null)
        {
            UnityEngine.Debug.LogWarning("A_Star: Missing grid/start/end references.");
            return;
        }

        if (useEuclidean)
        {
            directions = new Vector3Int[]
            {
                new(-1, 0, 0),  //left
                new(1, 0, 0),   //right
                new(0, 1, 0),   //up
                new(0, -1, 0),  //down
                new(-1, 1, 0),  //left up
                new(1, 1, 0),   //right up
                new(-1, -1, 0), //left down
                new(1, -1, 0)   //right down
            };
        }
        else
        {
            directions = new Vector3Int[]
            {
                new(-1, 0, 0),  // left
                new(1, 0, 0),   // right
                new(0, 1, 0),   // up
                new(0, -1, 0),  // down
            };
        }
    }

    private void OnEnable()
    {
        EventManager.OnPathRequested.AddListener(HandlePathRequest);
    }

    private void OnDisable()
    {
        EventManager.OnPathRequested.RemoveListener(HandlePathRequest);
    }

    private float deltaTimeDisplay = 0.0f;
    private float fpsDisplay = 0.0f;

    void Update()
    {
        deltaTimeDisplay = Time.deltaTime;
        fpsDisplay = 1.0f / Time.deltaTime;
    }

    private void OnGUI()
    {
        if (!displayAvarageComputationTime) return;
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 45;
        style.normal.textColor = Color.black;

        GUI.Box(new Rect(100, 10, 1000, 100), displayText, style);
        //GUI.Box(new Rect(1500, 5, 700, 100), Time.deltaTime.ToString(), style);

        string text = $"{deltaTimeDisplay * 1000:F1}ms\n" +
                 $"FPS: {fpsDisplay:F0}\n";

        GUI.Label(new Rect(100, 100, 700, 200), text, style);
        GUI.Label(new Rect(100, 300, 300, 50), SceneManager.GetActiveScene().name, style);

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
        foreach (var c in cells)
            path.Add(grid.GetCellCenterWorld(c));

        EventManager.OnPathCalculatedFor?.Invoke(requester, path);
    }


    public List<Vector3Int> FindPathCells(Vector3Int startCell, Vector3Int goalCell, out int stepsTaken)
    {
        stepsTaken = 0;
        List<Vector3Int> openList = new();
        HashSet<Vector3Int> closedSet = new();
        Dictionary<Vector3Int, Vector3Int> cameFrom = new();
        Dictionary<Vector3Int, float> gScore = new() { [startCell] = 0 };
        Dictionary<Vector3Int, float> fScore = new();

        if (useEuclidean)
            fScore[startCell] = EuclideanCostEstimate(startCell, goalCell);
        else
            fScore[startCell] = ManhattanCostEstimate(startCell, goalCell);

        openList.Add(startCell);

        while (openList.Count > 0)
        {
            // Find node with lowest fScore
            Vector3Int current = default;
            float smallestF = float.MaxValue;

            foreach (var n in openList)
            {
                float f = fScore.TryGetValue(n, out float fValue) ? fValue : float.MaxValue;
                if (f < smallestF)
                {
                    smallestF = f;
                    current = n;
                }
            }
            stepsTaken++;
            if (current == goalCell)
                return ReconstructPath(cameFrom, current);

            openList.Remove(current);
            closedSet.Add(current);

            // Check neighbors
            for (int i = 0; i < directions.Length; i++)
            {
                Vector3Int neighbor = current + directions[i];

                if (closedSet.Contains(neighbor)) continue;
                if (!IsWalkable(neighbor)) continue;

                bool isDiagonal = directions[i].x != 0 && directions[i].y != 0;
                float moveCost = isDiagonal ? 1.414f : 1f;
                float tempGScore = gScore[current] + moveCost;

                // Check if this path is better
                if (!gScore.ContainsKey(neighbor) || tempGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tempGScore;

                    if (useEuclidean)
                        fScore[neighbor] = tempGScore + EuclideanCostEstimate(neighbor, goalCell);
                    else
                        fScore[neighbor] = tempGScore + ManhattanCostEstimate(neighbor, goalCell);

                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
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
            currentCellPosition = parent;
            path.Add(currentCellPosition);
        }
        path.Reverse();
        return path;
    }

    private float EuclideanCostEstimate(Vector3Int startCell, Vector3Int endCell)
    {
        float dx = endCell.x - startCell.x;
        float dy = endCell.y - startCell.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private float ManhattanCostEstimate(Vector3Int startCell, Vector3Int endCell)
    {
        return Mathf.Abs(endCell.x - startCell.x) + Mathf.Abs(endCell.y - startCell.y);
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
}