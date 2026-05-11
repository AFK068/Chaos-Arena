using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float panSpeed = 6f;

    private bool _transitioning;
    private Vector3 _target;

    private float _shakeTimeRemaining;
    private float _shakeMagnitude;
    private Vector3 _appliedShakeOffset;

    public void Shake(float duration, float magnitude)
    {
        _shakeTimeRemaining = duration;
        _shakeMagnitude = magnitude;
    }

    public void PanToRoom(Vector3 roomCenter)
    {
        _target = new Vector3(roomCenter.x, roomCenter.y, transform.position.z);
        _transitioning = true;
    }

    public void SnapToRoom(Vector3 roomCenter)
    {
        transform.position = new Vector3(roomCenter.x, roomCenter.y, transform.position.z);
        _transitioning = false;
    }

    private void LateUpdate()
    {
        // Снимаем shake-смещение прошлого кадра, чтобы Lerp работал по чистой базовой позиции
        transform.position -= _appliedShakeOffset;
        _appliedShakeOffset = Vector3.zero;

        if (_transitioning)
        {
            transform.position = Vector3.Lerp(transform.position, _target, panSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, _target) < 0.05f)
            {
                transform.position = _target;
                _transitioning = false;
            }
        }

        if (_shakeTimeRemaining > 0f)
        {
            _shakeTimeRemaining -= Time.deltaTime;
            Vector2 offset = Random.insideUnitCircle * _shakeMagnitude;
            _appliedShakeOffset = new Vector3(offset.x, offset.y, 0f);
            transform.position += _appliedShakeOffset;
        }
    }
}
