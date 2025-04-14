using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class Buratino : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private List<Transform> waypoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waypointReachedDistance = 0.1f;
    [SerializeField] private bool loopPath = true;
    [SerializeField] private float decelerationRate = 5f; // deceleration rate for smooth stopping

    [Header("Detection")]
    [SerializeField] private float detectionRange = 5f; 
    [SerializeField] private LayerMask playerLayer; // delete after player tag is static
    [SerializeField] private string playerTag = "Player"; // delete after player tag is static

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int damageAmount = 2;

    // State tracking
    private int currentWaypointIndex = 0;
    private bool movingForward = true;
    private bool isChasing = false;
    private Transform playerTransform;
    private float lastAttackTime;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Use Dynamic body type with gravity disabled for physics-based movement - to be changed
        rb.gravityScale = 0f;
    }

    private void Start()
    {
        // Check waypoints
        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogWarning("No waypoints assigned to Buratino enemy!");
        }

        // Initialize attack cooldown
        lastAttackTime = -attackCooldown;
    }

    private void Update()
    {
        DetectPlayer();
    }

    private void FixedUpdate()
    {
        if (isChasing && playerTransform != null)
        {
            ChasePlayerPhysics();
        }
        else
        {
            MoveAlongPathPhysics();
        }
    }

    private void DetectPlayer()
    {
        // Check for player in detection range
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);

        if (playerCollider != null && playerCollider.CompareTag(playerTag))
        {
            Debug.Log($"playerLayer: {playerLayer.value}, Player found: {playerCollider != null}");
            isChasing = true;
            playerTransform = playerCollider.transform;
        }
        else if (isChasing && playerTransform != null)
        {
            // Check if player is out of range.
            float distanceToPlayer = Vector2.Distance((Vector2)transform.position, (Vector2)playerTransform.position);
            if (distanceToPlayer > detectionRange * 1.5f) // ensure the player is not abusing the detection by 1.5f
            {
                isChasing = false;
                playerTransform = null;
            }
        }
    }

    private void ChasePlayerPhysics()
    {
        if (playerTransform == null)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * decelerationRate);
            return;
        }

        Vector2 directionToPlayer = (Vector2)(playerTransform.position - transform.position);
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer <= attackRange)
        {
            rb.linearVelocity = Vector2.zero;
            AttackPlayer();
        }
        else
        {
            directionToPlayer.Normalize();
            rb.linearVelocity = directionToPlayer * moveSpeed;
        }
    }

    private void MoveAlongPathPhysics()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector2 direction = (Vector2)(targetWaypoint.position - transform.position);
        float distanceToWaypoint = direction.magnitude;

        if (distanceToWaypoint <= waypointReachedDistance)
        {
            SelectNextWaypoint();
            targetWaypoint = waypoints[currentWaypointIndex];
            direction = (Vector2)(targetWaypoint.position - transform.position);
        }
        direction.Normalize();
        rb.linearVelocity = direction * moveSpeed;
    }

    private void AttackPlayer()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Debug.Log("Buratino attacks player!");
            // Deal damage to player when player health is implemented.
            
            // var playerHealth = playerTransform.GetComponent<PlayerHealth>();
            // if (playerHealth != null)
            // {
            //     playerHealth.TakeDamage(damageAmount);
            // }
            lastAttackTime = Time.time;
        }
    }

    private void SelectNextWaypoint()
    {
        if (loopPath)
        {
            // Loop around all waypoints
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }
        else
        {
            // Ping-pong between first and last waypoints
            if (movingForward)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Count - 1)
                {
                    movingForward = false;
                }
            }
            else
            {
                currentWaypointIndex--;
                if (currentWaypointIndex <= 0)
                {
                    movingForward = true;
                }
            }
        }
    }

    // Visual debugging
    private void OnDrawGizmosSelected()
    {
        // Detection
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack 
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Waypoints
        if (waypoints != null && waypoints.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null)
                {
                    Vector3 pos = waypoints[i].position;
                    Gizmos.DrawSphere(pos, 0.2f);

                    if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(pos, waypoints[i + 1].position);
                    }
                    else if (loopPath && waypoints[0] != null)
                    {
                        Gizmos.DrawLine(pos, waypoints[0].position);
                    }
                }
            }
        }
    }
}
