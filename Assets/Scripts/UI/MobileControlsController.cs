using System.Collections.Generic;
using ChaosArena.Platform;
using TMPro;
using UnityEngine;
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
    private const float ClusterGap = 12f;

    private PlayerMovement _movement;
    private PlayerShoot _shoot;
    private PlayerInteractor _interactor;
    private PauseMenu _pauseMenu;
    private MobileAutoAimController _autoAim;
    private GameObject _overlay;
    private RectTransform _safeAreaRoot;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;
    private bool _pauseOverlayOpen;
    private bool _landscapeGameplayActive;
    private bool _landscapeVisibilityInitialized;
    private MobileHand _hand;
    private TextMeshProUGUI _handLabel;
    private readonly List<MobileLabel> _localizedLabels = new();
    private readonly List<MobileControlPlacement> _handPlacements = new();

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _shoot = GetComponent<PlayerShoot>();
        _interactor = GetComponent<PlayerInteractor>();
    }

    private void Start()
    {
        YandexPlatformService.TouchDeviceReady += OnTouchDeviceReady;
    }

    private void OnTouchDeviceReady(bool isTouchDevice)
    {
        if (!isTouchDevice || _overlay != null)
            return;

        _pauseMenu = FindFirstObjectByType<PauseMenu>();
        if (_pauseMenu != null)
        {
            _pauseMenu.OverlayVisibilityChanged += OnPauseOverlayVisibilityChanged;
            _pauseOverlayOpen = _pauseMenu.IsOverlayOpen;
        }
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        _autoAim = GetComponent<MobileAutoAimController>();
        if (_autoAim == null)
            _autoAim = gameObject.AddComponent<MobileAutoAimController>();
        _hand = MobileHandPreference.Load(new PlayerPrefsMobileHandStore());
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
        ClearMobileGameplayInput();
    }

    private void OnDestroy()
    {
        YandexPlatformService.TouchDeviceReady -= OnTouchDeviceReady;
        if (_pauseMenu != null)
            _pauseMenu.OverlayVisibilityChanged -= OnPauseOverlayVisibilityChanged;
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
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

        var moveStick = CreateStick("MoveStick", new Vector2(EdgePadding, EdgePadding));
        moveStick.ValueChanged += value => _movement?.SetMoveInput(value);

        CreateButton("DashButton", LocalizationCatalog.MobileDash,
            new Vector2(EdgePadding + ControlSize + ClusterGap, EdgePadding), () => _movement?.TryMobileDash());
        CreateButton("InteractButton", LocalizationCatalog.MobileInteract,
            new Vector2(EdgePadding + ControlSize + ClusterGap, EdgePadding + ButtonSize + ClusterGap),
            () => _interactor?.TryInteract());
        CreateButton("PauseButton", "II",
            new Vector2(EdgePadding + ControlSize + ClusterGap + 16f, EdgePadding + (ButtonSize + ClusterGap) * 2f),
            () => _pauseMenu?.Toggle(), PauseButtonSize);
        var handButton = CreateButton("HandSelector", GetHandLocalizationKey(),
            new Vector2(EdgePadding + ControlSize - PauseButtonSize, EdgePadding + (ButtonSize + ClusterGap) * 2f),
            ToggleHand, PauseButtonSize);
        _handLabel = handButton.GetComponentInChildren<TextMeshProUGUI>();
        ApplyHandLayout();
    }

    private MobileTouchStick CreateStick(string name, Vector2 clusterPosition)
    {
        var baseRect = CreateRect(name, _safeAreaRoot);
        RegisterHandPlacement(baseRect, clusterPosition);
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

    private RectTransform CreateButton(string name, string localizationKey, Vector2 clusterPosition,
        UnityEngine.Events.UnityAction action, float size = ButtonSize)
    {
        var buttonRect = CreateRect(name, _safeAreaRoot);
        RegisterHandPlacement(buttonRect, clusterPosition);
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
        return buttonRect;
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

    private void RegisterHandPlacement(RectTransform rect, Vector2 clusterPosition)
    {
        _handPlacements.Add(new MobileControlPlacement(rect, clusterPosition));
    }

    private void ApplyHandLayout()
    {
        var placement = MobileControlMath.GetHandPlacement(_hand);
        foreach (var control in _handPlacements)
        {
            control.Rect.anchorMin = new Vector2(placement.AnchorX, 0f);
            control.Rect.anchorMax = new Vector2(placement.AnchorX, 0f);
            control.Rect.pivot = new Vector2(placement.AnchorX, 0f);
            control.Rect.anchoredPosition = new Vector2(control.ClusterPosition.x * placement.HorizontalSign,
                control.ClusterPosition.y);
        }
    }

    private void UpdateLandscapeVisibility()
    {
        var visible = Screen.width >= Screen.height && !_pauseOverlayOpen;
        if (_landscapeVisibilityInitialized && _landscapeGameplayActive == visible)
            return;

        _landscapeVisibilityInitialized = true;
        _landscapeGameplayActive = visible;
        _overlay.SetActive(visible);
        _autoAim?.SetGameplayActive(visible);
        if (!visible)
            ClearMobileGameplayInput();
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
        if (_handLabel != null)
            _handLabel.text = LocalizationService.GetText(GetHandLocalizationKey());
    }

    private void ToggleHand()
    {
        _hand = MobileHandPreference.Toggle(new PlayerPrefsMobileHandStore());
        PlayerPrefs.Save();
        ApplyHandLayout();
        if (_handLabel != null)
            _handLabel.text = LocalizationService.GetText(GetHandLocalizationKey());
    }

    private string GetHandLocalizationKey() => _hand == MobileHand.Left
        ? LocalizationCatalog.MobileHandLeft
        : LocalizationCatalog.MobileHandRight;

    private void ClearMobileGameplayInput()
    {
        _movement?.ClearMoveInput();
        _shoot?.SetMobileAutoAimDirection(Vector2.zero);
        _autoAim?.SetGameplayActive(false);
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

    private readonly struct MobileControlPlacement
    {
        public MobileControlPlacement(RectTransform rect, Vector2 clusterPosition)
        {
            Rect = rect;
            ClusterPosition = clusterPosition;
        }

        public RectTransform Rect { get; }
        public Vector2 ClusterPosition { get; }
    }

    private sealed class PlayerPrefsMobileHandStore : IMobileHandPreferenceStore
    {
        public string GetString(string key, string defaultValue) => PlayerPrefs.GetString(key, defaultValue);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
    }
}
