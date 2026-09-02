using UnityEngine;

namespace PaperGame.C1
{
    public sealed class C1MobileOrientationHint : MonoBehaviour
    {
        private GameObject overlay;
        private int lastWidth = -1;
        private int lastHeight = -1;
        private bool lastMobileLike;

        public void Configure(GameObject targetOverlay)
        {
            overlay = targetOverlay;
            EvaluateCurrentViewport();
        }

        public void Evaluate(int width, int height, bool mobileLike)
        {
            lastWidth = width;
            lastHeight = height;
            lastMobileLike = mobileLike;

            if (overlay != null)
            {
                overlay.SetActive(mobileLike && height > width);
            }
        }

        private void Update()
        {
            var mobileLike = Application.isMobilePlatform || Input.touchSupported;
            if (Screen.width != lastWidth || Screen.height != lastHeight || mobileLike != lastMobileLike)
            {
                Evaluate(Screen.width, Screen.height, mobileLike);
            }
        }

        private void EvaluateCurrentViewport()
        {
            Evaluate(Screen.width, Screen.height, Application.isMobilePlatform || Input.touchSupported);
        }
    }
}
