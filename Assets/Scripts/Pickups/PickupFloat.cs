using UnityEngine;

public class PickupFloat : MonoBehaviour
{
    [Header("Float")]
    [SerializeField] private float floatHeight = 0.15f;
    [SerializeField] private float floatSpeed = 2f;

    [Header("Rotation")]
    [SerializeField] private float swayAngle = 8f;
    [SerializeField] private float swaySpeed = 1.5f;

    [Header("Scale Pulse")]
    [SerializeField] private float pulseAmount = 0.06f;
    [SerializeField] private float pulseSpeed = 3f;

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
        float t = Time.time * floatSpeed + _timeOffset;

        // Плавный подъём-опускание
        float yOffset = Mathf.Sin(t) * floatHeight;
        transform.position = _startPos + new Vector3(0f, yOffset, 0f);

        // Покачивание
        float sway = Mathf.Sin(Time.time * swaySpeed + _timeOffset) * swayAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, sway);

        // Пульсация масштаба - чуть больше вверху, меньше внизу
        float pulse = 1f + Mathf.Sin(t * pulseSpeed * 0.5f) * pulseAmount;
        transform.localScale = _baseScale * pulse;

        // Тень
        if (shadow != null)
        {
            float normalized = (yOffset + floatHeight) / (floatHeight * 2f);
            float shadowScale = Mathf.Lerp(0.55f, 1f, 1f - normalized);
            shadow.transform.localScale = _shadowBaseScale * shadowScale;

            var c = shadow.color;
            c.a = Mathf.Lerp(0.15f, 0.45f, 1f - normalized);
            shadow.color = c;
        }
    }
}
