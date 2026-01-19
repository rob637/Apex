using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.AntiCheat
{
    [Serializable]
    public class LocationValidationResult
    {
        public bool IsValid;
        public string Reason;
        public float TrustScore;
        public bool RequiresVerification;
    }

    [Serializable]
    public class TrustStatus
    {
        public float TrustScore = 1.0f;
        public int WarningCount;
        public bool IsSuspended;
        public DateTime? SuspendedUntil;
        public List<string> RecentViolations = new List<string>();
    }

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
    /// Manages anti-cheat and location validation.
    /// Requires Firebase SDK for full functionality.
    /// </summary>
    public class AntiCheatManager : MonoBehaviour
    {
        public static AntiCheatManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float locationUpdateInterval = 10f;
        [SerializeField] private float maxAcceptableAccuracy = 100f;
        [SerializeField] private float maxReasonableSpeed = 150f;
        [SerializeField] private bool enableDebugLogs = false;

        public event Action<LocationValidationResult> OnLocationValidated;
        public event Action<TrustStatus> OnTrustStatusUpdated;
        public event Action<string> OnViolationDetected;
        public event Action OnSuspensionStarted;

        private LocationData _lastValidatedLocation;
        private TrustStatus _trustStatus = new TrustStatus();
        private bool _isLocationServiceRunning;

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
            ApexLogger.LogWarning("Firebase SDK not installed. Running in stub mode.", ApexLogger.LogCategory.Network);
        }

        public void StartLocationValidation()
        {
            _isLocationServiceRunning = true;
            ApexLogger.Log("Location validation started (stub mode)", ApexLogger.LogCategory.Network);
        }

        public void StopLocationValidation()
        {
            _isLocationServiceRunning = false;
        }

        public LocationValidationResult ValidateLocation(LocationData location)
        {
            _lastValidatedLocation = location;
            var result = new LocationValidationResult
            {
                IsValid = true,
                TrustScore = 1.0f,
                Reason = "Stub mode - all locations valid"
            };
            OnLocationValidated?.Invoke(result);
            return result;
        }

        public void LoadTrustStatus()
        {
            OnTrustStatusUpdated?.Invoke(_trustStatus);
        }
    }
}
