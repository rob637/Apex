using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Building;
using ApexCitadels.Territory;
using ApexCitadels.Data;

namespace ApexCitadels.PC
{
    /// <summary>
    /// PC-exclusive detailed base editor for precise building placement.
    /// Provides mouse-based drag-and-drop building with snapping grid.
    /// </summary>
    public class BaseEditor : MonoBehaviour
    {
        public static BaseEditor Instance { get; private set; }

        [Header("Editor Settings")]
        [SerializeField] private float gridSize = 0.5f;
        [SerializeField] private float rotationStep = 15f;
        [SerializeField] private int undoHistorySize = 50;
        [SerializeField] private bool enableSnapping = true;
        [SerializeField] private bool showGrid = true;

        [Header("Placement")]
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;
        [SerializeField] private float placementHeight = 0.5f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask buildingLayer;

        [Header("Grid Visualization")]
        [SerializeField] private Material gridMaterial;
        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private Color gridHighlightColor = new Color(0f, 1f, 0f, 0.5f);

        [Header("Prefabs")]
        [SerializeField] private GameObject[] buildingPrefabs;

        // Events
        public event Action<BuildingBlock> OnBlockPlaced;
        public event Action<BuildingBlock> OnBlockRemoved;
        public event Action<BuildingBlock> OnBlockSelected;
        public event Action OnEditorModeEntered;
        public event Action OnEditorModeExited;
        public event Action<Blueprint> OnBlueprintSaved;
        public event Action<Blueprint> OnBlueprintLoaded;

        // State
        private bool _isEditorActive;
        private bool _isPlacementMode;
        private BlockType _selectedBlockType;
        private GameObject _placementPreview;
        private float _currentRotation;
        private string _currentTerritoryId;
        private Territory.Territory _currentTerritory;

        // Placed buildings in editor
        private Dictionary<string, EditorBlock> _editorBlocks = new Dictionary<string, EditorBlock>();
        private List<EditorAction> _undoHistory = new List<EditorAction>();
        private List<EditorAction> _redoHistory = new List<EditorAction>();

        // Selection
        private EditorBlock _selectedBlock;
        private List<EditorBlock> _multiSelection = new List<EditorBlock>();

        // Grid
        private GameObject _gridPlane;
        private Vector3 _gridCenter;
        private float _gridRadius;

        // Prefab lookup
        private Dictionary<BlockType, GameObject> _prefabLookup = new Dictionary<BlockType, GameObject>();

        public bool IsEditorActive => _isEditorActive;
        public bool IsPlacementMode => _isPlacementMode;
        public BlockType SelectedBlockType => _selectedBlockType;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePrefabLookup();
        }

        private void Start()
        {
            SetupInputBindings();
        }

        private void Update()
        {
            if (!_isEditorActive) return;

            if (_isPlacementMode)
            {
                UpdatePlacementPreview();
            }

            HandleShortcuts();
        }

        private void InitializePrefabLookup()
        {
            // Map block types to prefabs
            for (int i = 0; i < buildingPrefabs.Length && i < (int)BlockType.Count; i++)
            {
                _prefabLookup[(BlockType)i] = buildingPrefabs[i];
            }
        }

        private void SetupInputBindings()
        {
            if (PCInputManager.Instance == null) return;

            PCInputManager.Instance.OnWorldClick += HandleWorldClick;
            PCInputManager.Instance.OnWorldRightClick += HandleRightClick;
            PCInputManager.Instance.OnRotateBuildingLeft += () => RotatePreview(-rotationStep);
            PCInputManager.Instance.OnRotateBuildingRight += () => RotatePreview(rotationStep);
            PCInputManager.Instance.OnConfirmPlacement += ConfirmPlacement;
            PCInputManager.Instance.OnCancelPlacement += CancelPlacement;
            PCInputManager.Instance.OnDeleteBuilding += DeleteSelectedBlock;
        }

        #region Editor Mode

        /// <summary>
        /// Enter base editor mode for a territory
        /// </summary>
        public void EnterEditorMode(string territoryId)
        {
            _currentTerritoryId = territoryId;
            _isEditorActive = true;

            // Load territory data
            LoadTerritoryData(territoryId);

            // Show grid
            if (showGrid)
                CreateEditorGrid();

            // Load existing buildings
            LoadExistingBuildings();

            Debug.Log($"[BaseEditor] Entered editor mode for territory: {territoryId}");
            OnEditorModeEntered?.Invoke();
        }

        /// <summary>
        /// Exit base editor mode
        /// </summary>
        public void ExitEditorMode(bool saveChanges = true)
        {
            if (saveChanges)
            {
                SaveChangesToBackend();
            }

            // Cleanup
            CancelPlacement();
            ClearSelection();

            if (_gridPlane != null)
                Destroy(_gridPlane);

            _isEditorActive = false;
            _currentTerritoryId = null;
            _currentTerritory = null;

            Debug.Log("[BaseEditor] Exited editor mode");
            OnEditorModeExited?.Invoke();
        }

        private void LoadTerritoryData(string territoryId)
        {
            // TODO: Load from TerritoryManager
            _currentTerritory = new Territory.Territory
            {
                Id = territoryId,
                RadiusMeters = 50f
            };
            _gridCenter = Vector3.zero;
            _gridRadius = _currentTerritory.RadiusMeters;
        }

        private void LoadExistingBuildings()
        {
            _editorBlocks.Clear();

            // TODO: Load from BuildingManager
            // For now, create empty state
        }

        #endregion

        #region Block Selection

        /// <summary>
        /// Select a block type for placement
        /// </summary>
        public void SelectBlockType(BlockType type)
        {
            _selectedBlockType = type;
            EnterPlacementMode();
        }

        /// <summary>
        /// Enter placement mode with current block type
        /// </summary>
        public void EnterPlacementMode()
        {
            _isPlacementMode = true;
            CreatePlacementPreview();
        }

        /// <summary>
        /// Cancel placement mode
        /// </summary>
        public void CancelPlacement()
        {
            _isPlacementMode = false;
            if (_placementPreview != null)
            {
                Destroy(_placementPreview);
                _placementPreview = null;
            }
        }

        /// <summary>
        /// Select a placed block
        /// </summary>
        public void SelectBlock(EditorBlock block)
        {
            // Deselect previous
            if (_selectedBlock != null)
            {
                _selectedBlock.SetSelected(false);
            }

            _selectedBlock = block;

            if (block != null)
            {
                block.SetSelected(true);
                OnBlockSelected?.Invoke(block.Data);
            }
        }

        /// <summary>
        /// Clear selection
        /// </summary>
        public void ClearSelection()
        {
            if (_selectedBlock != null)
            {
                _selectedBlock.SetSelected(false);
                _selectedBlock = null;
            }

            foreach (var block in _multiSelection)
            {
                block.SetSelected(false);
            }
            _multiSelection.Clear();
        }

        #endregion

        #region Placement

        private void CreatePlacementPreview()
        {
            if (_placementPreview != null)
            {
                Destroy(_placementPreview);
            }

            GameObject prefab = GetPrefabForType(_selectedBlockType);
            if (prefab != null)
            {
                _placementPreview = Instantiate(prefab);
            }
            else
            {
                // Create default cube
                _placementPreview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _placementPreview.transform.localScale = new Vector3(gridSize, gridSize, gridSize);
            }

            _placementPreview.name = "PlacementPreview";

            // Disable collider on preview
            var collider = _placementPreview.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            // Apply preview material
            SetPreviewMaterial(true);
        }

        private void UpdatePlacementPreview()
        {
            if (_placementPreview == null) return;

            // Get mouse position in world
            Vector3 worldPos = GetMouseWorldPosition();

            // Snap to grid
            if (enableSnapping)
            {
                worldPos = SnapToGrid(worldPos);
            }

            // Check if position is valid
            bool isValid = IsPlacementValid(worldPos);
            SetPreviewMaterial(isValid);

            // Update position
            _placementPreview.transform.position = worldPos;
            _placementPreview.transform.rotation = Quaternion.Euler(0, _currentRotation, 0);
        }

        private void ConfirmPlacement()
        {
            if (!_isPlacementMode || _placementPreview == null) return;

            Vector3 position = _placementPreview.transform.position;

            if (!IsPlacementValid(position))
            {
                Debug.Log("[BaseEditor] Invalid placement position");
                return;
            }

            // Create the block
            PlaceBlock(_selectedBlockType, position, _currentRotation);

            // Continue placement mode for rapid building
            CreatePlacementPreview();
        }

        private void PlaceBlock(BlockType type, Vector3 position, float rotation)
        {
            string blockId = Guid.NewGuid().ToString();

            // Create building data
            BuildingBlock blockData = new BuildingBlock
            {
                Id = blockId,
                Type = type,
                TerritoryId = _currentTerritoryId,
                OwnerId = Core.GameManager.Instance?.UserId,
                Position = position,
                Rotation = Quaternion.Euler(0, rotation, 0)
            };

            // Create game object
            GameObject prefab = GetPrefabForType(type);
            GameObject blockObj;
            if (prefab != null)
            {
                blockObj = Instantiate(prefab, position, Quaternion.Euler(0, rotation, 0), transform);
            }
            else
            {
                blockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blockObj.transform.position = position;
                blockObj.transform.rotation = Quaternion.Euler(0, rotation, 0);
                blockObj.transform.localScale = Vector3.one * gridSize;
            }

            blockObj.name = $"Block_{blockId}";

            // Add editor component
            EditorBlock editorBlock = blockObj.AddComponent<EditorBlock>();
            editorBlock.Initialize(blockData);
            editorBlock.OnClicked += () => SelectBlock(editorBlock);

            _editorBlocks[blockId] = editorBlock;

            // Record for undo
            RecordAction(new EditorAction
            {
                Type = EditorActionType.Place,
                BlockId = blockId,
                BlockType = type,
                Position = position,
                Rotation = rotation
            });

            OnBlockPlaced?.Invoke(blockData);
            Debug.Log($"[BaseEditor] Placed {type} at {position}");
        }

        private void DeleteSelectedBlock()
        {
            if (_selectedBlock == null) return;

            string blockId = _selectedBlock.Data.Id;

            // Record for undo
            RecordAction(new EditorAction
            {
                Type = EditorActionType.Remove,
                BlockId = blockId,
                BlockType = _selectedBlock.Data.Type,
                Position = _selectedBlock.transform.position,
                Rotation = _selectedBlock.transform.eulerAngles.y
            });

            BuildingBlock removedData = _selectedBlock.Data;
            _editorBlocks.Remove(blockId);
            Destroy(_selectedBlock.gameObject);
            _selectedBlock = null;

            OnBlockRemoved?.Invoke(removedData);
            Debug.Log($"[BaseEditor] Removed block: {blockId}");
        }

        #endregion

        #region Undo/Redo

        private void RecordAction(EditorAction action)
        {
            _undoHistory.Add(action);
            _redoHistory.Clear();

            // Limit history size
            while (_undoHistory.Count > undoHistorySize)
            {
                _undoHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Undo the last action
        /// </summary>
        public void Undo()
        {
            if (_undoHistory.Count == 0) return;

            EditorAction action = _undoHistory[_undoHistory.Count - 1];
            _undoHistory.RemoveAt(_undoHistory.Count - 1);

            // Reverse the action
            switch (action.Type)
            {
                case EditorActionType.Place:
                    // Remove the placed block
                    if (_editorBlocks.TryGetValue(action.BlockId, out EditorBlock block))
                    {
                        _editorBlocks.Remove(action.BlockId);
                        Destroy(block.gameObject);
                    }
                    break;

                case EditorActionType.Remove:
                    // Re-place the removed block
                    PlaceBlockWithoutRecord(action.BlockType, action.Position, action.Rotation, action.BlockId);
                    break;

                case EditorActionType.Move:
                    // Move back to original position
                    if (_editorBlocks.TryGetValue(action.BlockId, out EditorBlock movedBlock))
                    {
                        movedBlock.transform.position = action.PreviousPosition;
                        movedBlock.transform.rotation = Quaternion.Euler(0, action.PreviousRotation, 0);
                    }
                    break;
            }

            _redoHistory.Add(action);
            Debug.Log($"[BaseEditor] Undo: {action.Type}");
        }

        /// <summary>
        /// Redo the last undone action
        /// </summary>
        public void Redo()
        {
            if (_redoHistory.Count == 0) return;

            EditorAction action = _redoHistory[_redoHistory.Count - 1];
            _redoHistory.RemoveAt(_redoHistory.Count - 1);

            // Re-apply the action
            switch (action.Type)
            {
                case EditorActionType.Place:
                    PlaceBlockWithoutRecord(action.BlockType, action.Position, action.Rotation, action.BlockId);
                    break;

                case EditorActionType.Remove:
                    if (_editorBlocks.TryGetValue(action.BlockId, out EditorBlock block))
                    {
                        _editorBlocks.Remove(action.BlockId);
                        Destroy(block.gameObject);
                    }
                    break;

                case EditorActionType.Move:
                    if (_editorBlocks.TryGetValue(action.BlockId, out EditorBlock movedBlock))
                    {
                        movedBlock.transform.position = action.Position;
                        movedBlock.transform.rotation = Quaternion.Euler(0, action.Rotation, 0);
                    }
                    break;
            }

            _undoHistory.Add(action);
            Debug.Log($"[BaseEditor] Redo: {action.Type}");
        }

        private void PlaceBlockWithoutRecord(BlockType type, Vector3 position, float rotation, string blockId)
        {
            BuildingBlock blockData = new BuildingBlock
            {
                Id = blockId,
                Type = type,
                TerritoryId = _currentTerritoryId,
                OwnerId = Core.GameManager.Instance?.UserId,
                Position = position,
                Rotation = Quaternion.Euler(0, rotation, 0)
            };

            GameObject prefab = GetPrefabForType(type);
            GameObject blockObj = prefab != null 
                ? Instantiate(prefab, position, Quaternion.Euler(0, rotation, 0), transform)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            if (prefab == null)
            {
                blockObj.transform.position = position;
                blockObj.transform.rotation = Quaternion.Euler(0, rotation, 0);
                blockObj.transform.localScale = Vector3.one * gridSize;
            }

            blockObj.name = $"Block_{blockId}";

            EditorBlock editorBlock = blockObj.AddComponent<EditorBlock>();
            editorBlock.Initialize(blockData);
            editorBlock.OnClicked += () => SelectBlock(editorBlock);

            _editorBlocks[blockId] = editorBlock;
        }

        #endregion

        #region Blueprints

        /// <summary>
        /// Save current layout as a blueprint
        /// </summary>
        public Blueprint SaveAsBlueprint(string name, string description = "")
        {
            Blueprint blueprint = new Blueprint
            {
                Id = Guid.NewGuid().ToString(),
                OwnerId = Core.GameManager.Instance?.UserId,
                Name = name,
                Description = description,
                SourceTerritoryId = _currentTerritoryId,
                Buildings = new List<BuildingPlacement>(),
                CreatedAt = DateTime.UtcNow
            };

            foreach (var kvp in _editorBlocks)
            {
                EditorBlock block = kvp.Value;
                blueprint.Buildings.Add(new BuildingPlacement
                {
                    BlockType = block.Data.Type.ToString(),
                    PositionX = block.transform.position.x,
                    PositionY = block.transform.position.y,
                    PositionZ = block.transform.position.z,
                    RotationY = block.transform.eulerAngles.y,
                    PlacedAt = DateTime.UtcNow
                });
            }

            OnBlueprintSaved?.Invoke(blueprint);
            Debug.Log($"[BaseEditor] Saved blueprint: {name} with {blueprint.Buildings.Count} buildings");

            return blueprint;
        }

        /// <summary>
        /// Load a blueprint into the editor
        /// </summary>
        public void LoadBlueprint(Blueprint blueprint)
        {
            // Clear current buildings
            foreach (var block in _editorBlocks.Values)
            {
                Destroy(block.gameObject);
            }
            _editorBlocks.Clear();
            _undoHistory.Clear();
            _redoHistory.Clear();

            // Place blueprint buildings
            foreach (var placement in blueprint.Buildings)
            {
                BlockType type = Enum.TryParse<BlockType>(placement.BlockType, out var parsedType) 
                    ? parsedType 
                    : BlockType.Stone;

                PlaceBlock(type, placement.GetPosition(), placement.RotationY);
            }

            OnBlueprintLoaded?.Invoke(blueprint);
            Debug.Log($"[BaseEditor] Loaded blueprint: {blueprint.Name}");
        }

        #endregion

        #region Utility

        private void HandleShortcuts()
        {
            // Ctrl+Z = Undo
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) 
                && Input.GetKeyDown(KeyCode.Z))
            {
                Undo();
            }

            // Ctrl+Y = Redo
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) 
                && Input.GetKeyDown(KeyCode.Y))
            {
                Redo();
            }

            // Escape = Exit editor
            if (Input.GetKeyDown(KeyCode.Escape) && !_isPlacementMode)
            {
                ExitEditorMode();
            }
        }

        private void HandleWorldClick(Vector3 worldPos)
        {
            if (!_isEditorActive) return;

            if (_isPlacementMode)
            {
                ConfirmPlacement();
            }
            else
            {
                // Try to select a block
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, buildingLayer))
                {
                    EditorBlock block = hit.collider.GetComponent<EditorBlock>();
                    if (block != null)
                    {
                        SelectBlock(block);
                        return;
                    }
                }

                ClearSelection();
            }
        }

        private void HandleRightClick(Vector3 worldPos)
        {
            if (_isPlacementMode)
            {
                CancelPlacement();
            }
        }

        private void RotatePreview(float angle)
        {
            _currentRotation += angle;
            if (_currentRotation >= 360f) _currentRotation -= 360f;
            if (_currentRotation < 0f) _currentRotation += 360f;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (Camera.main == null) return Vector3.zero;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 point = ray.GetPoint(distance);
                point.y = placementHeight;
                return point;
            }

            return Vector3.zero;
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.z = Mathf.Round(position.z / gridSize) * gridSize;
            return position;
        }

        private bool IsPlacementValid(Vector3 position)
        {
            // Check within territory bounds
            float distanceFromCenter = Vector3.Distance(position, _gridCenter);
            if (distanceFromCenter > _gridRadius) return false;

            // Check collision with existing blocks
            Collider[] overlaps = Physics.OverlapSphere(position, gridSize * 0.4f, buildingLayer);
            return overlaps.Length == 0;
        }

        private void SetPreviewMaterial(bool valid)
        {
            if (_placementPreview == null) return;

            Renderer renderer = _placementPreview.GetComponent<Renderer>();
            if (renderer == null) return;

            Material mat = valid ? validPlacementMaterial : invalidPlacementMaterial;
            if (mat != null)
            {
                renderer.material = mat;
            }
            else
            {
                renderer.material.color = valid 
                    ? new Color(0, 1, 0, 0.5f) 
                    : new Color(1, 0, 0, 0.5f);
            }
        }

        private void CreateEditorGrid()
        {
            if (_gridPlane != null) Destroy(_gridPlane);

            _gridPlane = new GameObject("EditorGrid");
            _gridPlane.transform.position = _gridCenter;

            // Create grid lines
            int gridLines = Mathf.CeilToInt(_gridRadius * 2 / gridSize);
            float halfSize = gridLines * gridSize / 2;

            for (int i = 0; i <= gridLines; i++)
            {
                float pos = -halfSize + i * gridSize;

                // Horizontal line
                CreateGridLine(new Vector3(-halfSize, 0.01f, pos), new Vector3(halfSize, 0.01f, pos));
                // Vertical line
                CreateGridLine(new Vector3(pos, 0.01f, -halfSize), new Vector3(pos, 0.01f, halfSize));
            }
        }

        private void CreateGridLine(Vector3 start, Vector3 end)
        {
            GameObject lineObj = new GameObject("GridLine");
            lineObj.transform.parent = _gridPlane.transform;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPositions(new Vector3[] { start + _gridCenter, end + _gridCenter });
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;

            if (gridMaterial != null)
            {
                lr.material = gridMaterial;
            }
            else
            {
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.material.color = gridColor;
            }
        }

        private GameObject GetPrefabForType(BlockType type)
        {
            return _prefabLookup.TryGetValue(type, out GameObject prefab) ? prefab : null;
        }

        private void SaveChangesToBackend()
        {
            // TODO: Save to BuildingManager / Firebase
            Debug.Log($"[BaseEditor] Saving {_editorBlocks.Count} blocks to backend");
        }

        #endregion
    }

    /// <summary>
    /// Component for blocks in the editor
    /// </summary>
    public class EditorBlock : MonoBehaviour
    {
        public BuildingBlock Data { get; private set; }
        public event Action OnClicked;

        private bool _isSelected;
        private Renderer _renderer;
        private Color _originalColor;
        private Outline _outline;

        public void Initialize(BuildingBlock data)
        {
            Data = data;
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _originalColor = _renderer.material.color;
            }
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (_outline == null)
            {
                _outline = gameObject.AddComponent<Outline>();
            }

            _outline.enabled = selected;
        }

        private void OnMouseDown()
        {
            OnClicked?.Invoke();
        }
    }

    /// <summary>
    /// Simple outline component for selection highlighting
    /// </summary>
    public class Outline : MonoBehaviour
    {
        // Placeholder - would need shader implementation
        public new bool enabled { get; set; }
    }

    /// <summary>
    /// Editor action for undo/redo
    /// </summary>
    public class EditorAction
    {
        public EditorActionType Type;
        public string BlockId;
        public BlockType BlockType;
        public Vector3 Position;
        public float Rotation;
        public Vector3 PreviousPosition;
        public float PreviousRotation;
    }

    /// <summary>
    /// Types of editor actions
    /// </summary>
    public enum EditorActionType
    {
        Place,
        Remove,
        Move,
        Rotate
    }
}
