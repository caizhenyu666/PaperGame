using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1
{
    /// <summary>关卡选择页的 1920×1080 静态布局，供 Editor 生成正式 Prefab。</summary>
    public static class C1LevelSelectionLayout
    {
        private static Sprite CharacterArt(string name) => Resources.Load<Sprite>("C1CharacterUI/" + name);
        private static Sprite GameArt(string name) => Resources.Load<Sprite>("C1GameUI/" + name);

        public static GameObject Build(Transform parent)
        {
            var root = new GameObject("PaperGameLevelSelection", typeof(RectTransform));
            if (parent != null) root.transform.SetParent(parent, false);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
            var t = root.transform;

            ImageAt(t, "Background", CharacterArt("background"), 0, 0, 1920, 1080, false, false);
            ImageAt(t, "Cloud Left", CharacterArt("cloud"), 80, 25, 190, 115);
            ImageAt(t, "Sun", CharacterArt("sun"), 390, 20, 190, 160);
            ImageAt(t, "Cloud Right", CharacterArt("cloud"), 1260, 25, 185, 115);
            ImageAt(t, "Tree Left", CharacterArt("tree-left"), 0, 720, 200, 360);
            ImageAt(t, "Tree Right", CharacterArt("tree-right"), 1760, 720, 160, 360);
            ImageAt(t, "Bottom Grass", CharacterArt("bottom-grass"), 0, 1020, 1920, 60, false, false);

            var title = ImageAt(t, "Title", GameArt("paper-label"), 650, 20, 620, 150);
            TextAt(title.transform, "Label", "我的纸上关卡", 54, 35, 15, 550, 115, TextAnchor.MiddleCenter, 620, 150);

            var help = ImageAt(t, "Drawing Help", GameArt("paper-label"), 1325, 42, 230, 92, true);
            help.gameObject.AddComponent<Button>().targetGraphic = help;
            TextAt(help.transform, "Label", "怎么画？", 32, 18, 8, 194, 72, TextAnchor.MiddleCenter, 230, 92);

            var back = ImageAt(t, "Back Home", CharacterArt("back"), 1550, 28, 330, 102, true);
            back.gameObject.AddComponent<Button>().targetGraphic = back;

            var book = ImageAt(t, "Level Book", CharacterArt("notebook"), 70, 180, 560, 690, false, false);
            var viewport = Box(book.transform, "Viewport", 82, 70, 420, 430, 560, 690);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            var content = Box(viewport, "Content", 0, 0, 420, 430, 420, 430);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 7, 7);
            layout.spacing = 12;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;

            var template = ImageAt(content, "Level Item Template", CharacterArt("paper"), 0, 0, 406, 132, true, false, 420, 132);
            template.gameObject.AddComponent<Button>().targetGraphic = template;
            template.gameObject.AddComponent<LayoutElement>().preferredHeight = 132;
            ImageAt(template.transform, "Selection", CharacterArt("selected"), 0, 0, 406, 132, false, false, 406, 132);
            RawImageAt(template.transform, "Thumbnail", 24, 18, 120, 96, 406, 132);
            TextAt(template.transform, "Label", "第 1 页", 26, 155, 23, 180, 48, TextAnchor.MiddleLeft, 406, 132);
            TextAt(template.transform, "Source", "内置关卡", 18, 155, 70, 170, 34, TextAnchor.MiddleLeft, 406, 132);
            ImageAt(template.transform, "Star", CharacterArt("star-on"), 335, 30, 54, 61, false, true, 406, 132);
            template.gameObject.SetActive(false);

            var create = ImageAt(book.transform, "Create Level", CharacterArt("paper"), 82, 530, 420, 125, true, false, 560, 690);
            create.gameObject.AddComponent<Button>().targetGraphic = create;
            ImageAt(create.transform, "Icon", CharacterArt("add"), 35, 20, 92, 85, false, true, 420, 125);
            TextAt(create.transform, "Label", "拍照创建新关卡", 28, 126, 18, 265, 88, TextAnchor.MiddleCenter, 420, 125);

            var previewPaper = ImageAt(t, "Preview Paper", CharacterArt("preview-paper"), 660, 185, 1110, 675, false, false);
            RawImageAt(previewPaper.transform, "Level Preview", 85, 65, 940, 545, 1110, 675, true);

            var statusPaper = ImageAt(t, "Status Paper", GameArt("paper-label"), 650, 895, 650, 105, false, false);
            var status = TextAt(t, "Status", "请选择一个关卡", 24, 690, 912, 570, 72, TextAnchor.MiddleCenter);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            status.resizeTextForBestFit = true;
            status.resizeTextMinSize = 16;
            status.resizeTextMaxSize = 24;
            statusPaper.raycastTarget = false;

            var regenerate = TextButton(t, "Regenerate", "重新生成", new Color(.55f, .79f, .42f), 1280, 895, 260, 105, 31);
            regenerate.SetActive(false);
            TextButton(t, "Play", "开始关卡  →", new Color(1f, .78f, .18f), 1495, 875, 365, 135, 38);

            root.AddComponent<C1LevelSelection>();
            root.AddComponent<C1LevelSelectionScreenFit>();
            return root;
        }

        private static GameObject TextButton(Transform parent, string name, string label, Color color,
            float x, float y, float w, float h, int size)
        {
            var image = ImageAt(parent, name, GameArt("paper-label"), x, y, w, h, true);
            image.color = color;
            image.gameObject.AddComponent<Button>().targetGraphic = image;
            TextAt(image.transform, "Label", label, size, 20, 10, w - 40, h - 20, TextAnchor.MiddleCenter, w, h);
            return image.gameObject;
        }

        private static RectTransform Box(Transform parent, string name, float x, float y, float w, float h,
            float parentWidth = 1920, float parentHeight = 1080)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(x / parentWidth, 1 - (y + h) / parentHeight);
            rect.anchorMax = new Vector2((x + w) / parentWidth, 1 - y / parentHeight);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Image ImageAt(Transform parent, string name, Sprite sprite, float x, float y, float w, float h,
            bool raycast = false, bool preserveAspect = true, float parentWidth = 1920, float parentHeight = 1080)
        {
            var image = Box(parent, name, x, y, w, h, parentWidth, parentHeight).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = raycast;
            return image;
        }

        private static RawImage RawImageAt(Transform parent, string name, float x, float y, float w, float h,
            float parentWidth, float parentHeight, bool fit = false)
        {
            var image = Box(parent, name, x, y, w, h, parentWidth, parentHeight).gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.color = Color.clear;
            if (fit)
            {
                var aspect = image.gameObject.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            }
            return image;
        }

        private static Text TextAt(Transform parent, string name, string value, int size, float x, float y,
            float w, float h, TextAnchor alignment, float parentWidth = 1920, float parentHeight = 1080)
        {
            var text = Box(parent, name, x, y, w, h, parentWidth, parentHeight).gameObject.AddComponent<Text>();
            text.font = C1UiFont.Load();
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(.20f, .18f, .14f);
            text.raycastTarget = false;
            return text;
        }
    }
}
