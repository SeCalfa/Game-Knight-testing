using UnityEngine;
using System.Collections.Generic;
using System;

namespace Game.Code.Enemy
{
    public class Butterflies : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private List<Transform> waypoints;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float waypointReachedDistance = 0.1f;
        [SerializeField] private bool loopPath = true;
        [SerializeField] private float slowdownDistance = 1f; // Distance to start slowing down

        [Header("Damage")]
        [SerializeField] private string playerTag = "Player";

        private int currentWaypointIndex = 0;
        private bool movingForward = true;
        private Rigidbody2D rb;

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
            {
                Debug.LogWarning("No waypoints assigned to butterfly enemy!");
                // Disable the script if no waypoints are assigned to prevent errors
                enabled = false;
                return;
            }

            Collider2D collider = GetComponent<Collider2D>();
            if (collider == null)
                Debug.LogError("No Collider2D component found on Butterflies GameObject!");
            else if (!collider.isTrigger)
            {
                Debug.LogWarning("Converting Collider2D to Trigger for kinematic body interactions!");
                collider.isTrigger = true;
            }
        }

        private void Update()
        {
            // Removed obstacle detection and gradual rotation calls
        }

        private void FixedUpdate()
        {
            MoveAlongPathKinematic();
        }

        private void MoveAlongPathKinematic()
        {
            if (waypoints == null || waypoints.Count == 0 || !enabled)
                return;

            Transform targetWaypoint = waypoints[currentWaypointIndex];
            if (targetWaypoint == null)
            {
                Debug.LogWarning($"Waypoint {currentWaypointIndex} is null. Skipping movement.");
                SelectNextWaypoint(); // Try to move to the next waypoint
                return;
            }

            Vector2 currentPos = rb.position;
            Vector2 waypointPos = targetWaypoint.position;
            Vector2 directionToWaypoint = (waypointPos - currentPos).normalized;
            float distanceToWaypoint = Vector2.Distance(currentPos, waypointPos);

            if (distanceToWaypoint <= waypointReachedDistance)
            {
                SelectNextWaypoint();
                if (!enabled) return; 
                targetWaypoint = waypoints[currentWaypointIndex];
                if (targetWaypoint == null)
                {
                     Debug.LogWarning($"Next waypoint {currentWaypointIndex} is null after selection. Skipping movement.");
                     enabled = false; // Or handle differently, e.g., stop moving
                     return;
                }
                waypointPos = targetWaypoint.position;
                directionToWaypoint = (waypointPos - currentPos).normalized;
                distanceToWaypoint = Vector2.Distance(currentPos, waypointPos);
            }

            float currentMoveSpeed = moveSpeed;
            // Slow down if approaching the waypoint
            if (distanceToWaypoint < slowdownDistance)
            {
                
                currentMoveSpeed = Mathf.Lerp(moveSpeed * 0.2f, moveSpeed, distanceToWaypoint / slowdownDistance);
                currentMoveSpeed = Mathf.Max(currentMoveSpeed, moveSpeed * 0.1f); // Ensure a minimum speed
            }
            
            Vector2 velocity = directionToWaypoint * currentMoveSpeed;
            rb.MovePosition(currentPos + velocity * Time.fixedDeltaTime);
        }

        private void SelectNextWaypoint()
        {
            if (loopPath)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            }
            else
            {
                if (movingForward)
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex >= waypoints.Count) 
                    {
                        currentWaypointIndex = waypoints.Count - 1; // Stay at the last waypoint
                        movingForward = false;
                        // disable movement if it should stop at the end
                        // enabled = false; 
                        Debug.Log("Reached end of non-looping path (forward).");
                    }
                }
                else
                {
                    currentWaypointIndex--;
                    if (currentWaypointIndex < 0)
                    {
                        currentWaypointIndex = 0; // Stay at the first waypoint
                        movingForward = true;
                        // disable movement if it should stop at the end
                        // enabled = false;
                        Debug.Log("Reached end of non-looping path (backward).");
                    }
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
                        if (loopPath)
                        {
                            if (waypoints[(i + 1) % waypoints.Count] != null)
                                Gizmos.DrawLine(pos, waypoints[(i + 1) % waypoints.Count].position);
                        }
                        else
                        {
                            if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
                                Gizmos.DrawLine(pos, waypoints[i + 1].position);
                        }
                    }
                }
            }
        }
    }
}
