using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if FIREBASE_ENABLED
using Firebase.Functions;
#endif
using Newtonsoft.Json;
using ApexCitadels.Core;

namespace ApexCitadels.Privacy
{
    /// <summary>
    /// Consent preferences
    /// </summary>
    [Serializable]
    public class ConsentPreferences
    {
        public bool Essential = true; // Always true
        public bool Analytics = true;
        public bool Marketing = false;
        public bool Personalization = true;
        public bool ThirdPartySharing = false;
    }

    /// <summary>
    /// Privacy settings
    /// </summary>
    [Serializable]
    public class PrivacySettings
    {
        public string ProfileVisibility = "public"; // public, friends, private
        public bool ShowOnLeaderboards = true;
        public bool AllowFriendRequests = true;
        public bool ShowOnlineStatus = true;
        public bool ShowLastActive = true;
        public bool AllowAllianceInvites = true;
        public bool ShareLocationWithAlliance = true;
    }

    /// <summary>
    /// Data export request status
    /// </summary>
    [Serializable]
    public class DataExportStatus
    {
        public string RequestId;
        public string Status; // pending, processing, completed, failed, expired
        public string DownloadUrl;
        public DateTime? ExpiresAt;
        public string ErrorMessage;
    }

    /// <summary>
    /// Data deletion request status
    /// </summary>
    [Serializable]
    public class DataDeletionStatus
    {
        public string RequestId;
        public string Status; // pending, processing, completed, failed, cancelled
        public DateTime ScheduledDeletionAt;
        public int GracePeriodDays;
    }

    /// <summary>
    /// Manages GDPR compliance, consent, and privacy settings
    /// </summary>
    public class GDPRManager : MonoBehaviour
    {
        public static GDPRManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private string currentConsentVersion = "1.0.0";
        [SerializeField] private bool debugMode = false;

        // Events
        public event Action<ConsentPreferences> OnConsentUpdated;
        public event Action<PrivacySettings> OnPrivacySettingsUpdated;
        public event Action<DataExportStatus> OnExportStatusChanged;
        public event Action<DataDeletionStatus> OnDeletionStatusChanged;
        public event Action OnConsentRequired; // Show consent dialog

        // State
#if FIREBASE_ENABLED
        private FirebaseFunctions _functions;
#endif
        private ConsentPreferences _currentConsent;
        private PrivacySettings _privacySettings;
        private string _storedConsentVersion;
        private bool _hasValidConsent;

        public ConsentPreferences CurrentConsent => _currentConsent;
        public PrivacySettings CurrentPrivacySettings => _privacySettings;
        public bool HasValidConsent => _hasValidConsent;
        public bool NeedsConsentUpdate => _storedConsentVersion != currentConsentVersion;

        private const string CONSENT_PREFS_KEY = "gdpr_consent";
        private const string CONSENT_VERSION_KEY = "gdpr_consent_version";
        private const string PRIVACY_PREFS_KEY = "privacy_settings";

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
                return;
            }
        }

#if FIREBASE_ENABLED
        private void Start()
        {
            _functions = FirebaseFunctions.DefaultInstance;
            LoadLocalPreferences();
            CheckConsentStatus();
        }
#else
        private void Start()
        {
            ApexLogger.LogWarning("Firebase SDK not installed. Running in stub mode.", ApexLogger.LogCategory.General);
            LoadLocalPreferences();
            // Check local consent only in stub mode
            if (!_hasValidConsent)
            {
                OnConsentRequired?.Invoke();
            }
        }
#endif

        /// <summary>
        /// Load locally cached preferences
        /// </summary>
        private void LoadLocalPreferences()
        {
            // Load consent
            string consentJson = PlayerPrefs.GetString(CONSENT_PREFS_KEY, "");
            if (!string.IsNullOrEmpty(consentJson))
            {
                try
                {
                    _currentConsent = JsonConvert.DeserializeObject<ConsentPreferences>(consentJson);
                }
                catch
                {
                    _currentConsent = new ConsentPreferences();
                }
            }
            else
            {
                _currentConsent = new ConsentPreferences();
            }

            _storedConsentVersion = PlayerPrefs.GetString(CONSENT_VERSION_KEY, "");
            _hasValidConsent = !string.IsNullOrEmpty(_storedConsentVersion) && 
                              _storedConsentVersion == currentConsentVersion;

            // Load privacy settings
            string privacyJson = PlayerPrefs.GetString(PRIVACY_PREFS_KEY, "");
            if (!string.IsNullOrEmpty(privacyJson))
            {
                try
                {
                    _privacySettings = JsonConvert.DeserializeObject<PrivacySettings>(privacyJson);
                }
                catch
                {
                    _privacySettings = new PrivacySettings();
                }
            }
            else
            {
                _privacySettings = new PrivacySettings();
            }

            if (debugMode)
            {
                ApexLogger.Log($"Loaded consent: {consentJson}, version: {_storedConsentVersion}, valid: {_hasValidConsent}", ApexLogger.LogCategory.General);
            }
        }

        /// <summary>
        /// Check if consent is needed (call on app start)
        /// </summary>
#if FIREBASE_ENABLED
        public async void CheckConsentStatus()
        {
            // First check local state
            if (!_hasValidConsent)
            {
                OnConsentRequired?.Invoke();
                return;
            }

            // Then verify with server
            try
            {
                var function = _functions.GetHttpsCallable("getConsent");
                var result = await function.CallAsync();
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                bool needsUpdate = response.ContainsKey("needsUpdate") && (bool)response["needsUpdate"];
                bool hasConsent = response.ContainsKey("hasConsent") && (bool)response["hasConsent"];

                if (!hasConsent || needsUpdate)
                {
                    _hasValidConsent = false;
                    OnConsentRequired?.Invoke();
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    ApexLogger.LogError($"Failed to check consent status: {ex.Message}", LogCategory.General);
                }
                // On error, use local state
            }
        }
#else
        public void CheckConsentStatus()
        {
            // Stub mode - only check local state
            if (!_hasValidConsent)
            {
                OnConsentRequired?.Invoke();
            }
        }
#endif

        /// <summary>
        /// Update consent preferences
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<bool> UpdateConsent(ConsentPreferences consent)
        {
            try
            {
                var function = _functions.GetHttpsCallable("updateConsent");
                var data = new Dictionary<string, object>
                {
                    ["consents"] = new Dictionary<string, object>
                    {
                        ["analytics"] = consent.Analytics,
                        ["marketing"] = consent.Marketing,
                        ["personalization"] = consent.Personalization,
                        ["thirdPartySharing"] = consent.ThirdPartySharing
                    }
                };

                var result = await function.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                if (response.ContainsKey("success") && (bool)response["success"])
                {
                    _currentConsent = consent;
                    _storedConsentVersion = currentConsentVersion;
                    _hasValidConsent = true;

                    // Cache locally
                    PlayerPrefs.SetString(CONSENT_PREFS_KEY, JsonConvert.SerializeObject(consent));
                    PlayerPrefs.SetString(CONSENT_VERSION_KEY, currentConsentVersion);
                    PlayerPrefs.Save();

                    OnConsentUpdated?.Invoke(consent);

                    if (debugMode)
                    {
                        ApexLogger.Log("Consent updated successfully", LogCategory.General);
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"Failed to update consent: {ex.Message}", LogCategory.General);
                return false;
            }
        }
#else
        public Task<bool> UpdateConsent(ConsentPreferences consent)
        {
            ApexLogger.LogWarning("UpdateConsent called but Firebase SDK not installed. Saving locally only.", ApexLogger.LogCategory.General);
            
            // Save locally in stub mode
            _currentConsent = consent;
            _storedConsentVersion = currentConsentVersion;
            _hasValidConsent = true;

            PlayerPrefs.SetString(CONSENT_PREFS_KEY, JsonConvert.SerializeObject(consent));
            PlayerPrefs.SetString(CONSENT_VERSION_KEY, currentConsentVersion);
            PlayerPrefs.Save();

            OnConsentUpdated?.Invoke(consent);
            return Task.FromResult(true);
        }
#endif

        /// <summary>
        /// Update privacy settings
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<bool> UpdatePrivacySettings(PrivacySettings settings)
        {
            try
            {
                var function = _functions.GetHttpsCallable("updatePrivacySettings");
                var data = new Dictionary<string, object>
                {
                    ["settings"] = new Dictionary<string, object>
                    {
                        ["profileVisibility"] = settings.ProfileVisibility,
                        ["showOnLeaderboards"] = settings.ShowOnLeaderboards,
                        ["allowFriendRequests"] = settings.AllowFriendRequests,
                        ["showOnlineStatus"] = settings.ShowOnlineStatus,
                        ["showLastActive"] = settings.ShowLastActive,
                        ["allowAllianceInvites"] = settings.AllowAllianceInvites,
                        ["shareLocationWithAlliance"] = settings.ShareLocationWithAlliance
                    }
                };

                var result = await function.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                if (response.ContainsKey("success") && (bool)response["success"])
                {
                    _privacySettings = settings;

                    // Cache locally
                    PlayerPrefs.SetString(PRIVACY_PREFS_KEY, JsonConvert.SerializeObject(settings));
                    PlayerPrefs.Save();

                    OnPrivacySettingsUpdated?.Invoke(settings);

                    if (debugMode)
                    {
                        ApexLogger.Log("Privacy settings updated successfully", LogCategory.General);
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"Failed to update privacy settings: {ex.Message}", LogCategory.General);
                return false;
            }
        }
#else
        public Task<bool> UpdatePrivacySettings(PrivacySettings settings)
        {
            ApexLogger.LogWarning("UpdatePrivacySettings called but Firebase SDK not installed. Saving locally only.", ApexLogger.LogCategory.General);
            
            _privacySettings = settings;
            PlayerPrefs.SetString(PRIVACY_PREFS_KEY, JsonConvert.SerializeObject(settings));
            PlayerPrefs.Save();
            OnPrivacySettingsUpdated?.Invoke(settings);
            return Task.FromResult(true);
        }
#endif

        /// <summary>
        /// Load privacy settings from server
        /// </summary>
#if FIREBASE_ENABLED
        public async Task LoadPrivacySettings()
        {
            try
            {
                var function = _functions.GetHttpsCallable("getPrivacySettings");
                var result = await function.CallAsync();
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                if (response.ContainsKey("settings"))
                {
                    var settingsData = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        response["settings"].ToString()
                    );

                    _privacySettings = new PrivacySettings
                    {
                        ProfileVisibility = settingsData.GetValueOrDefault("profileVisibility", "public").ToString(),
                        ShowOnLeaderboards = Convert.ToBoolean(settingsData.GetValueOrDefault("showOnLeaderboards", true)),
                        AllowFriendRequests = Convert.ToBoolean(settingsData.GetValueOrDefault("allowFriendRequests", true)),
                        ShowOnlineStatus = Convert.ToBoolean(settingsData.GetValueOrDefault("showOnlineStatus", true)),
                        ShowLastActive = Convert.ToBoolean(settingsData.GetValueOrDefault("showLastActive", true)),
                        AllowAllianceInvites = Convert.ToBoolean(settingsData.GetValueOrDefault("allowAllianceInvites", true)),
                        ShareLocationWithAlliance = Convert.ToBoolean(settingsData.GetValueOrDefault("shareLocationWithAlliance", true))
                    };

                    // Cache locally
                    PlayerPrefs.SetString(PRIVACY_PREFS_KEY, JsonConvert.SerializeObject(_privacySettings));
                    PlayerPrefs.Save();

                    OnPrivacySettingsUpdated?.Invoke(_privacySettings);
                }
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"Failed to load privacy settings: {ex.Message}", LogCategory.General);
            }
        }
#else
        public Task LoadPrivacySettings()
        {
            ApexLogger.LogWarning("LoadPrivacySettings called but Firebase SDK not installed. Using local settings.", ApexLogger.LogCategory.General);
            OnPrivacySettingsUpdated?.Invoke(_privacySettings);
            return Task.CompletedTask;
        }
#endif

        #region Data Export

        /// <summary>
        /// Request a full data export
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<DataExportStatus> RequestDataExport(string format = "json")
        {
            try
            {
                var function = _functions.GetHttpsCallable("requestDataExport");
                var data = new Dictionary<string, object> { ["format"] = format };
                var result = await function.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                var status = new DataExportStatus
                {
                    RequestId = response.GetValueOrDefault("requestId", "").ToString(),
                    Status = (bool)response.GetValueOrDefault("success", false) ? "pending" : "failed"
                };

                OnExportStatusChanged?.Invoke(status);
                return status;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"Failed to request data export: {ex.Message}", LogCategory.General);
                return new DataExportStatus { Status = "failed", ErrorMessage = ex.Message };
            }
        }
#else
        public Task<DataExportStatus> RequestDataExport(string format = "json")
        {
            ApexLogger.LogWarning("RequestDataExport called but Firebase SDK not installed.", ApexLogger.LogCategory.General);
            var status = new DataExportStatus { Status = "unavailable", ErrorMessage = "Firebase not installed" };
            OnExportStatusChanged?.Invoke(status);
            return Task.FromResult(status);
        }
#endif

        /// <summary>
        /// Check data export status
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<DataExportStatus> GetExportStatus(string requestId = null)
        {
            try
            {
                var function = _functions.GetHttpsCallable("getDataExportStatus");
                var data = requestId != null 
                    ? new Dictionary<string, object> { ["requestId"] = requestId }
                    : null;
                var result = await function.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                if (response.ContainsKey("request"))
                {
                    var requestData = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        response["request"].ToString()
                    );

                    var status = new DataExportStatus
                    {
                        RequestId = requestData.GetValueOrDefault("id", "").ToString(),
                        Status = requestData.GetValueOrDefault("status", "unknown").ToString(),
                        DownloadUrl = requestData.GetValueOrDefault("downloadUrl", "").ToString()
                    };

                    if (requestData.ContainsKey("expiresAt"))
                    {
                        // Parse Firestore timestamp
                        var expiresData = requestData["expiresAt"];
                        if (expiresData is Dictionary<string, object> timestampDict && 
                            timestampDict.ContainsKey("_seconds"))
                        {
                            long seconds = Convert.ToInt64(timestampDict["_seconds"]);
                            status.ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime;
                        }
                    }

                    OnExportStatusChanged?.Invoke(status);
                    return status;
                }

                return new DataExportStatus { Status = "not_found" };
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"Failed to get export status: {ex.Message}", LogCategory.General);
                return new DataExportStatus { Status = "error", ErrorMessage = ex.Message };
            }
        }
#else
        public Task<DataExportStatus> GetExportStatus(string requestId = null)
        {
            ApexLogger.LogWarning("GetExportStatus called but Firebase SDK not installed.", ApexLogger.LogCategory.General);
            return Task.FromResult(new DataExportStatus { Status = "unavailable", ErrorMessage = "Firebase not installed" });
        }
#endif

        #endregion

        #region Data Deletion

        /// <summary>
        /// Request account and data deletion
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<DataDeletionStatus> RequestDataDeletion()
        {
            try
            {
                var function = _functions.GetHttpsCallable("requestDataDeletion");
                var data = new Dictionary<string, object> { ["confirmation"] = "DELETE_MY_DATA" };
                var result = await function.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                if ((bool)response.GetValueOrDefault("success", false))
                {
                    var status = new DataDeletionStatus
                    {
                        RequestId = response.GetValueOrDefault("requestId", "").ToString(),
                        Status = "pending",
                        GracePeriodDays = Convert.ToInt32(response.GetValueOrDefault("gracePeriodDays", 30))
                    };

                    if (response.ContainsKey("scheduledDeletionAt"))
                    {
                        DateTime.TryParse(response["scheduledDeletionAt"].ToString(), out DateTime scheduled);
                        status.ScheduledDeletionAt = scheduled;
                    }

                    OnDeletionStatusChanged?.Invoke(status);
                    return status;
                }

                return new DataDeletionStatus { Status = "failed" };
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"Failed to request data deletion: {ex.Message}", LogCategory.General);
                return new DataDeletionStatus { Status = "error" };
            }
        }
#else
        public Task<DataDeletionStatus> RequestDataDeletion()
        {
            ApexLogger.LogWarning("RequestDataDeletion called but Firebase SDK not installed.", ApexLogger.LogCategory.General);
            var status = new DataDeletionStatus { Status = "unavailable" };
            OnDeletionStatusChanged?.Invoke(status);
            return Task.FromResult(status);
        }
#endif

        /// <summary>
        /// Cancel a pending deletion request
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<bool> CancelDataDeletion(string requestId)
        {
            try
            {
                var function = _functions.GetHttpsCallable("cancelDataDeletion");
                var data = new Dictionary<string, object> { ["requestId"] = requestId };
                var result = await function.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                bool success = (bool)response.GetValueOrDefault("success", false);

                if (success)
                {
                    OnDeletionStatusChanged?.Invoke(new DataDeletionStatus
                    {
                        RequestId = requestId,
                        Status = "cancelled"
                    });
                }

                return success;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"Failed to cancel deletion: {ex.Message}", LogCategory.General);
                return false;
            }
        }
#else
        public Task<bool> CancelDataDeletion(string requestId)
        {
            ApexLogger.LogWarning("CancelDataDeletion called but Firebase SDK not installed.", ApexLogger.LogCategory.General);
            return Task.FromResult(false);
        }
#endif

        #endregion

        #region Analytics Control

        /// <summary>
        /// Check if analytics is enabled (respects consent)
        /// </summary>
        public bool IsAnalyticsEnabled()
        {
            return _hasValidConsent && _currentConsent.Analytics;
        }

        /// <summary>
        /// Check if marketing is enabled (respects consent)
        /// </summary>
        public bool IsMarketingEnabled()
        {
            return _hasValidConsent && _currentConsent.Marketing;
        }

        /// <summary>
        /// Check if personalization is enabled (respects consent)
        /// </summary>
        public bool IsPersonalizationEnabled()
        {
            return _hasValidConsent && _currentConsent.Personalization;
        }

        #endregion

        /// <summary>
        /// Clear all local data (for testing or logout)
        /// </summary>
        public void ClearLocalData()
        {
            PlayerPrefs.DeleteKey(CONSENT_PREFS_KEY);
            PlayerPrefs.DeleteKey(CONSENT_VERSION_KEY);
            PlayerPrefs.DeleteKey(PRIVACY_PREFS_KEY);
            PlayerPrefs.Save();

            _currentConsent = new ConsentPreferences();
            _privacySettings = new PrivacySettings();
            _hasValidConsent = false;
            _storedConsentVersion = "";

            if (debugMode)
            {
                ApexLogger.Log("Local data cleared", ApexLogger.LogCategory.General);
            }
        }
    }

    /// <summary>
    /// Extension methods for Dictionary
    /// </summary>
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary, 
            TKey key, 
            TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }
}
