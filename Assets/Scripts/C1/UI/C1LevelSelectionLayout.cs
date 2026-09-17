using System;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1
{
    /// <summary>关卡选择页的 1920×1080 静态布局，供 Editor 生成正式 Prefab。</summary>
    public static class C1LevelSelectionLayout
    {
        private static Sprite CharacterArt(string name) => Resources.Load<Sprite>("C1CharacterUI/" + name);

        public static GameObject Build(Transform parent, Func<string, Sprite> levelArt = null)
        {
            levelArt = levelArt ?? (_ => null);
            var root = new GameObject("PaperGameLevelSelection", typeof(RectTransform));
            if (parent != null) root.transform.SetParent(parent, false);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
            var t = root.transform;

            ImageAt(t, "Background", CharacterArt("background"), 0, 0, 1920, 1080, false, false);
            var decorations = Box(t, "Decorations", 0, 0, 1920, 1080);
            ImageAt(decorations, "Cloud Left", CharacterArt("cloud"), 58, 26, 205, 118);
            ImageAt(decorations, "Sun", CharacterArt("sun"), 390, 18, 185, 158);
            ImageAt(decorations, "Cloud Right", CharacterArt("cloud"), 1220, 24, 200, 118);
            ImageAt(decorations, "Tree Left", CharacterArt("tree-left"), 0, 716, 200, 364);
            ImageAt(decorations, "Tree Right", CharacterArt("tree-right"), 1752, 716, 168, 364);
            ImageAt(decorations, "Bottom Grass", CharacterArt("bottom-grass"), 0, 1018, 1920, 62, false, false);

            var header = Box(t, "Header", 0, 0, 1920, 170);
            var title = ImageAt(header, "Title", levelArt("title-paper"), 630, 18, 660, 138,
                false, false, 1920, 170);
            TextAt(title.transform, "Label", "我的纸上关卡", 52, 55, 13, 550, 110,
                TextAnchor.MiddleCenter, 660, 138);

            var help = ImageAt(header, "Drawing Help", levelArt("nav-paper"), 1300, 39, 246, 96,
                true, false, 1920, 170);
            help.gameObject.AddComponent<Button>().targetGraphic = help;
            ImageAt(help.transform, "Icon", levelArt("icon-help"), 20, 18, 58, 58,
                false, true, 246, 96);
            TextAt(help.transform, "Label", "怎么画？", 29, 78, 10, 148, 74,
                TextAnchor.MiddleCenter, 246, 96);

            var back = ImageAt(header, "Back Home", levelArt("nav-paper"), 1560, 39, 300, 96,
                true, false, 1920, 170);
            back.gameObject.AddComponent<Button>().targetGraphic = back;
            ImageAt(back.transform, "Icon", levelArt("icon-back"), 22, 18, 58, 58,
                false, true, 300, 96);
            TextAt(back.transform, "Label", "回到首页", 31, 78, 10, 196, 74,
                TextAnchor.MiddleCenter, 300, 96);

            var book = ImageAt(t, "Level Book", CharacterArt("notebook"), 64, 180, 568, 700, false, false);
            var viewport = Box(book.transform, "Viewport", 83, 68, 430, 455, 568, 700);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            var content = Box(viewport, "Content", 0, 0, 430, 455, 430, 455);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 7, 7);
            layout.spacing = 13;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;

            var template = ImageAt(content, "Level Item Template", CharacterArt("paper"), 0, 0, 416, 136,
                true, false, 430, 136);
            template.gameObject.AddComponent<Button>().targetGraphic = template;
            template.gameObject.AddComponent<LayoutElement>().preferredHeight = 136;
            ImageAt(template.transform, "Selection", CharacterArt("selected"), 0, 0, 416, 136,
                false, false, 416, 136);
            var thumbnail = RawImageAt(template.transform, "Thumbnail", 22, 17, 124, 102, 416, 136);
            thumbnail.color = new Color(1f, .98f, .90f, 1f);
            TextAt(template.transform, "Label", "第 1 页", 27, 157, 20, 176, 48,
                TextAnchor.MiddleLeft, 416, 136);
            TextAt(template.transform, "Source", "内置关卡", 19, 157, 72, 170, 34,
                TextAnchor.MiddleLeft, 416, 136);
            var star = Box(template.transform, "Star", 344, 35, 50, 50, 416, 136);
            ImageAt(star, "On", levelArt("star-on"), 0, 0, 50, 50, false, true, 50, 50);
            var starOff = ImageAt(star, "Off", levelArt("star-off"), 0, 0, 50, 50,
                false, true, 50, 50);
            starOff.gameObject.SetActive(false);
            template.gameObject.SetActive(false);

            var create = ImageAt(book.transform, "Create Level", levelArt("nav-paper"), 83, 548, 430, 118,
                true, false, 568, 700);
            create.gameObject.AddComponent<Button>().targetGraphic = create;
            ImageAt(create.transform, "Icon", levelArt("icon-camera"), 28, 19, 78, 78,
                false, true, 430, 118);
            TextAt(create.transform, "Label", "拍照创建新关卡", 29, 103, 14, 294, 88,
                TextAnchor.MiddleCenter, 430, 118);

            var previewPaper = ImageAt(t, "Preview Paper", CharacterArt("preview-paper"),
                660, 184, 1114, 692, false, false);
            var levelPreview = RawImageAt(previewPaper.transform, "Level Preview", 74, 58, 966, 568,
                1114, 692, true);
            levelPreview.color = new Color(1f, .98f, .91f, 1f);
            ImageAt(previewPaper.transform, "Frame", levelArt("preview-frame"), 0, 0, 1114, 692,
                false, false, 1114, 692);

            var actionBar = Box(t, "Action Bar", 650, 882, 1215, 150);
            var statusPaper = ImageAt(actionBar, "Status Paper", levelArt("nav-paper"),
                0, 21, 590, 105, false, false, 1215, 150);
            var status = TextAt(statusPaper.transform, "Status", "请选择一个关卡", 24,
                40, 13, 510, 76, TextAnchor.MiddleCenter, 590, 105);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            status.resizeTextForBestFit = true;
            status.resizeTextMinSize = 16;
            status.resizeTextMaxSize = 24;

            var regenerate = TextButton(actionBar, "Regenerate", "重新生成", levelArt("button-secondary"),
                598, 21, 260, 105, 31, 1215, 150);
            regenerate.SetActive(false);
            TextButton(actionBar, "Play", "开始关卡  →", levelArt("button-primary"),
                868, 0, 347, 140, 38, 1215, 150);

            root.AddComponent<C1LevelSelection>();
            root.AddComponent<C1LevelSelectionScreenFit>();
            return root;
        }

        private static GameObject TextButton(Transform parent, string name, string label, Sprite sprite,
            float x, float y, float w, float h, int size, float parentWidth, float parentHeight)
        {
            var image = ImageAt(parent, name, sprite, x, y, w, h, true, false, parentWidth, parentHeight);
            image.gameObject.AddComponent<Button>().targetGraphic = image;
            TextAt(image.transform, "Label", label, size, 20, 10, w - 40, h - 20,
                TextAnchor.MiddleCenter, w, h);
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
