using UnityEngine;

namespace PaperGame.C1
{
    public readonly struct C1CameraFrame
    {
        public C1CameraFrame(Vector2 center, float orthographicSize)
        {
            Center = center;
            OrthographicSize = orthographicSize;
        }

        public Vector2 Center { get; }
        public float OrthographicSize { get; }
    }

    public static class C1CameraFraming
    {
        public static C1CameraFrame Calculate(Bounds bounds, float aspect, float margin)
        {
            var safeAspect = aspect > 0.01f && !float.IsNaN(aspect) && !float.IsInfinity(aspect)
                ? aspect
                : 16f / 9f;
            var safeMargin = Mathf.Max(0f, margin);
            var verticalSize = bounds.extents.y + safeMargin;
            var horizontalSize = (bounds.extents.x + safeMargin) / safeAspect;
            return new C1CameraFrame(bounds.center, Mathf.Max(0.5f, verticalSize, horizontalSize));
        }
    }
}
