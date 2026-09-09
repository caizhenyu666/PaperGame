using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaperGame.C1.Editor
{
    public static class PaperGameSceneGenerator
    {
        [MenuItem("PaperGame/Scenes/Generate Home And Game")]
        public static void Generate()
        {
            CreateScene("Assets/Scenes/Home.unity", "Home Bootstrap", typeof(HomeBootstrap));
            CreateScene("Assets/Scenes/Game.unity", "Game Bootstrap", typeof(C1GameBootstrap));
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Home.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
            };
            AssetDatabase.SaveAssets();
        }

        private static void CreateScene(string path, string objectName, System.Type bootstrapType)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject(objectName).AddComponent(bootstrapType);
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
