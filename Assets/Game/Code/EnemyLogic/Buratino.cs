using UnityEngine;
using System.Collections.Generic;
using System;

namespace Game.Code.Enemy
{
    public class Buratino : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float runAcceleration = 8f;
        [SerializeField] private float runDecceleration = 5f;
        [SerializeField] private float accelInAir = 0.5f;
        [SerializeField] private float deccelInAir = 0.5f;        [Header("Zone System")]
        [SerializeField] private BoxCollider2D patrolZone;
        [SerializeField] private float edgeOffset = 0.5f; // Відступ від краю зони        [Header("Gravity")]
        [SerializeField] private float gravityScale = 2f;
        [SerializeField] private float fallGravityMult = 2.5f;

        [Header("Detection")]
        [SerializeField] private string playerTag = "Player";

        [Header("Attack")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckRadius = 0.1f;
        
        [Header("Physics Protection")]
        [SerializeField] private float maxVerticalSpeed = 15f;        private Rigidbody2D rb;
        private Transform playerTransform;
        private float timeAfterAttack;
        private bool isOnGround;
        private Vector2 patrolDirection;
        private bool playerInZone = false; // Для відстеження входу гравця в зону

        private enum BuratinoState
        {
            Patrolling,
            Chasing,
            Attacking
        }
        private BuratinoState currentState = BuratinoState.Patrolling;

        public event Action OnAttack;        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravityScale;
            
            // Prevent rotation and ensure proper physics
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }        private void Start()
        {
            // Set initial patrol direction
            patrolDirection = UnityEngine.Random.value > 0.5f ? Vector2.right : Vector2.left;

            // Setup colliders properly - main collider should NOT be trigger for physics
            Collider2D[] colliders = GetComponents<Collider2D>();
            if (colliders.Length == 0)
            {
                Debug.LogError("No Collider2D component found on Buratino GameObject!");
            }
            else
            {
                // Keep main collider as solid for physics collision with ground
                colliders[0].isTrigger = false;
                Debug.Log("Main collider kept as solid for physics collision");
                
                // If there's a second collider, make it trigger for attack detection
                if (colliders.Length > 1)
                {
                    colliders[1].isTrigger = true;
                    Debug.Log("Second collider set as trigger for attack detection");
                }
            }
            
            // Check if patrol zone is assigned
            if (patrolZone == null)
            {
                Debug.LogError("Patrol Zone not assigned! Please assign a BoxCollider2D GameObject in the inspector.");
            }
            else
            {
                // Make sure patrol zone is set as trigger
                patrolZone.isTrigger = true;
            }
        }private void Update()
        {
            DetectPlayer();
            UpdateState();
            GroundCheck();
            TimersUpdate();
            ApplyGravity();
        }

        private void TimersUpdate()
        {
            timeAfterAttack += Time.deltaTime;
        }        private void FixedUpdate()
        {
            switch (currentState)
            {
                case BuratinoState.Patrolling:
                    PatrolInZone();
                    break;
                case BuratinoState.Chasing:
                    ChasePlayer();
                    break;
                case BuratinoState.Attacking:
                    ApplyDeceleration();
                    break;
            }
            
            // Обмеження вертикальної швидкості для захисту від потовкання
            if (Mathf.Abs(rb.linearVelocity.y) > maxVerticalSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Sign(rb.linearVelocity.y) * maxVerticalSpeed);
            }
        }        private void DetectPlayer()
        {
            if (patrolZone == null) return;
            
            // Перевіряємо чи гравець в зоні патрулювання
            bool playerCurrentlyInZone = false;
            Transform detectedPlayer = null;
            
            Collider2D[] collidersInZone = Physics2D.OverlapBoxAll(
                patrolZone.bounds.center, 
                patrolZone.bounds.size, 
                0f
            );

            foreach (var col in collidersInZone)
            {
                if (col.CompareTag(playerTag))
                {
                    playerCurrentlyInZone = true;
                    detectedPlayer = col.transform;
                    break;
                }
            }
            
            // Якщо гравець тільки що зайшов в зону - почати агро
            if (playerCurrentlyInZone && !playerInZone)
            {
                Debug.Log("Player entered patrol zone - starting aggro!");
                playerInZone = true;
                playerTransform = detectedPlayer;
            }
            // Якщо гравець покинув зону - зупинити агро
            else if (!playerCurrentlyInZone && playerInZone)
            {
                Debug.Log("Player left patrol zone - stopping aggro!");
                playerInZone = false;
                playerTransform = null;
            }
            // Якщо гравець в зоні - оновлюємо посилання
            else if (playerCurrentlyInZone && playerInZone)
            {
                playerTransform = detectedPlayer;
            }
        }        private void UpdateState()
        {
            if (playerTransform != null && playerInZone)
            {
                float distToPlayer = Vector2.Distance(rb.position, playerTransform.position);
                
                // Якщо гравець в зоні - відразу почати переслідування
                if (distToPlayer <= attackRange)
                {
                    currentState = BuratinoState.Attacking;
                    AttackPlayer();
                }
                else
                {
                    // Гравець в зоні, але далеко - переслідувати
                    currentState = BuratinoState.Chasing;
                }
            }
            else
            {
                // Немає гравця або він поза зоною - патрулювати
                currentState = BuratinoState.Patrolling;
            }
        }private void PatrolInZone()
        {
            if (patrolZone == null) return;

            // Get current position and zone bounds
            Vector2 currentPos = rb.position;
            Bounds zoneBounds = patrolZone.bounds;
            
            // Check if we're about to hit the zone boundaries
            Vector2 nextPos = currentPos + patrolDirection * moveSpeed * Time.fixedDeltaTime;
            
            // Check horizontal boundaries with offset
            if (nextPos.x <= zoneBounds.min.x + edgeOffset || nextPos.x >= zoneBounds.max.x - edgeOffset)
            {
                patrolDirection = -patrolDirection;
            }

            MovePhysics(patrolDirection);
        }private void ChasePlayer()
        {
            if (playerTransform == null) return;

            Vector2 directionToPlayer = ((Vector2)playerTransform.position - rb.position).normalized;
            MovePhysics(directionToPlayer);
        }private void MovePhysics(Vector2 moveDir)
        {
            if (moveDir.sqrMagnitude < 0.01f)
            {
                ApplyDeceleration();
                return;
            }

            var targetSpeed = moveDir.x * moveSpeed;
            targetSpeed = Mathf.Lerp(rb.linearVelocity.x, targetSpeed, 1f);

            var accelRate = isOnGround 
                ? (Mathf.Abs(targetSpeed) > 0.01f ? runAcceleration : runDecceleration)
                : (Mathf.Abs(targetSpeed) > 0.01f ? runAcceleration * accelInAir : runDecceleration * deccelInAir);

            // Handle conservation of momentum when changing direction in air
            if (Mathf.Abs(rb.linearVelocity.x) > Mathf.Abs(targetSpeed) &&
                Mathf.Approximately(Mathf.Sign(rb.linearVelocity.x), Mathf.Sign(targetSpeed)) &&
                Mathf.Abs(targetSpeed) > 0.01f &&
                !isOnGround)
            {
                accelRate = 0;
            }

            var speedDiff = targetSpeed - rb.linearVelocity.x;
            var movement = speedDiff * accelRate;

            rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
            
            // Handle sprite flipping like Character system
            transform.localScale = new Vector3(
                moveDir.x < 0 ? -1f : moveDir.x > 0 ? 1f : transform.localScale.x, 
                transform.localScale.y, 
                transform.localScale.z
            );
        }

        private void ApplyDeceleration()
        {
            var accelRate = isOnGround ? runDecceleration : runDecceleration * deccelInAir;
            var speedDiff = 0f - rb.linearVelocity.x;
            var movement = speedDiff * accelRate;
            rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
        }

        private void ApplyGravity()
        {
            switch (rb.linearVelocity.y)
            {
                // Fall
                case < 0:
                    SetGravityScale(gravityScale * fallGravityMult);
                    break;
                // Rise or neutral
                default:
                    SetGravityScale(gravityScale);
                    break;
            }
        }        private void GroundCheck()
        {
            Vector2 checkPosition;
            if (groundCheckPoint != null)
                checkPosition = groundCheckPoint.position;
            else
                checkPosition = (Vector2)transform.position + Vector2.down * 0.5f; // Offset down from center
            
            isOnGround = Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer);
            
            // Debug visualization
            Debug.DrawRay(checkPosition, Vector2.down * groundCheckRadius, isOnGround ? Color.green : Color.red);
        }

        private void SetGravityScale(float newGravityScale)
        {
            rb.gravityScale = newGravityScale;
        }        private void AttackPlayer()
        {
            if (timeAfterAttack >= attackCooldown)
            {
                timeAfterAttack = 0f;
                OnAttack?.Invoke();
                Debug.Log("Buratino attacks player!");
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag(playerTag))
            {
                Debug.Log("Buratino deals damage to player!");
                OnAttack?.Invoke();
            }
        }        private void OnDrawGizmosSelected()
        {
            // Draw patrol zone
            if (patrolZone != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(patrolZone.bounds.center, patrolZone.bounds.size);
            }

            // Draw attack range
            if (Application.isPlaying && isActiveAndEnabled)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f); // orange
                Gizmos.DrawWireSphere(transform.position, attackRange);
            }

            // Draw ground check
            if (groundCheckPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
            }
        }
    }
}