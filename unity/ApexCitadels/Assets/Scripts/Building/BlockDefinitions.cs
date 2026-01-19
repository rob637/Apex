// ============================================================================
// APEX CITADELS - BLOCK DEFINITIONS
// Static helper class for accessing block type definitions
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.Building
{
    /// <summary>
    /// Definition data for a building block
    /// </summary>
    [System.Serializable]
    public class BlockDefinition
    {
        public BlockType Type;
        public string DisplayName;
        public string Description;
        public int ResourceCost;
        public int HealthPoints;
        public float BuildTime;
        public BlockCategory Category;

        public BlockDefinition(BlockType type, string name, string desc, int cost, int hp = 100, float time = 1f, BlockCategory cat = BlockCategory.Basic)
        {
            Type = type;
            DisplayName = name;
            Description = desc;
            ResourceCost = cost;
            HealthPoints = hp;
            BuildTime = time;
            Category = cat;
        }
    }

    /// <summary>
    /// Categories for organizing blocks in the editor
    /// </summary>
    public enum BlockCategory
    {
        Basic,
        Walls,
        Defense,
        Production,
        Military,
        Storage,
        Decoration,
        Special
    }

    /// <summary>
    /// Static accessor for block definitions
    /// </summary>
    public static class BlockDefinitions
    {
        private static Dictionary<BlockType, BlockDefinition> _definitions;

        static BlockDefinitions()
        {
            Initialize();
        }

        private static void Initialize()
        {
            _definitions = new Dictionary<BlockType, BlockDefinition>
            {
                // Basic blocks
                { BlockType.Stone, new BlockDefinition(BlockType.Stone, "Stone Block", "Basic stone building block", 10, 100, 1f, BlockCategory.Basic) },
                { BlockType.Wood, new BlockDefinition(BlockType.Wood, "Wood Block", "Basic wooden building block", 5, 60, 0.5f, BlockCategory.Basic) },
                { BlockType.Metal, new BlockDefinition(BlockType.Metal, "Metal Block", "Reinforced metal block", 25, 150, 2f, BlockCategory.Basic) },
                { BlockType.Glass, new BlockDefinition(BlockType.Glass, "Glass Block", "Transparent glass block", 15, 30, 1f, BlockCategory.Basic) },
                { BlockType.Brick, new BlockDefinition(BlockType.Brick, "Brick Block", "Standard brick block", 12, 80, 1f, BlockCategory.Basic) },
                { BlockType.Concrete, new BlockDefinition(BlockType.Concrete, "Concrete Block", "Heavy concrete block", 20, 200, 2f, BlockCategory.Basic) },
                { BlockType.Foundation, new BlockDefinition(BlockType.Foundation, "Foundation", "Building foundation block", 30, 300, 3f, BlockCategory.Basic) },

                // Walls
                { BlockType.Wall, new BlockDefinition(BlockType.Wall, "Wall", "Standard wall section", 15, 120, 1f, BlockCategory.Walls) },
                { BlockType.WallStone, new BlockDefinition(BlockType.WallStone, "Stone Wall", "Sturdy stone wall", 20, 150, 1.5f, BlockCategory.Walls) },
                { BlockType.WallWood, new BlockDefinition(BlockType.WallWood, "Wooden Wall", "Basic wooden wall", 10, 80, 0.8f, BlockCategory.Walls) },
                { BlockType.WallMetal, new BlockDefinition(BlockType.WallMetal, "Metal Wall", "Reinforced metal wall", 35, 200, 2f, BlockCategory.Walls) },
                { BlockType.WallReinforced, new BlockDefinition(BlockType.WallReinforced, "Reinforced Wall", "Extra strong wall", 50, 300, 3f, BlockCategory.Walls) },
                { BlockType.WallCorner, new BlockDefinition(BlockType.WallCorner, "Corner Wall", "Wall corner piece", 18, 130, 1f, BlockCategory.Walls) },
                { BlockType.WallGate, new BlockDefinition(BlockType.WallGate, "Wall Gate", "Gate section for walls", 40, 180, 2f, BlockCategory.Walls) },
                { BlockType.WallWindow, new BlockDefinition(BlockType.WallWindow, "Window Wall", "Wall with window", 25, 100, 1.5f, BlockCategory.Walls) },
                { BlockType.Battlement, new BlockDefinition(BlockType.Battlement, "Battlement", "Defensive wall top", 30, 150, 2f, BlockCategory.Walls) },
                { BlockType.Fence, new BlockDefinition(BlockType.Fence, "Fence", "Simple fence", 5, 40, 0.3f, BlockCategory.Walls) },
                { BlockType.Gate, new BlockDefinition(BlockType.Gate, "Gate", "Entry gate", 50, 200, 3f, BlockCategory.Walls) },

                // Defense
                { BlockType.Tower, new BlockDefinition(BlockType.Tower, "Watch Tower", "Basic defense tower", 100, 500, 10f, BlockCategory.Defense) },
                { BlockType.TowerArcher, new BlockDefinition(BlockType.TowerArcher, "Archer Tower", "Tower with archers", 150, 400, 15f, BlockCategory.Defense) },
                { BlockType.TowerCannon, new BlockDefinition(BlockType.TowerCannon, "Cannon Tower", "Tower with cannon", 200, 600, 20f, BlockCategory.Defense) },
                { BlockType.TowerMage, new BlockDefinition(BlockType.TowerMage, "Mage Tower", "Tower with mage", 250, 350, 25f, BlockCategory.Defense) },
                { BlockType.ArrowTower, new BlockDefinition(BlockType.ArrowTower, "Arrow Tower", "Rapid fire arrows", 120, 350, 12f, BlockCategory.Defense) },
                { BlockType.CannonTower, new BlockDefinition(BlockType.CannonTower, "Heavy Cannon", "High damage cannon", 220, 550, 22f, BlockCategory.Defense) },
                { BlockType.MageTower, new BlockDefinition(BlockType.MageTower, "Arcane Tower", "Magic attacks", 280, 300, 28f, BlockCategory.Defense) },
                { BlockType.Turret, new BlockDefinition(BlockType.Turret, "Auto Turret", "Automatic defense", 180, 250, 18f, BlockCategory.Defense) },
                { BlockType.Trap, new BlockDefinition(BlockType.Trap, "Trap", "Hidden trap", 30, 50, 2f, BlockCategory.Defense) },
                { BlockType.TrapFire, new BlockDefinition(BlockType.TrapFire, "Fire Trap", "Burning trap", 50, 50, 3f, BlockCategory.Defense) },
                { BlockType.TrapPit, new BlockDefinition(BlockType.TrapPit, "Pit Trap", "Fall trap", 40, 100, 4f, BlockCategory.Defense) },
                { BlockType.SpikeTrap, new BlockDefinition(BlockType.SpikeTrap, "Spike Trap", "Spike damage trap", 35, 60, 2f, BlockCategory.Defense) },
                { BlockType.Spikes, new BlockDefinition(BlockType.Spikes, "Spikes", "Ground spikes", 20, 80, 1f, BlockCategory.Defense) },
                { BlockType.Barricade, new BlockDefinition(BlockType.Barricade, "Barricade", "Blocking obstacle", 25, 150, 1.5f, BlockCategory.Defense) },
                { BlockType.Moat, new BlockDefinition(BlockType.Moat, "Moat", "Water defense", 80, 1000, 8f, BlockCategory.Defense) },

                // Production
                { BlockType.Mine, new BlockDefinition(BlockType.Mine, "Mine", "Extracts ore", 200, 400, 30f, BlockCategory.Production) },
                { BlockType.Quarry, new BlockDefinition(BlockType.Quarry, "Quarry", "Extracts stone", 150, 400, 25f, BlockCategory.Production) },
                { BlockType.Sawmill, new BlockDefinition(BlockType.Sawmill, "Sawmill", "Produces wood", 120, 300, 20f, BlockCategory.Production) },
                { BlockType.Foundry, new BlockDefinition(BlockType.Foundry, "Foundry", "Smelts metal", 250, 450, 35f, BlockCategory.Production) },
                { BlockType.Forge, new BlockDefinition(BlockType.Forge, "Forge", "Crafts equipment", 300, 400, 40f, BlockCategory.Production) },
                { BlockType.Farm, new BlockDefinition(BlockType.Farm, "Farm", "Produces food", 100, 200, 15f, BlockCategory.Production) },
                { BlockType.Generator, new BlockDefinition(BlockType.Generator, "Generator", "Produces energy", 350, 300, 45f, BlockCategory.Production) },
                { BlockType.CrystalExtractor, new BlockDefinition(BlockType.CrystalExtractor, "Crystal Extractor", "Extracts crystals", 400, 350, 50f, BlockCategory.Production) },
                { BlockType.ResourceNode, new BlockDefinition(BlockType.ResourceNode, "Resource Node", "Generic resource", 80, 200, 10f, BlockCategory.Production) },

                // Military
                { BlockType.Barracks, new BlockDefinition(BlockType.Barracks, "Barracks", "Train infantry", 300, 500, 45f, BlockCategory.Military) },
                { BlockType.Armory, new BlockDefinition(BlockType.Armory, "Armory", "Store weapons", 200, 400, 30f, BlockCategory.Military) },
                { BlockType.TrainingGround, new BlockDefinition(BlockType.TrainingGround, "Training Ground", "Train troops faster", 250, 300, 35f, BlockCategory.Military) },
                { BlockType.Workshop, new BlockDefinition(BlockType.Workshop, "Workshop", "Build siege", 350, 450, 50f, BlockCategory.Military) },
                { BlockType.Stable, new BlockDefinition(BlockType.Stable, "Stable", "Train cavalry", 280, 350, 40f, BlockCategory.Military) },

                // Storage
                { BlockType.Storage, new BlockDefinition(BlockType.Storage, "Storage", "Basic storage", 80, 200, 10f, BlockCategory.Storage) },
                { BlockType.StorageVault, new BlockDefinition(BlockType.StorageVault, "Vault", "Secure storage", 200, 500, 25f, BlockCategory.Storage) },
                { BlockType.Warehouse, new BlockDefinition(BlockType.Warehouse, "Warehouse", "Large storage", 150, 350, 20f, BlockCategory.Storage) },
                { BlockType.Treasury, new BlockDefinition(BlockType.Treasury, "Treasury", "Gold storage", 300, 600, 40f, BlockCategory.Storage) },
                { BlockType.Silo, new BlockDefinition(BlockType.Silo, "Silo", "Food storage", 100, 250, 15f, BlockCategory.Storage) },

                // Decorative
                { BlockType.Flag, new BlockDefinition(BlockType.Flag, "Flag", "Decorative flag", 5, 20, 0.5f, BlockCategory.Decoration) },
                { BlockType.Banner, new BlockDefinition(BlockType.Banner, "Banner", "Decorative banner", 8, 25, 0.5f, BlockCategory.Decoration) },
            };
        }

        /// <summary>
        /// Get definition for a block type
        /// </summary>
        public static BlockDefinition Get(BlockType type)
        {
            if (_definitions == null) Initialize();
            return _definitions.TryGetValue(type, out var def) ? def : null;
        }

        /// <summary>
        /// Get all definitions
        /// </summary>
        public static IEnumerable<BlockDefinition> GetAll()
        {
            if (_definitions == null) Initialize();
            return _definitions.Values;
        }

        /// <summary>
        /// Get definitions by category
        /// </summary>
        public static IEnumerable<BlockDefinition> GetByCategory(BlockCategory category)
        {
            if (_definitions == null) Initialize();
            foreach (var kvp in _definitions)
            {
                if (kvp.Value.Category == category)
                    yield return kvp.Value;
            }
        }
    }
}
