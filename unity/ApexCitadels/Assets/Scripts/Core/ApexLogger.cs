using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ApexCitadels.Core
{
    /// <summary>
    /// Centralized logging system for Apex Citadels.
    /// Provides conditional logging based on categories and log levels.
    /// Use this instead of Debug.Log for production-ready code.
    /// 
    /// Usage:
    ///   ApexLogger.Log("Message");                    // General log
    ///   ApexLogger.Log("Message", LogCategory.Combat); // Categorized log
    ///   ApexLogger.LogWarning("Warning!");            // Warning level
    ///   ApexLogger.LogError("Error!");                // Error level (always shown)
    ///   
    /// Conditional compilation:
    ///   In release builds, all non-error logs are stripped unless APEX_DEBUG is defined.
    /// </summary>
    public static class ApexLogger
    {
        #region Enums

        /// <summary>
        /// Log categories for filtering output
        /// </summary>
        [Flags]
        public enum LogCategory
        {
            None = 0,
            General = 1 << 0,
            Combat = 1 << 1,
            Building = 1 << 2,
            Economy = 1 << 3,
            Map = 1 << 4,
            Network = 1 << 5,
            Firebase = 1 << 6,
            Audio = 1 << 7,
            UI = 1 << 8,
            AR = 1 << 9,
            Performance = 1 << 10,
            Replay = 1 << 11,
            Territory = 1 << 12,
            Alliance = 1 << 13,
            Social = 1 << 14,
            Events = 1 << 15,
            Loading = 1 << 16,
            All = ~0
        }

        /// <summary>
        /// Log severity levels
        /// </summary>
        public enum LogLevel
        {
            Verbose = 0,    // Most detailed, rarely needed
            Debug = 1,      // Development debugging
            Info = 2,       // Normal operational messages
            Warning = 3,    // Potential issues
            Error = 4,      // Errors that need attention
            Fatal = 5       // Critical failures
        }

        #endregion

        #region Configuration

        // Static configuration
        private static LogLevel _minimumLevel = LogLevel.Debug;
        private static LogCategory _enabledCategories = LogCategory.All;
        private static bool _includeTimestamps = true;
        private static bool _includeCategory = true;
        private static bool _includeStackTrace = false;
        private static bool _logToFile = false;
        private static StringBuilder _fileBuffer = new StringBuilder();
        private static int _fileBufferFlushThreshold = 50;
        private static int _logCount = 0;

        /// <summary>
        /// Minimum log level to display. Messages below this level are ignored.
        /// </summary>
        public static LogLevel MinimumLevel
        {
            get => _minimumLevel;
            set => _minimumLevel = value;
        }

        /// <summary>
        /// Enabled log categories. Only messages in these categories are displayed.
        /// </summary>
        public static LogCategory EnabledCategories
        {
            get => _enabledCategories;
            set => _enabledCategories = value;
        }

        /// <summary>
        /// Whether to include timestamps in log messages
        /// </summary>
        public static bool IncludeTimestamps
        {
            get => _includeTimestamps;
            set => _includeTimestamps = value;
        }

        /// <summary>
        /// Whether to include category names in log messages
        /// </summary>
        public static bool IncludeCategory
        {
            get => _includeCategory;
            set => _includeCategory = value;
        }

        /// <summary>
        /// Whether to include stack trace in log messages
        /// </summary>
        public static bool IncludeStackTrace
        {
            get => _includeStackTrace;
            set => _includeStackTrace = value;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize logger with default settings based on build type
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if UNITY_EDITOR || APEX_DEBUG
            _minimumLevel = LogLevel.Debug;
            _enabledCategories = LogCategory.All;
            _includeTimestamps = true;
            _includeCategory = true;
#else
            // Production: Only warnings and errors
            _minimumLevel = LogLevel.Warning;
            _enabledCategories = LogCategory.All;
            _includeTimestamps = false;
            _includeCategory = true;
#endif
        }

        /// <summary>
        /// Configure the logger at runtime
        /// </summary>
        public static void Configure(LogLevel minimumLevel, LogCategory categories, bool timestamps = true)
        {
            _minimumLevel = minimumLevel;
            _enabledCategories = categories;
            _includeTimestamps = timestamps;
        }

        /// <summary>
        /// Enable specific categories
        /// </summary>
        public static void EnableCategory(LogCategory category)
        {
            _enabledCategories |= category;
        }

        /// <summary>
        /// Disable specific categories
        /// </summary>
        public static void DisableCategory(LogCategory category)
        {
            _enabledCategories &= ~category;
        }

        #endregion

        #region Main Logging Methods

        /// <summary>
        /// Log a general message (Info level)
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void Log(string message, LogCategory category = LogCategory.General)
        {
            LogInternal(message, LogLevel.Info, category, null);
        }

        /// <summary>
        /// Log a message with context object
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void Log(string message, UnityEngine.Object context, LogCategory category = LogCategory.General)
        {
            LogInternal(message, LogLevel.Info, category, context);
        }

        /// <summary>
        /// Log a verbose message (most detailed)
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogVerbose(string message, LogCategory category = LogCategory.General)
        {
            LogInternal(message, LogLevel.Verbose, category, null);
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogDebug(string message, LogCategory category = LogCategory.General)
        {
            LogInternal(message, LogLevel.Debug, category, null);
        }

        /// <summary>
        /// Log an info message
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogInfo(string message, LogCategory category = LogCategory.General)
        {
            LogInternal(message, LogLevel.Info, category, null);
        }

        /// <summary>
        /// Log a warning message (always compiled in, but filtered by level)
        /// </summary>
        public static void LogWarning(string message, LogCategory category = LogCategory.General)
        {
            LogInternal(message, LogLevel.Warning, category, null);
        }

        /// <summary>
        /// Log a warning with context
        /// </summary>
        public static void LogWarning(string message, UnityEngine.Object context, LogCategory category = LogCategory.General)
        {
            LogInternal(message, LogLevel.Warning, category, context);
        }

        /// <summary>
        /// Log an error message (always shown)
        /// </summary>
        public static void LogError(string message, LogCategory category = LogCategory.General)
        {
            LogInternal(message, LogLevel.Error, category, null);
        }

        /// <summary>
        /// Log an error with context
        /// </summary>
        public static void LogError(string message, UnityEngine.Object context, LogCategory category = LogCategory.General)
        {
            LogInternal(message, LogLevel.Error, category, context);
        }

        /// <summary>
        /// Log an exception
        /// </summary>
        public static void LogException(Exception exception, LogCategory category = LogCategory.General)
        {
            LogInternal($"Exception: {exception.Message}\n{exception.StackTrace}", LogLevel.Error, category, null);
            Debug.LogException(exception);
        }

        /// <summary>
        /// Log a fatal error (critical failure)
        /// </summary>
        public static void LogFatal(string message, LogCategory category = LogCategory.General)
        {
            LogInternal($"[FATAL] {message}", LogLevel.Fatal, category, null);
        }

        #endregion

        #region Specialized Logging

        /// <summary>
        /// Log combat-related messages
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogCombat(string message)
        {
            LogInternal(message, LogLevel.Debug, LogCategory.Combat, null);
        }

        /// <summary>
        /// Log building-related messages
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogBuilding(string message)
        {
            LogInternal(message, LogLevel.Debug, LogCategory.Building, null);
        }

        /// <summary>
        /// Log economy-related messages
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogEconomy(string message)
        {
            LogInternal(message, LogLevel.Debug, LogCategory.Economy, null);
        }

        /// <summary>
        /// Log map-related messages
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogMap(string message)
        {
            LogInternal(message, LogLevel.Debug, LogCategory.Map, null);
        }

        /// <summary>
        /// Log network-related messages
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogNetwork(string message)
        {
            LogInternal(message, LogLevel.Debug, LogCategory.Network, null);
        }

        /// <summary>
        /// Log Firebase-related messages
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogFirebase(string message)
        {
            LogInternal(message, LogLevel.Debug, LogCategory.Firebase, null);
        }

        /// <summary>
        /// Log audio-related messages
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogAudio(string message)
        {
            LogInternal(message, LogLevel.Debug, LogCategory.Audio, null);
        }

        /// <summary>
        /// Log UI-related messages
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogUI(string message)
        {
            LogInternal(message, LogLevel.Debug, LogCategory.UI, null);
        }

        /// <summary>
        /// Log AR-related messages
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogAR(string message)
        {
            LogInternal(message, LogLevel.Debug, LogCategory.AR, null);
        }

        /// <summary>
        /// Log performance-related messages
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogPerformance(string message)
        {
            LogInternal(message, LogLevel.Debug, LogCategory.Performance, null);
        }

        /// <summary>
        /// Log loading-related messages
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void LogLoading(string message)
        {
            LogInternal(message, LogLevel.Debug, LogCategory.Loading, null);
        }

        #endregion

        #region Assertions

        /// <summary>
        /// Assert a condition. Logs error if condition is false.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void Assert(bool condition, string message, LogCategory category = LogCategory.General)
        {
            if (!condition)
            {
                LogInternal($"[ASSERTION FAILED] {message}", LogLevel.Error, category, null);
                Debug.Assert(condition, message);
            }
        }

        /// <summary>
        /// Assert that an object is not null
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void AssertNotNull(object obj, string objectName, LogCategory category = LogCategory.General)
        {
            if (obj == null)
            {
                LogInternal($"[ASSERTION FAILED] {objectName} is null!", LogLevel.Error, category, null);
                Debug.Assert(obj != null, $"{objectName} is null!");
            }
        }

        #endregion

        #region Performance Measurement

        /// <summary>
        /// Start a performance measurement
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void BeginSample(string name)
        {
            UnityEngine.Profiling.Profiler.BeginSample(name);
        }

        /// <summary>
        /// End a performance measurement
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void EndSample()
        {
            UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <summary>
        /// Measure and log execution time of an action
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("APEX_DEBUG")]
        public static void MeasureTime(string operationName, Action action, LogCategory category = LogCategory.Performance)
        {
            var stopwatch = Stopwatch.StartNew();
            action?.Invoke();
            stopwatch.Stop();
            LogInternal($"{operationName} took {stopwatch.ElapsedMilliseconds}ms", LogLevel.Debug, category, null);
        }

        #endregion

        #region Internal Implementation

        private static void LogInternal(string message, LogLevel level, LogCategory category, UnityEngine.Object context)
        {
            // Filter by level
            if (level < _minimumLevel)
                return;

            // Filter by category (errors always pass)
            if (level < LogLevel.Error && (category & _enabledCategories) == 0)
                return;

            // Build formatted message
            var formattedMessage = FormatMessage(message, level, category);

            // Output to Unity console
            switch (level)
            {
                case LogLevel.Error:
                case LogLevel.Fatal:
                    if (context != null)
                        Debug.LogError(formattedMessage, context);
                    else
                        Debug.LogError(formattedMessage);
                    break;

                case LogLevel.Warning:
                    if (context != null)
                        Debug.LogWarning(formattedMessage, context);
                    else
                        Debug.LogWarning(formattedMessage);
                    break;

                default:
                    if (context != null)
                        Debug.Log(formattedMessage, context);
                    else
                        Debug.Log(formattedMessage);
                    break;
            }

            // Track log count
            _logCount++;

            // File logging (if enabled)
            if (_logToFile)
            {
                _fileBuffer.AppendLine(formattedMessage);
                if (_fileBuffer.Length > _fileBufferFlushThreshold * 100)
                {
                    FlushFileBuffer();
                }
            }
        }

        private static string FormatMessage(string message, LogLevel level, LogCategory category)
        {
            var sb = new StringBuilder();

            // Timestamp
            if (_includeTimestamps)
            {
                sb.Append($"[{DateTime.Now:HH:mm:ss.fff}] ");
            }

            // Level indicator
            sb.Append(GetLevelPrefix(level));

            // Category
            if (_includeCategory && category != LogCategory.General)
            {
                sb.Append($"[{category}] ");
            }

            // Message
            sb.Append(message);

            // Stack trace (if enabled and for errors)
            if (_includeStackTrace && level >= LogLevel.Error)
            {
                sb.AppendLine();
                sb.Append(System.Environment.StackTrace);
            }

            return sb.ToString();
        }

        private static string GetLevelPrefix(LogLevel level)
        {
            return level switch
            {
                LogLevel.Verbose => "[VERBOSE] ",
                LogLevel.Debug => "[DEBUG] ",
                LogLevel.Info => "[INFO] ",
                LogLevel.Warning => "[WARN] ",
                LogLevel.Error => "[ERROR] ",
                LogLevel.Fatal => "[FATAL] ",
                _ => ""
            };
        }

        private static void FlushFileBuffer()
        {
            // File logging implementation would go here
            // For now, just clear the buffer
            _fileBuffer.Clear();
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Get the total number of log messages
        /// </summary>
        public static int LogCount => _logCount;

        /// <summary>
        /// Reset log statistics
        /// </summary>
        public static void ResetStatistics()
        {
            _logCount = 0;
        }

        #endregion
    }

    #region Extension Methods

    /// <summary>
    /// Extension methods for easy logging from MonoBehaviours
    /// </summary>
    public static class ApexLoggerExtensions
    {
        /// <summary>
        /// Log a message with this object as context
        /// </summary>
        public static void ApexLog(this MonoBehaviour mono, string message, ApexLogger.LogCategory category = ApexLogger.LogCategory.General)
        {
            ApexLogger.Log(message, mono, category);
        }

        /// <summary>
        /// Log a warning with this object as context
        /// </summary>
        public static void ApexLogWarning(this MonoBehaviour mono, string message, ApexLogger.LogCategory category = ApexLogger.LogCategory.General)
        {
            ApexLogger.LogWarning(message, mono, category);
        }

        /// <summary>
        /// Log an error with this object as context
        /// </summary>
        public static void ApexLogError(this MonoBehaviour mono, string message, ApexLogger.LogCategory category = ApexLogger.LogCategory.General)
        {
            ApexLogger.LogError(message, mono, category);
        }
    }

    #endregion
}
