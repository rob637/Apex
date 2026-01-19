using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.Analytics
{
    /// <summary>
    /// Analytics Manager - requires Firebase SDK for full functionality
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private bool enableDebugLogs = false;

        public event Action OnSessionStarted;
        public event Action OnSessionEnded;

        private string _sessionId;
        private DateTime _sessionStartTime;
        private bool _isSessionActive;

        public bool IsSessionActive => _isSessionActive;
        public string SessionId => _sessionId;

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
            ApexLogger.LogWarning("Firebase SDK not installed. Running in stub mode.", LogCategory.Performance);
            StartSession();
        }

        public void StartSession()
        {
            _sessionId = Guid.NewGuid().ToString();
            _sessionStartTime = DateTime.UtcNow;
            _isSessionActive = true;
            OnSessionStarted?.Invoke();
            ApexLogger.Log($"Session started: {_sessionId}", LogCategory.Performance);
        }

        public void EndSession()
        {
            _isSessionActive = false;
            OnSessionEnded?.Invoke();
            ApexLogger.Log("Session ended", LogCategory.Performance);
        }

        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (enableDebugLogs)
                ApexLogger.LogVerbose($"Event: {eventName}", LogCategory.Performance);
        }

        public void SetUserProperty(string property, object value)
        {
            if (enableDebugLogs)
                ApexLogger.LogVerbose($"Property: {property} = {value}", LogCategory.Performance);
        }

        public void TrackScreenView(string screenName)
        {
            if (enableDebugLogs)
                ApexLogger.LogVerbose($"Screen: {screenName}", LogCategory.Performance);
        }

        public void TrackPurchase(string productId, decimal amount, string currency)
        {
            if (enableDebugLogs)
                ApexLogger.LogVerbose($"Purchase: {productId} - {amount} {currency}", LogCategory.Performance);
        }
    }
}
