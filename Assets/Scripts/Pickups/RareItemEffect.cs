using UnityEngine;

public class RareItemEffect : MonoBehaviour
{
    [Header("Float")]
    [SerializeField] private float floatHeight = 0.15f;
    [SerializeField] private float floatSpeed = 2f;

    [Header("Sway")]
    [SerializeField] private float swayAngle = 8f;
    [SerializeField] private float swaySpeed = 1.5f;

    [Header("Shake")]
    [SerializeField] private float shakeIntensity = 0.05f;
    [SerializeField] private float shakeSpeed = 20f;

    [Header("Pulse")]
    [SerializeField] private float pulseAmount = 0.1f;
    [SerializeField] private float pulseSpeed = 4f;

    [Header("Shadow")]
    [SerializeField] private SpriteRenderer shadow;

    private Vector3 _startPos;
    private Vector3 _baseScale;
    private Vector3 _shadowBaseScale;
    private float _timeOffset;

    private void Start()
    {
        _startPos = transform.position;
        _baseScale = transform.localScale;
        _timeOffset = Random.Range(0f, Mathf.PI * 2f);

        if (shadow != null)
            _shadowBaseScale = shadow.transform.localScale;
    }

    private void Update()
    {
        float t = Time.time + _timeOffset;

        // Подъём-опускание
        float yOffset = Mathf.Sin(t * floatSpeed) * floatHeight;

        // Тряска через Perlin
        float shakeX = (Mathf.PerlinNoise(t * shakeSpeed, 0f) - 0.5f) * 2f * shakeIntensity;
        float shakeY = (Mathf.PerlinNoise(0f, t * shakeSpeed) - 0.5f) * 2f * shakeIntensity;

        transform.position = _startPos + new Vector3(shakeX, yOffset + shakeY, 0f);

        // Покачивание
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * swaySpeed) * swayAngle);

        // Резкая пульсация масштаба
        float pulse = 1f + Mathf.Abs(Mathf.Sin(t * pulseSpeed)) * pulseAmount;
        transform.localScale = _baseScale * pulse;

        // Тень
        if (shadow != null)
        {
            float normalized = (yOffset + floatHeight) / (floatHeight * 2f);
            float shadowScale = Mathf.Lerp(0.7f, 1f, 1f - normalized);
            shadow.transform.localScale = _shadowBaseScale * shadowScale;
            shadow.transform.rotation = Quaternion.identity;
            var c = shadow.color;
            c.a = Mathf.Lerp(0.3f, 0.55f, 1f - normalized);
            shadow.color = c;
        }
    }
}
