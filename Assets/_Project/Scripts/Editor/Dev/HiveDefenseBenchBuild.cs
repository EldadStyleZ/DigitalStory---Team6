using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DigitalStory.Editor
{
    /// <summary>
    /// Development tool. Builds a development player of the F-010 scene for the benchmark (HiveDefenseBenchmark), so the
    /// profiler markers it reads exist. It passes the development option to this one build only and changes no project
    /// or build setting. The release player comes from <see cref="HiveDefenseBuild.BuildMac"/>.
    /// </summary>
    public static class HiveDefenseBenchBuild
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const string PathArgument = "-f010BuildPath";

        /// <summary>Run with -batchmode -executeMethod and "-f010BuildPath &lt;folder/Name.app&gt;".</summary>
        public static void BuildMacDevelopment()
        {
            string output = ReadArgument(PathArgument);
            if (string.IsNullOrEmpty(output))
            {
                throw new ArgumentException($"Pass {PathArgument} <path to the .app> on the command line.");
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development,
            };

            BuildReport report = HiveDefenseBuild.BuildWithFrameTimings(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[F010] Development player build failed: {report.summary.result}, {report.summary.totalErrors} errors.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            Debug.Log($"[F010] Player built: {output}");
        }

        private static string ReadArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index] == name)
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }
    }
}
