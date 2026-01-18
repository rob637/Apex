using System;
using System.Collections;
using UnityEngine;
using ApexCitadels.Core;
using ApexCitadels.PC.UI;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Main controller for the PC client.
    /// Initializes PC-specific systems and manages the game flow.
    /// </summary>
    public class PCGameController : MonoBehaviour
    {
        public static PCGameController Instance { get; private set; }

        [Header("Systems")]
        [SerializeField] private PCCameraController cameraController;
        [SerializeField] private PCInputManager inputManager;
        [SerializeField] private WorldMapRenderer worldMapRenderer;
        [SerializeField] private BaseEditor baseEditor;
        [SerializeField] private PCUIManager uiManager;

        [Header("Settings")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool showWelcomeScreen = true;

        // Events
        public event Action OnPCClientInitialized;
        public event Action<string> OnTerritorySelected;
        public event Action OnEnterBuildMode;
        public event Action OnExitBuildMode;

        // State
        private bool _isInitialized;
        private PCClientState _currentState = PCClientState.Loading;
        private string _selectedTerritoryId;

        public bool IsInitialized => _isInitialized;
        public PCClientState CurrentState => _currentState;
        public string SelectedTerritoryId => _selectedTerritoryId;

        private void Awake()
        {
            // Only run on PC, WebGL, or Editor
            if (!PlatformManager.HasKeyboardMouse)
            {
                Debug.Log("[PCGame] Not running on PC/WebGL, disabling PC controller");
                gameObject.SetActive(false);
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (autoInitialize)
            {
                StartCoroutine(InitializePCClient());
            }
        }

        /// <summary>
        /// Initialize the PC client
        /// </summary>
        public IEnumerator InitializePCClient()
        {
            Debug.Log("[PCGame] Initializing PC client...");
            SetState(PCClientState.Loading);

            // Wait for GameManager to be ready
            while (GameManager.Instance == null || !GameManager.Instance.IsInitialized)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Initialize systems
            yield return StartCoroutine(InitializeSystems());

            // Setup event bindings
            SetupEventBindings();

            // Initial state
            _isInitialized = true;
            SetState(PCClientState.WorldMap);

            Debug.Log("[PCGame] PC client initialized");
            OnPCClientInitialized?.Invoke();

            // Show welcome if first time
            if (showWelcomeScreen)
            {
                ShowWelcomeMessage();
            }
        }

        private IEnumerator InitializeSystems()
        {
            // Ensure camera is set up
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<PCCameraController>();
                if (cameraController == null)
                {
                    GameObject camObj = new GameObject("PCCameraController");
                    camObj.AddComponent<Camera>();
                    cameraController = camObj.AddComponent<PCCameraController>();
                }
            }

            yield return null;

            // Ensure input manager
            if (inputManager == null)
            {
                inputManager = FindFirstObjectByType<PCInputManager>();
                if (inputManager == null)
                {
                    GameObject inputObj = new GameObject("PCInputManager");
                    inputManager = inputObj.AddComponent<PCInputManager>();
                }
            }

            yield return null;

            // Ensure world map renderer
            if (worldMapRenderer == null)
            {
                worldMapRenderer = FindFirstObjectByType<WorldMapRenderer>();
                if (worldMapRenderer == null)
                {
                    GameObject mapObj = new GameObject("WorldMapRenderer");
                    worldMapRenderer = mapObj.AddComponent<WorldMapRenderer>();
                }
            }

            yield return null;

            // Ensure base editor
            if (baseEditor == null)
            {
                baseEditor = FindFirstObjectByType<BaseEditor>();
                if (baseEditor == null)
                {
                    GameObject editorObj = new GameObject("BaseEditor");
                    baseEditor = editorObj.AddComponent<BaseEditor>();
                }
            }

            yield return null;

            // Ensure UI manager
            if (uiManager == null)
            {
                uiManager = FindFirstObjectByType<PCUIManager>();
            }

            Debug.Log("[PCGame] All systems initialized");
        }

        private void SetupEventBindings()
        {
            // Camera mode changes
            if (cameraController != null)
            {
                cameraController.OnTerritoryFocused += HandleTerritoryFocused;
            }

            // World map clicks
            if (worldMapRenderer != null)
            {
                worldMapRenderer.OnTerritoryClicked += HandleTerritoryClicked;
            }

            // Input shortcuts
            if (inputManager != null)
            {
                inputManager.OnToggleMapTerritoryView += HandleToggleView;
                inputManager.OnOpenMenu += HandleOpenMenu;
                inputManager.OnCycleCameraMode += HandleCycleCameraMode;
            }

            // Base editor
            if (baseEditor != null)
            {
                baseEditor.OnEditorModeEntered += () => SetState(PCClientState.BaseEditor);
                baseEditor.OnEditorModeExited += () => SetState(PCClientState.TerritoryView);
            }
        }

        #region State Management

        private void SetState(PCClientState newState)
        {
            PCClientState previousState = _currentState;
            _currentState = newState;

            Debug.Log($"[PCGame] State changed: {previousState} -> {newState}");

            // Handle state transitions
            switch (newState)
            {
                case PCClientState.WorldMap:
                    cameraController?.EnterWorldMapMode();
                    break;

                case PCClientState.TerritoryView:
                    if (!string.IsNullOrEmpty(_selectedTerritoryId))
                    {
                        cameraController?.EnterTerritoryMode(_selectedTerritoryId);
                    }
                    break;

                case PCClientState.BaseEditor:
                    OnEnterBuildMode?.Invoke();
                    break;

                case PCClientState.Cinematic:
                    cameraController?.EnterCinematicMode();
                    break;
            }
        }

        private void HandleCycleCameraMode()
        {
            if (_currentState == PCClientState.Cinematic)
            {
                SetState(PCClientState.WorldMap);
            }
            else
            {
                SetState(PCClientState.Cinematic);
            }
        }

        /// <summary>
        /// Go to world map view
        /// </summary>
        public void GoToWorldMap()
        {
            if (_currentState == PCClientState.BaseEditor)
            {
                baseEditor?.ExitEditorMode();
            }
            SetState(PCClientState.WorldMap);
        }

        /// <summary>
        /// View a specific territory
        /// </summary>
        public void ViewTerritory(string territoryId)
        {
            _selectedTerritoryId = territoryId;
            SetState(PCClientState.TerritoryView);
            OnTerritorySelected?.Invoke(territoryId);
        }

        /// <summary>
        /// Enter base editor for current territory
        /// </summary>
        public void EnterBaseEditor()
        {
            if (string.IsNullOrEmpty(_selectedTerritoryId))
            {
                Debug.LogWarning("[PCGame] No territory selected for editing");
                return;
            }

            baseEditor?.EnterEditorMode(_selectedTerritoryId);
            SetState(PCClientState.BaseEditor);
        }

        /// <summary>
        /// Exit base editor
        /// </summary>
        public void ExitBaseEditor(bool saveChanges = true)
        {
            baseEditor?.ExitEditorMode(saveChanges);
            SetState(PCClientState.TerritoryView);
            OnExitBuildMode?.Invoke();
        }

        #endregion

        #region Event Handlers

        private void HandleTerritoryClicked(string territoryId)
        {
            _selectedTerritoryId = territoryId;

            if (_currentState == PCClientState.WorldMap)
            {
                // Show territory detail
                ViewTerritory(territoryId);
            }
        }

        private void HandleTerritoryFocused(string territoryId)
        {
            _selectedTerritoryId = territoryId;
            OnTerritorySelected?.Invoke(territoryId);
        }

        private void HandleToggleView()
        {
            switch (_currentState)
            {
                case PCClientState.WorldMap:
                    if (!string.IsNullOrEmpty(_selectedTerritoryId))
                    {
                        ViewTerritory(_selectedTerritoryId);
                    }
                    break;

                case PCClientState.TerritoryView:
                case PCClientState.BaseEditor:
                    GoToWorldMap();
                    break;
            }
        }

        private void HandleOpenMenu()
        {
            if (_currentState == PCClientState.BaseEditor)
            {
                // Confirm exit editor
                ExitBaseEditor();
            }
            else
            {
                uiManager?.TogglePanel(PCUIPanel.MainMenu);
            }
        }

        #endregion

        #region Welcome / Tutorial

        private void ShowWelcomeMessage()
        {
            string platform = PlatformManager.GetPlatformName();
            Debug.Log($"[PCGame] Welcome to Apex Citadels - {platform}!");

            // Show in UI
            uiManager?.ShowNotification(
                $"Welcome to Apex Citadels!\n" +
                $"Running on {platform}\n" +
                $"Use WASD to pan, scroll to zoom, click territories to view.",
                NotificationType.Info
            );
        }

        #endregion

        #region Quick Access

        /// <summary>
        /// Select a territory (can be called from WebGL bridge)
        /// </summary>
        public void SelectTerritory(string territoryId)
        {
            if (string.IsNullOrEmpty(territoryId))
            {
                Debug.LogWarning("[PCGame] SelectTerritory called with null/empty ID");
                return;
            }

            Debug.Log($"[PCGame] Selecting territory: {territoryId}");
            ViewTerritory(territoryId);
        }

        /// <summary>
        /// Quick access to refresh the world map
        /// </summary>
        public void RefreshWorldMap()
        {
            worldMapRenderer?.RefreshVisibleTerritories();
        }

        /// <summary>
        /// Quick access to save current blueprint
        /// </summary>
        public void QuickSaveBlueprint(string name)
        {
            if (_currentState == PCClientState.BaseEditor)
            {
                baseEditor?.SaveAsBlueprint(name);
            }
        }

        /// <summary>
        /// Get the current camera position in GPS coordinates
        /// </summary>
        public (double latitude, double longitude) GetCameraGPSPosition()
        {
            if (worldMapRenderer == null) return (0, 0);

            Vector3 camPos = cameraController?.transform.position ?? Vector3.zero;
            return worldMapRenderer.WorldToGPSPosition(camPos);
        }

        #endregion
    }

    /// <summary>
    /// PC client states
    /// </summary>
    public enum PCClientState
    {
        Loading,
        WorldMap,
        TerritoryView,
        BaseEditor,
        Alliance,
        Market,
        Cinematic,
        Settings
    }
}
