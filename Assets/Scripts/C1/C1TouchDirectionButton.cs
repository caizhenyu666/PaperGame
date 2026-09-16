using UnityEngine;
using UnityEngine.EventSystems;

namespace PaperGame.C1
{
    public sealed class C1TouchDirectionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private C1MobileControls controls;
        private bool movesLeft;

        public void Configure(C1MobileControls mobileControls, bool isLeft)
        {
            controls = mobileControls;
            movesLeft = isLeft;
        }

        public void OnPointerDown(PointerEventData eventData) => SetPressed(true);
        public void OnPointerUp(PointerEventData eventData) => SetPressed(false);
        public void OnPointerExit(PointerEventData eventData) => SetPressed(false);
        private void OnDisable() => SetPressed(false);

        private void SetPressed(bool pressed)
        {
            if (controls == null) return;
            if (movesLeft)
            {
                if (pressed) controls.PressLeft(); else controls.ReleaseLeft();
            }
            else if (pressed) controls.PressRight(); else controls.ReleaseRight();
        }
    }
}
