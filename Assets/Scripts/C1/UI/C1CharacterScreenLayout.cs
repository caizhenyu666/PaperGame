using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1
{
    /// <summary>以 1920×1080 为基准的独立切图布局，同时供编辑器生成和运行时兜底使用。</summary>
    public static class C1CharacterScreenLayout
    {
        public static Sprite Art(string name) => Resources.Load<Sprite>("C1CharacterUI/" + name);

        public static GameObject Build(Transform parent)
        {
            var root = new GameObject("PaperGameCharacterSelection", typeof(RectTransform));
            if (parent != null) root.transform.SetParent(parent, false);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
            var t = root.transform;
            ImageAt(t, "Background", "background", 0, 0, 1920, 1080, false);
            ImageAt(t, "Cloud Left", "cloud", 90, 20, 190, 115);
            ImageAt(t, "Sun", "sun", 400, 20, 190, 160);
            ImageAt(t, "Cloud Right", "cloud", 1290, 20, 185, 115);
            ImageAt(t, "Tree Left", "tree-left", 0, 720, 200, 360);
            ImageAt(t, "Tree Right", "tree-right", 1760, 720, 160, 360);
            ImageAt(t, "Bottom Grass", "bottom-grass", 0, 1020, 1920, 60, false);
            ImageAt(t, "Title", "title", 640, 25, 625, 160);
            ImageAt(t, "Back", "back", 1510, 28, 370, 114).gameObject.AddComponent<Button>();
            ImageAt(t, "Notebook", "notebook", 70, 175, 560, 645);
            var viewport = Box(t, "Characters", 158, 236, 423, 370);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport, false);
            var rect = content.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 18; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            layout.childControlWidth = true; layout.childForceExpandWidth = true;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = rect; scroll.viewport = viewport; scroll.horizontal = false;
            AddDefaultRow(content.transform, "Green", "green", true);
            AddDefaultRow(content.transform, "Chick", "chick", false);
            ImageAt(t, "Add Photo", "paper", 158, 615, 423, 155).gameObject.AddComponent<Button>();
            ImageAt(t.Find("Add Photo"), "Label", "add", 48, 25, 320, 103, true, 423, 155);
            var paper = ImageAt(t, "Preview", "preview-paper", 660, 200, 770, 630);
            ImageAt(paper.transform, "Animated Character", "green", 190, 170, 370, 360, true, 770, 630);
            ImageAt(t, "Run", "run-card", 1460, 195, 345, 310).gameObject.AddComponent<Button>();
            ImageAt(t, "Jump", "jump-card", 1475, 510, 330, 290).gameObject.AddComponent<Button>();
            foreach (var name in new[] { "Run", "Jump" })
            {
                var card = t.Find(name);
                ImageAt(card, "Character", name == "Run" ? "green-run" : "green-jump", 93, 25, 175, 178, true, name == "Run" ? 345 : 330, name == "Run" ? 310 : 290);
            }
            ImageAt(t, "Create", "create", 215, 840, 565, 175).gameObject.AddComponent<Button>();
            ImageAt(t, "Use", "use", 1100, 835, 630, 185).gameObject.AddComponent<Button>();
            TextAt(t, "Status", "", 24, 500, 1015, 1000, 50);
            TextAt(t, "Name", "", 24, 810, 765, 450, 40);
            TextAt(t, "Selected", "", 20, 120, 780, 470, 40);
            var retry = TextAt(t, "Retry", "重试 / 查询进度", 22, 1450, 800, 350, 40);
            retry.gameObject.AddComponent<Button>();
            return root;
        }

        private static void AddDefaultRow(Transform parent, string name, string character, bool selected)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<Image>().sprite = Art("paper");
            row.GetComponent<LayoutElement>().preferredHeight = 170;
            ImageAt(row.transform, "Thumbnail", character + "-thumbnail", 50, 25, 185, 130, true, 423, 170);
            if (selected) ImageAt(row.transform, "Selection", "selected", 0, 0, 423, 170, false, 423, 170);
            ImageAt(row.transform, "Star", selected ? "star-on" : "star-off", 326, 34, 68, 77, true, 423, 170);
        }

        public static RectTransform Box(Transform parent, string name, float x, float y, float w, float h, float pw = 1920, float ph = 1080)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(x / pw, 1 - (y + h) / ph);
            rect.anchorMax = new Vector2((x + w) / pw, 1 - y / ph);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image ImageAt(Transform parent, string name, string art, float x, float y, float w, float h, bool aspect = true, float pw = 1920, float ph = 1080)
        {
            var image = Box(parent, name, x, y, w, h, pw, ph).gameObject.AddComponent<Image>();
            image.sprite = Art(art); image.preserveAspect = aspect; image.raycastTarget = false;
            return image;
        }

        private static Text TextAt(Transform parent, string name, string value, int size, float x, float y, float w, float h)
        {
            var text = Box(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = C1UiFont.Load(); text.text = value; text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter; text.color = new Color(.24f, .22f, .18f); text.raycastTarget = name == "Retry";
            return text;
        }
    }
}
