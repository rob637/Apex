using UnityEngine;
using System;
using System.Collections.Generic;

namespace ApexCitadels.PC.Buildings
{
    /// <summary>
    /// Procedural Building Generator for PC client.
    /// Creates varied building models from base templates using procedural variation.
    /// Supports multiple architectural styles (medieval, fantasy, steampunk, etc.)
    /// and generates unique buildings through component mixing.
    /// </summary>
    public class ProceduralBuildingGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private bool enableProceduralGeneration = true;
        [SerializeField] private int randomSeed = 0;
        [SerializeField] private bool useLocationBasedSeed = true;
        
        [Header("Architectural Styles")]
        [SerializeField] private ArchitecturalStyle defaultStyle = ArchitecturalStyle.MedievalFantasy;
        [SerializeField] private List<StylePalette> stylePalettes = new List<StylePalette>();
        
        [Header("Component Libraries")]
        [SerializeField] private BuildingComponentLibrary componentLibrary;
        
        [Header("Size Ranges")]
        [SerializeField] private Vector2 baseSizeRange = new Vector2(3f, 8f);
        [SerializeField] private Vector2 heightRange = new Vector2(4f, 20f);
        [SerializeField] private Vector2Int floorCountRange = new Vector2Int(1, 5);
        [SerializeField] private float floorHeight = 3.5f;
        
        [Header("Variation")]
        [SerializeField, Range(0f, 1f)] private float variationAmount = 0.3f;
        [SerializeField] private bool allowAsymmetry = true;
        [SerializeField, Range(0f, 1f)] private float decorationDensity = 0.5f;
        
        // Singleton
        private static ProceduralBuildingGenerator _instance;
        public static ProceduralBuildingGenerator Instance => _instance;
        
        // Runtime
        private Dictionary<string, GeneratedBuilding> _generatedBuildings = new Dictionary<string, GeneratedBuilding>();
        private System.Random _random;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            _random = new System.Random(randomSeed);
            InitializeDefaultPalettes();
        }
        
        private void InitializeDefaultPalettes()
        {
            if (stylePalettes.Count == 0)
            {
                // Medieval Fantasy palette
                stylePalettes.Add(new StylePalette
                {
                    Style = ArchitecturalStyle.MedievalFantasy,
                    WallColors = new Color[] { 
                        new Color(0.7f, 0.65f, 0.55f), // Tan stone
                        new Color(0.5f, 0.45f, 0.4f),  // Gray stone
                        new Color(0.35f, 0.3f, 0.25f)  // Dark stone
                    },
                    RoofColors = new Color[] {
                        new Color(0.4f, 0.25f, 0.2f),  // Dark red
                        new Color(0.3f, 0.3f, 0.35f),  // Slate
                        new Color(0.5f, 0.4f, 0.3f)    // Brown
                    },
                    TrimColors = new Color[] {
                        new Color(0.4f, 0.3f, 0.2f),   // Dark wood
                        new Color(0.6f, 0.5f, 0.4f)    // Light wood
                    },
                    WindowStyle = WindowStyle.Gothic,
                    RoofStyle = RoofStyle.Peaked,
                    HasTimberFraming = true
                });
                
                // High Fantasy palette
                stylePalettes.Add(new StylePalette
                {
                    Style = ArchitecturalStyle.HighFantasy,
                    WallColors = new Color[] {
                        new Color(0.85f, 0.85f, 0.9f),  // White marble
                        new Color(0.7f, 0.8f, 0.85f),   // Ice blue
                        new Color(0.9f, 0.85f, 0.7f)    // Gold stone
                    },
                    RoofColors = new Color[] {
                        new Color(0.3f, 0.5f, 0.7f),    // Blue
                        new Color(0.6f, 0.4f, 0.6f),    // Purple
                        new Color(0.8f, 0.7f, 0.5f)     // Gold
                    },
                    TrimColors = new Color[] {
                        new Color(0.9f, 0.8f, 0.5f),    // Gold
                        new Color(0.7f, 0.7f, 0.8f)     // Silver
                    },
                    WindowStyle = WindowStyle.Arched,
                    RoofStyle = RoofStyle.Spires,
                    HasTimberFraming = false
                });
                
                // Dark Gothic palette
                stylePalettes.Add(new StylePalette
                {
                    Style = ArchitecturalStyle.DarkGothic,
                    WallColors = new Color[] {
                        new Color(0.2f, 0.2f, 0.22f),   // Dark gray
                        new Color(0.15f, 0.15f, 0.18f), // Near black
                        new Color(0.3f, 0.25f, 0.25f)   // Dark red stone
                    },
                    RoofColors = new Color[] {
                        new Color(0.15f, 0.15f, 0.15f), // Black
                        new Color(0.25f, 0.2f, 0.2f),   // Dark red
                        new Color(0.2f, 0.2f, 0.25f)    // Dark purple
                    },
                    TrimColors = new Color[] {
                        new Color(0.4f, 0.4f, 0.45f),   // Tarnished silver
                        new Color(0.3f, 0.2f, 0.2f)     // Rust
                    },
                    WindowStyle = WindowStyle.Gothic,
                    RoofStyle = RoofStyle.Peaked,
                    HasTimberFraming = false
                });
                
                // Steampunk palette
                stylePalettes.Add(new StylePalette
                {
                    Style = ArchitecturalStyle.Steampunk,
                    WallColors = new Color[] {
                        new Color(0.6f, 0.5f, 0.4f),    // Rust brown
                        new Color(0.5f, 0.45f, 0.4f),   // Industrial gray
                        new Color(0.7f, 0.6f, 0.5f)     // Brass
                    },
                    RoofColors = new Color[] {
                        new Color(0.45f, 0.35f, 0.25f), // Copper
                        new Color(0.55f, 0.5f, 0.4f),   // Brass
                        new Color(0.3f, 0.3f, 0.3f)     // Metal
                    },
                    TrimColors = new Color[] {
                        new Color(0.7f, 0.5f, 0.3f),    // Copper
                        new Color(0.8f, 0.7f, 0.4f)     // Brass
                    },
                    WindowStyle = WindowStyle.Round,
                    RoofStyle = RoofStyle.Domed,
                    HasTimberFraming = false
                });
            }
        }
        
        #region Building Generation
        
        /// <summary>
        /// Generate a building at the specified position
        /// </summary>
        public GameObject GenerateBuilding(Vector3 position, BuildingType type, 
            ArchitecturalStyle? styleOverride = null, int? seedOverride = null)
        {
            // Determine seed
            int seed = seedOverride ?? (useLocationBasedSeed ? 
                GetLocationSeed(position) : _random.Next());
            
            var buildRandom = new System.Random(seed);
            
            // Get style palette
            var style = styleOverride ?? defaultStyle;
            var palette = GetPalette(style);
            
            // Get building parameters from type
            var parameters = GetParametersForType(type);
            
            // Create building root
            var building = new GameObject($"Building_{type}_{seed}");
            building.transform.position = position;
            
            // Generate components
            var root = GenerateBuildingStructure(building.transform, parameters, palette, buildRandom);
            
            // Add decorations
            if (decorationDensity > 0)
            {
                AddDecorations(root, parameters, palette, buildRandom);
            }
            
            // Cache generated building
            var generatedBuilding = new GeneratedBuilding
            {
                Id = $"{type}_{seed}",
                GameObject = building,
                Type = type,
                Style = style,
                Seed = seed,
                Position = position
            };
            _generatedBuildings[generatedBuilding.Id] = generatedBuilding;
            
            return building;
        }
        
        /// <summary>
        /// Generate a building variant (same building with different decorations)
        /// </summary>
        public GameObject GenerateVariant(string baseId, Vector3 position)
        {
            if (!_generatedBuildings.TryGetValue(baseId, out var baseBuilding))
            {
                Debug.LogWarning($"[ProceduralBuilding] Base building not found: {baseId}");
                return null;
            }
            
            // Generate with offset seed
            return GenerateBuilding(position, baseBuilding.Type, baseBuilding.Style, 
                baseBuilding.Seed + _random.Next(1, 1000));
        }
        
        private int GetLocationSeed(Vector3 position)
        {
            // Create deterministic seed from position
            return Mathf.RoundToInt(position.x * 1000) ^ 
                   Mathf.RoundToInt(position.z * 1000) ^ 
                   randomSeed;
        }
        
        private StylePalette GetPalette(ArchitecturalStyle style)
        {
            foreach (var palette in stylePalettes)
            {
                if (palette.Style == style)
                    return palette;
            }
            return stylePalettes[0]; // Default to first
        }
        
        private BuildingParameters GetParametersForType(BuildingType type)
        {
            return type switch
            {
                BuildingType.House => new BuildingParameters
                {
                    BaseSize = new Vector2(5, 6),
                    FloorCount = 2,
                    HasChimney = true,
                    HasBalcony = false,
                    HasTower = false
                },
                BuildingType.Tower => new BuildingParameters
                {
                    BaseSize = new Vector2(4, 4),
                    FloorCount = 4,
                    HasChimney = false,
                    HasBalcony = true,
                    HasTower = true,
                    TowerStyle = TowerStyle.Round
                },
                BuildingType.Castle => new BuildingParameters
                {
                    BaseSize = new Vector2(15, 20),
                    FloorCount = 3,
                    HasChimney = true,
                    HasBalcony = true,
                    HasTower = true,
                    TowerCount = 4,
                    TowerStyle = TowerStyle.Square,
                    HasCurtainWall = true
                },
                BuildingType.Temple => new BuildingParameters
                {
                    BaseSize = new Vector2(10, 15),
                    FloorCount = 2,
                    HasChimney = false,
                    HasBalcony = false,
                    HasTower = true,
                    TowerStyle = TowerStyle.Spire,
                    HasDome = true
                },
                BuildingType.Barracks => new BuildingParameters
                {
                    BaseSize = new Vector2(12, 8),
                    FloorCount = 2,
                    HasChimney = true,
                    HasBalcony = false,
                    HasTower = false,
                    HasTrainingYard = true
                },
                BuildingType.Workshop => new BuildingParameters
                {
                    BaseSize = new Vector2(8, 10),
                    FloorCount = 2,
                    HasChimney = true,
                    HasBalcony = false,
                    HasTower = false,
                    HasSmokestacks = true
                },
                BuildingType.Tavern => new BuildingParameters
                {
                    BaseSize = new Vector2(8, 10),
                    FloorCount = 2,
                    HasChimney = true,
                    HasBalcony = true,
                    HasTower = false,
                    HasSignage = true
                },
                BuildingType.Wall => new BuildingParameters
                {
                    BaseSize = new Vector2(2, 10),
                    FloorCount = 1,
                    WallThickness = 2f,
                    HasBattlements = true
                },
                BuildingType.Gate => new BuildingParameters
                {
                    BaseSize = new Vector2(6, 8),
                    FloorCount = 2,
                    HasTower = true,
                    TowerCount = 2,
                    TowerStyle = TowerStyle.Square,
                    HasPortcullis = true
                },
                _ => new BuildingParameters
                {
                    BaseSize = new Vector2(5, 5),
                    FloorCount = 1
                }
            };
        }
        
        #endregion
        
        #region Structure Generation
        
        private Transform GenerateBuildingStructure(Transform parent, BuildingParameters parameters, 
            StylePalette palette, System.Random random)
        {
            // Apply variation to size
            float sizeVariation = 1f + (float)(random.NextDouble() - 0.5) * variationAmount * 2f;
            Vector2 actualSize = parameters.BaseSize * sizeVariation;
            
            // Vary floor count slightly
            int floorVariation = random.Next(-1, 2);
            int actualFloors = Mathf.Max(1, parameters.FloorCount + floorVariation);
            
            // Create foundation
            var foundation = CreateFoundation(parent, actualSize, palette, random);
            
            // Create floors
            Transform previousFloor = foundation;
            for (int i = 0; i < actualFloors; i++)
            {
                bool isTopFloor = (i == actualFloors - 1);
                previousFloor = CreateFloor(parent, previousFloor, actualSize, i, isTopFloor, 
                    parameters, palette, random);
            }
            
            // Create roof
            CreateRoof(parent, previousFloor, actualSize, parameters, palette, random);
            
            // Add towers if specified
            if (parameters.HasTower)
            {
                CreateTowers(parent, actualSize, actualFloors, parameters, palette, random);
            }
            
            // Add chimney
            if (parameters.HasChimney)
            {
                CreateChimney(parent, actualSize, palette, random);
            }
            
            return parent;
        }
        
        private Transform CreateFoundation(Transform parent, Vector2 size, StylePalette palette, System.Random random)
        {
            var foundation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foundation.name = "Foundation";
            foundation.transform.SetParent(parent);
            foundation.transform.localPosition = new Vector3(0, 0.25f, 0);
            foundation.transform.localScale = new Vector3(size.x + 0.4f, 0.5f, size.y + 0.4f);
            
            // Apply material
            ApplyMaterial(foundation, palette.WallColors[random.Next(palette.WallColors.Length)], true);
            
            return foundation.transform;
        }
        
        private Transform CreateFloor(Transform parent, Transform below, Vector2 size, int floorIndex,
            bool isTopFloor, BuildingParameters parameters, StylePalette palette, System.Random random)
        {
            float yPos = (floorIndex + 1) * floorHeight;
            
            // Slight inset for upper floors
            float inset = floorIndex * 0.1f;
            Vector2 floorSize = size - new Vector2(inset, inset);
            
            var floor = new GameObject($"Floor_{floorIndex}");
            floor.transform.SetParent(parent);
            floor.transform.localPosition = new Vector3(0, yPos - floorHeight / 2f, 0);
            
            // Create walls
            CreateWalls(floor.transform, floorSize, floorHeight, floorIndex, palette, random);
            
            // Create floor plate
            var floorPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorPlate.name = "FloorPlate";
            floorPlate.transform.SetParent(floor.transform);
            floorPlate.transform.localPosition = new Vector3(0, -floorHeight / 2f + 0.1f, 0);
            floorPlate.transform.localScale = new Vector3(floorSize.x - 0.1f, 0.2f, floorSize.y - 0.1f);
            ApplyMaterial(floorPlate, palette.TrimColors[random.Next(palette.TrimColors.Length)]);
            
            // Add windows
            CreateWindows(floor.transform, floorSize, floorHeight, palette, random);
            
            // Add balconies
            if (parameters.HasBalcony && floorIndex > 0 && random.NextDouble() > 0.5)
            {
                CreateBalcony(floor.transform, floorSize, palette, random);
            }
            
            // Add timber framing for medieval style
            if (palette.HasTimberFraming && floorIndex > 0)
            {
                CreateTimberFraming(floor.transform, floorSize, floorHeight, palette, random);
            }
            
            return floor.transform;
        }
        
        private void CreateWalls(Transform parent, Vector2 size, float height, int floorIndex,
            StylePalette palette, System.Random random)
        {
            Color wallColor = palette.WallColors[random.Next(palette.WallColors.Length)];
            
            // Front wall
            CreateWall(parent, new Vector3(0, 0, size.y / 2f), 
                new Vector3(size.x, height, 0.3f), wallColor);
            
            // Back wall
            CreateWall(parent, new Vector3(0, 0, -size.y / 2f), 
                new Vector3(size.x, height, 0.3f), wallColor);
            
            // Left wall
            CreateWall(parent, new Vector3(-size.x / 2f, 0, 0), 
                new Vector3(0.3f, height, size.y), wallColor);
            
            // Right wall
            CreateWall(parent, new Vector3(size.x / 2f, 0, 0), 
                new Vector3(0.3f, height, size.y), wallColor);
        }
        
        private void CreateWall(Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            ApplyMaterial(wall, color);
        }
        
        private void CreateWindows(Transform parent, Vector2 size, float height, StylePalette palette, 
            System.Random random)
        {
            // Calculate window spacing
            float windowSpacing = 2.5f;
            int windowsX = Mathf.Max(1, Mathf.FloorToInt(size.x / windowSpacing));
            int windowsZ = Mathf.Max(1, Mathf.FloorToInt(size.y / windowSpacing));
            
            // Front windows
            float startX = -(windowsX - 1) * windowSpacing / 2f;
            for (int i = 0; i < windowsX; i++)
            {
                if (random.NextDouble() > 0.2) // 80% chance of window
                {
                    Vector3 pos = new Vector3(startX + i * windowSpacing, 0, size.y / 2f + 0.1f);
                    CreateWindow(parent, pos, palette.WindowStyle, palette.TrimColors[0], random);
                }
            }
            
            // Side windows
            float startZ = -(windowsZ - 1) * windowSpacing / 2f;
            for (int i = 0; i < windowsZ; i++)
            {
                if (random.NextDouble() > 0.3)
                {
                    Vector3 posL = new Vector3(-size.x / 2f - 0.1f, 0, startZ + i * windowSpacing);
                    Vector3 posR = new Vector3(size.x / 2f + 0.1f, 0, startZ + i * windowSpacing);
                    CreateWindow(parent, posL, palette.WindowStyle, palette.TrimColors[0], random, 90);
                    CreateWindow(parent, posR, palette.WindowStyle, palette.TrimColors[0], random, -90);
                }
            }
        }
        
        private void CreateWindow(Transform parent, Vector3 position, WindowStyle style, 
            Color trimColor, System.Random random, float rotationY = 0)
        {
            var windowObj = new GameObject("Window");
            windowObj.transform.SetParent(parent);
            windowObj.transform.localPosition = position;
            windowObj.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
            
            // Window frame
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(windowObj.transform);
            frame.transform.localPosition = Vector3.zero;
            
            // Size based on style
            Vector3 frameSize = style switch
            {
                WindowStyle.Gothic => new Vector3(1f, 1.8f, 0.15f),
                WindowStyle.Arched => new Vector3(1.2f, 1.5f, 0.15f),
                WindowStyle.Round => new Vector3(1f, 1f, 0.15f),
                _ => new Vector3(1f, 1.2f, 0.15f)
            };
            frame.transform.localScale = frameSize;
            ApplyMaterial(frame, trimColor);
            
            // Glass
            var glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Glass";
            glass.transform.SetParent(windowObj.transform);
            glass.transform.localPosition = new Vector3(0, 0, 0.02f);
            glass.transform.localScale = frameSize * 0.8f;
            
            var glassMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            glassMat.SetFloat("_Surface", 1); // Transparent
            glassMat.SetFloat("_Blend", 0);
            glassMat.color = new Color(0.5f, 0.6f, 0.8f, 0.5f);
            glass.GetComponent<Renderer>().material = glassMat;
        }
        
        private void CreateRoof(Transform parent, Transform topFloor, Vector2 size,
            BuildingParameters parameters, StylePalette palette, System.Random random)
        {
            float roofY = topFloor.localPosition.y + floorHeight / 2f;
            Color roofColor = palette.RoofColors[random.Next(palette.RoofColors.Length)];
            
            switch (palette.RoofStyle)
            {
                case RoofStyle.Peaked:
                    CreatePeakedRoof(parent, roofY, size, roofColor, random);
                    break;
                    
                case RoofStyle.Flat:
                    CreateFlatRoof(parent, roofY, size, roofColor, palette);
                    break;
                    
                case RoofStyle.Domed:
                    CreateDomedRoof(parent, roofY, size, roofColor);
                    break;
                    
                case RoofStyle.Spires:
                    CreateSpireRoof(parent, roofY, size, roofColor, palette, random);
                    break;
            }
        }
        
        private void CreatePeakedRoof(Transform parent, float y, Vector2 size, Color color, System.Random random)
        {
            var roofObj = new GameObject("Roof");
            roofObj.transform.SetParent(parent);
            roofObj.transform.localPosition = new Vector3(0, y, 0);
            
            // Simple peaked roof using scaled cube (would use custom mesh in production)
            float roofHeight = Mathf.Min(size.x, size.y) * 0.4f;
            
            // Roof base
            var roofBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofBase.name = "RoofBase";
            roofBase.transform.SetParent(roofObj.transform);
            roofBase.transform.localPosition = new Vector3(0, roofHeight / 2f, 0);
            roofBase.transform.localScale = new Vector3(size.x + 0.5f, 0.3f, size.y + 0.5f);
            ApplyMaterial(roofBase, color);
            
            // Roof peak (use two angled cubes for simple peaked roof)
            float angle = 35f;
            float roofWidth = (size.x + 1f) / Mathf.Cos(angle * Mathf.Deg2Rad) / 2f;
            
            var roofLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofLeft.name = "RoofLeft";
            roofLeft.transform.SetParent(roofObj.transform);
            roofLeft.transform.localPosition = new Vector3(-size.x / 4f, roofHeight * 0.7f, 0);
            roofLeft.transform.localRotation = Quaternion.Euler(0, 0, angle);
            roofLeft.transform.localScale = new Vector3(roofWidth, 0.2f, size.y + 0.8f);
            ApplyMaterial(roofLeft, color);
            
            var roofRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofRight.name = "RoofRight";
            roofRight.transform.SetParent(roofObj.transform);
            roofRight.transform.localPosition = new Vector3(size.x / 4f, roofHeight * 0.7f, 0);
            roofRight.transform.localRotation = Quaternion.Euler(0, 0, -angle);
            roofRight.transform.localScale = new Vector3(roofWidth, 0.2f, size.y + 0.8f);
            ApplyMaterial(roofRight, color);
        }
        
        private void CreateFlatRoof(Transform parent, float y, Vector2 size, Color color, StylePalette palette)
        {
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "FlatRoof";
            roof.transform.SetParent(parent);
            roof.transform.localPosition = new Vector3(0, y + 0.2f, 0);
            roof.transform.localScale = new Vector3(size.x + 0.3f, 0.4f, size.y + 0.3f);
            ApplyMaterial(roof, color);
            
            // Add battlements
            CreateBattlements(parent, y + 0.4f, size, palette.TrimColors[0]);
        }
        
        private void CreateDomedRoof(Transform parent, float y, Vector2 size, Color color)
        {
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "Dome";
            dome.transform.SetParent(parent);
            dome.transform.localPosition = new Vector3(0, y + 1f, 0);
            float domeSize = Mathf.Min(size.x, size.y) * 0.8f;
            dome.transform.localScale = new Vector3(domeSize, domeSize * 0.6f, domeSize);
            ApplyMaterial(dome, color);
        }
        
        private void CreateSpireRoof(Transform parent, float y, Vector2 size, Color color, 
            StylePalette palette, System.Random random)
        {
            var spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spire.name = "Spire";
            spire.transform.SetParent(parent);
            float spireHeight = Mathf.Min(size.x, size.y) * 1.5f;
            spire.transform.localPosition = new Vector3(0, y + spireHeight / 2f, 0);
            spire.transform.localScale = new Vector3(Mathf.Min(size.x, size.y) * 0.3f, spireHeight, Mathf.Min(size.x, size.y) * 0.3f);
            ApplyMaterial(spire, color);
            
            // Add point at top
            var point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            point.name = "SpirePoint";
            point.transform.SetParent(parent);
            point.transform.localPosition = new Vector3(0, y + spireHeight + 0.3f, 0);
            point.transform.localScale = Vector3.one * 0.5f;
            ApplyMaterial(point, palette.TrimColors[random.Next(palette.TrimColors.Length)]);
        }
        
        private void CreateBattlements(Transform parent, float y, Vector2 size, Color color)
        {
            float spacing = 1.5f;
            float merlonSize = 0.4f;
            
            // Create merlons around perimeter
            int countX = Mathf.FloorToInt(size.x / spacing);
            int countZ = Mathf.FloorToInt(size.y / spacing);
            
            for (int i = 0; i < countX; i++)
            {
                float x = -size.x / 2f + i * spacing + spacing / 2f;
                CreateMerlon(parent, new Vector3(x, y, size.y / 2f), color);
                CreateMerlon(parent, new Vector3(x, y, -size.y / 2f), color);
            }
            
            for (int i = 0; i < countZ; i++)
            {
                float z = -size.y / 2f + i * spacing + spacing / 2f;
                CreateMerlon(parent, new Vector3(-size.x / 2f, y, z), color);
                CreateMerlon(parent, new Vector3(size.x / 2f, y, z), color);
            }
        }
        
        private void CreateMerlon(Transform parent, Vector3 position, Color color)
        {
            var merlon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            merlon.name = "Merlon";
            merlon.transform.SetParent(parent);
            merlon.transform.localPosition = position + Vector3.up * 0.4f;
            merlon.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
            ApplyMaterial(merlon, color);
        }
        
        private void CreateTowers(Transform parent, Vector2 baseSize, int floors,
            BuildingParameters parameters, StylePalette palette, System.Random random)
        {
            int towerCount = parameters.TowerCount > 0 ? parameters.TowerCount : 1;
            
            Vector3[] cornerPositions = new Vector3[]
            {
                new Vector3(-baseSize.x / 2f, 0, baseSize.y / 2f),
                new Vector3(baseSize.x / 2f, 0, baseSize.y / 2f),
                new Vector3(-baseSize.x / 2f, 0, -baseSize.y / 2f),
                new Vector3(baseSize.x / 2f, 0, -baseSize.y / 2f)
            };
            
            for (int i = 0; i < Mathf.Min(towerCount, 4); i++)
            {
                CreateTower(parent, cornerPositions[i], floors + 2, parameters.TowerStyle, 
                    palette, random);
            }
        }
        
        private void CreateTower(Transform parent, Vector3 position, int floors, TowerStyle style,
            StylePalette palette, System.Random random)
        {
            var tower = new GameObject("Tower");
            tower.transform.SetParent(parent);
            tower.transform.localPosition = position;
            
            float towerSize = 3f;
            float totalHeight = floors * floorHeight;
            Color wallColor = palette.WallColors[random.Next(palette.WallColors.Length)];
            
            // Tower body
            GameObject body;
            if (style == TowerStyle.Round)
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                body.transform.localScale = new Vector3(towerSize, totalHeight / 2f, towerSize);
            }
            else
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.transform.localScale = new Vector3(towerSize, totalHeight, towerSize);
            }
            body.name = "TowerBody";
            body.transform.SetParent(tower.transform);
            body.transform.localPosition = new Vector3(0, totalHeight / 2f, 0);
            ApplyMaterial(body, wallColor);
            
            // Tower roof
            Color roofColor = palette.RoofColors[random.Next(palette.RoofColors.Length)];
            if (style == TowerStyle.Spire)
            {
                var spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                spire.name = "TowerSpire";
                spire.transform.SetParent(tower.transform);
                spire.transform.localPosition = new Vector3(0, totalHeight + towerSize, 0);
                spire.transform.localScale = new Vector3(towerSize * 0.3f, towerSize, towerSize * 0.3f);
                ApplyMaterial(spire, roofColor);
            }
            else
            {
                CreateBattlements(tower.transform, totalHeight, new Vector2(towerSize, towerSize), 
                    palette.TrimColors[0]);
            }
        }
        
        private void CreateChimney(Transform parent, Vector2 size, StylePalette palette, System.Random random)
        {
            var chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimney.name = "Chimney";
            chimney.transform.SetParent(parent);
            
            // Position on roof
            float x = (float)(random.NextDouble() - 0.5) * size.x * 0.5f;
            float z = (float)(random.NextDouble() - 0.5) * size.y * 0.5f;
            float y = parent.childCount > 0 ? 
                parent.GetChild(parent.childCount - 1).localPosition.y + 3f : 5f;
            
            chimney.transform.localPosition = new Vector3(x, y, z);
            chimney.transform.localScale = new Vector3(0.8f, 2f, 0.8f);
            ApplyMaterial(chimney, palette.WallColors[palette.WallColors.Length - 1]);
        }
        
        private void CreateBalcony(Transform parent, Vector2 size, StylePalette palette, System.Random random)
        {
            var balcony = new GameObject("Balcony");
            balcony.transform.SetParent(parent);
            
            // Position on front
            balcony.transform.localPosition = new Vector3(0, -floorHeight / 3f, size.y / 2f + 0.7f);
            
            // Platform
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Platform";
            platform.transform.SetParent(balcony.transform);
            platform.transform.localPosition = Vector3.zero;
            platform.transform.localScale = new Vector3(2.5f, 0.15f, 1.2f);
            ApplyMaterial(platform, palette.TrimColors[0]);
            
            // Railing
            CreateRailing(balcony.transform, 2.5f, 1.2f, palette.TrimColors[0]);
        }
        
        private void CreateRailing(Transform parent, float width, float depth, Color color)
        {
            float railHeight = 1f;
            float postSpacing = 0.5f;
            
            // Posts
            int postCount = Mathf.CeilToInt(width / postSpacing) + 1;
            for (int i = 0; i < postCount; i++)
            {
                float x = -width / 2f + i * (width / (postCount - 1));
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "RailPost";
                post.transform.SetParent(parent);
                post.transform.localPosition = new Vector3(x, railHeight / 2f, depth / 2f);
                post.transform.localScale = new Vector3(0.1f, railHeight, 0.1f);
                ApplyMaterial(post, color);
            }
            
            // Top rail
            var topRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topRail.name = "TopRail";
            topRail.transform.SetParent(parent);
            topRail.transform.localPosition = new Vector3(0, railHeight, depth / 2f);
            topRail.transform.localScale = new Vector3(width + 0.1f, 0.1f, 0.1f);
            ApplyMaterial(topRail, color);
        }
        
        private void CreateTimberFraming(Transform parent, Vector2 size, float height, 
            StylePalette palette, System.Random random)
        {
            Color timberColor = palette.TrimColors[0];
            
            // Horizontal beams
            CreateTimber(parent, new Vector3(0, height / 2f, size.y / 2f + 0.16f), 
                new Vector3(size.x, 0.15f, 0.05f), timberColor);
            CreateTimber(parent, new Vector3(0, -height / 2f + 0.3f, size.y / 2f + 0.16f), 
                new Vector3(size.x, 0.15f, 0.05f), timberColor);
            
            // Vertical beams
            int beamCount = Mathf.FloorToInt(size.x / 2f);
            for (int i = 0; i < beamCount; i++)
            {
                float x = -size.x / 2f + (i + 0.5f) * (size.x / beamCount);
                CreateTimber(parent, new Vector3(x, 0, size.y / 2f + 0.16f), 
                    new Vector3(0.1f, height, 0.05f), timberColor);
            }
            
            // Diagonal crosses (random)
            if (random.NextDouble() > 0.5)
            {
                CreateTimber(parent, new Vector3(-size.x / 4f, 0, size.y / 2f + 0.17f), 
                    new Vector3(0.08f, height * 0.9f, 0.04f), timberColor, 20f);
                CreateTimber(parent, new Vector3(size.x / 4f, 0, size.y / 2f + 0.17f), 
                    new Vector3(0.08f, height * 0.9f, 0.04f), timberColor, -20f);
            }
        }
        
        private void CreateTimber(Transform parent, Vector3 position, Vector3 scale, Color color, float rotation = 0)
        {
            var timber = GameObject.CreatePrimitive(PrimitiveType.Cube);
            timber.name = "Timber";
            timber.transform.SetParent(parent);
            timber.transform.localPosition = position;
            timber.transform.localRotation = Quaternion.Euler(0, 0, rotation);
            timber.transform.localScale = scale;
            ApplyMaterial(timber, color);
        }
        
        #endregion
        
        #region Decorations
        
        private void AddDecorations(Transform building, BuildingParameters parameters, 
            StylePalette palette, System.Random random)
        {
            // Flags
            if (random.NextDouble() < decorationDensity * 0.5)
            {
                AddFlag(building, palette, random);
            }
            
            // Torches
            if (random.NextDouble() < decorationDensity)
            {
                AddTorches(building, palette, random);
            }
            
            // Signs (for taverns, shops)
            if (parameters.HasSignage)
            {
                AddSign(building, palette, random);
            }
        }
        
        private void AddFlag(Transform building, StylePalette palette, System.Random random)
        {
            var flagPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flagPole.name = "FlagPole";
            flagPole.transform.SetParent(building);
            
            // Find highest point
            float maxY = 0;
            foreach (Transform child in building)
            {
                maxY = Mathf.Max(maxY, child.position.y + child.localScale.y / 2f);
            }
            
            flagPole.transform.localPosition = new Vector3(0, maxY + 1.5f, 0);
            flagPole.transform.localScale = new Vector3(0.1f, 3f, 0.1f);
            ApplyMaterial(flagPole, palette.TrimColors[0]);
            
            // Flag cloth
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.SetParent(flagPole.transform);
            flag.transform.localPosition = new Vector3(0.6f, 1.2f, 0);
            flag.transform.localScale = new Vector3(12f, 8f, 0.5f);
            ApplyMaterial(flag, palette.RoofColors[random.Next(palette.RoofColors.Length)]);
        }
        
        private void AddTorches(Transform building, StylePalette palette, System.Random random)
        {
            // Add torches near entrances
            int torchCount = random.Next(2, 5);
            for (int i = 0; i < torchCount; i++)
            {
                float x = (float)(random.NextDouble() - 0.5) * 10f;
                float z = (float)(random.NextDouble() - 0.5) * 10f;
                
                var torch = new GameObject("Torch");
                torch.transform.SetParent(building);
                torch.transform.localPosition = new Vector3(x, 2f, z);
                
                // Torch holder
                var holder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                holder.name = "TorchHolder";
                holder.transform.SetParent(torch.transform);
                holder.transform.localPosition = Vector3.zero;
                holder.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
                ApplyMaterial(holder, palette.TrimColors[0]);
                
                // Light
                var light = torch.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.7f, 0.4f);
                light.intensity = 2f;
                light.range = 5f;
            }
        }
        
        private void AddSign(Transform building, StylePalette palette, System.Random random)
        {
            var signPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            signPost.name = "SignPost";
            signPost.transform.SetParent(building);
            signPost.transform.localPosition = new Vector3(3f, 1.5f, 0);
            signPost.transform.localScale = new Vector3(0.1f, 3f, 0.1f);
            ApplyMaterial(signPost, palette.TrimColors[0]);
            
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Sign";
            sign.transform.SetParent(signPost.transform);
            sign.transform.localPosition = new Vector3(0.5f, 0.8f, 0);
            sign.transform.localScale = new Vector3(10f, 6f, 0.5f);
            ApplyMaterial(sign, palette.WallColors[0]);
        }
        
        #endregion
        
        #region Helpers
        
        private void ApplyMaterial(GameObject obj, Color color, bool isStone = false)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null) return;
            
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            
            var mat = new Material(shader);
            mat.color = color;
            
            if (isStone)
            {
                mat.SetFloat("_Smoothness", 0.2f);
            }
            else
            {
                mat.SetFloat("_Smoothness", 0.4f);
            }
            
            renderer.material = mat;
            
            // Remove collider from decoration pieces
            var collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Set the default architectural style
        /// </summary>
        public void SetDefaultStyle(ArchitecturalStyle style)
        {
            defaultStyle = style;
        }
        
        /// <summary>
        /// Get a generated building by ID
        /// </summary>
        public GeneratedBuilding GetBuilding(string id)
        {
            return _generatedBuildings.TryGetValue(id, out var building) ? building : null;
        }
        
        /// <summary>
        /// Remove a generated building
        /// </summary>
        public void RemoveBuilding(string id)
        {
            if (_generatedBuildings.TryGetValue(id, out var building))
            {
                if (building.GameObject != null)
                {
                    Destroy(building.GameObject);
                }
                _generatedBuildings.Remove(id);
            }
        }
        
        #endregion
    }
    
    #region Data Types
    
    public enum ArchitecturalStyle
    {
        MedievalFantasy,
        HighFantasy,
        DarkGothic,
        Steampunk,
        Oriental,
        Nordic,
        Desert
    }
    
    public enum BuildingType
    {
        House,
        Tower,
        Castle,
        Temple,
        Barracks,
        Workshop,
        Tavern,
        Wall,
        Gate,
        Farm,
        Mill,
        Mine,
        Storage
    }
    
    public enum WindowStyle
    {
        Square,
        Gothic,
        Arched,
        Round
    }
    
    public enum RoofStyle
    {
        Flat,
        Peaked,
        Domed,
        Spires
    }
    
    public enum TowerStyle
    {
        Square,
        Round,
        Spire
    }
    
    [Serializable]
    public class StylePalette
    {
        public ArchitecturalStyle Style;
        public Color[] WallColors;
        public Color[] RoofColors;
        public Color[] TrimColors;
        public WindowStyle WindowStyle;
        public RoofStyle RoofStyle;
        public bool HasTimberFraming;
    }
    
    [Serializable]
    public class BuildingParameters
    {
        public Vector2 BaseSize;
        public int FloorCount;
        public bool HasChimney;
        public bool HasBalcony;
        public bool HasTower;
        public int TowerCount;
        public TowerStyle TowerStyle;
        public bool HasDome;
        public bool HasCurtainWall;
        public bool HasTrainingYard;
        public bool HasSmokestacks;
        public bool HasSignage;
        public float WallThickness;
        public bool HasBattlements;
        public bool HasPortcullis;
    }
    
    public class GeneratedBuilding
    {
        public string Id;
        public GameObject GameObject;
        public BuildingType Type;
        public ArchitecturalStyle Style;
        public int Seed;
        public Vector3 Position;
    }
    
    [Serializable]
    public class BuildingComponentLibrary
    {
        // Would contain prefab references for modular building parts
        public GameObject[] WallSegments;
        public GameObject[] RoofPieces;
        public GameObject[] Windows;
        public GameObject[] Doors;
        public GameObject[] Decorations;
    }
    
    #endregion
}
