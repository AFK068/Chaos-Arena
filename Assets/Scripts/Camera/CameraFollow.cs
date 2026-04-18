using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float panSpeed = 6f;

    private bool _transitioning;
    private Vector3 _target;

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
        if (!_transitioning) return;
        transform.position = Vector3.Lerp(transform.position, _target, panSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, _target) < 0.05f)
        {
            transform.position = _target;
            _transitioning = false;
        }
    }
}
