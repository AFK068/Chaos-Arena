using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private TrailRenderer trail;

    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private TrailRenderer _tr;
    private Animator _animator;

    private bool _canDash = true;
    private bool _isFacingRight = true;
    private bool _isDashing = false;

    public bool IsDashing => _isDashing;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if (_isDashing) { return; }

        _rb.linearVelocity = _moveInput * moveSpeed;

        // Animation
        _animator.SetFloat("Horizontal", _moveInput.x);
        _animator.SetFloat("Vertical", _moveInput.y);
        _animator.SetFloat("Speed", _moveInput.sqrMagnitude);

        // Flip 
        if (_moveInput.x > 0 && !_isFacingRight)
        {
            Flip();
        }
        else if (_moveInput.x < 0 && _isFacingRight)
        {
            Flip();
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    private void OnDash(InputAction.CallbackContext context)
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