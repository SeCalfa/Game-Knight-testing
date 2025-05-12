using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class Butterflies : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private List<Transform> waypoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waypointReachedDistance = 0.1f;
    [SerializeField] private bool loopPath = true;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float raycastDistance = 1f;
    [SerializeField] private int raycastCount = 5;
    [SerializeField] private float raycastAngle = 60f;
    [SerializeField] private float avoidanceRate = 1f;

    [Header("Damage")]
    [SerializeField] private string playerTag = "Player";

    private int currentWaypointIndex = 0;
    private bool movingForward = true;
    private Rigidbody2D rb;
    private Vector2 currentVelocity;
    private Vector2 currentDirection;
    private Vector2 targetDirection;
    private float obstacleDetectionTimer = 0f;

    public event Action OnDamageDealt;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    private void Start()
    {
        if (waypoints == null || waypoints.Count == 0)
            Debug.LogWarning("No waypoints assigned to butterfly enemy!");

        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
            Debug.LogError("No Collider2D component found on Butterflies GameObject!");
        else if (!collider.isTrigger)
        {
            Debug.LogWarning("Converting Collider2D to Trigger for kinematic body interactions!");
            collider.isTrigger = true;
        }

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
    }

    private void Update()
    {
        obstacleDetectionTimer += Time.deltaTime;
        DetectObstacles();
        GraduallyRotateDirection();
    }

    private void FixedUpdate()
    {
        MoveAlongPathKinematic();
    }

    private void DetectObstacles()
    {
        if (obstacleDetectionTimer < 0.05f)
            return;

        obstacleDetectionTimer = 0f;

        Vector2 toWaypoint = ((Vector2)waypoints[currentWaypointIndex].position - rb.position).normalized;
        RaycastHit2D directHit = Physics2D.Raycast(rb.position, toWaypoint, raycastDistance, obstacleLayer);
        Debug.DrawRay(rb.position, toWaypoint * raycastDistance, directHit ? Color.red : Color.green, 0.1f);

        if (!directHit)
        {
            targetDirection = toWaypoint;
            return;
        }

        float bestScore = float.MinValue;
        Vector2 bestDirection = currentDirection;

        for (int i = 0; i < raycastCount; i++)
        {
            float angle = -raycastAngle / 2 + raycastAngle * i / (raycastCount - 1);
            Vector2 testDir = Quaternion.Euler(0, 0, angle) * toWaypoint;
            RaycastHit2D hit = Physics2D.Raycast(rb.position, testDir, raycastDistance, obstacleLayer);
            Debug.DrawRay(rb.position, testDir * raycastDistance, hit ? Color.red : Color.green, 0.1f);

            if (!hit)
            {
                float score = Vector2.Dot(testDir, toWaypoint);
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

    private void MoveAlongPathKinematic()
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

        if (targetDirection.sqrMagnitude > 0.01f)
        {
            Vector2 targetVelocity = targetDirection.normalized * moveSpeed;
            currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * 5f);

            Vector2 nextPos = rb.position + currentVelocity * Time.fixedDeltaTime;
            Vector2 moveDir = (nextPos - rb.position).normalized;
            float moveDist = (nextPos - rb.position).magnitude;

            RaycastHit2D hit = Physics2D.Raycast(rb.position, moveDir, moveDist + 0.05f, obstacleLayer);
            if (hit.collider == null)
            {
                rb.MovePosition(nextPos);
            }
            else
            {
                Vector2 slideDir = Vector2.Perpendicular(hit.normal);
                if (Vector2.Dot(slideDir, targetDirection) < 0)
                    slideDir = -slideDir;
                Vector2 slideVelocity = slideDir.normalized * moveSpeed * 0.7f;
                rb.MovePosition(rb.position + slideVelocity * Time.fixedDeltaTime);
                currentVelocity = slideVelocity;
            }
        }
        else
        {
            currentVelocity = Vector2.zero;
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            Debug.Log("Butterflies deal damage to player!");
            OnDamageDealt?.Invoke();
        }
    }

    private void OnDrawGizmosSelected()
    {
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
                        Gizmos.DrawLine(pos, waypoints[i + 1].position);
                    else if (loopPath && waypoints[0] != null)
                        Gizmos.DrawLine(pos, waypoints[0].position);
                }
            }
        }
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
    }
}
