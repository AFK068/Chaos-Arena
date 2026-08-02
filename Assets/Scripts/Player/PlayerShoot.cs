using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : MonoBehaviour
{
    [SerializeField] private Transform shootPoint;
    [SerializeField] private ProjectileBase projectilePrefab;
    [SerializeField] private float fireRate = 0.3f;

    public ProjectileBase ProjectilePrefab
    {
        get => projectilePrefab;
        set => projectilePrefab = value;
    }

    // Input-action aim and touch auto-aim are separate sources. This prevents a
    // mobile source from changing keyboard/mouse/gamepad intent.
    private Vector2 manualShootDir = Vector2.zero;
    private Vector2 mobileAutoAimDir = Vector2.zero;
    private float fireTimer = 0f;
    private PlayerMovement playerMovement;
    private bool isShooting = false;
    private float _projectileDamageMultiplier = 1f;

    public bool IsShooting => isShooting;
    public float ProjectileDamageMultiplier => _projectileDamageMultiplier;

    public event System.Action<ProjectileBase> OnProjectileFired;

    private bool _fireRageActive;
    private Coroutine _fireRageCoroutine;

    public void ModifyFireRate(float multiplier) => fireRate *= multiplier;
    public void ModifyProjectileDamage(float multiplier) => _projectileDamageMultiplier *= Mathf.Max(0f, multiplier);

    public void ApplyFireRateBuff(float multiplier, float duration)
    {
        if (_fireRageCoroutine != null) StopCoroutine(_fireRageCoroutine);
        _fireRageCoroutine = StartCoroutine(FireRateRoutine(multiplier, duration));
    }

    private IEnumerator FireRateRoutine(float multiplier, float duration)
    {
        if (!_fireRageActive)
        {
            fireRate /= multiplier;
            _fireRageActive = true;
        }
        yield return new WaitForSeconds(duration);
        fireRate *= multiplier;
        _fireRageActive = false;
    }

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        SetShootDirection(context.ReadValue<Vector2>());
    }

    public void SetShootDirection(Vector2 direction)
    {
        manualShootDir = Vector2.ClampMagnitude(direction, 1f);
        RefreshShootingState();
    }

    /// <summary>Used only by the runtime mobile auto-aim controller.</summary>
    public void SetMobileAutoAimDirection(Vector2 direction)
    {
        mobileAutoAimDir = Vector2.ClampMagnitude(direction, 1f);
        RefreshShootingState();
    }

    private Vector2 ActiveShootDirection => manualShootDir.sqrMagnitude >= 0.01f
        ? manualShootDir
        : mobileAutoAimDir;

    private void RefreshShootingState()
    {
        isShooting = ActiveShootDirection.sqrMagnitude >= 0.01f;
    }

    void Update()
    {
        fireTimer += Time.deltaTime;
        var shootDirection = ActiveShootDirection;
        isShooting = shootDirection.sqrMagnitude >= 0.01f;
        if (isShooting && fireTimer >= fireRate)
        {
            Shoot(shootDirection);
            fireTimer = 0f;
        }
    }

    private void Shoot(Vector2 direction)
    {
        if (projectilePrefab == null || shootPoint == null) return;

        // Поворачиваем персонажа в сторону стрельбы
        playerMovement?.SetFacingDirection(direction);

        ProjectileBase projectile = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        projectile.ApplyDamageMultiplier(_projectileDamageMultiplier);
        projectile.Launch(direction);
        OnProjectileFired?.Invoke(projectile);
        AudioManager.Instance?.PlaySfx(SfxCue.Shoot);
    }
}
