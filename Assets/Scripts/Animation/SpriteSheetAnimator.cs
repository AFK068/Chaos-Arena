using UnityEngine;

public class SpriteSheetAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 12f;
    [SerializeField] private bool autoResizeCollider = false;

    public int FrameCount => frames != null ? frames.Length : 0;
    public float FramesPerSecond => fps;
    public float AnimationDuration => (fps > 0f && FrameCount > 0) ? FrameCount / fps : 0f;

    private SpriteRenderer _renderer;
    private BoxCollider2D _boxCollider;
    private CircleCollider2D _circleCollider;
    private float _timer;
    private int _currentFrame;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _circleCollider = GetComponent<CircleCollider2D>();

        if (frames != null && frames.Length > 0)
        {
            _renderer.sprite = frames[0];
            if (autoResizeCollider)
                UpdateCollider(frames[0]);
        }
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || fps <= 0f)
            return;

        _timer += Time.deltaTime;

        if (_timer >= 1f / fps)
        {
            _timer -= 1f / fps;
            _currentFrame = (_currentFrame + 1) % frames.Length;
            var sprite = frames[_currentFrame];
            _renderer.sprite = sprite;
            if (autoResizeCollider)
                UpdateCollider(sprite);
        }
    }

    private void UpdateCollider(Sprite sprite)
    {
        if (sprite == null) return;

        var size = sprite.bounds.size;

        if (_boxCollider != null)
        {
            _boxCollider.size = new Vector2(size.x, size.y);
        }

        if (_circleCollider != null)
        {
            _circleCollider.radius = Mathf.Max(size.x, size.y) / 2f;
        }
    }
}
