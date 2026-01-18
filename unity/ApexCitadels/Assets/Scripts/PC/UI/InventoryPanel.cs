using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Inventory Panel - Manage items, equipment, consumables, and materials.
    /// Full inventory management with sorting, filtering, and item interactions.
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color legendaryColor = new Color(0.9f, 0.6f, 0.2f);
        [SerializeField] private Color epicColor = new Color(0.6f, 0.3f, 0.9f);
        [SerializeField] private Color rareColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color commonColor = new Color(0.5f, 0.5f, 0.5f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _itemGrid;
        private GameObject _detailPanel;
        private ItemCategory _selectedCategory = ItemCategory.All;
        private InventoryItem _selectedItem;
        private SortMode _sortMode = SortMode.Rarity;
        private Dictionary<ItemCategory, GameObject> _categoryTabs = new Dictionary<ItemCategory, GameObject>();
        
        // Inventory data
        private List<InventoryItem> _items = new List<InventoryItem>();
        private int _maxSlots = 100;
        private int _usedSlots;
        
        public static InventoryPanel Instance { get; private set; }
        
        public event Action<InventoryItem> OnItemUsed;
        public event Action<InventoryItem> OnItemEquipped;
        public event Action<InventoryItem> OnItemSold;
        public event Action<InventoryItem, int> OnItemDropped;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeInventory();
        }

        private void Start()
        {
            CreatePanel();
            Hide();
        }

        private void InitializeInventory()
        {
            // Equipment
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_001",
                Name = "Dragon Scale Armor",
                Description = "Legendary armor forged from dragon scales. Provides exceptional protection.",
                Category = ItemCategory.Equipment,
                Rarity = ItemRarity.Legendary,
                Icon = "🛡️",
                Quantity = 1,
                IsEquipped = false,
                Stats = new ItemStats { Defense = 150, Health = 500 },
                SellPrice = 5000,
                Level = 40
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_002",
                Name = "Flamebrand Sword",
                Description = "A sword that burns with eternal flame. Deals fire damage to enemies.",
                Category = ItemCategory.Equipment,
                Rarity = ItemRarity.Epic,
                Icon = "🗡️",
                Quantity = 1,
                IsEquipped = true,
                Stats = new ItemStats { Attack = 120, FireDamage = 30 },
                SellPrice = 3000,
                Level = 35
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_003",
                Name = "Shadow Cloak",
                Description = "A cloak woven from shadows. Increases stealth and speed.",
                Category = ItemCategory.Equipment,
                Rarity = ItemRarity.Rare,
                Icon = "🧥",
                Quantity = 1,
                Stats = new ItemStats { Speed = 25, Stealth = 40 },
                SellPrice = 1500,
                Level = 25
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_004",
                Name = "Iron Helmet",
                Description = "A sturdy iron helmet. Basic but reliable.",
                Category = ItemCategory.Equipment,
                Rarity = ItemRarity.Common,
                Icon = "⛑️",
                Quantity = 1,
                Stats = new ItemStats { Defense = 30 },
                SellPrice = 200,
                Level = 10
            });
            
            // Consumables
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_010",
                Name = "Health Potion",
                Description = "Restores 500 HP instantly.",
                Category = ItemCategory.Consumable,
                Rarity = ItemRarity.Common,
                Icon = "🧪",
                Quantity = 25,
                MaxStack = 99,
                IsConsumable = true,
                Effect = "Restore 500 HP",
                SellPrice = 50
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_011",
                Name = "Mana Elixir",
                Description = "Restores 300 MP instantly.",
                Category = ItemCategory.Consumable,
                Rarity = ItemRarity.Common,
                Icon = "🔮",
                Quantity = 15,
                MaxStack = 99,
                IsConsumable = true,
                Effect = "Restore 300 MP",
                SellPrice = 75
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_012",
                Name = "Attack Boost",
                Description = "+50% attack for 5 minutes.",
                Category = ItemCategory.Consumable,
                Rarity = ItemRarity.Rare,
                Icon = "⚔️",
                Quantity = 5,
                MaxStack = 20,
                IsConsumable = true,
                Effect = "+50% ATK for 5min",
                SellPrice = 300
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_013",
                Name = "Experience Scroll",
                Description = "Grants 5000 XP when used.",
                Category = ItemCategory.Consumable,
                Rarity = ItemRarity.Epic,
                Icon = "📜",
                Quantity = 3,
                MaxStack = 10,
                IsConsumable = true,
                Effect = "Grant 5000 XP",
                SellPrice = 1000
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_014",
                Name = "Teleport Stone",
                Description = "Instantly teleport to your citadel.",
                Category = ItemCategory.Consumable,
                Rarity = ItemRarity.Rare,
                Icon = "💎",
                Quantity = 8,
                MaxStack = 50,
                IsConsumable = true,
                Effect = "Teleport to Citadel",
                SellPrice = 200
            });
            
            // Materials
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_020",
                Name = "Iron Ore",
                Description = "Raw iron ore. Used in crafting.",
                Category = ItemCategory.Material,
                Rarity = ItemRarity.Common,
                Icon = "🪨",
                Quantity = 150,
                MaxStack = 999,
                SellPrice = 5
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_021",
                Name = "Crystal Shard",
                Description = "A shard of magical crystal. Required for advanced crafting.",
                Category = ItemCategory.Material,
                Rarity = ItemRarity.Rare,
                Icon = "💠",
                Quantity = 45,
                MaxStack = 500,
                SellPrice = 50
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_022",
                Name = "Dragon Scale",
                Description = "A scale from a dragon. Extremely rare crafting material.",
                Category = ItemCategory.Material,
                Rarity = ItemRarity.Legendary,
                Icon = "🐲",
                Quantity = 3,
                MaxStack = 100,
                SellPrice = 1000
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_023",
                Name = "Enchanted Wood",
                Description = "Magically infused wood. Used in staff crafting.",
                Category = ItemCategory.Material,
                Rarity = ItemRarity.Epic,
                Icon = "🪵",
                Quantity = 20,
                MaxStack = 200,
                SellPrice = 100
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_024",
                Name = "Monster Essence",
                Description = "Essence extracted from monsters. Used for enchantments.",
                Category = ItemCategory.Material,
                Rarity = ItemRarity.Rare,
                Icon = "✨",
                Quantity = 67,
                MaxStack = 500,
                SellPrice = 25
            });
            
            // Treasures/Collectibles
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_030",
                Name = "Ancient Coin",
                Description = "A coin from an ancient civilization. Collectors pay well for these.",
                Category = ItemCategory.Treasure,
                Rarity = ItemRarity.Rare,
                Icon = "🪙",
                Quantity = 12,
                MaxStack = 100,
                SellPrice = 500
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_031",
                Name = "Golden Chalice",
                Description = "A beautifully crafted golden chalice. Worth a fortune.",
                Category = ItemCategory.Treasure,
                Rarity = ItemRarity.Epic,
                Icon = "🏆",
                Quantity = 1,
                MaxStack = 1,
                SellPrice = 3000
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_032",
                Name = "Ruby Gemstone",
                Description = "A flawless ruby. Can be sold or used in crafting.",
                Category = ItemCategory.Treasure,
                Rarity = ItemRarity.Rare,
                Icon = "💎",
                Quantity = 5,
                MaxStack = 50,
                SellPrice = 800
            });
            
            // Keys/Special
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_040",
                Name = "Dungeon Key",
                Description = "Opens the Dark Dungeon entrance.",
                Category = ItemCategory.Special,
                Rarity = ItemRarity.Epic,
                Icon = "🗝️",
                Quantity = 2,
                MaxStack = 10,
                SellPrice = 0,
                IsUnsellable = true
            });
            
            AddItem(new InventoryItem
            {
                ItemId = "ITEM_041",
                Name = "Event Token",
                Description = "Token for the Dragon Festival event.",
                Category = ItemCategory.Special,
                Rarity = ItemRarity.Rare,
                Icon = "🎫",
                Quantity = 47,
                MaxStack = 999,
                SellPrice = 0,
                IsUnsellable = true
            });
            
            CalculateUsedSlots();
        }

        private void AddItem(InventoryItem item)
        {
            _items.Add(item);
        }

        private void CalculateUsedSlots()
        {
            _usedSlots = _items.Count;
        }

        private void CreatePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("InventoryPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.05f);
            rect.anchorMax = new Vector2(0.92f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.1f, 0.98f);
            
            HorizontalLayoutGroup hlayout = _panel.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Left side - Item grid
            CreateItemGridSection();
            
            // Right side - Item details
            CreateDetailSection();
        }

        private void CreateItemGridSection()
        {
            GameObject gridSection = new GameObject("GridSection");
            gridSection.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = gridSection.AddComponent<LayoutElement>();
            le.flexibleWidth = 2;
            le.flexibleHeight = 1;
            
            Image bg = gridSection.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f);
            
            VerticalLayoutGroup vlayout = gridSection.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Header
            CreateGridHeader(gridSection.transform);
            
            // Category tabs
            CreateCategoryTabs(gridSection.transform);
            
            // Sorting options
            CreateSortingBar(gridSection.transform);
            
            // Item grid
            CreateItemGrid(gridSection.transform);
        }

        private void CreateGridHeader(Transform parent)
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(parent, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            
            // Title
            CreateText(header.transform, "🎒 INVENTORY", 24, TextAlignmentOptions.Left, accentColor);
            
            // Slot counter
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(header.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            CreateText(header.transform, $"📦 {_usedSlots}/{_maxSlots} slots", 14, TextAlignmentOptions.Right, new Color(0.7f, 0.7f, 0.7f));
        }

        private void CreateCategoryTabs(Transform parent)
        {
            GameObject tabs = new GameObject("Tabs");
            tabs.transform.SetParent(parent, false);
            
            LayoutElement le = tabs.AddComponent<LayoutElement>();
            le.preferredHeight = 35;
            
            HorizontalLayoutGroup hlayout = tabs.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 5;
            
            CreateCategoryTab(tabs.transform, ItemCategory.All, "📋 All");
            CreateCategoryTab(tabs.transform, ItemCategory.Equipment, "⚔️ Equipment");
            CreateCategoryTab(tabs.transform, ItemCategory.Consumable, "🧪 Consumables");
            CreateCategoryTab(tabs.transform, ItemCategory.Material, "🪨 Materials");
            CreateCategoryTab(tabs.transform, ItemCategory.Treasure, "💰 Treasures");
            CreateCategoryTab(tabs.transform, ItemCategory.Special, "🗝️ Special");
        }

        private void CreateCategoryTab(Transform parent, ItemCategory category, string label)
        {
            GameObject tab = new GameObject($"Tab_{category}");
            tab.transform.SetParent(parent, false);
            
            LayoutElement le = tab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            Color bgColor = category == _selectedCategory ? accentColor : new Color(0.15f, 0.15f, 0.2f);
            
            Image bg = tab.AddComponent<Image>();
            bg.color = bgColor;
            
            Button btn = tab.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectCategory(category));
            
            TextMeshProUGUI text = tab.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 11;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            _categoryTabs[category] = tab;
        }

        private void CreateSortingBar(Transform parent)
        {
            GameObject bar = new GameObject("SortingBar");
            bar.transform.SetParent(parent, false);
            
            LayoutElement le = bar.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            HorizontalLayoutGroup hlayout = bar.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            CreateText(bar.transform, "Sort by:", 11, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            CreateSortButton(bar.transform, SortMode.Rarity, "Rarity");
            CreateSortButton(bar.transform, SortMode.Name, "Name");
            CreateSortButton(bar.transform, SortMode.Level, "Level");
            CreateSortButton(bar.transform, SortMode.Quantity, "Qty");
            CreateSortButton(bar.transform, SortMode.Value, "Value");
            
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(bar.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            CreateSmallButton(bar.transform, "🗑️ Sell Junk", () => SellJunk(), new Color(0.5f, 0.3f, 0.3f));
        }

        private void CreateSortButton(Transform parent, SortMode mode, string label)
        {
            GameObject btn = new GameObject($"Sort_{mode}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 60;
            le.preferredHeight = 22;
            
            Color bgColor = mode == _sortMode ? accentColor : new Color(0.2f, 0.2f, 0.25f);
            
            Image bg = btn.AddComponent<Image>();
            bg.color = bgColor;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => SetSortMode(mode));
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 10;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateItemGrid(Transform parent)
        {
            _itemGrid = new GameObject("ItemGrid");
            _itemGrid.transform.SetParent(parent, false);
            
            LayoutElement le = _itemGrid.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            
            Image bg = _itemGrid.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.03f, 0.05f);
            
            GridLayoutGroup grid = _itemGrid.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(70, 80);
            grid.spacing = new Vector2(5, 5);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            
            RefreshItemGrid();
        }

        private void RefreshItemGrid()
        {
            foreach (Transform child in _itemGrid.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Filter items
            List<InventoryItem> filtered = _selectedCategory == ItemCategory.All 
                ? new List<InventoryItem>(_items)
                : _items.FindAll(i => i.Category == _selectedCategory);
            
            // Sort items
            filtered = SortItems(filtered);
            
            foreach (var item in filtered)
            {
                CreateItemSlot(item);
            }
        }

        private List<InventoryItem> SortItems(List<InventoryItem> items)
        {
            switch (_sortMode)
            {
                case SortMode.Rarity:
                    items.Sort((a, b) => b.Rarity.CompareTo(a.Rarity));
                    break;
                case SortMode.Name:
                    items.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
                    break;
                case SortMode.Level:
                    items.Sort((a, b) => b.Level.CompareTo(a.Level));
                    break;
                case SortMode.Quantity:
                    items.Sort((a, b) => b.Quantity.CompareTo(a.Quantity));
                    break;
                case SortMode.Value:
                    items.Sort((a, b) => (b.SellPrice * b.Quantity).CompareTo(a.SellPrice * a.Quantity));
                    break;
            }
            return items;
        }

        private void CreateItemSlot(InventoryItem item)
        {
            GameObject slot = new GameObject($"Item_{item.ItemId}");
            slot.transform.SetParent(_itemGrid.transform, false);
            
            Color rarityColor = GetRarityColor(item.Rarity);
            bool isSelected = _selectedItem?.ItemId == item.ItemId;
            
            Image bg = slot.AddComponent<Image>();
            bg.color = isSelected ? new Color(rarityColor.r * 0.4f, rarityColor.g * 0.4f, rarityColor.b * 0.4f) 
                                 : new Color(0.1f, 0.1f, 0.12f);
            
            if (isSelected || item.Rarity >= ItemRarity.Rare)
            {
                UnityEngine.UI.Outline outline = slot.AddComponent<Outline>();
                outline.effectColor = rarityColor;
                outline.effectDistance = new Vector2(2, 2);
            }
            
            Button btn = slot.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectItem(item));
            
            VerticalLayoutGroup vlayout = slot.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(5, 5, 5, 5);
            
            // Icon
            CreateText(slot.transform, item.Icon, 28, TextAlignmentOptions.Center);
            
            // Quantity (if stackable)
            if (item.MaxStack > 1 || item.Quantity > 1)
            {
                CreateText(slot.transform, $"x{item.Quantity}", 10, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f));
            }
            
            // Equipped indicator
            if (item.IsEquipped)
            {
                CreateText(slot.transform, "⚡", 12, TextAlignmentOptions.Center, goldColor);
            }
        }

        private void CreateDetailSection()
        {
            _detailPanel = new GameObject("DetailSection");
            _detailPanel.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _detailPanel.AddComponent<LayoutElement>();
            le.preferredWidth = 300;
            le.flexibleHeight = 1;
            
            Image bg = _detailPanel.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.07f, 0.1f);
            
            VerticalLayoutGroup vlayout = _detailPanel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            RefreshDetailPanel();
        }

        private void RefreshDetailPanel()
        {
            foreach (Transform child in _detailPanel.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Close button
            CreateCloseButton();
            
            if (_selectedItem == null)
            {
                CreateText(_detailPanel.transform, "Select an item to view details", 14, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
                return;
            }
            
            // Item header
            CreateItemHeader();
            
            // Description
            CreateText(_detailPanel.transform, _selectedItem.Description, 12, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f));
            
            // Stats (if equipment)
            if (_selectedItem.Stats != null && _selectedItem.Category == ItemCategory.Equipment)
            {
                CreateStatsSection();
            }
            
            // Effect (if consumable)
            if (_selectedItem.IsConsumable && !string.IsNullOrEmpty(_selectedItem.Effect))
            {
                CreateEffectSection();
            }
            
            // Item info
            CreateInfoSection();
            
            // Actions
            CreateActionButtons();
        }

        private void CreateCloseButton()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleRight;
            
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(header.transform, false);
            
            LayoutElement closele = closeBtn.AddComponent<LayoutElement>();
            closele.preferredWidth = 30;
            closele.preferredHeight = 30;
            
            Image closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = new Color(0.5f, 0.2f, 0.2f, 0.8f);
            
            Button btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(Hide);
            
            TextMeshProUGUI x = closeBtn.AddComponent<TextMeshProUGUI>();
            x.text = "✕";
            x.fontSize = 18;
            x.alignment = TextAlignmentOptions.Center;
        }

        private void CreateItemHeader()
        {
            GameObject header = new GameObject("ItemHeader");
            header.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            VerticalLayoutGroup vlayout = header.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            
            Color rarityColor = GetRarityColor(_selectedItem.Rarity);
            
            // Icon
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(header.transform, false);
            
            LayoutElement iconLE = icon.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 60;
            iconLE.preferredHeight = 60;
            
            Image iconBg = icon.AddComponent<Image>();
            iconBg.color = new Color(rarityColor.r * 0.3f, rarityColor.g * 0.3f, rarityColor.b * 0.3f);
            
            Outline iconOutline = icon.AddComponent<Outline>();
            iconOutline.effectColor = rarityColor;
            iconOutline.effectDistance = new Vector2(2, 2);
            
            TextMeshProUGUI iconText = icon.AddComponent<TextMeshProUGUI>();
            iconText.text = _selectedItem.Icon;
            iconText.fontSize = 36;
            iconText.alignment = TextAlignmentOptions.Center;
            
            // Name
            CreateText(header.transform, _selectedItem.Name, 16, TextAlignmentOptions.Center, rarityColor);
            
            // Rarity & Category
            CreateText(header.transform, $"{_selectedItem.Rarity} {_selectedItem.Category}", 11, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            
            // Equipped indicator
            if (_selectedItem.IsEquipped)
            {
                CreateText(header.transform, "⚡ EQUIPPED", 11, TextAlignmentOptions.Center, goldColor);
            }
        }

        private void CreateStatsSection()
        {
            CreateSectionLabel("📊 STATS");
            
            GameObject stats = new GameObject("Stats");
            stats.transform.SetParent(_detailPanel.transform, false);
            
            GridLayoutGroup grid = stats.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(130, 25);
            grid.spacing = new Vector2(5, 3);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            
            var s = _selectedItem.Stats;
            if (s.Attack > 0) CreateStatItem(stats.transform, "⚔️ Attack", $"+{s.Attack}");
            if (s.Defense > 0) CreateStatItem(stats.transform, "🛡️ Defense", $"+{s.Defense}");
            if (s.Health > 0) CreateStatItem(stats.transform, "❤️ Health", $"+{s.Health}");
            if (s.Speed > 0) CreateStatItem(stats.transform, "⚡ Speed", $"+{s.Speed}");
            if (s.FireDamage > 0) CreateStatItem(stats.transform, "🔥 Fire DMG", $"+{s.FireDamage}");
            if (s.Stealth > 0) CreateStatItem(stats.transform, "👻 Stealth", $"+{s.Stealth}");
        }

        private void CreateStatItem(Transform parent, string label, string value)
        {
            GameObject item = new GameObject("Stat");
            item.transform.SetParent(parent, false);
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 5;
            
            CreateText(item.transform, label, 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            CreateText(item.transform, value, 11, TextAlignmentOptions.Left, new Color(0.5f, 0.9f, 0.5f));
        }

        private void CreateEffectSection()
        {
            CreateSectionLabel("✨ EFFECT");
            
            GameObject effect = new GameObject("Effect");
            effect.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = effect.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            Image bg = effect.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.15f, 0.1f);
            
            CreateText(effect.transform, _selectedItem.Effect, 13, TextAlignmentOptions.Center, new Color(0.4f, 0.9f, 0.4f));
        }

        private void CreateInfoSection()
        {
            CreateSectionLabel("📋 INFO");
            
            GameObject info = new GameObject("Info");
            info.transform.SetParent(_detailPanel.transform, false);
            
            VerticalLayoutGroup vlayout = info.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 3;
            
            CreateText(info.transform, $"Quantity: {_selectedItem.Quantity}", 11, TextAlignmentOptions.Center, Color.white);
            
            if (_selectedItem.Level > 0)
            {
                CreateText(info.transform, $"Required Level: {_selectedItem.Level}", 11, TextAlignmentOptions.Center, Color.white);
            }
            
            if (!_selectedItem.IsUnsellable)
            {
                CreateText(info.transform, $"Sell Value: {_selectedItem.SellPrice:N0} 💰 each", 11, TextAlignmentOptions.Center, goldColor);
                CreateText(info.transform, $"Total Value: {(_selectedItem.SellPrice * _selectedItem.Quantity):N0} 💰", 11, TextAlignmentOptions.Center, goldColor);
            }
            else
            {
                CreateText(info.transform, "Cannot be sold", 11, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            }
        }

        private void CreateActionButtons()
        {
            GameObject actions = new GameObject("Actions");
            actions.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = actions.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            VerticalLayoutGroup vlayout = actions.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 8;
            
            // Use/Equip button
            if (_selectedItem.IsConsumable)
            {
                CreateActionButton(actions.transform, "🧪 Use Item", () => UseItem(_selectedItem), new Color(0.3f, 0.6f, 0.3f));
            }
            else if (_selectedItem.Category == ItemCategory.Equipment)
            {
                if (_selectedItem.IsEquipped)
                {
                    CreateActionButton(actions.transform, "❌ Unequip", () => UnequipItem(_selectedItem), new Color(0.5f, 0.4f, 0.3f));
                }
                else
                {
                    CreateActionButton(actions.transform, "⚔️ Equip", () => EquipItem(_selectedItem), accentColor);
                }
            }
            
            // Sell button
            if (!_selectedItem.IsUnsellable)
            {
                CreateActionButton(actions.transform, $"💰 Sell ({_selectedItem.SellPrice:N0})", () => SellItem(_selectedItem, 1), goldColor);
                
                if (_selectedItem.Quantity > 1)
                {
                    CreateActionButton(actions.transform, $"💰 Sell All ({_selectedItem.SellPrice * _selectedItem.Quantity:N0})", () => SellItem(_selectedItem, _selectedItem.Quantity), new Color(0.8f, 0.6f, 0.2f));
                }
            }
            
            // Drop button
            CreateActionButton(actions.transform, "🗑️ Drop", () => DropItem(_selectedItem, 1), new Color(0.5f, 0.3f, 0.3f));
        }

        #region UI Helpers

        private void CreateSectionLabel(string text)
        {
            GameObject label = new GameObject("Label");
            label.transform.SetParent(_detailPanel.transform, false);
            
            LayoutElement le = label.AddComponent<LayoutElement>();
            le.preferredHeight = 25;
            
            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 12;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = accentColor;
        }

        private GameObject CreateText(Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Color? color = null)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color ?? Color.white;
            tmp.enableWordWrapping = true;
            
            return obj;
        }

        private void CreateSmallButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btn = new GameObject($"Btn_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 90;
            le.preferredHeight = 25;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 10;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateActionButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btn = new GameObject($"ActionBtn_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            le.preferredHeight = 35;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 13;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => commonColor,
                ItemRarity.Rare => rareColor,
                ItemRarity.Epic => epicColor,
                ItemRarity.Legendary => legendaryColor,
                _ => Color.white
            };
        }

        #endregion

        #region Item Actions

        private void SelectCategory(ItemCategory category)
        {
            _selectedCategory = category;
            _selectedItem = null;
            
            foreach (var kvp in _categoryTabs)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == category ? accentColor : new Color(0.15f, 0.15f, 0.2f);
            }
            
            RefreshItemGrid();
            RefreshDetailPanel();
        }

        private void SetSortMode(SortMode mode)
        {
            _sortMode = mode;
            RefreshItemGrid();
        }

        private void SelectItem(InventoryItem item)
        {
            _selectedItem = item;
            RefreshItemGrid();
            RefreshDetailPanel();
        }

        private void UseItem(InventoryItem item)
        {
            if (!item.IsConsumable || item.Quantity <= 0) return;
            
            item.Quantity--;
            
            if (item.Quantity <= 0)
            {
                _items.Remove(item);
                _selectedItem = null;
            }
            
            CalculateUsedSlots();
            RefreshItemGrid();
            RefreshDetailPanel();
            
            OnItemUsed?.Invoke(item);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"Used {item.Name}: {item.Effect}");
            }
            
            Debug.Log($"[Inventory] Used item: {item.Name}");
        }

        private void EquipItem(InventoryItem item)
        {
            if (item.Category != ItemCategory.Equipment) return;
            
            // Unequip other items of same type (simplified)
            foreach (var other in _items)
            {
                if (other.Category == ItemCategory.Equipment && other.IsEquipped && other != item)
                {
                    // In a real game, you'd check equipment slots
                    other.IsEquipped = false;
                }
            }
            
            item.IsEquipped = true;
            
            RefreshItemGrid();
            RefreshDetailPanel();
            
            OnItemEquipped?.Invoke(item);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"Equipped {item.Name}");
            }
        }

        private void UnequipItem(InventoryItem item)
        {
            item.IsEquipped = false;
            
            RefreshItemGrid();
            RefreshDetailPanel();
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Unequipped {item.Name}");
            }
        }

        private void SellItem(InventoryItem item, int quantity)
        {
            if (item.IsUnsellable) return;
            
            int actualQty = Mathf.Min(quantity, item.Quantity);
            int totalGold = item.SellPrice * actualQty;
            
            item.Quantity -= actualQty;
            
            if (item.Quantity <= 0)
            {
                _items.Remove(item);
                _selectedItem = null;
            }
            
            CalculateUsedSlots();
            RefreshItemGrid();
            RefreshDetailPanel();
            
            OnItemSold?.Invoke(item);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowResourceGained(ResourceType.Gold, totalGold);
            }
            
            Debug.Log($"[Inventory] Sold {actualQty}x {item.Name} for {totalGold} gold");
        }

        private void DropItem(InventoryItem item, int quantity)
        {
            int actualQty = Mathf.Min(quantity, item.Quantity);
            
            item.Quantity -= actualQty;
            
            if (item.Quantity <= 0)
            {
                _items.Remove(item);
                _selectedItem = null;
            }
            
            CalculateUsedSlots();
            RefreshItemGrid();
            RefreshDetailPanel();
            
            OnItemDropped?.Invoke(item, actualQty);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowWarning($"Dropped {actualQty}x {item.Name}");
            }
        }

        private void SellJunk()
        {
            int totalGold = 0;
            int itemsSold = 0;
            
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (item.Rarity == ItemRarity.Common && !item.IsEquipped && !item.IsUnsellable 
                    && item.Category != ItemCategory.Special)
                {
                    totalGold += item.SellPrice * item.Quantity;
                    itemsSold++;
                    _items.RemoveAt(i);
                }
            }
            
            if (itemsSold > 0)
            {
                _selectedItem = null;
                CalculateUsedSlots();
                RefreshItemGrid();
                RefreshDetailPanel();
                
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowResourceGained(ResourceType.Gold, totalGold);
                    NotificationSystem.Instance.ShowSuccess($"Sold {itemsSold} common items!");
                }
            }
            else
            {
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowInfo("No junk items to sell");
                }
            }
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel.SetActive(true);
            RefreshItemGrid();
            RefreshDetailPanel();
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        public void Toggle()
        {
            if (_panel.activeSelf)
                Hide();
            else
                Show();
        }

        public bool AddItem(string itemId, int quantity)
        {
            var existing = _items.Find(i => i.ItemId == itemId);
            if (existing != null && existing.Quantity + quantity <= existing.MaxStack)
            {
                existing.Quantity += quantity;
                return true;
            }
            // In a real game, create new item from database
            return false;
        }

        public bool RemoveItem(string itemId, int quantity)
        {
            var item = _items.Find(i => i.ItemId == itemId);
            if (item != null && item.Quantity >= quantity)
            {
                item.Quantity -= quantity;
                if (item.Quantity <= 0)
                {
                    _items.Remove(item);
                }
                return true;
            }
            return false;
        }

        public int GetItemQuantity(string itemId)
        {
            var item = _items.Find(i => i.ItemId == itemId);
            return item?.Quantity ?? 0;
        }

        public int GetUsedSlots() => _usedSlots;
        public int GetMaxSlots() => _maxSlots;

        #endregion
    }

    #region Data Classes

    public enum ItemCategory
    {
        All,
        Equipment,
        Consumable,
        Material,
        Treasure,
        Special
    }

    public enum ItemRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum SortMode
    {
        Rarity,
        Name,
        Level,
        Quantity,
        Value
    }

    public class InventoryItem
    {
        public string ItemId;
        public string Name;
        public string Description;
        public ItemCategory Category;
        public ItemRarity Rarity;
        public string Icon;
        public int Quantity;
        public int MaxStack = 1;
        public bool IsEquipped;
        public bool IsConsumable;
        public bool IsUnsellable;
        public string Effect;
        public ItemStats Stats;
        public int SellPrice;
        public int Level;
    }

    public class ItemStats
    {
        public int Attack;
        public int Defense;
        public int Health;
        public int Speed;
        public int FireDamage;
        public int Stealth;
    }

    #endregion
}
