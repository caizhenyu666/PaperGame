using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class C1WebGLTemplateTests
    {
        [Test]
        public void MobileTemplate_UsesStaticViewportAndFullViewportCanvas()
        {
            var templateRoot = Path.Combine(Application.dataPath, "WebGLTemplates/PaperGameMobile");
            var html = File.ReadAllText(Path.Combine(templateRoot, "index.html"));
            var css = File.ReadAllText(Path.Combine(templateRoot, "TemplateData/style.css"));

            StringAssert.Contains("width=device-width, height=device-height", html);
            StringAssert.DoesNotContain("iPhone|iPad|iPod|Android", html);
            StringAssert.Contains("width: 100dvw", css);
            StringAssert.Contains("height: 100dvh", css);
            StringAssert.Contains("overflow: hidden", css);
            StringAssert.Contains("#unity-canvas", css);
        }

        [Test]
        public void WebGLBuilder_SelectsPaperGameMobileTemplate()
        {
            var source = File.ReadAllText(Path.Combine(Application.dataPath, "Editor/C1WebGLBuilder.cs"));

            StringAssert.Contains("PROJECT:PaperGameMobile", source);
            StringAssert.Contains("PlayerSettings.WebGL.template", source);
        }
    }
}
