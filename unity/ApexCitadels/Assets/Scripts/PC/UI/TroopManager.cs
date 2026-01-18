// ============================================================================
// APEX CITADELS - TROOP MANAGER
// Central manager for troop statistics, formations, and battle deployment
// ============================================================================
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ApexCitadels.Data;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Battle deployment entry
    /// </summary>
    [Serializable]
    public class DeploymentEntry
    {
        public TroopType Type;
        public int Count;
        public int FormationSlot; // Position in formation
    }

    /// <summary>
    /// Saved formation preset
    /// </summary>
    [Serializable]
    public class FormationPreset
    {
        public string Id;
        public string Name;
        public List<DeploymentEntry> Troops = new List<DeploymentEntry>();
        public DateTime CreatedAt;
        public int TotalPower;
    }

    /// <summary>
    /// Central manager for troops, formations, and deployment
    /// </summary>
    public class TroopManager : MonoBehaviour
    {
        public static TroopManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private int maxArmySize = 100;
        [SerializeField] private int maxFormationPresets = 5;
        [SerializeField] private int maxDeploymentSlots = 6;

        // Formation presets
        private List<FormationPreset> savedFormations = new List<FormationPreset>();
        private FormationPreset currentDeployment;

        // Events
        public event Action<int> OnArmySizeChanged;
        public event Action<int> OnArmyPowerChanged;
        public event Action<FormationPreset> OnFormationSaved;
        public event Action<FormationPreset> OnDeploymentChanged;

        // Persistence
        private const string FORMATIONS_KEY = "formation_presets";

        #region Initialization

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadFormations();
            SubscribeToTrainingEvents();
        }

        private void SubscribeToTrainingEvents()
        {
            if (TrainingQueueManager.Instance != null)
            {
                TrainingQueueManager.Instance.OnTroopsAdded += HandleTroopsAdded;
            }
        }

        private void HandleTroopsAdded(TroopType type, int count)
        {
            OnArmySizeChanged?.Invoke(GetTotalArmySize());
            OnArmyPowerChanged?.Invoke(GetTotalArmyPower());
        }

        #endregion

        #region Army Statistics

        /// <summary>
        /// Get current army size
        /// </summary>
        public int GetTotalArmySize()
        {
            return TrainingQueueManager.Instance?.GetTotalArmySize() ?? 0;
        }

        /// <summary>
        /// Get max army size
        /// </summary>
        public int MaxArmySize => maxArmySize;

        /// <summary>
        /// Get total army power
        /// </summary>
        public int GetTotalArmyPower()
        {
            return TrainingQueueManager.Instance?.GetTotalArmyPower() ?? 0;
        }

        /// <summary>
        /// Check if army is at capacity
        /// </summary>
        public bool IsArmyFull => GetTotalArmySize() >= maxArmySize;

        /// <summary>
        /// Get available army capacity
        /// </summary>
        public int AvailableCapacity => maxArmySize - GetTotalArmySize();

        /// <summary>
        /// Get troop breakdown by type
        /// </summary>
        public Dictionary<TroopType, int> GetTroopBreakdown()
        {
            return TrainingQueueManager.Instance?.GetAllTroops() ?? new Dictionary<TroopType, int>();
        }

        /// <summary>
        /// Get army composition (percentage by type)
        /// </summary>
        public Dictionary<TroopType, float> GetArmyComposition()
        {
            var breakdown = GetTroopBreakdown();
            int total = breakdown.Values.Sum();
            var composition = new Dictionary<TroopType, float>();

            foreach (var kvp in breakdown)
            {
                composition[kvp.Key] = total > 0 ? (float)kvp.Value / total : 0f;
            }

            return composition;
        }

        /// <summary>
        /// Get power contribution by type
        /// </summary>
        public Dictionary<TroopType, int> GetPowerBreakdown()
        {
            var breakdown = GetTroopBreakdown();
            var powerBreakdown = new Dictionary<TroopType, int>();

            foreach (var kvp in breakdown)
            {
                var troop = new Troop(kvp.Key, kvp.Value, 1);
                powerBreakdown[kvp.Key] = TroopConfig.CalculatePower(troop);
            }

            return powerBreakdown;
        }

        #endregion

        #region Formation Management

        /// <summary>
        /// Create a new formation preset
        /// </summary>
        public bool CreateFormation(string name, List<DeploymentEntry> troops)
        {
            if (savedFormations.Count >= maxFormationPresets)
            {
                Debug.LogWarning("[TroopManager] Max formations reached!");
                return false;
            }

            // Validate troop counts
            if (!ValidateDeployment(troops))
            {
                return false;
            }

            var formation = new FormationPreset
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Troops = new List<DeploymentEntry>(troops),
                CreatedAt = DateTime.UtcNow,
                TotalPower = CalculateFormationPower(troops)
            };

            savedFormations.Add(formation);
            SaveFormations();

            OnFormationSaved?.Invoke(formation);
            Debug.Log($"[TroopManager] Created formation: {name} with {troops.Sum(t => t.Count)} troops");
            return true;
        }

        /// <summary>
        /// Delete a formation preset
        /// </summary>
        public bool DeleteFormation(string formationId)
        {
            var formation = savedFormations.Find(f => f.Id == formationId);
            if (formation == null) return false;

            savedFormations.Remove(formation);
            SaveFormations();
            return true;
        }

        /// <summary>
        /// Get all saved formations
        /// </summary>
        public List<FormationPreset> GetFormations()
        {
            return new List<FormationPreset>(savedFormations);
        }

        /// <summary>
        /// Get formation by ID
        /// </summary>
        public FormationPreset GetFormation(string formationId)
        {
            return savedFormations.Find(f => f.Id == formationId);
        }

        #endregion

        #region Deployment

        /// <summary>
        /// Set current deployment for battle
        /// </summary>
        public bool SetDeployment(List<DeploymentEntry> troops)
        {
            if (!ValidateDeployment(troops))
            {
                return false;
            }

            currentDeployment = new FormationPreset
            {
                Id = "current",
                Name = "Current Deployment",
                Troops = new List<DeploymentEntry>(troops),
                CreatedAt = DateTime.UtcNow,
                TotalPower = CalculateFormationPower(troops)
            };

            OnDeploymentChanged?.Invoke(currentDeployment);
            return true;
        }

        /// <summary>
        /// Set deployment from saved formation
        /// </summary>
        public bool SetDeploymentFromFormation(string formationId)
        {
            var formation = GetFormation(formationId);
            if (formation == null) return false;

            // Check if we have enough troops
            if (!ValidateDeployment(formation.Troops))
            {
                Debug.LogWarning("[TroopManager] Not enough troops for this formation!");
                return false;
            }

            return SetDeployment(formation.Troops);
        }

        /// <summary>
        /// Get current deployment
        /// </summary>
        public FormationPreset GetCurrentDeployment()
        {
            return currentDeployment;
        }

        /// <summary>
        /// Clear current deployment
        /// </summary>
        public void ClearDeployment()
        {
            currentDeployment = null;
            OnDeploymentChanged?.Invoke(null);
        }

        /// <summary>
        /// Validate deployment against available troops
        /// </summary>
        public bool ValidateDeployment(List<DeploymentEntry> troops)
        {
            if (troops == null || troops.Count == 0) return false;

            var available = GetTroopBreakdown();
            var required = new Dictionary<TroopType, int>();

            // Sum up required troops by type
            foreach (var entry in troops)
            {
                if (!required.ContainsKey(entry.Type))
                    required[entry.Type] = 0;
                required[entry.Type] += entry.Count;
            }

            // Check if we have enough
            foreach (var kvp in required)
            {
                int availableCount = available.TryGetValue(kvp.Key, out int a) ? a : 0;
                if (availableCount < kvp.Value)
                {
                    Debug.LogWarning($"[TroopManager] Not enough {kvp.Key}: need {kvp.Value}, have {availableCount}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Calculate total power of a deployment
        /// </summary>
        public int CalculateFormationPower(List<DeploymentEntry> troops)
        {
            int totalPower = 0;
            foreach (var entry in troops)
            {
                var troop = new Troop(entry.Type, entry.Count, 1);
                totalPower += TroopConfig.CalculatePower(troop);
            }
            return totalPower;
        }

        #endregion

        #region Quick Deploy

        /// <summary>
        /// Create a balanced deployment using all available troops
        /// </summary>
        public FormationPreset CreateBalancedDeployment()
        {
            var available = GetTroopBreakdown();
            var troops = new List<DeploymentEntry>();

            int slot = 0;
            foreach (var kvp in available.Where(k => k.Value > 0))
            {
                troops.Add(new DeploymentEntry
                {
                    Type = kvp.Key,
                    Count = kvp.Value,
                    FormationSlot = slot++
                });
            }

            return new FormationPreset
            {
                Id = "balanced",
                Name = "Balanced Formation",
                Troops = troops,
                TotalPower = CalculateFormationPower(troops)
            };
        }

        /// <summary>
        /// Create an offensive deployment (prioritize attack)
        /// </summary>
        public FormationPreset CreateOffensiveDeployment()
        {
            var available = GetTroopBreakdown();
            var troops = new List<DeploymentEntry>();

            // Prioritize high-attack troops
            var priority = new[] { TroopType.Siege, TroopType.Mage, TroopType.Cavalry, TroopType.Archer, TroopType.Infantry, TroopType.Guardian };

            int slot = 0;
            foreach (var type in priority)
            {
                if (available.TryGetValue(type, out int count) && count > 0)
                {
                    troops.Add(new DeploymentEntry
                    {
                        Type = type,
                        Count = count,
                        FormationSlot = slot++
                    });
                }
            }

            return new FormationPreset
            {
                Id = "offensive",
                Name = "Offensive Formation",
                Troops = troops,
                TotalPower = CalculateFormationPower(troops)
            };
        }

        /// <summary>
        /// Create a defensive deployment (prioritize defense)
        /// </summary>
        public FormationPreset CreateDefensiveDeployment()
        {
            var available = GetTroopBreakdown();
            var troops = new List<DeploymentEntry>();

            // Prioritize high-defense troops
            var priority = new[] { TroopType.Guardian, TroopType.Infantry, TroopType.Cavalry, TroopType.Archer, TroopType.Mage, TroopType.Siege };

            int slot = 0;
            foreach (var type in priority)
            {
                if (available.TryGetValue(type, out int count) && count > 0)
                {
                    troops.Add(new DeploymentEntry
                    {
                        Type = type,
                        Count = count,
                        FormationSlot = slot++
                    });
                }
            }

            return new FormationPreset
            {
                Id = "defensive",
                Name = "Defensive Formation",
                Troops = troops,
                TotalPower = CalculateFormationPower(troops)
            };
        }

        #endregion

        #region Battle Integration

        /// <summary>
        /// Deploy troops for battle (removes from available pool)
        /// </summary>
        public bool DeployForBattle(List<DeploymentEntry> troops)
        {
            if (!ValidateDeployment(troops)) return false;

            // Remove troops from available pool
            foreach (var entry in troops)
            {
                TrainingQueueManager.Instance?.RemoveTroops(entry.Type, entry.Count);
            }

            Debug.Log($"[TroopManager] Deployed {troops.Sum(t => t.Count)} troops for battle");
            return true;
        }

        /// <summary>
        /// Return surviving troops after battle
        /// </summary>
        public void ReturnFromBattle(List<DeploymentEntry> survivors)
        {
            foreach (var entry in survivors)
            {
                TrainingQueueManager.Instance?.AddTroops(entry.Type, entry.Count);
            }

            Debug.Log($"[TroopManager] Returned {survivors.Sum(t => t.Count)} troops from battle");
        }

        /// <summary>
        /// Convert deployment to BattleFormation for combat system
        /// </summary>
        public BattleFormation ConvertToBattleFormation(FormationPreset formation)
        {
            var battleFormation = new BattleFormation();
            battleFormation.Troops = new List<Troop>();

            foreach (var entry in formation.Troops)
            {
                battleFormation.Troops.Add(new Troop(entry.Type, entry.Count, 1));
            }

            return battleFormation;
        }

        #endregion

        #region Persistence

        private void SaveFormations()
        {
            try
            {
                var data = new FormationSaveData
                {
                    formations = savedFormations
                };
                string json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString(FORMATIONS_KEY, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TroopManager] Save failed: {ex.Message}");
            }
        }

        private void LoadFormations()
        {
            try
            {
                if (PlayerPrefs.HasKey(FORMATIONS_KEY))
                {
                    string json = PlayerPrefs.GetString(FORMATIONS_KEY);
                    var data = JsonUtility.FromJson<FormationSaveData>(json);
                    if (data?.formations != null)
                    {
                        savedFormations = data.formations;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TroopManager] Load failed: {ex.Message}");
            }
        }

        #endregion

        #region Debug / Testing

        /// <summary>
        /// Add test troops for debugging
        /// </summary>
        [ContextMenu("Add Test Troops")]
        public void AddTestTroops()
        {
            TrainingQueueManager.Instance?.AddTroops(TroopType.Infantry, 20);
            TrainingQueueManager.Instance?.AddTroops(TroopType.Archer, 15);
            TrainingQueueManager.Instance?.AddTroops(TroopType.Cavalry, 10);
            TrainingQueueManager.Instance?.AddTroops(TroopType.Guardian, 5);
            TrainingQueueManager.Instance?.AddTroops(TroopType.Mage, 5);
            TrainingQueueManager.Instance?.AddTroops(TroopType.Siege, 3);
        }

        /// <summary>
        /// Log army stats
        /// </summary>
        [ContextMenu("Log Army Stats")]
        public void LogArmyStats()
        {
            Debug.Log($"=== Army Stats ===");
            Debug.Log($"Total Size: {GetTotalArmySize()}/{maxArmySize}");
            Debug.Log($"Total Power: {GetTotalArmyPower()}");

            var breakdown = GetTroopBreakdown();
            foreach (var kvp in breakdown)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }
        }

        #endregion
    }

    #region Save Data

    [Serializable]
    public class FormationSaveData
    {
        public List<FormationPreset> formations;
    }

    #endregion
}
