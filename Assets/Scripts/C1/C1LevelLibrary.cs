using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PaperGame.C1
{
    [Serializable]
    public sealed class C1SavedLevel
    {
        public string id, jobId, title, status, message, createdAt;
    }

    [Serializable]
    public sealed class C1PendingLevel
    {
        public string jobId, mime, serviceUrl;
    }

    // Each entry is published by its metadata file only after all required files exist.
    public sealed class C1LevelLibrary
    {
        public string Root { get; }
        public C1LevelLibrary(string root = null)
        {
            Root = root ?? Path.Combine(Application.persistentDataPath, "Levels", "v1");
        }

        public List<C1SavedLevel> Load()
        {
            var result = new List<C1SavedLevel>();
            if (!Directory.Exists(Root)) return result;
            foreach (var folder in Directory.GetDirectories(Root))
            {
                try
                {
                    var record = JsonUtility.FromJson<C1SavedLevel>(File.ReadAllText(Path.Combine(folder, "entry.json")));
                    if (record == null || !IsValidId(record.id) || Path.GetFileName(folder) != record.id ||
                        (record.status != "ready" && record.status != "needs_fix")) continue;
                    if (!File.Exists(Path.Combine(folder, "background.png")) || !File.Exists(Path.Combine(folder, "source"))) continue;
                    if (C1LevelLoader.Parse(File.ReadAllText(Path.Combine(folder, "response.json")), out _) == null) continue;
                    result.Add(record);
                }
                catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException) { }
            }
            result.Sort((a, b) => string.CompareOrdinal(b.createdAt, a.createdAt));
            var jobs = new HashSet<string>();
            result.RemoveAll(item => !jobs.Add(item.jobId));
            return result;
        }

        public C1PendingLevel Pending
        {
            get
            {
                var path = Path.Combine(Root, "pending.json");
                if (!File.Exists(path)) return null;
                var pending = JsonUtility.FromJson<C1PendingLevel>(File.ReadAllText(path));
                return pending != null && File.Exists(Path.Combine(Root, "pending-photo")) ? pending : null;
            }
        }

        public void BeginUpload(byte[] photo, string mime, string serviceUrl)
        {
            if (photo == null || photo.Length == 0) throw new ArgumentException("照片为空");
            Directory.CreateDirectory(Root);
            // Invalidate the previous job before replacing its photo.
            File.WriteAllText(Path.Combine(Root, "pending.json"), "null");
            File.WriteAllBytes(Path.Combine(Root, "pending-photo"), photo);
            SetPending(new C1PendingLevel { mime = mime, serviceUrl = serviceUrl });
        }

        public void SetPending(C1PendingLevel pending)
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(Path.Combine(Root, "pending.json"), JsonUtility.ToJson(pending));
        }

        public byte[] ReadPendingPhoto() => File.ReadAllBytes(Path.Combine(Root, "pending-photo"));

        public void ClearPending()
        {
            if (File.Exists(Path.Combine(Root, "pending.json"))) File.Delete(Path.Combine(Root, "pending.json"));
            if (File.Exists(Path.Combine(Root, "pending-photo"))) File.Delete(Path.Combine(Root, "pending-photo"));
        }

        public C1SavedLevel Save(string responseJson, byte[] background, byte[] source)
        {
            var response = C1LevelService.Parse(responseJson);
            var level = C1LevelService.ParsePlayableGeometry(response, out var error);
            if (level == null) throw new ArgumentException(error);
            if (source == null || source.Length == 0) throw new ArgumentException("缺少原始照片");
            var texture = DecodeBackground(background, level.CanvasPixelSize);
            Release(texture);
            var record = new C1SavedLevel
            {
                id = Guid.NewGuid().ToString("N"), jobId = response.jobId,
                title = "纸上关卡 " + DateTime.Now.ToString("MM-dd HH:mm"),
                status = response.status, message = response.UserMessage,
                createdAt = DateTime.UtcNow.ToString("O")
            };
            var folder = Folder(record.id);
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "source"), source);
            File.WriteAllBytes(Path.Combine(folder, "background.png"), background);
            File.WriteAllText(Path.Combine(folder, "response.json"), responseJson);
            File.WriteAllText(Path.Combine(folder, "entry.json"), JsonUtility.ToJson(record));
            return record;
        }

        public C1LevelDefinition Read(string id, out Texture2D background)
        {
            var folder = Folder(id);
            var response = C1LevelService.Parse(File.ReadAllText(Path.Combine(folder, "response.json")));
            var level = C1LevelService.ParsePlayableGeometry(response, out var error);
            if (level == null) throw new ArgumentException(error);
            background = DecodeBackground(File.ReadAllBytes(Path.Combine(folder, "background.png")), level.CanvasPixelSize);
            return level;
        }

        private string Folder(string id)
        {
            if (!IsValidId(id)) throw new ArgumentException("本地关卡编号无效");
            return Path.Combine(Root, id);
        }

        private static bool IsValidId(string id) => Guid.TryParseExact(id, "N", out _);

        private static Texture2D DecodeBackground(byte[] bytes, Vector2Int size)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (bytes == null || !texture.LoadImage(bytes) || texture.width != size.x || texture.height != size.y)
            {
                Release(texture);
                throw new ArgumentException("关卡背景图损坏或尺寸与关卡数据不一致，请重新生成");
            }
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        internal static void Release(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
