using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.Audio
{
    /// <summary>
    /// Central Audio Manager that coordinates all audio systems.
    /// Features:
    /// - Unified volume control
    /// - Audio mixer integration
    /// - Settings persistence
    /// - Audio ducking coordination
    /// - Performance management
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer masterMixer;
        
        [Header("Mixer Parameters")]
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SFXVolume";
        [SerializeField] private string uiVolumeParam = "UIVolume";
        [SerializeField] private string ambientVolumeParam = "AmbientVolume";
        [SerializeField] private string voiceVolumeParam = "VoiceVolume";
        
        [Header("Default Settings")]
        [SerializeField] private AudioSettings defaultSettings;
        
        // Singleton
        private static AudioManager _instance;
        public static AudioManager Instance => _instance;
        
        // Current settings
        private AudioSettings _currentSettings;
        
        // References to sub-systems
        private MusicManager _musicManager;
        private UISoundManager _uiSoundManager;
        private CombatSFXManager _combatSfxManager;
        private AmbientAudioManager _ambientManager;
        private VoiceLineManager _voiceManager;
        
        // Events
        public event Action<AudioSettings> OnSettingsChanged;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Load settings
            LoadSettings();
        }
        
        private void Start()
        {
            // Find audio subsystems
            _musicManager = MusicManager.Instance;
            _uiSoundManager = UISoundManager.Instance;
            _combatSfxManager = CombatSFXManager.Instance;
            _ambientManager = AmbientAudioManager.Instance;
            _voiceManager = VoiceLineManager.Instance;
            
            // Apply current settings
            ApplySettings();
        }
        
        #region Public API - Volume Control
        
        /// <summary>
        /// Set master volume (0-1)
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            _currentSettings.masterVolume = Mathf.Clamp01(volume);
            SetMixerVolume(masterVolumeParam, volume);
            SaveSettings();
            OnSettingsChanged?.Invoke(_currentSettings);
        }
        
        /// <summary>
        /// Set music volume (0-1)
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            _currentSettings.musicVolume = Mathf.Clamp01(volume);
            SetMixerVolume(musicVolumeParam, volume);
            SaveSettings();
            OnSettingsChanged?.Invoke(_currentSettings);
        }
        
        /// <summary>
        /// Set SFX volume (0-1)
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            _currentSettings.sfxVolume = Mathf.Clamp01(volume);
            SetMixerVolume(sfxVolumeParam, volume);
            SaveSettings();
            OnSettingsChanged?.Invoke(_currentSettings);
        }
        
        /// <summary>
        /// Set UI volume (0-1)
        /// </summary>
        public void SetUIVolume(float volume)
        {
            _currentSettings.uiVolume = Mathf.Clamp01(volume);
            SetMixerVolume(uiVolumeParam, volume);
            SaveSettings();
            OnSettingsChanged?.Invoke(_currentSettings);
        }
        
        /// <summary>
        /// Set ambient volume (0-1)
        /// </summary>
        public void SetAmbientVolume(float volume)
        {
            _currentSettings.ambientVolume = Mathf.Clamp01(volume);
            SetMixerVolume(ambientVolumeParam, volume);
            SaveSettings();
            OnSettingsChanged?.Invoke(_currentSettings);
        }
        
        /// <summary>
        /// Set voice volume (0-1)
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            _currentSettings.voiceVolume = Mathf.Clamp01(volume);
            SetMixerVolume(voiceVolumeParam, volume);
            SaveSettings();
            OnSettingsChanged?.Invoke(_currentSettings);
        }
        
        /// <summary>
        /// Mute/unmute all audio
        /// </summary>
        public void SetMuted(bool muted)
        {
            _currentSettings.muted = muted;
            
            if (masterMixer != null)
            {
                float db = muted ? -80f : LinearToDecibels(_currentSettings.masterVolume);
                masterMixer.SetFloat(masterVolumeParam, db);
            }
            
            SaveSettings();
            OnSettingsChanged?.Invoke(_currentSettings);
        }
        
        /// <summary>
        /// Toggle mute
        /// </summary>
        public void ToggleMute()
        {
            SetMuted(!_currentSettings.muted);
        }
        
        #endregion
        
        #region Public API - Settings
        
        /// <summary>
        /// Get current audio settings
        /// </summary>
        public AudioSettings GetSettings()
        {
            return _currentSettings;
        }
        
        /// <summary>
        /// Apply audio settings
        /// </summary>
        public void ApplySettings(AudioSettings settings)
        {
            _currentSettings = settings;
            ApplySettings();
            SaveSettings();
            OnSettingsChanged?.Invoke(_currentSettings);
        }
        
        /// <summary>
        /// Reset to default settings
        /// </summary>
        public void ResetToDefaults()
        {
            _currentSettings = defaultSettings ?? new AudioSettings();
            ApplySettings();
            SaveSettings();
            OnSettingsChanged?.Invoke(_currentSettings);
        }
        
        #endregion
        
        #region Public API - Convenience Methods
        
        /// <summary>
        /// Play UI sound
        /// </summary>
        public void PlayUISound(UISoundType type)
        {
            _uiSoundManager?.Play(type);
        }
        
        /// <summary>
        /// Set music context
        /// </summary>
        public void SetMusicContext(MusicContext context)
        {
            _musicManager?.SetContext(context);
        }
        
        /// <summary>
        /// Play stinger
        /// </summary>
        public void PlayStinger(StingerType type)
        {
            _musicManager?.PlayStinger(type);
        }
        
        /// <summary>
        /// Set ambient zone
        /// </summary>
        public void SetAmbientZone(AmbientZone zone)
        {
            _ambientManager?.SetZone(zone);
        }
        
        /// <summary>
        /// Play voice line
        /// </summary>
        public void PlayVoiceLine(string lineId)
        {
            _voiceManager?.PlayVoiceLine(lineId);
        }
        
        /// <summary>
        /// Duck music temporarily
        /// </summary>
        public void DuckMusic(float amount, float duration)
        {
            StartCoroutine(DuckMusicCoroutine(amount, duration));
        }
        
        /// <summary>
        /// Pause all audio
        /// </summary>
        public void PauseAll()
        {
            _musicManager?.Pause();
            _ambientManager?.Pause();
        }
        
        /// <summary>
        /// Resume all audio
        /// </summary>
        public void ResumeAll()
        {
            _musicManager?.Resume();
            _ambientManager?.Resume();
        }
        
        /// <summary>
        /// Fade out all audio (for scene transitions)
        /// </summary>
        public void FadeOutAll(float duration = 1f)
        {
            _musicManager?.FadeOut(duration);
            _ambientManager?.FadeOut(duration);
        }
        
        /// <summary>
        /// Fade in all audio
        /// </summary>
        public void FadeInAll(float duration = 1f)
        {
            _musicManager?.FadeIn(duration);
            _ambientManager?.FadeIn(duration);
        }
        
        #endregion
        
        #region Internal
        
        private void SetMixerVolume(string parameter, float linearVolume)
        {
            if (masterMixer == null) return;
            
            float db = LinearToDecibels(linearVolume);
            masterMixer.SetFloat(parameter, db);
        }
        
        private float LinearToDecibels(float linear)
        {
            // Avoid log(0) by clamping minimum
            linear = Mathf.Max(linear, 0.0001f);
            return Mathf.Log10(linear) * 20f;
        }
        
        private float DecibelsToLinear(float db)
        {
            return Mathf.Pow(10f, db / 20f);
        }
        
        private void ApplySettings()
        {
            if (_currentSettings == null)
            {
                _currentSettings = defaultSettings ?? new AudioSettings();
            }
            
            if (masterMixer != null)
            {
                SetMixerVolume(masterVolumeParam, _currentSettings.muted ? 0 : _currentSettings.masterVolume);
                SetMixerVolume(musicVolumeParam, _currentSettings.musicVolume);
                SetMixerVolume(sfxVolumeParam, _currentSettings.sfxVolume);
                SetMixerVolume(uiVolumeParam, _currentSettings.uiVolume);
                SetMixerVolume(ambientVolumeParam, _currentSettings.ambientVolume);
                SetMixerVolume(voiceVolumeParam, _currentSettings.voiceVolume);
            }
            
            // Apply to subsystems
            if (_voiceManager != null)
            {
                _voiceManager.SetSubtitlesEnabled(_currentSettings.subtitlesEnabled);
            }
        }
        
        private void LoadSettings()
        {
            string json = PlayerPrefs.GetString("AudioSettings", "");
            
            if (!string.IsNullOrEmpty(json))
            {
                _currentSettings = JsonUtility.FromJson<AudioSettings>(json);
            }
            else
            {
                _currentSettings = defaultSettings ?? new AudioSettings();
            }
        }
        
        private void SaveSettings()
        {
            string json = JsonUtility.ToJson(_currentSettings);
            PlayerPrefs.SetString("AudioSettings", json);
            PlayerPrefs.Save();
        }
        
        private IEnumerator DuckMusicCoroutine(float amount, float duration)
        {
            if (masterMixer == null) yield break;
            
            float currentDb;
            masterMixer.GetFloat(musicVolumeParam, out currentDb);
            float duckDb = currentDb - (amount * 20f);
            
            // Duck
            float elapsed = 0;
            float duckDuration = 0.2f;
            while (elapsed < duckDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duckDuration;
                masterMixer.SetFloat(musicVolumeParam, Mathf.Lerp(currentDb, duckDb, t));
                yield return null;
            }
            
            // Hold
            yield return new WaitForSecondsRealtime(duration);
            
            // Restore
            elapsed = 0;
            while (elapsed < duckDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duckDuration;
                masterMixer.SetFloat(musicVolumeParam, Mathf.Lerp(duckDb, currentDb, t));
                yield return null;
            }
            
            masterMixer.SetFloat(musicVolumeParam, currentDb);
        }
        
        #endregion
        
        #region Properties
        
        public float MasterVolume => _currentSettings?.masterVolume ?? 1f;
        public float MusicVolume => _currentSettings?.musicVolume ?? 0.8f;
        public float SFXVolume => _currentSettings?.sfxVolume ?? 1f;
        public float UIVolume => _currentSettings?.uiVolume ?? 1f;
        public float AmbientVolume => _currentSettings?.ambientVolume ?? 0.7f;
        public float VoiceVolume => _currentSettings?.voiceVolume ?? 1f;
        public bool IsMuted => _currentSettings?.muted ?? false;
        
        #endregion
    }
    
    /// <summary>
    /// Audio settings data
    /// </summary>
    [Serializable]
    public class AudioSettings
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public float uiVolume = 1f;
        public float ambientVolume = 0.7f;
        public float voiceVolume = 1f;
        public bool muted = false;
        public bool subtitlesEnabled = true;
    }
    
    /// <summary>
    /// Audio settings UI panel component.
    /// </summary>
    public class AudioSettingsPanel : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] private UnityEngine.UI.Slider masterSlider;
        [SerializeField] private UnityEngine.UI.Slider musicSlider;
        [SerializeField] private UnityEngine.UI.Slider sfxSlider;
        [SerializeField] private UnityEngine.UI.Slider uiSlider;
        [SerializeField] private UnityEngine.UI.Slider ambientSlider;
        [SerializeField] private UnityEngine.UI.Slider voiceSlider;
        
        [Header("Toggles")]
        [SerializeField] private UnityEngine.UI.Toggle muteToggle;
        [SerializeField] private UnityEngine.UI.Toggle subtitlesToggle;
        
        [Header("Labels")]
        [SerializeField] private TMPro.TextMeshProUGUI masterValueText;
        [SerializeField] private TMPro.TextMeshProUGUI musicValueText;
        [SerializeField] private TMPro.TextMeshProUGUI sfxValueText;
        
        private bool _isInitializing;
        
        private void OnEnable()
        {
            LoadCurrentSettings();
            
            // Subscribe to slider events
            masterSlider?.onValueChanged.AddListener(OnMasterChanged);
            musicSlider?.onValueChanged.AddListener(OnMusicChanged);
            sfxSlider?.onValueChanged.AddListener(OnSFXChanged);
            uiSlider?.onValueChanged.AddListener(OnUIChanged);
            ambientSlider?.onValueChanged.AddListener(OnAmbientChanged);
            voiceSlider?.onValueChanged.AddListener(OnVoiceChanged);
            
            muteToggle?.onValueChanged.AddListener(OnMuteChanged);
            subtitlesToggle?.onValueChanged.AddListener(OnSubtitlesChanged);
        }
        
        private void OnDisable()
        {
            masterSlider?.onValueChanged.RemoveListener(OnMasterChanged);
            musicSlider?.onValueChanged.RemoveListener(OnMusicChanged);
            sfxSlider?.onValueChanged.RemoveListener(OnSFXChanged);
            uiSlider?.onValueChanged.RemoveListener(OnUIChanged);
            ambientSlider?.onValueChanged.RemoveListener(OnAmbientChanged);
            voiceSlider?.onValueChanged.RemoveListener(OnVoiceChanged);
            
            muteToggle?.onValueChanged.RemoveListener(OnMuteChanged);
            subtitlesToggle?.onValueChanged.RemoveListener(OnSubtitlesChanged);
        }
        
        private void LoadCurrentSettings()
        {
            if (AudioManager.Instance == null) return;
            
            _isInitializing = true;
            
            var settings = AudioManager.Instance.GetSettings();
            
            if (masterSlider != null) masterSlider.value = settings.masterVolume;
            if (musicSlider != null) musicSlider.value = settings.musicVolume;
            if (sfxSlider != null) sfxSlider.value = settings.sfxVolume;
            if (uiSlider != null) uiSlider.value = settings.uiVolume;
            if (ambientSlider != null) ambientSlider.value = settings.ambientVolume;
            if (voiceSlider != null) voiceSlider.value = settings.voiceVolume;
            
            if (muteToggle != null) muteToggle.isOn = settings.muted;
            if (subtitlesToggle != null) subtitlesToggle.isOn = settings.subtitlesEnabled;
            
            UpdateLabels();
            
            _isInitializing = false;
        }
        
        private void UpdateLabels()
        {
            if (masterValueText != null && masterSlider != null)
                masterValueText.text = $"{Mathf.RoundToInt(masterSlider.value * 100)}%";
            if (musicValueText != null && musicSlider != null)
                musicValueText.text = $"{Mathf.RoundToInt(musicSlider.value * 100)}%";
            if (sfxValueText != null && sfxSlider != null)
                sfxValueText.text = $"{Mathf.RoundToInt(sfxSlider.value * 100)}%";
        }
        
        private void OnMasterChanged(float value)
        {
            if (_isInitializing) return;
            AudioManager.Instance?.SetMasterVolume(value);
            UpdateLabels();
        }
        
        private void OnMusicChanged(float value)
        {
            if (_isInitializing) return;
            AudioManager.Instance?.SetMusicVolume(value);
            UpdateLabels();
        }
        
        private void OnSFXChanged(float value)
        {
            if (_isInitializing) return;
            AudioManager.Instance?.SetSFXVolume(value);
            UpdateLabels();
            // Play test sound
            AudioManager.Instance?.PlayUISound(UISoundType.ButtonClick);
        }
        
        private void OnUIChanged(float value)
        {
            if (_isInitializing) return;
            AudioManager.Instance?.SetUIVolume(value);
        }
        
        private void OnAmbientChanged(float value)
        {
            if (_isInitializing) return;
            AudioManager.Instance?.SetAmbientVolume(value);
        }
        
        private void OnVoiceChanged(float value)
        {
            if (_isInitializing) return;
            AudioManager.Instance?.SetVoiceVolume(value);
        }
        
        private void OnMuteChanged(bool muted)
        {
            if (_isInitializing) return;
            AudioManager.Instance?.SetMuted(muted);
        }
        
        private void OnSubtitlesChanged(bool enabled)
        {
            if (_isInitializing) return;
            VoiceLineManager.Instance?.SetSubtitlesEnabled(enabled);
        }
        
        public void ResetToDefaults()
        {
            AudioManager.Instance?.ResetToDefaults();
            LoadCurrentSettings();
        }
    }
}
