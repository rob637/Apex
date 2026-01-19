using System;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.Config
{
    /// <summary>
    /// Manages game settings (audio, graphics, controls, etc.)
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        // Player Prefs Keys
        private const string KEY_MASTER_VOLUME = "Settings_MasterVolume";
        private const string KEY_MUSIC_VOLUME = "Settings_MusicVolume";
        private const string KEY_SFX_VOLUME = "Settings_SFXVolume";
        private const string KEY_HAPTICS_ENABLED = "Settings_HapticsEnabled";
        private const string KEY_NOTIFICATIONS_ENABLED = "Settings_NotificationsEnabled";
        private const string KEY_QUALITY_LEVEL = "Settings_QualityLevel";
        private const string KEY_FRAME_RATE = "Settings_FrameRate";
        private const string KEY_LANGUAGE = "Settings_Language";

        [Header("Default Settings")]
        [SerializeField] private float defaultMasterVolume = 1f;
        [SerializeField] private float defaultMusicVolume = 0.7f;
        [SerializeField] private float defaultSFXVolume = 1f;
        [SerializeField] private bool defaultHapticsEnabled = true;
        [SerializeField] private bool defaultNotificationsEnabled = true;
        [SerializeField] private int defaultQualityLevel = 2;
        [SerializeField] private int defaultFrameRate = 60;

        // Current Settings
        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public float SFXVolume { get; private set; }
        public bool HapticsEnabled { get; private set; }
        public bool NotificationsEnabled { get; private set; }
        public int QualityLevel { get; private set; }
        public int TargetFrameRate { get; private set; }
        public string Language { get; private set; }

        // Events
        public event Action OnSettingsChanged;
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettings();
            ApplySettings();
        }

        #region Load/Save

        public void LoadSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, defaultMasterVolume);
            MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, defaultMusicVolume);
            SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, defaultSFXVolume);
            HapticsEnabled = PlayerPrefs.GetInt(KEY_HAPTICS_ENABLED, defaultHapticsEnabled ? 1 : 0) == 1;
            NotificationsEnabled = PlayerPrefs.GetInt(KEY_NOTIFICATIONS_ENABLED, defaultNotificationsEnabled ? 1 : 0) == 1;
            QualityLevel = PlayerPrefs.GetInt(KEY_QUALITY_LEVEL, defaultQualityLevel);
            TargetFrameRate = PlayerPrefs.GetInt(KEY_FRAME_RATE, defaultFrameRate);
            Language = PlayerPrefs.GetString(KEY_LANGUAGE, "en");

            ApexLogger.Log("Settings loaded", LogCategory.General);
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, MasterVolume);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, MusicVolume);
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, SFXVolume);
            PlayerPrefs.SetInt(KEY_HAPTICS_ENABLED, HapticsEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KEY_NOTIFICATIONS_ENABLED, NotificationsEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KEY_QUALITY_LEVEL, QualityLevel);
            PlayerPrefs.SetInt(KEY_FRAME_RATE, TargetFrameRate);
            PlayerPrefs.SetString(KEY_LANGUAGE, Language);
            PlayerPrefs.Save();

            ApexLogger.Log("Settings saved", LogCategory.General);
        }

        public void ResetToDefaults()
        {
            MasterVolume = defaultMasterVolume;
            MusicVolume = defaultMusicVolume;
            SFXVolume = defaultSFXVolume;
            HapticsEnabled = defaultHapticsEnabled;
            NotificationsEnabled = defaultNotificationsEnabled;
            QualityLevel = defaultQualityLevel;
            TargetFrameRate = defaultFrameRate;
            Language = "en";

            SaveSettings();
            ApplySettings();
            OnSettingsChanged?.Invoke();

            ApexLogger.Log("Settings reset to defaults", LogCategory.General);
        }

        #endregion

        #region Apply Settings

        private void ApplySettings()
        {
            // Apply audio settings
            AudioListener.volume = MasterVolume;

            // Apply quality settings
            QualitySettings.SetQualityLevel(QualityLevel, true);

            // Apply frame rate
            Application.targetFrameRate = TargetFrameRate;

            ApexLogger.Log("Settings applied", LogCategory.General);
        }

        #endregion

        #region Setters

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = MasterVolume;
            OnMasterVolumeChanged?.Invoke(MasterVolume);
            OnSettingsChanged?.Invoke();
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            OnMusicVolumeChanged?.Invoke(MusicVolume);
            OnSettingsChanged?.Invoke();
        }

        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            OnSFXVolumeChanged?.Invoke(SFXVolume);
            OnSettingsChanged?.Invoke();
        }

        public void SetHapticsEnabled(bool enabled)
        {
            HapticsEnabled = enabled;
            OnSettingsChanged?.Invoke();
        }

        public void SetNotificationsEnabled(bool enabled)
        {
            NotificationsEnabled = enabled;
            OnSettingsChanged?.Invoke();
        }

        public void SetQualityLevel(int level)
        {
            QualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(QualityLevel, true);
            OnSettingsChanged?.Invoke();
        }

        public void SetTargetFrameRate(int fps)
        {
            TargetFrameRate = Mathf.Clamp(fps, 30, 120);
            Application.targetFrameRate = TargetFrameRate;
            OnSettingsChanged?.Invoke();
        }

        public void SetLanguage(string languageCode)
        {
            Language = languageCode;
            // TODO: Trigger localization system update
            OnSettingsChanged?.Invoke();
        }

        #endregion

        #region Utility

        public float GetEffectiveMusicVolume() => MasterVolume * MusicVolume;
        public float GetEffectiveSFXVolume() => MasterVolume * SFXVolume;

        public string[] GetAvailableLanguages()
        {
            return new[] { "en", "es", "fr", "de", "ja", "zh", "ko", "pt" };
        }

        public string[] GetQualityLevelNames()
        {
            return QualitySettings.names;
        }

        #endregion
    }
}
