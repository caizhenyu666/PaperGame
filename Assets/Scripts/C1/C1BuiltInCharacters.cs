using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaperGame.C1
{
    public static class C1BuiltInCharacters
    {
        public const string Green = "default";
        public const string Chick = "default-chick";
        public static bool IsBuiltIn(string id) => id == Green || id == Chick;
        private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();
        public static Sprite[] Frames(string id, string action)
        {
            if (!IsBuiltIn(id)) return Array.Empty<Sprite>();
            var path = "C1Character/Defaults/" + (id == Chick ? "chick" : "green") + "/" + action;
            if (!Cache.TryGetValue(path, out var frames))
            {
                frames = Resources.LoadAll<Sprite>(path);
                Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
                Cache[path] = frames;
            }
            return frames;
        }
        public static Sprite Sprite(string id)
        {
            var frames = Frames(id, "idle");
            return frames.Length > 0 ? frames[0] : null;
        }
        public static Sprite PreviewFrame(string id, string action, float time, bool loop = true)
        {
            var frames = Frames(id, action);
            if (frames.Length == 0) return null;
            var index = Mathf.Max(0, (int)(time * (action == "idle" ? 6 : 10)));
            return frames[loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
        }
        public static string Name(string id) => id == Chick ? "披风小鸡" : "独眼小绿";

        public static void Apply(C1PlayerController2D player, string id)
        {
            if (player == null || !IsBuiltIn(id)) return;
            var visual = player.transform.Find("Character Visual");
            var sprite = Sprite(id);
            if (visual == null || sprite == null) return;
            var run = Frames(id, "run");
            var jump = Frames(id, "jump");
            if (run.Length == 0 || jump.Length == 0) return;
            var animator = visual.GetComponent<C1CharacterAnimator2D>();
            animator.ConfigureRemote(run, jump, 10, 10);
            animator.Configure(Frames(id, "idle"), run, jump);
            visual.GetComponent<SpriteRenderer>().color = Color.white;
            var collider = player.GetComponent<BoxCollider2D>();
            if (collider != null) C1CharacterSizing.Apply(visual, collider, 1.9f);
        }
    }
}
