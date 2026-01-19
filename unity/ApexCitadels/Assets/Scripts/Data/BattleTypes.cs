using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.Data
{
    // ============================================================================
    // Troop System
    // ============================================================================

    /// <summary>
    /// Seven troop types with rock-paper-scissors counters
    /// Matches backend: TroopType in types/index.ts
    /// </summary>
    public enum TroopType
    {
        Infantry,   // Strong vs Archer, Weak vs Cavalry
        Archer,     // Strong vs Cavalry, Weak vs Infantry
        Cavalry,    // Strong vs Archer+Siege, Weak vs Guardian
        Siege,      // Strong vs Buildings, Weak vs Cavalry
        Mage,       // Strong vs Groups, Weak vs Infantry
        Guardian,   // Strong vs Cavalry, Weak vs Siege
        Elite       // Special high-tier troops
    }

    /// <summary>
    /// A squad of troops of a single type
    /// </summary>
    [Serializable]
    public class Troop
    {
        public TroopType Type;
        public int Count;
        public int Level;

        public Troop() { }

        public Troop(TroopType type, int count, int level = 1)
        {
            Type = type;
            Count = count;
            Level = level;
        }
    }

    /// <summary>
    /// Definition for each troop type - costs, stats, counters
    /// </summary>
    [Serializable]
    public class TroopDefinition
    {
        public TroopType Type;
        public string DisplayName;
        public int BaseAttack;
        public int BaseDefense;
        public int BaseHealth;
        public int TrainingTimeSeconds;
        public ResourceCost TrainingCost;
        public List<TroopType> StrongAgainst;
        public List<TroopType> WeakAgainst;
    }

    // ============================================================================
    // Territory State System
    // ============================================================================

    /// <summary>
    /// Territory state in the 3-strike siege system
    /// Matches backend: TerritoryState in types/index.ts
    /// </summary>
    public enum TerritoryState
    {
        Secure,     // Full production, all defenses active
        Contested,  // 20% production loss after first battle loss
        Vulnerable, // 50% production loss, structures damaged
        Fallen      // Territory lost, 24hr reclaim cooldown
    }

    /// <summary>
    /// Effects for each territory state
    /// </summary>
    [Serializable]
    public class TerritoryStateEffect
    {
        public TerritoryState State;
        public float ProductionMultiplier;
        public string Description;
    }

    // ============================================================================
    // Battle System
    // ============================================================================

    /// <summary>
    /// Status of a scheduled battle
    /// </summary>
    public enum BattleStatus
    {
        Scheduled,   // Battle scheduled, waiting for start time
        Preparing,   // Within preparation window, can set formations
        InProgress,  // Battle is executing
        Completed,   // Battle finished
        Cancelled    // Battle was cancelled
    }

    /// <summary>
    /// How the player is participating in a battle
    /// </summary>
    public enum ParticipationType
    {
        Physical,   // Within 50m - 100% effectiveness
        Nearby,     // Within 1km - 75% effectiveness
        Remote      // Anywhere - 50% effectiveness
    }

    /// <summary>
    /// Battle formation set by players before battle
    /// </summary>
    [Serializable]
    public class BattleFormation
    {
        public List<Troop> Troops;
        public int TotalPower;
        public string Strategy; // "aggressive", "defensive", "balanced"
        public bool IsReady;

        public BattleFormation()
        {
            Troops = new List<Troop>();
            TotalPower = 0;
            Strategy = "balanced";
            IsReady = false;
        }
    }

    /// <summary>
    /// A scheduled battle between attacker and defender
    /// </summary>
    [Serializable]
    public class ScheduledBattle
    {
        public string Id;
        public string AttackerId;
        public string AttackerName;
        public string DefenderId;
        public string DefenderName;
        public string TerritoryId;
        public DateTime ScheduledAt;
        public DateTime BattleStartsAt;
        public DateTime? BattleEndsAt;
        public BattleStatus Status;
        public BattleFormation AttackerFormation;
        public BattleFormation DefenderFormation;
        public ParticipationType? AttackerParticipation;
        public ParticipationType? DefenderParticipation;
        public float DefenderActivityBonus;
        public BattleResult Result;
        public string WarId;
        public bool IsWarBattle;

        // Aliases for compatibility
        public string BattleId { get => Id; set => Id = value; }
        public DateTime ScheduledTime { get => ScheduledAt; set => ScheduledAt = value; }
    }

    /// <summary>
    /// Result of a completed battle
    /// </summary>
    [Serializable]
    public class BattleResult
    {
        public string Winner; // "attacker" or "defender"
        public bool IsDecisive;
        public List<BattleRound> Rounds;
        public int TotalRounds;
        public List<Troop> AttackerLosses;
        public List<Troop> DefenderLosses;
        public List<Troop> AttackerSurvivors;
        public List<Troop> DefenderSurvivors;
        public float? StructureDamagePercent;
        public TerritoryState PreviousTerritoryState;
        public TerritoryState NewTerritoryState;
        public bool TerritoryFallen;
        public int AttackerXp;
        public int DefenderXp;
        public ResourceCost ResourcesLooted;

        // Additional properties for compatibility
        public string BattleId;
        public string WinnerId;
        public int AttackerCasualties;
        public int DefenderCasualties;
    }

    /// <summary>
    /// A single round of combat
    /// </summary>
    [Serializable]
    public class BattleRound
    {
        public int RoundNumber;
        public RoundAction AttackerAction;
        public RoundAction DefenderAction;
        public int AttackerDamageDealt;
        public int DefenderDamageDealt;
        public List<Troop> AttackerCasualties;
        public List<Troop> DefenderCasualties;
        public List<BattleEvent> Events;
    }

    /// <summary>
    /// Action taken by a player in a round
    /// </summary>
    [Serializable]
    public class RoundAction
    {
        public string Type; // "attack", "defend", "special"
        public TroopType? TargetTroopType;
        public string AbilityUsed;
    }

    /// <summary>
    /// Event that occurred during a battle round
    /// </summary>
    [Serializable]
    public class BattleEvent
    {
        public string Type; // "counter", "critical", "ability", "casualty", "morale"
        public string Message;
        public float? Impact;
    }

    // ============================================================================
    // Protection System
    // ============================================================================

    /// <summary>
    /// Location density classification
    /// </summary>
    public enum LocationDensity
    {
        Urban,      // Dense city: 25m radius
        Suburban,   // Suburbs: 35m radius
        Rural       // Rural areas: 50m radius
    }

    /// <summary>
    /// Activity level for defense bonus
    /// </summary>
    public enum ActivityLevel
    {
        Active,     // Played today: no bonus needed
        Away,       // 1-3 days inactive: 25% defense bonus
        Inactive,   // 3-7 days: 50% defense bonus
        Abandoned   // 7+ days: no protection, can be taken
    }

    /// <summary>
    /// Protection status for a player
    /// </summary>
    [Serializable]
    public class ProtectionStatus
    {
        public bool HasNewcomerShield;
        public DateTime? ShieldExpiresAt;
        public int ShieldDaysRemaining;
        public bool CanWaiveShield;
        public ActivityLevel ActivityLevel;
        public float DefenseBonus;
        public int DaysSinceLastActive;
    }

    // ============================================================================
    // Training Queue
    // ============================================================================

    /// <summary>
    /// An item in the troop training queue
    /// </summary>
    [Serializable]
    public class TrainingQueueItem
    {
        // Legacy fields (for Firebase compatibility)
        public TroopType TroopType;
        public int Count;
        public DateTime StartedAt;
        public DateTime CompletesAt;

        // New fields for UI/Manager usage
        public string Id;
        public TroopType Type;
        public int TotalTimeSeconds;
        public float ElapsedTime;
        public bool IsComplete;

        // Computed properties
        public float Progress => TotalTimeSeconds > 0 ? ElapsedTime / TotalTimeSeconds : GetProgress();
        public int RemainingSeconds => Mathf.Max(0, Mathf.CeilToInt(TotalTimeSeconds - ElapsedTime));

        public float GetProgress()
        {
            var total = (CompletesAt - StartedAt).TotalSeconds;
            var elapsed = (DateTime.UtcNow - StartedAt).TotalSeconds;
            return Mathf.Clamp01((float)(elapsed / total));
        }

        public TimeSpan GetTimeRemaining()
        {
            return CompletesAt - DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Player's troop inventory and training status
    /// </summary>
    [Serializable]
    public class UserTroops
    {
        public string OwnerId;
        public List<Troop> Troops;
        public int MaxCapacity;
        public List<TrainingQueueItem> TrainingQueue;
        public DateTime LastUpdated;

        // Convenience properties for troop counts by type
        public int Infantry { get => GetTroopCount(TroopType.Infantry); set => SetTroopCount(TroopType.Infantry, value); }
        public int Archer { get => GetTroopCount(TroopType.Archer); set => SetTroopCount(TroopType.Archer, value); }
        public int Cavalry { get => GetTroopCount(TroopType.Cavalry); set => SetTroopCount(TroopType.Cavalry, value); }
        public int Siege { get => GetTroopCount(TroopType.Siege); set => SetTroopCount(TroopType.Siege, value); }
        public int Mage { get => GetTroopCount(TroopType.Mage); set => SetTroopCount(TroopType.Mage, value); }
        public int Guardian { get => GetTroopCount(TroopType.Guardian); set => SetTroopCount(TroopType.Guardian, value); }

        public UserTroops()
        {
            Troops = new List<Troop>();
            TrainingQueue = new List<TrainingQueueItem>();
            MaxCapacity = 100;
        }

        public int GetTotalTroopCount()
        {
            int total = 0;
            foreach (var troop in Troops)
            {
                total += troop.Count;
            }
            return total;
        }

        public int GetTroopCount(TroopType type)
        {
            foreach (var troop in Troops)
            {
                if (troop.Type == type) return troop.Count;
            }
            return 0;
        }

        private void SetTroopCount(TroopType type, int count)
        {
            foreach (var troop in Troops)
            {
                if (troop.Type == type)
                {
                    troop.Count = count;
                    return;
                }
            }
            Troops.Add(new Troop { Type = type, Count = count });
        }
    }
}
