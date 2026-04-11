using System.Collections.Generic;
using UnityEngine;

public class DebuffVisualHandler : MonoBehaviour
{
    [System.Serializable]
    private struct DebuffEffectEntry
    {
        public DebuffType type;
        public GameObject prefab;
        public Vector2 offset;
        public Vector2 scale;
    }

    [SerializeField] private DebuffEffectEntry[] effects;

    private readonly Dictionary<DebuffType, GameObject> _activeEffects = new();
    private readonly Dictionary<DebuffType, Vector3> _originalScales = new();
    private readonly Dictionary<DebuffType, Vector2> _offsets = new();
    private SpriteRenderer _parentRenderer;

    private void Awake()
    {
        _parentRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        // Отменяем только флип родителя, сохраняя оригинальный размер эффекта
        var flipX = transform.lossyScale.x < 0 ? -1f : 1f;
        var flipY = transform.lossyScale.y < 0 ? -1f : 1f;

        foreach (var kvp in _activeEffects)
        {
            if (kvp.Value == null) continue;
            if (!_originalScales.TryGetValue(kvp.Key, out var orig)) continue;
            kvp.Value.transform.localScale = new Vector3(
                orig.x * flipX,
                orig.y * flipY,
                orig.z
            );
            // Offset не флипается — всегда снизу/сверху как настроено
            if (_offsets.TryGetValue(kvp.Key, out var offset))
                kvp.Value.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        }
    }

    public void ShowEffect(DebuffType debuffType, GameObject fallbackPrefab = null)
    {
        if (_activeEffects.ContainsKey(debuffType))
            return;

        GameObject prefabToUse = null;
        var effectOffset = Vector2.zero;
        var effectScale = Vector2.one;

        foreach (var entry in effects)
        {
            if (entry.type == debuffType)
            {
                prefabToUse = entry.prefab;
                effectOffset = entry.offset;
                effectScale = entry.scale == Vector2.zero ? Vector2.one : entry.scale;
                break;
            }
        }

        if (prefabToUse == null)
            prefabToUse = fallbackPrefab;

        if (prefabToUse == null)
            return;

        var instance = Instantiate(prefabToUse, transform);
        instance.transform.localPosition = new Vector3(effectOffset.x, effectOffset.y, 0f);
        instance.transform.localScale = new Vector3(effectScale.x, effectScale.y, 1f);
        _originalScales[debuffType] = instance.transform.localScale;
        _offsets[debuffType] = effectOffset;

        // Рендерим поверх родительского спрайта
        var parentOrder = _parentRenderer != null ? _parentRenderer.sortingOrder : 0;
        foreach (var sr in instance.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.sortingLayerID = _parentRenderer != null ? _parentRenderer.sortingLayerID : sr.sortingLayerID;
            sr.sortingOrder = parentOrder + 1;
        }

        _activeEffects[debuffType] = instance;
    }

    public void HideEffect(DebuffType debuffType)
    {
        if (_activeEffects.TryGetValue(debuffType, out var instance))
        {
            if (instance != null)
                Destroy(instance);

            _activeEffects.Remove(debuffType);
            _originalScales.Remove(debuffType);
            _offsets.Remove(debuffType);
        }
    }
}
