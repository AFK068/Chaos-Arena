using UnityEngine;
using Pathfinding;

public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    public float aggroRange = 10f;
    public Transform player;

    [Header("Speed")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;

    [Header("Patrol")]
    public bool canLeavePatrolArea = false;
    public float patrolRadius = 5f;

    [Header("Contact Damage")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float contactDamageCooldown = 0.8f;

    private AIPath aiPath;
    private Vector3 patrolCenter;
    private Vector3 spawnPoint;
    private Vector3 currentPatrolPoint;
    private bool isChasing = false;
    private float _nextDamageTime;
    private float _speedMultiplier = 1f;

    void Start()
    {
        aiPath = GetComponent<AIPath>();
        spawnPoint = transform.position;
        aiPath.maxSpeed = patrolSpeed;

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        UpdatePatrolCenter();
        SetNewPatrolPoint();
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= aggroRange)
        {
            if (!isChasing)
            {
                isChasing = true;
                aiPath.maxSpeed = chaseSpeed * _speedMultiplier;
            }
            aiPath.destination = player.position;
        }
        else
        {
            if (isChasing)
            {
                isChasing = false;
                aiPath.maxSpeed = patrolSpeed * _speedMultiplier;
                UpdatePatrolCenter();
                SetNewPatrolPoint();
            }

            if (Vector3.Distance(transform.position, currentPatrolPoint) < 0.5f)
            {
                UpdatePatrolCenter();
                SetNewPatrolPoint();
            }

            aiPath.destination = currentPatrolPoint;
        }
    }

    void UpdatePatrolCenter()
    {
        if (canLeavePatrolArea)
        {
            // Патруль от текущей позиции (может уходить дальше)
            patrolCenter = transform.position;
        }
        else
        {
            // Патруль от точки спавна (ходит вокруг одного места)
            patrolCenter = spawnPoint;
        }
    }

    void SetNewPatrolPoint()
    {
        int attempts = 0;
        bool foundValidPoint = false;

        while (!foundValidPoint && attempts < 10)
        {
            Vector2 randomDirection = Random.insideUnitCircle * patrolRadius;
            Vector3 targetPoint = patrolCenter + new Vector3(randomDirection.x, randomDirection.y, 0);

            GraphNode node = AstarPath.active.GetNearest(targetPoint).node;

            if (node != null && node.Walkable)
            {
                currentPatrolPoint = (Vector3)node.position;
                foundValidPoint = true;
            }

            attempts++;
        }

        if (!foundValidPoint)
        {
            currentPatrolPoint = patrolCenter;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDealContactDamage(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDealContactDamage(other);
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Max(multiplier, 0f);
        aiPath.maxSpeed = (isChasing ? chaseSpeed : patrolSpeed) * _speedMultiplier;
    }

    private void TryDealContactDamage(Collider2D other)
    {
        if (other == null || Time.time < _nextDamageTime)
        {
            return;
        }

        if (!other.TryGetComponent<PlayerHealth>(out var playerHealth))
        {
            return;
        }

        playerHealth.TakeDamage(contactDamage);
        _nextDamageTime = Time.time + contactDamageCooldown;
    }

}
