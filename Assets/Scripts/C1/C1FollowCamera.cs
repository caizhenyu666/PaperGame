using UnityEngine;

namespace PaperGame.C1
{
    public sealed class C1FollowCamera : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float smoothTime = 0.2f;
        [SerializeField] private float fixedY;

        private Transform target;
        private float velocityX;

        public void SetTarget(Transform value)
        {
            target = value;
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = new Vector3(target.position.x, fixedY, -10f);
            velocityX = 0f;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var position = transform.position;
            position.x = Mathf.SmoothDamp(position.x, target.position.x, ref velocityX, smoothTime);
            position.y = fixedY;
            position.z = -10f;
            transform.position = position;
        }
    }
}
