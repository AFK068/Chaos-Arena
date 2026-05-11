using UnityEngine;

public class PulseScale : MonoBehaviour
{
    [SerializeField] private float speed = 4f;
    [SerializeField] [Range(0f, 1f)] private float amount = 0.15f;

    private Vector3 _baseAbs;

    private void OnEnable()
    {
        var s = transform.localScale;
        _baseAbs = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), s.z);
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * speed) * amount;
        var current = transform.localScale;
        float signX = current.x < 0f ? -1f : 1f;
        float signY = current.y < 0f ? -1f : 1f;
        transform.localScale = new Vector3(
            _baseAbs.x * pulse * signX,
            _baseAbs.y * pulse * signY,
            _baseAbs.z);
    }
}
