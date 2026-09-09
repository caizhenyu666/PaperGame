using System;
using UnityEngine;

namespace PaperGame.C1
{
    public enum C1CharacterAnimationState
    {
        Idle,
        Run,
        Jump
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class C1CharacterAnimator2D : MonoBehaviour
    {
        private const float RunThreshold = 0.05f;

        [SerializeField, Min(1f)] private float idleFramesPerSecond = 6f;
        [SerializeField, Min(1f)] private float runFramesPerSecond = 10f;
        [SerializeField, Min(1f)] private float jumpFramesPerSecond = 10f;

        private Sprite[] idleFrames = Array.Empty<Sprite>();
        private Sprite[] runFrames = Array.Empty<Sprite>();
        private Sprite[] jumpFrames = Array.Empty<Sprite>();
        private SpriteRenderer spriteRenderer;
        private C1PlayerController2D player;
        private Rigidbody2D body;
        private float frameElapsed;

        public C1CharacterAnimationState CurrentState { get; private set; } = C1CharacterAnimationState.Idle;
        public int CurrentFrameIndex { get; private set; }
        public int IdleFrameCount => idleFrames.Length;
        public int RunFrameCount => runFrames.Length;
        public int JumpFrameCount => jumpFrames.Length;

        private void Awake()
        {
            CacheComponents();
        }

        private void Update()
        {
            CacheComponents();
            if (player == null || body == null)
            {
                return;
            }

            Tick(Time.deltaTime, player.IsGrounded, body.velocity.x, player.IsCompleted);
        }

        public void Configure(Sprite[] idle, Sprite[] run, Sprite[] jump)
        {
            idleFrames = idle ?? Array.Empty<Sprite>();
            runFrames = run ?? Array.Empty<Sprite>();
            jumpFrames = jump ?? Array.Empty<Sprite>();
            CurrentState = C1CharacterAnimationState.Idle;
            CurrentFrameIndex = 0;
            frameElapsed = 0f;
            ApplyCurrentFrame();
        }

        public void ConfigureRemote(Sprite[] run, Sprite[] jump, float runFps, float jumpFps)
        {
            runFramesPerSecond = Mathf.Max(1, runFps);
            jumpFramesPerSecond = Mathf.Max(1, jumpFps);
            Configure(new[] { run[0] }, run, jump);
        }

        public C1CharacterAnimationState ResolveState(bool grounded, float horizontalSpeed, bool completed)
        {
            if (completed)
            {
                return C1CharacterAnimationState.Idle;
            }

            if (!grounded)
            {
                return C1CharacterAnimationState.Jump;
            }

            return Mathf.Abs(horizontalSpeed) > RunThreshold
                ? C1CharacterAnimationState.Run
                : C1CharacterAnimationState.Idle;
        }

        public void Tick(float deltaTime, bool grounded, float horizontalSpeed, bool completed)
        {
            CacheComponents();

            if (horizontalSpeed < -RunThreshold)
            {
                spriteRenderer.flipX = true;
            }
            else if (horizontalSpeed > RunThreshold)
            {
                spriteRenderer.flipX = false;
            }

            var nextState = ResolveState(grounded, horizontalSpeed, completed);
            if (nextState != CurrentState)
            {
                CurrentState = nextState;
                CurrentFrameIndex = 0;
                frameElapsed = 0f;
                ApplyCurrentFrame();
            }

            var frames = GetFrames(CurrentState);
            if (frames.Length == 0 || completed)
            {
                return;
            }

            var frameDuration = 1f / GetFramesPerSecond(CurrentState);
            frameElapsed += Mathf.Max(0f, deltaTime);
            while (frameElapsed >= frameDuration)
            {
                frameElapsed -= frameDuration;
                CurrentFrameIndex = CurrentState == C1CharacterAnimationState.Jump
                    ? Mathf.Min(CurrentFrameIndex + 1, frames.Length - 1)
                    : (CurrentFrameIndex + 1) % frames.Length;
                ApplyCurrentFrame();
            }
        }

        private void CacheComponents()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (player == null)
            {
                player = GetComponentInParent<C1PlayerController2D>();
            }

            if (body == null && player != null)
            {
                body = player.GetComponent<Rigidbody2D>();
            }
        }

        private Sprite[] GetFrames(C1CharacterAnimationState state)
        {
            switch (state)
            {
                case C1CharacterAnimationState.Run:
                    return runFrames;
                case C1CharacterAnimationState.Jump:
                    return jumpFrames;
                default:
                    return idleFrames;
            }
        }

        private float GetFramesPerSecond(C1CharacterAnimationState state)
        {
            switch (state)
            {
                case C1CharacterAnimationState.Run:
                    return runFramesPerSecond;
                case C1CharacterAnimationState.Jump:
                    return jumpFramesPerSecond;
                default:
                    return idleFramesPerSecond;
            }
        }

        private void ApplyCurrentFrame()
        {
            CacheComponents();
            var frames = GetFrames(CurrentState);
            if (frames.Length > 0)
            {
                CurrentFrameIndex = Mathf.Clamp(CurrentFrameIndex, 0, frames.Length - 1);
                spriteRenderer.sprite = frames[CurrentFrameIndex];
            }
        }
    }
}
