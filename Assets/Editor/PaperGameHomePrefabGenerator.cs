using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PaperGame.C1.Editor
{
    public static class PaperGameHomePrefabGenerator
    {
        public const string PrefabPath = "Assets/Resources/C1UI/PaperGameHome.prefab";
        [MenuItem("PaperGame/UI/Generate Picture Book Home")]
        public static void GenerateHomePrefab()
        {
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Picture book home prefab is missing: " + PrefabPath);
            }

            var root = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            var captureLevel = root != null ? root.transform.Find("Capture Level") : null;
            if (captureLevel != null)
            {
                Object.DestroyImmediate(captureLevel.gameObject);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
        }
    }
}
