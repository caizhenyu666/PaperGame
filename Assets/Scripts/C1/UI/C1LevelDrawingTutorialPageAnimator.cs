using UnityEngine;

namespace PaperGame.C1
{
    public enum C1TutorialAnimationKind
    {
        PaperRise,
        Bob,
        Drift,
        Jump,
        Sway,
        Pulse
    }

    public sealed class C1LevelDrawingTutorialPageAnimator : MonoBehaviour
    {
        [SerializeField] private C1TutorialAnimationKind kind;

        private Vector3 originPosition;
        private Vector3 originScale;
        private Quaternion originRotation;
        private float elapsed;
        private bool captured;

        public void Configure(C1TutorialAnimationKind animationKind)
        {
            kind = animationKind;
            CapturePose();
        }

        private void OnEnable()
        {
            CapturePose();
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            ResetPose();
        }

        public void Tick(float deltaTime)
        {
            if (!captured) CapturePose();
            elapsed += Mathf.Max(0f, deltaTime);
            var wave = Mathf.Sin(elapsed * Mathf.PI * .8f);
            ResetTransformOnly();

            switch (kind)
            {
                case C1TutorialAnimationKind.PaperRise:
                    transform.localPosition = originPosition + Vector3.up * Mathf.Lerp(-12f, 0f, Mathf.Clamp01(elapsed * 2f));
                    break;
                case C1TutorialAnimationKind.Bob:
                    transform.localPosition = originPosition + Vector3.up * wave * 6f;
                    break;
                case C1TutorialAnimationKind.Drift:
                    transform.localPosition = originPosition + Vector3.right * wave * 8f;
                    break;
                case C1TutorialAnimationKind.Jump:
                    transform.localPosition = originPosition + Vector3.up * Mathf.Max(0f, wave) * 12f;
                    break;
                case C1TutorialAnimationKind.Sway:
                    transform.localRotation = originRotation * Quaternion.Euler(0f, 0f, wave * 4f);
                    break;
                case C1TutorialAnimationKind.Pulse:
                    transform.localScale = originScale * (1f + wave * .02f);
                    break;
            }
        }

        public void ResetPose()
        {
            if (!captured) return;
            elapsed = 0f;
            ResetTransformOnly();
        }

        private void CapturePose()
        {
            if (!captured)
            {
                originPosition = transform.localPosition;
                originScale = transform.localScale;
                originRotation = transform.localRotation;
                captured = true;
            }

            elapsed = 0f;
        }

        private void ResetTransformOnly()
        {
            transform.localPosition = originPosition;
            transform.localScale = originScale;
            transform.localRotation = originRotation;
        }
    }
}
