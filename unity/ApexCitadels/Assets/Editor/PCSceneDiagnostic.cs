using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using ApexCitadels.PC;
using ApexCitadels.PC.UI;
using ApexCitadels.PC.WebGL;

namespace ApexCitadels.PC.Editor
{
    /// <summary>
    /// Diagnostic tool to check if the PC scene is set up correctly.
    /// Run from Window → Apex Citadels → Scene Diagnostic
    /// </summary>
    public class PCSceneDiagnostic : EditorWindow
    {
        private Vector2 scrollPos;
        
        [MenuItem("Window/Apex Citadels/Scene Diagnostic")]
        public static void ShowWindow()
        {
            GetWindow<PCSceneDiagnostic>("PC Scene Diagnostic");
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            EditorGUILayout.LabelField("PC Scene Diagnostic", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Check each required component
            CheckCamera();
            EditorGUILayout.Space();
            CheckLighting();
            EditorGUILayout.Space();
            CheckRenderer();
            EditorGUILayout.Space();
            CheckURP();
            EditorGUILayout.Space();
            CheckManagers();
            EditorGUILayout.Space();
            
            // Fix buttons
            EditorGUILayout.LabelField("Quick Fixes", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Fix All Issues"))
            {
                FixAllIssues();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void CheckCamera()
        {
            EditorGUILayout.LabelField("📷 CAMERA", EditorStyles.boldLabel);
            
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                EditorGUILayout.HelpBox("❌ No Main Camera found! Tag a camera as 'MainCamera'.", UnityEditor.MessageType.Error);
                if (GUILayout.Button("Create Main Camera"))
                {
                    CreateMainCamera();
                }
                return;
            }
            
            EditorGUILayout.LabelField($"✅ Main Camera: {mainCam.gameObject.name}");
            
            // Check clear flags
            if (mainCam.clearFlags == CameraClearFlags.SolidColor)
            {
                EditorGUILayout.LabelField($"  ✅ Clear Flags: Solid Color");
                EditorGUILayout.LabelField($"  ✅ Background: {ColorToHex(mainCam.backgroundColor)}");
            }
            else
            {
                EditorGUILayout.HelpBox($"⚠️ Clear Flags is '{mainCam.clearFlags}' (should be SolidColor for consistent sky)", UnityEditor.MessageType.Warning);
            }
            
            // Check position
            EditorGUILayout.LabelField($"  Position: {mainCam.transform.position}");
            EditorGUILayout.LabelField($"  Rotation: {mainCam.transform.eulerAngles}");
            
            if (mainCam.transform.position.y < 50)
            {
                EditorGUILayout.HelpBox("⚠️ Camera Y position is low. For world map, try Y=200", UnityEditor.MessageType.Warning);
            }
            
            // Check for PCCameraController
            var camController = mainCam.GetComponent<PCCameraController>();
            if (camController != null)
            {
                EditorGUILayout.LabelField($"  ✅ Has PCCameraController");
            }
            else
            {
                EditorGUILayout.HelpBox("❌ Missing PCCameraController script", UnityEditor.MessageType.Error);
                if (GUILayout.Button("Add PCCameraController"))
                {
                    mainCam.gameObject.AddComponent<PCCameraController>();
                }
            }
        }

        private void CheckLighting()
        {
            EditorGUILayout.LabelField("💡 LIGHTING", EditorStyles.boldLabel);
            
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            Light directionalLight = null;
            
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    directionalLight = light;
                    break;
                }
            }
            
            if (directionalLight == null)
            {
                EditorGUILayout.HelpBox("❌ No Directional Light found! Objects will appear unlit/dark.", UnityEditor.MessageType.Error);
                if (GUILayout.Button("Create Directional Light (Sun)"))
                {
                    CreateDirectionalLight();
                }
                return;
            }
            
            EditorGUILayout.LabelField($"✅ Directional Light: {directionalLight.gameObject.name}");
            EditorGUILayout.LabelField($"  Intensity: {directionalLight.intensity}");
            EditorGUILayout.LabelField($"  Color: {ColorToHex(directionalLight.color)}");
            
            if (directionalLight.intensity < 0.5f)
            {
                EditorGUILayout.HelpBox("⚠️ Light intensity is very low. Try 1.0 or higher.", UnityEditor.MessageType.Warning);
            }
            
            // Check ambient
            EditorGUILayout.LabelField($"  Ambient Mode: {RenderSettings.ambientMode}");
            EditorGUILayout.LabelField($"  Ambient Intensity: {RenderSettings.ambientIntensity}");
        }

        private void CheckRenderer()
        {
            EditorGUILayout.LabelField("🎨 RENDER PIPELINE", EditorStyles.boldLabel);
            
            var pipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (pipelineAsset == null)
            {
                EditorGUILayout.HelpBox("⚠️ No Render Pipeline Asset set (using Built-in Renderer)", UnityEditor.MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"✅ Pipeline: {pipelineAsset.name}");
                
                // Check if URP
                string pipelineType = pipelineAsset.GetType().Name;
                EditorGUILayout.LabelField($"  Type: {pipelineType}");
                
                if (pipelineType.Contains("Universal"))
                {
                    EditorGUILayout.LabelField("  ✅ Using Universal Render Pipeline (URP)");
                    EditorGUILayout.HelpBox("URP requires URP-compatible shaders. Use 'Universal Render Pipeline/Lit' or 'Universal Render Pipeline/Simple Lit' for materials.", UnityEditor.MessageType.Info);
                }
            }
        }

        private void CheckURP()
        {
            EditorGUILayout.LabelField("🔧 URP SHADERS", EditorStyles.boldLabel);
            
            // Check if URP shaders are available
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader urpSimpleLit = Shader.Find("Universal Render Pipeline/Simple Lit");
            Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            Shader standard = Shader.Find("Standard");
            
            EditorGUILayout.LabelField($"  URP/Lit: {(urpLit != null ? "✅ Available" : "❌ Not Found")}");
            EditorGUILayout.LabelField($"  URP/Simple Lit: {(urpSimpleLit != null ? "✅ Available" : "❌ Not Found")}");
            EditorGUILayout.LabelField($"  URP/Unlit: {(urpUnlit != null ? "✅ Available" : "❌ Not Found")}");
            EditorGUILayout.LabelField($"  Standard: {(standard != null ? "✅ Available" : "❌ Not Found")}");
            
            if (urpLit == null && urpSimpleLit == null)
            {
                EditorGUILayout.HelpBox("❌ URP shaders not found! Make sure URP package is installed.", UnityEditor.MessageType.Error);
            }
        }

        private void CheckManagers()
        {
            EditorGUILayout.LabelField("📋 MANAGERS", EditorStyles.boldLabel);
            
            // PCSceneBootstrapper
            var bootstrapper = FindFirstObjectByType<PCSceneBootstrapper>();
            EditorGUILayout.LabelField($"  PCSceneBootstrapper: {(bootstrapper != null ? "✅ Found" : "❌ Not Found")}");
            
            // WorldMapRenderer
            var worldMap = FindFirstObjectByType<WorldMapRenderer>();
            EditorGUILayout.LabelField($"  WorldMapRenderer: {(worldMap != null ? "✅ Found" : "❌ Not Found")}");
            
            // PCGameController
            var gameController = FindFirstObjectByType<PCGameController>();
            EditorGUILayout.LabelField($"  PCGameController: {(gameController != null ? "✅ Found" : "❌ Not Found")}");
            
            // PCInputManager
            var inputManager = FindFirstObjectByType<PCInputManager>();
            EditorGUILayout.LabelField($"  PCInputManager: {(inputManager != null ? "✅ Found" : "❌ Not Found")}");
            
            // PCUIManager
            var uiManager = FindFirstObjectByType<PCUIManager>();
            EditorGUILayout.LabelField($"  PCUIManager: {(uiManager != null ? "✅ Found" : "❌ Not Found")}");
        }

        private void FixAllIssues()
        {
            Debug.Log("[Diagnostic] Fixing all issues...");
            
            // 1. Ensure Main Camera
            if (Camera.main == null)
            {
                CreateMainCamera();
            }
            else
            {
                // Fix camera settings
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0.5f, 0.7f, 1f);
                Camera.main.farClipPlane = 5000f;
                
                // Ensure PCCameraController
                if (Camera.main.GetComponent<PCCameraController>() == null)
                {
                    Camera.main.gameObject.AddComponent<PCCameraController>();
                }
            }
            
            // 2. Ensure Directional Light
            Light directionalLight = null;
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    directionalLight = light;
                    break;
                }
            }
            
            if (directionalLight == null)
            {
                CreateDirectionalLight();
            }
            else
            {
                // Ensure good intensity
                if (directionalLight.intensity < 0.5f)
                {
                    directionalLight.intensity = 1.0f;
                    Debug.Log($"[Diagnostic] Set light intensity to 1.0");
                }
            }
            
            // 3. Fix Ambient Settings
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.5f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.35f, 0.3f);
            RenderSettings.ambientIntensity = 1f;
            
            Debug.Log("[Diagnostic] All fixes applied!");
            EditorUtility.DisplayDialog("Fixes Applied", "Scene setup issues have been fixed. Save your scene!", "OK");
        }

        private void CreateMainCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.5f, 0.7f, 1f);
            cam.farClipPlane = 5000f;
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<PCCameraController>();
            
            // Position for world map view
            camObj.transform.position = new Vector3(0, 200, -100);
            camObj.transform.rotation = Quaternion.Euler(70, 0, 0);
            
            Debug.Log("[Diagnostic] Created Main Camera");
        }

        private void CreateDirectionalLight()
        {
            GameObject sunObj = new GameObject("Directional Light");
            Light sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.0f;
            sun.shadows = LightShadows.Soft;
            sunObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            sunObj.AddComponent<DayNightCycle>();
            
            Debug.Log("[Diagnostic] Created Directional Light (Sun)");
        }

        private string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }
    }
}
