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

        [MenuItem("PaperGame/UI/Generate And Render Character Selection")]
        public static void GenerateAndRender()
        {
            Generate();
            RenderPreview(1920, 1080, "character-selection-preview.png");
            RenderPreview(1094, 1016, "character-selection-preview-square.png");
            RenderPreview(2560, 1080, "character-selection-preview-ultrawide.png");
            RenderPreview(1920, 1080, "character-generation-loading-preview.png", true);
        }

        private static void RenderPreview(int width, int height, string filename, bool showGenerationLoading = false)
        {
            var root = new GameObject("Character UI Render", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var cameraObject = new GameObject("Character UI Camera", typeof(Camera));
            var target = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
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
                if (showGenerationLoading)
                {
                    var loading = screen.transform.Find("Generation Loading");
                    loading.gameObject.SetActive(true);
                    loading.Find("Loading Status").GetComponent<Text>().text = "正在让你的涂鸦动起来…";
                    loading.Find("Loading Tip").GetComponent<Text>().text = "彩色蜡笔正在努力工作…";
                }
                Canvas.ForceUpdateCanvases();
                screen.GetComponent<C1CharacterScreenFit>().RefreshLayout();
                Canvas.ForceUpdateCanvases();
                camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                Directory.CreateDirectory("Docs/ui");
                File.WriteAllBytes("Docs/ui/" + filename, texture.EncodeToPNG());
                Debug.Log("Rendered " + filename + " at " + width + "x" + height);
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
