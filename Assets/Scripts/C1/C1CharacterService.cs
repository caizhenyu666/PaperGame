using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace PaperGame.C1
{
    public sealed class C1CharacterService : MonoBehaviour
    {
        [SerializeField] private string baseUrl;
        public string BaseUrl
        {
            get => string.IsNullOrWhiteSpace(baseUrl)
                ? C1WebGLBuildSettings.Load()?.CharacterServiceBaseUrl ?? C1WebGLBuildSettings.DefaultCharacterServiceBaseUrl
                : baseUrl.TrimEnd('/');
            set => baseUrl = value?.TrimEnd('/') ?? string.Empty;
        }
        public IEnumerator Upload(byte[] bytes, string mime, bool force, Action<string> accepted, Action<string> failed)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > C1PhotoCapture.MaximumPhotoBytes)
            { failed("照片为空或超过 10 MiB，请重新拍照"); yield break; }
            var url = BaseUrl + "/v1/characters" + (force ? "?force=true" : "");
            Debug.Log("[C1Service] Upload → POST " + url + " (" + bytes.Length + " bytes, " + mime + ", force=" + force + ")");
            var form = new WWWForm();
            form.AddBinaryData("file", bytes, mime == "image/png" ? "doodle.png" : "doodle.jpg", mime);
            using (var request = UnityWebRequest.Post(url, form))
            {
                request.timeout = 45;
                yield return request.SendWebRequest();
                Debug.Log("[C1Service] Upload ← HTTP " + request.responseCode + " result=" + request.result);
                if (request.result != UnityWebRequest.Result.Success) { var err = RequestError(request); Debug.LogWarning("[C1Service] Upload failed: " + err); failed(err); yield break; }
                var body = request.downloadHandler.text;
                Debug.Log("[C1Service] Upload response: " + body);
                var record = Parse(body);
                if (request.responseCode != 202 || string.IsNullOrEmpty(record?.jobId)) { var err = "服务器未返回有效任务编号"; Debug.LogWarning("[C1Service] Upload invalid: code=" + request.responseCode + " jobId=" + (record?.jobId ?? "null")); failed(err); yield break; }
                Debug.Log("[C1Service] Upload accepted: jobId=" + record.jobId);
                accepted(record.jobId);
            }
        }
        public IEnumerator Poll(string id, Action<string> progress, Action<C1CharacterRecord> ready, Action<string> failed)
        {
            var url = BaseUrl + "/v1/characters/" + Uri.EscapeDataString(id);
            Debug.Log("[C1Service] Poll → GET " + url);
            var deadline = Time.realtimeSinceStartup + 360;
            var errors = 0;
            var polls = 0;
            while (Time.realtimeSinceStartup < deadline)
            {
                polls++;
                using (var request = UnityWebRequest.Get(url))
                {
                    request.timeout = 20;
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        errors++;
                        Debug.LogWarning("[C1Service] Poll #" + polls + " failed: HTTP " + request.responseCode + " errors=" + errors + "/3");
                        if (errors >= 3 || request.responseCode == 404) { var err = RequestError(request); Debug.LogWarning("[C1Service] Poll aborted: " + err); failed(err); yield break; }
                        progress("网络暂时不稳定，正在重新连接…");
                    }
                    else
                    {
                        errors = 0;
                        var body = request.downloadHandler.text;
                        var record = Parse(body);
                        Debug.Log("[C1Service] Poll #" + polls + " ← HTTP " + request.responseCode + " status=" + (record?.status ?? "null") + " body=" + body);
                        if (record?.status == "ready") { Debug.Log("[C1Service] Poll ready: jobId=" + id); ready(record); yield break; }
                        if (record?.status == "needs_correction") { var err = "请画一个完整、清晰的小人后重新拍照（" + record.reason + "）"; Debug.LogWarning("[C1Service] Poll needs_correction: " + err); failed(err); yield break; }
                        if (record?.status == "failed") { var err = "生成失败，可点击重新生成（" + record.code + "）"; Debug.LogWarning("[C1Service] Poll failed: " + err); failed(err); yield break; }
                        if (record?.status != "queued" && record?.status != "processing") { var err = "服务器返回了无法识别的任务状态"; Debug.LogWarning("[C1Service] Poll unknown status: " + (record?.status ?? "null")); failed(err); yield break; }
                        progress(record.status == "queued" ? "小主角正在排队，请稍等…" : "正在让你的涂鸦动起来…");
                    }
                }
                yield return new WaitForSecondsRealtime(1);
            }
            var timeoutErr = "等待超过 6 分钟，可点击继续查询，任务仍保留在服务器";
            Debug.LogWarning("[C1Service] Poll timeout: jobId=" + id + " polls=" + polls);
            failed(timeoutErr);
        }
        public IEnumerator Download(C1CharacterRecord record, Action<C1CharacterFrames> ready, Action<string> failed)
        {
            var meta = record?.animations;
            if (meta?.run == null || meta.jump == null || meta.run.frameWidth != meta.jump.frameWidth || meta.run.frameHeight != meta.jump.frameHeight)
            { var err = "服务器动画数据不完整"; Debug.LogWarning("[C1Service] Download invalid meta: run=" + (meta?.run != null) + " jump=" + (meta?.jump != null)); failed(err); yield break; }
            Debug.Log("[C1Service] Download → run=" + meta.run.spriteSheetUrl + " jump=" + meta.jump.spriteSheetUrl);
            var frames = new C1CharacterFrames();
            var complete = false;
            try
            {
                for (var i = 0; i < 2; i++)
                {
                    var animation = i == 0 ? meta.run : meta.jump;
                    var path = animation.spriteSheetUrl;
                    if (string.IsNullOrEmpty(path)) { var err = "精灵图集地址为空"; Debug.LogWarning("[C1Service] Download invalid path: " + path); failed(err); yield break; }
                    string url;
                    if (path.StartsWith("http://") || path.StartsWith("https://"))
                    {
                        url = path;
                    }
                    else if (path.StartsWith("/artifacts/"))
                    {
                        url = new Uri(new Uri(BaseUrl), path).AbsoluteUri;
                    }
                    else
                    {
                        var err = "精灵图集地址无效"; Debug.LogWarning("[C1Service] Download invalid path: " + path); failed(err); yield break;
                    }
                    Debug.Log("[C1Service] Download " + (i == 0 ? "run" : "jump") + " → GET " + url);
                    using (var request = UnityWebRequestTexture.GetTexture(url))
                    {
                        request.timeout = 45;
                        yield return request.SendWebRequest();
                        Debug.Log("[C1Service] Download " + (i == 0 ? "run" : "jump") + " ← HTTP " + request.responseCode + " result=" + request.result);
                        if (request.result != UnityWebRequest.Result.Success) { var err = RequestError(request); Debug.LogWarning("[C1Service] Download failed: " + err); failed(err); yield break; }
                        var texture = DownloadHandlerTexture.GetContent(request);
                        if (i == 0) frames.runTexture = texture; else frames.jumpTexture = texture;
                        Sprite[] sliced = null;
                        string error = null;
                        try { sliced = C1CharacterFrames.Slice(texture, animation); }
                        catch (ArgumentException e) { error = e.Message; }
                        if (error != null) { Debug.LogWarning("[C1Service] Slice failed: " + error); failed(error); yield break; }
                        if (i == 0) { frames.run = sliced; frames.runFps = animation.fps; }
                        else { frames.jump = sliced; frames.jumpFps = animation.fps; }
                        Debug.Log("[C1Service] Download " + (i == 0 ? "run" : "jump") + " ok: " + sliced.Length + " frames, " + texture.width + "x" + texture.height);
                    }
                }
                complete = true;
                Debug.Log("[C1Service] Download complete: jobId=" + record?.jobId);
                ready(frames);
            }
            finally { if (!complete) frames.Dispose(); }
        }
        public static C1CharacterRecord Parse(string json)
        {
            try { return JsonUtility.FromJson<C1CharacterRecord>(json); }
            catch (Exception) { return null; }
        }

        /// <summary>本地缓存目录：Application.persistentDataPath/C1CharacterFrames/</summary>
        private static string FramesDir
        {
            get
            {
                var dir = Path.Combine(Application.persistentDataPath, "C1CharacterFrames");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>将下载好的精灵图纹理保存为本地 PNG 缓存，并提取首帧作为缩略图。</summary>
        public void SaveFrames(string characterId, C1CharacterFrames frames)
        {
            if (string.IsNullOrEmpty(characterId) || frames == null) return;
            try
            {
                if (frames.runTexture != null)
                {
                    var bytes = frames.runTexture.EncodeToPNG();
                    File.WriteAllBytes(Path.Combine(FramesDir, characterId + "_run.png"), bytes);
                }
                if (frames.jumpTexture != null)
                {
                    var bytes = frames.jumpTexture.EncodeToPNG();
                    File.WriteAllBytes(Path.Combine(FramesDir, characterId + "_jump.png"), bytes);
                }
                // 提取 run 首帧作为缩略图，供列表显示
                if (frames.run != null && frames.run.Length > 0)
                {
                    var thumb = frames.run[0];
                    if (thumb != null && thumb.texture != null)
                    {
                        var thumbTex = new Texture2D((int)thumb.rect.width, (int)thumb.rect.height, TextureFormat.RGBA32, false);
                        var pixels = thumb.texture.GetPixels(
                            (int)thumb.rect.x, (int)thumb.rect.y,
                            (int)thumb.rect.width, (int)thumb.rect.height);
                        thumbTex.SetPixels(pixels);
                        thumbTex.Apply();
                        File.WriteAllBytes(Path.Combine(FramesDir, characterId + "_thumb.png"), thumbTex.EncodeToPNG());
                        UnityEngine.Object.Destroy(thumbTex);
                    }
                }
                Debug.Log("[C1Service] Frames saved locally: " + characterId);
            }
            catch (Exception e) { Debug.LogWarning("[C1Service] SaveFrames failed: " + e.Message); }
        }

        /// <summary>尝试加载本地缩略图，用于列表显示。优先读 _thumb.png，没有则从 _run.png 提取首帧。</summary>
        public Sprite LoadThumbnail(string characterId, C1CharacterRecord record)
        {
            // 1. 优先加载专用缩略图
            var thumbPath = Path.Combine(FramesDir, characterId + "_thumb.png");
            if (File.Exists(thumbPath))
            {
                try
                {
                    var bytes = File.ReadAllBytes(thumbPath);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 100);
                    UnityEngine.Object.Destroy(tex);
                }
                catch (Exception e) { Debug.LogWarning("[C1Service] LoadThumbnail _thumb failed: " + e.Message); }
            }

            // 2. 回退：从 _run.png 提取首帧（兼容旧缓存）
            if (record?.animations?.run == null) return null;
            var runPath = Path.Combine(FramesDir, characterId + "_run.png");
            if (!File.Exists(runPath)) return null;
            var meta = record.animations.run;
            if (meta.frameWidth <= 0 || meta.frameHeight <= 0 || meta.frameCount <= 0) return null;
            try
            {
                var runBytes = File.ReadAllBytes(runPath);
                var runTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!runTex.LoadImage(runBytes)) { UnityEngine.Object.Destroy(runTex); return null; }
                // 首帧区域：从左下角起 frameWidth x frameHeight
                var fw = meta.frameWidth;
                var fh = meta.frameHeight;
                if (fw > runTex.width || fh > runTex.height) { UnityEngine.Object.Destroy(runTex); return null; }
                var pixels = runTex.GetPixels(0, 0, fw, fh);
                var thumbTex = new Texture2D(fw, fh, TextureFormat.RGBA32, false);
                thumbTex.SetPixels(pixels);
                thumbTex.Apply();
                UnityEngine.Object.Destroy(runTex);
                // 同时补存 _thumb.png，下次就不用再提取了
                try { File.WriteAllBytes(thumbPath, thumbTex.EncodeToPNG()); } catch { /* ignore */ }
                return Sprite.Create(thumbTex, new Rect(0, 0, fw, fh), new Vector2(0.5f, 0f), 100);
            }
            catch (Exception e) { Debug.LogWarning("[C1Service] LoadThumbnail fallback failed: " + e.Message); return null; }
        }

        /// <summary>尝试从本地缓存加载精灵图，成功返回 true 并填充 frames。</summary>
        public bool TryLoadFrames(string characterId, C1CharacterRecord record, out C1CharacterFrames frames)
        {
            frames = null;
            if (string.IsNullOrEmpty(characterId) || record?.animations?.run == null || record?.animations?.jump == null) return false;
            var runPath = Path.Combine(FramesDir, characterId + "_run.png");
            var jumpPath = Path.Combine(FramesDir, characterId + "_jump.png");
            if (!File.Exists(runPath) || !File.Exists(jumpPath)) return false;
            try
            {
                var f = new C1CharacterFrames();
                var runBytes = File.ReadAllBytes(runPath);
                var jumpBytes = File.ReadAllBytes(jumpPath);
                f.runTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                f.jumpTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!f.runTexture.LoadImage(runBytes) || !f.jumpTexture.LoadImage(jumpBytes))
                {
                    Debug.LogWarning("[C1Service] Local cache image decode failed: " + characterId);
                    f.Dispose();
                    return false;
                }
                f.run = C1CharacterFrames.Slice(f.runTexture, record.animations.run);
                f.jump = C1CharacterFrames.Slice(f.jumpTexture, record.animations.jump);
                f.runFps = record.animations.run.fps;
                f.jumpFps = record.animations.jump.fps;
                frames = f;
                Debug.Log("[C1Service] Frames loaded from local cache: " + characterId);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[C1Service] TryLoadFrames failed: " + e.Message);
                if (frames != null) frames.Dispose();
                frames = null;
                return false;
            }
        }
        private static string RequestError(UnityWebRequest request)
        {
            var code = Parse(request.downloadHandler?.text)?.code;
            return string.IsNullOrEmpty(code) ? "连接服务器失败，请检查网络后重试" : "服务器请求失败（" + code + "）";
        }
    }
}
