using System.Collections.Generic;
using UnityEngine;

namespace PaperGame.C1
{
    public sealed class C1ReachabilityResult
    {
        public bool CanReachGoal { get; internal set; }
        public IReadOnlyList<C1BlockedJump> BlockedJumps { get; internal set; }
    }

    public readonly struct C1BlockedJump
    {
        public C1BlockedJump(int from, int to, float requiredHeight)
        {
            FromPlatformIndex = from;
            ToPlatformIndex = to;
            RequiredHeight = requiredHeight;
        }

        public int FromPlatformIndex { get; }
        public int ToPlatformIndex { get; }
        public float RequiredHeight { get; }
    }

    public static class C1Reachability
    {
        public static C1ReachabilityResult Analyze(C1LevelDefinition level, float jumpHeight)
        {
            var platforms = level.Platforms;
            var visited = new bool[platforms.Length];
            var queue = new Queue<int>();
            var blocked = new List<C1BlockedJump>();
            var start = FindPlatform(platforms, level.PlayerStart.x);
            var goal = FindPlatform(platforms, level.GoalPosition.x);
            visited[start] = true;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var from = queue.Dequeue();
                for (var to = 0; to < platforms.Length; to++)
                {
                    if (visited[to] || from == to) continue;
                    var rise = Mathf.Max(0f, platforms[to].Position.y - platforms[from].Position.y);
                    if (rise > jumpHeight)
                    {
                        blocked.Add(new C1BlockedJump(from, to, rise));
                        continue;
                    }

                    visited[to] = true;
                    queue.Enqueue(to);
                }
            }

            return new C1ReachabilityResult { CanReachGoal = visited[goal], BlockedJumps = blocked };
        }

        private static int FindPlatform(C1PlatformDefinition[] platforms, float x)
        {
            for (var index = 0; index < platforms.Length; index++)
            {
                var platform = platforms[index];
                if (x >= platform.Position.x - platform.Size.x * 0.5f && x <= platform.Position.x + platform.Size.x * 0.5f) return index;
            }
            return 0;
        }
    }
}
