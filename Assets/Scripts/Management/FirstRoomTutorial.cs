using ChaosArena.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Non-interactive, runtime-only floor text for the first room of each run.
/// It is parented to FloorRuntime, so changing floor or leaving the gameplay
/// scene tears it down without any persisted tutorial state.
/// </summary>
public sealed class FirstRoomTutorial : MonoBehaviour
{
    private const float WorldScale = 0.009f;

    private TextMeshProUGUI _text;
    private PauseMenu _pauseMenu;
    private Room _room;

    public static FirstRoomTutorial Create(Room room, Transform runtimeRoot, PauseMenu pauseMenu)
    {
        var tutorialObject = new GameObject("FirstRoomTutorial");
        tutorialObject.transform.SetParent(runtimeRoot, false);
        tutorialObject.transform.position = room.Center + new Vector3(0f, -2.2f, 0f);
        return tutorialObject.AddComponent<FirstRoomTutorial>().Initialize(room, pauseMenu);
    }

    private FirstRoomTutorial Initialize(Room room, PauseMenu pauseMenu)
    {
        _room = room;
        _pauseMenu = pauseMenu;
        if (_room != null)
            _room.OnLeft += OnRoomLeft;
        if (_pauseMenu != null)
            _pauseMenu.OverlayVisibilityChanged += OnPauseOverlayVisibilityChanged;
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;

        CreateVisual();
        RefreshText();
        gameObject.SetActive(_pauseMenu == null || !_pauseMenu.IsOverlayOpen);
        return this;
    }

    private void OnDestroy()
    {
        if (_room != null)
            _room.OnLeft -= OnRoomLeft;
        if (_pauseMenu != null)
            _pauseMenu.OverlayVisibilityChanged -= OnPauseOverlayVisibilityChanged;
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
    }

    private void CreateVisual()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        canvas.worldCamera = Camera.main;

        var canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        var rect = GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1050f, 430f);
        rect.localScale = Vector3.one * WorldScale;

        var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(transform, false);
        var frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
        var frameImage = frame.GetComponent<Image>();
        frameImage.color = new Color(0.03f, 0.06f, 0.11f, 0.72f);
        frameImage.raycastTarget = false;

        var textObject = new GameObject("Hints", typeof(RectTransform));
        textObject.transform.SetParent(frame.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(46f, 30f);
        textRect.offsetMax = new Vector2(-46f, -30f);

        _text = textObject.AddComponent<TextMeshProUGUI>();
        _text.alignment = TextAlignmentOptions.Center;
        _text.fontSize = 62f;
        _text.enableWordWrapping = true;
        _text.overflowMode = TextOverflowModes.Ellipsis;
        _text.color = new Color(0.96f, 0.88f, 0.70f, 1f);
        _text.raycastTarget = false;
        _text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Press Start 2P Font")
            ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/BoldPixels Font");
    }

    private void RefreshText()
    {
        if (_text == null)
            return;

        var isTouch = MobileControlMath.IsTouchRuntime(
            Application.isMobilePlatform,
            SystemInfo.deviceType == DeviceType.Handheld,
            UnityEngine.InputSystem.Touchscreen.current != null);
        _text.text = LocalizationService.GetText(
            isTouch ? LocalizationCatalog.TutorialMobile : LocalizationCatalog.TutorialDesktop);
    }

    private void OnLanguageChanged(string _) => RefreshText();

    private void OnPauseOverlayVisibilityChanged(bool isOpen) => gameObject.SetActive(!isOpen);

    private void OnRoomLeft() => Destroy(gameObject);
}
