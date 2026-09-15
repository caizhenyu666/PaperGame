using System;
using UnityEngine;

namespace PaperGame.C1
{
    [CreateAssetMenu(menuName = "PaperGame/WebGL Build Settings", fileName = "C1WebGLBuildSettings")]
    public sealed class C1WebGLBuildSettings : ScriptableObject
    {
        public const string ResourcePath = "C1WebGLBuildSettings";
        public const string AssetPath = "Assets/Resources/C1WebGLBuildSettings.asset";
        public const string DefaultCharacterServiceBaseUrl = "https://scjjysd.xyz";
        public const string DefaultWebGlOutputPath = "Builds/C1WebGL";
        public const string DefaultBuildVersion = "1";

        [SerializeField] private string characterServiceBaseUrl = DefaultCharacterServiceBaseUrl;
        [SerializeField] private string webGlOutputPath = DefaultWebGlOutputPath;
        [SerializeField] private string buildVersion = DefaultBuildVersion;

        public string CharacterServiceBaseUrl => NormalizeUrl(characterServiceBaseUrl);
        public string WebGlOutputPath => string.IsNullOrWhiteSpace(webGlOutputPath)
            ? DefaultWebGlOutputPath
            : webGlOutputPath.Trim();
        public string BuildVersion => string.IsNullOrWhiteSpace(buildVersion)
            ? DefaultBuildVersion
            : buildVersion.Trim();

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
