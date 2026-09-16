using UnityEngine;

namespace PaperGame.C1
{
    public sealed class C1MobileControls : MonoBehaviour
    {
        private C1PlayerController2D player;
        private bool leftPressed;
        private bool rightPressed;

        public void Configure(C1PlayerController2D targetPlayer)
        {
            player = targetPlayer;
            ApplyDirection();
        }

        public void PressLeft()
        {
            leftPressed = true;
            ApplyDirection();
        }

        public void ReleaseLeft()
        {
            leftPressed = false;
            ApplyDirection();
        }

        public void PressRight()
        {
            rightPressed = true;
            ApplyDirection();
        }

        public void ReleaseRight()
        {
            rightPressed = false;
            ApplyDirection();
        }

        public bool PressJump()
        {
            return player != null && player.TryJump();
        }

        public void ClearInput()
        {
            leftPressed = false;
            rightPressed = false;
            ApplyDirection();
        }

        private void OnDisable()
        {
            ClearInput();
        }

        private void ApplyDirection()
        {
            if (player == null)
            {
                return;
            }

            player.SetTouchHorizontalInput((leftPressed ? -1f : 0f) + (rightPressed ? 1f : 0f));
        }
    }

}
