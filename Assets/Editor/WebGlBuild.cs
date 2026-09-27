using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

namespace ShiftingMetropolis.EditorTools
{
    public static class WebGlBuild
    {
        public static void Build()
        {
            var scenes = EditorBuildSettings.scenes;
            var paths = new string[scenes.Length];
            int n = 0;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (!scenes[i].enabled) continue;
                paths[n++] = scenes[i].path;
            }
            if (n == 0)
            {
                Debug.LogError("WebGL build has no enabled scenes.");
                EditorApplication.Exit(1);
                return;
            }
            if (n != paths.Length) System.Array.Resize(ref paths, n);

            var options = new BuildPlayerOptions
            {
                scenes = paths,
                locationPathName = "Builds/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("WebGL build failed: " + report.summary.result);
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("WebGL build succeeded: " + report.summary.totalSize + " bytes");
            WriteWebAppMetadata();
            EditorApplication.Exit(0);
        }

        static void WriteWebAppMetadata()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "WebGL");
            string indexPath = Path.Combine(root, "index.html");
            if (File.Exists(indexPath))
            {
                string html = File.ReadAllText(indexPath);
                string marker = "<link rel=\"manifest\" href=\"manifest.webmanifest\">";
                if (!html.Contains(marker))
                {
                    html = html.Replace("<link rel=\"shortcut icon\" href=\"TemplateData/favicon.ico\">",
                        "<link rel=\"shortcut icon\" href=\"TemplateData/favicon.ico\">\n    " + marker +
                        "\n    <link rel=\"apple-touch-icon\" href=\"TemplateData/favicon.ico\">");
                    File.WriteAllText(indexPath, html);
                }
                html = html.Replace("// config.devicePixelRatio = 1;", "config.devicePixelRatio = 1;");
                File.WriteAllText(indexPath, html);
            }

            string manifest = "{\n" +
                "  \"name\": \"タケスタディクエスト\",\n" +
                "  \"short_name\": \"タケスタディ\",\n" +
                "  \"start_url\": \".\",\n" +
                "  \"display\": \"standalone\",\n" +
                "  \"background_color\": \"#10213b\",\n" +
                "  \"theme_color\": \"#10213b\",\n" +
                "  \"icons\": [{\"src\": \"TemplateData/favicon.ico\", \"sizes\": \"any\", \"type\": \"image/x-icon\"}]\n" +
                "}\n";
            File.WriteAllText(Path.Combine(root, "manifest.webmanifest"), manifest);
        }
    }
}
