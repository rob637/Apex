using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Territory;
using ApexCitadels.Player;
#if FIREBASE_ENABLED
using Firebase.Firestore;
using Firebase.Extensions;
#endif

namespace ApexCitadels.Building
{
    /// <summary>
    /// Manages building placement, block inventory, and construction.
    /// </summary>
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        [Header("Building Settings")]
        [SerializeField] private float maxBuildDistance = 5f;
        [SerializeField] private float blockSnapSize = 0.5f;
        [SerializeField] private bool enableSnapping = true;

        [Header("Prefabs")]
        [SerializeField] private GameObject[] blockPrefabs;
        [SerializeField] private GameObject placementPreviewPrefab;

        [Header("Materials")]
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;

        // Events
        public event Action<BuildingBlock> OnBlockPlaced;
        public event Action<BuildingBlock> OnBlockDestroyed;
        public event Action<BlockType> OnBlockSelected;

        // State
        private BlockType _selectedBlockType = BlockType.Stone;
        private GameObject _placementPreview;
        private bool _isPlacementMode = false;
        private Dictionary<string, GameObject> _placedBlocks = new Dictionary<string, GameObject>();

        // Prefab lookup
        private Dictionary<BlockType, GameObject> _blockPrefabLookup = new Dictionary<BlockType, GameObject>();

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

        private void InitializePrefabLookup()
        {
            // In real implementation, load from Resources or Addressables
            // For now, create simple primitives
            foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
            {
                // These will be replaced with actual prefabs
                _blockPrefabLookup[type] = null;
            }
        }

        private void Update()
        {
            if (_isPlacementMode)
            {
                UpdatePlacementPreview();
            }
        }

        #region Block Selection

        /// <summary>
        /// Select a block type to place
        /// </summary>
        public void SelectBlock(BlockType type)
        {
            _selectedBlockType = type;
            OnBlockSelected?.Invoke(type);
            Debug.Log($"[BuildingManager] Selected block: {type}");
        }

        /// <summary>
        /// Get currently selected block type
        /// </summary>
        public BlockType GetSelectedBlock() => _selectedBlockType;

        /// <summary>
        /// Get available block types for the player's current territory level
        /// </summary>
        public List<BlockType> GetAvailableBlocks(int territoryLevel)
        {
            List<BlockType> available = new List<BlockType>();
            
            foreach (var kvp in BlockDefinition.Definitions)
            {
                if (kvp.Value.MinTerritoryLevel <= territoryLevel)
                {
                    available.Add(kvp.Key);
                }
            }

            return available;
        }

        #endregion

        #region Placement Mode

        /// <summary>
        /// Enter placement mode - show preview of block
        /// </summary>
        public void EnterPlacementMode()
        {
            _isPlacementMode = true;
            CreatePlacementPreview();
            Debug.Log("[BuildingManager] Entered placement mode");
        }

        /// <summary>
        /// Exit placement mode - hide preview
        /// </summary>
        public void ExitPlacementMode()
        {
            _isPlacementMode = false;
            DestroyPlacementPreview();
            Debug.Log("[BuildingManager] Exited placement mode");
        }

        /// <summary>
        /// Toggle placement mode
        /// </summary>
        public void TogglePlacementMode()
        {
            if (_isPlacementMode)
                ExitPlacementMode();
            else
                EnterPlacementMode();
        }

        private void CreatePlacementPreview()
        {
            DestroyPlacementPreview();

            // Create preview object
            _placementPreview = CreateBlockPrimitive(_selectedBlockType);
            _placementPreview.name = "PlacementPreview";

            // Remove collider from preview
            var collider = _placementPreview.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Set preview material
            var renderer = _placementPreview.GetComponent<Renderer>();
            if (renderer != null && validPlacementMaterial != null)
            {
                renderer.material = validPlacementMaterial;
            }
            else if (renderer != null)
            {
                // Create semi-transparent material
                renderer.material.color = new Color(0, 1, 0, 0.5f);
            }
        }

        private void DestroyPlacementPreview()
        {
            if (_placementPreview != null)
            {
                Destroy(_placementPreview);
                _placementPreview = null;
            }
        }

        private void UpdatePlacementPreview()
        {
            if (_placementPreview == null) return;

            // Get position from raycast or AR hit
            Vector3 targetPosition = GetPlacementPosition();
            
            // Apply snapping if enabled
            if (enableSnapping)
            {
                targetPosition = SnapPosition(targetPosition);
            }

            _placementPreview.transform.position = targetPosition;

            // Check if placement is valid
            bool isValid = IsPlacementValid(targetPosition);
            UpdatePreviewMaterial(isValid);
        }

        private Vector3 GetPlacementPosition()
        {
            // For AR, this would use AR raycast
            // For desktop testing, use mouse raycast
            Camera cam = Camera.main;
            if (cam == null) return Vector3.zero;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return cam.transform.position + cam.transform.forward * 3f;
        }

        private Vector3 SnapPosition(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x / blockSnapSize) * blockSnapSize,
                Mathf.Round(position.y / blockSnapSize) * blockSnapSize,
                Mathf.Round(position.z / blockSnapSize) * blockSnapSize
            );
        }

        private bool IsPlacementValid(Vector3 position)
        {
            // Check distance from player/camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                float distance = Vector3.Distance(cam.transform.position, position);
                if (distance > maxBuildDistance) return false;
            }

            // Check for overlapping blocks
            Collider[] overlaps = Physics.OverlapSphere(position, 0.2f);
            foreach (var overlap in overlaps)
            {
                if (overlap.CompareTag("BuildingBlock"))
                {
                    return false;
                }
            }

            // Check territory requirements
            BlockDefinition def = BlockDefinition.Get(_selectedBlockType);
            if (def != null && def.RequiresTerritory)
            {
                // TODO: Check if position is within player's territory
                // For now, allow placement anywhere
            }

            return true;
        }

        private void UpdatePreviewMaterial(bool isValid)
        {
            if (_placementPreview == null) return;

            var renderer = _placementPreview.GetComponent<Renderer>();
            if (renderer == null) return;

            if (isValid && validPlacementMaterial != null)
            {
                renderer.material = validPlacementMaterial;
            }
            else if (!isValid && invalidPlacementMaterial != null)
            {
                renderer.material = invalidPlacementMaterial;
            }
            else
            {
                renderer.material.color = isValid 
                    ? new Color(0, 1, 0, 0.5f) 
                    : new Color(1, 0, 0, 0.5f);
            }
        }

        #endregion

        #region Block Placement

        /// <summary>
        /// Place a block at the current preview position
        /// </summary>
        public async Task<PlacementResult> PlaceBlock()
        {
            if (!_isPlacementMode || _placementPreview == null)
            {
                return new PlacementResult(false, "Not in placement mode!");
            }

            Vector3 position = _placementPreview.transform.position;
            
            if (!IsPlacementValid(position))
            {
                return new PlacementResult(false, "Invalid placement location!");
            }

            // Check resources (TODO: implement resource system)
            BlockDefinition def = BlockDefinition.Get(_selectedBlockType);
            if (def != null)
            {
                // TODO: Check if player has enough resources
                // if (!PlayerInventory.HasResources(def.ResourceCost))
                //     return new PlacementResult(false, "Not enough resources!");
            }

            // Create the block
            BuildingBlock block = new BuildingBlock(_selectedBlockType)
            {
                LocalPosition = position,
                LocalRotation = _placementPreview.transform.rotation,
                // TODO: Get GPS coordinates from AR system
            };

            // Create visual
            GameObject blockObject = CreateBlockPrimitive(_selectedBlockType);
            blockObject.name = $"Block_{block.Id}";
            blockObject.transform.position = position;
            blockObject.transform.rotation = block.LocalRotation;
            blockObject.tag = "BuildingBlock";

            _placedBlocks[block.Id] = blockObject;

            // Save to cloud
            await SaveBlockToCloud(block);

            // Fire event
            OnBlockPlaced?.Invoke(block);

            Debug.Log($"[BuildingManager] Placed {_selectedBlockType} at {position}");
            return new PlacementResult(true, "Block placed!", block);
        }

        /// <summary>
        /// Place a block at a specific position (used for loading saved blocks)
        /// </summary>
        public GameObject PlaceBlockAt(BuildingBlock block)
        {
            GameObject blockObject = CreateBlockPrimitive(block.Type);
            blockObject.name = $"Block_{block.Id}";
            blockObject.transform.position = block.LocalPosition;
            blockObject.transform.rotation = block.LocalRotation;
            blockObject.transform.localScale = block.LocalScale;
            blockObject.tag = "BuildingBlock";

            _placedBlocks[block.Id] = blockObject;

            return blockObject;
        }

        /// <summary>
        /// Destroy a placed block
        /// </summary>
        public async Task<bool> DestroyBlock(string blockId)
        {
            if (_placedBlocks.TryGetValue(blockId, out GameObject blockObject))
            {
                Destroy(blockObject);
                _placedBlocks.Remove(blockId);

                // Remove from cloud
                await RemoveBlockFromCloud(blockId);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Load all blocks in a territory from cloud
        /// </summary>
        public async Task LoadBlocksForTerritory(string territoryId)
        {
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                var query = db.Collection("buildings")
                    .WhereEqualTo("territoryId", territoryId);
                
                var snapshot = await query.GetSnapshotAsync();
                
                foreach (var doc in snapshot.Documents)
                {
                    var block = BuildingBlock.FromFirestore(doc);
                    if (block != null && !_placedBlocks.ContainsKey(block.Id))
                    {
                        PlaceBlockAt(block);
                    }
                }
                
                Debug.Log($"[BuildingManager] Loaded {snapshot.Count} blocks for territory {territoryId}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildingManager] Failed to load blocks: {ex.Message}");
            }
#else
            await Task.CompletedTask;
#endif
        }

        private GameObject CreateBlockPrimitive(BlockType type)
        {
            // Check if we have a prefab
            if (_blockPrefabLookup.TryGetValue(type, out GameObject prefab) && prefab != null)
            {
                return Instantiate(prefab);
            }

            // Create primitive based on type
            GameObject block;
            switch (type)
            {
                case BlockType.Wall:
                    block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.transform.localScale = new Vector3(2f, 3f, 0.3f);
                    break;

                case BlockType.Tower:
                    block = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    block.transform.localScale = new Vector3(1f, 4f, 1f);
                    break;

                case BlockType.Flag:
                    block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.transform.localScale = new Vector3(0.1f, 2f, 0.5f);
                    break;

                default:
                    block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    break;
            }

            // Set color based on type
            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = GetBlockColor(type);
            }

            return block;
        }

        private Color GetBlockColor(BlockType type)
        {
            switch (type)
            {
                case BlockType.Stone: return new Color(0.5f, 0.5f, 0.5f);
                case BlockType.Wood: return new Color(0.6f, 0.4f, 0.2f);
                case BlockType.Metal: return new Color(0.7f, 0.7f, 0.8f);
                case BlockType.Glass: return new Color(0.8f, 0.9f, 1f, 0.5f);
                case BlockType.Wall: return new Color(0.4f, 0.4f, 0.4f);
                case BlockType.Tower: return new Color(0.3f, 0.3f, 0.35f);
                case BlockType.Flag: return new Color(1f, 0f, 0f);
                case BlockType.Beacon: return new Color(1f, 1f, 0f);
                default: return Color.white;
            }
        }

        #endregion

        #region Cloud Sync

        private async Task SaveBlockToCloud(BuildingBlock block)
        {
            Debug.Log($"[BuildingManager] Saving block {block.Id} to cloud...");
            
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                var docRef = db.Collection("buildings").Document(block.Id);
                
                string playerId = PlayerManager.Instance?.GetCurrentPlayerId() ?? "";
                
                await docRef.SetAsync(new Dictionary<string, object>
                {
                    { "type", (int)block.Type },
                    { "ownerId", playerId },
                    { "territoryId", block.TerritoryId ?? "" },
                    { "localPosition", new Dictionary<string, object>
                        {
                            { "x", block.LocalPosition.x },
                            { "y", block.LocalPosition.y },
                            { "z", block.LocalPosition.z }
                        }
                    },
                    { "localRotation", new Dictionary<string, object>
                        {
                            { "x", block.LocalRotation.x },
                            { "y", block.LocalRotation.y },
                            { "z", block.LocalRotation.z },
                            { "w", block.LocalRotation.w }
                        }
                    },
                    { "localScale", new Dictionary<string, object>
                        {
                            { "x", block.LocalScale.x },
                            { "y", block.LocalScale.y },
                            { "z", block.LocalScale.z }
                        }
                    },
                    { "health", block.Health },
                    { "maxHealth", block.MaxHealth },
                    { "latitude", block.Latitude },
                    { "longitude", block.Longitude },
                    { "placedAt", Timestamp.FromDateTime(block.PlacedAt.ToUniversalTime()) }
                }, SetOptions.MergeAll);
                
                Debug.Log("[BuildingManager] Block saved to Firestore");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BuildingManager] Failed to save block: {ex.Message}");
            }
#else
            await Task.Delay(100);
            Debug.Log("[BuildingManager] Block saved (stub)");
#endif
        }

        private async Task RemoveBlockFromCloud(string blockId)
        {
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                await db.Collection("buildings").Document(blockId).DeleteAsync();
                Debug.Log($"[BuildingManager] Block {blockId} removed from Firestore");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildingManager] Failed to remove block: {ex.Message}");
            }
#else
            await Task.CompletedTask;
#endif
        }

        #endregion
    }

    /// <summary>
    /// Result of a block placement attempt
    /// </summary>
    public class PlacementResult
    {
        public bool Success;
        public string Message;
        public BuildingBlock Block;

        public PlacementResult(bool success, string message, BuildingBlock block = null)
        {
            Success = success;
            Message = message;
            Block = block;
        }
    }
}
