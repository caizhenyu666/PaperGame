using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace PaperGame.C1.Editor
{
    public static class C1WebGLBuilder
    {
        [MenuItem("PaperGame/C1/Build WebGL")]
        public static void BuildFromMenu()
        {
            Build("Builds/C1WebGL");
        }

        public static void BuildFromCommandLine()
        {
            var outputPath = Environment.GetEnvironmentVariable("C1_WEBGL_OUTPUT");
            Build(string.IsNullOrWhiteSpace(outputPath) ? "Builds/C1WebGL" : outputPath);
        }

        private static void Build(string outputPath)
        {
            PlayerSettings.WebGL.template = "PROJECT:PaperGameMobile";

            var options = new BuildPlayerOptions
            {
                scenes = Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled)
                    .Select(scene => scene.path).ToArray(),
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"C1 WebGL build failed with result {report.summary.result} and {report.summary.totalErrors} errors.");
            }
        }
    }
}
