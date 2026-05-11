using System.Collections;
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

        if (Node.type == RoomType.Boss)
            FloorManager.Instance.CameraFollowRef?.Shake(1.5f, 0.15f);

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
        const float horizontalOffset = 4f;

        var chestPos = Center + new Vector3(-horizontalOffset, 0f, 0f);
        var portalPos = Center + new Vector3(horizontalOffset, 0f, 0f);

        StartCoroutine(SpawnWithEffect(FloorManager.Instance.BossChestPrefab, chestPos));
        StartCoroutine(SpawnWithEffect(FloorManager.Instance.PortalPrefab, portalPos));
    }

    private static GameObject _spawnEffectCached;
    private static bool _spawnEffectLoaded;

    private static GameObject SpawnEffectPrefab
    {
        get
        {
            if (!_spawnEffectLoaded)
            {
                _spawnEffectCached = Resources.Load<GameObject>("Effects/SpawnEffect");
                _spawnEffectLoaded = true;
            }
            return _spawnEffectCached;
        }
    }

    private IEnumerator SpawnWithEffect(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) yield break;

        var parent = FloorManager.RuntimeRoot;

        float delay = 0f;
        var effectPrefab = SpawnEffectPrefab;
        if (effectPrefab != null)
        {
            var effect = Instantiate(effectPrefab, pos, Quaternion.identity, parent);
            effect.transform.localScale = Vector3.one * 2f;

            var anim = effect.GetComponent<SpriteSheetAnimator>();
            delay = anim != null ? anim.AnimationDuration : 0.5f;

            if (!effect.TryGetComponent<DestroyAfterSpriteSheetAnimation>(out _))
                effect.AddComponent<DestroyAfterSpriteSheetAnimation>();
        }

        yield return new WaitForSeconds(delay);

        Instantiate(prefab, pos, Quaternion.identity, parent);
    }

    private void SetDoorsLocked(bool locked)
    {
        foreach (var door in doors)
            if (door.gameObject.activeSelf)
                door.SetLocked(locked);
    }
}
