using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.Localization
{
    /// <summary>
    /// Central localization manager for multi-language support.
    /// Handles language loading, string lookups, and runtime language switching.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private SystemLanguage defaultLanguage = SystemLanguage.English;
        [SerializeField] private bool useSystemLanguage = true;
        [SerializeField] private bool autoLoadOnStart = true;

        [Header("Language Files")]
        [SerializeField] private string localizationFolder = "Localization";
        [SerializeField] private TextAsset[] embeddedLanguageFiles;

        [Header("Fallback")]
        [SerializeField] private bool useFallbackLanguage = true;
        [SerializeField] private SystemLanguage fallbackLanguage = SystemLanguage.English;

        // Supported languages
        public static readonly SystemLanguage[] SupportedLanguages = new SystemLanguage[]
        {
            SystemLanguage.English,
            SystemLanguage.Spanish,
            SystemLanguage.French,
            SystemLanguage.German,
            SystemLanguage.Italian,
            SystemLanguage.Portuguese,
            SystemLanguage.Russian,
            SystemLanguage.Japanese,
            SystemLanguage.Korean,
            SystemLanguage.ChineseSimplified,
            SystemLanguage.ChineseTraditional,
            SystemLanguage.Arabic,
            SystemLanguage.Turkish,
            SystemLanguage.Polish,
            SystemLanguage.Dutch
        };

        // Language data
        private Dictionary<string, string> currentStrings = new Dictionary<string, string>();
        private Dictionary<string, string> fallbackStrings = new Dictionary<string, string>();
        private Dictionary<SystemLanguage, Dictionary<string, string>> cachedLanguages = new Dictionary<SystemLanguage, Dictionary<string, string>>();

        // State
        private SystemLanguage currentLanguage;
        private bool isInitialized = false;

        // Events
        public event Action<SystemLanguage> OnLanguageChanged;
        public event Action OnLocalizationReady;

        // Properties
        public SystemLanguage CurrentLanguage => currentLanguage;
        public bool IsInitialized => isInitialized;
        public bool IsRTL => IsRightToLeft(currentLanguage);

        // Player prefs key
        private const string PREF_LANGUAGE = "Settings_Language";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (autoLoadOnStart)
            {
                Initialize();
            }
        }

        #region Initialization

        /// <summary>
        /// Initialize the localization system
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            // Determine language to use
            currentLanguage = GetInitialLanguage();

            // Load fallback language first
            if (useFallbackLanguage && fallbackLanguage != currentLanguage)
            {
                LoadLanguage(fallbackLanguage, fallbackStrings);
            }

            // Load current language
            LoadLanguage(currentLanguage, currentStrings);

            isInitialized = true;
            OnLocalizationReady?.Invoke();

            ApexLogger.Log($"[LocalizationManager] Initialized with language: {currentLanguage}", ApexLogger.LogCategory.General);
        }

        private SystemLanguage GetInitialLanguage()
        {
            // Check saved preference
            if (PlayerPrefs.HasKey(PREF_LANGUAGE))
            {
                string savedLang = PlayerPrefs.GetString(PREF_LANGUAGE);
                if (Enum.TryParse(savedLang, out SystemLanguage savedLanguage))
                {
                    if (IsLanguageSupported(savedLanguage))
                    {
                        return savedLanguage;
                    }
                }
            }

            // Use system language if enabled
            if (useSystemLanguage)
            {
                SystemLanguage systemLang = Application.systemLanguage;
                if (IsLanguageSupported(systemLang))
                {
                    return systemLang;
                }
            }

            return defaultLanguage;
        }

        #endregion

        #region Language Loading

        private void LoadLanguage(SystemLanguage language, Dictionary<string, string> targetDict)
        {
            targetDict.Clear();

            // Check cache first
            if (cachedLanguages.TryGetValue(language, out Dictionary<string, string> cached))
            {
                foreach (var kvp in cached)
                {
                    targetDict[kvp.Key] = kvp.Value;
                }
                return;
            }

            // Try loading from embedded TextAssets
            string langCode = GetLanguageCode(language);
            foreach (var textAsset in embeddedLanguageFiles)
            {
                if (textAsset != null && textAsset.name.Contains(langCode))
                {
                    ParseLanguageFile(textAsset.text, targetDict);
                    CacheLanguage(language, targetDict);
                    return;
                }
            }

            // Try loading from Resources
            string resourcePath = $"{localizationFolder}/{langCode}";
            TextAsset resourceFile = UnityEngine.Resources.Load<TextAsset>(resourcePath);
            if (resourceFile != null)
            {
                ParseLanguageFile(resourceFile.text, targetDict);
                CacheLanguage(language, targetDict);
                return;
            }

            // Try loading from StreamingAssets
            string streamingPath = Path.Combine(Application.streamingAssetsPath, localizationFolder, $"{langCode}.json");
            if (File.Exists(streamingPath))
            {
                string content = File.ReadAllText(streamingPath, Encoding.UTF8);
                ParseLanguageFile(content, targetDict);
                CacheLanguage(language, targetDict);
                return;
            }

            ApexLogger.LogWarning($"[LocalizationManager] Could not load language file for: {language} ({langCode})", ApexLogger.LogCategory.General);
        }

        private void ParseLanguageFile(string content, Dictionary<string, string> targetDict)
        {
            if (string.IsNullOrEmpty(content)) return;

            try
            {
                // Parse JSON format
                var data = JsonUtility.FromJson<LocalizationData>(content);
                if (data != null && data.entries != null)
                {
                    foreach (var entry in data.entries)
                    {
                        if (!string.IsNullOrEmpty(entry.key))
                        {
                            targetDict[entry.key] = entry.value ?? "";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"[LocalizationManager] Failed to parse language file: {e.Message}", ApexLogger.LogCategory.General);
                
                // Try simple key=value format as fallback
                ParseKeyValueFormat(content, targetDict);
            }
        }

        private void ParseKeyValueFormat(string content, Dictionary<string, string> targetDict)
        {
            string[] lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith("//"))
                    continue;

                int separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex > 0)
                {
                    string key = trimmed.Substring(0, separatorIndex).Trim();
                    string value = trimmed.Substring(separatorIndex + 1).Trim();
                    
                    // Handle escaped characters
                    value = value.Replace("\\n", "\n").Replace("\\t", "\t");
                    
                    targetDict[key] = value;
                }
            }
        }

        private void CacheLanguage(SystemLanguage language, Dictionary<string, string> strings)
        {
            cachedLanguages[language] = new Dictionary<string, string>(strings);
        }

        #endregion

        #region Language Switching

        /// <summary>
        /// Change the current language
        /// </summary>
        public void SetLanguage(SystemLanguage language)
        {
            if (!IsLanguageSupported(language))
            {
                ApexLogger.LogWarning($"[LocalizationManager] Language not supported: {language}", ApexLogger.LogCategory.General);
                return;
            }

            if (language == currentLanguage) return;

            currentLanguage = language;
            
            // Save preference
            PlayerPrefs.SetString(PREF_LANGUAGE, language.ToString());
            PlayerPrefs.Save();

            // Load new language
            LoadLanguage(currentLanguage, currentStrings);

            // Notify listeners
            OnLanguageChanged?.Invoke(currentLanguage);

            ApexLogger.Log($"[LocalizationManager] Language changed to: {language}", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Set language by code (e.g., "en", "es", "fr")
        /// </summary>
        public void SetLanguageByCode(string languageCode)
        {
            SystemLanguage language = GetLanguageFromCode(languageCode);
            SetLanguage(language);
        }

        /// <summary>
        /// Cycle to next available language (for testing)
        /// </summary>
        public void CycleLanguage()
        {
            int currentIndex = Array.IndexOf(SupportedLanguages, currentLanguage);
            int nextIndex = (currentIndex + 1) % SupportedLanguages.Length;
            SetLanguage(SupportedLanguages[nextIndex]);
        }

        #endregion

        #region String Lookup

        /// <summary>
        /// Get a localized string by key
        /// </summary>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";

            // Try current language
            if (currentStrings.TryGetValue(key, out string value))
            {
                return value;
            }

            // Try fallback language
            if (useFallbackLanguage && fallbackStrings.TryGetValue(key, out string fallbackValue))
            {
                return fallbackValue;
            }

            // Return key as fallback (useful for debugging)
            ApexLogger.LogWarning($"[LocalizationManager] Missing localization key: {key}", ApexLogger.LogCategory.General);
            return $"[{key}]";
        }

        /// <summary>
        /// Get a localized string with format arguments
        /// </summary>
        public string GetFormat(string key, params object[] args)
        {
            string template = Get(key);
            
            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                ApexLogger.LogWarning($"[LocalizationManager] Format error for key: {key}", ApexLogger.LogCategory.General);
                return template;
            }
        }

        /// <summary>
        /// Get a localized string with named placeholders
        /// </summary>
        public string GetWithPlaceholders(string key, Dictionary<string, object> placeholders)
        {
            string result = Get(key);
            
            if (placeholders != null)
            {
                foreach (var kvp in placeholders)
                {
                    result = result.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? "");
                }
            }

            return result;
        }

        /// <summary>
        /// Get a pluralized string
        /// </summary>
        public string GetPlural(string key, int count)
        {
            // Try specific plural form first
            string pluralKey = count == 1 ? $"{key}_one" : $"{key}_other";
            
            if (currentStrings.ContainsKey(pluralKey))
            {
                return GetFormat(pluralKey, count);
            }

            // Fall back to base key
            return GetFormat(key, count);
        }

        /// <summary>
        /// Check if a key exists
        /// </summary>
        public bool HasKey(string key)
        {
            return currentStrings.ContainsKey(key) || 
                   (useFallbackLanguage && fallbackStrings.ContainsKey(key));
        }

        /// <summary>
        /// Get all keys for current language
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            return currentStrings.Keys;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if a language is supported
        /// </summary>
        public bool IsLanguageSupported(SystemLanguage language)
        {
            return Array.IndexOf(SupportedLanguages, language) >= 0;
        }

        /// <summary>
        /// Check if a language is right-to-left
        /// </summary>
        public static bool IsRightToLeft(SystemLanguage language)
        {
            return language == SystemLanguage.Arabic ||
                   language == SystemLanguage.Hebrew;
        }

        /// <summary>
        /// Get ISO language code from SystemLanguage
        /// </summary>
        public static string GetLanguageCode(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.English: return "en";
                case SystemLanguage.Spanish: return "es";
                case SystemLanguage.French: return "fr";
                case SystemLanguage.German: return "de";
                case SystemLanguage.Italian: return "it";
                case SystemLanguage.Portuguese: return "pt";
                case SystemLanguage.Russian: return "ru";
                case SystemLanguage.Japanese: return "ja";
                case SystemLanguage.Korean: return "ko";
                case SystemLanguage.ChineseSimplified: return "zh-CN";
                case SystemLanguage.ChineseTraditional: return "zh-TW";
                case SystemLanguage.Arabic: return "ar";
                case SystemLanguage.Turkish: return "tr";
                case SystemLanguage.Polish: return "pl";
                case SystemLanguage.Dutch: return "nl";
                default: return "en";
            }
        }

        /// <summary>
        /// Get SystemLanguage from ISO code
        /// </summary>
        public static SystemLanguage GetLanguageFromCode(string code)
        {
            switch (code?.ToLower())
            {
                case "en": return SystemLanguage.English;
                case "es": return SystemLanguage.Spanish;
                case "fr": return SystemLanguage.French;
                case "de": return SystemLanguage.German;
                case "it": return SystemLanguage.Italian;
                case "pt": return SystemLanguage.Portuguese;
                case "ru": return SystemLanguage.Russian;
                case "ja": return SystemLanguage.Japanese;
                case "ko": return SystemLanguage.Korean;
                case "zh-cn": case "zh": return SystemLanguage.ChineseSimplified;
                case "zh-tw": return SystemLanguage.ChineseTraditional;
                case "ar": return SystemLanguage.Arabic;
                case "tr": return SystemLanguage.Turkish;
                case "pl": return SystemLanguage.Polish;
                case "nl": return SystemLanguage.Dutch;
                default: return SystemLanguage.English;
            }
        }

        /// <summary>
        /// Get native language name
        /// </summary>
        public static string GetLanguageNativeName(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.English: return "English";
                case SystemLanguage.Spanish: return "Español";
                case SystemLanguage.French: return "Français";
                case SystemLanguage.German: return "Deutsch";
                case SystemLanguage.Italian: return "Italiano";
                case SystemLanguage.Portuguese: return "Português";
                case SystemLanguage.Russian: return "Русский";
                case SystemLanguage.Japanese: return "日本語";
                case SystemLanguage.Korean: return "한국어";
                case SystemLanguage.ChineseSimplified: return "简体中文";
                case SystemLanguage.ChineseTraditional: return "繁體中文";
                case SystemLanguage.Arabic: return "العربية";
                case SystemLanguage.Turkish: return "Türkçe";
                case SystemLanguage.Polish: return "Polski";
                case SystemLanguage.Dutch: return "Nederlands";
                default: return language.ToString();
            }
        }

        /// <summary>
        /// Get localized number format
        /// </summary>
        public string FormatNumber(double number)
        {
            CultureInfo culture = GetCultureInfo(currentLanguage);
            return number.ToString("N0", culture);
        }

        /// <summary>
        /// Get localized currency format
        /// </summary>
        public string FormatCurrency(double amount, string currencySymbol = "$")
        {
            CultureInfo culture = GetCultureInfo(currentLanguage);
            return currencySymbol + amount.ToString("N2", culture);
        }

        /// <summary>
        /// Get localized date format
        /// </summary>
        public string FormatDate(DateTime date)
        {
            CultureInfo culture = GetCultureInfo(currentLanguage);
            return date.ToString("d", culture);
        }

        /// <summary>
        /// Get localized time format
        /// </summary>
        public string FormatTime(DateTime time)
        {
            CultureInfo culture = GetCultureInfo(currentLanguage);
            return time.ToString("t", culture);
        }

        /// <summary>
        /// Get localized datetime format
        /// </summary>
        public string FormatDateTime(DateTime dateTime)
        {
            CultureInfo culture = GetCultureInfo(currentLanguage);
            return dateTime.ToString("g", culture);
        }

        private CultureInfo GetCultureInfo(SystemLanguage language)
        {
            try
            {
                string code = GetLanguageCode(language);
                return new CultureInfo(code);
            }
            catch
            {
                return CultureInfo.InvariantCulture;
            }
        }

        #endregion

        #region Static Shorthand

        /// <summary>
        /// Static shorthand for Get()
        /// </summary>
        public static string T(string key)
        {
            return Instance?.Get(key) ?? $"[{key}]";
        }

        /// <summary>
        /// Static shorthand for GetFormat()
        /// </summary>
        public static string TF(string key, params object[] args)
        {
            return Instance?.GetFormat(key, args) ?? $"[{key}]";
        }

        #endregion
    }

    /// <summary>
    /// Serializable localization data structure
    /// </summary>
    [Serializable]
    public class LocalizationData
    {
        public string language;
        public string languageCode;
        public LocalizationEntry[] entries;
    }

    /// <summary>
    /// Single localization entry
    /// </summary>
    [Serializable]
    public class LocalizationEntry
    {
        public string key;
        public string value;
    }
}
