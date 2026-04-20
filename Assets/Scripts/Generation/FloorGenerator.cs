using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FloorGenerator
{
    [SerializeField] private int targetRoomCount = 10;
    [SerializeField] private int maxGridRadius = 4;

    [Header("Веса для тупиков (кроме Boss)")]
    [SerializeField] private RoomTypeWeight[] deadEndWeights = new RoomTypeWeight[]
    {
        new RoomTypeWeight { type = RoomType.Shop,   weight = 40 },
        new RoomTypeWeight { type = RoomType.Chest,  weight = 40 },
        new RoomTypeWeight { type = RoomType.Empty,  weight = 15 },
        new RoomTypeWeight { type = RoomType.Secret, weight = 5  },
    };

    [Header("Веса для проходных комнат")]
    [SerializeField] private RoomTypeWeight[] throughRoomWeights = new RoomTypeWeight[]
    {
        new RoomTypeWeight { type = RoomType.Normal,    weight = 60 },
        new RoomTypeWeight { type = RoomType.Challenge, weight = 25 },
        new RoomTypeWeight { type = RoomType.Gauntlet,  weight = 15 },
    };

    public List<FloorNode> Generate()
    {
        var nodes = new List<FloorNode>();
        var grid = new Dictionary<Vector2Int, int>();

        var startNode = new FloorNode { id = 0, type = RoomType.Start, gridPos = Vector2Int.zero };
        nodes.Add(startNode);
        grid[Vector2Int.zero] = 0;

        var current = Vector2Int.zero;

        while (nodes.Count < targetRoomCount)
        {
            var dir = (Direction)Random.Range(0, 4);
            var next = current + GetOffset(dir);

            if (Mathf.Abs(next.x) > maxGridRadius || Mathf.Abs(next.y) > maxGridRadius)
            {
                current = nodes[Random.Range(0, nodes.Count)].gridPos;
                continue;
            }

            if (!grid.ContainsKey(next))
            {
                int id = nodes.Count;
                var node = new FloorNode { id = id, gridPos = next, type = RoomType.Normal };
                nodes.Add(node);
                grid[next] = id;

                int currentId = grid[current];
                nodes[currentId].neighbors[(int)dir] = id;
                node.neighbors[(int)Opposite(dir)] = currentId;
            }

            current = next;
        }

        AssignSpecialRooms(nodes);
        return nodes;
    }

    private void AssignSpecialRooms(List<FloorNode> nodes)
    {
        var distances = BfsDistances(nodes, startId: 0);

        var deadEnds = new List<FloorNode>();
        foreach (var n in nodes)
            if (n.id != 0 && n.NeighborCount() == 1)
                deadEnds.Add(n);

        deadEnds.Sort((a, b) => distances[b.id].CompareTo(distances[a.id]));

        // Самый дальний тупик — всегда Boss
        if (deadEnds.Count > 0)
        {
            deadEnds[0].type = RoomType.Boss;
            deadEnds.RemoveAt(0);
        }

        // Остальные тупики — взвешенный рандом
        foreach (var n in deadEnds)
            n.type = SelectWeighted(deadEndWeights);

        // Проходные комнаты — взвешенный рандом
        foreach (var n in nodes)
            if (n.type == RoomType.Normal && n.NeighborCount() > 1)
                n.type = SelectWeighted(throughRoomWeights);
    }

    private static RoomType SelectWeighted(RoomTypeWeight[] weights)
    {
        float total = 0f;
        foreach (var w in weights) total += w.weight;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var w in weights)
        {
            cumulative += w.weight;
            if (roll <= cumulative) return w.type;
        }
        return weights[weights.Length - 1].type;
    }

    private static int[] BfsDistances(List<FloorNode> nodes, int startId)
    {
        var dist = new int[nodes.Count];
        for (int i = 0; i < dist.Length; i++) dist[i] = -1;
        var queue = new Queue<int>();
        dist[startId] = 0;
        queue.Enqueue(startId);
        while (queue.Count > 0)
        {
            int curr = queue.Dequeue();
            foreach (var n in nodes[curr].neighbors)
            {
                if (n.HasValue && dist[n.Value] == -1)
                {
                    dist[n.Value] = dist[curr] + 1;
                    queue.Enqueue(n.Value);
                }
            }
        }
        return dist;
    }

    private static Vector2Int GetOffset(Direction dir) => dir switch
    {
        Direction.North => Vector2Int.up,
        Direction.South => Vector2Int.down,
        Direction.East  => Vector2Int.right,
        Direction.West  => Vector2Int.left,
        _               => Vector2Int.zero
    };

    private static Direction Opposite(Direction dir) => dir switch
    {
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East  => Direction.West,
        Direction.West  => Direction.East,
        _               => Direction.North
    };
}

[System.Serializable]
public struct RoomTypeWeight
{
    public RoomType type;
    public float weight;
}
