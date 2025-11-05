using System.Collections.Generic;
using UnityEngine;

public static class NPCPathRegistry
{
    // Stores upcoming grid cells for each NPC (from its current path index forward)
    private static readonly Dictionary<Transform, List<Vector3Int>> upcomingByOwner = new();

    public static void SetUpcoming(Transform owner, List<Vector3Int> upcomingCells)
    {
        if (owner == null) return;
        if (upcomingCells == null || upcomingCells.Count == 0)
        {
            upcomingByOwner.Remove(owner);
            return;
        }
        upcomingByOwner[owner] = upcomingCells;
    }

    public static List<Vector3Int> GetUpcoming(Transform owner)
    {
        if (owner == null) return null;
        return upcomingByOwner.TryGetValue(owner, out var cells) ? cells : null;
    }

    public static Vector3Int? GetNext(Transform owner)
    {
        var cells = GetUpcoming(owner);
        if (cells == null || cells.Count == 0) return null;
        return cells[0];
    }

    public static void Remove(Transform owner)
    {
        if (owner == null) return;
        upcomingByOwner.Remove(owner);
    }
}