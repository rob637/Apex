using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.Building
{
    /// <summary>
    /// Types of building blocks available to players
    /// </summary>
    public enum BlockType
    {
        // Basic blocks
        Stone,
        Wood,
        Metal,
        Glass,

        // Defensive structures
        Wall,
        Gate,
        Tower,
        Turret,

        // Decorative
        Flag,
        Banner,
        Torch,

        // Special
        Beacon,      // Marks territory center
        ResourceNode // Generates resources
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
}
