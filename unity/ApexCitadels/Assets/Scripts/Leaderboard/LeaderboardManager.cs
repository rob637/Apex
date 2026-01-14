using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Player;

namespace ApexCitadels.Leaderboard
{
    /// <summary>
    /// Leaderboard types
    /// </summary>
    public enum LeaderboardType
    {
        Global,             // All players worldwide
        Regional,           // Players in same city/region
        Alliance,           // Alliance rankings
        Weekly,             // Weekly competition
        AllTime             // All-time stats
    }

    /// <summary>
    /// Leaderboard category for filtering
    /// </summary>
    public enum LeaderboardCategory
    {
        TotalXP,
        TerritoriesOwned,
        TerritoriesCaptured,
        AttacksWon,
        DefensesWon,
        BuildingsPlaced,
        ResourcesCollected
    }

    /// <summary>
    /// Leaderboard entry
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public int Rank;
        public string PlayerId;
        public string PlayerName;
        public string AllianceTag;
        public int Level;
        public long Score;
        public string Region;
        public DateTime LastActive;

        // Display
        public bool IsCurrentPlayer;
        public int RankChange; // Positive = moved up, negative = moved down
    }

    /// <summary>
    /// Manages leaderboards and rankings
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int entriesPerPage = 100;
        [SerializeField] private float refreshInterval = 300f; // 5 minutes

        // Events
        public event Action<List<LeaderboardEntry>> OnLeaderboardLoaded;
        public event Action<int> OnPlayerRankUpdated;

        // State
        private Dictionary<LeaderboardType, List<LeaderboardEntry>> _cachedLeaderboards;
        private DateTime _lastRefresh;
        private int _currentPlayerRank = -1;

        public int CurrentPlayerRank => _currentPlayerRank;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _cachedLeaderboards = new Dictionary<LeaderboardType, List<LeaderboardEntry>>();
        }

        #region Loading Leaderboards

        /// <summary>
        /// Get global leaderboard
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetGlobalLeaderboard(LeaderboardCategory category = LeaderboardCategory.TotalXP, int limit = 100)
        {
            // Check cache
            if (_cachedLeaderboards.TryGetValue(LeaderboardType.Global, out var cached) &&
                (DateTime.UtcNow - _lastRefresh).TotalSeconds < refreshInterval)
            {
                return cached;
            }

            var entries = await LoadLeaderboardFromCloud(LeaderboardType.Global, category, limit);
            _cachedLeaderboards[LeaderboardType.Global] = entries;
            _lastRefresh = DateTime.UtcNow;

            // Find current player rank
            string playerId = PlayerManager.Instance?.GetCurrentPlayerId() ?? "";
            var playerEntry = entries.Find(e => e.PlayerId == playerId);
            if (playerEntry != null)
            {
                _currentPlayerRank = playerEntry.Rank;
                OnPlayerRankUpdated?.Invoke(_currentPlayerRank);
            }

            OnLeaderboardLoaded?.Invoke(entries);
            return entries;
        }

        /// <summary>
        /// Get regional leaderboard (players nearby)
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetRegionalLeaderboard(string region, int limit = 100)
        {
            var entries = await LoadLeaderboardFromCloud(LeaderboardType.Regional, LeaderboardCategory.TotalXP, limit, region);
            _cachedLeaderboards[LeaderboardType.Regional] = entries;
            return entries;
        }

        /// <summary>
        /// Get alliance rankings
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetAllianceLeaderboard(int limit = 100)
        {
            var entries = await LoadLeaderboardFromCloud(LeaderboardType.Alliance, LeaderboardCategory.TotalXP, limit);
            _cachedLeaderboards[LeaderboardType.Alliance] = entries;
            return entries;
        }

        /// <summary>
        /// Get weekly competition leaderboard
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetWeeklyLeaderboard(int limit = 100)
        {
            var entries = await LoadLeaderboardFromCloud(LeaderboardType.Weekly, LeaderboardCategory.TerritoriesCaptured, limit);
            _cachedLeaderboards[LeaderboardType.Weekly] = entries;
            return entries;
        }

        /// <summary>
        /// Get entries around current player
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetEntriesAroundPlayer(LeaderboardType type, int range = 10)
        {
            string playerId = PlayerManager.Instance?.GetCurrentPlayerId() ?? "";
            
            // Find player's position first
            int playerRank = await GetPlayerRank(playerId, type);
            if (playerRank < 0) return new List<LeaderboardEntry>();

            int startRank = Math.Max(1, playerRank - range);
            int endRank = playerRank + range;

            return await LoadLeaderboardRange(type, startRank, endRank);
        }

        #endregion

        #region Player Stats

        /// <summary>
        /// Get current player's rank in a leaderboard
        /// </summary>
        public async Task<int> GetPlayerRank(string playerId, LeaderboardType type)
        {
            await Task.Delay(50);
            // TODO: Query Firestore for player's rank
            // Firestore.collection("leaderboards").doc(type).collection("entries")
            //   .where("playerId", "==", playerId).get()
            
            return _currentPlayerRank;
        }

        /// <summary>
        /// Submit score to leaderboard
        /// </summary>
        public async Task SubmitScore(LeaderboardCategory category, long score)
        {
            string playerId = PlayerManager.Instance?.GetCurrentPlayerId() ?? "";
            string playerName = PlayerManager.Instance?.CurrentPlayer?.DisplayName ?? "Unknown";
            string allianceTag = Alliance.AllianceManager.Instance?.CurrentAlliance?.Tag ?? "";

            var entry = new LeaderboardEntry
            {
                PlayerId = playerId,
                PlayerName = playerName,
                AllianceTag = allianceTag,
                Level = PlayerManager.Instance?.CurrentPlayer?.Level ?? 1,
                Score = score,
                LastActive = DateTime.UtcNow
            };

            await SaveScoreToCloud(category, entry);
            Debug.Log($"[LeaderboardManager] Submitted score: {score} for {category}");
        }

        /// <summary>
        /// Update all player stats on leaderboards
        /// </summary>
        public async void UpdateAllStats()
        {
            var player = PlayerManager.Instance?.CurrentPlayer;
            if (player == null) return;

            await SubmitScore(LeaderboardCategory.TotalXP, player.TotalExperience);
            await SubmitScore(LeaderboardCategory.TerritoriesOwned, player.TerritoriesOwned);
            await SubmitScore(LeaderboardCategory.TerritoriesCaptured, player.TotalTerritoriesCaptured);
            await SubmitScore(LeaderboardCategory.AttacksWon, player.AttacksWon);
            await SubmitScore(LeaderboardCategory.DefensesWon, player.DefensesWon);
            await SubmitScore(LeaderboardCategory.BuildingsPlaced, player.BuildingsPlaced);

            Debug.Log("[LeaderboardManager] All stats updated");
        }

        #endregion

        #region Weekly Competition

        /// <summary>
        /// Get time remaining in current weekly competition
        /// </summary>
        public TimeSpan GetWeeklyTimeRemaining()
        {
            // Week resets Sunday at midnight UTC
            DateTime now = DateTime.UtcNow;
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilSunday == 0 && now.Hour > 0) daysUntilSunday = 7;
            
            DateTime nextReset = now.Date.AddDays(daysUntilSunday);
            return nextReset - now;
        }

        /// <summary>
        /// Get weekly rewards based on rank
        /// </summary>
        public Dictionary<string, int> GetWeeklyRewards(int rank)
        {
            var rewards = new Dictionary<string, int>();

            if (rank == 1)
            {
                rewards["Gems"] = 500;
                rewards["Crystal"] = 1000;
                rewards["XP"] = 5000;
            }
            else if (rank <= 3)
            {
                rewards["Gems"] = 250;
                rewards["Crystal"] = 500;
                rewards["XP"] = 2500;
            }
            else if (rank <= 10)
            {
                rewards["Gems"] = 100;
                rewards["Crystal"] = 250;
                rewards["XP"] = 1000;
            }
            else if (rank <= 50)
            {
                rewards["Gems"] = 50;
                rewards["Crystal"] = 100;
                rewards["XP"] = 500;
            }
            else if (rank <= 100)
            {
                rewards["Gems"] = 20;
                rewards["Crystal"] = 50;
                rewards["XP"] = 250;
            }

            return rewards;
        }

        #endregion

        #region Cloud Operations (Stubs)

        private async Task<List<LeaderboardEntry>> LoadLeaderboardFromCloud(
            LeaderboardType type, 
            LeaderboardCategory category, 
            int limit,
            string region = "")
        {
            await Task.Delay(100);
            
            // TODO: Query Firestore
            // var query = Firestore.collection("leaderboards")
            //     .doc(type.ToString())
            //     .collection(category.ToString())
            //     .orderBy("score", "desc")
            //     .limit(limit);
            // if (!string.IsNullOrEmpty(region))
            //     query = query.where("region", "==", region);
            // var snapshot = await query.get();

            // Generate mock data for now
            var entries = new List<LeaderboardEntry>();
            string currentPlayerId = PlayerManager.Instance?.GetCurrentPlayerId() ?? "";

            for (int i = 0; i < Math.Min(limit, 20); i++)
            {
                bool isPlayer = (i == 7); // Place player at rank 8
                entries.Add(new LeaderboardEntry
                {
                    Rank = i + 1,
                    PlayerId = isPlayer ? currentPlayerId : $"player_{i}",
                    PlayerName = isPlayer ? 
                        (PlayerManager.Instance?.CurrentPlayer?.DisplayName ?? "You") : 
                        $"Player{i + 1}",
                    AllianceTag = i < 5 ? "[TOP]" : "",
                    Level = 50 - i,
                    Score = 10000 - (i * 500),
                    IsCurrentPlayer = isPlayer,
                    RankChange = UnityEngine.Random.Range(-3, 4)
                });
            }

            return entries;
        }

        private async Task<List<LeaderboardEntry>> LoadLeaderboardRange(LeaderboardType type, int startRank, int endRank)
        {
            await Task.Delay(50);
            // TODO: Query Firestore with offset/limit
            return new List<LeaderboardEntry>();
        }

        private async Task SaveScoreToCloud(LeaderboardCategory category, LeaderboardEntry entry)
        {
            await Task.Delay(50);
            // TODO: Firestore set with merge
            // Firestore.collection("leaderboards")
            //     .doc(category.ToString())
            //     .collection("entries")
            //     .doc(entry.PlayerId)
            //     .set(entry, { merge: true });
        }

        #endregion

        #region Force Refresh

        /// <summary>
        /// Force refresh all leaderboards
        /// </summary>
        public async void ForceRefresh()
        {
            _lastRefresh = DateTime.MinValue;
            _cachedLeaderboards.Clear();
            await GetGlobalLeaderboard();
            Debug.Log("[LeaderboardManager] Leaderboards refreshed");
        }

        #endregion
    }
}
