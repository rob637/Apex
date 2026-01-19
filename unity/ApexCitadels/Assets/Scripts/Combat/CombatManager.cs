using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Core;
using ApexCitadels.Territory;
using ApexCitadels.Player;

namespace ApexCitadels.Combat
{
    /// <summary>
    /// Attack types available to players
    /// </summary>
    public enum AttackType
    {
        QuickStrike,    // Low damage, no cooldown
        HeavyAttack,    // High damage, long cooldown
        Siege,          // Very high damage to structures
        Rally           // Buff for alliance members
    }

    /// <summary>
    /// Manages combat between players and territories
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("Combat Settings")]
        [SerializeField] private float quickStrikeDamage = 10f;
        [SerializeField] private float heavyAttackDamage = 30f;
        [SerializeField] private float siegeDamage = 50f;
        [SerializeField] private float heavyAttackCooldown = 5f;
        [SerializeField] private float siegeCooldown = 30f;
        [SerializeField] private float attackRange = 20f; // Must be within X meters

        [Header("Defense Settings")]
        [SerializeField] private float turretDamage = 5f;
        [SerializeField] private float turretRange = 15f;
        [SerializeField] private float turretFireRate = 2f;

        // Events
        public event Action<string, int> OnDamageDealt;
        public event Action<string> OnTerritoryConquered;
        public event Action<string, int> OnPlayerDamaged;
        public event Action OnCombatStarted;
        public event Action OnCombatEnded;

        // State
        private Dictionary<AttackType, float> _cooldowns = new Dictionary<AttackType, float>();
        private bool _inCombat = false;
        private Territory.Territory _currentTarget;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize cooldowns
            foreach (AttackType type in Enum.GetValues(typeof(AttackType)))
            {
                _cooldowns[type] = 0f;
            }
        }

        private void Update()
        {
            // Update cooldowns
            foreach (AttackType type in Enum.GetValues(typeof(AttackType)))
            {
                if (_cooldowns[type] > 0)
                {
                    _cooldowns[type] -= Time.deltaTime;
                }
            }

            // Auto-turret logic (if player owns turrets)
            if (_inCombat)
            {
                ProcessDefenses();
            }
        }

        #region Attacking

        /// <summary>
        /// Start attacking a territory
        /// </summary>
        public bool StartAttack(Territory.Territory target)
        {
            if (target == null) return false;

            string currentPlayerId = PlayerManager.Instance?.GetCurrentPlayerId() ?? "";
            if (target.OwnerId == currentPlayerId)
            {
                ApexLogger.Log(ApexLogger.LogCategory.Combat, "[CombatManager] Cannot attack your own territory!");
                return false;
            }

            // Check if in range (would use GPS in real implementation)
            // For now, allow attack

            _currentTarget = target;
            _inCombat = true;
            OnCombatStarted?.Invoke();

            ApexLogger.Log(ApexLogger.LogCategory.Combat, $"[CombatManager] Started attack on territory {target.Id}");
            return true;
        }

        /// <summary>
        /// Stop current attack
        /// </summary>
        public void StopAttack()
        {
            _currentTarget = null;
            _inCombat = false;
            OnCombatEnded?.Invoke();
            ApexLogger.Log(ApexLogger.LogCategory.Combat, "[CombatManager] Attack stopped");
        }

        /// <summary>
        /// Execute an attack on the current target
        /// </summary>
        public async Task<AttackResult> ExecuteAttack(AttackType type)
        {
            if (_currentTarget == null)
            {
                return new AttackResult(false, "No target selected!");
            }

            // Check cooldown
            if (_cooldowns[type] > 0)
            {
                return new AttackResult(false, $"Attack on cooldown! {_cooldowns[type]:F1}s remaining");
            }

            // Calculate damage
            int damage = CalculateDamage(type);

            // Apply damage via TerritoryManager
            if (TerritoryManager.Instance != null)
            {
                var result = await TerritoryManager.Instance.AttackTerritory(_currentTarget.Id, damage);

                if (result.Success)
                {
                    // Set cooldown
                    _cooldowns[type] = GetCooldown(type);

                    // Award XP
                    PlayerManager.Instance?.AwardExperience(damage / 2);

                    OnDamageDealt?.Invoke(_currentTarget.Id, damage);

                    if (result.Conquered)
                    {
                        OnTerritoryConquered?.Invoke(_currentTarget.Id);
                        StopAttack();
                    }
                }

                return new AttackResult(result.Success, result.Message, damage, result.Conquered);
            }

            return new AttackResult(false, "Combat system not available");
        }

        private int CalculateDamage(AttackType type)
        {
            float baseDamage = type switch
            {
                AttackType.QuickStrike => quickStrikeDamage,
                AttackType.HeavyAttack => heavyAttackDamage,
                AttackType.Siege => siegeDamage,
                AttackType.Rally => 0,
                _ => quickStrikeDamage
            };

            // Add randomness
            float variance = UnityEngine.Random.Range(0.8f, 1.2f);
            
            // Player level bonus
            int playerLevel = PlayerManager.Instance?.CurrentPlayer?.Level ?? 1;
            float levelBonus = 1f + (playerLevel - 1) * 0.05f; // 5% per level

            return Mathf.RoundToInt(baseDamage * variance * levelBonus);
        }

        private float GetCooldown(AttackType type)
        {
            return type switch
            {
                AttackType.QuickStrike => 0f,
                AttackType.HeavyAttack => heavyAttackCooldown,
                AttackType.Siege => siegeCooldown,
                AttackType.Rally => 60f,
                _ => 0f
            };
        }

        /// <summary>
        /// Get remaining cooldown for an attack type
        /// </summary>
        public float GetCooldownRemaining(AttackType type)
        {
            return Mathf.Max(0, _cooldowns[type]);
        }

        /// <summary>
        /// Check if an attack type is ready
        /// </summary>
        public bool IsAttackReady(AttackType type)
        {
            return _cooldowns[type] <= 0;
        }

        #endregion

        #region Defense

        private void ProcessDefenses()
        {
            // Auto-turret logic would go here
            // For now, this is a placeholder for Phase 3
        }

        /// <summary>
        /// Calculate defense rating for a territory
        /// </summary>
        public int CalculateDefenseRating(Territory.Territory territory)
        {
            int baseDefense = territory.Level * 10;
            
            // Add bonus from defensive structures
            int structureBonus = 0;
            if (Building.BuildingManager.Instance != null)
            {
                var blocks = Building.BuildingManager.Instance.GetBlocksInTerritory(territory.Id);
                foreach (var block in blocks)
                {
                    structureBonus += block.Type switch
                    {
                        Building.BlockType.Wall => 5,      // Walls add +5 defense each
                        Building.BlockType.Tower => 10,    // Towers add +10 defense
                        Building.BlockType.Turret => 15,   // Turrets add +15 defense
                        Building.BlockType.Gate => 3,      // Gates add +3 defense
                        _ => 0
                    };
                }
            }

            return baseDefense + structureBonus;
        }

        #endregion

        #region Raid Windows

        /// <summary>
        /// Check if raiding is currently allowed (time-based)
        /// </summary>
        public bool IsRaidWindowOpen()
        {
            // Raids only allowed during certain hours
            // This encourages scheduled battles and protects players during off-hours
            
            int hour = DateTime.Now.Hour;
            
            // Raid window: 6 PM to 10 PM local time
            return hour >= 18 && hour < 22;
        }

        /// <summary>
        /// Get time until next raid window
        /// </summary>
        public TimeSpan GetTimeUntilRaidWindow()
        {
            DateTime now = DateTime.Now;
            DateTime nextWindow;

            if (now.Hour < 18)
            {
                // Today at 6 PM
                nextWindow = now.Date.AddHours(18);
            }
            else if (now.Hour >= 22)
            {
                // Tomorrow at 6 PM
                nextWindow = now.Date.AddDays(1).AddHours(18);
            }
            else
            {
                // We're in the window
                return TimeSpan.Zero;
            }

            return nextWindow - now;
        }

        #endregion
    }

    /// <summary>
    /// Result of an attack attempt
    /// </summary>
    public class AttackResult
    {
        public bool Success;
        public string Message;
        public int DamageDealt;
        public bool Conquered;

        public AttackResult(bool success, string message, int damage = 0, bool conquered = false)
        {
            Success = success;
            Message = message;
            DamageDealt = damage;
            Conquered = conquered;
        }
    }
}
