using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1
{
    [RequireComponent(typeof(Image))]
    public sealed class C1CoverBackground : MonoBehaviour
    {
        private RectTransform rectTransform;
        private RectTransform parent;
        private Image image;
        private Vector2 lastParentSize = new Vector2(-1f, -1f);

        public static Vector2 CalculateSize(Vector2 imageSize, Vector2 parentSize)
        {
            if (imageSize.x <= 0f || imageSize.y <= 0f || parentSize.x <= 0f || parentSize.y <= 0f)
                return Vector2.zero;
            return imageSize * Mathf.Max(parentSize.x / imageSize.x, parentSize.y / imageSize.y);
        }

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            parent = rectTransform.parent as RectTransform;
            image = GetComponent<Image>();
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        private void Apply()
        {
            if (rectTransform == null || parent == null || image == null || image.sprite == null) return;
            if (parent.rect.size == lastParentSize) return;
            lastParentSize = parent.rect.size;
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(.5f, .5f);
            rectTransform.pivot = new Vector2(.5f, .5f);
            rectTransform.sizeDelta = CalculateSize(image.sprite.rect.size, lastParentSize);
        }
    }
}
