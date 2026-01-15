using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.Audio
{
    /// <summary>
    /// UI panel for audio settings including volume controls, vibration, and 3D audio toggles.
    /// </summary>
    public class AudioSettingsUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetButton;

        [Header("Master Volume")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeText;
        [SerializeField] private Button masterMuteButton;
        [SerializeField] private Image masterMuteIcon;
        [SerializeField] private Sprite muteOnSprite;
        [SerializeField] private Sprite muteOffSprite;

        [Header("Music Volume")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private TextMeshProUGUI musicVolumeText;
        [SerializeField] private Button musicTestButton;

        [Header("SFX Volume")]
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI sfxVolumeText;
        [SerializeField] private Button sfxTestButton;

        [Header("Ambient Volume")]
        [SerializeField] private Slider ambientVolumeSlider;
        [SerializeField] private TextMeshProUGUI ambientVolumeText;

        [Header("UI Volume")]
        [SerializeField] private Slider uiVolumeSlider;
        [SerializeField] private TextMeshProUGUI uiVolumeText;

        [Header("Voice Volume")]
        [SerializeField] private Slider voiceVolumeSlider;
        [SerializeField] private TextMeshProUGUI voiceVolumeText;

        [Header("Toggles")]
        [SerializeField] private Toggle vibrationToggle;
        [SerializeField] private TextMeshProUGUI vibrationStatusText;
        [SerializeField] private Toggle spatialAudioToggle;
        [SerializeField] private TextMeshProUGUI spatialAudioStatusText;
        [SerializeField] private Toggle muteUnfocusedToggle;
        [SerializeField] private TextMeshProUGUI muteUnfocusedStatusText;

        [Header("Now Playing")]
        [SerializeField] private GameObject nowPlayingSection;
        [SerializeField] private TextMeshProUGUI currentTrackText;
        [SerializeField] private Button skipTrackButton;

        [Header("Test Sounds")]
        [SerializeField] private AudioClip testMusicClip;
        [SerializeField] private AudioClip testSFXClip;

        // State
        private bool isInitialized = false;
        private bool isUpdatingUI = false;

        private void Start()
        {
            InitializeUI();
            
            // Hide panel initially
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            UpdateNowPlaying();
        }

        #region Initialization

        private void InitializeUI()
        {
            // Setup buttons
            closeButton?.onClick.AddListener(Hide);
            resetButton?.onClick.AddListener(ResetToDefaults);
            masterMuteButton?.onClick.AddListener(ToggleMute);
            musicTestButton?.onClick.AddListener(TestMusic);
            sfxTestButton?.onClick.AddListener(TestSFX);
            skipTrackButton?.onClick.AddListener(SkipTrack);

            // Setup sliders
            SetupSlider(masterVolumeSlider, OnMasterVolumeChanged);
            SetupSlider(musicVolumeSlider, OnMusicVolumeChanged);
            SetupSlider(sfxVolumeSlider, OnSFXVolumeChanged);
            SetupSlider(ambientVolumeSlider, OnAmbientVolumeChanged);
            SetupSlider(uiVolumeSlider, OnUIVolumeChanged);
            SetupSlider(voiceVolumeSlider, OnVoiceVolumeChanged);

            // Setup toggles
            SetupToggle(vibrationToggle, OnVibrationToggled);
            SetupToggle(spatialAudioToggle, OnSpatialAudioToggled);
            SetupToggle(muteUnfocusedToggle, OnMuteUnfocusedToggled);

            // Load current values
            LoadCurrentSettings();

            isInitialized = true;
        }

        private void SetupSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
        {
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.onValueChanged.AddListener(callback);
            }
        }

        private void SetupToggle(Toggle toggle, UnityEngine.Events.UnityAction<bool> callback)
        {
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(callback);
            }
        }

        private void SubscribeToEvents()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnMasterVolumeChanged += HandleMasterVolumeChanged;
                AudioManager.Instance.OnMusicVolumeChanged += HandleMusicVolumeChanged;
                AudioManager.Instance.OnSFXVolumeChanged += HandleSFXVolumeChanged;
                AudioManager.Instance.OnAmbientVolumeChanged += HandleAmbientVolumeChanged;
                AudioManager.Instance.OnUIVolumeChanged += HandleUIVolumeChanged;
                AudioManager.Instance.OnVoiceVolumeChanged += HandleVoiceVolumeChanged;
                AudioManager.Instance.OnMuteStateChanged += HandleMuteStateChanged;
            }

            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.OnTrackChanged += HandleTrackChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnMasterVolumeChanged -= HandleMasterVolumeChanged;
                AudioManager.Instance.OnMusicVolumeChanged -= HandleMusicVolumeChanged;
                AudioManager.Instance.OnSFXVolumeChanged -= HandleSFXVolumeChanged;
                AudioManager.Instance.OnAmbientVolumeChanged -= HandleAmbientVolumeChanged;
                AudioManager.Instance.OnUIVolumeChanged -= HandleUIVolumeChanged;
                AudioManager.Instance.OnVoiceVolumeChanged -= HandleVoiceVolumeChanged;
                AudioManager.Instance.OnMuteStateChanged -= HandleMuteStateChanged;
            }

            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.OnTrackChanged -= HandleTrackChanged;
            }
        }

        private void LoadCurrentSettings()
        {
            if (AudioManager.Instance == null) return;

            isUpdatingUI = true;

            var settings = AudioManager.Instance.GetSettings();

            // Volume sliders
            SetSliderValue(masterVolumeSlider, masterVolumeText, settings.masterVolume);
            SetSliderValue(musicVolumeSlider, musicVolumeText, settings.musicVolume);
            SetSliderValue(sfxVolumeSlider, sfxVolumeText, settings.sfxVolume);
            SetSliderValue(ambientVolumeSlider, ambientVolumeText, settings.ambientVolume);
            SetSliderValue(uiVolumeSlider, uiVolumeText, settings.uiVolume);
            SetSliderValue(voiceVolumeSlider, voiceVolumeText, settings.voiceVolume);

            // Toggles
            SetToggleValue(vibrationToggle, vibrationStatusText, settings.vibrationEnabled);
            SetToggleValue(spatialAudioToggle, spatialAudioStatusText, settings.enable3DAudio);
            SetToggleValue(muteUnfocusedToggle, muteUnfocusedStatusText, settings.muteWhenUnfocused);

            // Mute button
            UpdateMuteButton(AudioManager.Instance.IsMuted);

            isUpdatingUI = false;
        }

        #endregion

        #region UI Updates

        private void SetSliderValue(Slider slider, TextMeshProUGUI text, float value)
        {
            if (slider != null)
            {
                slider.value = value;
            }
            UpdateVolumeText(text, value);
        }

        private void SetToggleValue(Toggle toggle, TextMeshProUGUI text, bool value)
        {
            if (toggle != null)
            {
                toggle.isOn = value;
            }
            UpdateToggleText(text, value);
        }

        private void UpdateVolumeText(TextMeshProUGUI text, float value)
        {
            if (text != null)
            {
                text.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        private void UpdateToggleText(TextMeshProUGUI text, bool enabled)
        {
            if (text != null)
            {
                text.text = enabled ? "ON" : "OFF";
                text.color = enabled ? Color.green : Color.gray;
            }
        }

        private void UpdateMuteButton(bool isMuted)
        {
            if (masterMuteIcon != null)
            {
                masterMuteIcon.sprite = isMuted ? muteOnSprite : muteOffSprite;
            }
        }

        private void UpdateNowPlaying()
        {
            if (nowPlayingSection == null || !settingsPanel.activeInHierarchy) return;

            if (MusicManager.Instance != null && MusicManager.Instance.CurrentTrack != null)
            {
                nowPlayingSection.SetActive(true);
                if (currentTrackText != null)
                {
                    currentTrackText.text = MusicManager.Instance.CurrentTrack.displayName;
                }
            }
            else
            {
                nowPlayingSection.SetActive(false);
            }
        }

        #endregion

        #region Slider Callbacks

        private void OnMasterVolumeChanged(float value)
        {
            if (isUpdatingUI) return;
            
            AudioManager.Instance?.SetMasterVolume(value);
            UpdateVolumeText(masterVolumeText, value);
            PlaySliderSound();
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (isUpdatingUI) return;
            
            AudioManager.Instance?.SetMusicVolume(value);
            UpdateVolumeText(musicVolumeText, value);
            PlaySliderSound();
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (isUpdatingUI) return;
            
            AudioManager.Instance?.SetSFXVolume(value);
            UpdateVolumeText(sfxVolumeText, value);
            PlaySliderSound();
        }

        private void OnAmbientVolumeChanged(float value)
        {
            if (isUpdatingUI) return;
            
            AudioManager.Instance?.SetAmbientVolume(value);
            UpdateVolumeText(ambientVolumeText, value);
            PlaySliderSound();
        }

        private void OnUIVolumeChanged(float value)
        {
            if (isUpdatingUI) return;
            
            AudioManager.Instance?.SetUIVolume(value);
            UpdateVolumeText(uiVolumeText, value);
            PlaySliderSound();
        }

        private void OnVoiceVolumeChanged(float value)
        {
            if (isUpdatingUI) return;
            
            AudioManager.Instance?.SetVoiceVolume(value);
            UpdateVolumeText(voiceVolumeText, value);
            PlaySliderSound();
        }

        private void PlaySliderSound()
        {
            SFXManager.Instance?.PlaySlider();
        }

        #endregion

        #region Toggle Callbacks

        private void OnVibrationToggled(bool enabled)
        {
            if (isUpdatingUI) return;
            
            AudioManager.Instance?.SetVibrationEnabled(enabled);
            UpdateToggleText(vibrationStatusText, enabled);
            SFXManager.Instance?.PlayToggle();

            // Test vibration when enabled
            if (enabled)
            {
                AudioManager.Instance?.PlayHapticLight();
            }
        }

        private void OnSpatialAudioToggled(bool enabled)
        {
            if (isUpdatingUI) return;
            
            AudioManager.Instance?.Set3DAudioEnabled(enabled);
            UpdateToggleText(spatialAudioStatusText, enabled);
            SFXManager.Instance?.PlayToggle();
        }

        private void OnMuteUnfocusedToggled(bool enabled)
        {
            if (isUpdatingUI) return;
            
            AudioManager.Instance?.SetMuteWhenUnfocused(enabled);
            UpdateToggleText(muteUnfocusedStatusText, enabled);
            SFXManager.Instance?.PlayToggle();
        }

        #endregion

        #region Button Callbacks

        private void ToggleMute()
        {
            AudioManager.Instance?.ToggleMute();
            SFXManager.Instance?.PlayButtonClick();
        }

        private void TestMusic()
        {
            SFXManager.Instance?.PlayButtonClick();
            
            if (testMusicClip != null && MusicManager.Instance != null)
            {
                // Play a short preview
                var track = new MusicTrack
                {
                    id = "test",
                    displayName = "Test Music",
                    clip = testMusicClip,
                    volume = 1f,
                    loop = false
                };
                MusicManager.Instance.Play(track, false);
            }
        }

        private void TestSFX()
        {
            SFXManager.Instance?.PlayButtonClick();
            
            if (testSFXClip != null)
            {
                SFXManager.Instance?.PlayClip(testSFXClip);
            }
            else
            {
                // Play default test sound
                SFXManager.Instance?.PlaySuccess();
            }

            // Also trigger haptic
            AudioManager.Instance?.PlayHapticMedium();
        }

        private void SkipTrack()
        {
            SFXManager.Instance?.PlayButtonClick();
            MusicManager.Instance?.Skip();
        }

        private void ResetToDefaults()
        {
            SFXManager.Instance?.PlayButtonClick();
            AudioManager.Instance?.ResetToDefaults();
            LoadCurrentSettings();
        }

        #endregion

        #region Event Handlers

        private void HandleMasterVolumeChanged(float value)
        {
            isUpdatingUI = true;
            SetSliderValue(masterVolumeSlider, masterVolumeText, value);
            isUpdatingUI = false;
        }

        private void HandleMusicVolumeChanged(float value)
        {
            isUpdatingUI = true;
            SetSliderValue(musicVolumeSlider, musicVolumeText, value);
            isUpdatingUI = false;
        }

        private void HandleSFXVolumeChanged(float value)
        {
            isUpdatingUI = true;
            SetSliderValue(sfxVolumeSlider, sfxVolumeText, value);
            isUpdatingUI = false;
        }

        private void HandleAmbientVolumeChanged(float value)
        {
            isUpdatingUI = true;
            SetSliderValue(ambientVolumeSlider, ambientVolumeText, value);
            isUpdatingUI = false;
        }

        private void HandleUIVolumeChanged(float value)
        {
            isUpdatingUI = true;
            SetSliderValue(uiVolumeSlider, uiVolumeText, value);
            isUpdatingUI = false;
        }

        private void HandleVoiceVolumeChanged(float value)
        {
            isUpdatingUI = true;
            SetSliderValue(voiceVolumeSlider, voiceVolumeText, value);
            isUpdatingUI = false;
        }

        private void HandleMuteStateChanged(bool isMuted)
        {
            UpdateMuteButton(isMuted);
        }

        private void HandleTrackChanged(MusicTrack track)
        {
            UpdateNowPlaying();
        }

        #endregion

        #region Show/Hide

        /// <summary>
        /// Show the audio settings panel
        /// </summary>
        public void Show()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                LoadCurrentSettings();
                SFXManager.Instance?.PlayPanelOpen();
            }
        }

        /// <summary>
        /// Hide the audio settings panel
        /// </summary>
        public void Hide()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
                SFXManager.Instance?.PlayPanelClose();
            }
        }

        /// <summary>
        /// Toggle the audio settings panel
        /// </summary>
        public void Toggle()
        {
            if (settingsPanel != null)
            {
                if (settingsPanel.activeInHierarchy)
                {
                    Hide();
                }
                else
                {
                    Show();
                }
            }
        }

        /// <summary>
        /// Check if panel is visible
        /// </summary>
        public bool IsVisible => settingsPanel != null && settingsPanel.activeInHierarchy;

        #endregion
    }
}
