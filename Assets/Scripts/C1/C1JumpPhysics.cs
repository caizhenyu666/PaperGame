using UnityEngine;

namespace PaperGame.C1
{
    public static class C1JumpPhysics
    {
        public static float SpeedForHeight(float height, float downwardGravity)
        {
            return Mathf.Sqrt(2f * Mathf.Max(0f, height) * Mathf.Max(0f, downwardGravity));
        }
    }
}
