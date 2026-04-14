using System.Collections;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] private Sprite[] openFrames;
    [SerializeField] private Sprite[] disappearFrames;
    [SerializeField] private Color disappearColor = Color.white;
    [SerializeField] private float openFps = 12f;
    [SerializeField] private float destroyDelay = 3f;
    private SpriteRenderer _renderer;
    private bool _opened;
    private float _baseBottomY;

    private void Awake()
    {
        _renderer = GetComponentInChildren<SpriteRenderer>();
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

        yield return new WaitForSeconds(destroyDelay);

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
