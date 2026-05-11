using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FloorGenerator
{
    [SerializeField] private int targetRoomCount = 10;
    [SerializeField] private int maxGridRadius = 4;
    [SerializeField] [Range(0f, 1f)] private float branchChance = 0.1f;
    [SerializeField] [Range(0f, 1f)] private float chestRoomChance = 0.99f;
    [SerializeField] private int minDeadEnds = 2;
    [SerializeField] private int maxGenerationAttempts = 8;

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

    public List<FloorNode> Generate(RoomDataPool pool)
    {
        List<FloorNode> bestNodes = null;
        int bestDeadEnds = -1;

        for (int attempt = 0; attempt < Mathf.Max(1, maxGenerationAttempts); attempt++)
        {
            var nodes = GenerateGraph();
            int deadEnds = CountDeadEnds(nodes);

            if (deadEnds >= minDeadEnds)
            {
                AssignSpecialRooms(nodes, pool);
                return nodes;
            }

            if (deadEnds > bestDeadEnds)
            {
                bestDeadEnds = deadEnds;
                bestNodes = nodes;
            }
        }

        // Не нашли граф с достаточным числом тупиков — берём лучший из попыток
        AssignSpecialRooms(bestNodes, pool);
        return bestNodes;
    }

    private List<FloorNode> GenerateGraph()
    {
        var nodes = new List<FloorNode>();
        var grid = new Dictionary<Vector2Int, int>();

        var startNode = new FloorNode { id = 0, type = RoomType.Start, gridPos = Vector2Int.zero };
        nodes.Add(startNode);
        grid[Vector2Int.zero] = 0;

        var current = Vector2Int.zero;

        while (nodes.Count < targetRoomCount)
        {
            // Иногда прыгаем в случайную существующую комнату — это создаёт разветвления
            // вместо длинных змееобразных проходов
            if (nodes.Count > 1 && Random.value < branchChance)
                current = nodes[Random.Range(0, nodes.Count)].gridPos;

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

        return nodes;
    }

    private static int CountDeadEnds(List<FloorNode> nodes)
    {
        int count = 0;
        foreach (var n in nodes)
            if (n.id != 0 && n.NeighborCount() == 1)
                count++;
        return count;
    }

    private void AssignSpecialRooms(List<FloorNode> nodes, RoomDataPool pool)
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

        // Shop — гарантированная, кап-1.
        // Chest — появляется только через явный проброс (chestRoomChance), кап-1.
        // Оба типа исключаются из случайного выбора оставшихся тупиков.
        var excludeFromRandom = new HashSet<RoomType> { RoomType.Chest };
        if (TryGuaranteeType(deadEnds, RoomType.Shop, pool)) excludeFromRandom.Add(RoomType.Shop);
        if (Random.value < chestRoomChance) TryGuaranteeType(deadEnds, RoomType.Chest, pool);

        // Остальные тупики — взвешенный рандом, без уже занятых типов
        foreach (var n in deadEnds)
            n.type = SelectWeighted(deadEndWeights, pool, excludeFromRandom);

        // Проходные комнаты — взвешенный рандом
        foreach (var n in nodes)
            if (n.type == RoomType.Normal && n.NeighborCount() > 1)
                n.type = SelectWeighted(throughRoomWeights, pool, null);
    }

    private static bool TryGuaranteeType(List<FloorNode> deadEnds, RoomType type, RoomDataPool pool)
    {
        if (deadEnds.Count == 0) return false;
        if (pool != null && !pool.HasContent(type)) return false;

        deadEnds[0].type = type;
        deadEnds.RemoveAt(0);
        return true;
    }

    private static RoomType SelectWeighted(RoomTypeWeight[] weights, RoomDataPool pool, HashSet<RoomType> exclude)
    {
        float total = 0f;
        foreach (var w in weights)
        {
            if (pool != null && !pool.HasContent(w.type)) continue;
            if (exclude != null && exclude.Contains(w.type)) continue;
            total += w.weight;
        }

        if (total <= 0f) return RoomType.Normal;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var w in weights)
        {
            if (pool != null && !pool.HasContent(w.type)) continue;
            if (exclude != null && exclude.Contains(w.type)) continue;
            cumulative += w.weight;
            if (roll <= cumulative) return w.type;
        }
        return RoomType.Normal;
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
