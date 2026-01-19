// ============================================================================
// APEX CITADELS - BUILDING MODEL PROVIDER
// Integrates the 3D model library with the building system
// Uses GameAssetDatabase to provide real models instead of primitives
// ============================================================================
using UnityEngine;
using System;
using System.Collections.Generic;
using ApexCitadels.Core.Assets;

// Use type aliases to avoid conflicts with local BuildingCategory enum
using AssetBuildingCategory = ApexCitadels.Core.Assets.BuildingCategory;
using AssetTowerType = ApexCitadels.Core.Assets.TowerType;
using AssetWallType = ApexCitadels.Core.Assets.WallType;
using AssetWallMaterial = ApexCitadels.Core.Assets.WallMaterial;

namespace ApexCitadels.PC.Buildings
{
    /// <summary>
    /// Provides 3D models from the GameAssetDatabase for building generation.
    /// This bridges the gap between the procedural building system and the
    /// pre-made 3D models from Meshy.
    /// </summary>
    public class BuildingModelProvider : MonoBehaviour
    {
        [Header("Asset Database")]
        [SerializeField] private GameAssetDatabase assetDatabase;
        
        [Header("Fallback Settings")]
        [SerializeField] private bool useFallbackPrimitives = true;
        [SerializeField] private Material fallbackMaterial;
        
        [Header("Model Settings")]
        [SerializeField] private float defaultScale = 1f;
        [SerializeField] private bool randomizeScale = true;
        [SerializeField] private Vector2 scaleVariation = new Vector2(0.9f, 1.1f);
        
        // Singleton with lazy initialization
        private static BuildingModelProvider _instance;
        private static bool _searchedForInstance = false;
        
        public static BuildingModelProvider Instance
        {
            get
            {
                if (_instance == null && !_searchedForInstance)
                {
                    _searchedForInstance = true;
                    _instance = FindFirstObjectByType<BuildingModelProvider>();
                    
                    if (_instance == null)
                    {
                        // Auto-create if not found
                        Debug.Log("[BuildingModelProvider] Auto-creating instance...");
                        var go = new GameObject("BuildingModelProvider");
                        _instance = go.AddComponent<BuildingModelProvider>();
                    }
                    
                    // Ensure database is loaded
                    if (_instance != null && _instance.assetDatabase == null)
                    {
                        _instance.LoadAssetDatabase();
                    }
                }
                return _instance;
            }
        }
        
        // Cache
        private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
        
        private void Awake()
        {
            Debug.Log("[BuildingModelProvider] Awake called");
            
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _searchedForInstance = true;
            
            LoadAssetDatabase();
        }
        
        /// <summary>
        /// Check if the database is loaded and has content
        /// </summary>
        public bool HasDatabase => assetDatabase != null && assetDatabase.BuildingModels.Count > 0;
        
        private void LoadAssetDatabase()
        {
            Debug.Log("[BuildingModelProvider] LoadAssetDatabase called...");
            
            if (assetDatabase == null)
            {
                Debug.Log("[BuildingModelProvider] Trying to load GameAssetDatabase from Resources...");
                assetDatabase = UnityEngine.Resources.Load<GameAssetDatabase>("GameAssetDatabase");
            }
            
            if (assetDatabase == null)
            {
                Debug.LogWarning("[BuildingModelProvider] GameAssetDatabase not found in Resources folder! " +
                    "Make sure GameAssetDatabase.asset exists in Assets/Resources/GameAssetDatabase.asset");
            }
            else
            {
                Debug.Log($"[BuildingModelProvider] ✓ Loaded asset database with {assetDatabase.BuildingModels.Count} buildings, " +
                         $"{assetDatabase.TowerModels.Count} towers, {assetDatabase.WallModels.Count} walls");
                
                // Log first few entries to verify
                for (int i = 0; i < Mathf.Min(3, assetDatabase.BuildingModels.Count); i++)
                {
                    var b = assetDatabase.BuildingModels[i];
                    Debug.Log($"[BuildingModelProvider]   Building[{i}]: {b.Id}, Model={b.Model?.name ?? "NULL"}");
                }
            }
        }
        
        #region Public API - Get Models
        
        /// <summary>
        /// Get a building model by category
        /// </summary>
        public GameObject GetBuildingModel(AssetBuildingCategory category, Transform parent = null)
        {
            if (assetDatabase == null)
                return CreateFallbackCube(parent);
            
            var entry = assetDatabase.GetRandomBuilding(category);
            return InstantiateModel(entry, parent);
        }
        
        /// <summary>
        /// Get a specific building by ID
        /// </summary>
        public GameObject GetBuildingById(string id, Transform parent = null)
        {
            if (assetDatabase == null)
                return CreateFallbackCube(parent);
            
            var entry = assetDatabase.GetBuilding(id);
            return InstantiateModel(entry, parent);
        }
        
        /// <summary>
        /// Get a tower model by type
        /// </summary>
        public GameObject GetTowerModel(AssetTowerType type, Transform parent = null)
        {
            if (assetDatabase == null)
                return CreateFallbackTower(parent);
            
            var entry = assetDatabase.GetRandomTower(type);
            return InstantiateTowerModel(entry, parent);
        }
        
        /// <summary>
        /// Get a wall segment
        /// </summary>
        public GameObject GetWallModel(AssetWallType type, AssetWallMaterial material = AssetWallMaterial.Stone, Transform parent = null)
        {
            if (assetDatabase == null)
                return CreateFallbackWall(parent);
            
            var entry = assetDatabase.GetWall(type, material);
            return InstantiateWallModel(entry, parent);
        }
        
        /// <summary>
        /// Get a gate model
        /// </summary>
        public GameObject GetGateModel(Transform parent = null)
        {
            if (assetDatabase == null)
                return CreateFallbackWall(parent);
            
            // Find a gate from wall models
            foreach (var wall in assetDatabase.WallModels)
            {
                if (wall.IsGate)
                {
                    return InstantiateWallModel(wall, parent);
                }
            }
            
            return CreateFallbackWall(parent);
        }
        
        /// <summary>
        /// Get a foundation/platform model based on citadel level
        /// Buildings are named B01_*, B02_*, etc. We pick appropriate buildings as foundations.
        /// </summary>
        public GameObject GetFoundationModel(int level, Transform parent = null)
        {
            Debug.Log($"[BuildingModelProvider] GetFoundationModel called, level={level}, database={assetDatabase != null}");
            
            if (assetDatabase == null)
            {
                Debug.Log("[BuildingModelProvider] No database, returning fallback cylinder");
                return CreateFallbackCylinder(parent);
            }
            
            Debug.Log($"[BuildingModelProvider] Database has {assetDatabase.BuildingModels.Count} buildings");
            
            // First: just use ANY available building model - we know we have 27 of them
            if (assetDatabase.BuildingModels.Count > 0)
            {
                // Pick a building based on level for variety
                int index = (level * 3) % assetDatabase.BuildingModels.Count;
                var entry = assetDatabase.BuildingModels[index];
                if (entry != null && entry.Model != null)
                {
                    Debug.Log($"[BuildingModelProvider] Using building [{index}]: {entry.Id} = {entry.Model.name}");
                    return InstantiateModel(entry, parent);
                }
                else
                {
                    Debug.LogWarning($"[BuildingModelProvider] Entry or Model is null at index {index}");
                }
            }
            
            Debug.Log("[BuildingModelProvider] Returning fallback cylinder");
            return CreateFallbackCylinder(parent);
        }
        
        /// <summary>
        /// Get random decoration/detail model
        /// </summary>
        public GameObject GetDecorationModel(string type, Transform parent = null)
        {
            // Could be extended to support decorations from asset database
            return null;
        }
        
        #endregion
        
        #region Model Instantiation
        
        private GameObject InstantiateModel(BuildingModelEntry entry, Transform parent)
        {
            if (entry == null || entry.Model == null)
                return CreateFallbackCube(parent);
            
            GameObject instance;
            
            // Use prefab if available, otherwise instantiate from model
            if (entry.Prefab != null)
            {
                instance = Instantiate(entry.Prefab, parent);
            }
            else
            {
                instance = Instantiate(entry.Model, parent);
            }
            
            // Apply random scale variation
            if (randomizeScale)
            {
                float scale = defaultScale * UnityEngine.Random.Range(scaleVariation.x, scaleVariation.y);
                instance.transform.localScale = Vector3.one * scale;
            }
            
            instance.name = $"Building_{entry.Id}_{entry.Name}";
            return instance;
        }
        
        private GameObject InstantiateTowerModel(TowerModelEntry entry, Transform parent)
        {
            if (entry == null || entry.Model == null)
                return CreateFallbackTower(parent);
            
            GameObject instance;
            
            if (entry.Prefab != null)
            {
                instance = Instantiate(entry.Prefab, parent);
            }
            else
            {
                instance = Instantiate(entry.Model, parent);
            }
            
            if (randomizeScale)
            {
                float scale = defaultScale * UnityEngine.Random.Range(scaleVariation.x, scaleVariation.y);
                instance.transform.localScale = Vector3.one * scale;
            }
            
            instance.name = $"Tower_{entry.Id}_{entry.Name}";
            return instance;
        }
        
        private GameObject InstantiateWallModel(WallModelEntry entry, Transform parent)
        {
            if (entry == null || entry.Model == null)
                return CreateFallbackWall(parent);
            
            GameObject instance;
            
            if (entry.Prefab != null)
            {
                instance = Instantiate(entry.Prefab, parent);
            }
            else
            {
                instance = Instantiate(entry.Model, parent);
            }
            
            instance.name = $"Wall_{entry.Id}_{entry.Name}";
            return instance;
        }
        
        #endregion
        
        #region Fallback Primitives
        
        private GameObject CreateFallbackCube(Transform parent)
        {
            if (!useFallbackPrimitives) return null;
            
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(parent);
            cube.transform.localScale = new Vector3(4f, 5f, 4f);
            
            if (fallbackMaterial != null)
            {
                cube.GetComponent<Renderer>().material = fallbackMaterial;
            }
            
            cube.name = "Building_Fallback";
            return cube;
        }
        
        private GameObject CreateFallbackTower(Transform parent)
        {
            if (!useFallbackPrimitives) return null;
            
            var tower = new GameObject("Tower_Fallback");
            tower.transform.SetParent(parent);
            
            // Base
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.transform.SetParent(tower.transform);
            baseObj.transform.localScale = new Vector3(3f, 5f, 3f);
            baseObj.transform.localPosition = new Vector3(0, 2.5f, 0);
            
            // Top (Unity doesn't have Cone primitive, use scaled cylinder)
            var top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            top.transform.SetParent(tower.transform);
            top.transform.localScale = new Vector3(4f, 2f, 4f);
            top.transform.localPosition = new Vector3(0, 6f, 0);
            
            if (fallbackMaterial != null)
            {
                baseObj.GetComponent<Renderer>().material = fallbackMaterial;
                top.GetComponent<Renderer>().material = fallbackMaterial;
            }
            
            return tower;
        }
        
        private GameObject CreateFallbackWall(Transform parent)
        {
            if (!useFallbackPrimitives) return null;
            
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(parent);
            wall.transform.localScale = new Vector3(4f, 3f, 0.5f);
            
            if (fallbackMaterial != null)
            {
                wall.GetComponent<Renderer>().material = fallbackMaterial;
            }
            
            wall.name = "Wall_Fallback";
            return wall;
        }
        
        private GameObject CreateFallbackCylinder(Transform parent)
        {
            if (!useFallbackPrimitives) return null;
            
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(parent);
            cylinder.transform.localScale = new Vector3(15f, 2f, 15f);
            
            if (fallbackMaterial != null)
            {
                cylinder.GetComponent<Renderer>().material = fallbackMaterial;
            }
            
            cylinder.name = "Foundation_Fallback";
            return cylinder;
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Check if the asset database is loaded and has content
        /// </summary>
        public bool HasAssets()
        {
            return assetDatabase != null && 
                   (assetDatabase.BuildingModels.Count > 0 || 
                    assetDatabase.TowerModels.Count > 0 ||
                    assetDatabase.WallModels.Count > 0);
        }
        
        /// <summary>
        /// Get total number of available models
        /// </summary>
        public int GetTotalModelCount()
        {
            if (assetDatabase == null) return 0;
            return assetDatabase.BuildingModels.Count + 
                   assetDatabase.TowerModels.Count + 
                   assetDatabase.WallModels.Count;
        }
        
        /// <summary>
        /// Get all building categories that have at least one model
        /// </summary>
        public List<AssetBuildingCategory> GetAvailableCategories()
        {
            var categories = new List<AssetBuildingCategory>();
            if (assetDatabase == null) return categories;
            
            foreach (AssetBuildingCategory cat in Enum.GetValues(typeof(AssetBuildingCategory)))
            {
                bool hasAny = false;
                foreach (var building in assetDatabase.BuildingModels)
                {
                    if (building.Category.Equals(cat))
                    {
                        hasAny = true;
                        break;
                    }
                }
                if (hasAny) categories.Add(cat);
            }
            
            return categories;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Extension methods to integrate with existing building system
    /// </summary>
    public static class BuildingModelExtensions
    {
        /// <summary>
        /// Replace all child primitives with real models from database
        /// </summary>
        public static void ReplaceWithRealModels(this GameObject building, BuildingModelProvider provider)
        {
            if (provider == null || !provider.HasAssets()) return;
            
            // This is a placeholder for more sophisticated replacement logic
            // Could analyze the building structure and swap components
            Debug.Log($"[BuildingModelExtensions] Would replace primitives in {building.name}");
        }
    }
}
