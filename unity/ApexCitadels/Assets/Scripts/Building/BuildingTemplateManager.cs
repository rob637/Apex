using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Building;

namespace ApexCitadels.BuildingTemplates
{
    /// <summary>
    /// Manages pre-designed building templates that players can place as complete structures
    /// </summary>
    public class BuildingTemplateManager : MonoBehaviour
    {
        public static BuildingTemplateManager Instance { get; private set; }

        [Header("Template Configuration")]
        [SerializeField] private TextAsset templatesJson;
        [SerializeField] private bool loadDefaultTemplates = true;

        // Loaded templates
        private Dictionary<string, BuildingTemplate> _templates = new Dictionary<string, BuildingTemplate>();
        private Dictionary<TemplateCategory, List<string>> _templatesByCategory = new Dictionary<TemplateCategory, List<string>>();

        // Events
        public event Action<BuildingTemplate> OnTemplateLoaded;
        public event Action<string> OnTemplatePlaced;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (loadDefaultTemplates)
            {
                LoadDefaultTemplates();
            }

            if (templatesJson != null)
            {
                LoadTemplatesFromJson(templatesJson.text);
            }
        }

        #region Template Loading

        /// <summary>
        /// Load built-in default templates
        /// </summary>
        private void LoadDefaultTemplates()
        {
            // === STARTER STRUCTURES ===
            RegisterTemplate(CreateWatchtower());
            RegisterTemplate(CreateSmallFortress());
            RegisterTemplate(CreateResourceOutpost());
            RegisterTemplate(CreateWalledCompound());
            RegisterTemplate(CreateDefensiveBunker());

            // === ADVANCED STRUCTURES ===
            RegisterTemplate(CreateCommandCenter());
            RegisterTemplate(CreateTradingPost());
            RegisterTemplate(CreateBarracks());
            RegisterTemplate(CreateArmory());

            // === DECORATIVE ===
            RegisterTemplate(CreateVictoryMonument());
            RegisterTemplate(CreateGardenPlaza());
            RegisterTemplate(CreateTorchCircle());

            // === DEFENSIVE ===
            RegisterTemplate(CreateWallSegment());
            RegisterTemplate(CreateGatehouse());
            RegisterTemplate(CreateTurretNest());
            RegisterTemplate(CreateTowerComplex());

            Debug.Log($"[BuildingTemplates] Loaded {_templates.Count} default templates");
        }

        /// <summary>
        /// Load templates from JSON
        /// </summary>
        public void LoadTemplatesFromJson(string json)
        {
            try
            {
                var container = JsonUtility.FromJson<TemplateContainer>(json);
                if (container?.templates != null)
                {
                    foreach (var template in container.templates)
                    {
                        RegisterTemplate(template);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuildingTemplates] Failed to load templates: {e.Message}");
            }
        }

        /// <summary>
        /// Register a template
        /// </summary>
        public void RegisterTemplate(BuildingTemplate template)
        {
            if (string.IsNullOrEmpty(template.Id))
            {
                template.Id = Guid.NewGuid().ToString();
            }

            _templates[template.Id] = template;

            // Index by category
            if (!_templatesByCategory.ContainsKey(template.Category))
            {
                _templatesByCategory[template.Category] = new List<string>();
            }
            _templatesByCategory[template.Category].Add(template.Id);

            OnTemplateLoaded?.Invoke(template);
        }

        #endregion

        #region Template Access

        /// <summary>
        /// Get a template by ID
        /// </summary>
        public BuildingTemplate GetTemplate(string templateId)
        {
            return _templates.TryGetValue(templateId, out var template) ? template : null;
        }

        /// <summary>
        /// Get all templates in a category
        /// </summary>
        public List<BuildingTemplate> GetTemplatesByCategory(TemplateCategory category)
        {
            List<BuildingTemplate> result = new List<BuildingTemplate>();

            if (_templatesByCategory.TryGetValue(category, out var ids))
            {
                foreach (var id in ids)
                {
                    if (_templates.TryGetValue(id, out var template))
                    {
                        result.Add(template);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get all available templates
        /// </summary>
        public List<BuildingTemplate> GetAllTemplates()
        {
            return new List<BuildingTemplate>(_templates.Values);
        }

        /// <summary>
        /// Check if player can afford a template
        /// </summary>
        public bool CanAfford(BuildingTemplate template, ResourceInventory inventory)
        {
            foreach (var cost in template.ResourceCost)
            {
                if (!inventory.HasResource(cost.Type, cost.Amount))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get templates the player can afford
        /// </summary>
        public List<BuildingTemplate> GetAffordableTemplates(ResourceInventory inventory)
        {
            List<BuildingTemplate> affordable = new List<BuildingTemplate>();

            foreach (var template in _templates.Values)
            {
                if (CanAfford(template, inventory))
                {
                    affordable.Add(template);
                }
            }

            return affordable;
        }

        #endregion

        #region Template Placement

        /// <summary>
        /// Place a template at a position
        /// </summary>
        public List<BuildingBlock> PlaceTemplate(string templateId, Vector3 position, Quaternion rotation, string ownerId, string territoryId)
        {
            var template = GetTemplate(templateId);
            if (template == null)
            {
                Debug.LogError($"[BuildingTemplates] Template not found: {templateId}");
                return null;
            }

            List<BuildingBlock> placedBlocks = new List<BuildingBlock>();

            foreach (var blockDef in template.Blocks)
            {
                BuildingBlock block = new BuildingBlock(blockDef.Type)
                {
                    OwnerId = ownerId,
                    TerritoryId = territoryId,
                    LocalPosition = rotation * blockDef.LocalPosition + position,
                    LocalRotation = rotation * blockDef.LocalRotation,
                    LocalScale = blockDef.LocalScale
                };

                placedBlocks.Add(block);
            }

            OnTemplatePlaced?.Invoke(templateId);
            return placedBlocks;
        }

        /// <summary>
        /// Preview a template (returns preview GameObjects without placing)
        /// </summary>
        public List<GameObject> PreviewTemplate(string templateId, Vector3 position, Quaternion rotation)
        {
            var template = GetTemplate(templateId);
            if (template == null) return null;

            List<GameObject> previews = new List<GameObject>();

            foreach (var blockDef in template.Blocks)
            {
                // Create preview primitive based on block type
                GameObject preview = CreatePreviewForBlock(blockDef);
                preview.transform.position = rotation * blockDef.LocalPosition + position;
                preview.transform.rotation = rotation * blockDef.LocalRotation;
                preview.transform.localScale = blockDef.LocalScale;

                // Make semi-transparent
                var renderer = preview.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var material = renderer.material;
                    var color = material.color;
                    color.a = 0.5f;
                    material.color = color;
                    material.SetFloat("_Mode", 3); // Transparent mode
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = 3000;
                }

                previews.Add(preview);
            }

            return previews;
        }

        private GameObject CreatePreviewForBlock(TemplateBlock block)
        {
            GameObject preview;

            switch (block.Type)
            {
                case BlockType.Wall:
                case BlockType.Stone:
                    preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
                case BlockType.Tower:
                case BlockType.Turret:
                    preview = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    break;
                case BlockType.Flag:
                case BlockType.Banner:
                    preview = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    break;
                default:
                    preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
            }

            preview.name = $"Preview_{block.Type}";
            
            // Disable collider for preview
            var collider = preview.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            return preview;
        }

        #endregion

        #region Default Template Definitions

        private BuildingTemplate CreateWatchtower()
        {
            return new BuildingTemplate
            {
                Id = "watchtower",
                Name = "Watchtower",
                Description = "A simple watchtower for surveying the surrounding area",
                Category = TemplateCategory.Starter,
                UnlockLevel = 1,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "stone", Amount = 50 },
                    new ResourceCost { Type = "wood", Amount = 30 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Base platform
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = Vector3.zero, LocalScale = new Vector3(3, 0.5f, 3) },
                    // Tower column
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = new Vector3(0, 3, 0), LocalScale = new Vector3(2, 5, 2) },
                    // Top platform
                    new TemplateBlock { Type = BlockType.Wood, LocalPosition = new Vector3(0, 6, 0), LocalScale = new Vector3(4, 0.3f, 4) },
                    // Flag
                    new TemplateBlock { Type = BlockType.Flag, LocalPosition = new Vector3(0, 8, 0), LocalScale = new Vector3(1, 2, 0.1f) }
                },
                TotalHealth = 350,
                DefenseBonus = 10
            };
        }

        private BuildingTemplate CreateSmallFortress()
        {
            return new BuildingTemplate
            {
                Id = "small_fortress",
                Name = "Small Fortress",
                Description = "A compact defensive fortress with walls and towers",
                Category = TemplateCategory.Defensive,
                UnlockLevel = 5,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "stone", Amount = 200 },
                    new ResourceCost { Type = "wood", Amount = 100 },
                    new ResourceCost { Type = "metal", Amount = 50 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Foundation
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = Vector3.zero, LocalScale = new Vector3(10, 0.5f, 10) },
                    
                    // Walls
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(0, 2, 5), LocalRotation = Quaternion.identity, LocalScale = new Vector3(10, 4, 1) },
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(0, 2, -5), LocalRotation = Quaternion.identity, LocalScale = new Vector3(10, 4, 1) },
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(5, 2, 0), LocalRotation = Quaternion.Euler(0, 90, 0), LocalScale = new Vector3(10, 4, 1) },
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(-5, 2, 0), LocalRotation = Quaternion.Euler(0, 90, 0), LocalScale = new Vector3(10, 4, 1) },
                    
                    // Corner towers
                    new TemplateBlock { Type = BlockType.Tower, LocalPosition = new Vector3(5, 3, 5), LocalScale = new Vector3(2, 6, 2) },
                    new TemplateBlock { Type = BlockType.Tower, LocalPosition = new Vector3(-5, 3, 5), LocalScale = new Vector3(2, 6, 2) },
                    new TemplateBlock { Type = BlockType.Tower, LocalPosition = new Vector3(5, 3, -5), LocalScale = new Vector3(2, 6, 2) },
                    new TemplateBlock { Type = BlockType.Tower, LocalPosition = new Vector3(-5, 3, -5), LocalScale = new Vector3(2, 6, 2) },
                    
                    // Gate
                    new TemplateBlock { Type = BlockType.Gate, LocalPosition = new Vector3(0, 1.5f, 5.5f), LocalScale = new Vector3(3, 3, 0.5f) }
                },
                TotalHealth = 2000,
                DefenseBonus = 30
            };
        }

        private BuildingTemplate CreateResourceOutpost()
        {
            return new BuildingTemplate
            {
                Id = "resource_outpost",
                Name = "Resource Outpost",
                Description = "A small outpost focused on resource gathering",
                Category = TemplateCategory.Economy,
                UnlockLevel = 3,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "wood", Amount = 100 },
                    new ResourceCost { Type = "stone", Amount = 50 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Base
                    new TemplateBlock { Type = BlockType.Wood, LocalPosition = Vector3.zero, LocalScale = new Vector3(6, 0.3f, 6) },
                    
                    // Storage shed
                    new TemplateBlock { Type = BlockType.Wood, LocalPosition = new Vector3(0, 1.5f, 0), LocalScale = new Vector3(4, 3, 4) },
                    
                    // Resource nodes
                    new TemplateBlock { Type = BlockType.ResourceNode, LocalPosition = new Vector3(3, 0.5f, 0), LocalScale = Vector3.one },
                    new TemplateBlock { Type = BlockType.ResourceNode, LocalPosition = new Vector3(-3, 0.5f, 0), LocalScale = Vector3.one },
                    
                    // Torches
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(3, 1, 3), LocalScale = new Vector3(0.2f, 1.5f, 0.2f) },
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(-3, 1, 3), LocalScale = new Vector3(0.2f, 1.5f, 0.2f) }
                },
                TotalHealth = 500,
                ResourceBonus = 25
            };
        }

        private BuildingTemplate CreateWalledCompound()
        {
            return new BuildingTemplate
            {
                Id = "walled_compound",
                Name = "Walled Compound",
                Description = "A protected living space with surrounding walls",
                Category = TemplateCategory.Starter,
                UnlockLevel = 2,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "stone", Amount = 150 },
                    new ResourceCost { Type = "wood", Amount = 80 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Foundation
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = Vector3.zero, LocalScale = new Vector3(8, 0.3f, 8) },
                    
                    // Walls (lower)
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(0, 1, 4), LocalScale = new Vector3(8, 2, 0.5f) },
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(0, 1, -4), LocalScale = new Vector3(8, 2, 0.5f) },
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(4, 1, 0), LocalRotation = Quaternion.Euler(0, 90, 0), LocalScale = new Vector3(8, 2, 0.5f) },
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(-4, 1, 0), LocalRotation = Quaternion.Euler(0, 90, 0), LocalScale = new Vector3(8, 2, 0.5f) },
                    
                    // Central structure
                    new TemplateBlock { Type = BlockType.Wood, LocalPosition = new Vector3(0, 1.5f, 0), LocalScale = new Vector3(3, 3, 3) }
                },
                TotalHealth = 800
            };
        }

        private BuildingTemplate CreateDefensiveBunker()
        {
            return new BuildingTemplate
            {
                Id = "defensive_bunker",
                Name = "Defensive Bunker",
                Description = "A heavily fortified bunker for maximum protection",
                Category = TemplateCategory.Defensive,
                UnlockLevel = 8,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "stone", Amount = 300 },
                    new ResourceCost { Type = "metal", Amount = 150 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Heavy base
                    new TemplateBlock { Type = BlockType.Metal, LocalPosition = Vector3.zero, LocalScale = new Vector3(6, 1, 6) },
                    
                    // Reinforced walls
                    new TemplateBlock { Type = BlockType.Metal, LocalPosition = new Vector3(0, 2, 3), LocalScale = new Vector3(6, 3, 0.5f) },
                    new TemplateBlock { Type = BlockType.Metal, LocalPosition = new Vector3(0, 2, -3), LocalScale = new Vector3(6, 3, 0.5f) },
                    new TemplateBlock { Type = BlockType.Metal, LocalPosition = new Vector3(3, 2, 0), LocalRotation = Quaternion.Euler(0, 90, 0), LocalScale = new Vector3(6, 3, 0.5f) },
                    new TemplateBlock { Type = BlockType.Metal, LocalPosition = new Vector3(-3, 2, 0), LocalRotation = Quaternion.Euler(0, 90, 0), LocalScale = new Vector3(6, 3, 0.5f) },
                    
                    // Roof
                    new TemplateBlock { Type = BlockType.Metal, LocalPosition = new Vector3(0, 3.75f, 0), LocalScale = new Vector3(6.5f, 0.5f, 6.5f) },
                    
                    // Turrets
                    new TemplateBlock { Type = BlockType.Turret, LocalPosition = new Vector3(2.5f, 4.5f, 2.5f), LocalScale = new Vector3(1, 1, 1) },
                    new TemplateBlock { Type = BlockType.Turret, LocalPosition = new Vector3(-2.5f, 4.5f, -2.5f), LocalScale = new Vector3(1, 1, 1) }
                },
                TotalHealth = 3000,
                DefenseBonus = 50
            };
        }

        private BuildingTemplate CreateCommandCenter()
        {
            return new BuildingTemplate
            {
                Id = "command_center",
                Name = "Command Center",
                Description = "A strategic command post for coordinating operations",
                Category = TemplateCategory.Advanced,
                UnlockLevel = 10,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "stone", Amount = 250 },
                    new ResourceCost { Type = "metal", Amount = 200 },
                    new ResourceCost { Type = "crystal", Amount = 50 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Large platform
                    new TemplateBlock { Type = BlockType.Metal, LocalPosition = Vector3.zero, LocalScale = new Vector3(12, 0.5f, 12) },
                    
                    // Main building
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = new Vector3(0, 3, 0), LocalScale = new Vector3(8, 5, 8) },
                    
                    // Beacon
                    new TemplateBlock { Type = BlockType.Beacon, LocalPosition = new Vector3(0, 6, 0), LocalScale = new Vector3(1, 2, 1) },
                    
                    // Antenna/spire
                    new TemplateBlock { Type = BlockType.Metal, LocalPosition = new Vector3(0, 9, 0), LocalScale = new Vector3(0.5f, 4, 0.5f) },
                    
                    // Side towers
                    new TemplateBlock { Type = BlockType.Tower, LocalPosition = new Vector3(5, 3, 5), LocalScale = new Vector3(2, 6, 2) },
                    new TemplateBlock { Type = BlockType.Tower, LocalPosition = new Vector3(-5, 3, -5), LocalScale = new Vector3(2, 6, 2) }
                },
                TotalHealth = 2500,
                DefenseBonus = 25
            };
        }

        private BuildingTemplate CreateTradingPost()
        {
            return new BuildingTemplate
            {
                Id = "trading_post",
                Name = "Trading Post",
                Description = "A marketplace for trading resources with allies",
                Category = TemplateCategory.Economy,
                UnlockLevel = 6,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "wood", Amount = 150 },
                    new ResourceCost { Type = "stone", Amount = 75 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Platform
                    new TemplateBlock { Type = BlockType.Wood, LocalPosition = Vector3.zero, LocalScale = new Vector3(10, 0.3f, 8) },
                    
                    // Stalls
                    new TemplateBlock { Type = BlockType.Wood, LocalPosition = new Vector3(-3, 1.5f, 0), LocalScale = new Vector3(3, 3, 4) },
                    new TemplateBlock { Type = BlockType.Wood, LocalPosition = new Vector3(3, 1.5f, 0), LocalScale = new Vector3(3, 3, 4) },
                    
                    // Canopy
                    new TemplateBlock { Type = BlockType.Wood, LocalPosition = new Vector3(0, 4, 0), LocalScale = new Vector3(10, 0.2f, 8) },
                    
                    // Banners
                    new TemplateBlock { Type = BlockType.Banner, LocalPosition = new Vector3(-5, 3, 0), LocalScale = new Vector3(1.5f, 2, 0.1f) },
                    new TemplateBlock { Type = BlockType.Banner, LocalPosition = new Vector3(5, 3, 0), LocalScale = new Vector3(1.5f, 2, 0.1f) }
                },
                TotalHealth = 600,
                ResourceBonus = 20
            };
        }

        private BuildingTemplate CreateBarracks()
        {
            return new BuildingTemplate
            {
                Id = "barracks",
                Name = "Barracks",
                Description = "Military housing that provides attack bonuses",
                Category = TemplateCategory.Military,
                UnlockLevel = 7,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "stone", Amount = 175 },
                    new ResourceCost { Type = "wood", Amount = 100 },
                    new ResourceCost { Type = "metal", Amount = 75 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Foundation
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = Vector3.zero, LocalScale = new Vector3(10, 0.5f, 6) },
                    
                    // Main building
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = new Vector3(0, 2.5f, 0), LocalScale = new Vector3(9, 4, 5) },
                    
                    // Roof
                    new TemplateBlock { Type = BlockType.Wood, LocalPosition = new Vector3(0, 5, 0), LocalScale = new Vector3(10, 0.5f, 6.5f) },
                    
                    // Training yard flag
                    new TemplateBlock { Type = BlockType.Flag, LocalPosition = new Vector3(6, 3, 0), LocalScale = new Vector3(1, 3, 0.1f) }
                },
                TotalHealth = 1200,
                AttackBonus = 15
            };
        }

        private BuildingTemplate CreateArmory()
        {
            return new BuildingTemplate
            {
                Id = "armory",
                Name = "Armory",
                Description = "Weapons storage providing attack and defense bonuses",
                Category = TemplateCategory.Military,
                UnlockLevel = 9,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "metal", Amount = 250 },
                    new ResourceCost { Type = "stone", Amount = 100 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Reinforced base
                    new TemplateBlock { Type = BlockType.Metal, LocalPosition = Vector3.zero, LocalScale = new Vector3(8, 0.75f, 8) },
                    
                    // Main vault
                    new TemplateBlock { Type = BlockType.Metal, LocalPosition = new Vector3(0, 3, 0), LocalScale = new Vector3(7, 5, 7) },
                    
                    // Security turrets
                    new TemplateBlock { Type = BlockType.Turret, LocalPosition = new Vector3(3.5f, 6, 3.5f), LocalScale = new Vector3(1, 1.5f, 1) },
                    new TemplateBlock { Type = BlockType.Turret, LocalPosition = new Vector3(-3.5f, 6, -3.5f), LocalScale = new Vector3(1, 1.5f, 1) }
                },
                TotalHealth = 2000,
                AttackBonus = 20,
                DefenseBonus = 15
            };
        }

        private BuildingTemplate CreateVictoryMonument()
        {
            return new BuildingTemplate
            {
                Id = "victory_monument",
                Name = "Victory Monument",
                Description = "A decorative monument celebrating your achievements",
                Category = TemplateCategory.Decorative,
                UnlockLevel = 4,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "stone", Amount = 100 },
                    new ResourceCost { Type = "crystal", Amount = 25 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Base pedestal
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = Vector3.zero, LocalScale = new Vector3(4, 1, 4) },
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = new Vector3(0, 1, 0), LocalScale = new Vector3(3, 1, 3) },
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = new Vector3(0, 2, 0), LocalScale = new Vector3(2, 1, 2) },
                    
                    // Obelisk
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = new Vector3(0, 5.5f, 0), LocalScale = new Vector3(1, 6, 1) },
                    
                    // Torches
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(2, 1.5f, 0), LocalScale = new Vector3(0.2f, 2, 0.2f) },
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(-2, 1.5f, 0), LocalScale = new Vector3(0.2f, 2, 0.2f) },
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(0, 1.5f, 2), LocalScale = new Vector3(0.2f, 2, 0.2f) },
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(0, 1.5f, -2), LocalScale = new Vector3(0.2f, 2, 0.2f) }
                },
                TotalHealth = 400
            };
        }

        private BuildingTemplate CreateGardenPlaza()
        {
            return new BuildingTemplate
            {
                Id = "garden_plaza",
                Name = "Garden Plaza",
                Description = "A beautiful plaza for displaying your territory",
                Category = TemplateCategory.Decorative,
                UnlockLevel = 3,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "stone", Amount = 75 },
                    new ResourceCost { Type = "wood", Amount = 50 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Main platform
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = Vector3.zero, LocalScale = new Vector3(8, 0.2f, 8) },
                    
                    // Center feature
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = new Vector3(0, 0.5f, 0), LocalScale = new Vector3(2, 1, 2) },
                    
                    // Corner planters
                    new TemplateBlock { Type = BlockType.Wood, LocalPosition = new Vector3(3, 0.5f, 3), LocalScale = new Vector3(1.5f, 1, 1.5f) },
                    new TemplateBlock { Type = BlockType.Wood, LocalPosition = new Vector3(-3, 0.5f, 3), LocalScale = new Vector3(1.5f, 1, 1.5f) },
                    new TemplateBlock { Type = BlockType.Wood, LocalPosition = new Vector3(3, 0.5f, -3), LocalScale = new Vector3(1.5f, 1, 1.5f) },
                    new TemplateBlock { Type = BlockType.Wood, LocalPosition = new Vector3(-3, 0.5f, -3), LocalScale = new Vector3(1.5f, 1, 1.5f) }
                },
                TotalHealth = 300
            };
        }

        private BuildingTemplate CreateTorchCircle()
        {
            return new BuildingTemplate
            {
                Id = "torch_circle",
                Name = "Torch Circle",
                Description = "A circle of torches for lighting an area",
                Category = TemplateCategory.Decorative,
                UnlockLevel = 1,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "wood", Amount = 30 }
                },
                Blocks = new List<TemplateBlock>
                {
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(3, 1, 0), LocalScale = new Vector3(0.2f, 2, 0.2f) },
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(-3, 1, 0), LocalScale = new Vector3(0.2f, 2, 0.2f) },
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(0, 1, 3), LocalScale = new Vector3(0.2f, 2, 0.2f) },
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(0, 1, -3), LocalScale = new Vector3(0.2f, 2, 0.2f) },
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(2.12f, 1, 2.12f), LocalScale = new Vector3(0.2f, 2, 0.2f) },
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(-2.12f, 1, 2.12f), LocalScale = new Vector3(0.2f, 2, 0.2f) },
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(2.12f, 1, -2.12f), LocalScale = new Vector3(0.2f, 2, 0.2f) },
                    new TemplateBlock { Type = BlockType.Torch, LocalPosition = new Vector3(-2.12f, 1, -2.12f), LocalScale = new Vector3(0.2f, 2, 0.2f) }
                },
                TotalHealth = 200
            };
        }

        private BuildingTemplate CreateWallSegment()
        {
            return new BuildingTemplate
            {
                Id = "wall_segment",
                Name = "Wall Segment",
                Description = "A modular wall section for building perimeters",
                Category = TemplateCategory.Defensive,
                UnlockLevel = 2,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "stone", Amount = 60 }
                },
                Blocks = new List<TemplateBlock>
                {
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = Vector3.zero, LocalScale = new Vector3(5, 3, 1) }
                },
                TotalHealth = 200,
                DefenseBonus = 5
            };
        }

        private BuildingTemplate CreateGatehouse()
        {
            return new BuildingTemplate
            {
                Id = "gatehouse",
                Name = "Gatehouse",
                Description = "A fortified entrance with a gate and guard towers",
                Category = TemplateCategory.Defensive,
                UnlockLevel = 4,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "stone", Amount = 120 },
                    new ResourceCost { Type = "metal", Amount = 40 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Left tower
                    new TemplateBlock { Type = BlockType.Tower, LocalPosition = new Vector3(-3, 2.5f, 0), LocalScale = new Vector3(3, 5, 3) },
                    // Right tower
                    new TemplateBlock { Type = BlockType.Tower, LocalPosition = new Vector3(3, 2.5f, 0), LocalScale = new Vector3(3, 5, 3) },
                    // Gate
                    new TemplateBlock { Type = BlockType.Gate, LocalPosition = new Vector3(0, 1.5f, 0), LocalScale = new Vector3(3, 3, 0.5f) },
                    // Arch
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = new Vector3(0, 4.5f, 0), LocalScale = new Vector3(3, 1, 1) }
                },
                TotalHealth = 800,
                DefenseBonus = 15
            };
        }

        private BuildingTemplate CreateTurretNest()
        {
            return new BuildingTemplate
            {
                Id = "turret_nest",
                Name = "Turret Nest",
                Description = "A cluster of defensive turrets",
                Category = TemplateCategory.Defensive,
                UnlockLevel = 6,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "metal", Amount = 150 },
                    new ResourceCost { Type = "stone", Amount = 50 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Platform
                    new TemplateBlock { Type = BlockType.Metal, LocalPosition = Vector3.zero, LocalScale = new Vector3(6, 0.5f, 6) },
                    // Turrets
                    new TemplateBlock { Type = BlockType.Turret, LocalPosition = new Vector3(0, 1.5f, 0), LocalScale = new Vector3(1.5f, 2, 1.5f) },
                    new TemplateBlock { Type = BlockType.Turret, LocalPosition = new Vector3(2, 1.25f, 2), LocalScale = new Vector3(1, 1.5f, 1) },
                    new TemplateBlock { Type = BlockType.Turret, LocalPosition = new Vector3(-2, 1.25f, 2), LocalScale = new Vector3(1, 1.5f, 1) },
                    new TemplateBlock { Type = BlockType.Turret, LocalPosition = new Vector3(2, 1.25f, -2), LocalScale = new Vector3(1, 1.5f, 1) },
                    new TemplateBlock { Type = BlockType.Turret, LocalPosition = new Vector3(-2, 1.25f, -2), LocalScale = new Vector3(1, 1.5f, 1) }
                },
                TotalHealth = 600,
                DefenseBonus = 35,
                AttackBonus = 25
            };
        }

        private BuildingTemplate CreateTowerComplex()
        {
            return new BuildingTemplate
            {
                Id = "tower_complex",
                Name = "Tower Complex",
                Description = "A multi-tower defensive structure",
                Category = TemplateCategory.Advanced,
                UnlockLevel = 12,
                ResourceCost = new List<ResourceCost>
                {
                    new ResourceCost { Type = "stone", Amount = 400 },
                    new ResourceCost { Type = "metal", Amount = 200 },
                    new ResourceCost { Type = "crystal", Amount = 50 }
                },
                Blocks = new List<TemplateBlock>
                {
                    // Main platform
                    new TemplateBlock { Type = BlockType.Stone, LocalPosition = Vector3.zero, LocalScale = new Vector3(14, 0.5f, 14) },
                    
                    // Central tower (tallest)
                    new TemplateBlock { Type = BlockType.Tower, LocalPosition = new Vector3(0, 5, 0), LocalScale = new Vector3(4, 10, 4) },
                    new TemplateBlock { Type = BlockType.Beacon, LocalPosition = new Vector3(0, 11, 0), LocalScale = new Vector3(1, 2, 1) },
                    
                    // Corner towers
                    new TemplateBlock { Type = BlockType.Tower, LocalPosition = new Vector3(5, 3.5f, 5), LocalScale = new Vector3(3, 7, 3) },
                    new TemplateBlock { Type = BlockType.Tower, LocalPosition = new Vector3(-5, 3.5f, 5), LocalScale = new Vector3(3, 7, 3) },
                    new TemplateBlock { Type = BlockType.Tower, LocalPosition = new Vector3(5, 3.5f, -5), LocalScale = new Vector3(3, 7, 3) },
                    new TemplateBlock { Type = BlockType.Tower, LocalPosition = new Vector3(-5, 3.5f, -5), LocalScale = new Vector3(3, 7, 3) },
                    
                    // Connecting walls
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(2.5f, 2, 5), LocalScale = new Vector3(2, 4, 0.5f) },
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(-2.5f, 2, 5), LocalScale = new Vector3(2, 4, 0.5f) },
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(2.5f, 2, -5), LocalScale = new Vector3(2, 4, 0.5f) },
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(-2.5f, 2, -5), LocalScale = new Vector3(2, 4, 0.5f) },
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(5, 2, 2.5f), LocalRotation = Quaternion.Euler(0, 90, 0), LocalScale = new Vector3(2, 4, 0.5f) },
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(5, 2, -2.5f), LocalRotation = Quaternion.Euler(0, 90, 0), LocalScale = new Vector3(2, 4, 0.5f) },
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(-5, 2, 2.5f), LocalRotation = Quaternion.Euler(0, 90, 0), LocalScale = new Vector3(2, 4, 0.5f) },
                    new TemplateBlock { Type = BlockType.Wall, LocalPosition = new Vector3(-5, 2, -2.5f), LocalRotation = Quaternion.Euler(0, 90, 0), LocalScale = new Vector3(2, 4, 0.5f) },
                    
                    // Turrets on corner towers
                    new TemplateBlock { Type = BlockType.Turret, LocalPosition = new Vector3(5, 7.5f, 5), LocalScale = new Vector3(1, 1.5f, 1) },
                    new TemplateBlock { Type = BlockType.Turret, LocalPosition = new Vector3(-5, 7.5f, 5), LocalScale = new Vector3(1, 1.5f, 1) },
                    new TemplateBlock { Type = BlockType.Turret, LocalPosition = new Vector3(5, 7.5f, -5), LocalScale = new Vector3(1, 1.5f, 1) },
                    new TemplateBlock { Type = BlockType.Turret, LocalPosition = new Vector3(-5, 7.5f, -5), LocalScale = new Vector3(1, 1.5f, 1) }
                },
                TotalHealth = 5000,
                DefenseBonus = 60,
                AttackBonus = 30
            };
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Template category for organization
    /// </summary>
    public enum TemplateCategory
    {
        Starter,
        Defensive,
        Economy,
        Military,
        Advanced,
        Decorative
    }

    /// <summary>
    /// A complete building template
    /// </summary>
    [Serializable]
    public class BuildingTemplate
    {
        public string Id;
        public string Name;
        public string Description;
        public TemplateCategory Category;
        public int UnlockLevel;
        public List<ResourceCost> ResourceCost = new List<ResourceCost>();
        public List<TemplateBlock> Blocks = new List<TemplateBlock>();
        public int TotalHealth;
        public int DefenseBonus;
        public int AttackBonus;
        public int ResourceBonus;
        public string PreviewImagePath;
    }

    /// <summary>
    /// A single block within a template
    /// </summary>
    [Serializable]
    public class TemplateBlock
    {
        public BlockType Type;
        public Vector3 LocalPosition = Vector3.zero;
        public Quaternion LocalRotation = Quaternion.identity;
        public Vector3 LocalScale = Vector3.one;
    }

    /// <summary>
    /// Resource cost for building
    /// </summary>
    [Serializable]
    public class ResourceCost
    {
        public string Type;
        public int Amount;
    }

    /// <summary>
    /// Container for JSON serialization
    /// </summary>
    [Serializable]
    public class TemplateContainer
    {
        public List<BuildingTemplate> templates;
    }

    /// <summary>
    /// Player's resource inventory (interface for checking affordability)
    /// </summary>
    public interface ResourceInventory
    {
        bool HasResource(string type, int amount);
        int GetResourceAmount(string type);
    }

    #endregion
}
