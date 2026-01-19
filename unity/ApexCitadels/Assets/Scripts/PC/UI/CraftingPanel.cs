using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// PC-exclusive Crafting Panel UI
    /// Displays recipes, crafting queue, and inventory
    /// </summary>
    public class CraftingPanel : MonoBehaviour
    {
        [Header("Tab Buttons")]
        [SerializeField] private Button materialsTabButton;
        [SerializeField] private Button defenseTabButton;
        [SerializeField] private Button offenseTabButton;
        [SerializeField] private Button specialTabButton;
        [SerializeField] private Button consumablesTabButton;
        [SerializeField] private Button inventoryTabButton;
        
        [Header("Recipe List")]
        [SerializeField] private Transform recipeListContent;
        [SerializeField] private GameObject recipeItemPrefab;
        [SerializeField] private TMP_InputField searchInput;
        
        [Header("Recipe Detail")]
        [SerializeField] private GameObject recipeDetailPanel;
        [SerializeField] private Image recipeIcon;
        [SerializeField] private TMP_Text recipeNameText;
        [SerializeField] private TMP_Text recipeDescriptionText;
        [SerializeField] private TMP_Text craftTimeText;
        [SerializeField] private Transform ingredientListContent;
        [SerializeField] private GameObject ingredientItemPrefab;
        [SerializeField] private TMP_Text outputText;
        [SerializeField] private Button craftButton;
        [SerializeField] private TMP_Text craftButtonText;
        
        [Header("Crafting Queue")]
        [SerializeField] private Transform queueContent;
        [SerializeField] private GameObject queueItemPrefab;
        [SerializeField] private TMP_Text queueCountText;
        [SerializeField] private int maxQueueDisplay = 5;
        
        [Header("Inventory Grid")]
        [SerializeField] private Transform inventoryContent;
        [SerializeField] private GameObject inventoryItemPrefab;
        [SerializeField] private TMP_Text totalItemsText;
        
        [Header("Colors")]
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color unavailableColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color selectedTabColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color normalTabColor = Color.white;
        
        [Header("Quality Colors")]
        [SerializeField] private Color normalQualityColor = Color.white;
        [SerializeField] private Color uncommonQualityColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color rareQualityColor = new Color(0.2f, 0.4f, 1f);
        [SerializeField] private Color epicQualityColor = new Color(0.6f, 0.2f, 0.8f);
        [SerializeField] private Color legendaryQualityColor = new Color(1f, 0.6f, 0f);
        
        // State
        private CraftingSystem _craftingSystem;
        private CraftingCategory _currentCategory = CraftingCategory.Materials;
        private CraftingRecipe _selectedRecipe;
        private List<GameObject> _recipeItems = new List<GameObject>();
        private List<GameObject> _ingredientItems = new List<GameObject>();
        private List<GameObject> _queueItems = new List<GameObject>();
        private List<GameObject> _inventoryItems = new List<GameObject>();
        private Button[] _tabButtons;
        
        private void Awake()
        {
            _craftingSystem = CraftingSystem.Instance ?? FindFirstObjectByType<CraftingSystem>();
            _tabButtons = new Button[]
            {
                materialsTabButton, defenseTabButton, offenseTabButton,
                specialTabButton, consumablesTabButton, inventoryTabButton
            };
            
            SetupEventHandlers();
        }
        
        private void SetupEventHandlers()
        {
            // Tab buttons
            materialsTabButton?.onClick.AddListener(() => SelectCategory(CraftingCategory.Materials));
            defenseTabButton?.onClick.AddListener(() => SelectCategory(CraftingCategory.Defense));
            offenseTabButton?.onClick.AddListener(() => SelectCategory(CraftingCategory.Offense));
            specialTabButton?.onClick.AddListener(() => SelectCategory(CraftingCategory.Special));
            consumablesTabButton?.onClick.AddListener(() => SelectCategory(CraftingCategory.Consumables));
            inventoryTabButton?.onClick.AddListener(ShowInventory);
            
            // Craft button
            craftButton?.onClick.AddListener(OnCraftClicked);
            
            // Search
            searchInput?.onValueChanged.AddListener(OnSearchChanged);
            
            // Crafting system events
            if (_craftingSystem != null)
            {
                _craftingSystem.OnCraftingStarted += OnCraftingStarted;
                _craftingSystem.OnCraftingProgress += OnCraftingProgress;
                _craftingSystem.OnCraftingCompleted += OnCraftingCompleted;
                _craftingSystem.OnCraftingCancelled += OnCraftingCancelled;
                _craftingSystem.OnInventoryChanged += OnInventoryChanged;
                _craftingSystem.OnRecipesLoaded += OnRecipesLoaded;
            }
        }
        
        private void OnDestroy()
        {
            if (_craftingSystem != null)
            {
                _craftingSystem.OnCraftingStarted -= OnCraftingStarted;
                _craftingSystem.OnCraftingProgress -= OnCraftingProgress;
                _craftingSystem.OnCraftingCompleted -= OnCraftingCompleted;
                _craftingSystem.OnCraftingCancelled -= OnCraftingCancelled;
                _craftingSystem.OnInventoryChanged -= OnInventoryChanged;
                _craftingSystem.OnRecipesLoaded -= OnRecipesLoaded;
            }
        }
        
        #region Panel Control
        
        public void Show()
        {
            gameObject.SetActive(true);
            SelectCategory(CraftingCategory.Materials);
            RefreshQueue();
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        #endregion
        
        #region Category Selection
        
        private void SelectCategory(CraftingCategory category)
        {
            _currentCategory = category;
            
            // Update tab visuals
            UpdateTabVisuals((int)category);
            
            // Show recipe list
            RefreshRecipeList();
            
            // Hide detail panel until selection
            recipeDetailPanel?.SetActive(false);
        }
        
        private void ShowInventory()
        {
            // Update tab visuals
            UpdateTabVisuals(5); // Inventory is index 5
            
            RefreshInventoryDisplay();
        }
        
        private void UpdateTabVisuals(int selectedIndex)
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null) continue;
                
                var colors = _tabButtons[i].colors;
                colors.normalColor = (i == selectedIndex) ? selectedTabColor : normalTabColor;
                _tabButtons[i].colors = colors;
            }
        }
        
        #endregion
        
        #region Recipe List
        
        private void RefreshRecipeList()
        {
            // Clear existing items
            foreach (var item in _recipeItems)
            {
                if (item != null) Destroy(item);
            }
            _recipeItems.Clear();
            
            if (_craftingSystem == null) return;
            
            // Get recipes for category
            var recipes = _craftingSystem.GetRecipesByCategory(_currentCategory);
            
            // Apply search filter
            string search = searchInput?.text?.ToLower() ?? "";
            if (!string.IsNullOrEmpty(search))
            {
                recipes = recipes.FindAll(r => 
                    r.Name.ToLower().Contains(search) ||
                    r.Description.ToLower().Contains(search));
            }
            
            // Create recipe items
            foreach (var recipe in recipes)
            {
                CreateRecipeItem(recipe);
            }
        }
        
        private void CreateRecipeItem(CraftingRecipe recipe)
        {
            if (recipeItemPrefab == null || recipeListContent == null) return;
            
            var item = Instantiate(recipeItemPrefab, recipeListContent);
            _recipeItems.Add(item);
            
            // Get components
            var texts = item.GetComponentsInChildren<TMP_Text>();
            var images = item.GetComponentsInChildren<Image>();
            var button = item.GetComponent<Button>();
            
            // Configure display
            if (texts.Length >= 2)
            {
                texts[0].text = recipe.Name;
                texts[1].text = FormatTime(recipe.CraftTime);
            }
            
            // Check if craftable
            var validation = _craftingSystem.CanCraft(recipe.Id);
            
            // Update visual state
            var canvasGroup = item.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = validation.IsValid ? 1f : 0.5f;
            }
            else if (images.Length > 0)
            {
                images[0].color = validation.IsValid ? availableColor : unavailableColor;
            }
            
            // PC exclusive indicator
            if (recipe.IsPCExclusive && texts.Length >= 3)
            {
                texts[2].text = "PC";
                texts[2].gameObject.SetActive(true);
            }
            
            // Click handler
            if (button != null)
            {
                string recipeId = recipe.Id;
                button.onClick.AddListener(() => SelectRecipe(recipeId));
            }
        }
        
        private void SelectRecipe(string recipeId)
        {
            if (_craftingSystem == null || !_craftingSystem.Recipes.TryGetValue(recipeId, out var recipe))
            {
                return;
            }
            
            _selectedRecipe = recipe;
            ShowRecipeDetail(recipe);
        }
        
        private void OnSearchChanged(string text)
        {
            RefreshRecipeList();
        }
        
        #endregion
        
        #region Recipe Detail
        
        private void ShowRecipeDetail(CraftingRecipe recipe)
        {
            recipeDetailPanel?.SetActive(true);
            
            // Update basic info
            if (recipeNameText != null) recipeNameText.text = recipe.Name;
            if (recipeDescriptionText != null) recipeDescriptionText.text = recipe.Description;
            if (craftTimeText != null) craftTimeText.text = $"Craft Time: {FormatTime(recipe.CraftTime)}";
            if (outputText != null) outputText.text = $"Creates: {recipe.OutputItem} x{recipe.OutputAmount}";
            
            // Update ingredients
            RefreshIngredientList(recipe);
            
            // Update craft button
            UpdateCraftButton();
        }
        
        private void RefreshIngredientList(CraftingRecipe recipe)
        {
            // Clear existing items
            foreach (var item in _ingredientItems)
            {
                if (item != null) Destroy(item);
            }
            _ingredientItems.Clear();
            
            if (ingredientItemPrefab == null || ingredientListContent == null) return;
            
            // Create ingredient items
            foreach (var ingredient in recipe.Ingredients)
            {
                var item = Instantiate(ingredientItemPrefab, ingredientListContent);
                _ingredientItems.Add(item);
                
                var texts = item.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 2)
                {
                    int available = _craftingSystem.GetItemCount(ingredient.ItemId);
                    texts[0].text = ingredient.ItemId;
                    texts[1].text = $"{available}/{ingredient.Amount}";
                    texts[1].color = available >= ingredient.Amount ? Color.green : Color.red;
                }
            }
        }
        
        private void UpdateCraftButton()
        {
            if (_selectedRecipe == null || craftButton == null) return;
            
            var validation = _craftingSystem.CanCraft(_selectedRecipe.Id);
            
            craftButton.interactable = validation.IsValid;
            
            if (craftButtonText != null)
            {
                if (validation.IsValid)
                {
                    craftButtonText.text = "Craft";
                }
                else
                {
                    craftButtonText.text = validation.Message;
                }
            }
        }
        
        private async void OnCraftClicked()
        {
            if (_selectedRecipe == null || _craftingSystem == null) return;
            
            var job = await _craftingSystem.StartCrafting(_selectedRecipe.Id);
            
            if (job != null)
            {
                RefreshRecipeList();
                RefreshIngredientList(_selectedRecipe);
                UpdateCraftButton();
                RefreshQueue();
            }
        }
        
        #endregion
        
        #region Crafting Queue
        
        private void RefreshQueue()
        {
            // Clear existing items
            foreach (var item in _queueItems)
            {
                if (item != null) Destroy(item);
            }
            _queueItems.Clear();
            
            if (_craftingSystem == null || queueContent == null) return;
            
            var queue = _craftingSystem.Queue;
            
            // Update count text
            if (queueCountText != null)
            {
                queueCountText.text = $"Queue: {queue.Count}/{maxQueueDisplay}";
            }
            
            // Create queue items
            int displayCount = Mathf.Min(queue.Count, maxQueueDisplay);
            for (int i = 0; i < displayCount; i++)
            {
                CreateQueueItem(queue[i], i);
            }
        }
        
        private void CreateQueueItem(CraftingJob job, int index)
        {
            if (queueItemPrefab == null || queueContent == null) return;
            
            var item = Instantiate(queueItemPrefab, queueContent);
            _queueItems.Add(item);
            
            // Get recipe info
            _craftingSystem.Recipes.TryGetValue(job.RecipeId, out var recipe);
            
            // Configure display
            var texts = item.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = recipe?.Name ?? job.RecipeId;
                texts[1].text = GetJobStatusText(job);
            }
            
            // Progress bar
            var sliders = item.GetComponentsInChildren<Slider>();
            if (sliders.Length > 0)
            {
                sliders[0].value = job.Progress;
            }
            
            // Cancel button
            var buttons = item.GetComponentsInChildren<Button>();
            if (buttons.Length > 0)
            {
                string jobId = job.Id;
                buttons[0].onClick.AddListener(async () =>
                {
                    await _craftingSystem.CancelCrafting(jobId);
                });
            }
        }
        
        private string GetJobStatusText(CraftingJob job)
        {
            switch (job.Status)
            {
                case CraftingStatus.Queued:
                    return "Queued";
                case CraftingStatus.InProgress:
                    return $"{FormatTime(job.RemainingTime)} remaining";
                case CraftingStatus.Completed:
                    return "Complete!";
                case CraftingStatus.Cancelled:
                    return "Cancelled";
                default:
                    return "";
            }
        }
        
        private void UpdateQueueProgress()
        {
            if (_craftingSystem == null) return;
            
            var queue = _craftingSystem.Queue;
            
            for (int i = 0; i < _queueItems.Count && i < queue.Count; i++)
            {
                var item = _queueItems[i];
                var job = queue[i];
                
                if (item == null) continue;
                
                // Update progress bar
                var sliders = item.GetComponentsInChildren<Slider>();
                if (sliders.Length > 0)
                {
                    sliders[0].value = job.Progress;
                }
                
                // Update time text
                var texts = item.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 2)
                {
                    texts[1].text = GetJobStatusText(job);
                }
            }
        }
        
        private void Update()
        {
            if (gameObject.activeInHierarchy)
            {
                UpdateQueueProgress();
            }
        }
        
        #endregion
        
        #region Inventory Display
        
        private void RefreshInventoryDisplay()
        {
            // Clear existing items
            foreach (var item in _inventoryItems)
            {
                if (item != null) Destroy(item);
            }
            _inventoryItems.Clear();
            
            // Hide recipe list and detail, show inventory
            recipeListContent?.gameObject.SetActive(false);
            recipeDetailPanel?.SetActive(false);
            inventoryContent?.gameObject.SetActive(true);
            
            if (_craftingSystem == null) return;
            
            var inventory = _craftingSystem.Inventory;
            
            // Update total count
            int totalItems = 0;
            foreach (var count in inventory.Values)
            {
                totalItems += count;
            }
            
            if (totalItemsText != null)
            {
                totalItemsText.text = $"Items: {totalItems}";
            }
            
            // Create inventory items
            foreach (var kvp in inventory)
            {
                CreateInventoryItem(kvp.Key, kvp.Value);
            }
        }
        
        private void CreateInventoryItem(string itemId, int count)
        {
            if (inventoryItemPrefab == null || inventoryContent == null) return;
            
            var item = Instantiate(inventoryItemPrefab, inventoryContent);
            _inventoryItems.Add(item);
            
            var texts = item.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = FormatItemName(itemId);
                texts[1].text = $"x{count}";
            }
            
            // Set tooltip with full name
            // var tooltip = item.GetComponent<TooltipTrigger>();
            // if (tooltip != null) tooltip.text = itemId;
        }
        
        private string FormatItemName(string itemId)
        {
            // Convert snake_case to Title Case
            var words = itemId.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }
            return string.Join(" ", words);
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnRecipesLoaded()
        {
            RefreshRecipeList();
        }
        
        private void OnCraftingStarted(CraftingJob job)
        {
            RefreshQueue();
            RefreshRecipeList();
            UpdateCraftButton();
        }
        
        private void OnCraftingProgress(CraftingJob job)
        {
            // Progress updated in Update()
        }
        
        private void OnCraftingCompleted(CraftingJob job, CraftingResult result)
        {
            RefreshQueue();
            
            // Show completion notification
            ShowCraftingNotification(result);
        }
        
        private void OnCraftingCancelled(CraftingJob job)
        {
            RefreshQueue();
            RefreshRecipeList();
            
            if (_selectedRecipe != null)
            {
                RefreshIngredientList(_selectedRecipe);
                UpdateCraftButton();
            }
        }
        
        private void OnInventoryChanged()
        {
            if (_selectedRecipe != null)
            {
                RefreshIngredientList(_selectedRecipe);
                UpdateCraftButton();
            }
            RefreshRecipeList();
        }
        
        private void ShowCraftingNotification(CraftingResult result)
        {
            string qualityText = result.Quality != CraftingQuality.Normal 
                ? $" ({result.Quality})" 
                : "";
            
            string message = $"Crafted: {result.ItemId} x{result.Amount}{qualityText}";
            
            if (!string.IsNullOrEmpty(result.BonusItemId))
            {
                message += $"\nBonus: {result.BonusItemId} x{result.BonusAmount}!";
            }
            
            ApexLogger.Log($"[CraftingPanel] {message}", ApexLogger.LogCategory.UI);
            
            // TODO: Show actual UI notification
        }
        
        #endregion
        
        #region Utility
        
        private string FormatTime(float seconds)
        {
            if (seconds < 60)
            {
                return $"{seconds:N0}s";
            }
            else if (seconds < 3600)
            {
                int minutes = Mathf.FloorToInt(seconds / 60);
                int secs = Mathf.FloorToInt(seconds % 60);
                return $"{minutes}m {secs}s";
            }
            else
            {
                int hours = Mathf.FloorToInt(seconds / 3600);
                int minutes = Mathf.FloorToInt((seconds % 3600) / 60);
                return $"{hours}h {minutes}m";
            }
        }
        
        public Color GetQualityColor(CraftingQuality quality)
        {
            switch (quality)
            {
                case CraftingQuality.Uncommon: return uncommonQualityColor;
                case CraftingQuality.Rare: return rareQualityColor;
                case CraftingQuality.Epic: return epicQualityColor;
                case CraftingQuality.Legendary: return legendaryQualityColor;
                default: return normalQualityColor;
            }
        }
        
        #endregion
    }
}
