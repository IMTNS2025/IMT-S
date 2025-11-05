using System.Collections.Generic;
using UnityEngine;

public static class DynamicObstacles
{
    private static Dictionary<Transform, Vector3Int> dynamicObstacles = new();

    public static void AddOrUpdateObstacle(Transform owner, Vector3Int position)
    {
        dynamicObstacles[owner] = position;
    }

    public static void RemoveObstacle(Transform owner)
    {
        if (dynamicObstacles.ContainsKey(owner))
        {
            dynamicObstacles.Remove(owner);
        }
    }

    public static Dictionary<Transform, Vector3Int> GetAllObstacles()
    {
        return dynamicObstacles;
    }

    public static bool IsPositionOccupied(Vector3Int position)
    {
        foreach (var obstaclePosition in dynamicObstacles.Values)
        {
            if (obstaclePosition == position)
            {
                return true;
            }
        }
        return false;
    }

    public static Transform GetOwnerAtPosition(Vector3Int pos)
    {
        foreach (var kv in dynamicObstacles)
        {
            if (kv.Value == pos)
                return kv.Key;
        }
        return null;
    }
}
