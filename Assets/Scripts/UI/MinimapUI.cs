using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private RectTransform container;
    [SerializeField] private float iconSize = 24f;
    [SerializeField] private float spacing = 32f;
    [SerializeField] private float padding = 16f;
    [SerializeField] private Sprite playerIcon;

    private readonly Dictionary<int, Image> _roomIcons = new();
    private readonly Dictionary<int, FloorNode> _nodes = new();
    private readonly Dictionary<(int, int), Image> _connectors = new();
    private readonly HashSet<int> _revealedRooms = new();
    private Image _playerMarker;

    private void Awake()
    {
        _playerMarker = CreateImage("PlayerMarker", iconSize, iconSize).GetComponent<Image>();
        _playerMarker.sprite = playerIcon;
        _playerMarker.color = Color.white;
        _playerMarker.gameObject.SetActive(false);
    }

    public void BuildMinimap(List<FloorNode> nodes)
    {
        foreach (Transform child in container)
            if (child != _playerMarker.transform)
                Destroy(child.gameObject);

        _roomIcons.Clear();
        _nodes.Clear();
        _connectors.Clear();
        _revealedRooms.Clear();

        foreach (var node in nodes)
            _nodes[node.id] = node;

        // Коннекторы — скрыты изначально, под иконками комнат
        foreach (var node in nodes)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!node.neighbors[i].HasValue) continue;
                int neighborId = node.neighbors[i].Value;
                if (neighborId < node.id) continue;

                var key = (node.id, neighborId);
                var dir = (Direction)i;
                bool horizontal = dir == Direction.East || dir == Direction.West;
                float w = horizontal ? spacing - iconSize : iconSize * 0.5f;
                float h = horizontal ? iconSize * 0.5f : spacing - iconSize;

                var conn = CreateImage("Connector", w, h).GetComponent<Image>();
                conn.color = Color.clear;
                var mid = (GridToMinimap(node.gridPos) + GridToMinimap(_nodes[neighborId].gridPos)) / 2f;
                conn.rectTransform.anchoredPosition = mid;
                _connectors[key] = conn;
            }
        }

        // Иконки комнат — поверх коннекторов
        foreach (var node in nodes)
        {
            var icon = CreateImage($"Room_{node.id}", iconSize, iconSize).GetComponent<Image>();
            icon.color = Color.clear;
            icon.rectTransform.anchoredPosition = GridToMinimap(node.gridPos);
            _roomIcons[node.id] = icon;
        }

        _playerMarker.transform.SetAsLastSibling();
        FitToMap(nodes);
    }

    private void FitToMap(List<FloorNode> nodes)
    {
        var minPos = new Vector2Int(int.MaxValue, int.MaxValue);
        var maxPos = new Vector2Int(int.MinValue, int.MinValue);
        foreach (var node in nodes)
        {
            minPos = Vector2Int.Min(minPos, node.gridPos);
            maxPos = Vector2Int.Max(maxPos, node.gridPos);
        }

        Vector2 minMinimap = GridToMinimap(minPos);
        Vector2 maxMinimap = GridToMinimap(maxPos);
        Vector2 center = (minMinimap + maxMinimap) / 2f;

        container.anchoredPosition = -center;

        float width  = (maxPos.x - minPos.x) * spacing + iconSize + padding * 2f;
        float height = (maxPos.y - minPos.y) * spacing + iconSize + padding * 2f;
        if (panel != null)
            panel.sizeDelta = new Vector2(width, height);
    }

    public void RevealRoom(int nodeId)
    {
        if (!_roomIcons.TryGetValue(nodeId, out var icon)) return;
        if (!_nodes.TryGetValue(nodeId, out var node)) return;

        _revealedRooms.Add(nodeId);
        icon.color = GetRoomColor(node.type);

        foreach (var neighbor in node.neighbors)
        {
            if (!neighbor.HasValue) continue;
            int nId = neighbor.Value;

            // Показать соседнюю комнату как тёмно-серую если ещё скрыта
            if (!_revealedRooms.Contains(nId) && _roomIcons.TryGetValue(nId, out var neighborIcon))
                if (neighborIcon.color == Color.clear)
                    neighborIcon.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            // Показать коннектор между текущей и соседней комнатой
            var key = nodeId < nId ? (nodeId, nId) : (nId, nodeId);
            if (_connectors.TryGetValue(key, out var conn))
                conn.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        }
    }

    public void SetCurrentRoom(int nodeId)
    {
        if (!_roomIcons.TryGetValue(nodeId, out var icon)) return;
        _playerMarker.gameObject.SetActive(true);
        _playerMarker.rectTransform.anchoredPosition = icon.rectTransform.anchoredPosition;
        _playerMarker.transform.SetAsLastSibling();
    }

    private GameObject CreateImage(string objName, float w, float h)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(container, false);
        var img = go.AddComponent<Image>();
        img.rectTransform.sizeDelta = new Vector2(w, h);
        return go;
    }

    private Vector2 GridToMinimap(Vector2Int gridPos) =>
        new Vector2(gridPos.x * spacing, gridPos.y * spacing);

    private static Color GetRoomColor(RoomType type) => type switch
    {
        RoomType.Start     => new Color(0.3f, 0.8f, 0.3f),
        RoomType.Normal    => new Color(0.7f, 0.7f, 0.7f),
        RoomType.Boss      => new Color(0.9f, 0.2f, 0.2f),
        RoomType.Shop      => new Color(0.9f, 0.8f, 0.2f),
        RoomType.Chest     => new Color(0.9f, 0.6f, 0.1f),
        RoomType.Challenge => new Color(0.7f, 0.3f, 0.9f),
        RoomType.Gauntlet  => new Color(0.8f, 0.3f, 0.3f),
        RoomType.Secret    => new Color(0.2f, 0.9f, 0.9f),
        RoomType.Empty     => new Color(0.4f, 0.4f, 0.4f),
        _                  => Color.white
    };
}
