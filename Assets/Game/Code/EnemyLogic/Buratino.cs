using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class Buratino : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private List<Transform> waypoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float runAcceleration = 8f;
    [SerializeField] private float runDecceleration = 5f;
    [SerializeField] private float accelInAir = 0.5f;
    [SerializeField] private float deccelInAir = 0.5f;
    [SerializeField] private float waypointReachedDistance = 0.1f;
    [SerializeField] private bool loopPath = true;

    [Header("Gravity")]
    [SerializeField] private float gravityScale = 2f;
    [SerializeField] private float fallGravityMult = 2.5f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float raycastDistance = 1f;
    [SerializeField] private int raycastCount = 5;
    [SerializeField] private float raycastAngle = 60f;
    [SerializeField] private float avoidanceRate = 1f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private string playerTag = "Player";

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int damageAmount = 2;

    // State tracking
    private int currentWaypointIndex = 0;
    private bool movingForward = true;
    private Rigidbody2D rb;
    private Vector2 currentDirection;
    private Vector2 targetDirection;
    private float obstacleDetectionTimer = 0f;

    private Transform playerTransform;
    private float lastAttackTime;
    private float timeAfterAttack;

    private bool isOnGround;
    private float groundCheckRadius = 0.1f;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayer;

    private enum BuratinoState
    {
        Patrolling,
        Chasing,
        Attacking,
        Returning
    }
    private BuratinoState currentState = BuratinoState.Patrolling;

    public event Action OnAttack;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
    }

    private void Start()
    {
        if (waypoints == null || waypoints.Count == 0)
            Debug.LogWarning("No waypoints assigned to Buratino enemy!");

        if (waypoints != null && waypoints.Count > 0)
        {
            currentDirection = ((Vector2)waypoints[0].position - (Vector2)transform.position).normalized;
            targetDirection = currentDirection;
        }
        else
        {
            currentDirection = Vector2.right;
            targetDirection = Vector2.right;
        }

        lastAttackTime = -attackCooldown;
    }

    private void Update()
    {
        obstacleDetectionTimer += Time.deltaTime;
        DetectPlayer();
        TimersUpdate();
        UpdateState();
        DetectObstacles();
        GraduallyRotateDirection();
        GroundCheck();
        ApplyGravity();
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case BuratinoState.Patrolling:
                MoveAlongPathPhysics();
                break;
            case BuratinoState.Chasing:
                MoveTowardsPlayerPhysics();
                break;
            case BuratinoState.Attacking:
                ApplyDeceleration();
                break;
            case BuratinoState.Returning:
                MoveToClosestWaypointPhysics();
                break;
        }
    }

    private void TimersUpdate()
    {
        timeAfterAttack += Time.deltaTime;
    }

    private void UpdateState()
    {
        if (playerTransform != null)
        {
            float distToPlayer = Vector2.Distance(rb.position, playerTransform.position);
            if (distToPlayer <= attackRange)
            {
                currentState = BuratinoState.Attacking;
                AttackPlayer();
            }
            else if (distToPlayer <= detectionRange)
            {
                currentState = BuratinoState.Chasing;
            }
            else
            {
                // Player out of range, forget and return
                playerTransform = null;
                currentState = BuratinoState.Returning;
                FindClosestWaypoint();
            }
        }
        else
        {
            if (currentState == BuratinoState.Returning)
            {
                // If reached waypoint, resume patrol
                Vector2 toWaypoint = (waypoints[currentWaypointIndex].position - transform.position);
                if (toWaypoint.magnitude <= waypointReachedDistance)
                    currentState = BuratinoState.Patrolling;
            }
            else
            {
                currentState = BuratinoState.Patrolling;
            }
        }
    }

    private void DetectPlayer()
    {
        if (playerTransform != null)
            return;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRange);
        foreach (var col in colliders)
        {
            if (col.CompareTag(playerTag))
            {
                playerTransform = col.transform;
                break;
            }
        }
    }

    private void DetectObstacles()
    {
        if (obstacleDetectionTimer < 0.05f)
            return;

        obstacleDetectionTimer = 0f;

        Vector2 toTarget;
        if (currentState == BuratinoState.Chasing && playerTransform != null)
            toTarget = ((Vector2)playerTransform.position - rb.position).normalized;
        else if (currentState == BuratinoState.Returning || currentState == BuratinoState.Patrolling)
            toTarget = ((Vector2)waypoints[currentWaypointIndex].position - rb.position).normalized;
        else
            toTarget = currentDirection;

        RaycastHit2D directHit = Physics2D.Raycast(rb.position, toTarget, raycastDistance, obstacleLayer);
        Debug.DrawRay(rb.position, toTarget * raycastDistance, directHit ? Color.red : Color.green, 0.1f);

        if (!directHit)
        {
            targetDirection = toTarget;
            return;
        }

        float bestScore = float.MinValue;
        Vector2 bestDirection = currentDirection;

        for (int i = 0; i < raycastCount; i++)
        {
            float angle = -raycastAngle / 2 + raycastAngle * i / (raycastCount - 1);
            Vector2 testDir = Quaternion.Euler(0, 0, angle) * toTarget;
            RaycastHit2D hit = Physics2D.Raycast(rb.position, testDir, raycastDistance, obstacleLayer);
            Debug.DrawRay(rb.position, testDir * raycastDistance, hit ? Color.red : Color.green, 0.1f);

            if (!hit)
            {
                float score = Vector2.Dot(testDir, toTarget);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = testDir;
                }
            }
        }

        targetDirection = bestDirection.normalized;
    }

    private void GraduallyRotateDirection()
    {
        float angle = Vector2.SignedAngle(currentDirection, targetDirection);
        if (Mathf.Abs(angle) < 0.1f)
        {
            currentDirection = targetDirection;
            return;
        }
        float rotationThisFrame = Mathf.Clamp(angle, -avoidanceRate, avoidanceRate);
        currentDirection = Quaternion.Euler(0, 0, rotationThisFrame) * currentDirection;
        currentDirection.Normalize();
    }

    private void MoveAlongPathPhysics()
    {
        if (waypoints == null || waypoints.Count == 0)
            return;

        Vector2 currentPos = rb.position;
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector2 waypointPos = targetWaypoint.position;
        Vector2 directionToWaypoint = waypointPos - currentPos;
        float distanceToWaypoint = directionToWaypoint.magnitude;

        if (distanceToWaypoint <= waypointReachedDistance)
        {
            SelectNextWaypoint();
            return;
        }

        MovePhysics(targetDirection);
    }

    private void MoveTowardsPlayerPhysics()
    {
        if (playerTransform == null)
            return;

        Vector2 toPlayer = ((Vector2)playerTransform.position - rb.position);
        if (toPlayer.magnitude <= attackRange)
        {
            ApplyDeceleration();
            return;
        }

        MovePhysics(targetDirection);
    }

    private void MoveToClosestWaypointPhysics()
    {
        if (waypoints == null || waypoints.Count == 0)
            return;

        Vector2 currentPos = rb.position;
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector2 waypointPos = targetWaypoint.position;
        Vector2 directionToWaypoint = waypointPos - currentPos;
        float distanceToWaypoint = directionToWaypoint.magnitude;

        if (distanceToWaypoint <= waypointReachedDistance)
        {
            currentState = BuratinoState.Patrolling;
            return;
        }

        MovePhysics(targetDirection);
    }

    private void MovePhysics(Vector2 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.01f)
        {
            ApplyDeceleration();
            return;
        }

        float targetSpeed = moveDir.normalized.x * moveSpeed;
        float accelRate = isOnGround
            ? (Mathf.Abs(targetSpeed) > 0.01f ? runAcceleration : runDecceleration)
            : (Mathf.Abs(targetSpeed) > 0.01f ? runAcceleration * accelInAir : runDecceleration * deccelInAir);

        float speedDif = targetSpeed - rb.linearVelocity.x;
        float movement = speedDif * accelRate;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        // Flip sprite if needed
        if (targetSpeed != 0)
        {
            transform.localScale = new Vector3(targetSpeed < 0 ? -1 : 1, transform.localScale.y, transform.localScale.z);
        }
    }

    private void ApplyDeceleration()
    {
        float targetSpeed = 0f;
        float accelRate = isOnGround ? runDecceleration : runDecceleration * deccelInAir;
        float speedDif = targetSpeed - rb.linearVelocity.x;
        float movement = speedDif * accelRate;
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    private void ApplyGravity()
    {
        if (rb.linearVelocity.y < 0)
            rb.gravityScale = gravityScale * fallGravityMult;
        else
            rb.gravityScale = gravityScale;
    }

    private void GroundCheck()
    {
        if (groundCheckPoint != null)
            isOnGround = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
        else
            isOnGround = Physics2D.OverlapCircle(transform.position, groundCheckRadius, groundLayer);
    }

    private void AttackPlayer()
    {
        if (timeAfterAttack >= attackCooldown)
        {
            Debug.Log("Buratino attacks player!");
            // var playerHealth = playerTransform.GetComponent<PlayerHealth>();
            // if (playerHealth != null)
            // {
            //     playerHealth.TakeDamage(damageAmount);
            // }
            timeAfterAttack = 0;
            lastAttackTime = Time.time;
            OnAttack?.Invoke();
        }
    }

    private void SelectNextWaypoint()
    {
        if (loopPath)
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        else
        {
            if (movingForward)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Count - 1)
                    movingForward = false;
            }
            else
            {
                currentWaypointIndex--;
                if (currentWaypointIndex <= 0)
                    movingForward = true;
            }
        }
    }

    private void FindClosestWaypoint()
    {
        if (waypoints == null || waypoints.Count == 0)
            return;

        float minDist = float.MaxValue;
        int closestIndex = 0;
        Vector2 pos = rb.position;
        for (int i = 0; i < waypoints.Count; i++)
        {
            float dist = Vector2.Distance(pos, waypoints[i].position);
            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
        }
        currentWaypointIndex = closestIndex;
    }



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

        // Obstacle avoidance rays
        if (Application.isPlaying && isActiveAndEnabled)
        {
            for (int i = 0; i < raycastCount; i++)
            {
                float angle = -raycastAngle / 2 + raycastAngle * i / (raycastCount - 1);
                Vector2 direction = Quaternion.Euler(0, 0, angle) * currentDirection;
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, direction * raycastDistance);
            }
        }

        // Ground check
        Gizmos.color = Color.green;
        if (groundCheckPoint != null)
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        else
            Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
    }

}
