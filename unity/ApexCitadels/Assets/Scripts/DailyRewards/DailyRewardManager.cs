using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Data;
using ApexCitadels.Player;
using ApexCitadels.Notifications;

namespace ApexCitadels.DailyRewards
{
    /// <summary>
    /// Daily reward tier
    /// </summary>
    [Serializable]
    public class DailyReward
    {
        public int Day; // 1-7, repeats weekly
        public ResourceType ResourceType;
        public int Amount;
        public bool IsPremium; // Bonus for premium users
        public string IconName;

        public DailyReward() { }

        public DailyReward(int day, ResourceType type, int amount, bool premium = false)
        {
            Day = day;
            ResourceType = type;
            Amount = amount;
            IsPremium = premium;
            IconName = GetIconForResource(type);
        }

        private static string GetIconForResource(ResourceType type)
        {
            return type switch
            {
                ResourceType.Stone => "🪨",
                ResourceType.Wood => "🪵",
                ResourceType.Metal => "⚙️",
                ResourceType.Crystal => "💎",
                ResourceType.Gems => "💠",
                _ => "📦"
            };
        }
    }

    /// <summary>
    /// Player's login streak data
    /// </summary>
    [Serializable]
    public class LoginStreak
    {
        public int CurrentStreak;
        public int LongestStreak;
        public DateTime LastLoginDate;
        public int TotalLogins;
        public bool ClaimedToday;
        public List<DateTime> LoginHistory = new List<DateTime>();

        public int DayInWeek => ((CurrentStreak - 1) % 7) + 1; // 1-7
        public bool CanClaimReward => !ClaimedToday && IsLoginValidForStreak();

        public bool IsLoginValidForStreak()
        {
            if (LastLoginDate == DateTime.MinValue) return true;
            
            DateTime today = DateTime.UtcNow.Date;
            DateTime lastDate = LastLoginDate.Date;
            
            // Same day - already logged in
            if (today == lastDate) return true;
            
            // Next day - continue streak
            if (today == lastDate.AddDays(1)) return true;
            
            // Missed day - streak broken (but can start new one)
            return true;
        }

        public bool WillExtendStreak()
        {
            if (LastLoginDate == DateTime.MinValue) return true;
            
            DateTime today = DateTime.UtcNow.Date;
            DateTime lastDate = LastLoginDate.Date;
            
            return today == lastDate.AddDays(1);
        }
    }

    /// <summary>
    /// Manages daily rewards and login streaks
    /// </summary>
    public class DailyRewardManager : MonoBehaviour
    {
        public static DailyRewardManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float streakBonusPercentage = 10f; // Per day
        [SerializeField] private int maxStreakBonus = 100; // Max 100% bonus

        // Events
        public event Action<DailyReward> OnRewardClaimed;
        public event Action<int> OnStreakUpdated;
        public event Action OnStreakBroken;

        // State
        private LoginStreak _streak;
        private List<DailyReward> _weeklyRewards;

        public LoginStreak CurrentStreak => _streak;
        public List<DailyReward> WeeklyRewards => _weeklyRewards;
        public bool CanClaim => _streak?.CanClaimReward ?? false;
        public bool CanClaimReward => CanClaim;

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
            InitializeRewards();
            LoadStreak();
            ProcessLogin();
        }

        #region Reward Definitions

        private void InitializeRewards()
        {
            _weeklyRewards = new List<DailyReward>
            {
                // Basic week rewards - escalating value
                new DailyReward(1, ResourceType.Stone, 100),
                new DailyReward(2, ResourceType.Wood, 100),
                new DailyReward(3, ResourceType.Stone, 200),
                new DailyReward(4, ResourceType.Metal, 50),
                new DailyReward(5, ResourceType.Wood, 200),
                new DailyReward(6, ResourceType.Crystal, 25),
                new DailyReward(7, ResourceType.Gems, 10, true) // Day 7 bonus!
            };
        }

        #endregion

        #region Login Processing

        private void ProcessLogin()
        {
            if (_streak == null)
            {
                _streak = new LoginStreak();
            }

            DateTime today = DateTime.UtcNow.Date;
            DateTime lastDate = _streak.LastLoginDate.Date;

            // Check if this is a new day
            if (today == lastDate)
            {
                // Same day, nothing to do
                Debug.Log("[DailyReward] Already logged in today");
                return;
            }

            // Record login
            _streak.TotalLogins++;
            _streak.LoginHistory.Add(DateTime.UtcNow);
            _streak.ClaimedToday = false;

            // Check streak
            if (today == lastDate.AddDays(1) || lastDate == DateTime.MinValue.Date)
            {
                // Continue or start streak
                _streak.CurrentStreak++;
                _streak.LongestStreak = Math.Max(_streak.CurrentStreak, _streak.LongestStreak);
                Debug.Log($"[DailyReward] Streak: {_streak.CurrentStreak} days!");
            }
            else
            {
                // Streak broken
                Debug.Log($"[DailyReward] Streak broken! Was {_streak.CurrentStreak} days");
                _streak.CurrentStreak = 1;
                OnStreakBroken?.Invoke();
            }

            _streak.LastLoginDate = DateTime.UtcNow;
            OnStreakUpdated?.Invoke(_streak.CurrentStreak);
            SaveStreak();

            // Show notification
            NotificationManager.Instance?.AddNotification(
                NotificationType.ResourcesCollected,
                "Daily Reward Available!",
                $"Day {_streak.DayInWeek} reward ready to claim!"
            );
        }

        #endregion

        #region Claiming Rewards

        /// <summary>
        /// Claim today's daily reward
        /// </summary>
        public (bool Success, DailyReward Reward) ClaimDailyReward()
        {
            if (!CanClaim)
            {
                Debug.Log("[DailyReward] Cannot claim - already claimed or not available");
                return (false, null);
            }

            // Get today's reward
            DailyReward reward = GetTodaysReward();
            if (reward == null)
            {
                return (false, null);
            }

            // Calculate streak bonus
            int streakBonus = CalculateStreakBonus(reward.Amount);
            int totalAmount = reward.Amount + streakBonus;

            // Grant reward
            PlayerManager.Instance?.AddResource(reward.ResourceType, totalAmount);

            // Award XP
            PlayerManager.Instance?.AwardExperience(50 * _streak.DayInWeek);

            // Mark as claimed
            _streak.ClaimedToday = true;
            SaveStreak();

            OnRewardClaimed?.Invoke(reward);

            Debug.Log($"[DailyReward] Claimed Day {reward.Day}: {totalAmount} {reward.ResourceType} " +
                     $"(base {reward.Amount} + streak bonus {streakBonus})");

            return (true, reward);
        }

        /// <summary>
        /// Get today's reward based on streak day
        /// </summary>
        public DailyReward GetTodaysReward()
        {
            int day = _streak?.DayInWeek ?? 1;
            return _weeklyRewards.Find(r => r.Day == day);
        }

        /// <summary>
        /// Get all rewards for the week with claim status
        /// </summary>
        public List<(DailyReward Reward, bool IsClaimed, bool IsToday)> GetWeekOverview()
        {
            var overview = new List<(DailyReward, bool, bool)>();
            int currentDay = _streak?.DayInWeek ?? 1;

            foreach (var reward in _weeklyRewards)
            {
                bool isClaimed = reward.Day < currentDay || 
                                (reward.Day == currentDay && _streak.ClaimedToday);
                bool isToday = reward.Day == currentDay;
                
                overview.Add((reward, isClaimed, isToday));
            }

            return overview;
        }

        private int CalculateStreakBonus(int baseAmount)
        {
            float bonusPercent = Math.Min(_streak.CurrentStreak * streakBonusPercentage, maxStreakBonus);
            return Mathf.RoundToInt(baseAmount * bonusPercent / 100f);
        }

        #endregion

        #region Special Events

        /// <summary>
        /// Check if there's a special event bonus active
        /// </summary>
        public (bool IsActive, float Multiplier, string EventName) GetActiveEvent()
        {
            // Weekend bonus
            if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Saturday ||
                DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
            {
                return (true, 2f, "Weekend Bonus");
            }

            // Could add holiday events, special promotions, etc.

            return (false, 1f, "");
        }

        /// <summary>
        /// Get time until next reward reset
        /// </summary>
        public TimeSpan GetTimeUntilReset()
        {
            DateTime now = DateTime.UtcNow;
            DateTime tomorrow = now.Date.AddDays(1);
            return tomorrow - now;
        }

        #endregion

        #region Persistence

        private void LoadStreak()
        {
            string json = PlayerPrefs.GetString("login_streak", "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    _streak = JsonUtility.FromJson<LoginStreak>(json);
                }
                catch
                {
                    _streak = new LoginStreak();
                }
            }
            else
            {
                _streak = new LoginStreak();
            }

            Debug.Log($"[DailyReward] Loaded streak: {_streak.CurrentStreak} days");
        }

        private void SaveStreak()
        {
            string json = JsonUtility.ToJson(_streak);
            PlayerPrefs.SetString("login_streak", json);
            PlayerPrefs.Save();
            Debug.Log("[DailyReward] Streak saved");
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveStreak();
        }

        #endregion

        #region Admin/Debug

        /// <summary>
        /// Reset streak (for testing)
        /// </summary>
        public void ResetStreak()
        {
            _streak = new LoginStreak();
            SaveStreak();
            Debug.Log("[DailyReward] Streak reset");
        }

        /// <summary>
        /// Simulate next day (for testing)
        /// </summary>
        public void SimulateNextDay()
        {
            _streak.LastLoginDate = DateTime.UtcNow.AddDays(-1);
            _streak.ClaimedToday = false;
            ProcessLogin();
        }

        #endregion
    }
}
