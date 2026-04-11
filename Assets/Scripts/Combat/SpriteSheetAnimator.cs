using UnityEngine;

public class SpriteSheetAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 12f;

    private SpriteRenderer _renderer;
    private float _timer;
    private int _currentFrame;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0)
            _renderer.sprite = frames[0];
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
            _renderer.sprite = frames[_currentFrame];
        }
    }
}
