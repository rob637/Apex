// ============================================================================
// APEX CITADELS - TRAINING QUEUE MANAGER
// Persistent training queue with Firebase sync and offline support
// ============================================================================
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ApexCitadels.Data;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Manages training queue persistence and Firebase synchronization
    /// </summary>
    public class TrainingQueueManager : MonoBehaviour
    {
        public static TrainingQueueManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private int maxQueueSize = 5;
        [SerializeField] private int maxBatchSize = 10; // Max troops per queue item
        [SerializeField] private float saveInterval = 30f; // Auto-save interval

        // Training state
        private List<TrainingQueueItem> activeQueue = new List<TrainingQueueItem>();
        private Dictionary<TroopType, int> trainedTroops = new Dictionary<TroopType, int>();
        private float lastSaveTime;
        private bool isDirty = false;

        // Events
        public event Action<TrainingQueueItem> OnTrainingStarted;
        public event Action<TrainingQueueItem> OnTrainingProgress;
        public event Action<TrainingQueueItem> OnTrainingCompleted;
        public event Action<TrainingQueueItem> OnTrainingCancelled;
        public event Action<TroopType, int> OnTroopsAdded;
        public event Action OnQueueChanged;

        // Persistence keys
        private const string QUEUE_KEY = "training_queue";
        private const string TROOPS_KEY = "trained_troops";

        #region Initialization

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeTrainedTroops();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadFromStorage();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SaveToStorage();
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                SaveToStorage();
            }
        }

        private void InitializeTrainedTroops()
        {
            foreach (TroopType type in Enum.GetValues(typeof(TroopType)))
            {
                trainedTroops[type] = 0;
            }
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            UpdateActiveTraining();

            // Auto-save periodically
            if (isDirty && Time.time - lastSaveTime > saveInterval)
            {
                SaveToStorage();
            }
        }

        private void UpdateActiveTraining()
        {
            if (activeQueue.Count == 0) return;

            // Only first item trains at a time
            var current = activeQueue[0];
            if (current.IsComplete) return;

            current.ElapsedTime += Time.deltaTime;
            OnTrainingProgress?.Invoke(current);

            if (current.ElapsedTime >= current.TotalTimeSeconds)
            {
                CompleteTraining(current);
            }
        }

        #endregion

        #region Queue Management

        /// <summary>
        /// Add troops to training queue
        /// </summary>
        public bool QueueTraining(TroopType type, int count, bool deductResources = true)
        {
            // Validate
            if (activeQueue.Count >= maxQueueSize)
            {
                Debug.LogWarning("[TrainingQueue] Queue is full!");
                return false;
            }

            if (count <= 0 || count > maxBatchSize)
            {
                Debug.LogWarning($"[TrainingQueue] Invalid count: {count}");
                return false;
            }

            // Check and deduct resources
            if (deductResources)
            {
                var cost = TroopConfig.GetTotalTrainingCost(type, count);
                if (!TryDeductResources(cost))
                {
                    Debug.LogWarning("[TrainingQueue] Insufficient resources!");
                    return false;
                }
            }

            // Create queue item
            var def = TroopConfig.Definitions[type];
            var item = new TrainingQueueItem
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                Count = count,
                TotalTimeSeconds = def.TrainingTimeSeconds * count,
                ElapsedTime = 0,
                IsComplete = false
            };

            activeQueue.Add(item);
            isDirty = true;

            OnTrainingStarted?.Invoke(item);
            OnQueueChanged?.Invoke();

            Debug.Log($"[TrainingQueue] Queued {count} {type} ({item.TotalTimeSeconds}s total)");
            return true;
        }

        /// <summary>
        /// Cancel a training queue item
        /// </summary>
        public bool CancelTraining(string itemId, bool refundResources = true)
        {
            var item = activeQueue.Find(q => q.Id == itemId);
            if (item == null) return false;

            // Calculate refund (based on remaining progress)
            if (refundResources)
            {
                float remainingRatio = 1f - item.Progress;
                var totalCost = TroopConfig.GetTotalTrainingCost(item.Type, item.Count);
                RefundResources(totalCost, remainingRatio);
            }

            activeQueue.Remove(item);
            isDirty = true;

            OnTrainingCancelled?.Invoke(item);
            OnQueueChanged?.Invoke();

            Debug.Log($"[TrainingQueue] Cancelled {item.Type} training");
            return true;
        }

        /// <summary>
        /// Clear all training queue
        /// </summary>
        public void ClearQueue(bool refundAll = true)
        {
            foreach (var item in activeQueue.ToList())
            {
                CancelTraining(item.Id, refundAll);
            }
        }

        private void CompleteTraining(TrainingQueueItem item)
        {
            item.IsComplete = true;
            trainedTroops[item.Type] += item.Count;
            isDirty = true;

            OnTrainingCompleted?.Invoke(item);
            OnTroopsAdded?.Invoke(item.Type, item.Count);

            // Remove from queue
            activeQueue.Remove(item);
            OnQueueChanged?.Invoke();

            Debug.Log($"[TrainingQueue] Completed training: {item.Count} {item.Type}");
        }

        #endregion

        #region Resource Integration

        private bool TryDeductResources(ResourceCost cost)
        {
            if (ResourceInventory.Instance == null)
            {
                Debug.LogWarning("[TrainingQueue] ResourceInventory not found - skipping deduction");
                return true; // Allow training without resource check in dev
            }

            // Check if we have enough
            if (!ResourceInventory.Instance.CanAfford(cost.Stone, cost.Wood, cost.Iron, 
                cost.Crystal, cost.ArcaneEssence, cost.Gems))
            {
                return false;
            }

            // Deduct resources
            return ResourceInventory.Instance.Spend(cost.Stone, cost.Wood, cost.Iron,
                cost.Crystal, cost.ArcaneEssence, cost.Gems);
        }

        private void RefundResources(ResourceCost cost, float ratio)
        {
            if (ResourceInventory.Instance == null) return;

            int stoneRefund = Mathf.FloorToInt(cost.Stone * ratio);
            int woodRefund = Mathf.FloorToInt(cost.Wood * ratio);
            int ironRefund = Mathf.FloorToInt(cost.Iron * ratio);
            int crystalRefund = Mathf.FloorToInt(cost.Crystal * ratio);
            int arcaneRefund = Mathf.FloorToInt(cost.ArcaneEssence * ratio);
            int gemsRefund = Mathf.FloorToInt(cost.Gems * ratio);

            ResourceInventory.Instance.Add(stoneRefund, woodRefund, ironRefund,
                crystalRefund, arcaneRefund, gemsRefund);

            Debug.Log($"[TrainingQueue] Refunded {ratio:P0} of resources");
        }

        #endregion

        #region Troop Access

        /// <summary>
        /// Get current trained troop count
        /// </summary>
        public int GetTroopCount(TroopType type)
        {
            return trainedTroops.TryGetValue(type, out int count) ? count : 0;
        }

        /// <summary>
        /// Get all trained troops
        /// </summary>
        public Dictionary<TroopType, int> GetAllTroops()
        {
            return new Dictionary<TroopType, int>(trainedTroops);
        }

        /// <summary>
        /// Get total army size
        /// </summary>
        public int GetTotalArmySize()
        {
            return trainedTroops.Values.Sum();
        }

        /// <summary>
        /// Get total army power
        /// </summary>
        public int GetTotalArmyPower()
        {
            int totalPower = 0;
            foreach (var kvp in trainedTroops)
            {
                var troop = new Troop(kvp.Key, kvp.Value, 1);
                totalPower += TroopConfig.CalculatePower(troop);
            }
            return totalPower;
        }

        /// <summary>
        /// Remove troops (for battles, etc.)
        /// </summary>
        public bool RemoveTroops(TroopType type, int count)
        {
            if (trainedTroops[type] < count) return false;
            trainedTroops[type] -= count;
            isDirty = true;
            return true;
        }

        /// <summary>
        /// Add troops directly (for rewards, etc.)
        /// </summary>
        public void AddTroops(TroopType type, int count)
        {
            trainedTroops[type] += count;
            isDirty = true;
            OnTroopsAdded?.Invoke(type, count);
        }

        #endregion

        #region Queue Access

        /// <summary>
        /// Get current training queue
        /// </summary>
        public List<TrainingQueueItem> GetQueue()
        {
            return new List<TrainingQueueItem>(activeQueue);
        }

        /// <summary>
        /// Get queue size
        /// </summary>
        public int QueueCount => activeQueue.Count;

        /// <summary>
        /// Get max queue size
        /// </summary>
        public int MaxQueueSize => maxQueueSize;

        /// <summary>
        /// Check if queue is full
        /// </summary>
        public bool IsQueueFull => activeQueue.Count >= maxQueueSize;

        /// <summary>
        /// Get currently training item (first in queue)
        /// </summary>
        public TrainingQueueItem CurrentTraining => activeQueue.Count > 0 ? activeQueue[0] : null;

        /// <summary>
        /// Get estimated time to complete all training
        /// </summary>
        public int GetTotalQueueTime()
        {
            return activeQueue.Sum(q => q.RemainingSeconds);
        }

        #endregion

        #region Persistence

        private void SaveToStorage()
        {
            try
            {
                // Save queue
                var queueData = new TrainingQueueData
                {
                    items = activeQueue,
                    savedAt = DateTime.UtcNow.Ticks
                };
                string queueJson = JsonUtility.ToJson(queueData);
                PlayerPrefs.SetString(QUEUE_KEY, queueJson);

                // Save troops
                var troopData = new TrainedTroopsData();
                troopData.troops = new List<TroopSaveEntry>();
                foreach (var kvp in trainedTroops)
                {
                    troopData.troops.Add(new TroopSaveEntry
                    {
                        type = kvp.Key.ToString(),
                        count = kvp.Value
                    });
                }
                string troopJson = JsonUtility.ToJson(troopData);
                PlayerPrefs.SetString(TROOPS_KEY, troopJson);

                PlayerPrefs.Save();
                lastSaveTime = Time.time;
                isDirty = false;

                Debug.Log("[TrainingQueue] Saved to storage");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TrainingQueue] Save failed: {ex.Message}");
            }
        }

        private void LoadFromStorage()
        {
            try
            {
                // Load troops
                if (PlayerPrefs.HasKey(TROOPS_KEY))
                {
                    string troopJson = PlayerPrefs.GetString(TROOPS_KEY);
                    var troopData = JsonUtility.FromJson<TrainedTroopsData>(troopJson);
                    if (troopData?.troops != null)
                    {
                        foreach (var entry in troopData.troops)
                        {
                            if (Enum.TryParse<TroopType>(entry.type, out var type))
                            {
                                trainedTroops[type] = entry.count;
                            }
                        }
                    }
                }

                // Load queue
                if (PlayerPrefs.HasKey(QUEUE_KEY))
                {
                    string queueJson = PlayerPrefs.GetString(QUEUE_KEY);
                    var queueData = JsonUtility.FromJson<TrainingQueueData>(queueJson);
                    if (queueData?.items != null)
                    {
                        // Calculate elapsed time since save
                        DateTime savedTime = new DateTime(queueData.savedAt);
                        float elapsedSeconds = (float)(DateTime.UtcNow - savedTime).TotalSeconds;

                        // Apply elapsed time to queue items
                        activeQueue = new List<TrainingQueueItem>();
                        foreach (var item in queueData.items)
                        {
                            if (!item.IsComplete)
                            {
                                // Add offline progress
                                item.ElapsedTime += elapsedSeconds;
                                elapsedSeconds = Mathf.Max(0, elapsedSeconds - item.RemainingSeconds);

                                if (item.ElapsedTime >= item.TotalTimeSeconds)
                                {
                                    // Training completed while offline
                                    trainedTroops[item.Type] += item.Count;
                                    OnTroopsAdded?.Invoke(item.Type, item.Count);
                                }
                                else
                                {
                                    activeQueue.Add(item);
                                }
                            }
                        }
                    }
                }

                Debug.Log($"[TrainingQueue] Loaded from storage - {GetTotalArmySize()} troops, {activeQueue.Count} in queue");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TrainingQueue] Load failed: {ex.Message}");
            }
        }

        #endregion

        #region Speed Ups

        /// <summary>
        /// Speed up current training by a number of seconds
        /// </summary>
        public bool SpeedUp(int seconds, int gemCost = 0)
        {
            if (activeQueue.Count == 0) return false;

            // TODO: Deduct gems if gemCost > 0

            var current = activeQueue[0];
            current.ElapsedTime += seconds;

            if (current.ElapsedTime >= current.TotalTimeSeconds)
            {
                CompleteTraining(current);
            }

            isDirty = true;
            return true;
        }

        /// <summary>
        /// Instantly complete current training
        /// </summary>
        public bool InstantComplete(int gemCost = 0)
        {
            if (activeQueue.Count == 0) return false;

            // TODO: Deduct gems if gemCost > 0

            var current = activeQueue[0];
            CompleteTraining(current);
            return true;
        }

        #endregion
    }

    #region Save Data Structures

    [Serializable]
    public class TrainingQueueData
    {
        public List<TrainingQueueItem> items;
        public long savedAt;
    }

    [Serializable]
    public class TrainedTroopsData
    {
        public List<TroopSaveEntry> troops;
    }

    [Serializable]
    public class TroopSaveEntry
    {
        public string type;
        public int count;
    }

    #endregion
}
