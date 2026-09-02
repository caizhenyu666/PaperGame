using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1
{
    public sealed class C1PhotoCapture : MonoBehaviour
    {
        public const int MaximumPhotoBytes = 15 * 1024 * 1024;

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
#else
            SetStatus("请在手机浏览器中使用拍照功能");
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
                SetStatus("照片不能超过 15 MB");
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
