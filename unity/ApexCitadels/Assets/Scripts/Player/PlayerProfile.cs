using System;
using System.Collections.Generic;
using UnityEngine;
#if FIREBASE_ENABLED
using Firebase.Firestore;
#endif

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
        public bool IsAnonymous;
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
        public int AttacksWon;
        public int DefensesWon;
        public int BuildingsPlaced;

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

        // Property aliases for compatibility
        public int CurrentXP => Experience;
        public int XPToNextLevel => GetExperienceForNextLevel();
        public int TotalExperience => Experience + (Level - 1) * 100; // Accumulated XP estimate
        public int TotalTerritoriesCaptured => TerritoresConquered;

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

#if FIREBASE_ENABLED
        /// <summary>
        /// Convert profile to Firestore-compatible dictionary
        /// </summary>
        public Dictionary<string, object> ToFirestoreData()
        {
            return new Dictionary<string, object>
            {
                { "displayName", DisplayName },
                { "email", Email ?? "" },
                { "isAnonymous", IsAnonymous },
                { "createdAt", Timestamp.FromDateTime(CreatedAt.ToUniversalTime()) },
                { "lastLoginAt", Timestamp.FromDateTime(LastLoginAt.ToUniversalTime()) },
                
                // Stats
                { "level", Level },
                { "experience", Experience },
                { "territoriesOwned", TerritoriesOwned },
                { "territoriesConquered", TerritoresConquered },
                { "territoriesLost", TerritoriesLost },
                { "blocksPlaced", BlocksPlaced },
                { "blocksDestroyed", BlocksDestroyed },
                { "attacksWon", AttacksWon },
                { "defensesWon", DefensesWon },
                { "buildingsPlaced", BuildingsPlaced },
                
                // Resources
                { "resources", new Dictionary<string, object>
                    {
                        { "stone", Stone },
                        { "wood", Wood },
                        { "metal", Metal },
                        { "crystal", Crystal },
                        { "gems", Gems }
                    }
                },
                
                // Alliance
                { "allianceId", AllianceId ?? "" },
                { "allianceName", AllianceName ?? "" }
            };
        }

        /// <summary>
        /// Create profile from Firestore document snapshot
        /// </summary>
        public static PlayerProfile FromFirestore(DocumentSnapshot snapshot)
        {
            var profile = new PlayerProfile
            {
                Id = snapshot.Id,
                DisplayName = snapshot.GetValue<string>("displayName") ?? "Player",
                Email = snapshot.GetValue<string>("email") ?? "",
                IsAnonymous = snapshot.TryGetValue<bool>("isAnonymous", out var isAnon) && isAnon,
                Level = snapshot.TryGetValue<long>("level", out var level) ? (int)level : 1,
                Experience = snapshot.TryGetValue<long>("experience", out var exp) ? (int)exp : 0,
                TerritoriesOwned = snapshot.TryGetValue<long>("territoriesOwned", out var terrOwned) ? (int)terrOwned : 0,
                TerritoresConquered = snapshot.TryGetValue<long>("territoriesConquered", out var terrConq) ? (int)terrConq : 0,
                TerritoriesLost = snapshot.TryGetValue<long>("territoriesLost", out var terrLost) ? (int)terrLost : 0,
                BlocksPlaced = snapshot.TryGetValue<long>("blocksPlaced", out var blocksP) ? (int)blocksP : 0,
                BlocksDestroyed = snapshot.TryGetValue<long>("blocksDestroyed", out var blocksD) ? (int)blocksD : 0,
                AttacksWon = snapshot.TryGetValue<long>("attacksWon", out var attW) ? (int)attW : 0,
                DefensesWon = snapshot.TryGetValue<long>("defensesWon", out var defW) ? (int)defW : 0,
                BuildingsPlaced = snapshot.TryGetValue<long>("buildingsPlaced", out var bldgP) ? (int)bldgP : 0,
                AllianceId = snapshot.GetValue<string>("allianceId") ?? "",
                AllianceName = snapshot.GetValue<string>("allianceName") ?? ""
            };

            // Parse timestamps
            if (snapshot.TryGetValue<Timestamp>("createdAt", out var createdAt))
                profile.CreatedAt = createdAt.ToDateTime();
            if (snapshot.TryGetValue<Timestamp>("lastLoginAt", out var lastLogin))
                profile.LastLoginAt = lastLogin.ToDateTime();

            // Parse resources
            if (snapshot.TryGetValue<Dictionary<string, object>>("resources", out var resources))
            {
                if (resources.TryGetValue("stone", out var stone)) profile.Stone = Convert.ToInt32(stone);
                if (resources.TryGetValue("wood", out var wood)) profile.Wood = Convert.ToInt32(wood);
                if (resources.TryGetValue("metal", out var metal)) profile.Metal = Convert.ToInt32(metal);
                if (resources.TryGetValue("crystal", out var crystal)) profile.Crystal = Convert.ToInt32(crystal);
                if (resources.TryGetValue("gems", out var gems)) profile.Gems = Convert.ToInt32(gems);
            }

            return profile;
        }
#endif
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
