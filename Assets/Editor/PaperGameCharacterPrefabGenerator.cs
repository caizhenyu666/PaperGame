using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Editor
{
    public static class PaperGameCharacterPrefabGenerator
    {
        public const string PrefabPath = "Assets/Resources/C1UI/PaperGameCharacterSelection.prefab";

        [MenuItem("PaperGame/UI/Generate Character Selection")]
        public static void Generate()
        {
            foreach (var path in Directory.GetFiles("Assets/Resources/C1CharacterUI", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
            }
            Directory.CreateDirectory("Assets/Resources/C1UI");
            var root = C1CharacterScreenLayout.Build(null);
            foreach (var button in root.GetComponentsInChildren<Button>())
            {
                var image = button.GetComponent<Image>();
                if (image != null) { image.raycastTarget = true; button.targetGraphic = image; }
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("Character selection prefab generated: " + PrefabPath);
        }

        public static void GenerateAndRender()
        {
            Generate();
            var root = new GameObject("Character UI Render", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var cameraObject = new GameObject("Character UI Camera", typeof(Camera));
            var target = new RenderTexture(1920, 1080, 24);
            var texture = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.targetTexture = target; camera.orthographic = true; camera.orthographicSize = 540;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                var screen = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), root.transform, false);
                var rect = screen.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                screen.transform.Find("Retry").gameObject.SetActive(false);
                Canvas.ForceUpdateCanvases();
                camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); texture.Apply();
                Directory.CreateDirectory("Docs/ui");
                File.WriteAllBytes("Docs/ui/character-selection-preview.png", texture.EncodeToPNG());
                Debug.Log("Rendered character-selection-preview.png at 1920x1080");
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(root); Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(texture); target.Release(); Object.DestroyImmediate(target);
            }
        }
    }
}
