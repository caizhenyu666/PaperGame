using UnityEngine;

namespace PaperGame.C1
{
    public static class C1BuiltInCharacters
    {
        public const string Green = "default";
        public const string Chick = "default-chick";
        public static bool IsBuiltIn(string id) => id == Green || id == Chick;
        public static Sprite Sprite(string id) => C1CharacterScreenLayout.Art(id == Chick ? "chick" : "green");
        public static string Name(string id) => id == Chick ? "披风小鸡" : "独眼小绿";

        public static void Apply(C1PlayerController2D player, string id)
        {
            if (player == null || !IsBuiltIn(id)) return;
            var visual = player.transform.Find("Character Visual");
            var sprite = Sprite(id);
            if (visual == null || sprite == null) return;
            var run = id == Green ? C1CharacterScreenLayout.Art("green-run") : sprite;
            var jump = id == Green ? C1CharacterScreenLayout.Art("green-jump") : sprite;
            visual.GetComponent<C1CharacterAnimator2D>().Configure(new[] { sprite }, new[] { run ?? sprite }, new[] { jump ?? sprite });
            visual.GetComponent<SpriteRenderer>().color = Color.white;
            visual.localScale = Vector3.one * (2.2f / Mathf.Max(.01f, sprite.bounds.size.y));
            visual.localPosition = Vector3.zero;
        }
    }
}
