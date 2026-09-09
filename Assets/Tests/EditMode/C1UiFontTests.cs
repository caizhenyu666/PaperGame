using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1UiFontTests
    {
        [Test]
        public void Load_ChineseUiFontContainsCharacterCreationGlyphs()
        {
            var font = C1UiFont.Load();

            Assert.That(font, Is.Not.Null);
            Assert.That(font.HasCharacter('拍'), Is.True);
            Assert.That(font.HasCharacter('照'), Is.True);
            Assert.That(font.HasCharacter('主'), Is.True);
            Assert.That(font.HasCharacter('角'), Is.True);
        }
    }
}
