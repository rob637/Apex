using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Handles all keyboard and mouse input for the PC client.
    /// Provides input abstraction and keybinding support.
    /// </summary>
    public class PCInputManager : MonoBehaviour
    {
        public static PCInputManager Instance { get; private set; }

        [Header("Input Settings")]
        [SerializeField] private bool enableInput = true;
        [SerializeField] private float doubleClickTime = 0.3f;
        [SerializeField] private float dragThreshold = 5f;

        // Events - Navigation
        public event Action OnToggleMapTerritoryView;     // Space
        public event Action OnOpenAlliancePanel;           // Tab
        public event Action OnOpenBuildingMenu;            // B
        public event Action OnOpenInventory;               // I
        public event Action OnOpenWorldMap;                // M
        public event Action OnOpenMenu;                    // Esc
        public event Action OnQuickSave;                   // F5
        public event Action OnQuickLoad;                   // F9

        // Events - Selection
        public event Action<Vector3> OnWorldClick;
        public event Action<Vector3> OnWorldDoubleClick;
        public event Action<Vector3> OnWorldRightClick;
        public event Action<Vector3, Vector3> OnWorldDragStart;
        public event Action<Vector3, Vector3> OnWorldDrag;
        public event Action<Vector3, Vector3> OnWorldDragEnd;

        // Events - Building
        public event Action OnRotateBuildingLeft;          // Q
        public event Action OnRotateBuildingRight;         // E
        public event Action OnConfirmPlacement;            // Enter / Left Click
        public event Action OnCancelPlacement;             // Esc / Right Click
        public event Action OnDeleteBuilding;              // Delete / X

        // Events - Camera
        public event Action OnCycleCameraMode;             // C
        public event Action<int> OnCameraPresetSelected;   // 1-5

        // Events - Debug
        public event Action OnToggleDebugInfo;             // F3
        public event Action OnToggleWireframe;             // F4

        // State
        private Dictionary<KeyCode, PCInputAction> _keyBindings;
        private bool _isDragging;
        private Vector3 _dragStartScreen;
        private Vector3 _dragStartWorld;
        private float _lastClickTime;
        private Camera _mainCamera;
        private bool _loggedPlatformInfo = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultKeyBindings();
            Debug.Log("[PCInput] PCInputManager initialized");
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            Debug.Log($"[PCInput] Start - enableInput: {enableInput}, HasKeyboardMouse: {PlatformManager.HasKeyboardMouse}");
        }

        private void Update()
        {
            if (!_loggedPlatformInfo)
            {
                Debug.Log($"[PCInput] Platform: {PlatformManager.CurrentPlatform}, IsWebGL: {PlatformManager.IsWebGL}, HasKeyboardMouse: {PlatformManager.HasKeyboardMouse}");
                _loggedPlatformInfo = true;
            }

            if (!enableInput)
            {
                return;
            }
            
            if (!PlatformManager.HasKeyboardMouse)
            {
                return;
            }

            HandleKeyboardInput();
            HandleMouseInput();
        }

        #region Keyboard Input

        private void HandleKeyboardInput()
        {
            // Debug: Log any key press
            if (Input.anyKeyDown)
            {
                Debug.Log($"[PCInput] Key pressed: {Input.inputString}");
            }

            // Navigation keys
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("[PCInput] SPACE detected");
                OnToggleMapTerritoryView?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Debug.Log("[PCInput] TAB detected");
                OnOpenAlliancePanel?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                Debug.Log("[PCInput] B detected");
                OnOpenBuildingMenu?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                Debug.Log("[PCInput] I detected");
                OnOpenInventory?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                Debug.Log("[PCInput] M detected");
                OnOpenWorldMap?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[PCInput] ESC detected");
                OnOpenMenu?.Invoke();
            }

            // Building controls
            if (Input.GetKeyDown(KeyCode.Q))
                OnRotateBuildingLeft?.Invoke();

            if (Input.GetKeyDown(KeyCode.E))
                OnRotateBuildingRight?.Invoke();

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                OnConfirmPlacement?.Invoke();

            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.X))
                OnDeleteBuilding?.Invoke();

            // Camera
            if (Input.GetKeyDown(KeyCode.C))
                OnCycleCameraMode?.Invoke();

            // Camera presets (1-5)
            for (int i = 1; i <= 5; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    OnCameraPresetSelected?.Invoke(i);
                }
            }

            // Quick save/load
            if (Input.GetKeyDown(KeyCode.F5))
                OnQuickSave?.Invoke();

            if (Input.GetKeyDown(KeyCode.F9))
                OnQuickLoad?.Invoke();

            // Debug
            if (Input.GetKeyDown(KeyCode.F3))
                OnToggleDebugInfo?.Invoke();

            if (Input.GetKeyDown(KeyCode.F4))
                OnToggleWireframe?.Invoke();
        }

        #endregion

        #region Mouse Input

        private void HandleMouseInput()
        {
            // Skip if over UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector3 mousePos = Input.mousePosition;

            // Left click
            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftClickDown(mousePos);
            }

            if (Input.GetMouseButton(0) && _isDragging)
            {
                HandleDragging(mousePos);
            }

            if (Input.GetMouseButtonUp(0))
            {
                HandleLeftClickUp(mousePos);
            }

            // Right click
            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick(mousePos);
            }
        }

        private void HandleLeftClickDown(Vector3 screenPos)
        {
            _dragStartScreen = screenPos;
            _dragStartWorld = ScreenToWorldPosition(screenPos);
        }

        private void HandleLeftClickUp(Vector3 screenPos)
        {
            Vector3 worldPos = ScreenToWorldPosition(screenPos);

            if (_isDragging)
            {
                // End drag
                OnWorldDragEnd?.Invoke(_dragStartWorld, worldPos);
                _isDragging = false;
            }
            else
            {
                // Check for double click
                float timeSinceLastClick = Time.time - _lastClickTime;
                _lastClickTime = Time.time;

                if (timeSinceLastClick <= doubleClickTime)
                {
                    OnWorldDoubleClick?.Invoke(worldPos);
                }
                else
                {
                    OnWorldClick?.Invoke(worldPos);
                    OnConfirmPlacement?.Invoke();
                }
            }
        }

        private void HandleDragging(Vector3 screenPos)
        {
            if (!_isDragging)
            {
                // Check if we've moved enough to start dragging
                float distance = Vector3.Distance(screenPos, _dragStartScreen);
                if (distance >= dragThreshold)
                {
                    _isDragging = true;
                    OnWorldDragStart?.Invoke(_dragStartWorld, ScreenToWorldPosition(screenPos));
                }
            }
            else
            {
                OnWorldDrag?.Invoke(_dragStartWorld, ScreenToWorldPosition(screenPos));
            }
        }

        private void HandleRightClick(Vector3 screenPos)
        {
            Vector3 worldPos = ScreenToWorldPosition(screenPos);
            OnWorldRightClick?.Invoke(worldPos);
            OnCancelPlacement?.Invoke();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Convert screen position to world position via raycast
        /// </summary>
        public Vector3 ScreenToWorldPosition(Vector3 screenPos)
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null)
                return Vector3.zero;

            Ray ray = _mainCamera.ScreenPointToRay(screenPos);

            // First try to hit the ground plane
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            // Fallback to physics raycast
            if (Physics.Raycast(ray, out RaycastHit hit, 10000f))
            {
                return hit.point;
            }

            // Last resort: project to fixed distance
            return ray.GetPoint(100f);
        }

        /// <summary>
        /// Get the object under the mouse cursor
        /// </summary>
        public GameObject GetObjectUnderMouse()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 10000f))
            {
                return hit.collider.gameObject;
            }
            return null;
        }

        /// <summary>
        /// Check if a specific key is held down
        /// </summary>
        public bool IsKeyHeld(KeyCode key)
        {
            return Input.GetKey(key);
        }

        /// <summary>
        /// Check if any modifier key is held
        /// </summary>
        public bool IsModifierHeld(ModifierKey modifier)
        {
            return modifier switch
            {
                ModifierKey.Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                ModifierKey.Control => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
                ModifierKey.Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
                _ => false
            };
        }

        /// <summary>
        /// Enable or disable input handling
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            enableInput = enabled;
        }

        #endregion

        #region Key Bindings

        private void InitializeDefaultKeyBindings()
        {
            _keyBindings = new Dictionary<KeyCode, PCInputAction>
            {
                // Navigation
                { KeyCode.Space, PCInputAction.ToggleView },
                { KeyCode.Tab, PCInputAction.AlliancePanel },
                { KeyCode.B, PCInputAction.BuildMenu },
                { KeyCode.I, PCInputAction.Inventory },
                { KeyCode.M, PCInputAction.WorldMap },
                { KeyCode.Escape, PCInputAction.Menu },

                // Building
                { KeyCode.Q, PCInputAction.RotateLeft },
                { KeyCode.E, PCInputAction.RotateRight },
                { KeyCode.Delete, PCInputAction.Delete },
                { KeyCode.X, PCInputAction.Delete },

                // Camera
                { KeyCode.C, PCInputAction.CycleCamera },

                // Debug
                { KeyCode.F3, PCInputAction.DebugInfo },
                { KeyCode.F4, PCInputAction.Wireframe },
                { KeyCode.F5, PCInputAction.QuickSave },
                { KeyCode.F9, PCInputAction.QuickLoad }
            };
        }

        /// <summary>
        /// Rebind a key to an action
        /// </summary>
        public void RebindKey(KeyCode key, PCInputAction action)
        {
            // Remove old binding for this action
            KeyCode? oldKey = null;
            foreach (var kvp in _keyBindings)
            {
                if (kvp.Value == action)
                {
                    oldKey = kvp.Key;
                    break;
                }
            }
            if (oldKey.HasValue)
            {
                _keyBindings.Remove(oldKey.Value);
            }

            _keyBindings[key] = action;
            Debug.Log($"[PCInput] Rebound {action} to {key}");
        }

        /// <summary>
        /// Get the key bound to an action
        /// </summary>
        public KeyCode GetKeyForAction(PCInputAction action)
        {
            foreach (var kvp in _keyBindings)
            {
                if (kvp.Value == action)
                    return kvp.Key;
            }
            return KeyCode.None;
        }

        #endregion
    }

    /// <summary>
    /// Input actions that can be rebound
    /// </summary>
    public enum PCInputAction
    {
        // Navigation
        ToggleView,
        AlliancePanel,
        BuildMenu,
        Inventory,
        WorldMap,
        Menu,

        // Building
        RotateLeft,
        RotateRight,
        Confirm,
        Cancel,
        Delete,

        // Camera
        CycleCamera,
        CameraPreset1,
        CameraPreset2,
        CameraPreset3,
        CameraPreset4,
        CameraPreset5,

        // Debug
        DebugInfo,
        Wireframe,
        QuickSave,
        QuickLoad
    }

    /// <summary>
    /// Modifier keys for combo inputs
    /// </summary>
    public enum ModifierKey
    {
        None,
        Shift,
        Control,
        Alt
    }
}
