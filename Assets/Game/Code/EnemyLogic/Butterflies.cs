using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class Butterflies : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private List<Transform> waypoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waypointReachedDistance = 0.1f; // for test purpose
    [SerializeField] private bool loopPath = true;

    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer; // delete after player tag is static
    [SerializeField] private string playerTag = "Player"; // delete after player tag is static

    [Header("Damage")]
    [SerializeField] private int damageAmount = 1; // depends on enemy type

    private int currentWaypointIndex = 0;
    private bool movingForward = true;
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
            Debug.LogWarning("No waypoints assigned to butterfly enemy!");
        }

        // Check for Collider2D component
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("No Collider2D component found on Butterflies GameObject!");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogError("Collider2D component is not set as Trigger on Butterflies GameObject!");
        }
    }

    private void FixedUpdate()
    {
        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return; // no waypoints to move to
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector2 direction = (Vector2)(targetWaypoint.position - transform.position);
        float distanceToWaypoint = direction.magnitude;

        if (distanceToWaypoint <= waypointReachedDistance)
        {
            SelectNextWaypoint();
            // Recalculate after switching waypoint
            targetWaypoint = waypoints[currentWaypointIndex];
            direction = (Vector2)(targetWaypoint.position - transform.position);
        }

        direction.Normalize();
        rb.linearVelocity = direction * moveSpeed;
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            Debug.Log("Butterflies deal damage to player!");
                // Uncomment when player health is implemented:
                // var player = collision.GetComponent<PlayerHealth>();
                // if (player != null)
                // {
                //     player.TakeDamage(damageAmount);
                // }
        }
    }

    // Visual debugging for waypoints
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
