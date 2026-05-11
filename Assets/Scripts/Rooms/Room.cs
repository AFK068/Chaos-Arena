using System.Collections.Generic;
using UnityEngine;

public enum RoomState { Unvisited, Active, Cleared }

public class Room : MonoBehaviour
{
    // Двери в порядке Direction: [0]=North [1]=South [2]=East [3]=West
    [SerializeField] private Door[] doors = new Door[4];
    // Точки появления игрока при входе с каждой стороны: [0]=North [1]=South [2]=East [3]=West
    [SerializeField] private Transform[] entryPoints = new Transform[4];

    public RoomState State { get; private set; } = RoomState.Unvisited;
    public FloorNode Node { get; private set; }
    public Vector3 Center => transform.position;

    public event System.Action OnLeft;

    public void NotifyLeft() => OnLeft?.Invoke();

    public Vector3 GetEntryPosition(Direction entryDirection) =>
        entryPoints[(int)entryDirection] != null
            ? entryPoints[(int)entryDirection].position
            : Center;

    private RoomSpawner _spawner;
    private readonly List<EnemyDeathNotifier> _aliveEnemies = new();

    private void Awake()
    {
        _spawner = GetComponent<RoomSpawner>();
    }

    // Вызывается FloorManager после инстанциирования
    public void Initialize(FloorNode node)
    {
        Node = node;
        for (int i = 0; i < 4; i++)
        {
            bool hasNeighbor = node.neighbors[i].HasValue;
            doors[i].gameObject.SetActive(hasNeighbor);
            if (hasNeighbor)
            {
                doors[i].direction = (Direction)i;
                doors[i].targetNodeId = node.neighbors[i].Value;
            }
        }
    }

    public void OnRoomEntered()
    {
        if (State == RoomState.Cleared) return;
        State = RoomState.Active;

        bool needsClearing = Node.type is RoomType.Normal or RoomType.Boss or RoomType.Gauntlet;
        if (needsClearing && _spawner != null)
        {
            SetDoorsLocked(true);
            _spawner.Spawn(Node.data, RegisterEnemy);
            if (_aliveEnemies.Count == 0)
                ClearRoom();
        }
        // Challenge — очищается через ChallengeGoal.OnReached → OnChallengeGoalReached()
        // Start/Chest/Shop/Empty/Secret — сразу Cleared
        else if (!needsClearing)
        {
            ClearRoom();
        }
    }

    private void RegisterEnemy(EnemyDeathNotifier notifier)
    {
        _aliveEnemies.Add(notifier);
        notifier.OnDied += OnEnemyDied;
    }

    private void OnEnemyDied(EnemyDeathNotifier sender)
    {
        _aliveEnemies.Remove(sender);
        if (_aliveEnemies.Count == 0)
            ClearRoom();
    }

    public void OnChallengeGoalReached() => ClearRoom();

    private void ClearRoom()
    {
        State = RoomState.Cleared;
        SetDoorsLocked(false);

        if (Node.type == RoomType.Boss)
            SpawnBossLoot();
    }

    private void SpawnBossLoot()
    {
        var portal = FloorManager.Instance.PortalPrefab;
        if (portal != null)
            Instantiate(portal, Center, Quaternion.identity);

        var pool = Node.data.bossDropPool;
        if (pool == null || pool.Length == 0) return;

        int count = Mathf.Max(0, Node.data.bossDropCount);
        float startAngle = Random.Range(0f, 360f);
        const float radius = 1.5f;
        const float duration = 0.5f;

        for (int i = 0; i < count; i++)
        {
            var prefab = pool[Random.Range(0, pool.Length)];
            if (prefab == null) continue;

            float angle = (startAngle + i * (360f / count)) * Mathf.Deg2Rad;
            var target = Center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);

            var instance = Instantiate(prefab, Center, Quaternion.identity);
            var drop = instance.GetComponent<ItemDrop>();
            if (drop == null) drop = instance.AddComponent<ItemDrop>();
            drop.Throw(target, duration);
        }
    }

    private void SetDoorsLocked(bool locked)
    {
        foreach (var door in doors)
            if (door.gameObject.activeSelf)
                door.SetLocked(locked);
    }
}
