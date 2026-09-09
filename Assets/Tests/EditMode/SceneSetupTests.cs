using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaperGame.C1.Tests
{
    public sealed class SceneSetupTests
    {
        [Test]
        public void BuildSettings_RegistersHomeThenGame()
        {
            Assert.That(EditorBuildSettings.scenes.Select(scene => scene.path), Is.EqualTo(new[]
            {
                "Assets/Scenes/Home.unity", "Assets/Scenes/Game.unity"
            }));
        }

        [TestCase("Assets/Scenes/Home.unity", typeof(HomeBootstrap))]
        [TestCase("Assets/Scenes/Game.unity", typeof(C1GameBootstrap))]
        public void Scene_HasExpectedBootstrap(string path, System.Type expectedType)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Assert.That(Object.FindObjectOfType(expectedType), Is.Not.Null, scene.name);
        }
    }
}
