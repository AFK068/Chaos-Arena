using System.Collections.Generic;
using ChaosArena.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Runtime-only landscape touch overlay. It deliberately calls the same player
/// intent methods as input actions, leaving keyboard, mouse and gamepad intact.
/// </summary>
public sealed class MobileControlsController : MonoBehaviour
{
    // At 844x390 (a compact landscape WebGL viewport), CanvasScaler's 0.5
    // match yields ~0.40. These become ~104px sticks and ~57px buttons.
    private const float ControlSize = 260f;
    private const float ButtonSize = 144f;
    private const float PauseButtonSize = 112f;
    private const float EdgePadding = 20f;

    private PlayerMovement _movement;
    private PlayerShoot _shoot;
    private PlayerInteractor _interactor;
    private PauseMenu _pauseMenu;
    private GameObject _overlay;
    private RectTransform _safeAreaRoot;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;
    private bool _pauseOverlayOpen;
    private readonly List<MobileLabel> _localizedLabels = new();

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _shoot = GetComponent<PlayerShoot>();
        _interactor = GetComponent<PlayerInteractor>();
    }

    private void Start()
    {
        if (!ShouldShowTouchControls())
            return;

        _pauseMenu = FindFirstObjectByType<PauseMenu>();
        if (_pauseMenu != null)
        {
            _pauseMenu.OverlayVisibilityChanged += OnPauseOverlayVisibilityChanged;
            _pauseOverlayOpen = _pauseMenu.IsOverlayOpen;
        }
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        CreateOverlay();
        RefreshSafeArea();
        UpdateLandscapeVisibility();
    }

    private void LateUpdate()
    {
        if (_safeAreaRoot == null || (_lastSafeArea == Screen.safeArea && _lastScreenSize.x == Screen.width && _lastScreenSize.y == Screen.height))
            return;

        RefreshSafeArea();
        UpdateLandscapeVisibility();
    }

    private void OnDisable()
    {
        _movement?.SetMoveInput(Vector2.zero);
        _shoot?.SetShootDirection(Vector2.zero);
    }

    private void OnDestroy()
    {
        if (_pauseMenu != null)
            _pauseMenu.OverlayVisibilityChanged -= OnPauseOverlayVisibilityChanged;
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
    }

    private static bool ShouldShowTouchControls()
    {
        return MobileControlMath.IsTouchRuntime(
            Application.isMobilePlatform,
            SystemInfo.deviceType == DeviceType.Handheld,
            Touchscreen.current != null);
    }

    private void CreateOverlay()
    {
        _overlay = new GameObject("MobileControls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _overlay.transform.SetParent(null, false);

        var canvas = _overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = _overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _safeAreaRoot = CreateRect("SafeArea", _overlay.transform);
        Stretch(_safeAreaRoot);

        var moveStick = CreateStick("MoveStick", new Vector2(0f, 0f), new Vector2(EdgePadding, EdgePadding));
        moveStick.ValueChanged += value => _movement?.SetMoveInput(value);

        var aimStick = CreateStick("AimAndFireStick", new Vector2(1f, 0f), new Vector2(-EdgePadding, EdgePadding));
        aimStick.ValueChanged += value => _shoot?.SetShootDirection(value);

        CreateButton("DashButton", LocalizationCatalog.MobileDash, new Vector2(1f, 0f), new Vector2(-EdgePadding - ButtonSize, EdgePadding + ControlSize),
            () => _movement?.TryDash());
        CreateButton("InteractButton", LocalizationCatalog.MobileInteract, new Vector2(0f, 0f), new Vector2(EdgePadding + ButtonSize, EdgePadding + ControlSize),
            () => _interactor?.TryInteract());
        CreateButton("PauseButton", "II", new Vector2(1f, 0f), new Vector2(-EdgePadding, EdgePadding + ControlSize + ButtonSize * 2f + 12f),
            () => _pauseMenu?.Toggle(), PauseButtonSize);
    }

    private MobileTouchStick CreateStick(string name, Vector2 anchor, Vector2 position)
    {
        var baseRect = CreateRect(name, _safeAreaRoot);
        baseRect.anchorMin = anchor;
        baseRect.anchorMax = anchor;
        baseRect.pivot = anchor;
        baseRect.anchoredPosition = position;
        baseRect.sizeDelta = Vector2.one * ControlSize;
        var image = baseRect.gameObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.12f, 0.2f, 0.62f);

        var knob = CreateRect("Knob", baseRect);
        knob.anchorMin = knob.anchorMax = new Vector2(0.5f, 0.5f);
        knob.sizeDelta = Vector2.one * (ControlSize * 0.42f);
        var knobImage = knob.gameObject.AddComponent<Image>();
        knobImage.color = new Color(0.55f, 0.82f, 1f, 0.84f);
        knobImage.raycastTarget = false;

        var stick = baseRect.gameObject.AddComponent<MobileTouchStick>();
        stick.Initialize(knob);
        return stick;
    }

    private void CreateButton(string name, string localizationKey, Vector2 anchor, Vector2 position, UnityEngine.Events.UnityAction action, float size = ButtonSize)
    {
        var buttonRect = CreateRect(name, _safeAreaRoot);
        buttonRect.anchorMin = anchor;
        buttonRect.anchorMax = anchor;
        buttonRect.pivot = anchor;
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = Vector2.one * size;

        var image = buttonRect.gameObject.AddComponent<Image>();
        image.color = new Color(0.13f, 0.23f, 0.38f, 0.86f);
        var button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        var textRect = CreateRect("Label", buttonRect);
        Stretch(textRect);
        var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = LocalizationService.GetText(localizationKey);
        text.alignment = TextAlignmentOptions.Center;
        text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Press Start 2P Font");
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = localizationKey == "II" ? 26f : 18f;
        text.color = Color.white;
        text.raycastTarget = false;
        _localizedLabels.Add(new MobileLabel(text, localizationKey));
    }

    private void RefreshSafeArea()
    {
        _lastSafeArea = Screen.safeArea;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        var anchors = MobileControlMath.ToAnchors(_lastSafeArea.x, _lastSafeArea.y, _lastSafeArea.width, _lastSafeArea.height,
            Screen.width, Screen.height);
        _safeAreaRoot.anchorMin = new Vector2(anchors.MinX, anchors.MinY);
        _safeAreaRoot.anchorMax = new Vector2(anchors.MaxX, anchors.MaxY);
        _safeAreaRoot.offsetMin = Vector2.zero;
        _safeAreaRoot.offsetMax = Vector2.zero;
    }

    private void UpdateLandscapeVisibility()
    {
        var visible = Screen.width >= Screen.height && !_pauseOverlayOpen;
        if (_overlay.activeSelf == visible)
            return;

        _overlay.SetActive(visible);
        if (!visible)
        {
            _movement?.SetMoveInput(Vector2.zero);
            _shoot?.SetShootDirection(Vector2.zero);
        }
    }

    private void OnPauseOverlayVisibilityChanged(bool isOpen)
    {
        _pauseOverlayOpen = isOpen;
        UpdateLandscapeVisibility();
    }

    private void OnLanguageChanged(string _)
    {
        foreach (var label in _localizedLabels)
            label.Text.text = LocalizationService.GetText(label.Key);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var child = new GameObject(name, typeof(RectTransform));
        var rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private readonly struct MobileLabel
    {
        public MobileLabel(TextMeshProUGUI text, string key)
        {
            Text = text;
            Key = key;
        }

        public TextMeshProUGUI Text { get; }
        public string Key { get; }
    }
}
