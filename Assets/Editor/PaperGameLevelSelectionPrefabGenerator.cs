using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PaperGame.C1.Editor
{
    public static class PaperGameLevelSelectionPrefabGenerator
    {
        public const string PrefabPath = "Assets/Resources/C1UI/PaperGameLevelSelection.prefab";

        [MenuItem("PaperGame/UI/Generate Level Selection")]
        public static void Generate()
        {
            Directory.CreateDirectory("Assets/Resources/C1UI");
            var root = C1LevelSelectionLayout.Build(null);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("Level selection prefab generated: " + PrefabPath);
        }

        [MenuItem("PaperGame/UI/Generate And Render Level Selection")]
        public static void GenerateAndRender()
        {
            Generate();
            RenderPreview(1920, 1080, "level-selection-preview.png");
            RenderPreview(1094, 1016, "level-selection-preview-square.png");
            RenderPreview(2560, 1080, "level-selection-preview-ultrawide.png");
            AssetDatabase.Refresh();
        }

        private static void RenderPreview(int width, int height, string filename)
        {
            var root = new GameObject("Level Selection Render", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var cameraObject = new GameObject("Level Selection Camera", typeof(Camera));
            var target = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            var tempRoot = Path.Combine(Path.GetTempPath(), "PaperGameLevelSelectionPreview-" + Guid.NewGuid().ToString("N"));
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.targetTexture = target;
                camera.orthographic = true;
                camera.orthographicSize = 540;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.96f, .93f, .84f);
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                var screen = Object.Instantiate(prefab, root.transform, false);
                screen.name = "PaperGameLevelSelection";
                screen.GetComponent<C1LevelSelection>().Configure(() => { }, false, new C1LevelLibrary(tempRoot), () => { });
                screen.GetComponent<C1LevelSelectionScreenFit>().RefreshLayout();
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                Directory.CreateDirectory("Docs/ui");
                File.WriteAllBytes("Docs/ui/" + filename, texture.EncodeToPNG());
                Debug.Log("Rendered " + filename + " at " + width + "x" + height);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(texture);
                target.Release();
                Object.DestroyImmediate(target);
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
        }
    }
}
