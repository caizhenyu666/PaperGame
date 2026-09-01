using System;
using UnityEngine;

namespace PaperGame.C1
{
    [Serializable]
    public sealed class C1PlatformDefinition
    {
        [field: SerializeField]
        public Vector2 Start { get; set; }

        [field: SerializeField]
        public Vector2 End { get; set; }

        public Vector2 Position => (Start + End) * 0.5f;
        public Vector2 Size => new Vector2(Mathf.Abs(End.x - Start.x), Mathf.Max(1f, Mathf.Abs(End.y - Start.y)));

        public C1PlatformDefinition(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }
    }

    [Serializable]
    public sealed class C1LevelDefinition
    {
        [field: SerializeField]
        public string BackgroundResourcePath { get; set; } = string.Empty;

        [field: SerializeField]
        public Vector2Int CanvasPixelSize { get; set; }

        [field: SerializeField]
        public C1PlatformDefinition[] Platforms { get; set; } = Array.Empty<C1PlatformDefinition>();

        [field: SerializeField]
        public Vector2 PlayerStart { get; set; }

        [field: SerializeField]
        public Rect GoalRegion { get; set; }

        public Vector2 GoalPosition => GoalRegion.center;
        public Vector2 CanvasSize => CanvasPixelSize;
        public Vector2 GoalPoleSize => GoalRegion.size;
        public Vector2 GoalFlagSize => GoalRegion.size;

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(BackgroundResourcePath))
            {
                error = "Level requires a background resource path.";
                return false;
            }

            if (CanvasPixelSize.x <= 0 || CanvasPixelSize.y <= 0)
            {
                error = "Canvas pixel size must be positive.";
                return false;
            }

            if (Platforms == null || Platforms.Length == 0)
            {
                error = "Level requires at least one platform.";
                return false;
            }

            if (!IsFinite(PlayerStart) || !IsInsideCanvas(PlayerStart))
            {
                error = "Player coordinates must be finite and inside the canvas.";
                return false;
            }

            if (!IsFinite(GoalRegion.position) || !IsFinite(GoalRegion.size) || GoalRegion.width <= 0f || GoalRegion.height <= 0f || GoalRegion.xMin < 0f || GoalRegion.yMin < 0f || GoalRegion.xMax > CanvasPixelSize.x || GoalRegion.yMax > CanvasPixelSize.y)
            {
                error = "Goal region must be finite, positive and inside the canvas.";
                return false;
            }

            foreach (var platform in Platforms)
            {
                if (platform == null || !IsFinite(platform.Start) || !IsFinite(platform.End))
                {
                    error = "Every platform coordinate must be finite.";
                    return false;
                }

                if (!IsInsideCanvas(platform.Start) || !IsInsideCanvas(platform.End))
                {
                    error = "Every platform endpoint must be inside the canvas.";
                    return false;
                }

                if ((platform.End - platform.Start).sqrMagnitude <= Mathf.Epsilon)
                {
                    error = "Every platform must have a positive line length.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private bool IsInsideCanvas(Vector2 value)
        {
            return value.x >= 0f && value.y >= 0f && value.x <= CanvasPixelSize.x && value.y <= CanvasPixelSize.y;
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
