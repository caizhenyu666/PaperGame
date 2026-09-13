using System;
using System.Collections;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

namespace PaperGame.C1
{
    [Serializable] public sealed class C1LevelResponse
    {
        public string jobId, status, message, schemaVersion;
        public C1LevelProgress progress;
        public C1LevelError error;
        public C1LevelReview review;
        public C1LevelResult result;
        [NonSerialized] public string json;
        public string UserMessage
        {
            get
            {
                var text = error?.message ?? review?.message ?? message ?? "关卡数据无效，请重新生成";
                if (result?.analysis?.warnings != null)
                    foreach (var warning in result.analysis.warnings)
                        if (!string.IsNullOrEmpty(warning?.message)) text += "\n" + warning.message;
                return text;
            }
        }
    }
    [Serializable] public sealed class C1LevelProgress { public string stageLabel; public int percent; }
    [Serializable] public sealed class C1LevelError { public string code, message; public bool retryable; }
    [Serializable] public sealed class C1LevelReview { public string message; }
    [Serializable] public sealed class C1LevelResult { public C1RemoteLevel level; public C1LevelAnalysis analysis; }
    [Serializable] public sealed class C1RemoteLevel { public string schemaVersion; public C1LevelBackground background; }
    [Serializable] public sealed class C1LevelBackground { public string imageUrl, sha256; }
    [Serializable] public sealed class C1LevelAnalysis { public C1LevelError[] warnings; }

    public sealed class C1LevelService : MonoBehaviour
    {
        [SerializeField] private string baseUrl;
        public string BaseUrl
        {
            get => string.IsNullOrWhiteSpace(baseUrl)
                ? C1WebGLBuildSettings.Load()?.CharacterServiceBaseUrl ?? C1WebGLBuildSettings.DefaultCharacterServiceBaseUrl
                : baseUrl.TrimEnd('/');
            set => baseUrl = value;
        }

        public IEnumerator Upload(byte[] photo, string mime, bool force, Action<string> accepted, Action<string> failed)
        {
            if (photo == null || photo.Length == 0 || photo.Length > C1PhotoCapture.MaximumPhotoBytes)
            { failed("照片为空或超过 10 MiB，请重新拍照"); yield break; }
            if (mime != "image/png" && mime != "image/jpeg")
            { failed("仅支持 JPEG 或 PNG 照片"); yield break; }
            var form = new WWWForm();
            form.AddBinaryData("file", photo, mime == "image/png" ? "level.png" : "level.jpg", mime);
            form.AddField("schemaVersion", "1.0");
            // Default C1 controller: speed 6, jump speed 9, gravity scale 3, collider .8 x 1.3.
            var gravity = Mathf.Max(.001f, Mathf.Abs(Physics2D.gravity.y) * 3f);
            var profile = new C1PlayabilityProfile
            {
                maxJumpRisePixels = Mathf.Max(1, Mathf.FloorToInt(81f / (2f * gravity) * C1LevelSpace.PixelsPerUnit)),
                maxJumpDistancePixels = Mathf.Max(1, Mathf.FloorToInt(6f * 18f / gravity * C1LevelSpace.PixelsPerUnit))
            };
            form.AddField("playabilityProfile", JsonUtility.ToJson(profile));
            using (var request = UnityWebRequest.Post(BaseUrl + "/v1/levels" + (force ? "?force=true" : ""), form))
            {
                request.timeout = 45;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) { failed(RequestError(request)); yield break; }
                var response = Parse(request.downloadHandler.text);
                if (request.responseCode != 202 || string.IsNullOrEmpty(response?.jobId))
                { failed("服务器未返回有效的关卡任务编号"); yield break; }
                accepted(response.jobId);
            }
        }

        public IEnumerator Poll(string id, Action<string> progress, Action<C1LevelResponse> finished, Action<string> failed)
        {
            var deadline = Time.realtimeSinceStartup + 360f;
            var errors = 0;
            while (Time.realtimeSinceStartup < deadline)
            {
                using (var request = UnityWebRequest.Get(BaseUrl + "/v1/levels/" + Uri.EscapeDataString(id)))
                {
                    request.timeout = 20;
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        if (++errors >= 3 || request.responseCode == 404)
                        { failed(RequestError(request)); yield break; }
                        progress("网络暂时不稳定，正在重连…");
                    }
                    else
                    {
                        errors = 0;
                        var response = Parse(request.downloadHandler.text);
                        if (response == null || response.jobId != id)
                        { failed("服务器返回了无效的关卡任务"); yield break; }
                        switch (response.status)
                        {
                            case "queued": case "processing":
                                progress(response.progress?.stageLabel ?? response.message ?? "正在生成关卡…");
                                break;
                            case "ready": case "needs_fix": case "needs_review": case "failed":
                                finished(response); yield break;
                            default: failed("无法识别关卡任务状态，请稍后重试"); yield break;
                        }
                    }
                }
                yield return new WaitForSecondsRealtime(1f);
            }
            failed("等待超时，任务已保留，可点击继续生成查询进度");
        }

        public IEnumerator Download(C1LevelResponse response, Action<byte[]> ready, Action<string> failed)
        {
            if (ParsePlayableGeometry(response, out var error) == null) { failed(error); yield break; }
            if (!TryResolveImageUrl(BaseUrl, response.result.level.background?.imageUrl, out var url))
            { failed("关卡背景图地址无效"); yield break; }
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 45;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) { failed(RequestError(request)); yield break; }
                var bytes = request.downloadHandler.data;
                var expected = response.result.level.background.sha256;
                if (!string.IsNullOrEmpty(expected))
                {
                    using (var sha = SHA256.Create())
                    {
                        var actual = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                        { failed("背景图校验失败，请重试下载"); yield break; }
                    }
                }
                ready(bytes);
            }
        }

        public static bool TryResolveImageUrl(string serviceUrl, string imageUrl, out string url)
        {
            url = null;
            if (string.IsNullOrWhiteSpace(imageUrl) || !Uri.TryCreate(serviceUrl, UriKind.Absolute, out var origin) ||
                !Uri.TryCreate(origin, imageUrl, out var resolved) ||
                (resolved.Scheme != Uri.UriSchemeHttp && resolved.Scheme != Uri.UriSchemeHttps)) return false;
            url = resolved.AbsoluteUri;
            return true;
        }

        public static C1LevelResponse Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var result = JsonUtility.FromJson<C1LevelResponse>(json);
                if (result != null) result.json = json;
                return result;
            }
            catch (ArgumentException) { return null; }
        }

        public static C1LevelDefinition ParsePlayableGeometry(C1LevelResponse response, out string error)
        {
            error = "服务器尚未返回可用关卡";
            if (response == null || string.IsNullOrEmpty(response.jobId) ||
                (response.status != "ready" && response.status != "needs_fix")) return null;
            if (response.result?.level?.schemaVersion != "1.0" || response.result.level.background == null)
            { error = "关卡协议版本不受支持或背景数据缺失"; return null; }
            return C1LevelLoader.Parse(response.json, out error);
        }

        private static string RequestError(UnityWebRequest request)
            => Parse(request.downloadHandler?.text)?.error?.message ?? "连接关卡服务器失败，请检查网络后重试";

        [Serializable] private sealed class C1PlayabilityProfile
        {
            public string profileVersion = "unity-c1-default-1";
            public int maxJumpRisePixels, maxJumpDistancePixels;
            public int characterWidthPixels = 32, characterHeightPixels = 52, landingTolerancePixels = 3;
        }
    }
}
