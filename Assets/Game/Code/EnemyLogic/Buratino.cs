using UnityEngine;
using System.Collections.Generic;

public class Buratino : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private List<Transform> waypoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waypointReachedDistance = 0.1f;
    [SerializeField] private bool loopPath = true;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 5f; // personalize this
    [SerializeField] private LayerMask playerLayer; // delete after player tag is static
    [SerializeField] private string playerTag = "Player"; // to be deleted after player tag is static

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
        // Try to detect player
        DetectPlayer();

        if (isChasing && playerTransform != null)
        {
            ChasePlayer();
        }
        else
        {
            MoveAlongPath();
        }
    }

    private void DetectPlayer()
    {
        // Check for player in detection range
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);

        if (playerCollider != null && playerCollider.CompareTag(playerTag))
        {
            isChasing = true;
            playerTransform = playerCollider.transform;
        }
        else if (isChasing && playerTransform != null)
        {
            // Check if player is out of range
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer > detectionRange * 1.5f) // Add some hysteresis to prevent flipping
            {
                isChasing = false;
                playerTransform = null;
            }
        }
    }

    private void ChasePlayer()
    {
        if (playerTransform == null)
            return;

        // Calculate direction to player
        Vector2 directionToPlayer = playerTransform.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // Check if in attack range
        if (distanceToPlayer <= attackRange)
        {
            //AttackPlayer();
        }
        else
        {
            // Move towards player
            transform.position = Vector2.MoveTowards(
                transform.position,
                playerTransform.position,
                moveSpeed * Time.deltaTime
            );
        }
    }

    // apply damage to player on collision, to be uncommented when player health is implemented


    //private void AttackPlayer()
    //{
    //    // Check cooldown
    //    if (Time.time >= lastAttackTime + attackCooldown)
    //    {
    //        // Perform attack
    //        Debug.Log("Buratino attacks player!");

    //        // Deal damage to player
    //        if (playerTransform != null)
    //        {
    //            var playerHealth = playerTransform.GetComponent<PlayerHealth>();
    //            if (playerHealth != null)
    //            {
    //                playerHealth.TakeDamage(damageAmount);
    //            }
    //        }

    //        // Update attack time
    //        lastAttackTime = Time.time;
    //    }
    //}

    private void MoveAlongPath()
    {
        if (waypoints == null || waypoints.Count == 0)
            return;

        // Get current target waypoint
        Transform targetWaypoint = waypoints[currentWaypointIndex];

        // Calculate direction to the target
        Vector2 directionToWaypoint = targetWaypoint.position - transform.position;
        float distanceToWaypoint = directionToWaypoint.magnitude;

        // Flip sprite based on movement direction
        if (directionToWaypoint.x < 0)
        {
            transform.localScale = new Vector2(-Mathf.Abs(transform.localScale.x), transform.localScale.y);
        }
        else if (directionToWaypoint.x > 0)
        {
            transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x), transform.localScale.y);
        }

        // Move towards waypoint
        transform.position = Vector2.MoveTowards(
            transform.position,
            targetWaypoint.position,
            moveSpeed * Time.deltaTime
        );

        // Check if waypoint reached
        if (distanceToWaypoint <= waypointReachedDistance)
        {
            SelectNextWaypoint();
        }
    }


    private void SelectNextWaypoint()
    {
        if (loopPath)
        {
            // Simple loop around all waypoints
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }
        else
        {
            // Ping-pong between start and end
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
        // Detection - to be adjusted
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack - to be adjusted
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

    }
}
