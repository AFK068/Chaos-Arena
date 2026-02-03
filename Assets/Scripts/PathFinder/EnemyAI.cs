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
    
    private AIPath aiPath;
    private Vector3 patrolCenter; 
    private Vector3 spawnPoint;
    private Vector3 currentPatrolPoint;
    private bool isChasing = false;
    
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
                aiPath.maxSpeed = chaseSpeed;
            }
            aiPath.destination = player.position;
        }
        else
        {
            if (isChasing)
            {
                isChasing = false;
                aiPath.maxSpeed = patrolSpeed;
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
}