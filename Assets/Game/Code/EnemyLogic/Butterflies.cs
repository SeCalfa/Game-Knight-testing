using UnityEngine;
using System.Collections.Generic;

public class Butterflies : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private List<Transform> waypoints; // points to move between
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waypointReachedDistance = 0.1f; // Here for test purpose, delete later
    [SerializeField] private bool loopPath = true; 

    [Header("Damage")]
    [SerializeField] private int damageAmount = 1; // depends on enemy type
    [SerializeField] private string playerTag = "Player"; // to be deleted after player tag is static

    private int currentWaypointIndex = 0;
    private bool movingForward = true;

    private void Start()
    {
        // Check waypoints
        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogWarning("No waypoints assigned to butterfly enemy!");
        }
    }

    private void Update()
    {
        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (waypoints == null || waypoints.Count == 0)
            return; // no waypoints to move to

        // Get current target waypoint
        Transform targetWaypoint = waypoints[currentWaypointIndex];

        // Calculate direction to the target
        Vector2 directionToWaypoint = targetWaypoint.position - transform.gameObject.transform.position;
        float distanceToWaypoint = directionToWaypoint.magnitude;

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

    private void SelectNextWaypoint() // not sure if this is the best way to loop
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
    // apply damage to player on collision, to be uncommented when player health is implemented
    
    
    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    if (collision.CompareTag(playerTag))
    //    {
    //        // Apply damage to player
    //        var player = collision.GetComponent<PlayerHealth>(); // Replace with your player health component name
    //        if (player != null)
    //        {
    //            player.TakeDamage(damageAmount);
    //        }
    //    }
    //}
}
