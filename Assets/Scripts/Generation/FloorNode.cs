using UnityEngine;

public enum RoomType { Start, Normal, Challenge, Gauntlet, Chest, Shop, Secret, Boss, Empty }
public enum Direction { North = 0, South = 1, East = 2, West = 3 }

public class FloorNode
{
    public int id;
    public RoomType type;
    public Vector2Int gridPos;
    public int?[] neighbors = new int?[4]; // индекс по Direction

    public int NeighborCount()
    {
        int count = 0;
        foreach (var n in neighbors)
            if (n.HasValue) count++;
        return count;
    }
}
