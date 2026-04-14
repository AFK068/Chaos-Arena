using System.Collections;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] private Sprite[] openFrames;
    [SerializeField] private Sprite[] disappearFrames;
    [SerializeField] private Color disappearColor = Color.white;
    [SerializeField] private float openFps = 12f;
    [SerializeField] private float destroyDelay = 3f;

    [Header("Drops")]
    [SerializeField] private GameObject[] dropPrefabs;
    [SerializeField] private float dropRadius = 1f;
    [SerializeField] private float dropDuration = 0.4f;
    private SpriteRenderer _renderer;
    private bool _opened;
    private float _baseBottomY;
    private Collider2D _collider;

    private void Awake()
    {
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        if (_renderer != null && _renderer.sprite != null)
        {
            var scale = _renderer.transform.lossyScale.y;
            _baseBottomY = _renderer.transform.position.y + _renderer.sprite.bounds.min.y * scale;
        }
    }

    public bool CanInteract(GameObject interactor) => !_opened;

    public void Interact(GameObject interactor)
    {
        if (_opened) return;
        _opened = true;
        StartCoroutine(OpenRoutine());
    }

    public Vector3 GetInteractionPosition() => transform.position;

    private void SpawnDrops()
    {
        if (dropPrefabs == null || dropPrefabs.Length == 0) return;

        var count = dropPrefabs.Length;
        // Случайный начальный угол чтобы было не всегда одинаково
        var startAngle = Random.Range(0f, 360f / count);

        for (var i = 0; i < count; i++)
        {
            if (dropPrefabs[i] == null) continue;

            // Равномерно по кругу
            var angle = startAngle + i * (360f / count);
            var rad = angle * Mathf.Deg2Rad;
            var target = transform.position + new Vector3(
                Mathf.Cos(rad) * dropRadius,
                Mathf.Sin(rad) * dropRadius,
                0f
            );

            var instance = Instantiate(dropPrefabs[i], transform.position, Quaternion.identity);
            var drop = instance.GetComponent<ItemDrop>();
            if (drop == null)
                drop = instance.AddComponent<ItemDrop>();
            drop.Throw(target, dropDuration);
        }
    }

    private IEnumerator OpenRoutine()
    {
        if (openFrames != null && openFrames.Length > 0 && _renderer != null)
        {
            var interval = 1f / Mathf.Max(openFps, 1f);
            for (var i = 0; i < openFrames.Length; i++)
            {
                var frame = openFrames[i];
                _renderer.sprite = frame;
                var scale = _renderer.transform.lossyScale.y;
                var pos = _renderer.transform.position;
                pos.y = _baseBottomY - frame.bounds.min.y * scale;
                _renderer.transform.position = pos;
                yield return new WaitForSeconds(interval);
            }
        }

        if (_collider != null)
            _collider.enabled = false;

        yield return new WaitForSeconds(destroyDelay);

        SpawnDrops();

        var shadow = transform.Find("Shadow");
        if (shadow != null)
            Destroy(shadow.gameObject);

        if (disappearFrames != null && disappearFrames.Length > 0 && _renderer != null)
        {
            _renderer.color = disappearColor;
            var interval = 1f / Mathf.Max(openFps, 1f);
            foreach (var frame in disappearFrames)
            {
                _renderer.sprite = frame;
                yield return new WaitForSeconds(interval);
            }
        }

        Destroy(gameObject);
    }
}
