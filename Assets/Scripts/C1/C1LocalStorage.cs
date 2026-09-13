using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PaperGame.C1
{
    public sealed class C1LocalStorage : MonoBehaviour
    {
        private bool waiting;
        public string Error { get; private set; }
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void PaperGame_SyncLevelStorage(string receiver);
#endif
        public IEnumerator Flush()
        {
            Error = null;
#if UNITY_WEBGL && !UNITY_EDITOR
            waiting = true;
            PaperGame_SyncLevelStorage(gameObject.name);
            // Wait for IndexedDB acknowledgement before publishing success or changing scene.
            while (waiting) yield return null;
#else
            yield break;
#endif
        }
        public void ReceiveStorageResult(string error)
        {
            Error = string.IsNullOrEmpty(error) ? null : "本地保存失败，请检查浏览器存储空间后重试：" + error;
            waiting = false;
        }
    }
}
