using System;
using UnityEngine;
using UnityEngine.UI;

namespace PaperGame.C1
{
    public sealed class C1LevelDrawingTutorialController : MonoBehaviour
    {
        public const string SeenPreferenceKey = "C1.LevelDrawingTutorial.Seen.v1";
        public const string MutedPreferenceKey = "C1.LevelDrawingTutorial.Muted.v1";

        private GameObject[] pages = Array.Empty<GameObject>();
        private Image[] pageDots = Array.Empty<Image>();
        private Button nextButton;
        private Button skipButton;
        private Text nextLabel;
        private Text skipLabel;
        private AudioSource audioSource;
        private Action afterDismiss;
        private bool completed;

        public static bool HasSeen => PlayerPrefs.GetInt(SeenPreferenceKey, 0) == 1;
        public int PageCount => pages.Length;
        public int CurrentPageIndex { get; private set; }
        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            EnsureInitialized();
            gameObject.SetActive(false);
        }

        public void Open(Action onDismiss)
        {
            EnsureInitialized();
            afterDismiss = onDismiss;
            completed = false;
            CurrentPageIndex = 0;
            gameObject.SetActive(true);
            ShowPage();
        }

        private void EnsureInitialized()
        {
            if (pages.Length > 0) return;
            pages = Children(transform.Find("Pages"));
            pageDots = ComponentsInChildren<Image>(transform.Find("Page Dots"));
            nextButton = FindButton("Next");
            skipButton = FindButton("Skip");
            nextLabel = FindLabel(nextButton);
            skipLabel = FindLabel(skipButton);
            audioSource = GetComponent<AudioSource>();

            nextButton.onClick.AddListener(Next);
            skipButton.onClick.AddListener(Skip);
        }

        public void Next()
        {
            if (completed) return;
            if (CurrentPageIndex < pages.Length - 1)
            {
                CurrentPageIndex++;
                ShowPage();
                return;
            }

            Complete();
        }

        public void Skip()
        {
            Complete();
        }

        private void Complete()
        {
            if (completed) return;
            completed = true;
            PlayerPrefs.SetInt(SeenPreferenceKey, 1);
            PlayerPrefs.Save();
            if (audioSource != null) audioSource.Stop();
            gameObject.SetActive(false);
            var callback = afterDismiss;
            afterDismiss = null;
            callback?.Invoke();
        }

        private void ShowPage()
        {
            for (var i = 0; i < pages.Length; i++) pages[i].SetActive(i == CurrentPageIndex);
            for (var i = 0; i < pageDots.Length; i++)
            {
                var color = pageDots[i].color;
                color.a = i == CurrentPageIndex ? 1f : .35f;
                pageDots[i].color = color;
            }

            if (nextLabel != null) nextLabel.text = CurrentPageIndex == pages.Length - 1 ? "我画好啦" : "下一步";
            if (skipLabel != null) skipLabel.text = afterDismiss == null ? "关闭" : "先跳过";
        }

        private Button FindButton(string childName)
        {
            var child = transform.Find(childName);
            if (child == null || child.GetComponent<Button>() == null)
            {
                throw new MissingReferenceException("Tutorial button is missing: " + childName);
            }

            return child.GetComponent<Button>();
        }

        private static Text FindLabel(Button button)
        {
            var label = button.transform.Find("Label");
            return label == null ? null : label.GetComponent<Text>();
        }

        private static GameObject[] Children(Transform parent)
        {
            if (parent == null) throw new MissingReferenceException("Tutorial pages are missing.");
            var result = new GameObject[parent.childCount];
            for (var i = 0; i < result.Length; i++) result[i] = parent.GetChild(i).gameObject;
            return result;
        }

        private static T[] ComponentsInChildren<T>(Transform parent) where T : Component
        {
            if (parent == null) throw new MissingReferenceException("Tutorial page dots are missing.");
            var result = new T[parent.childCount];
            for (var i = 0; i < result.Length; i++) result[i] = parent.GetChild(i).GetComponent<T>();
            return result;
        }
    }
}
