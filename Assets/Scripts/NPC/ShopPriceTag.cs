using System.Collections;
using TMPro;
using UnityEngine;

public class ShopPriceTag : MonoBehaviour
{
    [SerializeField] private float swayAmplitude = 3f;
    [SerializeField] private float swaySpeed = 1.2f;
    [SerializeField] private Vector3 offset = new Vector3(0f, -0.9f, 0f);

    private TextMeshProUGUI _text;
    private RectTransform _textRect;
    private float _swayTimer;
    private bool _shaking;
    private int _price;

    private static readonly Color ColorNormal = new Color(1f, 0.85f, 0.15f);
    private static readonly Color ColorFail = new Color(1f, 0.2f, 0.2f);

    public void Init(int price)
    {
        _price = price;
        if (price <= 0) { gameObject.SetActive(false); return; }
        CreateLabel();
    }

    private GameObject _canvasGo;
    private Vector3 _canvasOrigin;

    private void OnDestroy()
    {
        if (_canvasGo != null) Destroy(_canvasGo);
    }

    private void CreateLabel()
    {
        _canvasGo = new GameObject("PriceTag");
        _canvasGo.transform.position = transform.position + offset;
        _canvasGo.transform.localScale = Vector3.one * 0.025f;
        _canvasOrigin = _canvasGo.transform.position;

        var canvasGo = _canvasGo;

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60f, 20f);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);
        _textRect = textGo.AddComponent<RectTransform>();
        _textRect.sizeDelta = new Vector2(60f, 20f);

        _text = textGo.AddComponent<TextMeshProUGUI>();
        _text.text = $"$ {_price}";
        _text.fontSize = 14f;
        _text.alignment = TextAlignmentOptions.Center;
        _text.color = ColorNormal;
        _text.fontStyle = FontStyles.Bold;
        _text.textWrappingMode = TextWrappingModes.NoWrap;

        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/BoldPixels Font");
        if (font != null) _text.font = font;
    }

    private void Update()
    {
        if (_shaking || _text == null) return;
        _swayTimer += Time.deltaTime * swaySpeed;
        var x = Mathf.Sin(_swayTimer) * swayAmplitude * 0.01f;
        _canvasGo.transform.position = _canvasOrigin + new Vector3(x, 0f, 0f);
    }

    public void OnCannotAfford()
    {
        if (_shaking || _text == null) return;
        StartCoroutine(FailRoutine());
    }

    private IEnumerator FailRoutine()
    {
        _shaking = true;
        _text.color = ColorFail;

        float elapsed = 0f;
        float duration = 0.4f;
        var origin = _text.transform.parent.localPosition;

        while (elapsed < duration)
        {
            float t = 1f - elapsed / duration;
            float x = Random.Range(-1f, 1f) * 0.12f * t;
            _text.transform.parent.localPosition = origin + new Vector3(x, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _text.transform.parent.localPosition = origin;
        _text.color = ColorNormal;
        _shaking = false;
    }
}
