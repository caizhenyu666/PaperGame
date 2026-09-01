using UnityEngine;

namespace PaperGame.C1
{
    public static class C1LevelSpace
    {
        public const float PixelsPerUnit = 40f;
        public const float GroundThickness = 0.08f;

        public static Vector2 CanvasWorldSize(Vector2Int canvasPixelSize)
        {
            return new Vector2(canvasPixelSize.x, canvasPixelSize.y) / PixelsPerUnit;
        }

        public static Vector2 PixelToWorld(Vector2 pixel, Vector2Int canvasPixelSize)
        {
            return new Vector2(pixel.x, canvasPixelSize.y - pixel.y) / PixelsPerUnit;
        }
    }
}
