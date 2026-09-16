using System;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1
{
    public sealed class C1GameHudController : MonoBehaviour
    {
        private C1MobileControls controls;
        private GameObject pauseModal;
        private Text pageText;
        private Action returnHome;
        private float timeScaleBeforePause = 1f;

        public bool IsPaused => pauseModal != null && pauseModal.activeSelf;
        public string PageText => pageText == null ? string.Empty : pageText.text;

        public void Configure(C1PlayerController2D player, int pageNumber, Action onReturnHome)
        {
            Bind();
            returnHome = onReturnHome;
            controls.Configure(player);
            transform.Find("Move Left").GetComponent<C1TouchDirectionButton>().Configure(controls, true);
            transform.Find("Move Right").GetComponent<C1TouchDirectionButton>().Configure(controls, false);
            transform.Find("Jump").GetComponent<C1TouchJumpButton>().Configure(controls);
            pageText.text = $"第{Mathf.Max(1, pageNumber)}页";
            ContinueGame();
        }

        public void PauseGame()
        {
            Bind();
            if (IsPaused) return;
            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            controls.ClearInput();
            pauseModal.SetActive(true);
            Time.timeScale = 0f;
        }

        public void ContinueGame()
        {
            Bind();
            if (pauseModal != null) pauseModal.SetActive(false);
            Time.timeScale = Mathf.Max(.01f, timeScaleBeforePause);
        }

        public void ReturnHome()
        {
            ContinueGame();
            returnHome?.Invoke();
        }

        private void Awake()
        {
            Bind();
            transform.Find("Pause").GetComponent<Button>().onClick.AddListener(PauseGame);
            transform.Find("Pause Modal/Continue").GetComponent<Button>().onClick.AddListener(ContinueGame);
            transform.Find("Pause Modal/Return Home").GetComponent<Button>().onClick.AddListener(ReturnHome);
        }

        private void Bind()
        {
            if (controls == null) controls = GetComponent<C1MobileControls>();
            if (pageText == null) pageText = transform.Find("Page Label/Page Text").GetComponent<Text>();
            if (pauseModal == null) pauseModal = transform.Find("Pause Modal").gameObject;
        }

        private void OnDisable()
        {
            controls?.ClearInput();
            if (IsPaused) Time.timeScale = Mathf.Max(.01f, timeScaleBeforePause);
        }
    }
}
