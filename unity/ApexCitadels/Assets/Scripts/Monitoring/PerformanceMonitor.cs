using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
#if FIREBASE_ENABLED
using Firebase.Analytics;
using Firebase.Crashlytics;
#endif

namespace ApexCitadels.Monitoring
{
    /// <summary>
    /// Performance metrics data
    /// </summary>
    [Serializable]
    public class PerformanceMetrics
    {
        public float AverageFps;
        public float MinFps;
        public float MaxFps;
        public float FrameTimeMs;
        public long MemoryUsedMb;
        public long MemoryPeakMb;
        public int DrawCalls;
        public int TriangleCount;
        public int ActiveGameObjects;
        public float BatteryLevel;
        public float CpuTemperature;
        public float GpuTemperature;
    }

    /// <summary>
    /// Custom trace for performance monitoring
    /// </summary>
    public class PerformanceTrace : IDisposable
    {
        private readonly string _name;
        private readonly Stopwatch _stopwatch;
        private readonly Dictionary<string, object> _attributes;
        private readonly Dictionary<string, long> _metrics;
        private bool _disposed;

        public PerformanceTrace(string name)
        {
            _name = name;
            _stopwatch = Stopwatch.StartNew();
            _attributes = new Dictionary<string, object>();
            _metrics = new Dictionary<string, long>();
        }

        public void SetAttribute(string key, string value)
        {
            _attributes[key] = value;
        }

        public void IncrementMetric(string key, long value = 1)
        {
            if (_metrics.ContainsKey(key))
                _metrics[key] += value;
            else
                _metrics[key] = value;
        }

        public void SetMetric(string key, long value)
        {
            _metrics[key] = value;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _stopwatch.Stop();
            PerformanceMonitor.Instance?.EndTrace(this, _stopwatch.ElapsedMilliseconds);
        }

        public string Name => _name;
        public Dictionary<string, object> Attributes => _attributes;
        public Dictionary<string, long> Metrics => _metrics;
    }

    /// <summary>
    /// Comprehensive performance monitoring for Apex Citadels
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        public static PerformanceMonitor Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private bool enableFpsLogging = true;
        [SerializeField] private bool enableMemoryLogging = true;
        [SerializeField] private bool enableCrashlytics = true;
        [SerializeField] private float samplingIntervalSeconds = 1f;
        [SerializeField] private int fpsHistorySize = 60;

        [Header("Thresholds")]
        [SerializeField] private float lowFpsThreshold = 30f;
        [SerializeField] private float criticalFpsThreshold = 15f;
        [SerializeField] private long memoryWarningMb = 512;
        [SerializeField] private long memoryCriticalMb = 768;

        [Header("Debug Display")]
        [SerializeField] private bool showDebugOverlay = false;

        // Events
        public event Action<PerformanceMetrics> OnMetricsUpdated;
        public event Action<string, float> OnLowFpsDetected;
        public event Action<string, long> OnHighMemoryDetected;
        public event Action<Exception, string> OnErrorLogged;

        // State
        private PerformanceMetrics _currentMetrics;
        private List<float> _fpsHistory = new List<float>();
        private float _lastSampleTime;
        private int _frameCount;
        private float _fpsAccumulator;
        private bool _isInitialized;

        // Active traces
        private Dictionary<string, Stopwatch> _activeTraces = new Dictionary<string, Stopwatch>();

        // Session data
        private string _sessionId;
        private DateTime _sessionStartTime;
        private int _errorCount;
        private int _warningCount;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            _sessionId = Guid.NewGuid().ToString();
            _sessionStartTime = DateTime.UtcNow;
            _currentMetrics = new PerformanceMetrics();
            _isInitialized = true;

            // Initialize Crashlytics
#if FIREBASE_ENABLED
            if (enableCrashlytics)
            {
                try
                {
                    Crashlytics.SetUserId(SystemInfo.deviceUniqueIdentifier);
                    Crashlytics.SetCustomKey("session_id", _sessionId);
                    Crashlytics.SetCustomKey("device_model", SystemInfo.deviceModel);
                    Crashlytics.SetCustomKey("os_version", SystemInfo.operatingSystem);
                    Crashlytics.SetCustomKey("graphics_device", SystemInfo.graphicsDeviceName);
                    Crashlytics.SetCustomKey("memory_size_mb", SystemInfo.systemMemorySize.ToString());
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[PerformanceMonitor] Crashlytics init failed: {ex.Message}");
                }
            }
#endif

            // Log session start
            LogEvent("session_start", new Dictionary<string, object>
            {
                ["session_id"] = _sessionId,
                ["device_model"] = SystemInfo.deviceModel,
                ["os_version"] = SystemInfo.operatingSystem,
                ["graphics_api"] = SystemInfo.graphicsDeviceType.ToString(),
                ["memory_mb"] = SystemInfo.systemMemorySize
            });

            UnityEngine.Debug.Log($"[PerformanceMonitor] Initialized - Session: {_sessionId}");
        }

        private void Update()
        {
            if (!enableMonitoring || !_isInitialized) return;

            // Count frames
            _frameCount++;
            _fpsAccumulator += Time.deltaTime;

            // Sample at interval
            if (Time.time - _lastSampleTime >= samplingIntervalSeconds)
            {
                SampleMetrics();
                _lastSampleTime = Time.time;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                LogSessionEnd();
            }
        }

        /// <summary>
        /// Sample current performance metrics
        /// </summary>
        private void SampleMetrics()
        {
            // Calculate FPS
            float currentFps = _frameCount / _fpsAccumulator;
            _frameCount = 0;
            _fpsAccumulator = 0f;

            // Update FPS history
            _fpsHistory.Add(currentFps);
            if (_fpsHistory.Count > fpsHistorySize)
            {
                _fpsHistory.RemoveAt(0);
            }

            // Calculate FPS stats
            _currentMetrics.AverageFps = CalculateAverageFps();
            _currentMetrics.MinFps = CalculateMinFps();
            _currentMetrics.MaxFps = CalculateMaxFps();
            _currentMetrics.FrameTimeMs = 1000f / currentFps;

            // Memory stats
            if (enableMemoryLogging)
            {
                _currentMetrics.MemoryUsedMb = GC.GetTotalMemory(false) / (1024 * 1024);

                #if UNITY_ANDROID
                // Get more accurate Android memory if available
                try
                {
                    using (var activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        var activity = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
                        var runtime = new AndroidJavaClass("java.lang.Runtime").CallStatic<AndroidJavaObject>("getRuntime");
                        long totalMemory = runtime.Call<long>("totalMemory");
                        long freeMemory = runtime.Call<long>("freeMemory");
                        _currentMetrics.MemoryUsedMb = (totalMemory - freeMemory) / (1024 * 1024);
                    }
                }
                catch { }
                #endif

                // Track peak
                if (_currentMetrics.MemoryUsedMb > _currentMetrics.MemoryPeakMb)
                {
                    _currentMetrics.MemoryPeakMb = _currentMetrics.MemoryUsedMb;
                }
            }

            // Battery level (mobile only)
            #if UNITY_ANDROID || UNITY_IOS
            _currentMetrics.BatteryLevel = SystemInfo.batteryLevel;
            #endif

            // Check thresholds
            CheckPerformanceThresholds(currentFps);

            OnMetricsUpdated?.Invoke(_currentMetrics);
        }

        /// <summary>
        /// Check for performance issues
        /// </summary>
        private void CheckPerformanceThresholds(float currentFps)
        {
            // FPS checks
            if (currentFps < criticalFpsThreshold)
            {
                OnLowFpsDetected?.Invoke("critical", currentFps);
                LogEvent("performance_critical_fps", new Dictionary<string, object>
                {
                    ["fps"] = currentFps,
                    ["memory_mb"] = _currentMetrics.MemoryUsedMb,
                    ["scene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                });
            }
            else if (currentFps < lowFpsThreshold)
            {
                OnLowFpsDetected?.Invoke("low", currentFps);
            }

            // Memory checks
            if (_currentMetrics.MemoryUsedMb > memoryCriticalMb)
            {
                OnHighMemoryDetected?.Invoke("critical", _currentMetrics.MemoryUsedMb);
                LogEvent("performance_critical_memory", new Dictionary<string, object>
                {
                    ["memory_mb"] = _currentMetrics.MemoryUsedMb,
                    ["memory_peak_mb"] = _currentMetrics.MemoryPeakMb,
                    ["scene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                });
            }
            else if (_currentMetrics.MemoryUsedMb > memoryWarningMb)
            {
                OnHighMemoryDetected?.Invoke("warning", _currentMetrics.MemoryUsedMb);
            }
        }

        /// <summary>
        /// Start a custom performance trace
        /// </summary>
        public PerformanceTrace StartTrace(string traceName)
        {
            return new PerformanceTrace(traceName);
        }

        /// <summary>
        /// End a trace and log its data
        /// </summary>
        internal void EndTrace(PerformanceTrace trace, long durationMs)
        {
            var parameters = new Dictionary<string, object>
            {
                ["duration_ms"] = durationMs,
                ["session_id"] = _sessionId
            };

            foreach (var attr in trace.Attributes)
            {
                parameters[$"attr_{attr.Key}"] = attr.Value;
            }

            foreach (var metric in trace.Metrics)
            {
                parameters[$"metric_{metric.Key}"] = metric.Value;
            }

            LogEvent($"trace_{trace.Name}", parameters);
        }

        /// <summary>
        /// Log a screen view
        /// </summary>
        public void LogScreenView(string screenName, string screenClass = null)
        {
            try
            {
#if FIREBASE_ENABLED
                FirebaseAnalytics.LogEvent("screen_view", new[]
                {
                    new Parameter("screen_name", screenName),
                    new Parameter("screen_class", screenClass ?? screenName)
                });
#endif
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[PerformanceMonitor] Failed to log screen view: {ex.Message}");
            }
        }

        /// <summary>
        /// Log a custom event
        /// </summary>
        public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            try
            {
#if FIREBASE_ENABLED
                if (parameters == null || parameters.Count == 0)
                {
                    FirebaseAnalytics.LogEvent(eventName);
                    return;
                }

                var paramList = new List<Parameter>();
                foreach (var kvp in parameters)
                {
                    if (kvp.Value is string strValue)
                        paramList.Add(new Parameter(kvp.Key, strValue));
                    else if (kvp.Value is long longValue)
                        paramList.Add(new Parameter(kvp.Key, longValue));
                    else if (kvp.Value is double doubleValue)
                        paramList.Add(new Parameter(kvp.Key, doubleValue));
                    else if (kvp.Value is int intValue)
                        paramList.Add(new Parameter(kvp.Key, (long)intValue));
                    else if (kvp.Value is float floatValue)
                        paramList.Add(new Parameter(kvp.Key, (double)floatValue));
                    else
                        paramList.Add(new Parameter(kvp.Key, kvp.Value?.ToString() ?? "null"));
                }

                FirebaseAnalytics.LogEvent(eventName, paramList.ToArray());
#endif
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[PerformanceMonitor] Failed to log event: {ex.Message}");
            }
        }

        /// <summary>
        /// Log an error
        /// </summary>
        public void LogError(string message, string stackTrace = null, bool isFatal = false)
        {
            _errorCount++;

            try
            {
#if FIREBASE_ENABLED
                if (enableCrashlytics)
                {
                    Crashlytics.SetCustomKey("last_error", message);
                    Crashlytics.SetCustomKey("error_count", _errorCount.ToString());
                    Crashlytics.Log(message);

                    if (!string.IsNullOrEmpty(stackTrace))
                    {
                        Crashlytics.Log(stackTrace);
                    }
                }
#endif

                LogEvent("error_logged", new Dictionary<string, object>
                {
                    ["error_message"] = message.Length > 100 ? message.Substring(0, 100) : message,
                    ["is_fatal"] = isFatal,
                    ["error_count"] = _errorCount,
                    ["scene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                });

                OnErrorLogged?.Invoke(new Exception(message), stackTrace);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[PerformanceMonitor] Failed to log error: {ex.Message}");
            }
        }

        /// <summary>
        /// Log an exception
        /// </summary>
        public void LogException(Exception exception, string context = null)
        {
            _errorCount++;

            try
            {
#if FIREBASE_ENABLED
                if (enableCrashlytics)
                {
                    Crashlytics.SetCustomKey("last_exception", exception.GetType().Name);
                    Crashlytics.SetCustomKey("exception_context", context ?? "unknown");
                    Crashlytics.LogException(exception);
                }
#endif

                LogEvent("exception_logged", new Dictionary<string, object>
                {
                    ["exception_type"] = exception.GetType().Name,
                    ["exception_message"] = exception.Message.Length > 100 
                        ? exception.Message.Substring(0, 100) 
                        : exception.Message,
                    ["context"] = context ?? "unknown",
                    ["error_count"] = _errorCount
                });

                OnErrorLogged?.Invoke(exception, exception.StackTrace);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[PerformanceMonitor] Failed to log exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Log a warning
        /// </summary>
        public void LogWarning(string message, string context = null)
        {
            _warningCount++;

            try
            {
#if FIREBASE_ENABLED
                if (enableCrashlytics)
                {
                    Crashlytics.Log($"[Warning] {message}");
                }
#endif

                LogEvent("warning_logged", new Dictionary<string, object>
                {
                    ["warning_message"] = message.Length > 100 ? message.Substring(0, 100) : message,
                    ["context"] = context ?? "unknown",
                    ["warning_count"] = _warningCount
                });
            }
            catch { }
        }

        /// <summary>
        /// Set user property for analytics
        /// </summary>
        public void SetUserProperty(string name, string value)
        {
            try
            {
#if FIREBASE_ENABLED
                FirebaseAnalytics.SetUserProperty(name, value);

                if (enableCrashlytics)
                {
                    Crashlytics.SetCustomKey(name, value);
                }
#endif
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[PerformanceMonitor] Failed to set user property: {ex.Message}");
            }
        }

        /// <summary>
        /// Set user ID for analytics
        /// </summary>
        public void SetUserId(string userId)
        {
            try
            {
#if FIREBASE_ENABLED
                FirebaseAnalytics.SetUserId(userId);

                if (enableCrashlytics)
                {
                    Crashlytics.SetUserId(userId);
                }
#endif
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[PerformanceMonitor] Failed to set user ID: {ex.Message}");
            }
        }

        /// <summary>
        /// Log session end
        /// </summary>
        private void LogSessionEnd()
        {
            var sessionDuration = (DateTime.UtcNow - _sessionStartTime).TotalSeconds;

            LogEvent("session_end", new Dictionary<string, object>
            {
                ["session_id"] = _sessionId,
                ["duration_seconds"] = sessionDuration,
                ["error_count"] = _errorCount,
                ["warning_count"] = _warningCount,
                ["avg_fps"] = _currentMetrics.AverageFps,
                ["min_fps"] = _currentMetrics.MinFps,
                ["memory_peak_mb"] = _currentMetrics.MemoryPeakMb
            });
        }

        /// <summary>
        /// Calculate average FPS from history
        /// </summary>
        private float CalculateAverageFps()
        {
            if (_fpsHistory.Count == 0) return 0;

            float sum = 0;
            foreach (var fps in _fpsHistory)
            {
                sum += fps;
            }
            return sum / _fpsHistory.Count;
        }

        /// <summary>
        /// Calculate minimum FPS from history
        /// </summary>
        private float CalculateMinFps()
        {
            if (_fpsHistory.Count == 0) return 0;

            float min = float.MaxValue;
            foreach (var fps in _fpsHistory)
            {
                if (fps < min) min = fps;
            }
            return min;
        }

        /// <summary>
        /// Calculate maximum FPS from history
        /// </summary>
        private float CalculateMaxFps()
        {
            if (_fpsHistory.Count == 0) return 0;

            float max = 0;
            foreach (var fps in _fpsHistory)
            {
                if (fps > max) max = fps;
            }
            return max;
        }

        /// <summary>
        /// Get current metrics
        /// </summary>
        public PerformanceMetrics GetCurrentMetrics() => _currentMetrics;

        /// <summary>
        /// Get session ID
        /// </summary>
        public string GetSessionId() => _sessionId;

        /// <summary>
        /// Force garbage collection and log memory change
        /// </summary>
        public void ForceGarbageCollection()
        {
            long beforeMb = GC.GetTotalMemory(false) / (1024 * 1024);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long afterMb = GC.GetTotalMemory(false) / (1024 * 1024);

            LogEvent("gc_forced", new Dictionary<string, object>
            {
                ["memory_before_mb"] = beforeMb,
                ["memory_after_mb"] = afterMb,
                ["memory_freed_mb"] = beforeMb - afterMb
            });

            UnityEngine.Debug.Log($"[PerformanceMonitor] GC: {beforeMb}MB -> {afterMb}MB (freed {beforeMb - afterMb}MB)");
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!showDebugOverlay || !_isInitialized) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"FPS: {_currentMetrics.AverageFps:F1} (min: {_currentMetrics.MinFps:F1})");
            GUILayout.Label($"Frame Time: {_currentMetrics.FrameTimeMs:F2}ms");
            GUILayout.Label($"Memory: {_currentMetrics.MemoryUsedMb}MB (peak: {_currentMetrics.MemoryPeakMb}MB)");
            GUILayout.Label($"Session: {_sessionId.Substring(0, 8)}...");
            GUILayout.Label($"Errors: {_errorCount} | Warnings: {_warningCount}");

            if (GUILayout.Button("Force GC"))
            {
                ForceGarbageCollection();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        #endif
    }
}
