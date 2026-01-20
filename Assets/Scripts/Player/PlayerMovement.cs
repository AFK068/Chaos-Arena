using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f; 
     
    [SerializeField] private Rigidbody2D rb;  
    [SerializeField] private Animator animator;

    private Vector2 _movement;
    
    private void Awake()
     {
         if (rb == null) rb = GetComponent<Rigidbody2D>();
         if (animator == null) animator = GetComponent<Animator>();
     }
    
    void Update()
    {
        _movement.x = Input.GetAxisRaw("Horizontal");
        _movement.y = Input.GetAxisRaw("Vertical");
        
        animator.SetFloat("Horizontal", _movement.x);
        animator.SetFloat("Vertical", _movement.y);
        animator.SetFloat("Speed", _movement.sqrMagnitude);
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + _movement * (speed * Time.fixedDeltaTime));
    }
}