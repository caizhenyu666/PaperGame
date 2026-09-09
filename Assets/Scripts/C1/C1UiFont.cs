using UnityEngine;

namespace PaperGame.C1
{
    /// <summary>
    /// 统一提供运行时 UI 使用的中文字体，确保 WebGL 构建中也包含中文字符。
    /// </summary>
    public static class C1UiFont
    {
        private const string ResourcePath = "C1UI/SourceHanSansSC-Regular";
        private static Font cached;

        public static Font Load()
        {
            if (cached == null)
            {
                cached = Resources.Load<Font>(ResourcePath);
            }

            return cached;
        }
    }
}
