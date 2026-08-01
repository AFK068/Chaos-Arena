using System.Collections;
using ChaosArena.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FloorTransitionFX : MonoBehaviour
{
    public static FloorTransitionFX Instance { get; private set; }

    [Header("Monster (left)")]
    [SerializeField] private Sprite[] monsterFrames;
    [SerializeField] private Vector2 monsterSize = new Vector2(100f, 100f);

    [Header("Player (right)")]
    [SerializeField] private Sprite[] playerFrames;
    [SerializeField] private Vector2 playerSize = new Vector2(100f, 100f);

    [Header("Animation")]
    [SerializeField] private float fps = 8f;
    [SerializeField] private float moveSpeed = 600f;
    [SerializeField] private float spacing = 240f;

    [Header("Text")]
    [SerializeField] private TMPro.TMP_FontAsset font;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float fontSize = 36f;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.3f;

    private CanvasGroup _canvasGroup;
    private Image _monster;
    private Image _player;
    private TextMeshProUGUI _label;
    private RectTransform _canvasRect;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Build();
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        _canvasRect = GetComponent<RectTransform>();

        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;

        var bg = new GameObject("BG").AddComponent<Image>();
        bg.transform.SetParent(transform, false);
        bg.color = Color.black;
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        _monster = MakeImage("Monster", monsterSize);
        _player = MakeImage("Player", playerSize);

        _label = new GameObject("Label").AddComponent<TextMeshProUGUI>();
        _label.transform.SetParent(transform, false);
        _label.fontSize = fontSize;
        _label.color = textColor;
        _label.alignment = TextAlignmentOptions.Center;
        if (font != null) _label.font = font;
        var labelRect = _label.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(300f, 80f);
        labelRect.anchoredPosition = new Vector2(-9999f, 0f);
    }

    private Image MakeImage(string name, Vector2 size)
    {
        var img = new GameObject(name).AddComponent<Image>();
        img.transform.SetParent(transform, false);
        img.preserveAspect = true;
        img.color = Color.clear;
        img.GetComponent<RectTransform>().sizeDelta = size;
        return img;
    }

    public void Play(int floor, System.Action onMidpoint)
    {
        StartCoroutine(Routine(floor, onMidpoint, fadeIn: true));
    }

    public void PlayFromBlack(int floor, System.Action onMidpoint)
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        StartCoroutine(Routine(floor, onMidpoint, fadeIn: false));
    }

    private IEnumerator Routine(int floor, System.Action onMidpoint, bool fadeIn = true)
    {
        _monster.GetComponent<RectTransform>().anchoredPosition = new Vector2(-9999f, 0f);
        _player.GetComponent<RectTransform>().anchoredPosition = new Vector2(-9999f, 0f);
        _label.GetComponent<RectTransform>().anchoredPosition = new Vector2(-9999f, 0f);
        _label.text = LocalizationService.FormatText(LocalizationCatalog.FloorFormat, floor);
        _label.color = textColor;
        _canvasGroup.blocksRaycasts = true;

        if (fadeIn) yield return Fade(0f, 1f);
        onMidpoint?.Invoke();

        float halfW = _canvasRect.rect.width > 0f ? _canvasRect.rect.width * 0.5f : Screen.width * 0.5f;
        float startX = -halfW - 200f;
        float endX = halfW + 200f;

        SetSprite(_monster, monsterFrames, 0);
        SetSprite(_player, playerFrames, 0);

        float frameTimer = 0f;
        int frameIndex = 0;
        float x = startX;

        while (x < endX)
        {
            x += moveSpeed * Time.deltaTime;

            // [monster] [-spacing] [label] [+spacing] [player]
            _monster.GetComponent<RectTransform>().anchoredPosition = new Vector2(x - spacing, 0f);
            _label.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, 0f);
            _player.GetComponent<RectTransform>().anchoredPosition = new Vector2(x + spacing, 0f);

            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / Mathf.Max(fps, 0.01f))
            {
                frameTimer = 0f;
                frameIndex++;
                SetSprite(_monster, monsterFrames, frameIndex);
                SetSprite(_player, playerFrames, frameIndex);
            }

            yield return null;
        }

        yield return Fade(1f, 0f);
        _canvasGroup.blocksRaycasts = false;
    }

    private void SetSprite(Image img, Sprite[] frames, int index)
    {
        if (frames == null || frames.Length == 0) return;
        img.sprite = frames[index % frames.Length];
        img.color = Color.white;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = to;
    }
}
