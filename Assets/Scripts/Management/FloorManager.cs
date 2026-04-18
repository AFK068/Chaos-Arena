using System.Collections.Generic;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    [SerializeField] private FloorGenerator generator;
    [SerializeField] private RoomDataPool roomPool;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private Transform player;

    private List<FloorNode> _nodes;
    private readonly Dictionary<int, Room> _rooms = new();
    private Room _currentRoom;
    private bool _transitioning;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GenerateFloor();
    }

    public void GenerateFloor()
    {
        foreach (var room in _rooms.Values)
            if (room != null) Destroy(room.gameObject);
        _rooms.Clear();

        _nodes = generator.Generate();

        foreach (var node in _nodes)
        {
            node.data = roomPool.GetRandom(node.type);
            if (node.data?.roomPrefab == null)
            {
                Debug.LogWarning($"FloorManager: нет prefab для типа {node.type}");
                continue;
            }

            var roomGO = Instantiate(node.data.roomPrefab, GridToWorld(node.gridPos), Quaternion.identity);
            var room = roomGO.GetComponent<Room>();
            room.Initialize(node);
            _rooms[node.id] = room;
        }

        var startRoom = _rooms[0];
        player.position = startRoom.Center;
        cameraFollow.SnapToRoom(startRoom.Center);
        EnterRoom(startRoom);
    }

    public void TransitionToRoom(int targetNodeId, Direction fromDirection)
    {
        if (_transitioning) return;
        if (!_rooms.TryGetValue(targetNodeId, out var targetRoom)) return;

        _transitioning = true;

        Direction entryDir = Opposite(fromDirection);
        player.position = targetRoom.GetEntryPosition(entryDir);
        cameraFollow.PanToRoom(targetRoom.Center);
        EnterRoom(targetRoom);

        _transitioning = false;
    }

    private void EnterRoom(Room room)
    {
        _currentRoom = room;
        room.OnRoomEntered();
    }

    private static Vector3 GridToWorld(Vector2Int gridPos) =>
        new Vector3(gridPos.x * 21f, gridPos.y * 15f, 0f);

    private static Direction Opposite(Direction dir) => dir switch
    {
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East  => Direction.West,
        Direction.West  => Direction.East,
        _               => Direction.North
    };
}
