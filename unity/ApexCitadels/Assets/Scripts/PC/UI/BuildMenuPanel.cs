using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Building;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// PC Building menu panel with full building catalog.
    /// Provides category-based building selection and placement.
    /// </summary>
    public class BuildMenuPanel : MonoBehaviour
    {
        [Header("Categories")]
        [SerializeField] private Transform categoryContainer;
        [SerializeField] private GameObject categoryButtonPrefab;

        [Header("Building Grid")]
        [SerializeField] private Transform buildingGridContainer;
        [SerializeField] private GameObject buildingCardPrefab;
        [SerializeField] private int gridColumns = 4;

        [Header("Building Info")]
        [SerializeField] private GameObject buildingInfoPanel;
        [SerializeField] private TextMeshProUGUI buildingNameText;
        [SerializeField] private TextMeshProUGUI buildingDescriptionText;
        [SerializeField] private TextMeshProUGUI buildingCostText;
        [SerializeField] private Image buildingPreviewImage;
        [SerializeField] private Button selectBuildingButton;

        [Header("Search")]
        [SerializeField] private TMP_InputField searchInput;

        // Events
        public event Action<BlockType> OnBuildingSelected;
        public event Action OnPanelClosed;

        // State
        private BuildMenuCategory _selectedCategory = BuildMenuCategory.Foundations;
        private BlockType _hoveredBuilding;
        private Dictionary<BuildMenuCategory, List<BlockType>> _categoryBuildings;
        private List<GameObject> _currentCards = new List<GameObject>();

        private void Awake()
        {
            InitializeCategoryData();
            SetupSearch();
        }

        private void OnEnable()
        {
            CreateCategoryButtons();
            SelectCategory(_selectedCategory);
        }

        private void InitializeCategoryData()
        {
            _categoryBuildings = new Dictionary<BuildMenuCategory, List<BlockType>>
            {
                { BuildMenuCategory.Foundations, new List<BlockType>
                    { BlockType.Stone, BlockType.Wood, BlockType.Metal, BlockType.Foundation }
                },
                { BuildMenuCategory.Walls, new List<BlockType>
                    { BlockType.WallStone, BlockType.WallWood, BlockType.WallMetal, BlockType.WallReinforced,
                      BlockType.Fence, BlockType.Gate }
                },
                { BuildMenuCategory.Defenses, new List<BlockType>
                    { BlockType.Tower, BlockType.ArrowTower, BlockType.CannonTower, BlockType.MageTower,
                      BlockType.Trap, BlockType.SpikeTrap, BlockType.Barricade }
                },
                { BuildMenuCategory.Production, new List<BlockType>
                    { BlockType.Mine, BlockType.Quarry, BlockType.Sawmill, BlockType.Forge,
                      BlockType.CrystalExtractor, BlockType.StorageVault }
                },
                { BuildMenuCategory.Decorative, new List<BlockType>
                    { BlockType.Pillar, BlockType.Statue, BlockType.Flag, BlockType.Torch,
                      BlockType.Garden, BlockType.Fountain }
                },
                { BuildMenuCategory.Special, new List<BlockType>
                    { BlockType.CitadelCore, BlockType.Portal, BlockType.Beacon, BlockType.AncientRelic }
                }
            };
        }

        private void SetupSearch()
        {
            if (searchInput != null)
            {
                searchInput.onValueChanged.AddListener(OnSearchChanged);
            }
        }

        #region Categories

        private void CreateCategoryButtons()
        {
            if (categoryContainer == null || categoryButtonPrefab == null) return;

            // Clear existing
            foreach (Transform child in categoryContainer)
            {
                Destroy(child.gameObject);
            }

            // Create category buttons
            foreach (BuildMenuCategory category in Enum.GetValues(typeof(BuildMenuCategory)))
            {
                GameObject buttonObj = Instantiate(categoryButtonPrefab, categoryContainer);
                var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = GetCategoryDisplayName(category);
                }

                var button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    BuildMenuCategory cat = category; // Capture for closure
                    button.onClick.AddListener(() => SelectCategory(cat));
                }
            }
        }

        private void SelectCategory(BuildMenuCategory category)
        {
            _selectedCategory = category;
            RefreshBuildingGrid();
        }

        private string GetCategoryDisplayName(BuildMenuCategory category)
        {
            return category switch
            {
                BuildMenuCategory.Foundations => "📦 Foundations",
                BuildMenuCategory.Walls => "🧱 Walls",
                BuildMenuCategory.Defenses => "🗼 Defenses",
                BuildMenuCategory.Production => "⚙️ Production",
                BuildMenuCategory.Decorative => "🎨 Decorative",
                BuildMenuCategory.Special => "⭐ Special",
                _ => category.ToString()
            };
        }

        #endregion

        #region Building Grid

        private void RefreshBuildingGrid()
        {
            ClearBuildingGrid();

            if (!_categoryBuildings.TryGetValue(_selectedCategory, out var buildings))
                return;

            foreach (var blockType in buildings)
            {
                CreateBuildingCard(blockType);
            }
        }

        private void ClearBuildingGrid()
        {
            foreach (var card in _currentCards)
            {
                Destroy(card);
            }
            _currentCards.Clear();
        }

        private void CreateBuildingCard(BlockType blockType)
        {
            if (buildingCardPrefab == null || buildingGridContainer == null) return;

            GameObject cardObj = Instantiate(buildingCardPrefab, buildingGridContainer);
            _currentCards.Add(cardObj);

            // Set building name
            var texts = cardObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0)
            {
                texts[0].text = GetBuildingDisplayName(blockType);
            }

            // Set cost preview
            if (texts.Length > 1)
            {
                var cost = GetBuildingCost(blockType);
                texts[1].text = $"🪨{cost.Stone} 🪵{cost.Wood}";
            }

            // Preview image (would load from resources)
            var images = cardObj.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img.name.Contains("Preview") || img.name.Contains("Icon"))
                {
                    img.color = GetBuildingPreviewColor(blockType);
                }
            }

            // Click to select
            var button = cardObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => SelectBuilding(blockType));
            }

            // Hover for info
            var eventTrigger = cardObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            pointerEnter.callback.AddListener((data) => ShowBuildingInfo(blockType));
            eventTrigger.triggers.Add(pointerEnter);

            var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
            };
            pointerExit.callback.AddListener((data) => HideBuildingInfo());
            eventTrigger.triggers.Add(pointerExit);
        }

        #endregion

        #region Building Info

        private void ShowBuildingInfo(BlockType blockType)
        {
            _hoveredBuilding = blockType;

            if (buildingInfoPanel != null)
                buildingInfoPanel.SetActive(true);

            if (buildingNameText != null)
                buildingNameText.text = GetBuildingDisplayName(blockType);

            if (buildingDescriptionText != null)
                buildingDescriptionText.text = GetBuildingDescription(blockType);

            if (buildingCostText != null)
            {
                var cost = GetBuildingCost(blockType);
                buildingCostText.text = $"Cost: {cost.Stone} Stone, {cost.Wood} Wood, {cost.Metal} Metal";
            }

            if (buildingPreviewImage != null)
                buildingPreviewImage.color = GetBuildingPreviewColor(blockType);
        }

        private void HideBuildingInfo()
        {
            if (buildingInfoPanel != null)
                buildingInfoPanel.SetActive(false);
        }

        private void SelectBuilding(BlockType blockType)
        {
            Debug.Log($"[BuildMenu] Selected building: {blockType}");
            OnBuildingSelected?.Invoke(blockType);

            // Enter placement mode
            if (BaseEditor.Instance != null)
            {
                BaseEditor.Instance.SelectBlockType(blockType);
            }
        }

        #endregion

        #region Search

        private void OnSearchChanged(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                RefreshBuildingGrid();
                return;
            }

            ClearBuildingGrid();

            // Search all categories
            foreach (var kvp in _categoryBuildings)
            {
                foreach (var blockType in kvp.Value)
                {
                    string name = GetBuildingDisplayName(blockType).ToLower();
                    if (name.Contains(searchText.ToLower()))
                    {
                        CreateBuildingCard(blockType);
                    }
                }
            }
        }

        #endregion

        #region Building Data

        private string GetBuildingDisplayName(BlockType type)
        {
            return type switch
            {
                BlockType.Stone => "Stone Block",
                BlockType.Wood => "Wooden Plank",
                BlockType.Metal => "Metal Plate",
                BlockType.Foundation => "Foundation",
                BlockType.WallStone => "Stone Wall",
                BlockType.WallWood => "Wooden Wall",
                BlockType.WallMetal => "Metal Wall",
                BlockType.WallReinforced => "Reinforced Wall",
                BlockType.Fence => "Fence",
                BlockType.Gate => "Gate",
                BlockType.Tower => "Watch Tower",
                BlockType.ArrowTower => "Arrow Tower",
                BlockType.CannonTower => "Cannon Tower",
                BlockType.MageTower => "Mage Tower",
                BlockType.Trap => "Hidden Trap",
                BlockType.SpikeTrap => "Spike Trap",
                BlockType.Barricade => "Barricade",
                BlockType.Mine => "Mine",
                BlockType.Quarry => "Quarry",
                BlockType.Sawmill => "Sawmill",
                BlockType.Forge => "Forge",
                BlockType.CrystalExtractor => "Crystal Extractor",
                BlockType.StorageVault => "Storage Vault",
                BlockType.Pillar => "Decorative Pillar",
                BlockType.Statue => "Statue",
                BlockType.Flag => "Banner Flag",
                BlockType.Torch => "Torch",
                BlockType.Garden => "Garden",
                BlockType.Fountain => "Fountain",
                BlockType.CitadelCore => "Citadel Core",
                BlockType.Portal => "Portal",
                BlockType.Beacon => "Beacon",
                BlockType.AncientRelic => "Ancient Relic",
                _ => type.ToString()
            };
        }

        private string GetBuildingDescription(BlockType type)
        {
            return type switch
            {
                BlockType.Stone => "Basic stone building block. Durable and affordable.",
                BlockType.Wood => "Wooden plank for quick construction. Burns easily.",
                BlockType.Metal => "Strong metal plate. Expensive but very durable.",
                BlockType.Tower => "Defensive tower. Provides vision and basic defense.",
                BlockType.ArrowTower => "Fires arrows at enemies. Good range.",
                BlockType.CannonTower => "Heavy damage cannon. Slow but devastating.",
                BlockType.MageTower => "Magical attacks ignore armor. High damage.",
                BlockType.Mine => "Produces metal ore over time.",
                BlockType.Quarry => "Produces stone over time.",
                BlockType.Sawmill => "Produces wood from nearby trees.",
                BlockType.CitadelCore => "The heart of your territory. Protect it!",
                _ => "A useful building for your citadel."
            };
        }

        private ResourceCost GetBuildingCost(BlockType type)
        {
            return type switch
            {
                BlockType.Stone => new ResourceCost { Stone = 10 },
                BlockType.Wood => new ResourceCost { Wood = 10 },
                BlockType.Metal => new ResourceCost { Metal = 10 },
                BlockType.Tower => new ResourceCost { Stone = 100, Wood = 50 },
                BlockType.ArrowTower => new ResourceCost { Stone = 150, Wood = 100, Metal = 25 },
                BlockType.CannonTower => new ResourceCost { Stone = 200, Metal = 150 },
                BlockType.MageTower => new ResourceCost { Stone = 150, Crystal = 100 },
                BlockType.Mine => new ResourceCost { Stone = 200, Wood = 100, Metal = 50 },
                BlockType.Quarry => new ResourceCost { Wood = 150, Metal = 50 },
                BlockType.Sawmill => new ResourceCost { Stone = 100, Metal = 50 },
                BlockType.CitadelCore => new ResourceCost { Stone = 500, Wood = 300, Metal = 200, Crystal = 100 },
                _ => new ResourceCost { Stone = 20, Wood = 10 }
            };
        }

        private Color GetBuildingPreviewColor(BlockType type)
        {
            return type switch
            {
                BlockType.Stone or BlockType.WallStone => new Color(0.5f, 0.5f, 0.5f),
                BlockType.Wood or BlockType.WallWood => new Color(0.6f, 0.4f, 0.2f),
                BlockType.Metal or BlockType.WallMetal => new Color(0.7f, 0.7f, 0.8f),
                BlockType.Tower or BlockType.ArrowTower => new Color(0.4f, 0.4f, 0.5f),
                BlockType.CannonTower => new Color(0.3f, 0.3f, 0.35f),
                BlockType.MageTower => new Color(0.5f, 0.3f, 0.8f),
                BlockType.Mine or BlockType.Quarry => new Color(0.4f, 0.35f, 0.3f),
                BlockType.CitadelCore => new Color(1f, 0.8f, 0.2f),
                _ => Color.white
            };
        }

        #endregion
    }

    /// <summary>
    /// Building categories for the menu
    /// </summary>
    public enum BuildMenuCategory
    {
        Foundations,
        Walls,
        Defenses,
        Production,
        Decorative,
        Special
    }
}
