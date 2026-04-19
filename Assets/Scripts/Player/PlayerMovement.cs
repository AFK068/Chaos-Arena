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
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private int dashCharges = 1;
    [SerializeField] private int maxDashCharges = 3;
    [SerializeField] private TrailRenderer trail;

    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private Vector2 _lastFacingDirection = Vector2.down;
    private Animator _animator;
    private PlayerShoot _playerShoot;

    private bool _isFacingRight = true;
    private bool _isDashing = false;

    private int _currentCharges;
    private float[] _chargesCooldown;
    private float _recoveryTimer;

    public bool IsDashing => _isDashing;
    public int MaxCharges => dashCharges;
    public int AbsoluteMaxCharges => maxDashCharges;
    public float[] ChargesCooldownNormalized => _chargesCooldown;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _playerShoot = GetComponent<PlayerShoot>();
        InitCharges(dashCharges);
    }

    private void InitCharges(int count)
    {
        _chargesCooldown = new float[count];
        for (int i = 0; i < count; i++)
            _chargesCooldown[i] = 1f;
        _currentCharges = count;
        _recoveryTimer = 0f;
    }

    public void RestoreDashCharge()
    {
        if (_currentCharges >= dashCharges) return;
        for (int i = 0; i < _chargesCooldown.Length; i++)
        {
            if (_chargesCooldown[i] < 1f)
            {
                _chargesCooldown[i] = 1f;
                _currentCharges++;
                _recoveryTimer = 0f;
                break;
            }
        }
    }

    public void AddDashCharge()
    {
        if (dashCharges >= maxDashCharges) return;
        dashCharges++;
        _currentCharges++;
        var newCooldowns = new float[dashCharges];
        for (int i = 0; i < _chargesCooldown.Length; i++)
            newCooldowns[i] = _chargesCooldown[i];
        newCooldowns[dashCharges - 1] = 1f;
        _chargesCooldown = newCooldowns;
    }

    private void Update()
    {
        if (dashCharges != _chargesCooldown.Length)
            InitCharges(dashCharges);

        // Находим самый левый пустой слот — он восстанавливается
        int leftmost = -1;
        for (int i = 0; i < _chargesCooldown.Length; i++)
        {
            if (_chargesCooldown[i] < 1f) { leftmost = i; break; }
        }

        if (leftmost >= 0)
        {
            _recoveryTimer += Time.deltaTime;
            _chargesCooldown[leftmost] = Mathf.Clamp01(_recoveryTimer / dashCooldown);

            if (_recoveryTimer >= dashCooldown)
            {
                _chargesCooldown[leftmost] = 1f;
                _currentCharges++;
                _recoveryTimer = 0f;
            }
        }
        else
        {
            _recoveryTimer = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (_isDashing) return;

        _rb.linearVelocity = _moveInput * moveSpeed;

        if (_playerShoot != null && !_playerShoot.IsShooting && _moveInput.sqrMagnitude > 0.01f)
            _lastFacingDirection = _moveInput.normalized;

        UpdateAnimation();
    }

    public void UpdateAnimation()
    {
        _animator.SetFloat("Horizontal", _lastFacingDirection.x);
        _animator.SetFloat("Vertical", _lastFacingDirection.y);

        Vector2 moveDirection = _moveInput;

        if (_moveInput.sqrMagnitude > 0.01f && _lastFacingDirection.sqrMagnitude > 0.01f)
        {
            float dot = Vector2.Dot(_moveInput.normalized, _lastFacingDirection);
            if (dot < 0)
                moveDirection = -_moveInput;
        }

        _animator.SetFloat("MoveX", moveDirection.x);
        _animator.SetFloat("MoveY", moveDirection.y);
        _animator.SetFloat("Speed", _moveInput.sqrMagnitude);

        if (_lastFacingDirection.x > 0 && !_isFacingRight) Flip();
        else if (_lastFacingDirection.x < 0 && _isFacingRight) Flip();
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
            _lastFacingDirection = direction.normalized;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (_currentCharges <= 0 || _moveInput == Vector2.zero) return;

        int slot = -1;
        for (int i = _chargesCooldown.Length - 1; i >= 0; i--)
        {
            if (_chargesCooldown[i] >= 1f) { slot = i; break; }
        }
        if (slot == -1) return;

        _currentCharges--;
        _chargesCooldown[slot] = 0f;

        // Сбрасываем таймер только если это первый потраченный заряд
        if (_currentCharges == dashCharges - 1)
            _recoveryTimer = 0f;

        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        _isDashing = true;
        trail.emitting = true;
        _rb.linearVelocity = _moveInput.normalized * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        trail.emitting = false;
        _isDashing = false;
    }

    private void Flip()
    {
        _isFacingRight = !_isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }
}
