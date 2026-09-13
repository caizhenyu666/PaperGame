using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace PaperGame.C1
{
    public sealed class C1CharacterService : MonoBehaviour
    {
        [SerializeField] private string baseUrl = "http://10.131.58.235:8000";
        public string BaseUrl { get => baseUrl; set => baseUrl = value.TrimEnd('/'); }
        public IEnumerator Upload(byte[] bytes, string mime, bool force, Action<string> accepted, Action<string> failed)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > C1PhotoCapture.MaximumPhotoBytes)
            { failed("照片为空或超过 10 MiB，请重新拍照"); yield break; }
            var url = baseUrl.TrimEnd('/') + "/v1/characters" + (force ? "?force=true" : "");
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
            var url = baseUrl.TrimEnd('/') + "/v1/characters/" + Uri.EscapeDataString(id);
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
                    if (string.IsNullOrEmpty(path) || !path.StartsWith("/artifacts/")) { var err = "精灵图集地址无效"; Debug.LogWarning("[C1Service] Download invalid path: " + path); failed(err); yield break; }
                    var url = new Uri(new Uri(baseUrl), path).AbsoluteUri;
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
        private static string RequestError(UnityWebRequest request)
        {
            var code = Parse(request.downloadHandler?.text)?.code;
            return string.IsNullOrEmpty(code) ? "连接服务器失败，请检查网络后重试" : "服务器请求失败（" + code + "）";
        }
    }
}
