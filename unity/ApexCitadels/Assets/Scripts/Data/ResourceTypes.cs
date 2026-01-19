using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.Data
{
    // ============================================================================
    // Resource System
    // ============================================================================

    /// <summary>
    /// All resource types in the game
    /// Matches backend: ResourceType in types/index.ts
    /// </summary>
    public enum ResourceType
    {
        Stone,          // Basic building material
        Wood,           // Basic building material
        Iron,           // Weapons/armor
        Metal,          // Alias for Iron (legacy compatibility)
        Crystal,        // Advanced building material
        ArcaneEssence,  // Magical resource for elite troops
        Gems,           // Premium currency
        Gold,           // Currency
        Food,           // Troop upkeep
        Energy,         // Action resource
        Influence       // Social/political resource
    }

    /// <summary>
    /// Simple resource cost item with Type and Amount - used for list-based operations
    /// </summary>
    [Serializable]
    public struct ResourceCostItem
    {
        public ResourceType Type;
        public int Amount;

        public ResourceCostItem(ResourceType type, int amount)
        {
            Type = type;
            Amount = amount;
        }
    }

    /// <summary>
    /// Resource amounts - used for costs, rewards, etc.
    /// </summary>
    [Serializable]
    public class ResourceCost
    {
        public int Stone;
        public int Wood;
        public int Iron;
        public int Crystal;
        public int ArcaneEssence;
        public int Gems;

        public ResourceCost() { }

        public ResourceCost(int stone = 0, int wood = 0, int iron = 0, 
                           int crystal = 0, int arcaneEssence = 0, int gems = 0)
        {
            Stone = stone;
            Wood = wood;
            Iron = iron;
            Crystal = crystal;
            ArcaneEssence = arcaneEssence;
            Gems = gems;
        }

        public int GetAmount(ResourceType type)
        {
            return type switch
            {
                ResourceType.Stone => Stone,
                ResourceType.Wood => Wood,
                ResourceType.Iron => Iron,
                ResourceType.Crystal => Crystal,
                ResourceType.ArcaneEssence => ArcaneEssence,
                ResourceType.Gems => Gems,
                _ => 0
            };
        }

        public void SetAmount(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Stone: Stone = amount; break;
                case ResourceType.Wood: Wood = amount; break;
                case ResourceType.Iron: Iron = amount; break;
                case ResourceType.Crystal: Crystal = amount; break;
                case ResourceType.ArcaneEssence: ArcaneEssence = amount; break;
                case ResourceType.Gems: Gems = amount; break;
            }
        }

        public bool CanAfford(ResourceCost cost)
        {
            return Stone >= cost.Stone &&
                   Wood >= cost.Wood &&
                   Iron >= cost.Iron &&
                   Crystal >= cost.Crystal &&
                   ArcaneEssence >= cost.ArcaneEssence &&
                   Gems >= cost.Gems;
        }

        public void Subtract(ResourceCost cost)
        {
            Stone -= cost.Stone;
            Wood -= cost.Wood;
            Iron -= cost.Iron;
            Crystal -= cost.Crystal;
            ArcaneEssence -= cost.ArcaneEssence;
            Gems -= cost.Gems;
        }

        public void Add(ResourceCost cost)
        {
            Stone += cost.Stone;
            Wood += cost.Wood;
            Iron += cost.Iron;
            Crystal += cost.Crystal;
            ArcaneEssence += cost.ArcaneEssence;
            Gems += cost.Gems;
        }

        /// <summary>
        /// Convert to list of ResourceCostItem (only non-zero amounts)
        /// </summary>
        public List<ResourceCostItem> ToList()
        {
            var list = new List<ResourceCostItem>();
            if (Stone > 0) list.Add(new ResourceCostItem(ResourceType.Stone, Stone));
            if (Wood > 0) list.Add(new ResourceCostItem(ResourceType.Wood, Wood));
            if (Iron > 0) list.Add(new ResourceCostItem(ResourceType.Iron, Iron));
            if (Crystal > 0) list.Add(new ResourceCostItem(ResourceType.Crystal, Crystal));
            if (ArcaneEssence > 0) list.Add(new ResourceCostItem(ResourceType.ArcaneEssence, ArcaneEssence));
            if (Gems > 0) list.Add(new ResourceCostItem(ResourceType.Gems, Gems));
            return list;
        }

        public static ResourceCost operator +(ResourceCost a, ResourceCost b)
        {
            return new ResourceCost(
                a.Stone + b.Stone,
                a.Wood + b.Wood,
                a.Iron + b.Iron,
                a.Crystal + b.Crystal,
                a.ArcaneEssence + b.ArcaneEssence,
                a.Gems + b.Gems
            );
        }

        public static ResourceCost operator -(ResourceCost a, ResourceCost b)
        {
            return new ResourceCost(
                a.Stone - b.Stone,
                a.Wood - b.Wood,
                a.Iron - b.Iron,
                a.Crystal - b.Crystal,
                a.ArcaneEssence - b.ArcaneEssence,
                a.Gems - b.Gems
            );
        }

        /// <summary>
        /// Apply a multiplier to all resources (for discounts, bonuses)
        /// </summary>
        public ResourceCost Multiply(float multiplier)
        {
            return new ResourceCost(
                Mathf.CeilToInt(Stone * multiplier),
                Mathf.CeilToInt(Wood * multiplier),
                Mathf.CeilToInt(Iron * multiplier),
                Mathf.CeilToInt(Crystal * multiplier),
                Mathf.CeilToInt(ArcaneEssence * multiplier),
                Mathf.CeilToInt(Gems * multiplier)
            );
        }
    }

    /// <summary>
    /// Display info for a resource type
    /// </summary>
    [Serializable]
    public class ResourceInfo
    {
        public ResourceType Type;
        public string DisplayName;
        public string Icon;
        public Color Color;
        public string Description;
    }

    /// <summary>
    /// Static resource configuration - matches backend RESOURCE_CONFIG
    /// </summary>
    public static class ResourceConfig
    {
        public static readonly Dictionary<ResourceType, ResourceInfo> Info = new()
        {
            {
                ResourceType.Stone, new ResourceInfo
                {
                    Type = ResourceType.Stone,
                    DisplayName = "Stone",
                    Icon = "🪨",
                    Color = new Color(0.5f, 0.5f, 0.5f),
                    Description = "Basic building material from quarries"
                }
            },
            {
                ResourceType.Wood, new ResourceInfo
                {
                    Type = ResourceType.Wood,
                    DisplayName = "Wood",
                    Icon = "🪵",
                    Color = new Color(0.6f, 0.4f, 0.2f),
                    Description = "Basic building material from forests"
                }
            },
            {
                ResourceType.Iron, new ResourceInfo
                {
                    Type = ResourceType.Iron,
                    DisplayName = "Iron",
                    Icon = "⚙️",
                    Color = new Color(0.4f, 0.4f, 0.5f),
                    Description = "Refined material for weapons and armor"
                }
            },
            {
                ResourceType.Crystal, new ResourceInfo
                {
                    Type = ResourceType.Crystal,
                    DisplayName = "Crystal",
                    Icon = "💎",
                    Color = new Color(0.3f, 0.7f, 1.0f),
                    Description = "Rare material for advanced construction"
                }
            },
            {
                ResourceType.ArcaneEssence, new ResourceInfo
                {
                    Type = ResourceType.ArcaneEssence,
                    DisplayName = "Arcane Essence",
                    Icon = "✨",
                    Color = new Color(0.6f, 0.2f, 0.8f),
                    Description = "Magical essence for elite troops"
                }
            },
            {
                ResourceType.Gems, new ResourceInfo
                {
                    Type = ResourceType.Gems,
                    DisplayName = "Gems",
                    Icon = "💠",
                    Color = new Color(0.2f, 0.8f, 0.4f),
                    Description = "Premium currency"
                }
            }
        };

        /// <summary>
        /// Default starting resources for new players
        /// </summary>
        public static ResourceCost DefaultResources => new(
            stone: 500,
            wood: 500,
            iron: 100,
            crystal: 25,
            arcaneEssence: 0,
            gems: 50
        );
    }
}
