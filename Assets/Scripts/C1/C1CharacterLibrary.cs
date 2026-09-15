using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaperGame.C1
{
    // The API has exactly two animation keys; typed fields allow JsonUtility without a Dictionary.
    [Serializable] public sealed class C1AnimationMeta
    {
        public string spriteSheetUrl;
        public int frameCount, fps, frameWidth, frameHeight;
        public C1FootAnchor footAnchor;
    }
    [Serializable] public sealed class C1FootAnchor { public float x, y; }
    [Serializable] public sealed class C1Animations { public C1AnimationMeta run, jump; }
    [Serializable] public sealed class C1CharacterRecord
    {
        public string jobId, characterId, status, code, reason;
        public C1Animations animations;
    }
    [Serializable] public sealed class C1CharacterLibrary
    {
        public const string StorageKey = "PaperGame.Characters.v1";
        public string selectedId = "default";
        public string pendingJobId;
        public List<C1CharacterRecord> characters = new List<C1CharacterRecord>();

        public static C1CharacterLibrary Load()
        {
            try
            {
                var library = JsonUtility.FromJson<C1CharacterLibrary>(PlayerPrefs.GetString(StorageKey, "{}"));
                if (library == null) return new C1CharacterLibrary();
                if (library.characters == null) library.characters = new List<C1CharacterRecord>();
                library.characters.RemoveAll(x => x == null || string.IsNullOrEmpty(x.characterId));
                if (!C1BuiltInCharacters.IsBuiltIn(library.selectedId) && !library.characters.Exists(x => x.characterId == library.selectedId)) library.selectedId = "default";
                return library;
            }
            catch (Exception) { return new C1CharacterLibrary(); }
        }
        public void Save() { PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(this)); PlayerPrefs.Save(); }
        public void Add(C1CharacterRecord character)
        {
            characters.RemoveAll(x => x.characterId == character.characterId);
            characters.Add(character);
            pendingJobId = null;
            Save();
        }
    }

    public sealed class C1CharacterFrames : IDisposable
    {
        public Sprite[] run, jump;
        public float runFps, jumpFps;
        public Texture2D runTexture, jumpTexture;
        public void Dispose()
        {
            if (run != null) foreach (var sprite in run) Release(sprite);
            if (jump != null) foreach (var sprite in jump) Release(sprite);
            Release(runTexture); Release(jumpTexture);
        }
        private static void Release(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
        public static Sprite[] Slice(Texture2D texture, C1AnimationMeta meta)
        {
            if (texture == null || meta == null || meta.footAnchor == null || meta.frameCount <= 0 ||
                meta.fps <= 0 || meta.frameWidth <= 0 || meta.frameHeight <= 0 ||
                (long)meta.frameWidth * meta.frameCount != texture.width || meta.frameHeight != texture.height ||
                float.IsNaN(meta.footAnchor.x) || float.IsNaN(meta.footAnchor.y) ||
                meta.footAnchor.x < 0 || meta.footAnchor.x > meta.frameWidth ||
                meta.footAnchor.y < 0 || meta.footAnchor.y > meta.frameHeight)
                throw new ArgumentException("精灵图集与切帧参数不一致");
            texture.wrapMode = TextureWrapMode.Clamp;
            var frames = new Sprite[meta.frameCount];
            var pivot = new Vector2(meta.footAnchor.x / meta.frameWidth, 1 - meta.footAnchor.y / meta.frameHeight);
            for (var i = 0; i < frames.Length; i++)
                frames[i] = Sprite.Create(texture, new Rect(i * meta.frameWidth, 0, meta.frameWidth, meta.frameHeight), pivot, 100);
            return frames;
        }
    }
}
