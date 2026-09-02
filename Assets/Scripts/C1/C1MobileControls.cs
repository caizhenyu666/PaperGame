using UnityEngine;
using UnityEngine.EventSystems;

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

        private void OnDisable()
        {
            leftPressed = false;
            rightPressed = false;
            ApplyDirection();
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

    public sealed class C1TouchDirectionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private C1MobileControls controls;
        private bool movesLeft;

        public void Configure(C1MobileControls mobileControls, bool isLeft)
        {
            controls = mobileControls;
            movesLeft = isLeft;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SetPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetPressed(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetPressed(false);
        }

        private void OnDisable()
        {
            SetPressed(false);
        }

        private void SetPressed(bool pressed)
        {
            if (controls == null)
            {
                return;
            }

            if (movesLeft)
            {
                if (pressed)
                {
                    controls.PressLeft();
                }
                else
                {
                    controls.ReleaseLeft();
                }
            }
            else if (pressed)
            {
                controls.PressRight();
            }
            else
            {
                controls.ReleaseRight();
            }
        }
    }

    public sealed class C1TouchJumpButton : MonoBehaviour, IPointerDownHandler
    {
        private C1MobileControls controls;

        public void Configure(C1MobileControls mobileControls)
        {
            controls = mobileControls;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            controls?.PressJump();
        }
    }
}
