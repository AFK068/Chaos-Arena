using UnityEngine;
using UnityEngine.UI;

public class UIImageAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 12f;

    private Image _image;
    private float _timer;
    private int _currentFrame;

    private void Awake()
    {
        _image = GetComponent<Image>();
        if (frames != null && frames.Length > 0)
            _image.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || fps <= 0f) return;

        _timer += Time.deltaTime;
        if (_timer >= 1f / fps)
        {
            _timer -= 1f / fps;
            _currentFrame = (_currentFrame + 1) % frames.Length;
            _image.sprite = frames[_currentFrame];
        }
    }
}
