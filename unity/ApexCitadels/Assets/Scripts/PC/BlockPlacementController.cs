using Camera = UnityEngine.Camera;
// ============================================================================
// APEX CITADELS - BLOCK PLACEMENT CONTROLLER
// Enhanced block placement with visual feedback and validation
// ============================================================================
using UnityEngine;
using System;
using System.Collections.Generic;
using ApexCitadels.Building;
using ApexCitadels.Core;
using ApexCitadels.Data;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Placement validation result
    /// </summary>
    public class PlacementValidation
    {
        public bool IsValid;
        public string Message;
        public Color IndicatorColor;

        public static PlacementValidation Valid(string msg = "Valid placement")
        {
            return new PlacementValidation
            {
                IsValid = true,
                Message = msg,
                IndicatorColor = new Color(0.2f, 0.8f, 0.3f, 0.6f)
            };
        }

        public static PlacementValidation Invalid(string msg)
        {
            return new PlacementValidation
            {
                IsValid = false,
                Message = msg,
                IndicatorColor = new Color(0.8f, 0.2f, 0.2f, 0.6f)
            };
        }
    }

    /// <summary>
    /// Enhanced block placement system for PC editor
    /// Handles previews, validation, multi-placement, and visual feedback
    /// </summary>
    public class BlockPlacementController : MonoBehaviour
    {
        public static BlockPlacementController Instance { get; private set; }

        [Header("Placement Settings")]
        [SerializeField] private float gridSize = 0.5f;
        [SerializeField] private float rotationSnap = 15f;
        [SerializeField] private float placementHeight = 0.25f;
        [SerializeField] private bool enableSnapping = true;
        [SerializeField] private bool continuousPlacement = true;
        [SerializeField] private float placementCooldown = 0.15f;

        [Header("Preview")]
        [SerializeField] private Material previewValidMaterial;
        [SerializeField] private Material previewInvalidMaterial;
        [SerializeField] private Material gridHighlightMaterial;

        [Header("Visual Feedback")]
        [SerializeField] private bool showPlacementGrid = true;
        [SerializeField] private bool showDistanceMarkers = true;
        [SerializeField] private float previewPulseSpeed = 2f;
        [SerializeField] private float previewPulseAmount = 0.1f;

        [Header("Collision")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask buildingLayer;
        [SerializeField] private float overlapCheckRadius = 0.3f;

        [Header("Audio")]
        [SerializeField] private AudioClip placementSound;
        [SerializeField] private AudioClip invalidSound;
        [SerializeField] private AudioClip rotateSound;
        private AudioSource audioSource;

        // State
        private bool isPlacementActive;
        private BlockType currentBlockType;
        private GameObject previewObject;
        private GameObject gridHighlight;
        private float currentRotation;
        private float lastPlacementTime;
        private PlacementValidation lastValidation;

        // Multi-placement
        private bool isMultiPlacing;
        private Vector3 multiPlaceStart;
        private List<Vector3> multiPlacePositions = new List<Vector3>();
        private List<GameObject> multiPreviews = new List<GameObject>();

        // Events
        public event Action<BlockType, Vector3, Quaternion> OnBlockPlaced;
        public event Action<BlockType> OnBlockTypeSelected;
        public event Action OnPlacementCancelled;
        public event Action<PlacementValidation> OnValidationChanged;

        // Properties
        public bool IsPlacementActive => isPlacementActive;
        public BlockType CurrentBlockType => currentBlockType;
        public float CurrentRotation => currentRotation;
        public bool SnappingEnabled { get => enableSnapping; set => enableSnapping = value; }
        public float GridSize { get => gridSize; set => gridSize = value; }

        private Camera mainCam;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void Start()
        {
            mainCam = Camera.main;
            CreateMaterials();
            CreateGridHighlight();
        }

        private void Update()
        {
            if (!isPlacementActive) return;

            UpdatePreviewPosition();
            HandlePlacementInput();
        }

        #region Initialization

        private void CreateMaterials()
        {
            if (previewValidMaterial == null)
            {
                previewValidMaterial = new Material(Shader.Find("Standard"));
                previewValidMaterial.color = new Color(0.2f, 0.8f, 0.3f, 0.5f);
                previewValidMaterial.SetFloat("_Mode", 3);
                previewValidMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                previewValidMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                previewValidMaterial.EnableKeyword("_ALPHABLEND_ON");
                previewValidMaterial.renderQueue = 3000;
            }

            if (previewInvalidMaterial == null)
            {
                previewInvalidMaterial = new Material(Shader.Find("Standard"));
                previewInvalidMaterial.color = new Color(0.8f, 0.2f, 0.2f, 0.5f);
                previewInvalidMaterial.SetFloat("_Mode", 3);
                previewInvalidMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                previewInvalidMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                previewInvalidMaterial.EnableKeyword("_ALPHABLEND_ON");
                previewInvalidMaterial.renderQueue = 3000;
            }

            if (gridHighlightMaterial == null)
            {
                gridHighlightMaterial = new Material(Shader.Find("Standard"));
                gridHighlightMaterial.color = new Color(1f, 1f, 1f, 0.2f);
                gridHighlightMaterial.SetFloat("_Mode", 3);
                gridHighlightMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                gridHighlightMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                gridHighlightMaterial.EnableKeyword("_ALPHABLEND_ON");
                gridHighlightMaterial.renderQueue = 2999;
            }
        }

        private void CreateGridHighlight()
        {
            gridHighlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            gridHighlight.name = "GridHighlight";
            gridHighlight.transform.rotation = Quaternion.Euler(90, 0, 0);
            gridHighlight.transform.localScale = new Vector3(gridSize, gridSize, 1);

            Destroy(gridHighlight.GetComponent<Collider>());

            var renderer = gridHighlight.GetComponent<Renderer>();
            renderer.material = gridHighlightMaterial;

            gridHighlight.SetActive(false);
        }

        #endregion

        #region Placement Control

        /// <summary>
        /// Start placement mode for a block type
        /// </summary>
        public void StartPlacement(BlockType blockType)
        {
            currentBlockType = blockType;
            isPlacementActive = true;
            currentRotation = 0f;

            CreatePreviewObject();

            OnBlockTypeSelected?.Invoke(blockType);
            ApexLogger.Log($"Started placement: {blockType}", ApexLogger.LogCategory.Building);
        }

        /// <summary>
        /// Cancel placement mode
        /// </summary>
        public void CancelPlacement()
        {
            isPlacementActive = false;

            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }

            ClearMultiPreviews();

            if (gridHighlight != null)
            {
                gridHighlight.SetActive(false);
            }

            OnPlacementCancelled?.Invoke();
            ApexLogger.Log("Placement cancelled", ApexLogger.LogCategory.Building);
        }

        /// <summary>
        /// Rotate current preview
        /// </summary>
        public void Rotate(float angle)
        {
            currentRotation += angle;
            if (currentRotation >= 360f) currentRotation -= 360f;
            if (currentRotation < 0f) currentRotation += 360f;

            // Snap rotation
            if (rotationSnap > 0)
            {
                currentRotation = Mathf.Round(currentRotation / rotationSnap) * rotationSnap;
            }

            PlaySound(rotateSound, 0.3f);
        }

        /// <summary>
        /// Confirm placement at current position
        /// </summary>
        public bool ConfirmPlacement()
        {
            if (!isPlacementActive || previewObject == null) return false;
            if (Time.time - lastPlacementTime < placementCooldown) return false;

            Vector3 position = previewObject.transform.position;
            var validation = ValidatePlacement(position);

            if (!validation.IsValid)
            {
                PlaySound(invalidSound, 0.5f);
                return false;
            }

            // Check resources
            var blockDef = BlockDefinitions.Get(currentBlockType);
            if (blockDef != null && ResourceInventory.Instance != null)
            {
                if (!ResourceInventory.Instance.HasResource(ResourceType.Stone, blockDef.ResourceCost))
                {
                    lastValidation = PlacementValidation.Invalid("Insufficient resources");
                    OnValidationChanged?.Invoke(lastValidation);
                    PlaySound(invalidSound, 0.5f);
                    return false;
                }

                // Deduct cost
                ResourceInventory.Instance.RemoveResource(ResourceType.Stone, blockDef.ResourceCost, $"Build {currentBlockType}");
            }

            // Place the block
            Quaternion rotation = Quaternion.Euler(0, currentRotation, 0);
            OnBlockPlaced?.Invoke(currentBlockType, position, rotation);

            lastPlacementTime = Time.time;
            PlaySound(placementSound, 0.5f);
            CreatePlacementEffect(position);

            // Continue or exit placement
            if (!continuousPlacement)
            {
                CancelPlacement();
            }

            return true;
        }

        #endregion

        #region Preview Management

        private void CreatePreviewObject()
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
            }

            // Create based on block type
            previewObject = CreateBlockPreview(currentBlockType);
            previewObject.name = "PlacementPreview";

            // Disable colliders
            foreach (var col in previewObject.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            // Apply preview material
            foreach (var renderer in previewObject.GetComponentsInChildren<Renderer>())
            {
                renderer.material = previewValidMaterial;
            }
        }

        private GameObject CreateBlockPreview(BlockType type)
        {
            GameObject preview;

            // Create appropriate primitive
            switch (type)
            {
                case BlockType.Wall:
                case BlockType.WallStone:
                case BlockType.WallWood:
                case BlockType.WallMetal:
                case BlockType.WallReinforced:
                    preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    preview.transform.localScale = new Vector3(2f, 3f, 0.3f);
                    break;

                case BlockType.Tower:
                case BlockType.TowerArcher:
                case BlockType.TowerCannon:
                case BlockType.TowerMage:
                    preview = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    preview.transform.localScale = new Vector3(1.5f, 4f, 1.5f);
                    break;

                case BlockType.Turret:
                    preview = CreateTurretPreview();
                    break;

                case BlockType.Flag:
                case BlockType.Banner:
                    preview = CreateFlagPreview();
                    break;

                case BlockType.Gate:
                case BlockType.WallGate:
                    preview = CreateGatePreview();
                    break;

                case BlockType.Barracks:
                case BlockType.Armory:
                case BlockType.Workshop:
                case BlockType.Warehouse:
                    preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    preview.transform.localScale = new Vector3(4f, 3f, 4f);
                    break;

                case BlockType.Mine:
                case BlockType.Foundry:
                case BlockType.Sawmill:
                    preview = CreateProductionBuildingPreview();
                    break;

                case BlockType.Moat:
                    preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    preview.transform.localScale = new Vector3(2f, 0.5f, 2f);
                    break;

                case BlockType.Spikes:
                case BlockType.TrapFire:
                case BlockType.TrapPit:
                    preview = CreateTrapPreview();
                    break;

                default:
                    preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    preview.transform.localScale = Vector3.one * gridSize * 2;
                    break;
            }

            return preview;
        }

        private GameObject CreateTurretPreview()
        {
            GameObject parent = new GameObject("TurretPreview");

            // Base
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.transform.SetParent(parent.transform);
            baseObj.transform.localScale = new Vector3(1f, 0.3f, 1f);
            baseObj.transform.localPosition = new Vector3(0, 0.15f, 0);

            // Barrel
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.transform.SetParent(parent.transform);
            barrel.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
            barrel.transform.localPosition = new Vector3(0, 0.5f, 0.3f);
            barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);

            return parent;
        }

        private GameObject CreateFlagPreview()
        {
            GameObject parent = new GameObject("FlagPreview");

            // Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(parent.transform);
            pole.transform.localScale = new Vector3(0.1f, 2f, 0.1f);
            pole.transform.localPosition = new Vector3(0, 1f, 0);

            // Flag
            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.transform.SetParent(parent.transform);
            flag.transform.localScale = new Vector3(0.8f, 0.5f, 0.05f);
            flag.transform.localPosition = new Vector3(0.4f, 1.75f, 0);

            return parent;
        }

        private GameObject CreateGatePreview()
        {
            GameObject parent = new GameObject("GatePreview");

            // Left pillar
            GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
            left.transform.SetParent(parent.transform);
            left.transform.localScale = new Vector3(0.5f, 3f, 0.5f);
            left.transform.localPosition = new Vector3(-1f, 1.5f, 0);

            // Right pillar
            GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
            right.transform.SetParent(parent.transform);
            right.transform.localScale = new Vector3(0.5f, 3f, 0.5f);
            right.transform.localPosition = new Vector3(1f, 1.5f, 0);

            // Top
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.transform.SetParent(parent.transform);
            top.transform.localScale = new Vector3(2.5f, 0.5f, 0.5f);
            top.transform.localPosition = new Vector3(0, 3.25f, 0);

            return parent;
        }

        private GameObject CreateProductionBuildingPreview()
        {
            GameObject parent = new GameObject("ProductionPreview");

            // Main building
            GameObject main = GameObject.CreatePrimitive(PrimitiveType.Cube);
            main.transform.SetParent(parent.transform);
            main.transform.localScale = new Vector3(3f, 2f, 3f);
            main.transform.localPosition = new Vector3(0, 1f, 0);

            // Chimney
            GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chimney.transform.SetParent(parent.transform);
            chimney.transform.localScale = new Vector3(0.4f, 1f, 0.4f);
            chimney.transform.localPosition = new Vector3(0.8f, 2.5f, 0.8f);

            return parent;
        }

        private GameObject CreateTrapPreview()
        {
            GameObject parent = new GameObject("TrapPreview");

            // Base plate
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.transform.SetParent(parent.transform);
            plate.transform.localScale = new Vector3(1f, 0.1f, 1f);
            plate.transform.localPosition = new Vector3(0, 0.05f, 0);

            // Spikes
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spike.transform.SetParent(parent.transform);
                spike.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);
                spike.transform.localPosition = new Vector3(Mathf.Sin(angle) * 0.3f, 0.35f, Mathf.Cos(angle) * 0.3f);
                spike.transform.localRotation = Quaternion.Euler(0, i * 90f, 0);
            }

            return parent;
        }

        private void UpdatePreviewPosition()
        {
            if (previewObject == null) return;

            Vector3 worldPos = GetMouseWorldPosition();

            // Snap to grid
            if (enableSnapping)
            {
                worldPos = SnapToGrid(worldPos);
            }

            // Set height
            worldPos.y = placementHeight;

            // Update preview
            previewObject.transform.position = worldPos;
            previewObject.transform.rotation = Quaternion.Euler(0, currentRotation, 0);

            // Animate preview (subtle pulse)
            float pulse = 1f + Mathf.Sin(Time.time * previewPulseSpeed) * previewPulseAmount;
            previewObject.transform.localScale *= pulse / (1f + previewPulseAmount);

            // Update validation
            var validation = ValidatePlacement(worldPos);
            if (lastValidation == null || lastValidation.IsValid != validation.IsValid)
            {
                lastValidation = validation;
                UpdatePreviewMaterial(validation.IsValid);
                OnValidationChanged?.Invoke(validation);
            }

            // Update grid highlight
            if (showPlacementGrid && gridHighlight != null)
            {
                gridHighlight.SetActive(true);
                gridHighlight.transform.position = new Vector3(worldPos.x, 0.01f, worldPos.z);
            }
        }

        private void UpdatePreviewMaterial(bool valid)
        {
            if (previewObject == null) return;

            Material mat = valid ? previewValidMaterial : previewInvalidMaterial;
            foreach (var renderer in previewObject.GetComponentsInChildren<Renderer>())
            {
                renderer.material = mat;
            }
        }

        #endregion

        #region Multi-Placement

        /// <summary>
        /// Start multi-placement (drag to place multiple)
        /// </summary>
        public void StartMultiPlacement()
        {
            if (!isPlacementActive) return;

            isMultiPlacing = true;
            multiPlaceStart = GetMouseWorldPosition();
            multiPlacePositions.Clear();
        }

        /// <summary>
        /// Update multi-placement during drag
        /// </summary>
        public void UpdateMultiPlacement()
        {
            if (!isMultiPlacing) return;

            Vector3 end = GetMouseWorldPosition();
            if (enableSnapping)
            {
                end = SnapToGrid(end);
            }

            // Calculate positions along line
            multiPlacePositions.Clear();
            ClearMultiPreviews();

            Vector3 start = SnapToGrid(multiPlaceStart);
            float distance = Vector3.Distance(start, end);
            int count = Mathf.Max(1, Mathf.FloorToInt(distance / gridSize));

            for (int i = 0; i <= count; i++)
            {
                float t = count > 0 ? (float)i / count : 0;
                Vector3 pos = Vector3.Lerp(start, end, t);
                pos = SnapToGrid(pos);
                pos.y = placementHeight;

                multiPlacePositions.Add(pos);

                // Create preview
                GameObject preview = CreateBlockPreview(currentBlockType);
                preview.transform.position = pos;
                preview.transform.rotation = Quaternion.Euler(0, currentRotation, 0);

                bool valid = ValidatePlacement(pos).IsValid;
                foreach (var r in preview.GetComponentsInChildren<Renderer>())
                {
                    r.material = valid ? previewValidMaterial : previewInvalidMaterial;
                }
                foreach (var c in preview.GetComponentsInChildren<Collider>())
                {
                    c.enabled = false;
                }

                multiPreviews.Add(preview);
            }
        }

        /// <summary>
        /// Confirm multi-placement
        /// </summary>
        public void ConfirmMultiPlacement()
        {
            if (!isMultiPlacing) return;

            int placed = 0;
            foreach (var pos in multiPlacePositions)
            {
                var validation = ValidatePlacement(pos);
                if (validation.IsValid)
                {
                    var blockDef = BlockDefinitions.Get(currentBlockType);
                    if (blockDef == null || ResourceInventory.Instance == null ||
                        ResourceInventory.Instance.HasResource(ResourceType.Stone, blockDef.ResourceCost))
                    {
                        ResourceInventory.Instance?.RemoveResource(ResourceType.Stone, blockDef?.ResourceCost ?? 0);
                        OnBlockPlaced?.Invoke(currentBlockType, pos, Quaternion.Euler(0, currentRotation, 0));
                        placed++;
                    }
                }
            }

            if (placed > 0)
            {
                PlaySound(placementSound, 0.7f);
            }

            EndMultiPlacement();
        }

        /// <summary>
        /// Cancel multi-placement
        /// </summary>
        public void EndMultiPlacement()
        {
            isMultiPlacing = false;
            multiPlacePositions.Clear();
            ClearMultiPreviews();
        }

        private void ClearMultiPreviews()
        {
            foreach (var preview in multiPreviews)
            {
                if (preview != null) Destroy(preview);
            }
            multiPreviews.Clear();
        }

        #endregion

        #region Validation

        private PlacementValidation ValidatePlacement(Vector3 position)
        {
            // Check overlaps with existing blocks
            Collider[] overlaps = Physics.OverlapSphere(position, overlapCheckRadius, buildingLayer);
            if (overlaps.Length > 0)
            {
                return PlacementValidation.Invalid("Overlapping existing structure");
            }

            // Check if on valid ground
            if (!Physics.Raycast(position + Vector3.up * 5, Vector3.down, 10f, groundLayer))
            {
                return PlacementValidation.Invalid("Invalid ground");
            }

            // Check territory bounds (if applicable)
            if (BaseEditor.Instance != null && !BaseEditor.Instance.IsEditorActive)
            {
                // Territory check would go here
            }

            // Check resources
            var blockDef = BlockDefinitions.Get(currentBlockType);
            if (blockDef != null && ResourceInventory.Instance != null)
            {
                if (!ResourceInventory.Instance.HasResource(ResourceType.Stone, blockDef.ResourceCost))
                {
                    return PlacementValidation.Invalid($"Need {blockDef.ResourceCost} stone");
                }
            }

            return PlacementValidation.Valid();
        }

        #endregion

        #region Input Handling

        private void HandlePlacementInput()
        {
            // Left click to place
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    StartMultiPlacement();
                }
                else
                {
                    ConfirmPlacement();
                }
            }

            // Shift+drag for multi-place
            if (Input.GetMouseButton(0) && isMultiPlacing)
            {
                UpdateMultiPlacement();
            }

            if (Input.GetMouseButtonUp(0) && isMultiPlacing)
            {
                ConfirmMultiPlacement();
            }

            // Right click to cancel
            if (Input.GetMouseButtonDown(1))
            {
                if (isMultiPlacing)
                {
                    EndMultiPlacement();
                }
                else
                {
                    CancelPlacement();
                }
            }

            // Scroll to rotate
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.1f && Input.GetKey(KeyCode.LeftControl))
            {
                Rotate(scroll * rotationSnap);
            }

            // Q/E to rotate
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Rotate(-rotationSnap);
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                Rotate(rotationSnap);
            }

            // Escape to cancel
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }

            // Tab to toggle snapping
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                enableSnapping = !enableSnapping;
                ApexLogger.LogVerbose($"Snapping: {enableSnapping}", ApexLogger.LogCategory.Building);
            }
        }

        private bool IsPointerOverUI()
        {
            return UnityEngine.EventSystems.EventSystem.current != null &&
                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }

        #endregion

        #region Utility

        private Vector3 GetMouseWorldPosition()
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return Vector3.zero;

            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, placementHeight, 0));

            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.z = Mathf.Round(position.z / gridSize) * gridSize;
            return position;
        }

        private void CreatePlacementEffect(Vector3 position)
        {
            // Create a simple particle effect
            GameObject effect = new GameObject("PlacementEffect");
            effect.transform.position = position;

            ParticleSystem ps = effect.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.3f;
            main.startLifetime = 0.5f;
            main.startSpeed = 3f;
            main.startSize = 0.1f;
            main.startColor = new Color(0.3f, 0.8f, 0.4f);
            main.loop = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 20) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            ps.Play();
            Destroy(effect, 1f);
        }

        private void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (previewObject != null) Destroy(previewObject);
            if (gridHighlight != null) Destroy(gridHighlight);
            ClearMultiPreviews();
        }
    }
}
