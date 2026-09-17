using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Editor
{
    public static class PaperGameHomePrefabGenerator
    {
        public const string PrefabPath = "Assets/Resources/C1UI/PaperGameHome.prefab";
        private const string ArtPath = "Assets/Art/UI/PictureBook/";

        [MenuItem("PaperGame/UI/Generate Picture Book Home")]
        public static void GenerateHomePrefab()
        {
            Directory.CreateDirectory("Assets/Resources/C1UI");
            ConfigureSprites();
            var root = new GameObject("PaperGameHome", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1280f, 720f);

            var background = AddImage(root.transform, "Background", "home-background", new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            background.gameObject.AddComponent<C1CoverBackground>();
            AddImage(root.transform, "Title", "title-change-hero", new Vector2(.22f, .68f), new Vector2(.78f, .92f), Vector2.zero, Vector2.zero);
            AddImage(root.transform, "Start Play", "button-primary", new Vector2(.35f, .30f), new Vector2(.65f, .50f), Vector2.zero, Vector2.zero, true);
            AddImage(root.transform, "Create Role", "button-create-hero", new Vector2(.04f, .06f), new Vector2(.30f, .24f), Vector2.zero, Vector2.zero, true);
            AddImage(root.transform, "Tutorial Card", "tutorial-card", new Vector2(.69f, .12f), new Vector2(.96f, .46f), Vector2.zero, Vector2.zero);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureSprites()
        {
            var spriteNames = new[]
            {
                "home-background",
                "title-change-hero",
                "button-primary",
                "button-create-hero",
                "tutorial-card"
            };

            foreach (var spriteName in spriteNames)
            {
                var assetPath = ArtPath + spriteName + ".png";
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    throw new FileNotFoundException("Missing UI image: " + assetPath);
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        private static Image AddImage(Transform parent, string name, string spriteName, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, bool interactive = false)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = child.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + spriteName + ".png");
            image.preserveAspect = true;
            image.raycastTarget = interactive;
            if (interactive)
            {
                child.AddComponent<Button>();
            }

            return image;
        }
    }
}
