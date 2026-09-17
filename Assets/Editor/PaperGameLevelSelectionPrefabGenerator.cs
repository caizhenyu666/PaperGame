using System.IO;
using UnityEditor;
using UnityEngine;

namespace PaperGame.C1.Editor
{
    public static class PaperGameLevelSelectionPrefabGenerator
    {
        public const string PrefabPath = "Assets/Resources/C1UI/PaperGameLevelSelection.prefab";

        [MenuItem("PaperGame/UI/Generate Level Selection")]
        public static void Generate()
        {
            Directory.CreateDirectory("Assets/Resources/C1UI");
            var root = C1LevelSelectionLayout.Build(null);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("Level selection prefab generated: " + PrefabPath);
        }
    }
}
