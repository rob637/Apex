// ============================================================================
// APEX CITADELS - BUILDING MODEL PROVIDER
// Integrates the 3D model library with the building system
// Uses GameAssetDatabase to provide real models instead of primitives
// ============================================================================
using UnityEngine;
using System;
using System.Collections.Generic;
using ApexCitadels.Core.Assets;

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
        
        // Singleton
        private static BuildingModelProvider _instance;
        public static BuildingModelProvider Instance => _instance;
        
        // Cache
        private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            LoadAssetDatabase();
        }
        
        private void LoadAssetDatabase()
        {
            if (assetDatabase == null)
            {
                assetDatabase = Resources.Load<GameAssetDatabase>("GameAssetDatabase");
            }
            
            if (assetDatabase == null)
            {
                Debug.LogWarning("[BuildingModelProvider] GameAssetDatabase not found! Using fallback primitives.");
            }
            else
            {
                Debug.Log($"[BuildingModelProvider] Loaded asset database with {assetDatabase.BuildingModels.Count} buildings, " +
                         $"{assetDatabase.TowerModels.Count} towers, {assetDatabase.WallModels.Count} walls");
            }
        }
        
        #region Public API - Get Models
        
        /// <summary>
        /// Get a building model by category
        /// </summary>
        public GameObject GetBuildingModel(BuildingCategory category, Transform parent = null)
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
        public GameObject GetTowerModel(TowerType type, Transform parent = null)
        {
            if (assetDatabase == null)
                return CreateFallbackTower(parent);
            
            var entry = assetDatabase.GetRandomTower(type);
            return InstantiateTowerModel(entry, parent);
        }
        
        /// <summary>
        /// Get a wall segment
        /// </summary>
        public GameObject GetWallModel(WallType type, WallMaterial material = WallMaterial.Stone, Transform parent = null)
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
            
            // Top
            var top = GameObject.CreatePrimitive(PrimitiveType.Cone);
            if (top == null) top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
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
        public List<BuildingCategory> GetAvailableCategories()
        {
            var categories = new List<BuildingCategory>();
            if (assetDatabase == null) return categories;
            
            foreach (BuildingCategory cat in Enum.GetValues(typeof(BuildingCategory)))
            {
                bool hasAny = false;
                foreach (var building in assetDatabase.BuildingModels)
                {
                    if (building.Category == cat)
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
