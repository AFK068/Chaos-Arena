using UnityEngine;
using Pathfinding;

public class AIAnimationController : MonoBehaviour
{
    private Animator animator;
    private AIPath aiPath;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        aiPath = GetComponent<AIPath>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float currentSpeed = aiPath.velocity.magnitude;
        animator.SetFloat("Speed", currentSpeed);
        
        if (aiPath.velocity.x != 0)
        {
            spriteRenderer.flipX = aiPath.velocity.x < 0;
        }
    }
}