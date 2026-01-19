using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;
using ApexCitadels.Config;

namespace ApexCitadels.UI
{
    /// <summary>
    /// UI controller for the settings panel
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeText;
        [SerializeField] private TextMeshProUGUI musicVolumeText;
        [SerializeField] private TextMeshProUGUI sfxVolumeText;

        [Header("Game Settings")]
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private Toggle notificationsToggle;

        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown frameRateDropdown;

        [Header("Language")]
        [SerializeField] private TMP_Dropdown languageDropdown;

        [Header("Buttons")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        private void Start()
        {
            SetupListeners();
            LoadCurrentSettings();
        }

        private void OnEnable()
        {
            LoadCurrentSettings();
        }

        private void SetupListeners()
        {
            // Audio sliders
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            // Toggles
            if (hapticsToggle != null)
                hapticsToggle.onValueChanged.AddListener(OnHapticsChanged);

            if (notificationsToggle != null)
                notificationsToggle.onValueChanged.AddListener(OnNotificationsChanged);

            // Dropdowns
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

            if (frameRateDropdown != null)
                frameRateDropdown.onValueChanged.AddListener(OnFrameRateChanged);

            if (languageDropdown != null)
                languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

            // Buttons
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveClicked);

            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void LoadCurrentSettings()
        {
            if (SettingsManager.Instance == null) return;

            var settings = SettingsManager.Instance;

            // Audio
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = settings.MasterVolume;
                UpdateVolumeText(masterVolumeText, settings.MasterVolume);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = settings.MusicVolume;
                UpdateVolumeText(musicVolumeText, settings.MusicVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = settings.SFXVolume;
                UpdateVolumeText(sfxVolumeText, settings.SFXVolume);
            }

            // Toggles
            if (hapticsToggle != null)
                hapticsToggle.isOn = settings.HapticsEnabled;

            if (notificationsToggle != null)
                notificationsToggle.isOn = settings.NotificationsEnabled;

            // Quality dropdown
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(settings.GetQualityLevelNames()));
                qualityDropdown.value = settings.QualityLevel;
            }

            // Frame rate dropdown
            if (frameRateDropdown != null)
            {
                frameRateDropdown.ClearOptions();
                frameRateDropdown.AddOptions(new System.Collections.Generic.List<string> { "30 FPS", "60 FPS", "90 FPS", "120 FPS" });
                frameRateDropdown.value = GetFrameRateIndex(settings.TargetFrameRate);
            }

            // Language dropdown
            if (languageDropdown != null)
            {
                languageDropdown.ClearOptions();
                languageDropdown.AddOptions(new System.Collections.Generic.List<string> 
                { 
                    "English", "Español", "Français", "Deutsch", "日本語", "中文", "한국어", "Português" 
                });
                languageDropdown.value = GetLanguageIndex(settings.Language);
            }
        }

        private void UpdateVolumeText(TextMeshProUGUI text, float value)
        {
            if (text != null)
                text.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private int GetFrameRateIndex(int fps)
        {
            return fps switch
            {
                30 => 0,
                60 => 1,
                90 => 2,
                120 => 3,
                _ => 1
            };
        }

        private int GetFrameRateFromIndex(int index)
        {
            return index switch
            {
                0 => 30,
                1 => 60,
                2 => 90,
                3 => 120,
                _ => 60
            };
        }

        private int GetLanguageIndex(string code)
        {
            return code switch
            {
                "en" => 0,
                "es" => 1,
                "fr" => 2,
                "de" => 3,
                "ja" => 4,
                "zh" => 5,
                "ko" => 6,
                "pt" => 7,
                _ => 0
            };
        }

        private string GetLanguageCode(int index)
        {
            return index switch
            {
                0 => "en",
                1 => "es",
                2 => "fr",
                3 => "de",
                4 => "ja",
                5 => "zh",
                6 => "ko",
                7 => "pt",
                _ => "en"
            };
        }

        #region Event Handlers

        private void OnMasterVolumeChanged(float value)
        {
            SettingsManager.Instance?.SetMasterVolume(value);
            UpdateVolumeText(masterVolumeText, value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            SettingsManager.Instance?.SetMusicVolume(value);
            UpdateVolumeText(musicVolumeText, value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            SettingsManager.Instance?.SetSFXVolume(value);
            UpdateVolumeText(sfxVolumeText, value);
        }

        private void OnHapticsChanged(bool enabled)
        {
            SettingsManager.Instance?.SetHapticsEnabled(enabled);
        }

        private void OnNotificationsChanged(bool enabled)
        {
            SettingsManager.Instance?.SetNotificationsEnabled(enabled);
        }

        private void OnQualityChanged(int index)
        {
            SettingsManager.Instance?.SetQualityLevel(index);
        }

        private void OnFrameRateChanged(int index)
        {
            SettingsManager.Instance?.SetTargetFrameRate(GetFrameRateFromIndex(index));
        }

        private void OnLanguageChanged(int index)
        {
            SettingsManager.Instance?.SetLanguage(GetLanguageCode(index));
        }

        private void OnSaveClicked()
        {
            SettingsManager.Instance?.SaveSettings();
            ApexLogger.Log("Settings saved", ApexLogger.LogCategory.UI);
        }

        private void OnResetClicked()
        {
            SettingsManager.Instance?.ResetToDefaults();
            LoadCurrentSettings();
            ApexLogger.Log("Settings reset", ApexLogger.LogCategory.UI);
        }

        private void OnCloseClicked()
        {
            SettingsManager.Instance?.SaveSettings();
            gameObject.SetActive(false);
        }

        #endregion
    }
}
