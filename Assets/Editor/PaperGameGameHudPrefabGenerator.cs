using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Editor
{
    public static class PaperGameGameHudPrefabGenerator
    {
        private const string PrefabPath = "Assets/Resources/C1UI/PaperGameGameHUD.prefab";
        private const string ArtFolder = "Assets/Resources/C1GameUI";

        [MenuItem("PaperGame/UI/Generate Game HUD")]
        public static void Generate()
        {
            ImportArt();
            var root = Build();
            Directory.CreateDirectory("Assets/Resources/C1UI");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("Game HUD prefab generated: " + PrefabPath);
        }

        [MenuItem("PaperGame/UI/Generate And Render Game HUD")]
        public static void GenerateAndRender()
        {
            Generate();
            var host = new GameObject("Game HUD Preview Host");
            var bootstrap = host.AddComponent<C1GameBootstrap>();
            bootstrap.Build(C1LevelLoader.LoadDefault());
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            var target = new RenderTexture(1920, 1080, 24);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                bootstrap.HudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                bootstrap.HudCanvas.worldCamera = camera;
                bootstrap.HudCanvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                camera.Render(); RenderTexture.active = target;
                var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0,0,1920,1080),0,0); image.Apply();
                Directory.CreateDirectory("Docs/ui");
                File.WriteAllBytes("Docs/ui/game-hud-preview.png", image.EncodeToPNG());
                Object.DestroyImmediate(image);
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                Object.DestroyImmediate(target); Object.DestroyImmediate(host);
            }
            AssetDatabase.Refresh();
            Debug.Log("Rendered Docs/ui/game-hud-preview.png at 1920x1080");
        }

        private static GameObject Build()
        {
            var root = new GameObject("PaperGameGameHUD", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(C1MobileControls));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            var page = ImageAt(root.transform, "Page Label", "paper-label", new Vector2(0,1), new Vector2(0,1),
                new Vector2(44,-28), new Vector2(286,118), false);
            var pageText = TextAt(page.transform, "Page Text", "第1页", 54, TextAnchor.MiddleCenter);
            Stretch(pageText.rectTransform, new Vector2(18,10), new Vector2(-18,-8));

            ButtonAt(root.transform, "Pause", "pause", new Vector2(1,1), new Vector2(1,1),
                new Vector2(-38,-24), new Vector2(132,132));
            ButtonAt(root.transform, "Move Left", "left", Vector2.zero, Vector2.zero,
                new Vector2(44,38), new Vector2(182,165)).AddComponent<C1TouchDirectionButton>();
            ButtonAt(root.transform, "Move Right", "right", Vector2.zero, Vector2.zero,
                new Vector2(246,38), new Vector2(197,169)).AddComponent<C1TouchDirectionButton>();
            ButtonAt(root.transform, "Jump", "jump", Vector2.right, Vector2.right,
                new Vector2(-48,34), new Vector2(220,207)).AddComponent<C1TouchJumpButton>();

            var modal = new GameObject("Pause Modal", typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(root.transform, false); Stretch(modal.GetComponent<RectTransform>());
            modal.GetComponent<Image>().color = new Color(0,0,0,.42f);

            var dialog = ImageAt(modal.transform, "Dialog Paper", null, new Vector2(.5f,.5f), new Vector2(.5f,.5f),
                Vector2.zero, new Vector2(720,430), false);
            dialog.GetComponent<Image>().sprite = Resources.Load<Sprite>("C1CharacterUI/preview-paper");
            dialog.GetComponent<Image>().color = Color.white;
            var title = TextAt(dialog.transform, "Title", "游戏暂停", 58, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(.1f,.67f); title.rectTransform.anchorMax = new Vector2(.9f,.92f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

            ButtonAt(modal.transform, "Return Home", "return-home", new Vector2(.5f,.5f), new Vector2(.5f,.5f),
                new Vector2(0,-72), new Vector2(370,114));
            var resume = ButtonAt(modal.transform, "Continue", "paper-label", new Vector2(.5f,.5f), new Vector2(.5f,.5f),
                new Vector2(0,58), new Vector2(370,114));
            var resumeText = TextAt(resume.transform, "Label", "继续游戏", 42, TextAnchor.MiddleCenter);
            Stretch(resumeText.rectTransform, new Vector2(18,6), new Vector2(-18,-6));
            modal.SetActive(false);
            root.AddComponent<C1GameHudController>();
            return root;
        }

        private static void ImportArt()
        {
            foreach (var path in Directory.GetFiles(ArtFolder, "*.png"))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
        }

        private static GameObject ButtonAt(Transform parent, string name, string art, Vector2 anchor, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            var go = ImageAt(parent, name, art, anchor, pivot, position, size, true);
            var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
            return go;
        }

        private static GameObject ImageAt(Transform parent, string name, string art, Vector2 anchor, Vector2 pivot,
            Vector2 position, Vector2 size, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.sprite = string.IsNullOrEmpty(art) ? null : Resources.Load<Sprite>("C1GameUI/" + art);
            image.preserveAspect = true; image.raycastTarget = raycast; image.color = Color.white;
            return go;
        }

        private static Text TextAt(Transform parent, string name, string value, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>(); text.font = C1UiFont.Load(); text.text = value; text.fontSize = size;
            text.alignment = alignment; text.color = new Color(.09f,.08f,.07f); text.raycastTarget = false;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 22; text.resizeTextMaxSize = size;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2? min = null, Vector2? max = null)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = min ?? Vector2.zero; rect.offsetMax = max ?? Vector2.zero;
        }
    }
}
