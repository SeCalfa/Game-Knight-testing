using System;
using Game.Code.AI;
using Game.Code.Effects;
using UnityEngine;

namespace Game.Code.Character
{
    public class CharacterMovement : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform groundPoint;
        [SerializeField] private Transform wallSlidePoint;
        [SerializeField] private LayerMask whatIsGround;
        [SerializeField] private LayerMask whatIsWall;
        [Space]
        [SerializeField] private CharacterParams characterParams;
        [Space]
        [SerializeField] private Transform hitPoint;
        [SerializeField] private LayerMask hittableLayer;
        
        private Rigidbody2D rb;

        private CharacterTimers characterTimers;
        private CharacterInput characterInput;
        private InputData inputData;

        private bool isOnGround, isOnWall;
        private bool wasInAir, wasSliding;
        private bool isDashing, isSliding;
        private bool nextHitActive;
        private float dashDuration;
        private int jumpsAmount;
        private Vector2 dashDirection;
        private AttackState attackState = AttackState.Default;

        public event Action OnJump;
        public event Action OnLand;

        public Animator GetAnimator => animator;
        
        private static readonly int Horizontal = Animator.StringToHash("Horizontal");
        private static readonly int JumpUp = Animator.StringToHash("JumpUp");
        private static readonly int JumpDown = Animator.StringToHash("JumpDown");
        private static readonly int JumpReset = Animator.StringToHash("JumpReset");
        private static readonly int DashStart = Animator.StringToHash("DashStart");
        private static readonly int DashEnd = Animator.StringToHash("DashEnd");
        private static readonly int SlideStart = Animator.StringToHash("SlideStart");
        private static readonly int SlideEnd = Animator.StringToHash("SlideEnd");
        private static readonly int Attack1 = Animator.StringToHash("Attack");

        private bool CanJump => isOnGround && rb.linearVelocityY < 0.1f;
        private bool CanAdditionalJump => !isOnGround && jumpsAmount > 0;
        private bool CanDash => dashDuration > 0f && !isDashing && characterTimers.timeAfterDash > characterParams.dashResetTime;
        private bool CanSlide => isOnWall && !isOnGround && Mathf.Abs(inputData.Horizontal) > 0 && rb.linearVelocityY < 0;
        private int DashDirection => transform.localScale.x < 0 ? -1 : 1;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            characterTimers = new CharacterTimers();
            characterInput = new CharacterInput();

            dashDuration = characterParams.dashDuration;
            jumpsAmount = characterParams.amountOfJumps;
        }

        private void Update()
        {
            inputData = characterInput.Update();

            Jump();
            Dash();
            Dashing();
            Attack();

            GroundPointCheck();
            WallSlidePointCheck();
            
            TimersUpdate();
            ApplyGravity();

            if (Input.GetKeyDown(KeyCode.R))
            {
                CameraShake.Instance.Shake(0.15f, 16, 0.8f, 17);
            }
        }

        private void FixedUpdate()
        {
            Movement();
            Slide();
        }

        private void TimersUpdate()
        {
            characterTimers.timeAfterDash += Time.deltaTime;
        }
        
        private void Movement()
        {
            if (attackState is AttackState.Attacking or AttackState.Hit)
            {
                return;
            }
            
            var targetSpeed = inputData.Horizontal * characterParams.speed;
            targetSpeed = Mathf.Lerp(rb.linearVelocity.x, targetSpeed, 1f);

            var accelRate = characterTimers.lastOnGroundTime > 0 ? Mathf.Abs(targetSpeed) > 0.01f ? characterParams.runAcceleration : characterParams.runDecceleration : Mathf.Abs(targetSpeed) > 0.01f ? characterParams.runAcceleration * characterParams.accelInAir : characterParams.runDecceleration * characterParams.deccelInAir;

            if (Mathf.Abs(rb.linearVelocity.x) > Mathf.Abs(targetSpeed) &&
                Mathf.Approximately(Mathf.Sign(rb.linearVelocity.x), Mathf.Sign(targetSpeed)) &&
                Mathf.Abs(targetSpeed) > 0.01f &&
                characterTimers.lastOnGroundTime < 0)
            {
                accelRate = 0;
            }

            var speedDif = targetSpeed - rb.linearVelocity.x;
            var movement = speedDif * accelRate;

            rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
            transform.localScale = new Vector3(inputData.Horizontal < 0 ? -1f : inputData.Horizontal > 0 ? 1f : transform.localScale.x, transform.localScale.y, 1f);
            animator.SetFloat(Horizontal, Mathf.Abs(inputData.Horizontal));
        }
        
        private void Jump()
        {
            if (!inputData.Jump || (!CanJump && !CanAdditionalJump) || attackState is AttackState.Attacking or AttackState.Hit)
            {
                return;
            }

            if (CanAdditionalJump || CanSlide)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            }

            if (isSliding)
            {
                jumpsAmount = 2;
            }
            else
            {
                jumpsAmount -= 1;
            }

            var force = characterParams.jumpForce - Mathf.Max(rb.linearVelocity.y, 0);
            rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
            OnJump?.Invoke();
        }

        private void Dash()
        {
            if (!inputData.Dash || !CanDash || attackState is AttackState.Attacking or AttackState.Hit)
            {
                return;
            }

            isDashing = true;
            dashDirection = GetDashDirection();
            animator.SetTrigger(DashStart);
        }

        private void Dashing()
        {
            if (isDashing)
            {
                dashDuration -= Time.deltaTime;
                rb.linearVelocity = new Vector2(dashDirection.x * characterParams.dashSpeed, dashDirection.y);
                
                if (dashDuration <= 0f)
                {
                    dashDuration = characterParams.dashDuration;
                    isDashing = false;
                    characterTimers.timeAfterDash = 0;
                    animator.SetTrigger(DashEnd);
                }
            }
        }

        private void Slide()
        {
            if (!CanSlide)
            {
                isSliding = false;
                return;
            }
            
            rb.linearVelocity = new Vector2(rb.linearVelocityX, -characterParams.wallSlideSpeed);
            isSliding = true;
        }

        private void Attack()
        {
            if (!inputData.Attack || attackState == AttackState.Attacking)
            {
                print("R1");
                return;
            }

            if (attackState == AttackState.Hit)
            {
                print("R2");
                nextHitActive = true;
                return;
            }

            print("Attack start");
            animator.SetTrigger(Attack1);
            attackState = AttackState.Attacking;
        }

        private void ApplyGravity()
        {
            if (isDashing)
            {
                SetGravityScale(0);
                return;
            }

            if (isSliding)
            {
                jumpsAmount = 2;
                wasSliding = true;
                SetGravityScale(characterParams.gravityScale);
                animator.SetTrigger(SlideStart);
                return;
            }

            if (attackState is AttackState.Attacking or AttackState.Hit)
            {
                print("+");
                rb.linearVelocity = new Vector2(0, 0);
                return;
            }

            if (wasSliding)
            {
                animator.SetTrigger(SlideEnd);
                wasSliding = false;
            }

            switch (rb.linearVelocity.y)
            {
                // Fall
                case < 0:
                    SetGravityScale(characterParams.gravityScale * characterParams.fallGravityMult);
                    animator.SetTrigger(JumpDown);
                    wasInAir = true;
                    break;
                // Rise
                case > 0:
                    SetGravityScale(characterParams.gravityScale);
                    animator.SetTrigger(JumpUp);
                    wasInAir = true;
                    break;
                // Land
                default:
                    SetGravityScale(characterParams.gravityScale);
                    animator.SetTrigger(JumpReset);
                    if (wasInAir && isOnGround)
                    {
                        wasInAir = false;
                        jumpsAmount = characterParams.amountOfJumps;
                        OnLand?.Invoke();
                    }
                    break;
            }
        }

        private void GroundPointCheck()
        {
            var overlapCircle = Physics2D.OverlapCircle(groundPoint.position, 0.1f, whatIsGround);
            isOnGround = overlapCircle;
        }

        private void WallSlidePointCheck()
        {
            var overlapCircle = Physics2D.OverlapCircle(wallSlidePoint.position, 0.1f, whatIsWall);
            isOnWall = overlapCircle;
        }

        private void SetGravityScale(float gravityScale)
        {
            rb.gravityScale = gravityScale;
        }

        private Vector2 GetDashDirection()
        {
            if (inputData.Horizontal == 0)
                return transform.right * DashDirection;
            
            return new Vector2(inputData.Horizontal, 0).normalized;
        }

        public void Hit()
        {
            var colliders = Physics2D.OverlapCircle(hitPoint.position, 0.5f, hittableLayer);
            
            if (colliders)
            {
                colliders.GetComponent<ImmortalEnemy>().SpawnImpact();
            }

            attackState = AttackState.Hit;
            CameraShake.Instance.Shake(0.15f, 16, 0.8f, 17);
        }

        public void AttackEnd()
        {
            if (nextHitActive)
            {
                animator.ResetTrigger(Attack1);
                animator.SetTrigger(Attack1);
                attackState = AttackState.Attacking;
                nextHitActive = false;
                
                return;
            }
            
            print("Attack end");
            attackState = AttackState.Default;
        }
    }
}
