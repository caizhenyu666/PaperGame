using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1.Editor
{
    public static class PaperGameLevelDrawingTutorialPrefabGenerator
    {
        public const string PrefabPath = "Assets/Resources/C1UI/PaperGameLevelDrawingTutorial.prefab";
        private const string TutorialArtPath = "Assets/Art/UI/LevelDrawingTutorial/";
        private const string HomeBackgroundPath = "Assets/Art/UI/PictureBook/home-background.png";
        private const string PaperLabelPath = "Assets/Resources/C1GameUI/paper-label.png";

        private static readonly string[] Titles =
        {
            "横着放好一张白纸",
            "画小英雄的起点",
            "画平平的冒险道路",
            "留出能跳过的小空隙",
            "给冒险画一个终点",
            "把所有画画装进白框"
        };

        private static readonly string[] MainCopy =
        {
            "让我们一起画一个冒险世界！",
            "画一个清楚的空心圆圈，下面接一条路。",
            "道路要清楚、连续，也不要太短。",
            "别让道路离得太远，也别一下变得太高。",
            "画一根直旗杆，再在旁边画一个三角旗面。",
            "整张画都要待在框里，不要碰到框边。"
        };

        private static readonly string[] Rules =
        {
            "• 把白纸横着放\n• 四周留出一点空白",
            "• 整张纸只画 1 个起点\n• 圆圈保持空心\n• 圆圈下面必须有平台",
            "• 画接近水平的长线\n• 不画斜坡\n• 不画太短或断开的线",
            "• 可以留一点小空隙\n• 平台不要离得太远\n• 高低变化不要太大",
            "• 旗杆近似竖直\n• 旗面与旗杆相连\n• 颜色随你喜欢，只画 1 面",
            "• 所有内容完整进入白框\n• 不要贴着框边\n• 小手和影子不要挡住画"
        };

        private static readonly C1TutorialAnimationKind[] Animations =
        {
            C1TutorialAnimationKind.PaperRise,
            C1TutorialAnimationKind.Bob,
            C1TutorialAnimationKind.Drift,
            C1TutorialAnimationKind.Jump,
            C1TutorialAnimationKind.Sway,
            C1TutorialAnimationKind.Pulse
        };

        [MenuItem("PaperGame/UI/Generate Level Drawing Tutorial")]
        public static void GenerateTutorialPrefab()
        {
            Directory.CreateDirectory("Assets/Resources/C1UI");
            ConfigureSprites();

            var root = new GameObject("PaperGameLevelDrawingTutorial", typeof(RectTransform), typeof(Image), typeof(AudioSource));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);
            var background = root.GetComponent<Image>();
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HomeBackgroundPath);
            background.preserveAspect = false;
            root.AddComponent<C1CoverBackground>();

            AddPanel(root.transform, "Warm Wash", Vector2.zero, Vector2.one, new Color(1f, .97f, .83f, .56f), false);
            var pages = AddRect(root.transform, "Pages", new Vector2(.05f, .14f), new Vector2(.95f, .87f));
            for (var i = 0; i < 6; i++) CreatePage(pages.transform, i);

            var dots = AddRect(root.transform, "Page Dots", new Vector2(.37f, .055f), new Vector2(.63f, .105f));
            var layout = dots.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            for (var i = 0; i < 6; i++)
            {
                var dot = AddPanel(dots.transform, "Dot " + (i + 1), Vector2.zero, Vector2.zero, new Color(.17f, .49f, .88f, i == 0 ? 1f : .35f), false);
                dot.GetComponent<RectTransform>().sizeDelta = new Vector2(i == 0 ? 34f : 18f, 18f);
                dot.AddComponent<LayoutElement>();
            }

            AddButton(root.transform, "Skip", "先跳过", new Vector2(.82f, .87f), new Vector2(.95f, .95f), new Color(1f, .96f, .78f));
            AddButton(root.transform, "Sound", "声音", new Vector2(.70f, .87f), new Vector2(.81f, .95f), new Color(.77f, .90f, 1f));
            AddButton(root.transform, "Next", "下一步", new Vector2(.72f, .05f), new Vector2(.94f, .145f), new Color(1f, .77f, .18f));
            root.AddComponent<C1LevelDrawingTutorialController>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateFromCommandLine()
        {
            GenerateTutorialPrefab();
            EditorApplication.Exit(0);
        }

        private static void CreatePage(Transform parent, int index)
        {
            var page = AddRect(parent, "Page " + (index + 1), Vector2.zero, Vector2.one);
            var art = AddImage(page.transform, "Scene Art", TutorialArtPath + $"tutorial-page-{index + 1:00}.png", new Vector2(.02f, .05f), new Vector2(.65f, .92f));
            art.preserveAspect = true;
            art.gameObject.AddComponent<C1LevelDrawingTutorialPageAnimator>().Configure(Animations[index]);

            var copyPanel = AddImage(page.transform, "Copy Paper", PaperLabelPath, new Vector2(.62f, .08f), new Vector2(.98f, .89f));
            copyPanel.preserveAspect = false;
            copyPanel.color = new Color(1f, 1f, 1f, .95f);
            AddText(copyPanel.transform, "Title", Titles[index], 48, new Vector2(.08f, .69f), new Vector2(.92f, .94f), TextAnchor.MiddleCenter, new Color(.12f, .12f, .10f));
            AddText(copyPanel.transform, "Main Copy", MainCopy[index], 34, new Vector2(.10f, .43f), new Vector2(.90f, .70f), TextAnchor.MiddleLeft, new Color(.13f, .16f, .12f));
            AddText(copyPanel.transform, "Rules", Rules[index], 28, new Vector2(.10f, .09f), new Vector2(.90f, .45f), TextAnchor.UpperLeft, new Color(.23f, .29f, .20f));
            page.SetActive(index == 0);
        }

        private static void ConfigureSprites()
        {
            ConfigureSprite(HomeBackgroundPath);
            ConfigureSprite(PaperLabelPath);
            for (var i = 1; i <= 6; i++) ConfigureSprite(TutorialArtPath + $"tutorial-page-{i:00}.png");
        }

        private static void ConfigureSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing tutorial image: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static Image AddImage(Transform parent, string name, string assetPath, Vector2 anchorMin, Vector2 anchorMax)
        {
            var child = AddRect(parent, name, anchorMin, anchorMax);
            var image = child.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            image.raycastTarget = false;
            return image;
        }

        private static GameObject AddPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color, bool raycast)
        {
            var child = AddRect(parent, name, anchorMin, anchorMax);
            var image = child.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return child;
        }

        private static Button AddButton(Transform parent, string name, string title, Vector2 anchorMin, Vector2 anchorMax, Color tint)
        {
            var image = AddImage(parent, name, PaperLabelPath, anchorMin, anchorMax);
            image.color = tint;
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            var label = AddText(image.transform, "Label", title, 30, new Vector2(.08f, .10f), new Vector2(.92f, .90f), TextAnchor.MiddleCenter, new Color(.12f, .12f, .10f));
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 20;
            label.resizeTextMaxSize = 30;
            return button;
        }

        private static Text AddText(Transform parent, string name, string value, int size, Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment, Color color)
        {
            var child = AddRect(parent, name, anchorMin, anchorMax);
            var text = child.AddComponent<Text>();
            text.text = value;
            text.font = C1UiFont.Load();
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject AddRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return child;
        }
    }
}
