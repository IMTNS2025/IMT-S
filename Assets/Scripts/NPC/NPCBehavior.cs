using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCBehavior : MonoBehaviour
{
    public enum NPCState
    {
        Idle,
        Walking,
        Working
    }

    public NPCState currentState = NPCState.Idle;
    [SerializeField] private Grid grid;
    [SerializeField] private List<Transform> workStationPositions;
    [SerializeField] private float walkingSpeed;
    private List<Vector3> path = new();

    private Coroutine movementCoroutine;
    private int currentPathIndex = 0;
    public Vector3 currentPosition;

    private void OnEnable()
    {
        // Subscribe to per-owner path results only
        EventManager.OnPathCalculatedFor.AddListener(OnPathCalculatedForOwner);
    }

    private void OnDisable()
    {
        EventManager.OnPathCalculatedFor.RemoveListener(OnPathCalculatedForOwner);
    }

    private void Start()
    {
        RequestPath();
    }

    private void RequestPath()
    {
        if (currentState == NPCState.Idle && workStationPositions != null && workStationPositions.Count > 0)
        {
            int randomIndex = Random.Range(0, workStationPositions.Count);
            Vector3Int pos = Vector3Int.FloorToInt(workStationPositions[randomIndex].position);
            var v = grid.WorldToCell((Vector3)pos);

            // Per-owner path request: tag with this NPC's transform
            EventManager.OnPathRequested?.Invoke(transform, v);

            currentState = NPCState.Walking;
            currentPosition = v;
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

    // Receives only paths intended for this NPC instance
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
            while (Vector3.Distance(transform.position, target) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, walkingSpeed * Time.deltaTime);
                yield return null;
            }
            currentPathIndex++;
        }

        currentState = NPCState.Working;
    }
}
