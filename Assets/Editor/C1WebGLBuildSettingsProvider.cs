using System.IO;
using UnityEditor;
using UnityEngine;

namespace PaperGame.C1.Editor
{
    internal static class C1WebGLBuildSettingsProvider
    {
        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Paper Game", SettingsScope.Project)
            {
                guiHandler = _ => DrawSettings()
            };
        }

        private static void DrawSettings()
        {
            var settings = LoadOrCreate();
            var serializedSettings = new SerializedObject(settings);
            EditorGUILayout.PropertyField(
                serializedSettings.FindProperty("characterServiceBaseUrl"),
                new GUIContent("角色服务地址"));
            EditorGUILayout.PropertyField(
                serializedSettings.FindProperty("webGlOutputPath"),
                new GUIContent("WebGL 输出目录"));

            if (serializedSettings.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }

        private static C1WebGLBuildSettings LoadOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<C1WebGLBuildSettings>(C1WebGLBuildSettings.AssetPath);
            if (settings != null)
            {
                return settings;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(C1WebGLBuildSettings.AssetPath));
            settings = ScriptableObject.CreateInstance<C1WebGLBuildSettings>();
            AssetDatabase.CreateAsset(settings, C1WebGLBuildSettings.AssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}
