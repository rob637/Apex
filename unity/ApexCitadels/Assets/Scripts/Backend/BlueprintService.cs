using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Data;

namespace ApexCitadels.Backend
{
    /// <summary>
    /// Implementation of IBlueprintService using Firebase Cloud Functions.
    /// Handles saving, loading, and applying building blueprints.
    /// </summary>
    public class BlueprintService : MonoBehaviour, IBlueprintService
    {
        public static BlueprintService Instance { get; private set; }

        // Cache blueprints list
        private List<Blueprint> _cachedBlueprints;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private const float CACHE_DURATION_SECONDS = 120f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Task<Blueprint> SaveBlueprintAsync(string territoryId, string name, string description = null)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("saveBlueprint");
                var data = new Dictionary<string, object>
                {
                    { "territoryId", territoryId },
                    { "name", name }
                };
                if (!string.IsNullOrEmpty(description))
                {
                    data["description"] = description;
                }
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                var blueprint = JsonUtility.FromJson<Blueprint>(json);
                
                // Invalidate cache to refresh on next fetch
                _cacheExpiry = DateTime.MinValue;
                
                return blueprint;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlueprintService] SaveBlueprint failed: {ex.Message}");
                throw;
            }
#else
            Debug.Log($"[STUB] SaveBlueprint: {name} from territory {territoryId}");
            await Task.Delay(100);
            return new Blueprint
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = description ?? "",
                TerritoryId = territoryId,
                BuildingCount = 15,
                CreatedAt = DateTime.UtcNow,
                IsAutoSave = false
            };
#endif
        }

        public async Task<BlueprintListResponse> GetMyBlueprintsAsync()
        {
            // Return cached value if still valid
            if (_cachedBlueprints != null && DateTime.UtcNow < _cacheExpiry)
            {
                return new BlueprintListResponse
                {
                    Blueprints = _cachedBlueprints,
                    TotalCount = _cachedBlueprints.Count
                };
            }

#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("getMyBlueprints");
                var result = await callable.CallAsync(new Dictionary<string, object>());
                var json = result.Data.ToString();
                var response = JsonUtility.FromJson<BlueprintListResponse>(json);
                
                _cachedBlueprints = response.Blueprints;
                _cacheExpiry = DateTime.UtcNow.AddSeconds(CACHE_DURATION_SECONDS);
                
                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlueprintService] GetMyBlueprints failed: {ex.Message}");
                throw;
            }
#else
            Debug.Log("[STUB] GetMyBlueprints called");
            await Task.Delay(100);
            _cachedBlueprints = new List<Blueprint>
            {
                new Blueprint
                {
                    Id = "stub-blueprint-1",
                    Name = "My First Citadel",
                    Description = "Starter fortress design",
                    BuildingCount = 12,
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    IsAutoSave = false
                },
                new Blueprint
                {
                    Id = "stub-blueprint-auto",
                    Name = "[Auto] Central Park Citadel",
                    Description = "Auto-saved on territory loss",
                    BuildingCount = 25,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    IsAutoSave = true
                }
            };
            _cacheExpiry = DateTime.UtcNow.AddSeconds(CACHE_DURATION_SECONDS);
            return new BlueprintListResponse
            {
                Blueprints = _cachedBlueprints,
                TotalCount = _cachedBlueprints.Count
            };
#endif
        }

        public async Task<Blueprint> GetBlueprintDetailsAsync(string blueprintId)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("getBlueprintDetails");
                var data = new Dictionary<string, object> { { "blueprintId", blueprintId } };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                return JsonUtility.FromJson<Blueprint>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlueprintService] GetBlueprintDetails failed: {ex.Message}");
                throw;
            }
#else
            Debug.Log($"[STUB] GetBlueprintDetails: {blueprintId}");
            await Task.Delay(100);
            return new Blueprint
            {
                Id = blueprintId,
                Name = "Detailed Blueprint",
                BuildingCount = 20,
                Buildings = new List<Data.BuildingPlacement>
                {
                    new Data.BuildingPlacement { BlockType = "stone_wall", PositionX = 0, PositionY = 0, PositionZ = 0 },
                    new Data.BuildingPlacement { BlockType = "wooden_tower", PositionX = 5, PositionY = 0, PositionZ = 0 },
                    new Data.BuildingPlacement { BlockType = "iron_gate", PositionX = 10, PositionY = 0, PositionZ = 0 }
                }
            };
#endif
        }

        public async Task<bool> DeleteBlueprintAsync(string blueprintId)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("deleteBlueprint");
                var data = new Dictionary<string, object> { { "blueprintId", blueprintId } };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                var response = JsonUtility.FromJson<SuccessResponse>(json);
                
                if (response.success)
                {
                    _cacheExpiry = DateTime.MinValue;
                }
                return response.success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlueprintService] DeleteBlueprint failed: {ex.Message}");
                return false;
            }
#else
            Debug.Log($"[STUB] DeleteBlueprint: {blueprintId}");
            await Task.Delay(100);
            _cachedBlueprints?.RemoveAll(b => b.Id == blueprintId);
            return true;
#endif
        }

        public async Task<bool> RenameBlueprintAsync(string blueprintId, string name, string description = null)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("renameBlueprint");
                var data = new Dictionary<string, object>
                {
                    { "blueprintId", blueprintId },
                    { "name", name }
                };
                if (!string.IsNullOrEmpty(description))
                {
                    data["description"] = description;
                }
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                var response = JsonUtility.FromJson<SuccessResponse>(json);
                
                if (response.success)
                {
                    _cacheExpiry = DateTime.MinValue;
                }
                return response.success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlueprintService] RenameBlueprint failed: {ex.Message}");
                return false;
            }
#else
            Debug.Log($"[STUB] RenameBlueprint: {blueprintId} -> {name}");
            await Task.Delay(100);
            var bp = _cachedBlueprints?.Find(b => b.Id == blueprintId);
            if (bp != null)
            {
                bp.Name = name;
                if (!string.IsNullOrEmpty(description))
                {
                    bp.Description = description;
                }
            }
            return true;
#endif
        }

        public async Task<(bool success, int buildingsPlaced, ResourceCost spent)> ApplyBlueprintAsync(
            string territoryId, string blueprintId)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("applyBlueprint");
                var data = new Dictionary<string, object>
                {
                    { "territoryId", territoryId },
                    { "blueprintId", blueprintId }
                };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                var response = JsonUtility.FromJson<ApplyBlueprintResponse>(json);
                return (response.success, response.buildingsPlaced, response.spent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlueprintService] ApplyBlueprint failed: {ex.Message}");
                return (false, 0, new ResourceCost());
            }
#else
            Debug.Log($"[STUB] ApplyBlueprint: {blueprintId} to territory {territoryId}");
            await Task.Delay(500);
            return (true, 15, new ResourceCost
            {
                Stone = 100,
                Wood = 75,
                Iron = 25
            });
#endif
        }

        public async Task<BlueprintCostPreview> PreviewBlueprintCostAsync(string blueprintId)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("previewBlueprintCost");
                var data = new Dictionary<string, object> { { "blueprintId", blueprintId } };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                return JsonUtility.FromJson<BlueprintCostPreview>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlueprintService] PreviewBlueprintCost failed: {ex.Message}");
                throw;
            }
#else
            Debug.Log($"[STUB] PreviewBlueprintCost: {blueprintId}");
            await Task.Delay(100);
            return new BlueprintCostPreview
            {
                BlueprintId = blueprintId,
                FullCost = new ResourceCost { Stone = 200, Wood = 150, Iron = 50 },
                DiscountedCost = new ResourceCost { Stone = 100, Wood = 75, Iron = 25 },
                DiscountPercent = 50,
                CanAfford = true
            };
#endif
        }

        /// <summary>
        /// Force refresh blueprints list from server
        /// </summary>
        public void InvalidateCache()
        {
            _cacheExpiry = DateTime.MinValue;
        }

        #region Helper Classes

        [Serializable]
        private class SuccessResponse
        {
            public bool success;
        }

        [Serializable]
        private class ApplyBlueprintResponse
        {
            public bool success;
            public int buildingsPlaced;
            public ResourceCost spent;
        }

        #endregion
    }

    #region Response Types

    [Serializable]
    public class BlueprintListResponse
    {
        public List<Blueprint> Blueprints;
        public int TotalCount;
        public int ManualCount;
        public int AutoSaveCount;
    }

    [Serializable]
    public class BlueprintCostPreview
    {
        public string BlueprintId;
        public ResourceCost FullCost;
        public ResourceCost DiscountedCost;
        public int DiscountPercent;
        public bool CanAfford;
        public ResourceCost Missing;
    }

    [Serializable]
    public class BlueprintBuilding
    {
        public string BlockType;
        public int Count;
        public Vector3 RelativePosition;
        public Quaternion Rotation;
    }

    #endregion
}
