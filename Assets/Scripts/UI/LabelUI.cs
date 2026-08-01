using System.Collections;
using System.Collections.Generic;
using TMPro;
using ChaosArena.Platform;
using UnityEngine;
using UnityEngine.UI;

public class LabelUI : MonoBehaviour
{
    public static LabelUI Instance { get; private set; }

    private TextMeshProUGUI _text;
    private CanvasGroup _canvasGroup;
    private Transform _followTarget;
    private Transform _currentSource;
    private ProximityLabel _currentLabel;
    private Vector3 _worldOffset;
    private Coroutine _fadeCoroutine;
    private RectTransform _labelRect;
    private Transform _player;

    private readonly List<ProximityLabel> _stack = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var container = new GameObject("Label");
        container.transform.SetParent(transform, false);
        _labelRect = container.AddComponent<RectTransform>();
        _labelRect.anchorMin = _labelRect.anchorMax = _labelRect.pivot = new Vector2(0.5f, 0f);
        // Fixed virtual bounds keep long RU/TR descriptions inside a compact
        // landscape viewport. TMP wraps first and ellipsizes only if it still
        // exceeds this deliberately bounded tooltip area.
        _labelRect.sizeDelta = new Vector2(760f, 560f);

        _canvasGroup = container.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(container.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        _text = textGo.AddComponent<TextMeshProUGUI>();
        _text.fontSize = 28f;
        _text.alignment = TextAlignmentOptions.Center;
        _text.color = new Color(0.96f, 0.88f, 0.70f, 1f);
        _text.textWrappingMode = TextWrappingModes.Normal;
        _text.overflowMode = TextOverflowModes.Ellipsis;
        _text.margin = new Vector4(24f, 0f, 24f, 0f);

        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/BoldPixels Font");
        if (font != null) _text.font = font;

        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
        if (Instance == this)
            Instance = null;
    }

    private float _targetRotation;

    private void Update()
    {
        if (_followTarget != null && _player != null)
        {
            var dir = (_player.position - _followTarget.position).normalized;
            _targetRotation = dir.x * 6f;
        }

        _labelRect.localRotation = Quaternion.Lerp(
            _labelRect.localRotation,
            Quaternion.Euler(0f, 0f, -_targetRotation),
            Time.deltaTime * 6f
        );
    }

    private void LateUpdate()
    {
        if (_followTarget == null) return;
        var screenPos = Camera.main.WorldToScreenPoint(_followTarget.position + _worldOffset);
        _labelRect.position = screenPos;
    }

    public void Register(ProximityLabel label, Transform player)
    {
        _stack.Remove(label);
        _stack.Add(label);
        _player = player;
        ShowTop();
    }

    public void Unregister(ProximityLabel label)
    {
        _stack.Remove(label);
        if (_stack.Count > 0)
            ShowTop();
        else
            HideAll();
    }

    private void ShowTop()
    {
        var top = _stack[_stack.Count - 1];
        _currentSource = top.transform;
        _currentLabel = top;
        _followTarget = top.transform;
        _worldOffset = top.Offset;
        _text.text = top.LabelText;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(Fade(1f));
    }

    private void HideAll()
    {
        _currentSource = null;
        _currentLabel = null;
        _followTarget = null;
        _player = null;
        _targetRotation = 0f;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(Fade(0f));
    }

    public void Show(Transform target, Vector3 offset, string label, Transform player)
    {
        _currentSource = target;
        _currentLabel = null;
        _followTarget = target;
        _worldOffset = offset;
        _text.text = label;
        _player = player;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(Fade(1f));
    }

    public void Hide(Transform source)
    {
        if (_currentSource != source) return;
        _currentSource = null;
        _currentLabel = null;
        _followTarget = null;
        _player = null;
        _targetRotation = 0f;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(Fade(0f));
    }

    private IEnumerator Fade(float target)
    {
        while (!Mathf.Approximately(_canvasGroup.alpha, target))
        {
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, target, 5f * Time.deltaTime);
            yield return null;
        }
        _canvasGroup.alpha = target;
    }

    private void OnLanguageChanged(string _)
    {
        if (_currentLabel != null)
            _text.text = _currentLabel.LabelText;
    }
}
