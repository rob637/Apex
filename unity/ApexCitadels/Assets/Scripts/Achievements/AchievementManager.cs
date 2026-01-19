using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Data;
using ApexCitadels.Player;
using ApexCitadels.Territory;
using ApexCitadels.Notifications;
using ApexCitadels.UI;
using Firebase.Firestore;

namespace ApexCitadels.Achievements
{
    /// <summary>
    /// Achievement categories
    /// </summary>
    public enum AchievementCategory
    {
        Territory,
        Building,
        Combat,
        Social,
        Resources,
        Exploration,
        Milestones
    }

    /// <summary>
    /// Achievement data
    /// </summary>
    [Serializable]
    public class Achievement
    {
        public string Id;
        public string Name;
        public string Description;
        public AchievementCategory Category;
        public string IconName;
        
        // Progress
        public int TargetValue;
        public int CurrentValue;
        public bool IsUnlocked;
        public DateTime UnlockedAt;
        
        // Rewards
        public int XPReward;
        public int GemReward;
        public string TitleReward; // Special title player can display

        public float Progress => (float)CurrentValue / TargetValue;
        public bool IsComplete => CurrentValue >= TargetValue;

        public Achievement() { }

        public Achievement(string id, string name, string description, 
                          AchievementCategory category, int target,
                          int xpReward = 100, int gemReward = 0, string titleReward = "")
        {
            Id = id;
            Name = name;
            Description = description;
            Category = category;
            TargetValue = target;
            CurrentValue = 0;
            IsUnlocked = false;
            XPReward = xpReward;
            GemReward = gemReward;
            TitleReward = titleReward;
            IconName = GetIconForCategory(category);
        }

        private static string GetIconForCategory(AchievementCategory category)
        {
            return category switch
            {
                AchievementCategory.Territory => GameIcons.Citadel,
                AchievementCategory.Building => GameIcons.Build,
                AchievementCategory.Combat => GameIcons.Battle,
                AchievementCategory.Social => GameIcons.Alliance,
                AchievementCategory.Resources => GameIcons.Gem,
                AchievementCategory.Exploration => GameIcons.Map,
                AchievementCategory.Milestones => GameIcons.Trophy,
                _ => GameIcons.Star
            };
        }
    }

    /// <summary>
    /// Manages achievements and progress tracking
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        // Events
        public event Action<Achievement> OnAchievementUnlocked;
        public event Action<Achievement, int> OnAchievementProgress;

        // State
        private Dictionary<string, Achievement> _achievements = new Dictionary<string, Achievement>();

        public IReadOnlyDictionary<string, Achievement> AllAchievements => _achievements;
        public int TotalUnlocked => GetUnlockedCount();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeAchievements();
            LoadProgress();
            SubscribeToEvents();
        }

        #region Achievement Definitions

        private void InitializeAchievements()
        {
            // Territory Achievements
            AddAchievement(new Achievement("first_claim", "Landowner", 
                "Claim your first territory", AchievementCategory.Territory, 1, 100, 10));
            AddAchievement(new Achievement("claim_5", "Land Baron", 
                "Own 5 territories", AchievementCategory.Territory, 5, 500, 25));
            AddAchievement(new Achievement("claim_25", "Territory Tycoon", 
                "Own 25 territories", AchievementCategory.Territory, 25, 2500, 100, "Tycoon"));
            AddAchievement(new Achievement("claim_100", "Dominator", 
                "Own 100 territories", AchievementCategory.Territory, 100, 10000, 500, "Dominator"));

            // Building Achievements
            AddAchievement(new Achievement("first_block", "Builder", 
                "Place your first block", AchievementCategory.Building, 1, 50));
            AddAchievement(new Achievement("blocks_100", "Architect", 
                "Place 100 blocks", AchievementCategory.Building, 100, 500, 25));
            AddAchievement(new Achievement("blocks_1000", "Master Builder", 
                "Place 1000 blocks", AchievementCategory.Building, 1000, 2500, 100, "Architect"));
            AddAchievement(new Achievement("tall_tower", "Reach for the Sky", 
                "Build a structure 10 blocks tall", AchievementCategory.Building, 10, 250, 25));

            // Combat Achievements
            AddAchievement(new Achievement("first_attack", "Aggressor", 
                "Attack your first enemy territory", AchievementCategory.Combat, 1, 100));
            AddAchievement(new Achievement("capture_5", "Conqueror", 
                "Capture 5 enemy territories", AchievementCategory.Combat, 5, 500, 50));
            AddAchievement(new Achievement("capture_50", "Warlord", 
                "Capture 50 enemy territories", AchievementCategory.Combat, 50, 2500, 200, "Warlord"));
            AddAchievement(new Achievement("defend_10", "Defender", 
                "Successfully defend 10 attacks", AchievementCategory.Combat, 10, 500, 50));
            AddAchievement(new Achievement("defend_100", "Fortress", 
                "Successfully defend 100 attacks", AchievementCategory.Combat, 100, 5000, 250, "Unbreakable"));

            // Social Achievements
            AddAchievement(new Achievement("join_alliance", "Team Player", 
                "Join an alliance", AchievementCategory.Social, 1, 100, 10));
            AddAchievement(new Achievement("create_alliance", "Leader", 
                "Create an alliance", AchievementCategory.Social, 1, 500, 50, "Founder"));
            AddAchievement(new Achievement("alliance_war_win", "Victorious", 
                "Win an alliance war", AchievementCategory.Social, 1, 1000, 100));
            AddAchievement(new Achievement("invite_5", "Recruiter", 
                "Invite 5 players to your alliance", AchievementCategory.Social, 5, 250, 25));

            // Resource Achievements
            AddAchievement(new Achievement("harvest_1000", "Gatherer", 
                "Harvest 1000 resources", AchievementCategory.Resources, 1000, 250));
            AddAchievement(new Achievement("harvest_10000", "Hoarder", 
                "Harvest 10000 resources", AchievementCategory.Resources, 10000, 1000, 50));
            AddAchievement(new Achievement("collect_gems_100", "Gem Hunter", 
                "Collect 100 gems", AchievementCategory.Resources, 100, 500, 25));

            // Exploration Achievements
            AddAchievement(new Achievement("walk_1km", "Walker", 
                "Walk 1 kilometer", AchievementCategory.Exploration, 1000, 100));
            AddAchievement(new Achievement("walk_10km", "Trekker", 
                "Walk 10 kilometers", AchievementCategory.Exploration, 10000, 500, 25));
            AddAchievement(new Achievement("walk_100km", "Marathon", 
                "Walk 100 kilometers", AchievementCategory.Exploration, 100000, 2500, 100, "Explorer"));
            AddAchievement(new Achievement("discover_nodes_50", "Prospector", 
                "Discover 50 resource nodes", AchievementCategory.Exploration, 50, 500, 25));

            // Milestone Achievements
            AddAchievement(new Achievement("level_10", "Apprentice", 
                "Reach level 10", AchievementCategory.Milestones, 10, 500, 50));
            AddAchievement(new Achievement("level_25", "Veteran", 
                "Reach level 25", AchievementCategory.Milestones, 25, 1500, 100, "Veteran"));
            AddAchievement(new Achievement("level_50", "Elite", 
                "Reach level 50", AchievementCategory.Milestones, 50, 5000, 250, "Elite"));
            AddAchievement(new Achievement("level_100", "Legendary", 
                "Reach level 100", AchievementCategory.Milestones, 100, 25000, 1000, "Legend"));
            AddAchievement(new Achievement("play_7_days", "Dedicated", 
                "Play 7 days in a row", AchievementCategory.Milestones, 7, 500, 50));
            AddAchievement(new Achievement("play_30_days", "Committed", 
                "Play 30 days in a row", AchievementCategory.Milestones, 30, 2500, 250, "Committed"));

            Debug.Log($"[AchievementManager] Initialized {_achievements.Count} achievements");
        }

        private void AddAchievement(Achievement achievement)
        {
            _achievements[achievement.Id] = achievement;
        }

        #endregion

        #region Progress Tracking

        /// <summary>
        /// Update progress on an achievement
        /// </summary>
        public void UpdateProgress(string achievementId, int value)
        {
            if (!_achievements.TryGetValue(achievementId, out var achievement))
            {
                Debug.LogWarning($"[AchievementManager] Unknown achievement: {achievementId}");
                return;
            }

            if (achievement.IsUnlocked) return;

            int oldValue = achievement.CurrentValue;
            achievement.CurrentValue = Math.Min(value, achievement.TargetValue);

            if (achievement.CurrentValue != oldValue)
            {
                OnAchievementProgress?.Invoke(achievement, achievement.CurrentValue);

                if (achievement.IsComplete)
                {
                    UnlockAchievement(achievement);
                }
            }
        }

        /// <summary>
        /// Increment progress on an achievement
        /// </summary>
        public void IncrementProgress(string achievementId, int amount = 1)
        {
            if (!_achievements.TryGetValue(achievementId, out var achievement))
            {
                return;
            }

            UpdateProgress(achievementId, achievement.CurrentValue + amount);
        }

        private void UnlockAchievement(Achievement achievement)
        {
            if (achievement.IsUnlocked) return;

            achievement.IsUnlocked = true;
            achievement.UnlockedAt = DateTime.UtcNow;

            // Grant rewards
            if (achievement.XPReward > 0)
            {
                PlayerManager.Instance?.AwardExperience(achievement.XPReward);
            }
            if (achievement.GemReward > 0)
            {
                PlayerManager.Instance?.AddResource(ResourceType.Gems, achievement.GemReward);
            }

            // Show notification
            NotificationManager.Instance?.AddNotification(
                NotificationType.Achievement,
                "Achievement Unlocked!",
                $"{achievement.IconName} {achievement.Name}\n{achievement.Description}"
            );

            OnAchievementUnlocked?.Invoke(achievement);
            SaveProgress();

            Debug.Log($"[AchievementManager] Unlocked: {achievement.Name}!");
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            // Territory events
            if (TerritoryManager.Instance != null)
            {
                TerritoryManager.Instance.OnTerritoryClaimed += (t) =>
                {
                    IncrementProgress("first_claim");
                    int owned = PlayerManager.Instance?.CurrentPlayer?.TerritoriesOwned ?? 0;
                    UpdateProgress("claim_5", owned);
                    UpdateProgress("claim_25", owned);
                    UpdateProgress("claim_100", owned);
                };

                TerritoryManager.Instance.OnTerritoryLost += (t) =>
                {
                    int owned = PlayerManager.Instance?.CurrentPlayer?.TerritoriesOwned ?? 0;
                    UpdateProgress("claim_5", owned);
                    UpdateProgress("claim_25", owned);
                    UpdateProgress("claim_100", owned);
                };
            }

            // Building events
            if (Building.BuildingManager.Instance != null)
            {
                Building.BuildingManager.Instance.OnBlockPlaced += (block) =>
                {
                    IncrementProgress("first_block");
                    int placed = PlayerManager.Instance?.CurrentPlayer?.BuildingsPlaced ?? 0;
                    UpdateProgress("blocks_100", placed);
                    UpdateProgress("blocks_1000", placed);
                };
            }

            // Combat events
            if (Combat.CombatManager.Instance != null)
            {
                Combat.CombatManager.Instance.OnTerritoryConquered += (id) =>
                {
                    IncrementProgress("capture_5");
                    IncrementProgress("capture_50");
                };
            }

            // Player events
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.OnLevelUp += (level) =>
                {
                    UpdateProgress("level_10", level);
                    UpdateProgress("level_25", level);
                    UpdateProgress("level_50", level);
                    UpdateProgress("level_100", level);
                };
            }

            // Resource events
            if (Resources.ResourceManager.Instance != null)
            {
                Resources.ResourceManager.Instance.OnResourceHarvested += (type, amount) =>
                {
                    IncrementProgress("harvest_1000", amount);
                    IncrementProgress("harvest_10000", amount);
                    if (type == ResourceType.Gems)
                    {
                        IncrementProgress("collect_gems_100", amount);
                    }
                };

                Resources.ResourceManager.Instance.OnNodeDiscovered += (node) =>
                {
                    IncrementProgress("discover_nodes_50");
                };
            }

            // Alliance events
            if (Alliance.AllianceManager.Instance != null)
            {
                Alliance.AllianceManager.Instance.OnAllianceJoined += (a) =>
                {
                    IncrementProgress("join_alliance");
                };
            }
        }

        #endregion

        #region Queries

        /// <summary>
        /// Get achievements by category
        /// </summary>
        public List<Achievement> GetByCategory(AchievementCategory category)
        {
            var list = new List<Achievement>();
            foreach (var kvp in _achievements)
            {
                if (kvp.Value.Category == category)
                {
                    list.Add(kvp.Value);
                }
            }
            return list;
        }

        /// <summary>
        /// Get all unlocked achievements
        /// </summary>
        public List<Achievement> GetUnlocked()
        {
            var list = new List<Achievement>();
            foreach (var kvp in _achievements)
            {
                if (kvp.Value.IsUnlocked)
                {
                    list.Add(kvp.Value);
                }
            }
            return list;
        }

        /// <summary>
        /// Get achievements in progress (partially complete)
        /// </summary>
        public List<Achievement> GetInProgress()
        {
            var list = new List<Achievement>();
            foreach (var kvp in _achievements)
            {
                if (!kvp.Value.IsUnlocked && kvp.Value.CurrentValue > 0)
                {
                    list.Add(kvp.Value);
                }
            }
            return list;
        }

        private int GetUnlockedCount()
        {
            int count = 0;
            foreach (var kvp in _achievements)
            {
                if (kvp.Value.IsUnlocked) count++;
            }
            return count;
        }

        /// <summary>
        /// Get total achievement points earned
        /// </summary>
        public int GetTotalPoints()
        {
            int total = 0;
            foreach (var kvp in _achievements)
            {
                if (kvp.Value.IsUnlocked)
                {
                    total += kvp.Value.XPReward;
                }
            }
            return total;
        }

        #endregion

        #region Persistence

        [Serializable]
        private class AchievementProgressWrapper
        {
            public List<AchievementProgress> achievements;
        }

        [Serializable]
        private class AchievementProgress
        {
            public string id;
            public int currentValue;
            public bool isUnlocked;
            public string unlockedAt;
        }

        private void LoadProgress()
        {
            // First try local storage for immediate load
            string json = PlayerPrefs.GetString("achievements", "");
            if (!string.IsNullOrEmpty(json))
            {
                DeserializeProgress(json);
            }

            // Then sync with Firebase in background
            SyncProgressFromCloud();
        }

        private void DeserializeProgress(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<AchievementProgressWrapper>(json);
                if (wrapper?.achievements != null)
                {
                    foreach (var progress in wrapper.achievements)
                    {
                        if (_achievements.TryGetValue(progress.id, out var achievement))
                        {
                            achievement.CurrentValue = progress.currentValue;
                            achievement.IsUnlocked = progress.isUnlocked;
                            if (!string.IsNullOrEmpty(progress.unlockedAt) &&
                                DateTime.TryParse(progress.unlockedAt, out var unlockedAt))
                            {
                                achievement.UnlockedAt = unlockedAt;
                            }
                        }
                    }
                    Debug.Log($"[AchievementManager] Loaded {wrapper.achievements.Count} achievement progresses");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AchievementManager] Failed to deserialize progress: {ex.Message}");
            }
        }

        private void SaveProgress()
        {
            try
            {
                var wrapper = new AchievementProgressWrapper
                {
                    achievements = new List<AchievementProgress>()
                };

                foreach (var kvp in _achievements)
                {
                    var a = kvp.Value;
                    if (a.CurrentValue > 0 || a.IsUnlocked)
                    {
                        wrapper.achievements.Add(new AchievementProgress
                        {
                            id = a.Id,
                            currentValue = a.CurrentValue,
                            isUnlocked = a.IsUnlocked,
                            unlockedAt = a.IsUnlocked ? a.UnlockedAt.ToString("O") : ""
                        });
                    }
                }

                string json = JsonUtility.ToJson(wrapper);
                PlayerPrefs.SetString("achievements", json);
                PlayerPrefs.Save();
                Debug.Log($"[AchievementManager] Saved {wrapper.achievements.Count} achievement progresses");

                // Also sync to cloud
                SyncProgressToCloud(wrapper);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AchievementManager] Failed to save progress: {ex.Message}");
            }
        }

        private async void SyncProgressFromCloud()
        {
            var playerId = PlayerManager.Instance?.GetCurrentPlayerId();
            if (string.IsNullOrEmpty(playerId)) return;

            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                var docRef = db.Collection("user_achievements").Document(playerId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    if (snapshot.TryGetValue("achievements", out List<object> achievementsList))
                    {
                        foreach (var obj in achievementsList)
                        {
                            if (obj is Dictionary<string, object> dict)
                            {
                                string id = dict["id"]?.ToString();
                                if (_achievements.TryGetValue(id, out var achievement))
                                {
                                    int cloudValue = Convert.ToInt32(dict["currentValue"]);
                                    bool cloudUnlocked = Convert.ToBoolean(dict["isUnlocked"]);

                                    // Take the max progress
                                    if (cloudValue > achievement.CurrentValue)
                                    {
                                        achievement.CurrentValue = cloudValue;
                                    }

                                    if (cloudUnlocked && !achievement.IsUnlocked)
                                    {
                                        achievement.IsUnlocked = true;
                                        if (dict.TryGetValue("unlockedAt", out object unlockedAtObj) &&
                                            unlockedAtObj is Timestamp ts)
                                        {
                                            achievement.UnlockedAt = ts.ToDateTime();
                                        }
                                    }
                                }
                            }
                        }
                        Debug.Log("[AchievementManager] Synced progress from cloud");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AchievementManager] Failed to sync from cloud: {ex.Message}");
            }
        }

        private async void SyncProgressToCloud(AchievementProgressWrapper wrapper)
        {
            var playerId = PlayerManager.Instance?.GetCurrentPlayerId();
            if (string.IsNullOrEmpty(playerId)) return;

            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                var docRef = db.Collection("user_achievements").Document(playerId);

                var achievementData = new List<Dictionary<string, object>>();
                foreach (var a in wrapper.achievements)
                {
                    var dict = new Dictionary<string, object>
                    {
                        { "id", a.id },
                        { "currentValue", a.currentValue },
                        { "isUnlocked", a.isUnlocked }
                    };

                    if (!string.IsNullOrEmpty(a.unlockedAt) && DateTime.TryParse(a.unlockedAt, out var dt))
                    {
                        dict["unlockedAt"] = Timestamp.FromDateTime(dt.ToUniversalTime());
                    }

                    achievementData.Add(dict);
                }

                await docRef.SetAsync(new Dictionary<string, object>
                {
                    { "playerId", playerId },
                    { "achievements", achievementData },
                    { "totalPoints", GetTotalPoints() },
                    { "updatedAt", Timestamp.GetCurrentTimestamp() }
                }, SetOptions.MergeAll);

                Debug.Log("[AchievementManager] Synced progress to cloud");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AchievementManager] Failed to sync to cloud: {ex.Message}");
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveProgress();
        }

        #endregion
    }
}
