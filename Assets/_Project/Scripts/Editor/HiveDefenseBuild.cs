using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DigitalStory.Editor
{
    /// <summary>Builds a player that starts in the F-010 scene, without touching the shared build settings.</summary>
    public static class HiveDefenseBuild
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const string PathArgument = "-f010BuildPath";

        /// <summary>Run with -batchmode -executeMethod and "-f010BuildPath &lt;folder/Name.app&gt;".</summary>
        public static void BuildMac()
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
                options = BuildOptions.None,
            };

            BuildReport report = BuildWithFrameTimings(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[F010] Player build failed: {report.summary.result}, {report.summary.totalErrors} errors.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            Debug.Log($"[F010] Player built: {output}");
        }

        /// <summary>
        /// Builds with Frame Timing Stats on, so FrameTimingManager reports the main thread, the render thread and the GPU
        /// (plan section AD: the benchmark's load metrics, which a frame time pinned to vsync hides), then puts the project's
        /// setting back, so no shared setting changes. On Metal its cost is a timestamp per command buffer.
        /// </summary>
        internal static BuildReport BuildWithFrameTimings(BuildPlayerOptions options)
        {
            bool wasOn = PlayerSettings.enableFrameTimingStats;
            PlayerSettings.enableFrameTimingStats = true;
            try
            {
                return BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                PlayerSettings.enableFrameTimingStats = wasOn;
            }
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
