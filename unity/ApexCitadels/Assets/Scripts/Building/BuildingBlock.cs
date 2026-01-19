using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;

namespace ApexCitadels.Building
{
    /// <summary>
    /// Types of building blocks available to players
    /// Organized by category for editor palette
    /// </summary>
    public enum BlockType
    {
        // ============ BASIC BLOCKS ============
        Stone,
        Wood,
        Metal,
        Glass,
        Brick,
        Concrete,
        Foundation,

        // ============ WALLS ============
        Wall,
        WallStone,
        WallWood,
        WallMetal,
        WallReinforced,
        WallCorner,
        WallGate,
        WallWindow,
        Battlement,
        Fence,
        Gate,

        // ============ DEFENSE ============
        Tower,
        TowerArcher,
        TowerCannon,
        TowerMage,
        ArrowTower,
        CannonTower,
        MageTower,
        Turret,
        Trap,
        TrapFire,
        TrapPit,
        SpikeTrap,
        Spikes,
        Barricade,
        Moat,

        // ============ PRODUCTION ============
        Mine,
        Quarry,
        Sawmill,
        Foundry,
        Forge,
        Farm,
        Generator,
        CrystalExtractor,
        ResourceNode,

        // ============ MILITARY ============
        Barracks,
        Armory,
        TrainingGround,
        Workshop,
        Stable,

        // ============ STORAGE ============
        Storage,
        StorageVault,
        Warehouse,
        Treasury,
        Silo,

        // ============ DECORATIVE ============
        Flag,
        Banner,
        Torch,
        Lamp,
        Pillar,
        Statue,
        Garden,
        Fountain,
        Beacon,

        // ============ SPECIAL ============
        CommandCenter,
        CitadelCore,
        Portal,
        Shrine,
        Altar,
        AncientRelic,

        // Count marker (for iteration)
        Count
    }

    /// <summary>
    /// Represents a single building block placed in the world
    /// </summary>
    [Serializable]
    public class BuildingBlock
    {
        public string Id;
        public BlockType Type;
        public string OwnerId;
        public string TerritoryId;
        
        // Position (geospatial)
        public double Latitude;
        public double Longitude;
        public double Altitude;
        
        // Local transform (relative to anchor)
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;

        // Block state
        public int Health;
        public int MaxHealth;
        public DateTime PlacedAt;

        // World position property for PC editor (converts LocalPosition)
        public Vector3 Position
        {
            get => LocalPosition;
            set => LocalPosition = value;
        }

        // Rotation property for PC editor
        public Quaternion Rotation
        {
            get => LocalRotation;
            set => LocalRotation = value;
        }

        public BuildingBlock()
        {
            Id = Guid.NewGuid().ToString();
            PlacedAt = DateTime.UtcNow;
            LocalScale = Vector3.one;
            LocalRotation = Quaternion.identity;
        }

        public BuildingBlock(BlockType type) : this()
        {
            Type = type;
            SetHealthForType(type);
        }

        private void SetHealthForType(BlockType type)
        {
            switch (type)
            {
                case BlockType.Stone:
                    MaxHealth = 100;
                    break;
                case BlockType.Wood:
                    MaxHealth = 50;
                    break;
                case BlockType.Metal:
                    MaxHealth = 150;
                    break;
                case BlockType.Glass:
                    MaxHealth = 25;
                    break;
                case BlockType.Wall:
                    MaxHealth = 200;
                    break;
                case BlockType.Tower:
                    MaxHealth = 300;
                    break;
                case BlockType.Turret:
                    MaxHealth = 100;
                    break;
                default:
                    MaxHealth = 100;
                    break;
            }
            Health = MaxHealth;
        }

        public bool TakeDamage(int damage)
        {
            Health -= damage;
            return Health <= 0;
        }

        /// <summary>
        /// Convert block to Firestore-compatible dictionary
        /// </summary>
        public Dictionary<string, object> ToFirestoreData()
        {
            return new Dictionary<string, object>
            {
                { "id", Id },
                { "type", Type.ToString() },
                { "ownerId", OwnerId },
                { "territoryId", TerritoryId },
                { "latitude", Latitude },
                { "longitude", Longitude },
                { "altitude", Altitude },
                { "localPosition", new Dictionary<string, object>
                    {
                        { "x", LocalPosition.x },
                        { "y", LocalPosition.y },
                        { "z", LocalPosition.z }
                    }
                },
                { "localRotation", new Dictionary<string, object>
                    {
                        { "x", LocalRotation.x },
                        { "y", LocalRotation.y },
                        { "z", LocalRotation.z },
                        { "w", LocalRotation.w }
                    }
                },
                { "localScale", new Dictionary<string, object>
                    {
                        { "x", LocalScale.x },
                        { "y", LocalScale.y },
                        { "z", LocalScale.z }
                    }
                },
                { "health", Health },
                { "maxHealth", MaxHealth },
                { "placedAt", Timestamp.FromDateTime(PlacedAt.ToUniversalTime()) }
            };
        }

        /// <summary>
        /// Create BuildingBlock from Firestore document
        /// </summary>
        public static BuildingBlock FromFirestore(DocumentSnapshot doc)
        {
            if (!doc.Exists) return null;

            var block = new BuildingBlock();
            
            block.Id = doc.GetValue<string>("id");
            
            if (doc.TryGetValue("type", out string typeStr) && 
                Enum.TryParse(typeStr, out BlockType blockType))
            {
                block.Type = blockType;
            }

            block.OwnerId = doc.GetValue<string>("ownerId");
            block.TerritoryId = doc.GetValue<string>("territoryId");
            
            // Geospatial coordinates
            block.Latitude = doc.GetValue<double>("latitude");
            block.Longitude = doc.GetValue<double>("longitude");
            block.Altitude = doc.GetValue<double>("altitude");

            // Local position
            if (doc.TryGetValue("localPosition", out Dictionary<string, object> posDict))
            {
                block.LocalPosition = new Vector3(
                    Convert.ToSingle(posDict["x"]),
                    Convert.ToSingle(posDict["y"]),
                    Convert.ToSingle(posDict["z"])
                );
            }

            // Local rotation
            if (doc.TryGetValue("localRotation", out Dictionary<string, object> rotDict))
            {
                block.LocalRotation = new Quaternion(
                    Convert.ToSingle(rotDict["x"]),
                    Convert.ToSingle(rotDict["y"]),
                    Convert.ToSingle(rotDict["z"]),
                    Convert.ToSingle(rotDict["w"])
                );
            }

            // Local scale
            if (doc.TryGetValue("localScale", out Dictionary<string, object> scaleDict))
            {
                block.LocalScale = new Vector3(
                    Convert.ToSingle(scaleDict["x"]),
                    Convert.ToSingle(scaleDict["y"]),
                    Convert.ToSingle(scaleDict["z"])
                );
            }

            // Health
            block.Health = doc.GetValue<int>("health");
            block.MaxHealth = doc.GetValue<int>("maxHealth");

            // Timestamp
            if (doc.TryGetValue("placedAt", out Timestamp placedAt))
            {
                block.PlacedAt = placedAt.ToDateTime();
            }

            return block;
        }
    }

    /// <summary>
    /// Block definitions with metadata
    /// </summary>
    [Serializable]
    public class BlockDefinition
    {
        public BlockType Type;
        public string DisplayName;
        public string Description;
        public int ResourceCost;
        public string PrefabPath;
        public Sprite Icon;
        public bool RequiresTerritory;
        public int MinTerritoryLevel;

        public static Dictionary<BlockType, BlockDefinition> Definitions = new Dictionary<BlockType, BlockDefinition>
        {
            {
                BlockType.Stone, new BlockDefinition
                {
                    Type = BlockType.Stone,
                    DisplayName = "Stone Block",
                    Description = "A basic stone building block",
                    ResourceCost = 10,
                    PrefabPath = "Prefabs/Blocks/StoneBlock",
                    RequiresTerritory = false,
                    MinTerritoryLevel = 1
                }
            },
            {
                BlockType.Wood, new BlockDefinition
                {
                    Type = BlockType.Wood,
                    DisplayName = "Wood Block",
                    Description = "A basic wooden building block",
                    ResourceCost = 5,
                    PrefabPath = "Prefabs/Blocks/WoodBlock",
                    RequiresTerritory = false,
                    MinTerritoryLevel = 1
                }
            },
            {
                BlockType.Metal, new BlockDefinition
                {
                    Type = BlockType.Metal,
                    DisplayName = "Metal Block",
                    Description = "A sturdy metal building block",
                    ResourceCost = 25,
                    PrefabPath = "Prefabs/Blocks/MetalBlock",
                    RequiresTerritory = true,
                    MinTerritoryLevel = 2
                }
            },
            {
                BlockType.Wall, new BlockDefinition
                {
                    Type = BlockType.Wall,
                    DisplayName = "Defensive Wall",
                    Description = "A tall wall to protect your territory",
                    ResourceCost = 50,
                    PrefabPath = "Prefabs/Blocks/Wall",
                    RequiresTerritory = true,
                    MinTerritoryLevel = 1
                }
            },
            {
                BlockType.Tower, new BlockDefinition
                {
                    Type = BlockType.Tower,
                    DisplayName = "Watch Tower",
                    Description = "A tower that extends your visibility",
                    ResourceCost = 100,
                    PrefabPath = "Prefabs/Blocks/Tower",
                    RequiresTerritory = true,
                    MinTerritoryLevel = 2
                }
            },
            {
                BlockType.Turret, new BlockDefinition
                {
                    Type = BlockType.Turret,
                    DisplayName = "Defense Turret",
                    Description = "Automatically attacks nearby enemies",
                    ResourceCost = 200,
                    PrefabPath = "Prefabs/Blocks/Turret",
                    RequiresTerritory = true,
                    MinTerritoryLevel = 3
                }
            },
            {
                BlockType.Flag, new BlockDefinition
                {
                    Type = BlockType.Flag,
                    DisplayName = "Territory Flag",
                    Description = "Mark your territory with your colors",
                    ResourceCost = 15,
                    PrefabPath = "Prefabs/Blocks/Flag",
                    RequiresTerritory = true,
                    MinTerritoryLevel = 1
                }
            },
            {
                BlockType.Beacon, new BlockDefinition
                {
                    Type = BlockType.Beacon,
                    DisplayName = "Beacon",
                    Description = "A glowing beacon visible from far away",
                    ResourceCost = 75,
                    PrefabPath = "Prefabs/Blocks/Beacon",
                    RequiresTerritory = true,
                    MinTerritoryLevel = 2
                }
            }
        };

        public static BlockDefinition Get(BlockType type)
        {
            if (Definitions.TryGetValue(type, out BlockDefinition def))
            {
                return def;
            }
            return null;
        }
    }

    /// <summary>
    /// Static accessor for block definitions (wrapper for BlockDefinition.Get)
    /// </summary>
    public static class BlockDefinitions
    {
        public static BlockDefinition Get(BlockType type) => BlockDefinition.Get(type);
        
        public static IEnumerable<BlockDefinition> GetAll() => BlockDefinition.Definitions.Values;
    }
}
