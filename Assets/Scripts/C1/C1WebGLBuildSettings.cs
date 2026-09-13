using System;
using UnityEngine;

namespace PaperGame.C1
{
    [CreateAssetMenu(menuName = "PaperGame/WebGL Build Settings", fileName = "C1WebGLBuildSettings")]
    public sealed class C1WebGLBuildSettings : ScriptableObject
    {
        public const string ResourcePath = "C1WebGLBuildSettings";
        public const string AssetPath = "Assets/Resources/C1WebGLBuildSettings.asset";
        public const string DefaultCharacterServiceBaseUrl = "http://scjjysd.xyz";
        public const string DefaultWebGlOutputPath = "Builds/C1WebGL";

        [SerializeField] private string characterServiceBaseUrl = DefaultCharacterServiceBaseUrl;
        [SerializeField] private string webGlOutputPath = DefaultWebGlOutputPath;

        public string CharacterServiceBaseUrl => NormalizeUrl(characterServiceBaseUrl);
        public string WebGlOutputPath => string.IsNullOrWhiteSpace(webGlOutputPath)
            ? DefaultWebGlOutputPath
            : webGlOutputPath.Trim();

        public static C1WebGLBuildSettings Load()
        {
            return Resources.Load<C1WebGLBuildSettings>(ResourcePath);
        }

        private static string NormalizeUrl(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? DefaultCharacterServiceBaseUrl
                : value.Trim().TrimEnd('/');
        }
    }
}
