using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private List<Vector3> path;
    [SerializeField] private Vector3 currentPosition;

    private Coroutine movementCoroutine;
    private int currentPathIndex = 0;

    private void OnEnable() => EventManager.OnPathCalculated.AddListener(CalculateWalkablePath);

    private void OnDisable() => EventManager.OnPathCalculated.RemoveListener(CalculateWalkablePath);

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

    private IEnumerator PlayerPositioning()
    {
        while (currentPathIndex < path.Count)
        {
            Vector3 target = path[currentPathIndex];
            while (Vector3.Distance(transform.position, target) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, 5f * Time.deltaTime);
                yield return null;
            }
            currentPathIndex++;
        }
    }
}
