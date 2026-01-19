using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Core;
#if FIREBASE_ENABLED
using Firebase.Firestore;
#endif

namespace ApexCitadels.Territory
{
    /// <summary>
    /// Represents a claimed territory in the game world.
    /// Territories are circular areas around a player's citadel.
    /// </summary>
    [Serializable]
    public class Territory
    {
        public string Id;
        public string OwnerId;
        public string OwnerName;
        public string TerritoryName;
        public double CenterLatitude;
        public double CenterLongitude;
        public float RadiusMeters;
        public DateTime ClaimedAt;
        public DateTime LastDefendedAt;
        public int Level;
        public int Health;
        public int MaxHealth;

        // Territory states
        public bool IsContested;
        public string ContestingPlayerId;
        public string AllianceId;

        // Property aliases for compatibility
        public string Name { get => TerritoryName ?? $"Territory {Id?.Substring(0, 8)}"; set => TerritoryName = value; }
        public double Latitude { get => CenterLatitude; set => CenterLatitude = value; }
        public double Longitude { get => CenterLongitude; set => CenterLongitude = value; }
        public float Radius { get => RadiusMeters; set => RadiusMeters = value; }

        public Territory()
        {
            Id = Guid.NewGuid().ToString();
            ClaimedAt = DateTime.UtcNow;
            LastDefendedAt = DateTime.UtcNow;
            Level = 1;
            RadiusMeters = 10f; // Default 10 meter radius
            Health = 100;
            MaxHealth = 100;
            IsContested = false;
        }

        /// <summary>
        /// Check if a GPS coordinate is within this territory
        /// </summary>
        public bool ContainsLocation(double latitude, double longitude)
        {
            float distance = CalculateDistance(CenterLatitude, CenterLongitude, latitude, longitude);
            return distance <= RadiusMeters;
        }

        /// <summary>
        /// Calculate distance between two GPS coordinates in meters
        /// Uses Haversine formula
        /// </summary>
        public static float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // Earth's radius in meters

            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return (float)(R * c);
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        /// <summary>
        /// Check if this territory overlaps with another
        /// </summary>
        public bool OverlapsWith(Territory other)
        {
            float distance = CalculateDistance(
                CenterLatitude, CenterLongitude,
                other.CenterLatitude, other.CenterLongitude
            );
            return distance < (RadiusMeters + other.RadiusMeters);
        }

        /// <summary>
        /// Upgrade territory to next level (increases radius and health)
        /// </summary>
        public void Upgrade()
        {
            Level++;
            RadiusMeters = 10f + (Level - 1) * 5f; // +5m per level
            MaxHealth = 100 + (Level - 1) * 50;    // +50 HP per level
            Health = MaxHealth;
        }

        /// <summary>
        /// Apply damage to territory
        /// </summary>
        public bool TakeDamage(int damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                return true; // Territory destroyed
            }
            return false;
        }

        /// <summary>
        /// Repair territory health
        /// </summary>
        public void Repair(int amount)
        {
            Health = Math.Min(Health + amount, MaxHealth);
        }

#if FIREBASE_ENABLED
        /// <summary>
        /// Convert territory to Firestore-compatible dictionary
        /// </summary>
        public Dictionary<string, object> ToFirestoreData()
        {
            return new Dictionary<string, object>
            {
                { "ownerId", OwnerId },
                { "ownerName", OwnerName ?? "" },
                { "territoryName", TerritoryName ?? "" },
                { "centerLatitude", CenterLatitude },
                { "centerLongitude", CenterLongitude },
                { "radiusMeters", RadiusMeters },
                { "claimedAt", Timestamp.FromDateTime(ClaimedAt.ToUniversalTime()) },
                { "lastDefendedAt", Timestamp.FromDateTime(LastDefendedAt.ToUniversalTime()) },
                { "level", Level },
                { "health", Health },
                { "maxHealth", MaxHealth },
                { "isContested", IsContested },
                { "contestingPlayerId", ContestingPlayerId ?? "" },
                { "allianceId", AllianceId ?? "" }
            };
        }

        /// <summary>
        /// Create territory from Firestore document snapshot
        /// </summary>
        public static Territory FromFirestore(DocumentSnapshot snapshot)
        {
            try
            {
                var territory = new Territory
                {
                    Id = snapshot.Id,
                    OwnerId = snapshot.GetValue<string>("ownerId"),
                    OwnerName = snapshot.GetValue<string>("ownerName") ?? "",
                    TerritoryName = snapshot.GetValue<string>("territoryName") ?? "",
                    CenterLatitude = snapshot.GetValue<double>("centerLatitude"),
                    CenterLongitude = snapshot.GetValue<double>("centerLongitude"),
                    RadiusMeters = (float)snapshot.GetValue<double>("radiusMeters"),
                    Level = snapshot.TryGetValue<long>("level", out var level) ? (int)level : 1,
                    Health = snapshot.TryGetValue<long>("health", out var health) ? (int)health : 100,
                    MaxHealth = snapshot.TryGetValue<long>("maxHealth", out var maxHealth) ? (int)maxHealth : 100,
                    IsContested = snapshot.TryGetValue<bool>("isContested", out var contested) && contested,
                    ContestingPlayerId = snapshot.GetValue<string>("contestingPlayerId") ?? "",
                    AllianceId = snapshot.GetValue<string>("allianceId") ?? ""
                };

                // Parse timestamps
                if (snapshot.TryGetValue<Timestamp>("claimedAt", out var claimedAt))
                    territory.ClaimedAt = claimedAt.ToDateTime();
                if (snapshot.TryGetValue<Timestamp>("lastDefendedAt", out var lastDefended))
                    territory.LastDefendedAt = lastDefended.ToDateTime();

                return territory;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"Failed to parse from Firestore: {ex.Message}", ApexLogger.LogCategory.Territory);
                return null;
            }
        }
#endif
    }
}
