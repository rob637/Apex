using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if FIREBASE_ENABLED
using Firebase.Functions;
#endif
using Newtonsoft.Json;

namespace ApexCitadels.AntiCheat
{
    /// <summary>
    /// Location validation result
    /// </summary>
    [Serializable]
    public class LocationValidationResult
    {
        public bool IsValid;
        public string Reason;
        public float TrustScore;
        public bool RequiresVerification;
    }

    /// <summary>
    /// Player trust status
    /// </summary>
    [Serializable]
    public class TrustStatus
    {
        public float TrustScore;
        public int WarningCount;
        public bool IsSuspended;
        public DateTime? SuspendedUntil;
        public List<string> RecentViolations;
    }

    /// <summary>
    /// Location data for validation
    /// </summary>
    [Serializable]
    public class LocationData
    {
        public double Latitude;
        public double Longitude;
        public float Accuracy;
        public double Altitude;
        public float Speed;
        public long Timestamp;
    }

    /// <summary>
    /// Manages anti-cheat and location validation
    /// </summary>
    public class AntiCheatManager : MonoBehaviour
    {
        public static AntiCheatManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float locationUpdateInterval = 10f;
        [SerializeField] private float maxAcceptableAccuracy = 100f; // meters
        [SerializeField] private float maxReasonableSpeed = 150f; // km/h
        [SerializeField] private bool enableDebugLogs = false;

        // Events
        public event Action<LocationValidationResult> OnLocationValidated;
        public event Action<TrustStatus> OnTrustStatusUpdated;
        public event Action<string> OnViolationDetected;
        public event Action OnSuspensionStarted;

        // State
        private FirebaseFunctions _functions;
        private string _userId;
        
        private LocationData _lastValidatedLocation;
        private TrustStatus _trustStatus;
        private bool _isLocationServiceRunning;
        private float _lastLocationUpdateTime;
        private List<LocationData> _locationHistory = new List<LocationData>();
        private int _maxHistorySize = 100;

        public TrustStatus CurrentTrustStatus => _trustStatus;
        public LocationData LastValidatedLocation => _lastValidatedLocation;
        public bool IsLocationValid => _lastValidatedLocation != null;
        public bool IsSuspended => _trustStatus?.IsSuspended ?? false;

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
            _functions = FirebaseFunctions.DefaultInstance;
            _userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

            if (!string.IsNullOrEmpty(_userId))
            {
                StartLocationValidation();
                LoadTrustStatus();
            }
        }

        private void OnDestroy()
        {
            StopLocationValidation();
        }

        /// <summary>
        /// Start location validation service
        /// </summary>
        public void StartLocationValidation()
        {
            if (_isLocationServiceRunning) return;

            StartCoroutine(LocationValidationLoop());
            _isLocationServiceRunning = true;

            if (enableDebugLogs)
            {
                Debug.Log("[AntiCheat] Location validation started");
            }
        }

        /// <summary>
        /// Stop location validation service
        /// </summary>
        public void StopLocationValidation()
        {
            _isLocationServiceRunning = false;
            StopAllCoroutines();

            if (enableDebugLogs)
            {
                Debug.Log("[AntiCheat] Location validation stopped");
            }
        }

        private System.Collections.IEnumerator LocationValidationLoop()
        {
            // Request location permission
            if (!Input.location.isEnabledByUser)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[AntiCheat] Location services not enabled");
                }
            }

            Input.location.Start(10f, 10f);

            // Wait for initialization
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.LogError("[AntiCheat] Unable to determine device location");
                yield break;
            }

            while (_isLocationServiceRunning)
            {
                if (Time.time - _lastLocationUpdateTime >= locationUpdateInterval)
                {
                    ValidateCurrentLocation();
                    _lastLocationUpdateTime = Time.time;
                }
                yield return new WaitForSeconds(1f);
            }

            Input.location.Stop();
        }

        /// <summary>
        /// Validate current device location
        /// </summary>
        public async void ValidateCurrentLocation()
        {
            if (Input.location.status != LocationServiceStatus.Running)
                return;

            var locationInfo = Input.location.lastData;
            var locationData = new LocationData
            {
                Latitude = locationInfo.latitude,
                Longitude = locationInfo.longitude,
                Accuracy = locationInfo.horizontalAccuracy,
                Altitude = locationInfo.altitude,
                Speed = 0, // Calculate from history
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            // Calculate speed from history
            if (_locationHistory.Count > 0)
            {
                var lastLocation = _locationHistory[_locationHistory.Count - 1];
                float distance = CalculateDistance(
                    lastLocation.Latitude, lastLocation.Longitude,
                    locationData.Latitude, locationData.Longitude);
                float timeDiff = (locationData.Timestamp - lastLocation.Timestamp) / 1000f; // seconds
                
                if (timeDiff > 0)
                {
                    locationData.Speed = (distance / timeDiff) * 3.6f; // km/h
                }
            }

            // Add to history
            _locationHistory.Add(locationData);
            if (_locationHistory.Count > _maxHistorySize)
            {
                _locationHistory.RemoveAt(0);
            }

            // Client-side pre-validation
            var clientValidation = PerformClientSideValidation(locationData);
            if (!clientValidation.IsValid)
            {
                OnLocationValidated?.Invoke(clientValidation);
                OnViolationDetected?.Invoke(clientValidation.Reason);
                return;
            }

            // Server-side validation
            await ValidateLocationWithServer(locationData);
        }

        /// <summary>
        /// Perform client-side validation checks
        /// </summary>
        private LocationValidationResult PerformClientSideValidation(LocationData location)
        {
            var result = new LocationValidationResult { IsValid = true, TrustScore = 1f };

            // Check accuracy
            if (location.Accuracy > maxAcceptableAccuracy)
            {
                result.IsValid = false;
                result.Reason = "Location accuracy too low";
                result.TrustScore = 0.5f;
                return result;
            }

            // Check for impossible speed
            if (location.Speed > maxReasonableSpeed)
            {
                result.IsValid = false;
                result.Reason = "Impossible movement speed detected";
                result.TrustScore = 0.2f;
                return result;
            }

            // Check for mock location (Android)
#if UNITY_ANDROID && !UNITY_EDITOR
            if (IsMockLocationEnabled())
            {
                result.IsValid = false;
                result.Reason = "Mock location detected";
                result.TrustScore = 0f;
                return result;
            }
#endif

            // Check for jailbreak/root
            if (IsDeviceCompromised())
            {
                result.TrustScore = 0.7f; // Reduced trust but not blocked
                result.RequiresVerification = true;
            }

            return result;
        }

        /// <summary>
        /// Validate location with server
        /// </summary>
        public async Task<LocationValidationResult> ValidateLocationWithServer(LocationData location)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("validateLocation");
                var data = new Dictionary<string, object>
                {
                    { "latitude", location.Latitude },
                    { "longitude", location.Longitude },
                    { "accuracy", location.Accuracy },
                    { "altitude", location.Altitude },
                    { "speed", location.Speed },
                    { "timestamp", location.Timestamp },
                    { "deviceInfo", GetDeviceFingerprint() }
                };

                var result = await callable.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                var validationResult = new LocationValidationResult
                {
                    IsValid = (bool)response["valid"],
                    Reason = response.ContainsKey("reason") ? response["reason"].ToString() : "",
                    TrustScore = response.ContainsKey("trustScore") 
                        ? Convert.ToSingle(response["trustScore"]) : 1f,
                    RequiresVerification = response.ContainsKey("requiresVerification") 
                        && (bool)response["requiresVerification"]
                };

                if (validationResult.IsValid)
                {
                    _lastValidatedLocation = location;
                }
                else
                {
                    OnViolationDetected?.Invoke(validationResult.Reason);
                }

                // Update trust status if returned
                if (response.ContainsKey("trustStatus"))
                {
                    _trustStatus = JsonConvert.DeserializeObject<TrustStatus>(
                        JsonConvert.SerializeObject(response["trustStatus"]));
                    OnTrustStatusUpdated?.Invoke(_trustStatus);

                    if (_trustStatus.IsSuspended)
                    {
                        OnSuspensionStarted?.Invoke();
                    }
                }

                OnLocationValidated?.Invoke(validationResult);

                if (enableDebugLogs)
                {
                    Debug.Log($"[AntiCheat] Location validation: {validationResult.IsValid}, Trust: {validationResult.TrustScore}");
                }

                return validationResult;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AntiCheat] Server validation failed: {e.Message}");
                
                // Fail open with reduced trust for server errors
                return new LocationValidationResult
                {
                    IsValid = true,
                    TrustScore = 0.8f,
                    RequiresVerification = true
                };
            }
        }

        /// <summary>
        /// Load trust status from server
        /// </summary>
        public async void LoadTrustStatus()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getTrustStatus");
                var result = await callable.CallAsync(null);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                _trustStatus = JsonConvert.DeserializeObject<TrustStatus>(
                    JsonConvert.SerializeObject(response["trustStatus"]));

                OnTrustStatusUpdated?.Invoke(_trustStatus);

                if (_trustStatus.IsSuspended)
                {
                    OnSuspensionStarted?.Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AntiCheat] Failed to load trust status: {e.Message}");
            }
        }

        /// <summary>
        /// Report a suspicious player
        /// </summary>
        public async Task<bool> ReportPlayer(string targetUserId, string reason, Dictionary<string, object> evidence = null)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("reportSuspiciousPlayer");
                var data = new Dictionary<string, object>
                {
                    { "targetUserId", targetUserId },
                    { "reason", reason },
                    { "evidence", evidence ?? new Dictionary<string, object>() },
                    { "reporterLocation", _lastValidatedLocation != null ? new Dictionary<string, object>
                        {
                            { "latitude", _lastValidatedLocation.Latitude },
                            { "longitude", _lastValidatedLocation.Longitude }
                        } : null
                    }
                };

                await callable.CallAsync(data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AntiCheat] Failed to report player: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verify location for a specific action (e.g., territory capture)
        /// </summary>
        public async Task<LocationValidationResult> VerifyLocationForAction(
            string actionType, 
            double targetLatitude, 
            double targetLongitude, 
            float requiredProximity)
        {
            if (_lastValidatedLocation == null)
            {
                return new LocationValidationResult
                {
                    IsValid = false,
                    Reason = "No validated location available"
                };
            }

            // Check proximity
            float distance = CalculateDistance(
                _lastValidatedLocation.Latitude, _lastValidatedLocation.Longitude,
                targetLatitude, targetLongitude);

            if (distance > requiredProximity)
            {
                return new LocationValidationResult
                {
                    IsValid = false,
                    Reason = $"Too far from target ({distance:F0}m away, need to be within {requiredProximity:F0}m)"
                };
            }

            // Validate with server
            try
            {
                var callable = _functions.GetHttpsCallable("validateActionLocation");
                var data = new Dictionary<string, object>
                {
                    { "actionType", actionType },
                    { "playerLatitude", _lastValidatedLocation.Latitude },
                    { "playerLongitude", _lastValidatedLocation.Longitude },
                    { "targetLatitude", targetLatitude },
                    { "targetLongitude", targetLongitude },
                    { "requiredProximity", requiredProximity },
                    { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                };

                var result = await callable.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                return new LocationValidationResult
                {
                    IsValid = (bool)response["valid"],
                    Reason = response.ContainsKey("reason") ? response["reason"].ToString() : "",
                    TrustScore = response.ContainsKey("trustScore") 
                        ? Convert.ToSingle(response["trustScore"]) : 1f
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"[AntiCheat] Action validation failed: {e.Message}");
                return new LocationValidationResult
                {
                    IsValid = false,
                    Reason = "Validation service unavailable"
                };
            }
        }

        /// <summary>
        /// Get device fingerprint for validation
        /// </summary>
        private Dictionary<string, object> GetDeviceFingerprint()
        {
            return new Dictionary<string, object>
            {
                { "deviceId", SystemInfo.deviceUniqueIdentifier },
                { "deviceModel", SystemInfo.deviceModel },
                { "os", SystemInfo.operatingSystem },
                { "platform", Application.platform.ToString() },
                { "appVersion", Application.version },
                { "screenResolution", $"{Screen.width}x{Screen.height}" },
                { "systemLanguage", Application.systemLanguage.ToString() },
                { "graphicsDeviceId", SystemInfo.graphicsDeviceID },
                { "processorType", SystemInfo.processorType }
            };
        }

        /// <summary>
        /// Calculate distance between two coordinates in meters
        /// </summary>
        private float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // Earth's radius in meters
            
            double lat1Rad = lat1 * Math.PI / 180;
            double lat2Rad = lat2 * Math.PI / 180;
            double deltaLat = (lat2 - lat1) * Math.PI / 180;
            double deltaLon = (lon2 - lon1) * Math.PI / 180;

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                      Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                      Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return (float)(R * c);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// Check if mock location is enabled on Android
        /// </summary>
        private bool IsMockLocationEnabled()
        {
            try
            {
                using (var settingsSecure = new AndroidJavaClass("android.provider.Settings$Secure"))
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var contentResolver = activity.Call<AndroidJavaObject>("getContentResolver"))
                {
                    // Android 6.0+
                    int mockLocation = settingsSecure.CallStatic<int>(
                        "getInt", contentResolver, "mock_location", 0);
                    return mockLocation != 0;
                }
            }
            catch
            {
                return false;
            }
        }
#endif

        /// <summary>
        /// Check if device is rooted/jailbroken
        /// </summary>
        private bool IsDeviceCompromised()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Check for common root indicators
            string[] rootPaths = {
                "/system/app/Superuser.apk",
                "/sbin/su",
                "/system/bin/su",
                "/system/xbin/su",
                "/data/local/xbin/su",
                "/data/local/bin/su",
                "/system/sd/xbin/su",
                "/system/bin/failsafe/su",
                "/data/local/su"
            };

            foreach (var path in rootPaths)
            {
                if (System.IO.File.Exists(path))
                    return true;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            // Check for common jailbreak indicators
            string[] jailbreakPaths = {
                "/Applications/Cydia.app",
                "/Library/MobileSubstrate/MobileSubstrate.dylib",
                "/bin/bash",
                "/usr/sbin/sshd",
                "/etc/apt"
            };

            foreach (var path in jailbreakPaths)
            {
                if (System.IO.File.Exists(path))
                    return true;
            }
#endif
            return false;
        }

        /// <summary>
        /// Get current location for game actions
        /// </summary>
        public (double latitude, double longitude)? GetCurrentLocation()
        {
            if (_lastValidatedLocation != null)
            {
                return (_lastValidatedLocation.Latitude, _lastValidatedLocation.Longitude);
            }
            return null;
        }

        /// <summary>
        /// Check if player is within range of a target
        /// </summary>
        public bool IsWithinRange(double targetLat, double targetLon, float rangeMeters)
        {
            if (_lastValidatedLocation == null) return false;

            float distance = CalculateDistance(
                _lastValidatedLocation.Latitude, _lastValidatedLocation.Longitude,
                targetLat, targetLon);

            return distance <= rangeMeters;
        }
    }
}
