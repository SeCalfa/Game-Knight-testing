using System.Collections.Generic;
using UnityEngine;

namespace Game.Code.EnemyLogic
{
    public class Butterflies : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private List<Transform> wayPoints;
        [SerializeField] private float moveSpeed;
        [Space]
        [SerializeField] private bool loopPath;

        private Rigidbody2D rb;
        
        private Transform currentTarget;
        private int currentTargetIndex;
        private Vector2 direction;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;

            foreach (var point in wayPoints)
            {
                point.parent = null;
            }

            currentTarget = wayPoints[currentTargetIndex];
        }

        private void FixedUpdate()
        {
            Movement();
        }

        private void Movement()
        {
            var newPosition = Vector2.MoveTowards(rb.position, currentTarget.position, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);

            if (Vector2.Distance(rb.position, currentTarget.position) < 0.1f)
            {
                // Next target index
                currentTargetIndex += 1;
                if (currentTargetIndex == wayPoints.Count)
                {
                    currentTargetIndex = 0;
                }

                // Next target
                currentTarget = wayPoints[currentTargetIndex];

                //Watch direction
                WatchDirection();
            }
        }

        private void WatchDirection()
        {
            direction = (Vector2)currentTarget.position - rb.position;
            transform.localScale = new Vector3((direction.x < 0) ? -1 : 1, 1, 1);
        }

        private void OnDrawGizmosSelected()
        {
            if (wayPoints is not { Count: > 0 }) return;
            
            Gizmos.color = Color.cyan;
            
            for (var i = 0; i < wayPoints.Count; i++)
            {
                if (wayPoints[i] == null) continue;
                
                var pos = wayPoints[i].position;
                Gizmos.DrawSphere(pos, 0.2f);
                
                if (loopPath)
                {
                    if (wayPoints[(i + 1) % wayPoints.Count] != null)
                        Gizmos.DrawLine(pos, wayPoints[(i + 1) % wayPoints.Count].position);
                }
                else
                {
                    if (i < wayPoints.Count - 1 && wayPoints[i + 1] != null)
                        Gizmos.DrawLine(pos, wayPoints[i + 1].position);
                }
            }
        }
    }
}
