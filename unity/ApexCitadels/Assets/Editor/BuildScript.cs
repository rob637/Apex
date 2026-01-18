using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Linq;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Build script for command-line WebGL builds.
    /// 
    /// Usage from command line:
    /// Unity -projectPath /path/to/ApexCitadels -executeMethod ApexCitadels.Editor.BuildScript.BuildWebGL -batchmode -quit
    /// 
    /// Or from Editor:
    /// Window → Apex Citadels → Build WebGL
    /// </summary>
    public class BuildScript
    {
        private static string BUILD_PATH = "../../backend/hosting-pc/build";
        private static string[] SCENES = new string[] { "Assets/Scenes/PCMain.unity" };

        [MenuItem("Window/Apex Citadels/🔨 Build WebGL", false, 100)]
        public static void BuildWebGL()
        {
            Debug.Log("[BuildScript] Starting WebGL build...");
            
            // Ensure output directory exists
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, BUILD_PATH));
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                Debug.Log($"[BuildScript] Created output directory: {fullPath}");
            }
            
            // Get scenes to build
            string[] scenesToBuild = GetScenesToBuild();
            if (scenesToBuild.Length == 0)
            {
                Debug.LogError("[BuildScript] No scenes to build! Make sure PCMain.unity exists.");
                return;
            }
            
            Debug.Log($"[BuildScript] Building scenes: {string.Join(", ", scenesToBuild)}");
            Debug.Log($"[BuildScript] Output path: {fullPath}");
            
            // Build options
            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenesToBuild,
                locationPathName = fullPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            
            // Set WebGL-specific settings
            ConfigureWebGLSettings();
            
            // Build!
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;
            
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] ✅ WebGL build succeeded!");
                Debug.Log($"[BuildScript] Total size: {summary.totalSize / (1024 * 1024):F1} MB");
                Debug.Log($"[BuildScript] Time: {summary.totalTime.TotalSeconds:F1} seconds");
                Debug.Log($"[BuildScript] Output: {fullPath}");
                
                // Show success dialog in Editor
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Build Complete",
                        $"WebGL build succeeded!\n\n" +
                        $"Size: {summary.totalSize / (1024 * 1024):F1} MB\n" +
                        $"Time: {summary.totalTime.TotalSeconds:F1}s\n" +
                        $"Output: {fullPath}\n\n" +
                        "Deploy with: firebase deploy --only hosting:pc",
                        "OK");
                }
            }
            else
            {
                Debug.LogError($"[BuildScript] ❌ WebGL build failed: {summary.result}");
                
                // Log errors
                foreach (var step in report.steps)
                {
                    foreach (var message in step.messages)
                    {
                        if (message.type == LogType.Error)
                        {
                            Debug.LogError($"[BuildScript] {message.content}");
                        }
                    }
                }
                
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Build Failed",
                        $"WebGL build failed: {summary.result}\n\nCheck Console for details.",
                        "OK");
                }
            }
        }

        [MenuItem("Window/Apex Citadels/🔨 Build WebGL (Development)", false, 101)]
        public static void BuildWebGLDevelopment()
        {
            Debug.Log("[BuildScript] Starting WebGL development build...");
            
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, BUILD_PATH));
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            
            string[] scenesToBuild = GetScenesToBuild();
            if (scenesToBuild.Length == 0)
            {
                Debug.LogError("[BuildScript] No scenes to build!");
                return;
            }
            
            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenesToBuild,
                locationPathName = fullPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development | BuildOptions.ConnectWithProfiler
            };
            
            ConfigureWebGLSettings();
            
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] ✅ Development build succeeded! ({report.summary.totalTime.TotalSeconds:F1}s)");
            }
            else
            {
                Debug.LogError($"[BuildScript] ❌ Development build failed: {report.summary.result}");
            }
        }

        private static string[] GetScenesToBuild()
        {
            // First check if PCMain.unity exists
            if (File.Exists(Path.Combine(Application.dataPath, "Scenes/PCMain.unity")))
            {
                return SCENES;
            }
            
            // Fall back to scenes in build settings
            var buildScenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
            
            if (buildScenes.Length > 0)
            {
                return buildScenes;
            }
            
            // Last resort: find any scene
            var anyScene = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            if (anyScene.Length > 0)
            {
                return new[] { AssetDatabase.GUIDToAssetPath(anyScene[0]) };
            }
            
            return new string[0];
        }

        private static void ConfigureWebGLSettings()
        {
            // Company and product
            PlayerSettings.companyName = "Apex Studios";
            PlayerSettings.productName = "Apex Citadels";
            
            // WebGL specific
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.memorySize = 512; // 512 MB
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
            
            // Use default minimal template - we replace index.html post-build
            // PlayerSettings.WebGL.template = "APPLICATION:Default";
            
            // Resolution
            PlayerSettings.defaultWebScreenWidth = 1920;
            PlayerSettings.defaultWebScreenHeight = 1080;
            PlayerSettings.runInBackground = true;
            
            // Rendering
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            
            Debug.Log("[BuildScript] WebGL settings configured");
        }

        [MenuItem("Window/Apex Citadels/📁 Open Build Folder", false, 110)]
        public static void OpenBuildFolder()
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, BUILD_PATH));
            if (Directory.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Build Folder",
                    $"Build folder doesn't exist yet.\nRun a build first!\n\nExpected: {fullPath}",
                    "OK");
            }
        }
    }
}
