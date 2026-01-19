// ============================================================================
// APEX CITADELS - AAA SCENE SETUP WIZARD
// One-click setup for PC scene with all required components
// ============================================================================
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Editor wizard that sets up a complete AAA-quality PC scene with all required components.
    /// Menu: Tools > Apex Citadels > AAA Scene Setup
    /// </summary>
    public class AAASceneSetup : EditorWindow
    {
        private bool setupSkybox = true;
        private bool setupLighting = true;
        private bool setupPostProcessing = true;
        private bool setupMapSystem = true;
        private bool setupCamera = true;
        private bool setupUI = true;
        private bool setupAudio = true;

        private Vector2 scrollPos;

        [MenuItem("Tools/Apex Citadels/AAA Scene Setup Wizard", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<AAASceneSetup>("AAA Scene Setup");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        [MenuItem("Tools/Apex Citadels/Quick Setup (All Features)", false, 2)]
        public static void QuickSetupAll()
        {
            if (EditorUtility.DisplayDialog("AAA Scene Setup",
                "This will set up the current scene with all AAA features:\n\n" +
                "• HDR Skybox with Day/Night Cycle\n" +
                "• Real-world Map Tiles (OpenStreetMap)\n" +
                "• Post-processing (Bloom, Color Grading)\n" +
                "• Atmospheric Lighting\n" +
                "• Audio System\n" +
                "• Complete UI\n\n" +
                "Continue?", "Setup Scene", "Cancel"))
            {
                SetupCompleteScene();
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("🏰 Apex Citadels - AAA Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This wizard sets up your PC scene with all required components for AAA quality visuals.",
                MessageType.Info);

            GUILayout.Space(10);

            // Feature toggles
            GUILayout.Label("Features to Setup:", EditorStyles.boldLabel);
            setupSkybox = EditorGUILayout.Toggle("🌅 HDR Skybox + Day/Night", setupSkybox);
            setupLighting = EditorGUILayout.Toggle("☀️ Atmospheric Lighting", setupLighting);
            setupPostProcessing = EditorGUILayout.Toggle("✨ Post-Processing", setupPostProcessing);
            setupMapSystem = EditorGUILayout.Toggle("🗺️ Real-World Map Tiles", setupMapSystem);
            setupCamera = EditorGUILayout.Toggle("📷 Camera Controllers", setupCamera);
            setupUI = EditorGUILayout.Toggle("🖥️ UI System", setupUI);
            setupAudio = EditorGUILayout.Toggle("🔊 Audio System", setupAudio);

            GUILayout.Space(20);

            // Status checks
            GUILayout.Label("Current Scene Status:", EditorStyles.boldLabel);
            DrawStatusCheck("SkyboxEnvironmentSystem", FindFirstObjectByType<PC.Visual.SkyboxEnvironmentSystem>() != null);
            DrawStatusCheck("RealWorldMapRenderer", FindFirstObjectByType<PC.GeoMapping.RealWorldMapRenderer>() != null);
            DrawStatusCheck("Post-Processing Volume", FindFirstObjectByType<Volume>() != null);
            DrawStatusCheck("Directional Light", FindFirstObjectByType<Light>()?.type == LightType.Directional);
            DrawStatusCheck("Main Camera", Camera.main != null);
            DrawStatusCheck("EventSystem", FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null);

            GUILayout.Space(20);

            // Action buttons
            if (GUILayout.Button("🚀 Setup Selected Features", GUILayout.Height(40)))
            {
                SetupSelectedFeatures();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Setup ALL Features", GUILayout.Height(30)))
            {
                SetupCompleteScene();
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "After setup, enter Play Mode to see the real-world map tiles loading.",
                MessageType.Info);

            GUILayout.Space(10);

            // Quick fixes
            GUILayout.Label("Quick Fixes:", EditorStyles.boldLabel);

            if (GUILayout.Button("Fix Missing Skybox"))
            {
                FixSkybox();
            }

            if (GUILayout.Button("Fix Lighting"))
            {
                FixLighting();
            }

            if (GUILayout.Button("Verify Firebase Setup"))
            {
                VerifyFirebase();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatusCheck(string name, bool exists)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(exists ? "✅" : "❌", GUILayout.Width(20));
            GUILayout.Label(name);
            EditorGUILayout.EndHorizontal();
        }

        private void SetupSelectedFeatures()
        {
            Undo.SetCurrentGroupName("AAA Scene Setup");

            if (setupSkybox) SetupSkyboxSystem();
            if (setupLighting) SetupAtmosphericLighting();
            if (setupPostProcessing) SetupPostProcessingStack();
            if (setupMapSystem) SetupMapSystem();
            if (setupCamera) SetupCameraSystem();
            if (setupUI) SetupUISystem();
            if (setupAudio) SetupAudioSystem();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[AAA Setup] Scene setup complete! Enter Play Mode to test.");
        }

        public static void SetupCompleteScene()
        {
            Undo.SetCurrentGroupName("AAA Complete Scene Setup");

            SetupSkyboxSystem();
            SetupAtmosphericLighting();
            SetupPostProcessingStack();
            SetupMapSystem();
            SetupCameraSystem();
            SetupUISystem();
            SetupAudioSystem();
            SetupGameManagers();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[AAA Setup] Complete scene setup finished! Enter Play Mode to test.");
        }

        #region Skybox Setup

        private static void SetupSkyboxSystem()
        {
            // Check if already exists
            var existing = FindFirstObjectByType<PC.Visual.SkyboxEnvironmentSystem>();
            if (existing != null)
            {
                Debug.Log("[AAA Setup] SkyboxEnvironmentSystem already exists");
                return;
            }

            // Create skybox system
            GameObject skyboxObj = new GameObject("SkyboxEnvironmentSystem");
            skyboxObj.AddComponent<PC.Visual.SkyboxEnvironmentSystem>();
            Undo.RegisterCreatedObjectUndo(skyboxObj, "Create Skybox System");

            // Load and apply HDR skybox material
            ApplyHDRSkybox();

            Debug.Log("[AAA Setup] Created SkyboxEnvironmentSystem with day/night cycle");
        }

        private static void ApplyHDRSkybox()
        {
            // Try to load skybox texture from Resources
            Texture2D skyTex = Resources.Load<Texture2D>("PC/Skyboxes/SKY02");
            if (skyTex == null)
            {
                // Try from Art folder
                string[] guids = AssetDatabase.FindAssets("SKY02 t:Texture2D", new[] { "Assets/Art/Skyboxes" });
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }

            if (skyTex != null)
            {
                // Create panoramic skybox material
                Shader panoramicShader = Shader.Find("Skybox/Panoramic");
                if (panoramicShader != null)
                {
                    Material skyboxMat = new Material(panoramicShader);
                    skyboxMat.SetTexture("_MainTex", skyTex);
                    skyboxMat.SetFloat("_Exposure", 1.3f);

                    // Save the material
                    string matPath = "Assets/Materials/Skybox_HDR.mat";
                    Directory.CreateDirectory(Path.GetDirectoryName(matPath));
                    AssetDatabase.CreateAsset(skyboxMat, matPath);

                    RenderSettings.skybox = skyboxMat;
                    Debug.Log("[AAA Setup] Applied HDR panoramic skybox");
                }
            }
            else
            {
                // Fallback to procedural
                Shader procShader = Shader.Find("Skybox/Procedural");
                if (procShader != null)
                {
                    Material skyboxMat = new Material(procShader);
                    skyboxMat.SetFloat("_SunSize", 0.04f);
                    skyboxMat.SetFloat("_AtmosphereThickness", 1.0f);
                    skyboxMat.SetColor("_SkyTint", new Color(0.4f, 0.6f, 0.9f));
                    skyboxMat.SetFloat("_Exposure", 1.3f);

                    string matPath = "Assets/Materials/Skybox_Procedural.mat";
                    Directory.CreateDirectory(Path.GetDirectoryName(matPath));
                    AssetDatabase.CreateAsset(skyboxMat, matPath);

                    RenderSettings.skybox = skyboxMat;
                    Debug.Log("[AAA Setup] Created procedural skybox (no HDR texture found)");
                }
            }
        }

        private static void FixSkybox()
        {
            ApplyHDRSkybox();
            Debug.Log("[AAA Setup] Skybox fixed!");
        }

        #endregion

        #region Lighting Setup

        private static void SetupAtmosphericLighting()
        {
            // Find or create directional light (sun)
            Light sunLight = null;
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    sunLight = light;
                    break;
                }
            }

            if (sunLight == null)
            {
                GameObject sunObj = new GameObject("Directional Light (Sun)");
                sunLight = sunObj.AddComponent<Light>();
                sunLight.type = LightType.Directional;
                Undo.RegisterCreatedObjectUndo(sunObj, "Create Sun Light");
            }

            // Configure sun
            sunLight.color = new Color(1f, 0.95f, 0.8f);
            sunLight.intensity = 1.5f;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.8f;
            sunLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Configure ambient
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.6f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.5f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.25f, 0.2f);

            // Configure fog for atmosphere
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.7f, 0.8f, 0.9f);
            RenderSettings.fogDensity = 0.0005f;

            Debug.Log("[AAA Setup] Configured atmospheric lighting");
        }

        private static void FixLighting()
        {
            SetupAtmosphericLighting();
            Debug.Log("[AAA Setup] Lighting fixed!");
        }

        #endregion

        #region Post-Processing Setup

        private static void SetupPostProcessingStack()
        {
            // Check if volume exists
            var existingVolume = FindFirstObjectByType<Volume>();
            if (existingVolume != null)
            {
                Debug.Log("[AAA Setup] Post-processing Volume already exists");
                return;
            }

            // Create global volume
            GameObject volumeObj = new GameObject("Global Volume");
            Volume volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            Undo.RegisterCreatedObjectUndo(volumeObj, "Create Global Volume");

            // Create or load volume profile
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

            // Add Bloom
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.5f);
            bloom.scatter.Override(0.7f);

            // Add Color Adjustments
            ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.Override(0.5f);
            colorAdj.contrast.Override(10f);
            colorAdj.saturation.Override(10f);

            // Add Vignette
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.25f);
            vignette.smoothness.Override(0.5f);

            // Add Tonemapping
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            // Save profile
            string profilePath = "Assets/Settings/PostProcessing_AAA.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(profilePath));
            AssetDatabase.CreateAsset(profile, profilePath);

            volume.profile = profile;

            Debug.Log("[AAA Setup] Created post-processing volume with AAA settings");
        }

        #endregion

        #region Map System Setup

        private static void SetupMapSystem()
        {
            // Check if already exists
            var existingMap = FindFirstObjectByType<PC.GeoMapping.RealWorldMapRenderer>();
            if (existingMap != null)
            {
                Debug.Log("[AAA Setup] RealWorldMapRenderer already exists");
                return;
            }

            // Create map system container
            GameObject mapSystemObj = new GameObject("GeoMapSystem");
            Undo.RegisterCreatedObjectUndo(mapSystemObj, "Create Map System");

            // Add RealWorldMapRenderer
            mapSystemObj.AddComponent<PC.GeoMapping.RealWorldMapRenderer>();

            // Add MapTileProvider
            var tileProvider = mapSystemObj.AddComponent<PC.GeoMapping.MapTileProvider>();

            // Add OSMDataPipeline for road/building data
            mapSystemObj.AddComponent<PC.GeoMapping.OSMDataPipeline>();

            Debug.Log("[AAA Setup] Created GeoMapSystem with OpenStreetMap tiles");
            Debug.Log("[AAA Setup] Map will load real-world tiles at runtime");
        }

        #endregion

        #region Camera Setup

        private static void SetupCameraSystem()
        {
            // Check for main camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                Undo.RegisterCreatedObjectUndo(camObj, "Create Main Camera");
            }

            // Configure camera for AAA quality
            mainCam.clearFlags = CameraClearFlags.Skybox;
            mainCam.farClipPlane = 5000f;
            mainCam.allowHDR = true;
            mainCam.allowMSAA = true;

            // Position camera
            mainCam.transform.position = new Vector3(0, 200, -100);
            mainCam.transform.rotation = Quaternion.Euler(60f, 0, 0);

            // Add camera controller if not present
            if (mainCam.GetComponent<PC.PCCameraController>() == null)
            {
                mainCam.gameObject.AddComponent<PC.PCCameraController>();
            }

            // Add Universal Additional Camera Data for URP
            var urpCamData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            if (urpCamData == null)
            {
                urpCamData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            urpCamData.renderPostProcessing = true;

            Debug.Log("[AAA Setup] Configured main camera with HDR and post-processing");
        }

        #endregion

        #region UI Setup

        private static void SetupUISystem()
        {
            // Check for EventSystem
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
            }

            // Check for Canvas
            Canvas existingCanvas = FindFirstObjectByType<Canvas>();
            if (existingCanvas == null)
            {
                GameObject canvasObj = new GameObject("UI Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");

                // Add PCUIManager if not present
                if (canvasObj.GetComponent<PC.UI.PCUIManager>() == null)
                {
                    canvasObj.AddComponent<PC.UI.PCUIManager>();
                }
            }

            Debug.Log("[AAA Setup] UI system configured");
        }

        #endregion

        #region Audio Setup

        private static void SetupAudioSystem()
        {
            // Check for audio manager
            var existingAudio = FindFirstObjectByType<Audio.AudioManager>();
            if (existingAudio != null)
            {
                Debug.Log("[AAA Setup] AudioManager already exists");
                return;
            }

            // Create audio manager
            GameObject audioObj = new GameObject("AudioManager");
            audioObj.AddComponent<Audio.AudioManager>();
            Undo.RegisterCreatedObjectUndo(audioObj, "Create AudioManager");

            Debug.Log("[AAA Setup] Created AudioManager");
        }

        #endregion

        #region Game Managers Setup

        private static void SetupGameManagers()
        {
            // GameManager
            if (FindFirstObjectByType<Core.GameManager>() == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<Core.GameManager>();
                Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");
            }

            // PCSceneBootstrapper
            if (FindFirstObjectByType<PC.PCSceneBootstrapper>() == null)
            {
                GameObject bootObj = new GameObject("PCSceneBootstrapper");
                bootObj.AddComponent<PC.PCSceneBootstrapper>();
                Undo.RegisterCreatedObjectUndo(bootObj, "Create PCSceneBootstrapper");
            }

            // PCGameController
            if (FindFirstObjectByType<PC.PCGameController>() == null)
            {
                GameObject controllerObj = new GameObject("PCGameController");
                controllerObj.AddComponent<PC.PCGameController>();
                Undo.RegisterCreatedObjectUndo(controllerObj, "Create PCGameController");
            }

            // WorldMapRenderer
            if (FindFirstObjectByType<PC.WorldMapRenderer>() == null)
            {
                GameObject worldMapObj = new GameObject("WorldMapRenderer");
                worldMapObj.AddComponent<PC.WorldMapRenderer>();
                Undo.RegisterCreatedObjectUndo(worldMapObj, "Create WorldMapRenderer");
            }

            Debug.Log("[AAA Setup] Core game managers created");
        }

        #endregion

        #region Firebase Verification

        private static void VerifyFirebase()
        {
            bool hasGoogleServices = File.Exists("Assets/google-services.json");
            bool hasFirebasePlugins = Directory.Exists("Assets/Firebase/Plugins");
            bool hasDesktopConfig = File.Exists("Assets/StreamingAssets/google-services-desktop.json");

            string message = "Firebase Setup Status:\n\n";
            message += hasGoogleServices ? "✅ google-services.json found\n" : "❌ google-services.json MISSING\n";
            message += hasFirebasePlugins ? "✅ Firebase SDK installed\n" : "❌ Firebase SDK MISSING\n";
            message += hasDesktopConfig ? "✅ Desktop config found\n" : "❌ Desktop config MISSING\n";

            if (!hasGoogleServices && hasDesktopConfig)
            {
                message += "\n⚠️ Tip: Copy google-services-desktop.json to Assets/google-services.json";
            }

            EditorUtility.DisplayDialog("Firebase Verification", message, "OK");
        }

        #endregion
    }
}
