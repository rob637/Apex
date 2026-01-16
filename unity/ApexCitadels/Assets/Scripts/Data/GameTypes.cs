using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.Data
{
    // ============================================================================
    // Blueprint System
    // ============================================================================

    /// <summary>
    /// Building placement within a territory/blueprint
    /// </summary>
    [Serializable]
    public class BuildingPlacement
    {
        public string BlockType;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public float RotationY;
        public DateTime PlacedAt;

        public Vector3 GetPosition() => new(PositionX, PositionY, PositionZ);
        public Quaternion GetRotation() => Quaternion.Euler(0, RotationY, 0);
    }

    /// <summary>
    /// A saved citadel layout that can be restored
    /// Matches backend: Blueprint in types/index.ts
    /// </summary>
    [Serializable]
    public class Blueprint
    {
        public string Id;
        public string OwnerId;
        public string Name;
        public string Description;
        public List<BuildingPlacement> Buildings;
        public ResourceCost TotalBuildCost;
        public string SourceTerritoryId;
        public DateTime CreatedAt;
        public bool IsAutoSaved;

        // Property aliases for compatibility
        public string TerritoryId { get => SourceTerritoryId; set => SourceTerritoryId = value; }
        public bool IsAutoSave { get => IsAutoSaved; set => IsAutoSaved = value; }

        public Blueprint()
        {
            Id = Guid.NewGuid().ToString();
            Buildings = new List<BuildingPlacement>();
            TotalBuildCost = new ResourceCost();
            CreatedAt = DateTime.UtcNow;
        }

        private int _buildingCount;
        public int BuildingCount
        {
            get => Buildings?.Count ?? _buildingCount;
            set => _buildingCount = value;
        }
    }

    /// <summary>
    /// Summary info for blueprint list display
    /// </summary>
    [Serializable]
    public class BlueprintSummary
    {
        public string Id;
        public string Name;
        public string Description;
        public int BuildingCount;
        public ResourceCost TotalBuildCost;
        public bool IsAutoSaved;
        public DateTime CreatedAt;
    }

    /// <summary>
    /// Response from getMyBlueprints endpoint
    /// </summary>
    [Serializable]
    public class BlueprintListResponse
    {
        public List<BlueprintSummary> Blueprints;
        public List<BlueprintSummary> AutoSaves;
        public int TotalCount;
        public int MaxAllowed;
    }

    /// <summary>
    /// Response from previewBlueprintCost endpoint
    /// </summary>
    [Serializable]
    public class BlueprintCostPreview
    {
        public string BlueprintName;
        public int BuildingCount;
        public ResourceCost OriginalCost;
        public ResourceCost RebuildCost;
        public float Discount;
        public ResourceCost DiscountAmount;
        public bool CanAfford;
        public List<string> Missing;
        public ResourceCost CurrentResources;
    }

    // ============================================================================
    // Alliance War System
    // ============================================================================

    /// <summary>
    /// War status
    /// </summary>
    public enum WarStatus
    {
        Pending,    // War declared, waiting for warning period
        Warning,    // 24hr warning phase
        Active,     // 48hr active war
        Ended,      // War concluded
        Cancelled   // War cancelled before starting
    }

    /// <summary>
    /// War phase
    /// </summary>
    public enum WarPhase
    {
        Warning,        // 24hr warning period - can cancel
        Battle,         // 48hr battle period - fighting allowed
        PeaceTreaty     // 72hr peace treaty - no attacks
    }

    /// <summary>
    /// Alliance war data
    /// </summary>
    [Serializable]
    public class AllianceWar
    {
        public string Id;
        public string ChallengerAllianceId;
        public string ChallengerAllianceName;
        public string DefenderAllianceId;
        public string DefenderAllianceName;
        public WarStatus Status;
        public WarPhase Phase;
        public DateTime DeclaredAt;
        public DateTime WarningEndsAt;
        public DateTime StartsAt;
        public DateTime EndsAt;
        public DateTime? PeaceTreatyEndsAt;
        public int ChallengerScore;
        public int DefenderScore;
    }

    /// <summary>
    /// War participant stats
    /// </summary>
    [Serializable]
    public class WarParticipant
    {
        public string UserId;
        public string Username;
        public int BattlesWon;
        public int BattlesLost;
        public int PointsContributed;
    }

    // ============================================================================
    // Configuration Constants
    // ============================================================================

    /// <summary>
    /// Battle system configuration - matches backend BATTLE_CONFIG
    /// </summary>
    public static class BattleConfig
    {
        // Scheduling
        public const int SchedulingWindowHours = 24;
        public const int BattleDurationMinutes = 30;
        public const int MaxRounds = 10;

        // Post-battle
        public const int PostBattleShieldHours = 4;
        public const int SameAttackerCooldownHours = 48;
        public const int SameAllianceCooldownHours = 24;

        // Siege system
        public const int LossesToFall = 3;
        public const int ReclaimCooldownHours = 24;
        public const float ReclaimCostPercent = 0.3f;

        // Protection
        public const int NewcomerShieldDays = 7;
        public const int InactiveBonusThresholdDays = 2;
        public const float InactiveDefenseBonus = 0.5f;

        // Participation radius
        public const float PhysicalPresenceRadiusM = 50f;
        public const float NearbyRadiusM = 1000f;

        // Victory conditions
        public const float DecisiveVictoryThreshold = 0.7f;
        public const float CitadelDestroyThreshold = 0.5f;
    }

    /// <summary>
    /// Territory radius by density - matches backend TERRITORY_RADIUS_BY_DENSITY
    /// </summary>
    public static class TerritoryConfig
    {
        public static readonly Dictionary<LocationDensity, float> RadiusByDensity = new()
        {
            { LocationDensity.Urban, 25f },
            { LocationDensity.Suburban, 35f },
            { LocationDensity.Rural, 50f }
        };
    }

    /// <summary>
    /// Troop combat multipliers - matches backend
    /// </summary>
    public static class CombatConfig
    {
        public const float CounterMultiplier = 1.5f;
        public const float WeaknessMultiplier = 0.5f;

        public static readonly Dictionary<ParticipationType, float> ParticipationEffectiveness = new()
        {
            { ParticipationType.Physical, 1.0f },
            { ParticipationType.Nearby, 0.75f },
            { ParticipationType.Remote, 0.5f }
        };
    }

    /// <summary>
    /// Blueprint configuration
    /// </summary>
    public static class BlueprintConfig
    {
        public const int MaxBlueprintsPerUser = 10;
        public const int MaxAutoSaves = 3;
        public const float RebuildDiscount = 0.25f;
        public const int MaxNameLength = 50;
        public const int MaxDescriptionLength = 200;
    }
}
