using UnityEngine;

namespace PaperGame.C1
{
    /// <summary>统一游戏角色的主体尺寸，不随动画帧变化，也不影响 UI 预览。</summary>
    public static class C1CharacterSizing
    {
        public const float Height = 1.6f;
        public const float Width = 1.1f;

        public static void Apply(Transform visual, BoxCollider2D collider, float referenceHeight)
        {
            collider.size = new Vector2(Width, Height);
            collider.offset = Vector2.zero;
            visual.localScale = Vector3.one * (Height / Mathf.Max(.01f, referenceHeight));
            visual.localPosition = new Vector3(0f, -Height / 2f, 0f);
        }
    }
}
