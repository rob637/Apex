using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Data;

namespace ApexCitadels.Backend
{
    /// <summary>
    /// Implementation of IAllianceWarService using Firebase Cloud Functions.
    /// Handles alliance war declarations, status, and cancellations.
    /// </summary>
    public class AllianceWarService : MonoBehaviour, IAllianceWarService
    {
        public static AllianceWarService Instance { get; private set; }

        // Current war status (cached)
        private AllianceWar _currentWar;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private const float CACHE_DURATION_SECONDS = 30f;

        // Events for real-time war updates
        public event Action<AllianceWar> OnWarDeclared;
        public event Action<AllianceWar> OnWarPhaseChanged;
        public event Action<AllianceWar> OnWarEnded;
        public event Action<string, int> OnScoreUpdated; // allianceId, newScore

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Task<AllianceWar> DeclareWarAsync(string targetAllianceId)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("declareAllianceWar");
                var data = new Dictionary<string, object> { { "targetAllianceId", targetAllianceId } };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                var war = JsonUtility.FromJson<AllianceWar>(json);
                
                _currentWar = war;
                _cacheExpiry = DateTime.UtcNow.AddSeconds(CACHE_DURATION_SECONDS);
                OnWarDeclared?.Invoke(war);
                
                return war;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceWarService] DeclareWar failed: {ex.Message}");
                throw;
            }
#else
            Debug.Log($"[STUB] DeclareWar against: {targetAllianceId}");
            await Task.Delay(200);
            var war = new AllianceWar
            {
                WarId = Guid.NewGuid().ToString(),
                ChallengerId = "my-alliance",
                ChallengerName = "My Alliance",
                DefenderId = targetAllianceId,
                DefenderName = "Target Alliance",
                Phase = WarPhase.Warning,
                PhaseStartedAt = DateTime.UtcNow,
                PhaseEndsAt = DateTime.UtcNow.AddHours(24),
                ChallengerScore = 0,
                DefenderScore = 0
            };
            _currentWar = war;
            OnWarDeclared?.Invoke(war);
            return war;
#endif
        }

        public async Task<bool> CancelWarAsync(string warId)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("cancelAllianceWar");
                var data = new Dictionary<string, object> { { "warId", warId } };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                var response = JsonUtility.FromJson<SuccessResponse>(json);
                
                if (response.success)
                {
                    _currentWar = null;
                    _cacheExpiry = DateTime.MinValue;
                }
                return response.success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceWarService] CancelWar failed: {ex.Message}");
                return false;
            }
#else
            Debug.Log($"[STUB] CancelWar: {warId}");
            await Task.Delay(100);
            if (_currentWar?.WarId == warId && _currentWar?.Phase == WarPhase.Warning)
            {
                _currentWar = null;
                return true;
            }
            return false;
#endif
        }

        public async Task<AllianceWar> GetWarStatusAsync()
        {
            // Return cached value if still valid
            if (_currentWar != null && DateTime.UtcNow < _cacheExpiry)
            {
                return _currentWar;
            }

#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("getAllianceWarStatus");
                var result = await callable.CallAsync(new Dictionary<string, object>());
                var json = result.Data.ToString();
                
                if (string.IsNullOrEmpty(json) || json == "null")
                {
                    _currentWar = null;
                    return null;
                }
                
                _currentWar = JsonUtility.FromJson<AllianceWar>(json);
                _cacheExpiry = DateTime.UtcNow.AddSeconds(CACHE_DURATION_SECONDS);
                return _currentWar;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceWarService] GetWarStatus failed: {ex.Message}");
                throw;
            }
#else
            Debug.Log("[STUB] GetWarStatus called");
            await Task.Delay(100);
            return _currentWar;
#endif
        }

        /// <summary>
        /// Check if user's alliance is currently at war
        /// </summary>
        public bool IsAtWar => _currentWar != null && 
                               _currentWar.Phase != WarPhase.Ended && 
                               _currentWar.Phase != WarPhase.Peace;

        /// <summary>
        /// Check if currently in active battle phase
        /// </summary>
        public bool IsInBattlePhase => _currentWar?.Phase == WarPhase.Battle;

        /// <summary>
        /// Get time remaining in current phase
        /// </summary>
        public TimeSpan TimeRemainingInPhase
        {
            get
            {
                if (_currentWar == null) return TimeSpan.Zero;
                var remaining = _currentWar.PhaseEndsAt - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Force refresh war status from server
        /// </summary>
        public void InvalidateCache()
        {
            _cacheExpiry = DateTime.MinValue;
        }

        #region Real-time Listeners

        /// <summary>
        /// Start listening for real-time war updates
        /// Call this when entering alliance war UI
        /// </summary>
        public void StartWarListener()
        {
#if FIREBASE_ENABLED
            // TODO: Set up Firestore listener for war document
            Debug.Log("[AllianceWarService] Starting war listener...");
#else
            Debug.Log("[STUB] StartWarListener - would connect to Firestore");
#endif
        }

        /// <summary>
        /// Stop listening for real-time war updates
        /// Call this when leaving alliance war UI
        /// </summary>
        public void StopWarListener()
        {
#if FIREBASE_ENABLED
            // TODO: Remove Firestore listener
            Debug.Log("[AllianceWarService] Stopping war listener...");
#else
            Debug.Log("[STUB] StopWarListener");
#endif
        }

        /// <summary>
        /// Get all active/recent wars for an alliance
        /// </summary>
        public async Task<List<AllianceWar>> GetAllianceWarsAsync(string allianceId)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("getAllianceWars");
                var data = new Dictionary<string, object> { { "allianceId", allianceId } };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                // Parse JSON array to list
                var wars = JsonUtility.FromJson<AllianceWarList>("{\"wars\":" + json + "}");
                return wars?.wars ?? new List<AllianceWar>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceWarService] GetAllianceWars failed: {ex.Message}");
                return new List<AllianceWar>();
            }
#else
            Debug.Log($"[STUB] GetAllianceWarsAsync for alliance: {allianceId}");
            await Task.Delay(100);
            var wars = new List<AllianceWar>();
            if (_currentWar != null)
            {
                wars.Add(_currentWar);
            }
            return wars;
#endif
        }

        #endregion

        #region Helper Classes

        [Serializable]
        private class SuccessResponse
        {
            public bool success;
        }

        [Serializable]
        private class AllianceWarList
        {
            public List<AllianceWar> wars;
        }

        #endregion
    }

    #region War Types

    /// <summary>
    /// Alliance war data structure
    /// </summary>
    [Serializable]
    public class AllianceWar
    {
        public string WarId;
        public string ChallengerId;
        public string ChallengerName;
        public string DefenderId;
        public string DefenderName;
        public WarPhase Phase;
        public DateTime PhaseStartedAt;
        public DateTime PhaseEndsAt;
        public int ChallengerScore;
        public int DefenderScore;
        public List<WarParticipant> ChallengerParticipants;
        public List<WarParticipant> DefenderParticipants;
        public string WinnerId;
        public DateTime? EndedAt;
    }

    /// <summary>
    /// War phase enum
    /// </summary>
    public enum WarPhase
    {
        Warning,    // 24 hours before battle
        Battle,     // 48 hours of active fighting
        Peace,      // 72 hours after war ends
        Ended       // War completed
    }

    /// <summary>
    /// Individual participant in a war
    /// </summary>
    [Serializable]
    public class WarParticipant
    {
        public string UserId;
        public string DisplayName;
        public int Score;
        public int BattlesWon;
        public int BattlesLost;
    }

    #endregion
}
