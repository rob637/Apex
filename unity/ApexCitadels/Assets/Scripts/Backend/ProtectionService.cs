using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Core;
using ApexCitadels.Data;

namespace ApexCitadels.Backend
{
    /// <summary>
    /// Implementation of IProtectionService using Firebase Cloud Functions.
    /// Handles newcomer shields, activity bonuses, and attack eligibility checks.
    /// </summary>
    public class ProtectionService : MonoBehaviour, IProtectionService
    {
        public static ProtectionService Instance { get; private set; }

        // Cache protection status for quick access
        private ProtectionStatus _cachedStatus;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private const float CACHE_DURATION_SECONDS = 60f;

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

        public async Task<ProtectionStatus> GetProtectionStatusAsync()
        {
            // Return cached value if still valid
            if (_cachedStatus != null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedStatus;
            }

#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("getMyProtectionStatus");
                var result = await callable.CallAsync(new Dictionary<string, object>());
                var json = result.Data.ToString();
                _cachedStatus = JsonUtility.FromJson<ProtectionStatus>(json);
                _cacheExpiry = DateTime.UtcNow.AddSeconds(CACHE_DURATION_SECONDS);
                return _cachedStatus;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"GetProtectionStatus failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                throw;
            }
#else
            ApexLogger.LogVerbose("[STUB] GetProtectionStatus called", ApexLogger.LogCategory.Combat);
            await Task.Delay(100);
            _cachedStatus = new ProtectionStatus
            {
                HasNewcomerShield = true,
                ShieldExpiresAt = DateTime.UtcNow.AddDays(5),
                DaysRemaining = 5,
                ActivityBonus = 0.25f,
                CanAttack = false
            };
            _cacheExpiry = DateTime.UtcNow.AddSeconds(CACHE_DURATION_SECONDS);
            return _cachedStatus;
#endif
        }

        public async Task<(bool canAttack, string reason)> CheckTerritoryAttackableAsync(string territoryId)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("checkTerritoryAttackable");
                var data = new Dictionary<string, object> { { "territoryId", territoryId } };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                var response = JsonUtility.FromJson<AttackableResponse>(json);
                return (response.canAttack, response.reason);
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"CheckTerritoryAttackable failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                return (false, "Error checking territory status");
            }
#else
            ApexLogger.LogVerbose($"[STUB] CheckTerritoryAttackable: {territoryId}", ApexLogger.LogCategory.Combat);
            await Task.Delay(100);
            return (true, null);
#endif
        }

        public async Task<bool> WaiveNewcomerShieldAsync()
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("waiveNewcomerShield");
                var result = await callable.CallAsync(new Dictionary<string, object>());
                var json = result.Data.ToString();
                var response = JsonUtility.FromJson<SuccessResponse>(json);
                
                if (response.success)
                {
                    // Invalidate cache
                    _cacheExpiry = DateTime.MinValue;
                }
                return response.success;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"WaiveNewcomerShield failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                return false;
            }
#else
            ApexLogger.LogVerbose("[STUB] WaiveNewcomerShield called", ApexLogger.LogCategory.Combat);
            await Task.Delay(100);
            _cachedStatus = new ProtectionStatus
            {
                HasNewcomerShield = false,
                CanAttack = true
            };
            return true;
#endif
        }

        public async Task GrantTemporaryShieldAsync(int hours)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("grantTemporaryShield");
                var data = new Dictionary<string, object> { { "hours", hours } };
                await callable.CallAsync(data);
                
                // Invalidate cache
                _cacheExpiry = DateTime.MinValue;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"GrantTemporaryShield failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                throw;
            }
#else
            ApexLogger.LogVerbose($"[STUB] GrantTemporaryShield: {hours} hours", ApexLogger.LogCategory.Combat);
            await Task.Delay(100);
            _cachedStatus = new ProtectionStatus
            {
                HasTemporaryShield = true,
                ShieldExpiresAt = DateTime.UtcNow.AddHours(hours),
                CanAttack = true
            };
#endif
        }

        /// <summary>
        /// Force refresh protection status from server
        /// </summary>
        public void InvalidateCache()
        {
            _cacheExpiry = DateTime.MinValue;
        }

        #region Helper Classes

        [Serializable]
        private class AttackableResponse
        {
            public bool canAttack;
            public string reason;
        }

        [Serializable]
        private class SuccessResponse
        {
            public bool success;
        }

        #endregion
    }

    /// <summary>
    /// Protection status data from backend
    /// </summary>
    [Serializable]
    public class ProtectionStatus
    {
        public bool HasNewcomerShield;
        public bool HasTemporaryShield;
        public DateTime ShieldExpiresAt;
        public int DaysRemaining;
        public float ActivityBonus; // 0.0, 0.25, or 0.50
        public bool CanAttack;
        public DateTime LastActivity;
    }
}
