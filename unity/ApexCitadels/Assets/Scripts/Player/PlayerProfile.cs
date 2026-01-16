using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.Player
{
    /// <summary>
    /// Player profile and stats
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public string Id;
        public string DisplayName;
        public string Email;
        public DateTime CreatedAt;
        public DateTime LastLoginAt;

        // Alias for Id used by some systems
        public string PlayerId => Id;

        // Stats
        public int Level;
        public int Experience;
        public int TerritoriesOwned;
        public int TerritoresConquered;
        public int TerritoriesLost;
        public int BlocksPlaced;
        public int BlocksDestroyed;

        // Resources
        public int Stone;
        public int Wood;
        public int Metal;
        public int Crystal;

        // Premium currency
        public int Gems;

        // Alliance
        public string AllianceId;
        public string AllianceName;

        public PlayerProfile()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            LastLoginAt = DateTime.UtcNow;
            Level = 1;
            Experience = 0;

            // Starting resources
            Stone = 100;
            Wood = 100;
            Metal = 25;
            Crystal = 0;
            Gems = 10;
        }

        public int GetExperienceForNextLevel()
        {
            return Level * 100; // 100 XP per level
        }

        public bool AddExperience(int amount)
        {
            Experience += amount;
            int required = GetExperienceForNextLevel();
            
            if (Experience >= required)
            {
                Experience -= required;
                Level++;
                return true; // Leveled up
            }
            return false;
        }

        public bool HasResources(int stone = 0, int wood = 0, int metal = 0, int crystal = 0)
        {
            return Stone >= stone && Wood >= wood && Metal >= metal && Crystal >= crystal;
        }

        public bool SpendResources(int stone = 0, int wood = 0, int metal = 0, int crystal = 0)
        {
            if (!HasResources(stone, wood, metal, crystal))
                return false;

            Stone -= stone;
            Wood -= wood;
            Metal -= metal;
            Crystal -= crystal;
            return true;
        }

        public void AddResources(int stone = 0, int wood = 0, int metal = 0, int crystal = 0)
        {
            Stone += stone;
            Wood += wood;
            Metal += metal;
            Crystal += crystal;
        }
    }

    /// <summary>
    /// Resource types in the game
    /// </summary>
    public enum ResourceType
    {
        Stone,
        Wood,
        Metal,
        Crystal,
        Gems
    }

    /// <summary>
    /// Resource node that can be harvested
    /// </summary>
    [Serializable]
    public class ResourceNode
    {
        public string Id;
        public ResourceType Type;
        public double Latitude;
        public double Longitude;
        public int Amount;
        public int MaxAmount;
        public DateTime LastHarvestedAt;
        public float RespawnTimeMinutes;

        public ResourceNode()
        {
            Id = Guid.NewGuid().ToString();
            LastHarvestedAt = DateTime.MinValue;
            RespawnTimeMinutes = 30f;
        }

        public bool CanHarvest()
        {
            if (Amount <= 0)
            {
                TimeSpan elapsed = DateTime.UtcNow - LastHarvestedAt;
                if (elapsed.TotalMinutes >= RespawnTimeMinutes)
                {
                    Amount = MaxAmount; // Respawn
                    return true;
                }
                return false;
            }
            return true;
        }

        public int Harvest(int amount)
        {
            int harvested = Math.Min(amount, Amount);
            Amount -= harvested;
            LastHarvestedAt = DateTime.UtcNow;
            return harvested;
        }
    }
}
