#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Editor helper for setting up Apex Citadels scenes
    /// </summary>
    public class SceneSetupEditor : EditorWindow
    {
        [MenuItem("Apex Citadels/Scene Setup Helper")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupEditor>("Scene Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Apex Citadels Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Setup Main Menu Scene", GUILayout.Height(30)))
            {
                SetupMainMenuScene();
            }

            if (GUILayout.Button("Setup AR Gameplay Scene", GUILayout.Height(30)))
            {
                SetupARGameplayScene();
            }

            if (GUILayout.Button("Setup Bootstrap Scene", GUILayout.Height(30)))
            {
                SetupBootstrapScene();
            }

            GUILayout.Space(20);
            GUILayout.Label("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Add EventSystem"))
            {
                AddEventSystem();
            }

            if (GUILayout.Button("Add Debug Tools"))
            {
                AddDebugTools();
            }

            if (GUILayout.Button("Add Service Managers"))
            {
                AddServiceManagers();
            }
        }

        private static void SetupMainMenuScene()
        {
            Debug.Log("[SceneSetup] Setting up Main Menu Scene...");

            // Ensure EventSystem exists
            AddEventSystem();

            // Create Canvas
            var canvas = CreateCanvas("MainMenuCanvas");

            // Create Panels
            CreatePanel(canvas.transform, "MainPanel", new Vector2(0, 0));
            CreatePanel(canvas.transform, "LoginPanel", new Vector2(0, 0));
            CreatePanel(canvas.transform, "RegisterPanel", new Vector2(0, 0));
            CreatePanel(canvas.transform, "SettingsPanel", new Vector2(0, 0));
            CreatePanel(canvas.transform, "LoadingPanel", new Vector2(0, 0));

            // Add MainMenuController
            var controller = canvas.AddComponent<UI.MainMenuController>();

            // Add Camera if none exists
            if (Camera.main == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                var cam = camGO.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.2f);
                camGO.AddComponent<AudioListener>();
            }

            Debug.Log("[SceneSetup] Main Menu Scene setup complete!");
            EditorUtility.SetDirty(canvas);
        }

        private static void SetupARGameplayScene()
        {
            Debug.Log("[SceneSetup] Setting up AR Gameplay Scene...");

            // Add AR Session
            var arSession = new GameObject("AR Session");
            // Note: AR Session component would be added manually as it requires AR Foundation package

            // Add XR Origin placeholder
            var xrOrigin = new GameObject("XR Origin");
            var arCamera = new GameObject("AR Camera");
            arCamera.transform.SetParent(xrOrigin.transform);
            arCamera.tag = "MainCamera";
            arCamera.AddComponent<Camera>();
            arCamera.AddComponent<AudioListener>();

            // Add Managers
            AddServiceManagers();

            // Create UI Canvas
            var canvas = CreateCanvas("GameplayCanvas");
            
            // Add HUD Controller
            canvas.AddComponent<UI.GameHUDController>();

            Debug.Log("[SceneSetup] AR Gameplay Scene setup complete!");
        }

        private static void SetupBootstrapScene()
        {
            Debug.Log("[SceneSetup] Setting up Bootstrap Scene...");

            // Add EventSystem
            AddEventSystem();

            // Add Camera
            if (Camera.main == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                var cam = camGO.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                camGO.AddComponent<AudioListener>();
            }

            // Add GameManager
            var gameManager = new GameObject("GameManager");
            gameManager.AddComponent<Core.GameManager>();

            // Add SceneLoader
            var sceneLoader = new GameObject("SceneLoader");
            sceneLoader.AddComponent<Core.SceneLoader>();

            // Create Loading UI Canvas
            var canvas = CreateCanvas("BootstrapCanvas");
            
            // Add loading text
            var loadingText = CreateText(canvas.transform, "LoadingText", "Initializing...");
            loadingText.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            Debug.Log("[SceneSetup] Bootstrap Scene setup complete!");
        }

        private static Canvas CreateCanvas(string name)
        {
            var canvasGO = new GameObject(name);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 position)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.8f);

            return panel;
        }

        private static GameObject CreateText(Transform parent, string name, string text)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);

            var rect = textGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 60);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return textGO;
        }

        private static void AddEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
                Debug.Log("[SceneSetup] Added EventSystem");
            }
        }

        private static void AddDebugTools()
        {
            // Debug Console
            if (Object.FindObjectOfType<Debug.DebugConsole>() == null)
            {
                var console = new GameObject("DebugConsole");
                console.AddComponent<Debug.DebugConsole>();
                Debug.Log("[SceneSetup] Added DebugConsole");
            }

            // Debug Overlay
            if (Object.FindObjectOfType<Debug.DebugOverlay>() == null)
            {
                var overlay = new GameObject("DebugOverlay");
                overlay.AddComponent<Debug.DebugOverlay>();
                Debug.Log("[SceneSetup] Added DebugOverlay");
            }
        }

        private static void AddServiceManagers()
        {
            var managers = new GameObject("Managers");

            // Add service managers
            var serviceLocator = new GameObject("ServiceLocator");
            serviceLocator.transform.SetParent(managers.transform);
            serviceLocator.AddComponent<Backend.ServiceLocator>();

            var spatialAnchor = new GameObject("SpatialAnchorManager");
            spatialAnchor.transform.SetParent(managers.transform);
            spatialAnchor.AddComponent<AR.SpatialAnchorManager>();

            var anchorPersistence = new GameObject("AnchorPersistenceService");
            anchorPersistence.transform.SetParent(managers.transform);
            anchorPersistence.AddComponent<Backend.AnchorPersistenceService>();

            var playerManager = new GameObject("PlayerManager");
            playerManager.transform.SetParent(managers.transform);
            playerManager.AddComponent<Player.PlayerManager>();

            var territoryManager = new GameObject("TerritoryManager");
            territoryManager.transform.SetParent(managers.transform);
            territoryManager.AddComponent<Territory.TerritoryManager>();

            var buildingManager = new GameObject("BuildingManager");
            buildingManager.transform.SetParent(managers.transform);
            buildingManager.AddComponent<Building.BuildingManager>();

            var resourceManager = new GameObject("ResourceManager");
            resourceManager.transform.SetParent(managers.transform);
            resourceManager.AddComponent<Resources.ResourceManager>();

            Debug.Log("[SceneSetup] Added Service Managers");
        }
    }
}
#endif
