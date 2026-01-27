using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private TrailRenderer trail;

    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private Vector2 _lastFacingDirection = Vector2.down;
    private Animator _animator;
    private PlayerShoot _playerShoot;

    private bool _canDash = true;
    private bool _isFacingRight = true;
    private bool _isDashing = false;

    public bool IsDashing => _isDashing;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _playerShoot = GetComponent<PlayerShoot>();
    }

    private void FixedUpdate()
    {
        if (_isDashing) { return; }

        _rb.linearVelocity = _moveInput * moveSpeed;

        // Если не стреляем и двигаемся - обновляем направление взгляда
        if (_playerShoot != null && !_playerShoot.IsShooting && _moveInput.sqrMagnitude > 0.01f)
        {
            _lastFacingDirection = _moveInput.normalized;
        }

        UpdateAnimation();
    }

    public void UpdateAnimation()
    {
        // Направление для Idle
        _animator.SetFloat("Horizontal", _lastFacingDirection.x);
        _animator.SetFloat("Vertical", _lastFacingDirection.y);

        Vector2 moveDirection = _moveInput;

        // Если движемся и смотрим в противоположные стороны - инвертируем движение
        if (_moveInput.sqrMagnitude > 0.01f && _lastFacingDirection.sqrMagnitude > 0.01f)
        {
            float dot = Vector2.Dot(_moveInput.normalized, _lastFacingDirection);

            if (dot < 0)
            {
                moveDirection = -_moveInput;
            }
        }

        _animator.SetFloat("MoveX", moveDirection.x);
        _animator.SetFloat("MoveY", moveDirection.y);
        _animator.SetFloat("Speed", _moveInput.sqrMagnitude);

        // Flip по направлению взгляда
        if (_lastFacingDirection.x > 0 && !_isFacingRight)
        {
            Flip();
        }
        else if (_lastFacingDirection.x < 0 && _isFacingRight)
        {
            Flip();
        }
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            _lastFacingDirection = direction.normalized;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.started)
            return;

        if (!_canDash || _moveInput == Vector2.zero)
            return;

        StartCoroutine(Dash());
    }

    private IEnumerator Dash()
    {
        _canDash = false;
        _isDashing = true;

        Vector2 dashDirection = _moveInput.normalized;

        trail.emitting = true;
        _rb.linearVelocity = dashDirection * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        trail.emitting = false;
        _isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        _canDash = true;
    }

    private void Flip()
    {
        _isFacingRight = !_isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }
}