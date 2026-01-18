// ============================================================================
// APEX CITADELS - BASE EDITOR INTEGRATION
// Connects BaseEditor, UI Panel, Camera, and Placement systems
// ============================================================================
using UnityEngine;
using ApexCitadels.Building;
using ApexCitadels.Data;
using ApexCitadels.PC.UI;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Integration layer that connects all base editor components.
    /// Ensures proper communication between:
    /// - BaseEditor (core logic)
    /// - BaseEditorPanel (UI)
    /// - EditorCameraController (camera)
    /// - BlockPlacementController (placement)
    /// - ResourceInventory (resources)
    /// </summary>
    public class BaseEditorIntegration : MonoBehaviour
    {
        public static BaseEditorIntegration Instance { get; private set; }

        [Header("Component References")]
        [SerializeField] private BaseEditor baseEditor;
        [SerializeField] private BaseEditorPanel editorPanel;
        [SerializeField] private EditorCameraController cameraController;
        [SerializeField] private BlockPlacementController placementController;

        [Header("Auto-Create Components")]
        [SerializeField] private bool autoCreateMissing = true;

        // State
        private bool isInitialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize and connect all components
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            // Find or create components
            FindOrCreateComponents();

            // Subscribe to events
            SubscribeToEvents();

            // Initialize camera for editor mode
            if (cameraController != null)
            {
                cameraController.IsEditorMode = false; // Start disabled
            }

            isInitialized = true;
            Debug.Log("[BaseEditorIntegration] Initialized successfully");
        }

        private void FindOrCreateComponents()
        {
            // BaseEditor
            if (baseEditor == null)
            {
                baseEditor = FindFirstObjectByType<BaseEditor>();
                if (baseEditor == null && autoCreateMissing)
                {
                    GameObject obj = new GameObject("BaseEditor");
                    baseEditor = obj.AddComponent<BaseEditor>();
                }
            }

            // BaseEditorPanel
            if (editorPanel == null)
            {
                editorPanel = FindFirstObjectByType<BaseEditorPanel>();
                if (editorPanel == null && autoCreateMissing)
                {
                    GameObject obj = new GameObject("BaseEditorPanel");
                    editorPanel = obj.AddComponent<BaseEditorPanel>();
                }
            }

            // EditorCameraController
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<EditorCameraController>();
                if (cameraController == null && autoCreateMissing && Camera.main != null)
                {
                    cameraController = Camera.main.gameObject.AddComponent<EditorCameraController>();
                }
            }

            // BlockPlacementController
            if (placementController == null)
            {
                placementController = FindFirstObjectByType<BlockPlacementController>();
                if (placementController == null && autoCreateMissing)
                {
                    GameObject obj = new GameObject("BlockPlacementController");
                    placementController = obj.AddComponent<BlockPlacementController>();
                }
            }

            // Ensure ResourceInventory exists
            if (ResourceInventory.Instance == null)
            {
                GameObject obj = new GameObject("ResourceInventory");
                obj.AddComponent<ResourceInventory>();
            }
        }

        private void SubscribeToEvents()
        {
            // BaseEditor events
            if (baseEditor != null)
            {
                baseEditor.OnEditorModeEntered += OnEditorEntered;
                baseEditor.OnEditorModeExited += OnEditorExited;
                baseEditor.OnBlockPlaced += OnBlockPlacedHandler;
                baseEditor.OnBlockRemoved += OnBlockRemovedHandler;
                baseEditor.OnBlockSelected += OnBlockSelectedHandler;
            }

            // PlacementController events
            if (placementController != null)
            {
                placementController.OnBlockPlaced += OnPlacementConfirmed;
                placementController.OnPlacementCancelled += OnPlacementCancelled;
                placementController.OnValidationChanged += OnValidationChanged;
            }

            // ResourceInventory events
            if (ResourceInventory.Instance != null)
            {
                ResourceInventory.Instance.OnResourceChanged += OnResourceChanged;
            }
        }

        #region Event Handlers

        private void OnEditorEntered()
        {
            Debug.Log("[BaseEditorIntegration] Editor mode entered");

            // Enable camera controls
            if (cameraController != null)
            {
                cameraController.IsEditorMode = true;
                cameraController.SetIsometricView();
            }

            // Update resource display
            UpdateResourceDisplay();
        }

        private void OnEditorExited()
        {
            Debug.Log("[BaseEditorIntegration] Editor mode exited");

            // Disable camera controls
            if (cameraController != null)
            {
                cameraController.IsEditorMode = false;
            }

            // Cancel any placement
            if (placementController != null)
            {
                placementController.CancelPlacement();
            }
        }

        private void OnBlockPlacedHandler(BuildingBlock block)
        {
            // Update resource display after placement
            UpdateResourceDisplay();
        }

        private void OnBlockRemovedHandler(BuildingBlock block)
        {
            // Could refund resources here
        }

        private void OnBlockSelectedHandler(BuildingBlock block)
        {
            // Focus camera on selected block
            if (cameraController != null && block != null)
            {
                cameraController.FocusOn(block.LocalPosition, 15f);
            }
        }

        private void OnPlacementConfirmed(BlockType type, Vector3 position, Quaternion rotation)
        {
            // Forward to BaseEditor to actually place the block
            if (baseEditor != null)
            {
                baseEditor.SelectBlockType(type);
                // The BaseEditor will handle the actual placement
            }

            Debug.Log($"[BaseEditorIntegration] Placement confirmed: {type} at {position}");
        }

        private void OnPlacementCancelled()
        {
            // Reset UI state if needed
        }

        private void OnValidationChanged(PlacementValidation validation)
        {
            // Could show feedback in UI
        }

        private void OnResourceChanged(ResourceChangeEvent evt)
        {
            UpdateResourceDisplay();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Enter editor mode for a territory
        /// </summary>
        public void EnterEditor(string territoryId)
        {
            if (baseEditor != null)
            {
                baseEditor.EnterEditorMode(territoryId);
            }
        }

        /// <summary>
        /// Exit editor mode
        /// </summary>
        public void ExitEditor(bool save = true)
        {
            if (baseEditor != null)
            {
                baseEditor.ExitEditorMode(save);
            }
        }

        /// <summary>
        /// Select a block type for placement
        /// </summary>
        public void SelectBlock(BlockType type)
        {
            // Start placement in both systems
            if (baseEditor != null)
            {
                baseEditor.SelectBlockType(type);
            }

            if (placementController != null)
            {
                placementController.StartPlacement(type);
            }
        }

        /// <summary>
        /// Focus camera on a position
        /// </summary>
        public void FocusOn(Vector3 position)
        {
            if (cameraController != null)
            {
                cameraController.FocusOn(position);
            }
        }

        /// <summary>
        /// Update resource display in UI
        /// </summary>
        public void UpdateResourceDisplay()
        {
            if (editorPanel != null && ResourceInventory.Instance != null)
            {
                var inv = ResourceInventory.Instance;
                editorPanel.UpdateResources(
                    inv.Stone,
                    inv.Wood,
                    inv.Metal,
                    inv.Crystal
                );
            }
        }

        /// <summary>
        /// Undo last action
        /// </summary>
        public void Undo()
        {
            baseEditor?.Undo();
        }

        /// <summary>
        /// Redo last undone action
        /// </summary>
        public void Redo()
        {
            baseEditor?.Redo();
        }

        #endregion

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (baseEditor != null)
            {
                baseEditor.OnEditorModeEntered -= OnEditorEntered;
                baseEditor.OnEditorModeExited -= OnEditorExited;
                baseEditor.OnBlockPlaced -= OnBlockPlacedHandler;
                baseEditor.OnBlockRemoved -= OnBlockRemovedHandler;
                baseEditor.OnBlockSelected -= OnBlockSelectedHandler;
            }

            if (placementController != null)
            {
                placementController.OnBlockPlaced -= OnPlacementConfirmed;
                placementController.OnPlacementCancelled -= OnPlacementCancelled;
                placementController.OnValidationChanged -= OnValidationChanged;
            }

            if (ResourceInventory.Instance != null)
            {
                ResourceInventory.Instance.OnResourceChanged -= OnResourceChanged;
            }
        }
    }
}
