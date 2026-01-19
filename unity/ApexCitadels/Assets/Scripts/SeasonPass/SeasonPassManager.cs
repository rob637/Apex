using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if FIREBASE_ENABLED
using Firebase.Firestore;
using Firebase.Functions;
#endif
using Newtonsoft.Json;
using ApexCitadels.Core;

#pragma warning disable 0067

namespace ApexCitadels.SeasonPass
{
    /// <summary>
    /// Represents a season/battle pass
    /// </summary>
    [Serializable]
    public class Season
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalLevels { get; set; }
        public int PremiumPrice { get; set; }
        public List<SeasonReward> Rewards { get; set; }
        
        public bool IsActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;
        public TimeSpan TimeRemaining => EndDate - DateTime.UtcNow;
        public float ProgressPercentage(int currentLevel) => (float)currentLevel / TotalLevels * 100f;
    }

    /// <summary>
    /// Reward for a season level
    /// </summary>
    [Serializable]
    public class SeasonReward
    {
        public int Level { get; set; }
        public RewardItem FreeReward { get; set; }
        public RewardItem PremiumReward { get; set; }
    }

    [Serializable]
    public class RewardItem
    {
        public string Type { get; set; } // gold, gems, xp, skin, emote, banner, title
        public int Amount { get; set; }
        public string ItemId { get; set; } // For specific items
        public string DisplayName { get; set; }
        public string IconPath { get; set; }
    }

    /// <summary>
    /// Player's progress in a season
    /// </summary>
    [Serializable]
    public class SeasonProgress
    {
        public string SeasonId { get; set; }
        public string UserId { get; set; }
        public int CurrentLevel { get; set; }
        public int CurrentXp { get; set; }
        public int XpToNextLevel { get; set; }
        public int TotalXP => CurrentXp; // Alias for TotalXP
        public bool HasPremium { get; set; }
        public List<int> ClaimedFreeRewards { get; set; }
        public List<int> ClaimedPremiumRewards { get; set; }

        public float LevelProgress => XpToNextLevel > 0 ? (float)CurrentXp / XpToNextLevel : 0f;
    }

    /// <summary>
    /// Season challenge
    /// </summary>
    [Serializable]
    public class SeasonChallenge
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ChallengeType { get; set; } // daily, weekly, seasonal
        public int TargetAmount { get; set; }
        public int CurrentProgress { get; set; }
        public int XpReward { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsCompleted => CurrentProgress >= TargetAmount;
        public float ProgressPercentage => TargetAmount > 0 ? (float)CurrentProgress / TargetAmount * 100f : 0f;
    }

    /// <summary>
    /// Manages the season/battle pass system
    /// </summary>
    public class SeasonPassManager : MonoBehaviour
    {
        public static SeasonPassManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int baseXpPerLevel = 1000;
        [SerializeField] private float xpScalingFactor = 1.1f;

        // Events
        public event Action<Season> OnSeasonLoaded;
        public event Action<SeasonProgress> OnProgressUpdated;
        public event Action<int> OnLevelUp;
        public event Action<SeasonReward> OnRewardClaimed;
        public event Action<List<SeasonChallenge>> OnChallengesUpdated;
        public event Action OnPremiumPurchased;

        // State
        private Season _currentSeason;
        private SeasonProgress _progress;
        private List<SeasonChallenge> _challenges = new List<SeasonChallenge>();
#if FIREBASE_ENABLED
        private FirebaseFunctions _functions;
        private FirebaseFirestore _firestore;
#endif

        public Season CurrentSeason => _currentSeason;
        public SeasonProgress Progress => _progress;
        public List<SeasonChallenge> Challenges => _challenges;
        public bool HasPremium => _progress?.HasPremium ?? false;

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

#if FIREBASE_ENABLED
        private void Start()
        {
            _functions = FirebaseFunctions.DefaultInstance;
            _firestore = FirebaseFirestore.DefaultInstance;
            
            // Load current season
            LoadCurrentSeason();
        }

        /// <summary>
        /// Load the current active season
        /// </summary>
        public async Task LoadCurrentSeason()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getCurrentSeason");
                var result = await callable.CallAsync();
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("season"))
                {
                    var seasonJson = JsonConvert.SerializeObject(response["season"]);
                    _currentSeason = JsonConvert.DeserializeObject<Season>(seasonJson);
                    OnSeasonLoaded?.Invoke(_currentSeason);
                }

                if (response.ContainsKey("progress"))
                {
                    var progressJson = JsonConvert.SerializeObject(response["progress"]);
                    _progress = JsonConvert.DeserializeObject<SeasonProgress>(progressJson);
                    OnProgressUpdated?.Invoke(_progress);
                }

                if (response.ContainsKey("challenges"))
                {
                    var challengesJson = JsonConvert.SerializeObject(response["challenges"]);
                    _challenges = JsonConvert.DeserializeObject<List<SeasonChallenge>>(challengesJson);
                    OnChallengesUpdated?.Invoke(_challenges);
                }

                // Subscribe to progress updates
                SubscribeToProgressUpdates();
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to load current season: {e.Message}", ApexLogger.LogCategory.Events);
            }
        }

        /// <summary>
        /// Subscribe to real-time progress updates
        /// </summary>
        private void SubscribeToProgressUpdates()
        {
            if (_currentSeason == null) return;

            var userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            var progressDoc = _firestore
                .Collection("season_progress")
                .Document($"{_currentSeason.Id}_{userId}");

            progressDoc.Listen(snapshot =>
            {
                if (snapshot.Exists)
                {
                    var data = snapshot.ToDictionary();
                    var oldLevel = _progress?.CurrentLevel ?? 0;
                    
                    _progress = new SeasonProgress
                    {
                        SeasonId = _currentSeason.Id,
                        UserId = userId,
                        CurrentLevel = Convert.ToInt32(data.GetValueOrDefault("currentLevel", 0)),
                        CurrentXp = Convert.ToInt32(data.GetValueOrDefault("currentXp", 0)),
                        XpToNextLevel = Convert.ToInt32(data.GetValueOrDefault("xpToNextLevel", baseXpPerLevel)),
                        HasPremium = Convert.ToBoolean(data.GetValueOrDefault("hasPremium", false)),
                        ClaimedFreeRewards = data.GetValueOrDefault("claimedFreeRewards", new List<int>()) as List<int> ?? new List<int>(),
                        ClaimedPremiumRewards = data.GetValueOrDefault("claimedPremiumRewards", new List<int>()) as List<int> ?? new List<int>()
                    };

                    if (_progress.CurrentLevel > oldLevel)
                    {
                        OnLevelUp?.Invoke(_progress.CurrentLevel);
                    }

                    OnProgressUpdated?.Invoke(_progress);
                }
            });
        }

        /// <summary>
        /// Purchase the premium battle pass
        /// </summary>
        public async Task<bool> PurchasePremiumPass()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("purchasePremiumPass");
                var data = new Dictionary<string, object> { { "seasonId", _currentSeason.Id } };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("success") && Convert.ToBoolean(response["success"]))
                {
                    _progress.HasPremium = true;
                    OnPremiumPurchased?.Invoke();
                    OnProgressUpdated?.Invoke(_progress);
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to purchase premium pass: {e.Message}", ApexLogger.LogCategory.Events);
                return false;
            }
        }

        /// <summary>
        /// Claim a reward at a specific level
        /// </summary>
        public async Task<RewardItem> ClaimReward(int level, bool isPremium)
        {
            if (!CanClaimReward(level, isPremium))
            {
                ApexLogger.LogWarning("Cannot claim reward - requirements not met", ApexLogger.LogCategory.Events);
                return null;
            }

            try
            {
                var callable = _functions.GetHttpsCallable("claimSeasonReward");
                var data = new Dictionary<string, object>
                {
                    { "seasonId", _currentSeason.Id },
                    { "level", level },
                    { "isPremium", isPremium }
                };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("reward"))
                {
                    var rewardJson = JsonConvert.SerializeObject(response["reward"]);
                    var reward = JsonConvert.DeserializeObject<RewardItem>(rewardJson);

                    // Update local state
                    if (isPremium)
                    {
                        _progress.ClaimedPremiumRewards.Add(level);
                    }
                    else
                    {
                        _progress.ClaimedFreeRewards.Add(level);
                    }

                    var seasonReward = _currentSeason.Rewards.Find(r => r.Level == level);
                    OnRewardClaimed?.Invoke(seasonReward);
                    OnProgressUpdated?.Invoke(_progress);

                    return reward;
                }
                
                return null;
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to claim reward: {e.Message}", ApexLogger.LogCategory.Events);
                return null;
            }
        }

        /// <summary>
        /// Claim all available rewards up to current level
        /// </summary>
        public async Task<List<RewardItem>> ClaimAllAvailableRewards()
        {
            var claimedRewards = new List<RewardItem>();

            for (int level = 1; level <= _progress.CurrentLevel; level++)
            {
                // Claim free reward
                if (!_progress.ClaimedFreeRewards.Contains(level))
                {
                    var reward = await ClaimReward(level, false);
                    if (reward != null) claimedRewards.Add(reward);
                }

                // Claim premium reward if has premium
                if (_progress.HasPremium && !_progress.ClaimedPremiumRewards.Contains(level))
                {
                    var reward = await ClaimReward(level, true);
                    if (reward != null) claimedRewards.Add(reward);
                }
            }

            return claimedRewards;
        }

        /// <summary>
        /// Complete a challenge
        /// </summary>
        public async Task<bool> CompleteChallenge(string challengeId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("completeChallenge");
                var data = new Dictionary<string, object>
                {
                    { "seasonId", _currentSeason.Id },
                    { "challengeId", challengeId }
                };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("xpAwarded"))
                {
                    var xpAwarded = Convert.ToInt32(response["xpAwarded"]);
                    ApexLogger.Log($"Challenge completed! Awarded {xpAwarded} XP", ApexLogger.LogCategory.Events);
                    
                    // Remove completed challenge
                    _challenges.RemoveAll(c => c.Id == challengeId);
                    OnChallengesUpdated?.Invoke(_challenges);
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to complete challenge: {e.Message}", ApexLogger.LogCategory.Events);
                return false;
            }
        }

        /// <summary>
        /// Refresh progress from server
        /// </summary>
        public async void RefreshProgress()
        {
            await LoadCurrentSeason();
        }
#else
        private void Start()
        {
            ApexLogger.LogWarning("[SeasonPassManager] Firebase SDK not installed. Running in stub mode.", ApexLogger.LogCategory.Events);
        }

        public Task LoadCurrentSeason()
        {
            ApexLogger.LogWarning("[SeasonPassManager] Firebase SDK not installed. LoadCurrentSeason is a stub.", ApexLogger.LogCategory.Events);
            return Task.CompletedTask;
        }

        public Task<bool> PurchasePremiumPass()
        {
            ApexLogger.LogWarning("[SeasonPassManager] Firebase SDK not installed. PurchasePremiumPass is a stub.", ApexLogger.LogCategory.Events);
            return Task.FromResult(false);
        }

        public Task<RewardItem> ClaimReward(int level, bool isPremium)
        {
            ApexLogger.LogWarning("[SeasonPassManager] Firebase SDK not installed. ClaimReward is a stub.", ApexLogger.LogCategory.Events);
            return Task.FromResult<RewardItem>(null);
        }

        public Task<List<RewardItem>> ClaimAllAvailableRewards()
        {
            ApexLogger.LogWarning("[SeasonPassManager] Firebase SDK not installed. ClaimAllAvailableRewards is a stub.", ApexLogger.LogCategory.Events);
            return Task.FromResult(new List<RewardItem>());
        }

        public Task<bool> CompleteChallenge(string challengeId)
        {
            ApexLogger.LogWarning("[SeasonPassManager] Firebase SDK not installed. CompleteChallenge is a stub.", ApexLogger.LogCategory.Events);
            return Task.FromResult(false);
        }

        public void RefreshProgress()
        {
            ApexLogger.LogWarning("[SeasonPassManager] Firebase SDK not installed. RefreshProgress is a stub.", ApexLogger.LogCategory.Events);
        }
#endif

        /// <summary>
        /// Check if a reward can be claimed
        /// </summary>
        public bool CanClaimReward(int level, bool isPremium)
        {
            if (_progress == null || _currentSeason == null) return false;
            if (level > _progress.CurrentLevel) return false;
            if (isPremium && !_progress.HasPremium) return false;

            var claimedList = isPremium ? _progress.ClaimedPremiumRewards : _progress.ClaimedFreeRewards;
            return !claimedList.Contains(level);
        }

        /// <summary>
        /// Get unclaimed rewards count
        /// </summary>
        public int GetUnclaimedRewardsCount()
        {
            if (_progress == null) return 0;

            int count = 0;
            for (int level = 1; level <= _progress.CurrentLevel; level++)
            {
                if (!_progress.ClaimedFreeRewards.Contains(level)) count++;
                if (_progress.HasPremium && !_progress.ClaimedPremiumRewards.Contains(level)) count++;
            }
            return count;
        }

        /// <summary>
        /// Calculate XP needed for a specific level
        /// </summary>
        public int GetXpRequiredForLevel(int level)
        {
            return Mathf.RoundToInt(baseXpPerLevel * Mathf.Pow(xpScalingFactor, level - 1));
        }

        /// <summary>
        /// Get the reward for a specific level
        /// </summary>
        public SeasonReward GetRewardForLevel(int level)
        {
            return _currentSeason?.Rewards?.Find(r => r.Level == level);
        }

        // Compatibility properties
        public SeasonProgress CurrentProgress => _progress;
        public int GetXPForLevel(int level) => GetXpRequiredForLevel(level);
        public bool HasUnclaimedRewards => GetUnclaimedRewardsCount() > 0;
    }
}
