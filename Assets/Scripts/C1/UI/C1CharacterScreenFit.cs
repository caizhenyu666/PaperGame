using UnityEngine;

namespace PaperGame.C1
{
    /// <summary>前景使用固定设计坐标统一缩放，纸张背景铺满窗口，装饰和首尾按钮贴边。</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class C1CharacterScreenFit : MonoBehaviour
    {
        private static readonly Vector2 DesignSize = new Vector2(1920, 1080);
        private Vector2 lastViewport;
        private bool applying;

        private void OnEnable() => RefreshLayout();
        private void OnTransformParentChanged() => RefreshLayout();
        private void OnRectTransformDimensionsChange() => RefreshLayout();

        private void LateUpdate()
        {
            var viewport = transform.parent as RectTransform;
            if (viewport != null && viewport.rect.size != lastViewport) RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (applying) return;
            var viewport = transform.parent as RectTransform;
            if (viewport == null || viewport.rect.width <= 0 || viewport.rect.height <= 0) return;
            applying = true;
            try
            {
                lastViewport = viewport.rect.size;
                var scale = Mathf.Min(lastViewport.x / DesignSize.x, lastViewport.y / DesignSize.y);
                var rect = (RectTransform)transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = DesignSize;
                rect.localScale = new Vector3(scale, scale, 1);
                var extra = lastViewport / scale - DesignSize;
                var margin = extra * .5f;
                var background = transform.Find("Background") as RectTransform;
                if (background != null)
                {
                    background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one;
                    background.anchoredPosition = Vector2.zero; background.sizeDelta = extra;
                }
                Offset("Title", new Vector2(0, margin.y));
                Offset("Back", margin);
                Offset("Cloud Left", new Vector2(-margin.x, margin.y));
                Offset("Sun", new Vector2(-margin.x, margin.y));
                Offset("Cloud Right", margin);
                Offset("Tree Left", -margin);
                Offset("Tree Right", new Vector2(margin.x, -margin.y));
                Offset("Create", new Vector2(0, -margin.y));
                Offset("Use", new Vector2(0, -margin.y));
                Offset("Status", new Vector2(0, -margin.y));
                var grass = transform.Find("Bottom Grass") as RectTransform;
                if (grass != null)
                {
                    grass.sizeDelta = new Vector2(extra.x, 0);
                    grass.anchoredPosition = new Vector2(0, -margin.y);
                }
            }
            finally { applying = false; }
        }

        private void Offset(string name, Vector2 offset)
        {
            var child = transform.Find(name) as RectTransform;
            if (child != null) child.anchoredPosition = offset;
        }
    }
}
