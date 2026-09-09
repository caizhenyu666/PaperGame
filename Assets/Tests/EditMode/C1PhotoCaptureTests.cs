using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Tests
{
    public sealed class C1PhotoCaptureTests
    {
        private GameObject root;
        private C1PhotoCapture capture;
        private RawImage preview;
        private Text status;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Photo Capture Test");
            capture = root.AddComponent<C1PhotoCapture>();
            preview = new GameObject("Preview").AddComponent<RawImage>();
            preview.transform.SetParent(root.transform);
            status = new GameObject("Status").AddComponent<Text>();
            status.transform.SetParent(root.transform);
            capture.Configure(preview, status);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void ReceivePhotoDataUrl_ValidPng_PreservesBytesAndCreatesPreview()
        {
            var bytes = CreatePngBytes(3, 2, Color.cyan);

            capture.ReceivePhotoDataUrl("data:image/png;base64," + Convert.ToBase64String(bytes));

            CollectionAssert.AreEqual(bytes, capture.CapturedBytes);
            Assert.That(capture.PreviewTexture, Is.Not.Null);
            Assert.That(capture.PreviewTexture.width, Is.EqualTo(3));
            Assert.That(capture.PreviewTexture.height, Is.EqualTo(2));
            Assert.That(preview.texture, Is.SameAs(capture.PreviewTexture));
            Assert.That(status.text, Is.EqualTo("照片已获取"));
        }

        [Test]
        public void ReceivePhotoDataUrl_UnsupportedMime_DoesNotReplacePreviousPhoto()
        {
            var bytes = CreatePngBytes(2, 2, Color.red);
            capture.ReceivePhotoDataUrl("data:image/png;base64," + Convert.ToBase64String(bytes));
            var previousTexture = capture.PreviewTexture;
            var previousBytes = capture.CapturedBytes;

            capture.ReceivePhotoDataUrl("data:image/gif;base64," + Convert.ToBase64String(bytes));

            Assert.That(capture.PreviewTexture, Is.SameAs(previousTexture));
            Assert.That(capture.CapturedBytes, Is.SameAs(previousBytes));
            StringAssert.Contains("仅支持 JPEG 或 PNG", status.text);
        }

        [Test]
        public void ApplyPhoto_OverMaximumSize_IsRejectedBeforeDecode()
        {
            var oversizedBytes = new byte[C1PhotoCapture.MaximumPhotoBytes + 1];

            var applied = capture.ApplyPhoto(oversizedBytes, "image/png");

            Assert.That(applied, Is.False);
            Assert.That(capture.CapturedBytes, Is.Null);
            Assert.That(capture.PreviewTexture, Is.Null);
            StringAssert.Contains("10 MiB", status.text);
        }

        [Test]
        public void ReceivePhotoDataUrl_SecondValidPhoto_ReplacesAndDestroysOldTexture()
        {
            var firstBytes = CreatePngBytes(2, 2, Color.red);
            var secondBytes = CreatePngBytes(4, 3, Color.green);
            capture.ReceivePhotoDataUrl("data:image/png;base64," + Convert.ToBase64String(firstBytes));
            var firstTexture = capture.PreviewTexture;

            capture.ReceivePhotoDataUrl("data:image/png;base64," + Convert.ToBase64String(secondBytes));

            Assert.That(firstTexture == null, Is.True);
            CollectionAssert.AreEqual(secondBytes, capture.CapturedBytes);
            Assert.That(capture.PreviewTexture.width, Is.EqualTo(4));
            Assert.That(capture.PreviewTexture.height, Is.EqualTo(3));
        }

        private static byte[] CreatePngBytes(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var index = 0; index < pixels.Length; index++)
            {
                pixels[index] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            var bytes = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return bytes;
        }
    }
}
