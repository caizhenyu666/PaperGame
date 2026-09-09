using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaperGame.C1
{
    public sealed class C1PhotoCapture : MonoBehaviour
    {
        public const int MaximumPhotoBytes = 10 * 1024 * 1024;
        public event Action<byte[], string> PhotoCaptured;

        private RawImage preview;
        private Text status;

        public byte[] CapturedBytes { get; private set; }
        public Texture2D PreviewTexture { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void PaperGame_OpenPhotoCapture(string receiverName);
#endif

        public void Configure(RawImage previewImage, Text statusText)
        {
            preview = previewImage;
            status = statusText;
        }

        public void OpenCamera()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SetStatus("正在打开相机…");
            PaperGame_OpenPhotoCapture(gameObject.name);
#elif UNITY_EDITOR
            var path = EditorUtility.OpenFilePanel("\u9009\u62e9\u7167\u7247\u6587\u4ef6", "", "jpg,png");
            if (string.IsNullOrEmpty(path))
            {
                SetStatus("\u672a\u9009\u62e9\u6587\u4ef6");
                return;
            }
            if (!File.Exists(path))
            {
                SetStatus("\u6587\u4ef6\u4e0d\u5b58\u5728");
                return;
            }
            var bytes = File.ReadAllBytes(path);
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var mimeType = ext == ".png" ? "image/png" : "image/jpeg";
            ApplyPhoto(bytes, mimeType);
#else
            SetStatus("\u8bf7\u5728\u624b\u673a\u6d4f\u89c8\u5668\u4e2d\u4f7f\u7528\u62cd\u7167\u529f\u80fd");
#endif
        }

        public void ReceivePhotoDataUrl(string dataUrl)
        {
            if (!TryParseDataUrl(dataUrl, out var mimeType, out var encodedBytes))
            {
                SetStatus("照片数据格式无效，仅支持 JPEG 或 PNG");
                return;
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(encodedBytes);
            }
            catch (FormatException)
            {
                SetStatus("照片数据损坏，请重新拍照");
                return;
            }

            ApplyPhoto(bytes, mimeType);
        }

        public void ReceivePhotoError(string message)
        {
            SetStatus(string.IsNullOrWhiteSpace(message) ? "未能获取照片，请重试" : message);
        }

        public bool ApplyPhoto(byte[] bytes, string mimeType)
        {
            if (!IsSupportedMimeType(mimeType))
            {
                SetStatus("仅支持 JPEG 或 PNG 照片");
                return false;
            }

            if (bytes == null || bytes.Length == 0)
            {
                SetStatus("照片数据为空，请重新拍照");
                return false;
            }

            if (bytes.Length > MaximumPhotoBytes)
            {
                SetStatus("照片不能超过 10 MiB");
                return false;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes, false))
            {
                DestroyTexture(texture);
                SetStatus("无法读取照片，请使用有效的 JPEG 或 PNG 文件");
                return false;
            }

            var previousTexture = PreviewTexture;
            PreviewTexture = texture;
            CapturedBytes = bytes;

            if (preview != null)
            {
                preview.texture = texture;
                preview.gameObject.SetActive(true);
            }

            DestroyTexture(previousTexture);
            SetStatus("照片已获取");
            PhotoCaptured?.Invoke(bytes, mimeType);
            return true;
        }

        private void OnDestroy()
        {
            DestroyTexture(PreviewTexture);
            PreviewTexture = null;
            CapturedBytes = null;
        }

        private static bool TryParseDataUrl(string dataUrl, out string mimeType, out string encodedBytes)
        {
            mimeType = null;
            encodedBytes = null;
            if (string.IsNullOrWhiteSpace(dataUrl))
            {
                return false;
            }

            var separatorIndex = dataUrl.IndexOf(',');
            if (separatorIndex <= 5)
            {
                return false;
            }

            var header = dataUrl.Substring(0, separatorIndex);
            const string prefix = "data:";
            const string suffix = ";base64";
            if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                !header.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            mimeType = header.Substring(prefix.Length, header.Length - prefix.Length - suffix.Length);
            if (!IsSupportedMimeType(mimeType))
            {
                return false;
            }

            encodedBytes = dataUrl.Substring(separatorIndex + 1);
            return encodedBytes.Length > 0;
        }

        private static bool IsSupportedMimeType(string mimeType)
        {
            return string.Equals(mimeType, "image/jpeg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mimeType, "image/png", StringComparison.OrdinalIgnoreCase);
        }

        private void SetStatus(string message)
        {
            if (status != null)
            {
                status.text = message;
            }
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }
    }
}
