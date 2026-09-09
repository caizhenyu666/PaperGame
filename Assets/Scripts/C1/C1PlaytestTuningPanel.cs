using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1
{
    public sealed class C1PlaytestTuningPanel : MonoBehaviour
    {
        public const float MinimumJumpHeight = 1.4f;
        public const float MaximumJumpHeight = 18f;
        public const float JumpHeightStep = 0.1f;

        public float JumpHeight { get; private set; } = MinimumJumpHeight;

        private C1PlayerController2D player;
        private C1LevelDefinition level;
        private Text status;

        public void Configure(C1PlayerController2D targetPlayer, C1LevelDefinition targetLevel)
        {
            player = targetPlayer;
            level = targetLevel;
            CreateControls();
            ApplyJumpHeight(MinimumJumpHeight);
        }

        private void CreateControls()
        {
            var panel = new GameObject("Playtest Tuning", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -20f);
            rect.sizeDelta = new Vector2(280f, 88f);

            var slider = new GameObject("Jump Height Slider", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>();
            slider.transform.SetParent(panel.transform, false);
            slider.minValue = MinimumJumpHeight;
            slider.maxValue = MaximumJumpHeight;
            slider.value = MinimumJumpHeight;
            slider.onValueChanged.AddListener(ApplyJumpHeight);

            status = new GameObject("Status", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            status.transform.SetParent(panel.transform, false);
            status.font = C1UiFont.Load();
            status.alignment = TextAnchor.UpperRight;
            status.rectTransform.anchorMin = Vector2.zero;
            status.rectTransform.anchorMax = Vector2.one;
            status.rectTransform.offsetMin = Vector2.zero;
            status.rectTransform.offsetMax = new Vector2(0f, -28f);
        }

        private void ApplyJumpHeight(float value)
        {
            JumpHeight = Mathf.Round(value / JumpHeightStep) * JumpHeightStep;
            player.SetJumpHeight(JumpHeight);
            var result = C1Reachability.Analyze(level, JumpHeight);
            status.text = $"Jump Height: {JumpHeight:F1}\n{(result.CanReachGoal ? "Reachable" : "Needs higher jump")}";
        }
    }
}
