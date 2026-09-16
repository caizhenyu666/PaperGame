using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1DefaultCharacterAtlasTests
    {
        [TestCase("green", "idle")]
        [TestCase("green", "run")]
        [TestCase("green", "jump")]
        [TestCase("chick", "idle")]
        [TestCase("chick", "run")]
        [TestCase("chick", "jump")]
        public void DefaultAtlases_UseEightEqualFramesAndOneFootPivot(string character, string action)
        {
            var frames = Resources.LoadAll<Sprite>("C1Character/Defaults/" + character + "/" + action);
            Assert.That(frames.Length, Is.EqualTo(8));
            foreach (var frame in frames)
            {
                Assert.That(frame.rect.size, Is.EqualTo(new Vector2(256, 256)));
                Assert.That(frame.pivot, Is.EqualTo(new Vector2(128, 26)));
                Assert.That(frame.texture.width, Is.EqualTo(2048));
                Assert.That(frame.texture.height, Is.EqualTo(256));
            }
        }

        [TestCase("default")]
        [TestCase("default-chick")]
        public void BuiltInCharacter_UsesBalancedVisualAndCollisionSize(string id)
        {
            var root = new GameObject("Character Size Test", typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(C1PlayerController2D));
            try
            {
                var visual = new GameObject("Character Visual", typeof(SpriteRenderer), typeof(C1CharacterAnimator2D));
                visual.transform.SetParent(root.transform, false);
                C1BuiltInCharacters.Apply(root.GetComponent<C1PlayerController2D>(), id);
                var collider = root.GetComponent<BoxCollider2D>();
                Assert.That(collider.size, Is.EqualTo(new Vector2(1.1f, 1.6f)));
                Assert.That(collider.offset, Is.EqualTo(Vector2.zero));
                Assert.That(visual.transform.localScale.y * 1.9f, Is.EqualTo(1.6f).Within(.001f));
                Assert.That(visual.transform.localPosition.y, Is.EqualTo(-.8f).Within(.001f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase("default")]
        [TestCase("default-chick")]
        public void ApplyingBuiltInCharacter_UsesRealAnimationFrames(string id)
        {
            var root = new GameObject("Default Animation Test", typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(C1PlayerController2D));
            try
            {
                var visual = new GameObject("Character Visual", typeof(SpriteRenderer), typeof(C1CharacterAnimator2D));
                visual.transform.SetParent(root.transform, false);
                C1BuiltInCharacters.Apply(root.GetComponent<C1PlayerController2D>(), id);
                var animator = visual.GetComponent<C1CharacterAnimator2D>();
                Assert.That(animator.IdleFrameCount, Is.EqualTo(8));
                Assert.That(animator.RunFrameCount, Is.EqualTo(8));
                Assert.That(animator.JumpFrameCount, Is.EqualTo(8));
                animator.Tick(.12f, true, 1, false);
                Assert.That(animator.CurrentFrameIndex, Is.GreaterThan(0));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
