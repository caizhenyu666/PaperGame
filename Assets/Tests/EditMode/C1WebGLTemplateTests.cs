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
        public void MobileTemplate_KeepsUnityCanvasVisibleAndUsesOnlyCssForPortraitLayout()
        {
            var templateRoot = Path.Combine(Application.dataPath, "WebGLTemplates/PaperGameMobile");
            var html = File.ReadAllText(Path.Combine(templateRoot, "index.html"));
            var css = File.ReadAllText(Path.Combine(templateRoot, "TemplateData/style.css"));

            StringAssert.DoesNotContain("rotate-overlay", html);
            StringAssert.DoesNotContain("unityContainer.style.display", html);
            StringAssert.DoesNotContain("screen.orientation", html);
            StringAssert.Contains("@media (orientation: portrait)", css);
            StringAssert.Contains("rotate(90deg)", css);
            StringAssert.Contains("translate3d(100dvw, 0, 0)", css);
        }

        [Test]
        public void MobileTemplate_KeepsWebGlBackingBufferStableAcrossOrientationChanges()
        {
            var root = Path.Combine(Application.dataPath, "WebGLTemplates/PaperGameMobile");
            var html = File.ReadAllText(Path.Combine(root, "index.html"));
            var css = File.ReadAllText(Path.Combine(root, "TemplateData/style.css"));

            StringAssert.Contains("initializeUnityCanvas", html);
            StringAssert.DoesNotContain("addEventListener(\"resize\", resizeUnityCanvas)", html);
            StringAssert.DoesNotContain("new ResizeObserver(resizeUnityCanvas)", html);
            StringAssert.Contains("matchWebGLToCanvasSize: false", html);
            StringAssert.Contains("translate3d(100dvw, 0, 0) rotate(90deg)", css);
            StringAssert.Contains("backface-visibility: hidden", css);
        }

        [Test]
        public void MobileManifest_PrefersStandaloneLandscape()
        {
            var path = Path.Combine(Application.dataPath,
                "WebGLTemplates/PaperGameMobile/manifest.json");
            var manifest = File.ReadAllText(path);

            StringAssert.Contains("\"display\": \"standalone\"", manifest);
            StringAssert.Contains("\"orientation\": \"landscape\"", manifest);
        }

        [Test]
        public void PhotoCaptureOverlay_UsesStableLandscapeControlsAndLocalCropCoordinates()
        {
            var path = Path.Combine(Application.dataPath,
                "Plugins/WebGL/PaperGamePhotoCapture.jslib");
            var source = File.ReadAllText(path);

            StringAssert.Contains("id=\"pg-cam-close\"", source);
            StringAssert.Contains("aria-label=\"关闭相机\"", source);
            StringAssert.Contains("id=\"pg-cam-shutter-core\"", source);
            StringAssert.DoesNotContain(">拍 照<", source);
            StringAssert.Contains("aspect-ratio:8/5", source);
            StringAssert.Contains("translate3d(100dvw,0,0) rotate(90deg)", source);
            StringAssert.Contains("video.srcObject=null", source);
            StringAssert.Contains("close.addEventListener('click',cleanup)", source);
            StringAssert.Contains("frame.offsetLeft", source);
            StringAssert.Contains("video.clientWidth", source);
            StringAssert.DoesNotContain("frame.getBoundingClientRect()", source);
        }

        [Test]
        public void WebGLBuilder_SelectsPaperGameMobileTemplate()
        {
            var source = File.ReadAllText(Path.Combine(Application.dataPath, "Editor/C1WebGLBuilder.cs"));

            StringAssert.Contains("PROJECT:PaperGameMobile", source);
            StringAssert.Contains("PlayerSettings.WebGL.template", source);
        }

        [Test]
        public void WebGlSettingsProvider_RegistersProjectSettingsPage()
        {
            var source = File.ReadAllText(Path.Combine(Application.dataPath, "Editor/C1WebGLBuildSettingsProvider.cs"));

            StringAssert.Contains("Project/Paper Game", source);
            StringAssert.Contains("SettingsProvider", source);
            StringAssert.Contains("C1WebGLBuildSettings", source);
        }

        [Test]
        public void WebGLBuilder_UsesConfiguredOutputAndLogsAbsolutePath()
        {
            var source = File.ReadAllText(Path.Combine(Application.dataPath, "Editor/C1WebGLBuilder.cs"));

            StringAssert.Contains("C1WebGLBuildSettings", source);
            StringAssert.Contains("Path.GetFullPath", source);
            StringAssert.Contains("WebGL build succeeded:", source);
            StringAssert.Contains("C1_WEBGL_OUTPUT", source);
        }
    }
}
