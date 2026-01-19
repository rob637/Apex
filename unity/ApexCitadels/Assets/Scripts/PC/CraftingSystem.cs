using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApexCitadels.Core;

#if FIREBASE_ENABLED
using Firebase.Firestore;
#endif

namespace ApexCitadels.PC
{
    /// <summary>
    /// PC-exclusive Crafting System
    /// Allows players to craft special items, equipment, and upgrades
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float craftingSpeedMultiplier = 1f;
        [SerializeField] private int maxQueueSize = 5;
        [SerializeField] private int maxConcurrentCrafts = 2;
        
        // Singleton
        private static CraftingSystem _instance;
        public static CraftingSystem Instance => _instance;
        
        // Crafting state
        private Dictionary<string, CraftingRecipe> _recipes = new Dictionary<string, CraftingRecipe>();
        private List<CraftingJob> _craftingQueue = new List<CraftingJob>();
        private Dictionary<string, int> _inventory = new Dictionary<string, int>();
        
        // Events
        public event Action<CraftingJob> OnCraftingStarted;
        public event Action<CraftingJob> OnCraftingProgress;
        public event Action<CraftingJob, CraftingResult> OnCraftingCompleted;
        public event Action<CraftingJob> OnCraftingCancelled;
        public event Action OnRecipesLoaded;
        public event Action OnInventoryChanged;
        
        // Properties
        public IReadOnlyList<CraftingJob> Queue => _craftingQueue;
        public IReadOnlyDictionary<string, CraftingRecipe> Recipes => _recipes;
        public IReadOnlyDictionary<string, int> Inventory => _inventory;
        public int QueueCount => _craftingQueue.Count;
        public bool CanAddToQueue => _craftingQueue.Count < maxQueueSize;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        private async void Start()
        {
            await LoadRecipes();
            await LoadCraftingState();
        }
        
        private void Update()
        {
            UpdateCraftingProgress();
        }
        
        #region Recipe Management
        
        /// <summary>
        /// Load all crafting recipes from game data
        /// </summary>
        private async Task LoadRecipes()
        {
            _recipes.Clear();
            
            // Load built-in recipes
            LoadDefaultRecipes();
            
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                var snapshot = await db.Collection("crafting_recipes").GetSnapshotAsync();
                
                foreach (var doc in snapshot.Documents)
                {
                    var recipe = CraftingRecipe.FromFirestore(doc);
                    if (recipe != null)
                    {
                        _recipes[recipe.Id] = recipe;
                    }
                }
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[Crafting] Error loading recipes: {ex.Message}", ApexLogger.LogCategory.Economy);
            }
#endif
            
            OnRecipesLoaded?.Invoke();
            ApexLogger.Log($"[Crafting] Loaded {_recipes.Count} recipes", ApexLogger.LogCategory.Economy);
        }
        
        private void LoadDefaultRecipes()
        {
            // Basic Materials
            AddRecipe(new CraftingRecipe
            {
                Id = "refined_stone",
                Name = "Refined Stone",
                Category = CraftingCategory.Materials,
                Description = "High-quality building material",
                CraftTime = 60f,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = "stone", Amount = 10 }
                },
                OutputItem = "refined_stone",
                OutputAmount = 1,
                RequiredLevel = 1
            });
            
            AddRecipe(new CraftingRecipe
            {
                Id = "steel_ingot",
                Name = "Steel Ingot",
                Category = CraftingCategory.Materials,
                Description = "Forged steel for advanced construction",
                CraftTime = 120f,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = "iron", Amount = 5 },
                    new CraftingIngredient { ItemId = "coal", Amount = 3 }
                },
                OutputItem = "steel_ingot",
                OutputAmount = 1,
                RequiredLevel = 3
            });
            
            // Defense Items
            AddRecipe(new CraftingRecipe
            {
                Id = "wall_reinforcement",
                Name = "Wall Reinforcement Kit",
                Category = CraftingCategory.Defense,
                Description = "Strengthens walls by 25%",
                CraftTime = 300f,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = "steel_ingot", Amount = 3 },
                    new CraftingIngredient { ItemId = "refined_stone", Amount = 5 }
                },
                OutputItem = "wall_reinforcement",
                OutputAmount = 1,
                RequiredLevel = 5
            });
            
            AddRecipe(new CraftingRecipe
            {
                Id = "arrow_bundle",
                Name = "Enchanted Arrow Bundle",
                Category = CraftingCategory.Defense,
                Description = "High-damage arrows for towers",
                CraftTime = 180f,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = "wood", Amount = 20 },
                    new CraftingIngredient { ItemId = "steel_ingot", Amount = 2 },
                    new CraftingIngredient { ItemId = "crystal", Amount = 1 }
                },
                OutputItem = "enchanted_arrows",
                OutputAmount = 50,
                RequiredLevel = 4
            });
            
            // Offense Items
            AddRecipe(new CraftingRecipe
            {
                Id = "siege_bomb",
                Name = "Siege Bomb",
                Category = CraftingCategory.Offense,
                Description = "Devastating explosive for attacks",
                CraftTime = 600f,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = "sulfur", Amount = 10 },
                    new CraftingIngredient { ItemId = "steel_ingot", Amount = 5 },
                    new CraftingIngredient { ItemId = "crystal", Amount = 3 }
                },
                OutputItem = "siege_bomb",
                OutputAmount = 1,
                RequiredLevel = 8
            });
            
            AddRecipe(new CraftingRecipe
            {
                Id = "war_banner",
                Name = "War Banner",
                Category = CraftingCategory.Offense,
                Description = "Boosts attack units by 15%",
                CraftTime = 450f,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = "cloth", Amount = 10 },
                    new CraftingIngredient { ItemId = "gold", Amount = 5 },
                    new CraftingIngredient { ItemId = "dye", Amount = 3 }
                },
                OutputItem = "war_banner",
                OutputAmount = 1,
                RequiredLevel = 6
            });
            
            // Special Items
            AddRecipe(new CraftingRecipe
            {
                Id = "blueprint_scanner",
                Name = "Blueprint Scanner",
                Category = CraftingCategory.Special,
                Description = "Reveals enemy base layouts",
                CraftTime = 900f,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = "crystal", Amount = 10 },
                    new CraftingIngredient { ItemId = "electronics", Amount = 5 },
                    new CraftingIngredient { ItemId = "gold", Amount = 20 }
                },
                OutputItem = "blueprint_scanner",
                OutputAmount = 1,
                RequiredLevel = 10,
                IsPCExclusive = true
            });
            
            AddRecipe(new CraftingRecipe
            {
                Id = "territory_shield",
                Name = "Territory Shield",
                Category = CraftingCategory.Special,
                Description = "8-hour attack immunity",
                CraftTime = 1800f,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = "crystal", Amount = 20 },
                    new CraftingIngredient { ItemId = "refined_stone", Amount = 10 },
                    new CraftingIngredient { ItemId = "arcane_dust", Amount = 5 }
                },
                OutputItem = "territory_shield",
                OutputAmount = 1,
                RequiredLevel = 12
            });
            
            // Consumables
            AddRecipe(new CraftingRecipe
            {
                Id = "speed_potion",
                Name = "Speed Potion",
                Category = CraftingCategory.Consumables,
                Description = "Reduces build time by 50% for 1 hour",
                CraftTime = 120f,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = "herbs", Amount = 5 },
                    new CraftingIngredient { ItemId = "water", Amount = 3 },
                    new CraftingIngredient { ItemId = "crystal", Amount = 1 }
                },
                OutputItem = "speed_potion",
                OutputAmount = 1,
                RequiredLevel = 2
            });
            
            AddRecipe(new CraftingRecipe
            {
                Id = "resource_boost",
                Name = "Resource Boost Elixir",
                Category = CraftingCategory.Consumables,
                Description = "Double resource production for 4 hours",
                CraftTime = 240f,
                Ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { ItemId = "herbs", Amount = 10 },
                    new CraftingIngredient { ItemId = "gold", Amount = 3 },
                    new CraftingIngredient { ItemId = "arcane_dust", Amount = 2 }
                },
                OutputItem = "resource_boost",
                OutputAmount = 1,
                RequiredLevel = 4
            });
        }
        
        private void AddRecipe(CraftingRecipe recipe)
        {
            _recipes[recipe.Id] = recipe;
        }
        
        /// <summary>
        /// Get recipes filtered by category
        /// </summary>
        public List<CraftingRecipe> GetRecipesByCategory(CraftingCategory category)
        {
            var result = new List<CraftingRecipe>();
            foreach (var recipe in _recipes.Values)
            {
                if (recipe.Category == category)
                {
                    result.Add(recipe);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Get all available recipes for player's level
        /// </summary>
        public List<CraftingRecipe> GetAvailableRecipes(int playerLevel)
        {
            var result = new List<CraftingRecipe>();
            foreach (var recipe in _recipes.Values)
            {
                if (recipe.RequiredLevel <= playerLevel)
                {
                    result.Add(recipe);
                }
            }
            return result;
        }
        
        #endregion
        
        #region Crafting Operations
        
        /// <summary>
        /// Check if a recipe can be crafted
        /// </summary>
        public CraftingValidation CanCraft(string recipeId)
        {
            if (!_recipes.TryGetValue(recipeId, out var recipe))
            {
                return new CraftingValidation { IsValid = false, Message = "Recipe not found" };
            }
            
            // Check queue space
            if (_craftingQueue.Count >= maxQueueSize)
            {
                return new CraftingValidation { IsValid = false, Message = "Crafting queue is full" };
            }
            
            // Check ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                int available = GetItemCount(ingredient.ItemId);
                if (available < ingredient.Amount)
                {
                    return new CraftingValidation 
                    { 
                        IsValid = false, 
                        Message = $"Need {ingredient.Amount - available} more {ingredient.ItemId}",
                        MissingIngredient = ingredient.ItemId,
                        MissingAmount = ingredient.Amount - available
                    };
                }
            }
            
            // Check PC exclusive
            if (recipe.IsPCExclusive && !PlatformManager.IsPC)
            {
                return new CraftingValidation { IsValid = false, Message = "PC exclusive recipe" };
            }
            
            return new CraftingValidation { IsValid = true };
        }
        
        /// <summary>
        /// Start crafting a recipe
        /// </summary>
        public async Task<CraftingJob> StartCrafting(string recipeId)
        {
            var validation = CanCraft(recipeId);
            if (!validation.IsValid)
            {
                ApexLogger.LogWarning($"[Crafting] Cannot craft {recipeId}: {validation.Message}", ApexLogger.LogCategory.Economy);
                return null;
            }
            
            var recipe = _recipes[recipeId];
            
            // Consume ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                ConsumeItem(ingredient.ItemId, ingredient.Amount);
            }
            
            // Create crafting job
            var job = new CraftingJob
            {
                Id = Guid.NewGuid().ToString(),
                RecipeId = recipeId,
                StartTime = DateTime.UtcNow,
                TotalTime = recipe.CraftTime / craftingSpeedMultiplier,
                Progress = 0f,
                Status = CraftingStatus.Queued
            };
            
            _craftingQueue.Add(job);
            
            // Save state
            await SaveCraftingState();
            
            OnCraftingStarted?.Invoke(job);
            ApexLogger.Log($"[Crafting] Started crafting {recipe.Name}", ApexLogger.LogCategory.Economy);
            
            return job;
        }
        
        /// <summary>
        /// Cancel a crafting job
        /// </summary>
        public async Task<bool> CancelCrafting(string jobId)
        {
            var job = _craftingQueue.Find(j => j.Id == jobId);
            if (job == null) return false;
            
            // Refund partial ingredients based on progress
            if (_recipes.TryGetValue(job.RecipeId, out var recipe))
            {
                float refundRate = 1f - job.Progress;
                foreach (var ingredient in recipe.Ingredients)
                {
                    int refundAmount = Mathf.FloorToInt(ingredient.Amount * refundRate);
                    if (refundAmount > 0)
                    {
                        AddItem(ingredient.ItemId, refundAmount);
                    }
                }
            }
            
            _craftingQueue.Remove(job);
            job.Status = CraftingStatus.Cancelled;
            
            await SaveCraftingState();
            
            OnCraftingCancelled?.Invoke(job);
            ApexLogger.Log($"[Crafting] Cancelled crafting job {jobId}", ApexLogger.LogCategory.Economy);
            
            return true;
        }
        
        /// <summary>
        /// Speed up crafting with premium currency
        /// </summary>
        public async Task<bool> SpeedUpCrafting(string jobId, int crystalCost)
        {
            var job = _craftingQueue.Find(j => j.Id == jobId);
            if (job == null || job.Status != CraftingStatus.InProgress) return false;
            
            // TODO: Check and deduct crystal cost
            
            // Complete immediately
            job.Progress = 1f;
            await CompleteCrafting(job);
            
            return true;
        }
        
        private void UpdateCraftingProgress()
        {
            int activeCrafts = 0;
            
            for (int i = _craftingQueue.Count - 1; i >= 0; i--)
            {
                var job = _craftingQueue[i];
                
                if (job.Status == CraftingStatus.Completed || job.Status == CraftingStatus.Cancelled)
                {
                    continue;
                }
                
                // Only process up to max concurrent crafts
                if (job.Status == CraftingStatus.Queued && activeCrafts < maxConcurrentCrafts)
                {
                    job.Status = CraftingStatus.InProgress;
                    job.StartTime = DateTime.UtcNow;
                }
                
                if (job.Status == CraftingStatus.InProgress)
                {
                    activeCrafts++;
                    
                    float elapsed = (float)(DateTime.UtcNow - job.StartTime).TotalSeconds;
                    job.Progress = Mathf.Clamp01(elapsed / job.TotalTime);
                    
                    OnCraftingProgress?.Invoke(job);
                    
                    if (job.Progress >= 1f)
                    {
                        _ = CompleteCrafting(job);
                    }
                }
            }
        }
        
        private async Task CompleteCrafting(CraftingJob job)
        {
            if (!_recipes.TryGetValue(job.RecipeId, out var recipe))
            {
                return;
            }
            
            job.Status = CraftingStatus.Completed;
            
            // Create output item
            AddItem(recipe.OutputItem, recipe.OutputAmount);
            
            // Calculate quality (PC exclusive feature)
            var result = new CraftingResult
            {
                Success = true,
                ItemId = recipe.OutputItem,
                Amount = recipe.OutputAmount,
                Quality = CalculateCraftingQuality(recipe)
            };
            
            // Bonus items chance
            if (UnityEngine.Random.value < 0.1f) // 10% chance
            {
                result.BonusItemId = GetRandomBonusItem(recipe.Category);
                result.BonusAmount = 1;
                AddItem(result.BonusItemId, result.BonusAmount);
            }
            
            _craftingQueue.Remove(job);
            
            await SaveCraftingState();
            
            OnCraftingCompleted?.Invoke(job, result);
            ApexLogger.Log($"[Crafting] Completed {recipe.Name} x{result.Amount} (Quality: {result.Quality})", ApexLogger.LogCategory.Economy);
        }
        
        private CraftingQuality CalculateCraftingQuality(CraftingRecipe recipe)
        {
            // Quality system is PC-exclusive
            if (!PlatformManager.IsPC) return CraftingQuality.Normal;
            
            float roll = UnityEngine.Random.value;
            
            if (roll < 0.01f) return CraftingQuality.Legendary;      // 1%
            if (roll < 0.05f) return CraftingQuality.Epic;           // 4%
            if (roll < 0.15f) return CraftingQuality.Rare;           // 10%
            if (roll < 0.40f) return CraftingQuality.Uncommon;       // 25%
            return CraftingQuality.Normal;                            // 60%
        }
        
        private string GetRandomBonusItem(CraftingCategory category)
        {
            // Return a random bonus item based on category
            switch (category)
            {
                case CraftingCategory.Materials:
                    return "scrap_metal";
                case CraftingCategory.Defense:
                    return "repair_kit";
                case CraftingCategory.Offense:
                    return "battle_ration";
                case CraftingCategory.Special:
                    return "arcane_dust";
                case CraftingCategory.Consumables:
                    return "herbs";
                default:
                    return "gold";
            }
        }
        
        #endregion
        
        #region Inventory Management
        
        public int GetItemCount(string itemId)
        {
            return _inventory.TryGetValue(itemId, out int count) ? count : 0;
        }
        
        public void AddItem(string itemId, int amount)
        {
            if (!_inventory.ContainsKey(itemId))
            {
                _inventory[itemId] = 0;
            }
            _inventory[itemId] += amount;
            OnInventoryChanged?.Invoke();
        }
        
        public bool ConsumeItem(string itemId, int amount)
        {
            if (GetItemCount(itemId) < amount) return false;
            _inventory[itemId] -= amount;
            if (_inventory[itemId] <= 0)
            {
                _inventory.Remove(itemId);
            }
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        #endregion
        
        #region Persistence
        
        private async Task LoadCraftingState()
        {
#if FIREBASE_ENABLED
            string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId)) return;
            
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                var doc = await db.Collection("users").Document(userId)
                    .Collection("crafting").Document("state").GetSnapshotAsync();
                
                if (doc.Exists)
                {
                    // Load inventory
                    if (doc.ContainsField("inventory"))
                    {
                        var inv = doc.GetValue<Dictionary<string, object>>("inventory");
                        foreach (var kvp in inv)
                        {
                            _inventory[kvp.Key] = Convert.ToInt32(kvp.Value);
                        }
                    }
                    
                    // Load queue
                    if (doc.ContainsField("queue"))
                    {
                        var queue = doc.GetValue<List<Dictionary<string, object>>>("queue");
                        foreach (var jobData in queue)
                        {
                            var job = CraftingJob.FromDictionary(jobData);
                            if (job != null && job.Status != CraftingStatus.Completed)
                            {
                                _craftingQueue.Add(job);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[Crafting] Error loading state: {ex.Message}", ApexLogger.LogCategory.Economy);
            }
#endif
        }
        
        private async Task SaveCraftingState()
        {
#if FIREBASE_ENABLED
            string userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId)) return;
            
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                
                // Prepare queue data
                var queueData = new List<Dictionary<string, object>>();
                foreach (var job in _craftingQueue)
                {
                    queueData.Add(job.ToDictionary());
                }
                
                await db.Collection("users").Document(userId)
                    .Collection("crafting").Document("state").SetAsync(new Dictionary<string, object>
                    {
                        { "inventory", _inventory },
                        { "queue", queueData },
                        { "updatedAt", FieldValue.ServerTimestamp }
                    });
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[Crafting] Error saving state: {ex.Message}", ApexLogger.LogCategory.Economy);
            }
#endif
        }
        
        #endregion
    }
    
    #region Data Classes
    
    [Serializable]
    public class CraftingRecipe
    {
        public string Id;
        public string Name;
        public string Description;
        public CraftingCategory Category;
        public float CraftTime; // seconds
        public List<CraftingIngredient> Ingredients = new List<CraftingIngredient>();
        public string OutputItem;
        public int OutputAmount = 1;
        public int RequiredLevel = 1;
        public bool IsPCExclusive;
        public string IconPath;
        
#if FIREBASE_ENABLED
        public static CraftingRecipe FromFirestore(DocumentSnapshot doc)
        {
            try
            {
                var recipe = new CraftingRecipe
                {
                    Id = doc.Id,
                    Name = doc.GetValue<string>("name"),
                    Description = doc.GetValue<string>("description"),
                    CraftTime = doc.GetValue<float>("craftTime"),
                    OutputItem = doc.GetValue<string>("outputItem"),
                    OutputAmount = doc.GetValue<int>("outputAmount"),
                    RequiredLevel = doc.GetValue<int>("requiredLevel"),
                    IsPCExclusive = doc.ContainsField("isPCExclusive") && doc.GetValue<bool>("isPCExclusive")
                };
                
                if (Enum.TryParse<CraftingCategory>(doc.GetValue<string>("category"), out var cat))
                {
                    recipe.Category = cat;
                }
                
                if (doc.ContainsField("ingredients"))
                {
                    var ingredients = doc.GetValue<List<Dictionary<string, object>>>("ingredients");
                    foreach (var ing in ingredients)
                    {
                        recipe.Ingredients.Add(new CraftingIngredient
                        {
                            ItemId = ing["itemId"]?.ToString() ?? "",
                            Amount = Convert.ToInt32(ing["amount"])
                        });
                    }
                }
                
                return recipe;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[CraftingRecipe] Parse error: {ex.Message}", ApexLogger.LogCategory.Economy);
                return null;
            }
        }
#endif
    }
    
    [Serializable]
    public class CraftingIngredient
    {
        public string ItemId;
        public int Amount;
    }
    
    [Serializable]
    public class CraftingJob
    {
        public string Id;
        public string RecipeId;
        public DateTime StartTime;
        public float TotalTime;
        public float Progress;
        public CraftingStatus Status;
        
        public float RemainingTime => TotalTime * (1f - Progress);
        
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "id", Id },
                { "recipeId", RecipeId },
                { "startTime", StartTime.ToString("O") },
                { "totalTime", TotalTime },
                { "progress", Progress },
                { "status", Status.ToString() }
            };
        }
        
        public static CraftingJob FromDictionary(Dictionary<string, object> dict)
        {
            var job = new CraftingJob
            {
                Id = dict["id"]?.ToString() ?? "",
                RecipeId = dict["recipeId"]?.ToString() ?? "",
                TotalTime = Convert.ToSingle(dict["totalTime"]),
                Progress = Convert.ToSingle(dict["progress"])
            };
            
            if (DateTime.TryParse(dict["startTime"]?.ToString(), out var startTime))
            {
                job.StartTime = startTime;
            }
            
            if (Enum.TryParse<CraftingStatus>(dict["status"]?.ToString(), out var status))
            {
                job.Status = status;
            }
            
            return job;
        }
    }
    
    [Serializable]
    public class CraftingResult
    {
        public bool Success;
        public string ItemId;
        public int Amount;
        public CraftingQuality Quality;
        public string BonusItemId;
        public int BonusAmount;
    }
    
    public class CraftingValidation
    {
        public bool IsValid;
        public string Message;
        public string MissingIngredient;
        public int MissingAmount;
    }
    
    public enum CraftingCategory
    {
        Materials,
        Defense,
        Offense,
        Special,
        Consumables
    }
    
    public enum CraftingStatus
    {
        Queued,
        InProgress,
        Completed,
        Cancelled
    }
    
    public enum CraftingQuality
    {
        Normal,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    #endregion
}
