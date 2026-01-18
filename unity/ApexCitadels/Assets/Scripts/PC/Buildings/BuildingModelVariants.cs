using UnityEngine;
using System;
using System.Collections.Generic;

namespace ApexCitadels.PC.Buildings
{
    /// <summary>
    /// Building Model Variant System for PC client.
    /// Manages model variations for visual variety:
    /// - Multiple mesh variants per building type
    /// - Themed variants (seasons, factions)
    /// - Random detail attachments
    /// - Color scheme variations
    /// </summary>
    public class BuildingModelVariants : MonoBehaviour
    {
        [Header("Variant Settings")]
        [SerializeField] private bool enableVariants = true;
        [SerializeField] private bool consistentVariantsPerTerritory = true;
        [SerializeField] private int maxVariantsPerType = 5;
        
        [Header("Detail Attachments")]
        [SerializeField] private bool enableDetailAttachments = true;
        [SerializeField] private float attachmentProbability = 0.7f;
        [SerializeField] private int maxAttachmentsPerBuilding = 4;
        
        [Header("Color Variations")]
        [SerializeField] private bool enableColorVariations = true;
        [SerializeField] private float colorVariationAmount = 0.15f;
        [SerializeField] private bool enableFactionColors = true;
        
        // Singleton
        private static BuildingModelVariants _instance;
        public static BuildingModelVariants Instance => _instance;
        
        // Variant definitions
        private Dictionary<BuildingCategory, List<ModelVariantDefinition>> _variantDefinitions = 
            new Dictionary<BuildingCategory, List<ModelVariantDefinition>>();
            
        // Detail attachment prefabs
        private Dictionary<DetailCategory, List<DetailAttachment>> _detailAttachments = 
            new Dictionary<DetailCategory, List<DetailAttachment>>();
            
        // Color palettes by faction
        private Dictionary<string, FactionColorPalette> _factionPalettes = 
            new Dictionary<string, FactionColorPalette>();
        
        // Cache of applied variants
        private Dictionary<string, AppliedVariant> _appliedVariants = 
            new Dictionary<string, AppliedVariant>();
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeVariantDefinitions();
            InitializeDetailAttachments();
            InitializeFactionPalettes();
        }
        
        #region Initialization
        
        private void InitializeVariantDefinitions()
        {
            // Residential variants
            _variantDefinitions[BuildingCategory.Residential] = new List<ModelVariantDefinition>
            {
                new ModelVariantDefinition
                {
                    Name = "Cottage",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.RoofStyle, Style = "Thatch" },
                        new MeshModification { Type = ModificationType.WallTexture, Style = "Wattle" },
                        new MeshModification { Type = ModificationType.WindowStyle, Style = "Small" }
                    },
                    ScaleVariation = new Vector3(0.9f, 0.85f, 0.9f)
                },
                new ModelVariantDefinition
                {
                    Name = "TownHouse",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.RoofStyle, Style = "Slate" },
                        new MeshModification { Type = ModificationType.WallTexture, Style = "Plaster" },
                        new MeshModification { Type = ModificationType.WindowStyle, Style = "Tall" }
                    },
                    ScaleVariation = new Vector3(1f, 1.1f, 1f)
                },
                new ModelVariantDefinition
                {
                    Name = "Farmhouse",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.RoofStyle, Style = "Wooden" },
                        new MeshModification { Type = ModificationType.WallTexture, Style = "Stone" },
                        new MeshModification { Type = ModificationType.Addition, Style = "Barn" }
                    },
                    ScaleVariation = new Vector3(1.1f, 0.95f, 1.2f)
                },
                new ModelVariantDefinition
                {
                    Name = "Manor",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.RoofStyle, Style = "Tile" },
                        new MeshModification { Type = ModificationType.WallTexture, Style = "Brick" },
                        new MeshModification { Type = ModificationType.WindowStyle, Style = "Arched" },
                        new MeshModification { Type = ModificationType.Addition, Style = "Chimney" }
                    },
                    ScaleVariation = new Vector3(1.2f, 1.15f, 1.1f)
                }
            };
            
            // Military variants
            _variantDefinitions[BuildingCategory.Military] = new List<ModelVariantDefinition>
            {
                new ModelVariantDefinition
                {
                    Name = "Barracks_Standard",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.RoofStyle, Style = "Flat" },
                        new MeshModification { Type = ModificationType.WallTexture, Style = "Stone" }
                    }
                },
                new ModelVariantDefinition
                {
                    Name = "Barracks_Fortified",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.RoofStyle, Style = "Crenellated" },
                        new MeshModification { Type = ModificationType.WallTexture, Style = "Reinforced" },
                        new MeshModification { Type = ModificationType.Addition, Style = "Towers" }
                    }
                },
                new ModelVariantDefinition
                {
                    Name = "Barracks_Training",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.Addition, Style = "TrainingYard" },
                        new MeshModification { Type = ModificationType.Addition, Style = "Dummies" }
                    }
                }
            };
            
            // Production variants
            _variantDefinitions[BuildingCategory.Production] = new List<ModelVariantDefinition>
            {
                new ModelVariantDefinition
                {
                    Name = "Workshop_Standard",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.RoofStyle, Style = "Gabled" },
                        new MeshModification { Type = ModificationType.Addition, Style = "Waterwheel" }
                    }
                },
                new ModelVariantDefinition
                {
                    Name = "Workshop_Large",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.Addition, Style = "Extension" },
                        new MeshModification { Type = ModificationType.Addition, Style = "Smokestacks" }
                    },
                    ScaleVariation = new Vector3(1.3f, 1f, 1.2f)
                },
                new ModelVariantDefinition
                {
                    Name = "Workshop_Artisan",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.WindowStyle, Style = "Large" },
                        new MeshModification { Type = ModificationType.Addition, Style = "DisplayArea" }
                    }
                }
            };
            
            // Defense variants
            _variantDefinitions[BuildingCategory.Defense] = new List<ModelVariantDefinition>
            {
                new ModelVariantDefinition
                {
                    Name = "Tower_Round",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.Shape, Style = "Cylindrical" },
                        new MeshModification { Type = ModificationType.RoofStyle, Style = "Conical" }
                    }
                },
                new ModelVariantDefinition
                {
                    Name = "Tower_Square",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.Shape, Style = "Rectangular" },
                        new MeshModification { Type = ModificationType.RoofStyle, Style = "Crenellated" }
                    }
                },
                new ModelVariantDefinition
                {
                    Name = "Tower_Wizard",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.Shape, Style = "Tapered" },
                        new MeshModification { Type = ModificationType.RoofStyle, Style = "SpireWithOrb" },
                        new MeshModification { Type = ModificationType.Addition, Style = "MagicCrystals" }
                    }
                }
            };
            
            // Magic variants
            _variantDefinitions[BuildingCategory.Magic] = new List<ModelVariantDefinition>
            {
                new ModelVariantDefinition
                {
                    Name = "Temple_Classical",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.Addition, Style = "Columns" },
                        new MeshModification { Type = ModificationType.RoofStyle, Style = "Pediment" }
                    }
                },
                new ModelVariantDefinition
                {
                    Name = "Temple_Gothic",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.Addition, Style = "Spires" },
                        new MeshModification { Type = ModificationType.WindowStyle, Style = "StainedGlass" },
                        new MeshModification { Type = ModificationType.RoofStyle, Style = "Gothic" }
                    }
                },
                new ModelVariantDefinition
                {
                    Name = "Temple_Mystical",
                    MeshModifications = new MeshModification[]
                    {
                        new MeshModification { Type = ModificationType.Addition, Style = "FloatingCrystals" },
                        new MeshModification { Type = ModificationType.Addition, Style = "Runes" },
                        new MeshModification { Type = ModificationType.RoofStyle, Style = "Dome" }
                    }
                }
            };
        }
        
        private void InitializeDetailAttachments()
        {
            // Exterior decorations
            _detailAttachments[DetailCategory.ExteriorDecoration] = new List<DetailAttachment>
            {
                new DetailAttachment
                {
                    Name = "Flower_Box",
                    PlacementType = PlacementType.WindowSill,
                    Scale = new Vector3(0.5f, 0.3f, 0.3f),
                    RandomRotation = false
                },
                new DetailAttachment
                {
                    Name = "Hanging_Sign",
                    PlacementType = PlacementType.Entrance,
                    Scale = new Vector3(0.6f, 0.4f, 0.1f),
                    Offset = new Vector3(1f, 2f, 0)
                },
                new DetailAttachment
                {
                    Name = "Lantern",
                    PlacementType = PlacementType.Entrance,
                    Scale = new Vector3(0.3f, 0.5f, 0.3f),
                    Offset = new Vector3(-1f, 2.5f, 0),
                    HasLight = true,
                    LightColor = new Color(1f, 0.8f, 0.5f),
                    LightIntensity = 1.5f
                },
                new DetailAttachment
                {
                    Name = "Banner",
                    PlacementType = PlacementType.Wall,
                    Scale = new Vector3(0.4f, 1.2f, 0.05f),
                    UsesFactionColor = true
                },
                new DetailAttachment
                {
                    Name = "Shield",
                    PlacementType = PlacementType.Entrance,
                    Scale = new Vector3(0.5f, 0.6f, 0.1f),
                    Offset = new Vector3(0, 3f, 0),
                    UsesFactionColor = true
                }
            };
            
            // Roof decorations
            _detailAttachments[DetailCategory.RoofDecoration] = new List<DetailAttachment>
            {
                new DetailAttachment
                {
                    Name = "Weather_Vane",
                    PlacementType = PlacementType.RoofPeak,
                    Scale = new Vector3(0.3f, 0.5f, 0.1f),
                    RandomRotation = true
                },
                new DetailAttachment
                {
                    Name = "Chimney",
                    PlacementType = PlacementType.Roof,
                    Scale = new Vector3(0.5f, 1f, 0.5f),
                    HasParticles = true,
                    ParticleType = "Smoke"
                },
                new DetailAttachment
                {
                    Name = "Birds_Nest",
                    PlacementType = PlacementType.RoofEdge,
                    Scale = new Vector3(0.4f, 0.3f, 0.4f)
                },
                new DetailAttachment
                {
                    Name = "Flag",
                    PlacementType = PlacementType.RoofPeak,
                    Scale = new Vector3(0.5f, 0.4f, 0.05f),
                    UsesFactionColor = true,
                    HasAnimation = true,
                    AnimationType = "Wave"
                }
            };
            
            // Ground decorations
            _detailAttachments[DetailCategory.GroundDecoration] = new List<DetailAttachment>
            {
                new DetailAttachment
                {
                    Name = "Barrel",
                    PlacementType = PlacementType.GroundNearWall,
                    Scale = new Vector3(0.4f, 0.6f, 0.4f),
                    RandomRotation = true
                },
                new DetailAttachment
                {
                    Name = "Crate",
                    PlacementType = PlacementType.GroundNearWall,
                    Scale = new Vector3(0.5f, 0.5f, 0.5f),
                    RandomRotation = true
                },
                new DetailAttachment
                {
                    Name = "Cart",
                    PlacementType = PlacementType.GroundNearEntrance,
                    Scale = new Vector3(1f, 0.8f, 0.6f)
                },
                new DetailAttachment
                {
                    Name = "Well",
                    PlacementType = PlacementType.GroundNearEntrance,
                    Scale = new Vector3(0.8f, 1f, 0.8f)
                },
                new DetailAttachment
                {
                    Name = "Bush",
                    PlacementType = PlacementType.GroundCorner,
                    Scale = new Vector3(0.6f, 0.5f, 0.6f),
                    RandomScale = 0.2f
                }
            };
        }
        
        private void InitializeFactionPalettes()
        {
            _factionPalettes["Red"] = new FactionColorPalette
            {
                Primary = new Color(0.8f, 0.2f, 0.2f),
                Secondary = new Color(0.5f, 0.1f, 0.1f),
                Accent = new Color(1f, 0.8f, 0.3f),
                Trim = new Color(0.3f, 0.3f, 0.3f)
            };
            
            _factionPalettes["Blue"] = new FactionColorPalette
            {
                Primary = new Color(0.2f, 0.3f, 0.8f),
                Secondary = new Color(0.1f, 0.2f, 0.5f),
                Accent = new Color(0.9f, 0.9f, 1f),
                Trim = new Color(0.4f, 0.4f, 0.5f)
            };
            
            _factionPalettes["Green"] = new FactionColorPalette
            {
                Primary = new Color(0.2f, 0.6f, 0.3f),
                Secondary = new Color(0.1f, 0.4f, 0.15f),
                Accent = new Color(0.9f, 1f, 0.7f),
                Trim = new Color(0.4f, 0.35f, 0.3f)
            };
            
            _factionPalettes["Purple"] = new FactionColorPalette
            {
                Primary = new Color(0.5f, 0.2f, 0.6f),
                Secondary = new Color(0.3f, 0.1f, 0.4f),
                Accent = new Color(1f, 0.8f, 1f),
                Trim = new Color(0.4f, 0.3f, 0.4f)
            };
            
            _factionPalettes["Neutral"] = new FactionColorPalette
            {
                Primary = new Color(0.5f, 0.5f, 0.45f),
                Secondary = new Color(0.4f, 0.4f, 0.35f),
                Accent = new Color(0.7f, 0.7f, 0.65f),
                Trim = new Color(0.35f, 0.35f, 0.3f)
            };
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Apply a random variant to a building
        /// </summary>
        public void ApplyRandomVariant(string buildingId, GameObject building, BuildingCategory category, 
            string territoryId = null, string factionId = null)
        {
            if (!enableVariants)
            {
                _appliedVariants[buildingId] = new AppliedVariant { BuildingId = buildingId, Building = building };
                return;
            }
            
            // Get deterministic seed
            int seed = consistentVariantsPerTerritory && !string.IsNullOrEmpty(territoryId) ?
                (territoryId + buildingId).GetHashCode() :
                buildingId.GetHashCode();
            
            var rng = new System.Random(seed);
            
            // Select variant
            ModelVariantDefinition variant = null;
            if (_variantDefinitions.TryGetValue(category, out var variants) && variants.Count > 0)
            {
                int variantIndex = rng.Next(0, Mathf.Min(variants.Count, maxVariantsPerType));
                variant = variants[variantIndex];
            }
            
            // Apply variant modifications
            if (variant != null)
            {
                ApplyVariantModifications(building, variant, rng);
            }
            
            // Apply color variations
            if (enableColorVariations)
            {
                ApplyColorVariations(building, factionId, rng);
            }
            
            // Add detail attachments
            if (enableDetailAttachments)
            {
                AddDetailAttachments(building, category, factionId, rng);
            }
            
            // Cache applied variant
            _appliedVariants[buildingId] = new AppliedVariant
            {
                BuildingId = buildingId,
                Building = building,
                Variant = variant,
                FactionId = factionId
            };
        }
        
        /// <summary>
        /// Apply a specific variant to a building
        /// </summary>
        public void ApplySpecificVariant(string buildingId, GameObject building, string variantName, string factionId = null)
        {
            ModelVariantDefinition variant = null;
            
            foreach (var kvp in _variantDefinitions)
            {
                variant = kvp.Value.Find(v => v.Name == variantName);
                if (variant != null) break;
            }
            
            if (variant != null)
            {
                ApplyVariantModifications(building, variant, new System.Random(buildingId.GetHashCode()));
            }
            
            if (enableColorVariations && !string.IsNullOrEmpty(factionId))
            {
                ApplyColorVariations(building, factionId, new System.Random(buildingId.GetHashCode()));
            }
            
            _appliedVariants[buildingId] = new AppliedVariant
            {
                BuildingId = buildingId,
                Building = building,
                Variant = variant,
                FactionId = factionId
            };
        }
        
        /// <summary>
        /// Get current variant info for a building
        /// </summary>
        public AppliedVariant GetAppliedVariant(string buildingId)
        {
            _appliedVariants.TryGetValue(buildingId, out var variant);
            return variant;
        }
        
        /// <summary>
        /// Change faction colors on a building
        /// </summary>
        public void UpdateFactionColors(string buildingId, string newFactionId)
        {
            if (_appliedVariants.TryGetValue(buildingId, out var variant))
            {
                ApplyColorVariations(variant.Building, newFactionId, new System.Random(buildingId.GetHashCode()));
                variant.FactionId = newFactionId;
            }
        }
        
        /// <summary>
        /// Remove all variant modifications
        /// </summary>
        public void RemoveVariant(string buildingId)
        {
            if (_appliedVariants.TryGetValue(buildingId, out var variant))
            {
                // Cleanup detail attachments
                foreach (var attachment in variant.Attachments)
                {
                    if (attachment != null) Destroy(attachment);
                }
                
                _appliedVariants.Remove(buildingId);
            }
        }
        
        #endregion
        
        #region Variant Application
        
        private void ApplyVariantModifications(GameObject building, ModelVariantDefinition variant, System.Random rng)
        {
            // Apply scale variation
            if (variant.ScaleVariation != Vector3.one)
            {
                building.transform.localScale = Vector3.Scale(building.transform.localScale, variant.ScaleVariation);
            }
            
            // Apply mesh modifications
            foreach (var mod in variant.MeshModifications)
            {
                ApplyMeshModification(building, mod, rng);
            }
        }
        
        private void ApplyMeshModification(GameObject building, MeshModification mod, System.Random rng)
        {
            switch (mod.Type)
            {
                case ModificationType.RoofStyle:
                    ApplyRoofStyle(building, mod.Style);
                    break;
                    
                case ModificationType.WallTexture:
                    ApplyWallTexture(building, mod.Style);
                    break;
                    
                case ModificationType.WindowStyle:
                    ApplyWindowStyle(building, mod.Style);
                    break;
                    
                case ModificationType.Addition:
                    AddBuildingAddition(building, mod.Style, rng);
                    break;
                    
                case ModificationType.Shape:
                    // Shape modifications would require actual mesh changes
                    // For now, we'll use scaling as a proxy
                    break;
            }
        }
        
        private void ApplyRoofStyle(GameObject building, string style)
        {
            var roofObjects = FindChildrenWithName(building.transform, "Roof");
            
            foreach (var roof in roofObjects)
            {
                var renderer = roof.GetComponent<Renderer>();
                if (renderer == null) continue;
                
                // Apply style-specific modifications
                switch (style)
                {
                    case "Thatch":
                        renderer.material.color = new Color(0.6f, 0.5f, 0.3f);
                        break;
                    case "Slate":
                        renderer.material.color = new Color(0.3f, 0.3f, 0.35f);
                        break;
                    case "Tile":
                        renderer.material.color = new Color(0.7f, 0.4f, 0.3f);
                        break;
                    case "Wooden":
                        renderer.material.color = new Color(0.5f, 0.4f, 0.3f);
                        break;
                    case "Crenellated":
                        // Add battlements
                        AddBattlements(roof);
                        break;
                }
            }
        }
        
        private void ApplyWallTexture(GameObject building, string style)
        {
            var wallObjects = FindChildrenWithName(building.transform, "Wall");
            if (wallObjects.Count == 0)
            {
                // Try body/base
                wallObjects = FindChildrenWithName(building.transform, "Body");
            }
            
            foreach (var wall in wallObjects)
            {
                var renderer = wall.GetComponent<Renderer>();
                if (renderer == null) continue;
                
                switch (style)
                {
                    case "Wattle":
                        renderer.material.color = new Color(0.6f, 0.55f, 0.4f);
                        break;
                    case "Plaster":
                        renderer.material.color = new Color(0.9f, 0.85f, 0.8f);
                        break;
                    case "Stone":
                        renderer.material.color = new Color(0.5f, 0.5f, 0.45f);
                        break;
                    case "Brick":
                        renderer.material.color = new Color(0.6f, 0.35f, 0.3f);
                        break;
                    case "Reinforced":
                        renderer.material.color = new Color(0.4f, 0.4f, 0.4f);
                        break;
                }
            }
        }
        
        private void ApplyWindowStyle(GameObject building, string style)
        {
            var windowObjects = FindChildrenWithName(building.transform, "Window");
            
            foreach (var window in windowObjects)
            {
                // Modify window scale/shape based on style
                switch (style)
                {
                    case "Small":
                        window.transform.localScale *= 0.7f;
                        break;
                    case "Tall":
                        window.transform.localScale = new Vector3(
                            window.transform.localScale.x * 0.8f,
                            window.transform.localScale.y * 1.3f,
                            window.transform.localScale.z
                        );
                        break;
                    case "Arched":
                        // Add arch decoration
                        AddWindowArch(window);
                        break;
                    case "Large":
                        window.transform.localScale *= 1.3f;
                        break;
                    case "StainedGlass":
                        var renderer = window.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.material.color = new Color(0.5f, 0.3f, 0.7f, 0.6f);
                            renderer.material.EnableKeyword("_EMISSION");
                            renderer.material.SetColor("_EmissionColor", new Color(0.3f, 0.2f, 0.4f) * 0.5f);
                        }
                        break;
                }
            }
        }
        
        private void AddBuildingAddition(GameObject building, string additionType, System.Random rng)
        {
            var bounds = CalculateBounds(building);
            
            switch (additionType)
            {
                case "Chimney":
                    AddChimney(building, bounds);
                    break;
                case "Towers":
                    AddSmallTowers(building, bounds, rng);
                    break;
                case "TrainingYard":
                    // Just add ground marking
                    AddGroundMarking(building, bounds, new Color(0.5f, 0.4f, 0.3f));
                    break;
                case "Waterwheel":
                    AddWaterwheel(building, bounds);
                    break;
                case "Smokestacks":
                    AddSmokestacks(building, bounds, rng);
                    break;
                case "Columns":
                    AddColumns(building, bounds);
                    break;
                case "Spires":
                    AddSpires(building, bounds, rng);
                    break;
                case "FloatingCrystals":
                    AddFloatingCrystals(building, bounds, rng);
                    break;
            }
        }
        
        private void AddBattlements(Transform roof)
        {
            // Add simple battlements
            var bounds = roof.GetComponent<Renderer>()?.bounds ?? new Bounds(roof.position, Vector3.one);
            
            int merlon_count = 4;
            for (int i = 0; i < merlon_count; i++)
            {
                var merlon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                merlon.name = "Battlement";
                merlon.transform.SetParent(roof);
                
                float angle = (i / (float)merlon_count) * 360f * Mathf.Deg2Rad;
                float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.9f;
                
                merlon.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * radius,
                    bounds.extents.y,
                    Mathf.Sin(angle) * radius
                );
                merlon.transform.localScale = new Vector3(0.3f, 0.4f, 0.2f);
                
                Destroy(merlon.GetComponent<Collider>());
                merlon.GetComponent<Renderer>().material.color = new Color(0.4f, 0.4f, 0.4f);
            }
        }
        
        private void AddWindowArch(Transform window)
        {
            var arch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arch.name = "WindowArch";
            arch.transform.SetParent(window);
            arch.transform.localPosition = new Vector3(0, 0.5f, 0);
            arch.transform.localRotation = Quaternion.Euler(90, 0, 0);
            arch.transform.localScale = new Vector3(1.2f, 0.1f, 0.3f);
            
            Destroy(arch.GetComponent<Collider>());
        }
        
        private void AddChimney(GameObject building, Bounds bounds)
        {
            var chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimney.name = "Chimney";
            chimney.transform.SetParent(building.transform);
            chimney.transform.localPosition = new Vector3(
                UnityEngine.Random.Range(-bounds.extents.x * 0.3f, bounds.extents.x * 0.3f),
                bounds.max.y - building.transform.position.y + 0.5f,
                0
            );
            chimney.transform.localScale = new Vector3(0.4f, 1f, 0.4f);
            
            Destroy(chimney.GetComponent<Collider>());
            chimney.GetComponent<Renderer>().material.color = new Color(0.5f, 0.3f, 0.25f);
        }
        
        private void AddSmallTowers(GameObject building, Bounds bounds, System.Random rng)
        {
            int towerCount = 2 + rng.Next(0, 3);
            for (int i = 0; i < towerCount; i++)
            {
                var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tower.name = $"SmallTower_{i}";
                tower.transform.SetParent(building.transform);
                
                float angle = (i / (float)towerCount) * 360f * Mathf.Deg2Rad + (float)rng.NextDouble() * 0.5f;
                float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.8f;
                
                tower.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * radius,
                    (bounds.size.y / 2) + 0.5f,
                    Mathf.Sin(angle) * radius
                );
                tower.transform.localScale = new Vector3(0.8f, bounds.size.y / 2 + 1f, 0.8f);
                
                Destroy(tower.GetComponent<Collider>());
                tower.GetComponent<Renderer>().material.color = new Color(0.45f, 0.45f, 0.4f);
            }
        }
        
        private void AddGroundMarking(GameObject building, Bounds bounds, Color color)
        {
            var marking = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marking.name = "GroundMarking";
            marking.transform.SetParent(building.transform);
            marking.transform.localPosition = new Vector3(bounds.extents.x + 2, 0.01f, 0);
            marking.transform.localRotation = Quaternion.Euler(90, 0, 0);
            marking.transform.localScale = new Vector3(3, 3, 1);
            
            Destroy(marking.GetComponent<Collider>());
            marking.GetComponent<Renderer>().material.color = color;
        }
        
        private void AddWaterwheel(GameObject building, Bounds bounds)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Waterwheel";
            wheel.transform.SetParent(building.transform);
            wheel.transform.localPosition = new Vector3(-bounds.extents.x - 1f, 1f, 0);
            wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            wheel.transform.localScale = new Vector3(2f, 0.3f, 2f);
            
            Destroy(wheel.GetComponent<Collider>());
            wheel.GetComponent<Renderer>().material.color = new Color(0.4f, 0.3f, 0.2f);
            
            // Add rotation animation component
            var rotator = wheel.AddComponent<SimpleRotator>();
            rotator.RotationSpeed = new Vector3(0, 0, 30);
        }
        
        private void AddSmokestacks(GameObject building, Bounds bounds, System.Random rng)
        {
            int count = 1 + rng.Next(0, 2);
            for (int i = 0; i < count; i++)
            {
                var stack = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stack.name = $"Smokestack_{i}";
                stack.transform.SetParent(building.transform);
                stack.transform.localPosition = new Vector3(
                    (float)rng.NextDouble() * bounds.extents.x - bounds.extents.x / 2,
                    bounds.max.y - building.transform.position.y + 1f,
                    (float)rng.NextDouble() * bounds.extents.z - bounds.extents.z / 2
                );
                stack.transform.localScale = new Vector3(0.4f, 2f, 0.4f);
                
                Destroy(stack.GetComponent<Collider>());
                stack.GetComponent<Renderer>().material.color = new Color(0.35f, 0.3f, 0.3f);
            }
        }
        
        private void AddColumns(GameObject building, Bounds bounds)
        {
            int columnCount = 4;
            for (int i = 0; i < columnCount; i++)
            {
                var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                column.name = $"Column_{i}";
                column.transform.SetParent(building.transform);
                
                float x = Mathf.Lerp(-bounds.extents.x + 0.3f, bounds.extents.x - 0.3f, i / (float)(columnCount - 1));
                
                column.transform.localPosition = new Vector3(x, bounds.extents.y, bounds.extents.z + 0.5f);
                column.transform.localScale = new Vector3(0.4f, bounds.extents.y, 0.4f);
                
                Destroy(column.GetComponent<Collider>());
                column.GetComponent<Renderer>().material.color = new Color(0.85f, 0.8f, 0.75f);
            }
        }
        
        private void AddSpires(GameObject building, Bounds bounds, System.Random rng)
        {
            int spireCount = 2 + rng.Next(0, 3);
            for (int i = 0; i < spireCount; i++)
            {
                var spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                spire.name = $"Spire_{i}";
                spire.transform.SetParent(building.transform);
                
                float x = (float)rng.NextDouble() * bounds.size.x - bounds.extents.x;
                float z = (float)rng.NextDouble() * bounds.size.z - bounds.extents.z;
                float height = 1f + (float)rng.NextDouble() * 2f;
                
                spire.transform.localPosition = new Vector3(x, bounds.max.y - building.transform.position.y + height/2, z);
                spire.transform.localScale = new Vector3(0.2f, height, 0.2f);
                
                Destroy(spire.GetComponent<Collider>());
                spire.GetComponent<Renderer>().material.color = new Color(0.4f, 0.4f, 0.45f);
            }
        }
        
        private void AddFloatingCrystals(GameObject building, Bounds bounds, System.Random rng)
        {
            int crystalCount = 3 + rng.Next(0, 4);
            for (int i = 0; i < crystalCount; i++)
            {
                var crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crystal.name = $"Crystal_{i}";
                crystal.transform.SetParent(building.transform);
                
                float angle = (float)rng.NextDouble() * 360f;
                float radius = (float)rng.NextDouble() * bounds.extents.magnitude + 1f;
                float height = bounds.size.y + 1f + (float)rng.NextDouble() * 2f;
                
                crystal.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * radius,
                    height,
                    Mathf.Sin(angle) * radius
                );
                crystal.transform.localRotation = Quaternion.Euler(45, (float)rng.NextDouble() * 360, 45);
                crystal.transform.localScale = Vector3.one * (0.2f + (float)rng.NextDouble() * 0.3f);
                
                Destroy(crystal.GetComponent<Collider>());
                
                var mat = crystal.GetComponent<Renderer>().material;
                mat.color = new Color(0.6f, 0.5f, 1f, 0.7f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.4f, 0.3f, 0.8f) * 2f);
                
                // Add floating animation
                var floater = crystal.AddComponent<SimpleFloater>();
                floater.FloatSpeed = 1f + (float)rng.NextDouble();
                floater.FloatAmount = 0.2f + (float)rng.NextDouble() * 0.3f;
            }
        }
        
        #endregion
        
        #region Color Variations
        
        private void ApplyColorVariations(GameObject building, string factionId, System.Random rng)
        {
            FactionColorPalette palette = null;
            
            if (enableFactionColors && !string.IsNullOrEmpty(factionId))
            {
                _factionPalettes.TryGetValue(factionId, out palette);
            }
            
            if (palette == null)
            {
                _factionPalettes.TryGetValue("Neutral", out palette);
            }
            
            var renderers = building.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    var mat = renderer.materials[i];
                    
                    // Apply subtle color variation
                    Color baseColor = mat.color;
                    float variation = colorVariationAmount * ((float)rng.NextDouble() * 2 - 1);
                    
                    baseColor = new Color(
                        Mathf.Clamp01(baseColor.r + variation),
                        Mathf.Clamp01(baseColor.g + variation),
                        Mathf.Clamp01(baseColor.b + variation),
                        baseColor.a
                    );
                    
                    // Apply faction colors to trim/accents
                    if (palette != null && ShouldApplyFactionColor(renderer.gameObject.name))
                    {
                        baseColor = Color.Lerp(baseColor, palette.Accent, 0.3f);
                    }
                    
                    mat.color = baseColor;
                }
            }
        }
        
        private bool ShouldApplyFactionColor(string objectName)
        {
            string lower = objectName.ToLower();
            return lower.Contains("trim") || 
                   lower.Contains("banner") || 
                   lower.Contains("flag") ||
                   lower.Contains("shield") ||
                   lower.Contains("accent");
        }
        
        #endregion
        
        #region Detail Attachments
        
        private void AddDetailAttachments(GameObject building, BuildingCategory category, string factionId, System.Random rng)
        {
            var applied = _appliedVariants.ContainsKey(building.name) ? 
                _appliedVariants[building.name] : new AppliedVariant { Attachments = new List<GameObject>() };
            
            var bounds = CalculateBounds(building);
            int attachmentCount = 0;
            
            // Try to add from each category
            foreach (var kvp in _detailAttachments)
            {
                if (attachmentCount >= maxAttachmentsPerBuilding) break;
                
                foreach (var attachment in kvp.Value)
                {
                    if (attachmentCount >= maxAttachmentsPerBuilding) break;
                    if (rng.NextDouble() > attachmentProbability) continue;
                    
                    var attachObj = CreateAttachment(building, attachment, bounds, factionId, rng);
                    if (attachObj != null)
                    {
                        applied.Attachments.Add(attachObj);
                        attachmentCount++;
                    }
                }
            }
        }
        
        private GameObject CreateAttachment(GameObject building, DetailAttachment attachment, 
            Bounds bounds, string factionId, System.Random rng)
        {
            // Create simple primitive for attachment
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = attachment.Name;
            obj.transform.SetParent(building.transform);
            
            // Calculate position based on placement type
            Vector3 position = CalculateAttachmentPosition(attachment.PlacementType, bounds, rng);
            position += attachment.Offset;
            obj.transform.localPosition = position;
            
            // Apply scale
            Vector3 scale = attachment.Scale;
            if (attachment.RandomScale > 0)
            {
                float scaleMod = 1f + (float)(rng.NextDouble() * 2 - 1) * attachment.RandomScale;
                scale *= scaleMod;
            }
            obj.transform.localScale = scale;
            
            // Apply rotation
            if (attachment.RandomRotation)
            {
                obj.transform.localRotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0);
            }
            
            Destroy(obj.GetComponent<Collider>());
            
            // Apply material
            var mat = obj.GetComponent<Renderer>().material;
            if (attachment.UsesFactionColor && !string.IsNullOrEmpty(factionId) && 
                _factionPalettes.TryGetValue(factionId, out var palette))
            {
                mat.color = palette.Primary;
            }
            else
            {
                mat.color = new Color(0.5f, 0.45f, 0.4f); // Default brown
            }
            
            // Add light if specified
            if (attachment.HasLight)
            {
                var light = obj.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = attachment.LightColor;
                light.intensity = attachment.LightIntensity;
                light.range = 5f;
            }
            
            return obj;
        }
        
        private Vector3 CalculateAttachmentPosition(PlacementType placementType, Bounds bounds, System.Random rng)
        {
            switch (placementType)
            {
                case PlacementType.WindowSill:
                    return new Vector3(
                        (float)rng.NextDouble() * bounds.size.x - bounds.extents.x,
                        bounds.extents.y,
                        bounds.extents.z
                    );
                    
                case PlacementType.Entrance:
                    return new Vector3(0, 0, bounds.extents.z);
                    
                case PlacementType.Wall:
                    return new Vector3(
                        bounds.extents.x,
                        bounds.extents.y,
                        (float)rng.NextDouble() * bounds.size.z - bounds.extents.z
                    );
                    
                case PlacementType.RoofPeak:
                    return new Vector3(0, bounds.size.y, 0);
                    
                case PlacementType.Roof:
                    return new Vector3(
                        (float)rng.NextDouble() * bounds.extents.x - bounds.extents.x / 2,
                        bounds.size.y * 0.8f,
                        (float)rng.NextDouble() * bounds.extents.z - bounds.extents.z / 2
                    );
                    
                case PlacementType.RoofEdge:
                    return new Vector3(bounds.extents.x, bounds.size.y * 0.7f, 0);
                    
                case PlacementType.GroundNearWall:
                    return new Vector3(
                        bounds.extents.x * 0.8f,
                        0,
                        (float)rng.NextDouble() * bounds.size.z - bounds.extents.z
                    );
                    
                case PlacementType.GroundNearEntrance:
                    return new Vector3(
                        (float)rng.NextDouble() * 2 - 1,
                        0,
                        bounds.extents.z + 1
                    );
                    
                case PlacementType.GroundCorner:
                    return new Vector3(bounds.extents.x, 0, bounds.extents.z);
                    
                default:
                    return Vector3.zero;
            }
        }
        
        #endregion
        
        #region Helpers
        
        private Bounds CalculateBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(obj.transform.position, Vector3.one);
            }
            
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            
            // Convert to local
            var center = obj.transform.InverseTransformPoint(bounds.center);
            return new Bounds(center, bounds.size);
        }
        
        private List<Transform> FindChildrenWithName(Transform parent, string nameContains)
        {
            var result = new List<Transform>();
            FindChildrenRecursive(parent, nameContains, result);
            return result;
        }
        
        private void FindChildrenRecursive(Transform parent, string nameContains, List<Transform> result)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Contains(nameContains))
                {
                    result.Add(child);
                }
                FindChildrenRecursive(child, nameContains, result);
            }
        }
        
        #endregion
    }
    
    #region Helper Components
    
    public class SimpleRotator : MonoBehaviour
    {
        public Vector3 RotationSpeed = new Vector3(0, 30, 0);
        
        private void Update()
        {
            transform.Rotate(RotationSpeed * Time.deltaTime);
        }
    }
    
    public class SimpleFloater : MonoBehaviour
    {
        public float FloatSpeed = 1f;
        public float FloatAmount = 0.3f;
        
        private Vector3 _startPos;
        private float _offset;
        
        private void Start()
        {
            _startPos = transform.localPosition;
            _offset = UnityEngine.Random.value * Mathf.PI * 2;
        }
        
        private void Update()
        {
            float y = Mathf.Sin(Time.time * FloatSpeed + _offset) * FloatAmount;
            transform.localPosition = _startPos + Vector3.up * y;
        }
    }
    
    #endregion
    
    #region Data Types
    
    public enum BuildingCategory
    {
        Residential,
        Military,
        Production,
        Defense,
        Magic,
        Storage,
        Special
    }
    
    public enum ModificationType
    {
        RoofStyle,
        WallTexture,
        WindowStyle,
        Addition,
        Shape
    }
    
    public enum DetailCategory
    {
        ExteriorDecoration,
        RoofDecoration,
        GroundDecoration
    }
    
    public enum PlacementType
    {
        WindowSill,
        Entrance,
        Wall,
        RoofPeak,
        Roof,
        RoofEdge,
        GroundNearWall,
        GroundNearEntrance,
        GroundCorner
    }
    
    public class ModelVariantDefinition
    {
        public string Name;
        public MeshModification[] MeshModifications;
        public Vector3 ScaleVariation = Vector3.one;
    }
    
    public class MeshModification
    {
        public ModificationType Type;
        public string Style;
    }
    
    public class DetailAttachment
    {
        public string Name;
        public PlacementType PlacementType;
        public Vector3 Scale = Vector3.one;
        public Vector3 Offset = Vector3.zero;
        public bool RandomRotation;
        public float RandomScale;
        public bool UsesFactionColor;
        public bool HasLight;
        public Color LightColor = Color.white;
        public float LightIntensity = 1f;
        public bool HasParticles;
        public string ParticleType;
        public bool HasAnimation;
        public string AnimationType;
    }
    
    public class FactionColorPalette
    {
        public Color Primary;
        public Color Secondary;
        public Color Accent;
        public Color Trim;
    }
    
    public class AppliedVariant
    {
        public string BuildingId;
        public GameObject Building;
        public ModelVariantDefinition Variant;
        public string FactionId;
        public List<GameObject> Attachments = new List<GameObject>();
    }
    
    #endregion
}
