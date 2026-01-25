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

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private TrailRenderer tr;
    private Animator animator;

    private bool canDash = true;
    private bool isDashing = false;

    private bool isFacingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if (isDashing) { return; }

        rb.linearVelocity = moveInput * moveSpeed;

        // Animation
        animator.SetFloat("Horizontal", moveInput.x);
        animator.SetFloat("Vertical", moveInput.y);
        animator.SetFloat("Speed", moveInput.sqrMagnitude);

        // Flip 
        if (moveInput.x > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (moveInput.x < 0 && isFacingRight)
        {
            Flip();
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.started)
            return;

        if (!canDash || moveInput == Vector2.zero)
            return;

        StartCoroutine(Dash());
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        Vector2 dashDirection = moveInput.normalized;

        trail.emitting = true;
        rb.linearVelocity = dashDirection * dashSpeed;


        yield return new WaitForSeconds(dashDuration);

        trail.emitting = false;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }
}