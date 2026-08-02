using System.Collections;
using ChaosArena.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Brief feedback for automatically collected pickups.  These pickups do not
/// wait for an explicit interaction, so a proximity tooltip would often vanish
/// before it could be read; show the localized result after the effect applies.
/// </summary>
public sealed class PickupNotificationUI : MonoBehaviour
{
    private const float DisplaySeconds = 1.8f;

    private static PickupNotificationUI _instance;

    private TMP_Text _text;
    private CanvasGroup _canvasGroup;
    private Coroutine _routine;
    private string _key;

    public static void Show(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !LocalizationCatalog.HasKey(key))
            return;

        if (_instance == null)
        {
            var notificationObject = new GameObject(nameof(PickupNotificationUI));
            _instance = notificationObject.AddComponent<PickupNotificationUI>();
        }

        _instance.ShowInternal(key);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 101;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var container = new GameObject("Notification");
        container.transform.SetParent(transform, false);
        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = containerRect.anchorMax = new Vector2(0.5f, 0.78f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(980f, 220f);

        _canvasGroup = container.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;

        var textObject = new GameObject("Text");
        textObject.transform.SetParent(container.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        _text = textObject.AddComponent<TextMeshProUGUI>();
        _text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Press Start 2P Font");
        _text.fontSize = 26f;
        _text.enableAutoSizing = true;
        _text.fontSizeMin = 18f;
        _text.fontSizeMax = 26f;
        _text.alignment = TextAlignmentOptions.Center;
        _text.color = new Color(0.96f, 0.88f, 0.70f, 1f);
        _text.textWrappingMode = TextWrappingModes.Normal;

        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
        if (_instance == this)
            _instance = null;
    }

    private void ShowInternal(string key)
    {
        _key = key;
        _text.text = LocalizationService.GetText(_key);

        if (_routine != null)
            StopCoroutine(_routine);
        _routine = StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine()
    {
        _canvasGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(DisplaySeconds);

        while (_canvasGroup.alpha > 0f)
        {
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, 0f, 4f * Time.unscaledDeltaTime);
            yield return null;
        }

        _routine = null;
    }

    private void OnLanguageChanged(string _)
    {
        if (!string.IsNullOrWhiteSpace(_key))
            _text.text = LocalizationService.GetText(_key);
    }
}
