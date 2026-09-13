using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PaperGame.C1.Editor
{
    public sealed class C1WebGLBuildTimestamp : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;

            var indexPath = Path.Combine(report.summary.outputPath, "index.html");
            var html = File.ReadAllText(indexPath);
            const string timestampElement = "(<div id=\"build-time\">)[^<]*(</div>)";
            if (!Regex.IsMatch(html, timestampElement))
                throw new BuildFailedException("WebGL 页面缺少出包时间占位符，请使用 PaperGameMobile 模板。");

            var timestamp = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8))
                .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            File.WriteAllText(indexPath, Regex.Replace(html, timestampElement,
                match => match.Groups[1].Value + "出包时间：" + timestamp + " UTC+8" + match.Groups[2].Value));
        }
    }

    public static class C1WebGLBuilder
    {
        /// <summary>
        /// 服务端 WebGL 部署目录，相对于 Unity 项目根目录。
        /// </summary>
        private const string ServerWebGlDir = "../PaperGmaeSever/webgl";

        [MenuItem("PaperGame/C1/Build WebGL")]
        public static void BuildFromMenu()
        {
            Build(ConfiguredOutputPath());
        }

        public static void BuildFromCommandLine()
        {
            var outputPath = Environment.GetEnvironmentVariable("C1_WEBGL_OUTPUT");
            Build(string.IsNullOrWhiteSpace(outputPath) ? ConfiguredOutputPath() : outputPath);
        }

        private static void Build(string outputPath)
        {
            var resolvedOutputPath = ResolveOutputPath(outputPath);
            PlayerSettings.WebGL.template = "PROJECT:PaperGameMobile";

            var options = new BuildPlayerOptions
            {
                scenes = Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled)
                    .Select(scene => scene.path).ToArray(),
                locationPathName = resolvedOutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"C1 WebGL build failed at {resolvedOutputPath} with result {report.summary.result} and {report.summary.totalErrors} errors.");
            }

            Debug.Log("[PaperGame] WebGL build succeeded: " + resolvedOutputPath);

            DeployToServer(resolvedOutputPath);
        }

        /// <summary>
        /// 把构建产物复制到服务端 webgl/ 目录，方便直接上传发布。
        /// </summary>
        private static void DeployToServer(string buildOutputPath)
        {
            var projectRoot = Path.GetDirectoryName(Path.GetFullPath(Application.dataPath));
            var serverWebGl = Path.GetFullPath(Path.Combine(projectRoot, ServerWebGlDir));

            if (!Directory.Exists(serverWebGl))
            {
                Debug.LogWarning("[PaperGame] 服务端 webgl 目录不存在，跳过部署: " + serverWebGl);
                return;
            }

            CopyDirectory(buildOutputPath, serverWebGl);
            Debug.Log("[PaperGame] WebGL 已部署到服务端: " + serverWebGl);
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
            }
        }

        private static string ConfiguredOutputPath()
        {
            return C1WebGLBuildSettings.Load()?.WebGlOutputPath
                ?? C1WebGLBuildSettings.DefaultWebGlOutputPath;
        }

        private static string ResolveOutputPath(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new InvalidOperationException("WebGL 输出目录不能为空。");
            }

            return Path.GetFullPath(outputPath);
        }
    }
}
