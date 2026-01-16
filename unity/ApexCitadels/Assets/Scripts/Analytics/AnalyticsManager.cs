using System;
using System.Collections.Generic;
using UnityEngine;

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
            Debug.LogWarning("[AnalyticsManager] Firebase SDK not installed. Running in stub mode.");
            StartSession();
        }

        public void StartSession()
        {
            _sessionId = Guid.NewGuid().ToString();
            _sessionStartTime = DateTime.UtcNow;
            _isSessionActive = true;
            OnSessionStarted?.Invoke();
            Debug.Log($"[AnalyticsManager] Session started: {_sessionId}");
        }

        public void EndSession()
        {
            _isSessionActive = false;
            OnSessionEnded?.Invoke();
            Debug.Log("[AnalyticsManager] Session ended");
        }

        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (enableDebugLogs)
                Debug.Log($"[AnalyticsManager] Event: {eventName}");
        }

        public void SetUserProperty(string property, object value)
        {
            if (enableDebugLogs)
                Debug.Log($"[AnalyticsManager] Property: {property} = {value}");
        }

        public void TrackScreenView(string screenName)
        {
            if (enableDebugLogs)
                Debug.Log($"[AnalyticsManager] Screen: {screenName}");
        }

        public void TrackPurchase(string productId, decimal amount, string currency)
        {
            if (enableDebugLogs)
                Debug.Log($"[AnalyticsManager] Purchase: {productId} - {amount} {currency}");
        }
    }
}
