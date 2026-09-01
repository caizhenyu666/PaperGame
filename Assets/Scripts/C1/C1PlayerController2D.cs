using UnityEngine;

namespace PaperGame.C1
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class C1PlayerController2D : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float jumpSpeed = 9f;
        [SerializeField, Min(0.001f)] private float groundCheckDistance = 0.08f;

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private bool jumpConsumed;

        public float MoveSpeed => moveSpeed;
        public float JumpSpeed => jumpSpeed;
        public bool IsCompleted { get; private set; }
        public bool IsFallen { get; private set; }
        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            CacheComponents();
            body.freezeRotation = true;
        }

        private void Update()
        {
            ApplyHorizontalInput(Input.GetAxisRaw("Horizontal"));

            if (Input.GetKeyDown(KeyCode.Space))
            {
                TryJump();
            }
        }

        private void FixedUpdate()
        {
            RefreshGroundedState();
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

        public void RefreshGroundedState()
        {
            CacheComponents();
            var filter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = true,
                layerMask = Physics2D.GetLayerCollisionMask(gameObject.layer)
            };

            IsGrounded = bodyCollider.Cast(Vector2.down, filter, groundHits, groundCheckDistance) > 0 &&
                         body.velocity.y <= 0.01f;

            if (IsGrounded)
            {
                jumpConsumed = false;
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
                bodyCollider = GetComponent<Collider2D>();
            }
        }
    }
}
