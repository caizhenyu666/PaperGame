using UnityEngine;
using UnityEngine.EventSystems;

namespace PaperGame.C1
{
    public sealed class C1TouchJumpButton : MonoBehaviour, IPointerDownHandler
    {
        private C1MobileControls controls;
        public void Configure(C1MobileControls mobileControls) => controls = mobileControls;
        public void OnPointerDown(PointerEventData eventData) => controls?.PressJump();
    }
}
