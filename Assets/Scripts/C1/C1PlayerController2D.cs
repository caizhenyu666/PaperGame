using UnityEngine;

namespace PaperGame.C1
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class C1PlayerController2D : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float jumpSpeed = 9f;
        [SerializeField, Min(0.001f)] private float groundCheckDistance = 0.1f;
        [SerializeField, Min(0.001f)] private float animationGroundCheckDistance = 2f;
        private const float PinToGround = -0.1f;

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
        private readonly Collider2D[] overlapResults = new Collider2D[8];
        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private PhysicsMaterial2D movementMaterial;
        private bool jumpConsumed;

        public float MoveSpeed => moveSpeed;
        public float JumpSpeed => jumpSpeed;
        public bool IsCompleted { get; private set; }
        public bool IsFallen { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsGroundedForAnimation { get; private set; }
        public float TouchHorizontalInput { get; private set; }

        private void Awake()
        {
            CacheComponents();
            body.freezeRotation = true;
        }

        private void OnDestroy()
        {
            if (movementMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(movementMaterial);
                }
                else
                {
                    DestroyImmediate(movementMaterial);
                }
            }
        }

        private void Update()
        {
            ApplyHorizontalInput(ResolveHorizontalInput(Input.GetAxisRaw("Horizontal")));

            if (Input.GetKeyDown(KeyCode.Space))
            {
                TryJump();
            }
        }

        private void FixedUpdate()
        {
            RefreshGroundedState();
            PinToGroundIfGrounded();
        }

        public void ApplyHorizontalInput(float input)
        {
            CacheComponents();
            if (IsCompleted || IsFallen)
            {
                return;
            }

            var clampedInput = Mathf.Clamp(input, -1f, 1f);
            body.velocity = new Vector2(clampedInput * moveSpeed, body.velocity.y);
        }

        public void SetTouchHorizontalInput(float input)
        {
            TouchHorizontalInput = Mathf.Clamp(input, -1f, 1f);
        }

        public float ResolveHorizontalInput(float keyboardInput)
        {
            return Mathf.Abs(keyboardInput) > 0.001f
                ? Mathf.Clamp(keyboardInput, -1f, 1f)
                : TouchHorizontalInput;
        }

        public bool TryJump()
        {
            CacheComponents();
            if (IsCompleted || IsFallen || jumpConsumed)
            {
                return false;
            }

            RefreshGroundedState();

            if (!IsGrounded)
            {
                return false;
            }

            body.velocity = new Vector2(body.velocity.x, jumpSpeed);
            jumpConsumed = true;
            IsGrounded = false;
            return true;
        }

        public void SetJumpHeight(float height)
        {
            CacheComponents();
            var downwardGravity = Mathf.Abs(Physics2D.gravity.y) * body.gravityScale;
            jumpSpeed = C1JumpPhysics.SpeedForHeight(height, downwardGravity);
        }

        private bool wasGrounded;

        public void RefreshGroundedState()
        {
            CacheComponents();
            var filter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = true,
                layerMask = Physics2D.GetLayerCollisionMask(gameObject.layer),
                useNormalAngle = true,
                minNormalAngle = 45f,
                maxNormalAngle = 135f
            };

            var colSize = bodyCollider.bounds.size;
            var checkCenter = (Vector2)transform.position + (Vector2)bodyCollider.offset
                              - Vector2.up * (colSize.y * 0.5f - 0.05f);
            var checkSize = new Vector2(colSize.x * 0.8f, 0.3f);
            IsGroundedForAnimation = Physics2D.OverlapBoxNonAlloc(checkCenter, checkSize, 0f, overlapResults) > 0;

            IsGrounded = bodyCollider.Cast(Vector2.down, filter, groundHits, groundCheckDistance) > 0;

            if (IsGrounded && !wasGrounded)
            {
                jumpConsumed = false;
            }
            wasGrounded = IsGrounded;
        }

        private void PinToGroundIfGrounded()
        {
            if (IsGrounded && !jumpConsumed)
            {
                body.velocity = new Vector2(body.velocity.x, Mathf.Min(body.velocity.y, PinToGround));
            }
        }

        public void Complete()
        {
            CacheComponents();
            if (IsCompleted)
            {
                return;
            }

            IsCompleted = true;
            TouchHorizontalInput = 0f;
            body.velocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Static;
        }

        public void Fall()
        {
            CacheComponents();
            if (IsCompleted || IsFallen)
            {
                return;
            }

            IsFallen = true;
            TouchHorizontalInput = 0f;
            body.velocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Static;
        }

        private void CacheComponents()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<BoxCollider2D>();
            }

            if (movementMaterial == null)
            {
                // Prevent horizontal movement from generating friction that holds us on walls.
                movementMaterial = new PhysicsMaterial2D("Player Movement")
                {
                    friction = 0f,
                    bounciness = 0f
                };
                bodyCollider.sharedMaterial = movementMaterial;
            }
        }
    }
}
