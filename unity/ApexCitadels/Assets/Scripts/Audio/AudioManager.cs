// ============================================================================
// APEX CITADELS - AAA AUDIO MANAGER
// Unified audio system combining the best features from all audio implementations.
// This is the SINGLE source of truth for all audio in the game.
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using ApexCitadels.Core;

namespace ApexCitadels.Audio
{
    /// <summary>
    /// Sound effect categories for organization and volume control.
    /// </summary>
    public enum SFXCategory
    {
        UI,           // Button clicks, panel opens
        Combat,       // Attacks, hits, abilities
        Building,     // Construction, upgrades
        Environment,  // Footsteps, doors, ambient objects
        Notification, // Achievements, rewards
        Voice,        // Character voice lines
        Resource,     // Resource collection
        Social,       // Social interactions
        Achievement,  // Achievement unlocks
        Territory,    // Territory actions
        Ambient,      // Ambient sounds
        Explosion,    // Explosions
        Music,        // Music cues
        Misc          // Miscellaneous
    }
    
    /// <summary>
    /// Audio settings that can be saved/loaded.
    /// </summary>
    [Serializable]
    public struct AudioSettings
    {
        public float MasterVolume;
        public float MusicVolume;
        public float SFXVolume;
        public float AmbientVolume;
        public float UIVolume;
        public float VoiceVolume;
        public bool IsMuted;
        public bool HapticEnabled;
        public bool SubtitlesEnabled;
        public bool Enable3DAudio;
        public bool MuteWhenUnfocused;
        
        // Lowercase aliases for compatibility
        public float masterVolume => MasterVolume;
        public float musicVolume => MusicVolume;
        public float sfxVolume => SFXVolume;
        public float ambientVolume => AmbientVolume;
        public float uiVolume => UIVolume;
        public float voiceVolume => VoiceVolume;
        public bool vibrationEnabled => HapticEnabled;
        public bool enable3DAudio => Enable3DAudio;
        public bool muteWhenUnfocused => MuteWhenUnfocused;
        
        public static AudioSettings Default => new AudioSettings
        {
            MasterVolume = 1f,
            MusicVolume = 0.7f,
            SFXVolume = 0.8f,
            AmbientVolume = 0.6f,
            UIVolume = 0.8f,
            VoiceVolume = 1f,
            IsMuted = false,
            HapticEnabled = true,
            SubtitlesEnabled = false,
            Enable3DAudio = true,
            MuteWhenUnfocused = true
        };
    }
    
    /// <summary>
    /// AAA Audio Manager - Central audio control for Apex Citadels.
    /// 
    /// Features:
    /// - 6-channel volume control with AudioMixer integration
    /// - Audio clip caching and smart loading
    /// - 3D positional audio support
    /// - Music and ambient crossfading
    /// - Mobile haptic feedback
    /// - Settings persistence
    /// - Event system for UI binding
    /// - Scene transition audio control
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Singleton
        
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private AudioMixerGroup masterGroup;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup ambientGroup;
        [SerializeField] private AudioMixerGroup uiGroup;
        [SerializeField] private AudioMixerGroup voiceGroup;
        
        [Header("Mixer Parameter Names")]
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SFXVolume";
        [SerializeField] private string ambientVolumeParam = "AmbientVolume";
        [SerializeField] private string uiVolumeParam = "UIVolume";
        [SerializeField] private string voiceVolumeParam = "VoiceVolume";
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource uiSource;
        
        [Header("Settings")]
        [SerializeField] private bool loadClipsOnStart = true;
        [SerializeField] private string sfxResourcePath = "Audio/SFX";
        [SerializeField] private string musicResourcePath = "Audio/Music";
        [SerializeField] private string ambientResourcePath = "Audio/Ambient";
        
        [Header("3D Audio")]
        [SerializeField] private float default3DMinDistance = 1f;
        [SerializeField] private float default3DMaxDistance = 50f;
        
        [Header("Crossfade")]
        [SerializeField] private float musicCrossfadeDuration = 2f;
        [SerializeField] private float ambientCrossfadeDuration = 3f;
        
        #endregion
        
        #region Volume Properties
        
        private float _masterVolume = 1f;
        private float _musicVolume = 0.7f;
        private float _sfxVolume = 0.8f;
        private float _ambientVolume = 0.6f;
        private float _uiVolume = 0.8f;
        private float _voiceVolume = 1f;
        private bool _isMuted;
        private bool _hapticEnabled = true;
        private bool _subtitlesEnabled;
        private bool _enable3DAudio = true;
        
        public float MasterVolume
        {
            get => _masterVolume;
            set { _masterVolume = Mathf.Clamp01(value); ApplyVolume(masterVolumeParam, _masterVolume); OnMasterVolumeChanged?.Invoke(_masterVolume); SaveSettings(); }
        }
        
        public float MusicVolume
        {
            get => _musicVolume;
            set { _musicVolume = Mathf.Clamp01(value); ApplyVolume(musicVolumeParam, _musicVolume); OnMusicVolumeChanged?.Invoke(_musicVolume); SaveSettings(); }
        }
        
        public float SFXVolume
        {
            get => _sfxVolume;
            set { _sfxVolume = Mathf.Clamp01(value); ApplyVolume(sfxVolumeParam, _sfxVolume); OnSFXVolumeChanged?.Invoke(_sfxVolume); SaveSettings(); }
        }
        
        public float AmbientVolume
        {
            get => _ambientVolume;
            set { _ambientVolume = Mathf.Clamp01(value); ApplyVolume(ambientVolumeParam, _ambientVolume); OnAmbientVolumeChanged?.Invoke(_ambientVolume); SaveSettings(); }
        }
        
        public float UIVolume
        {
            get => _uiVolume;
            set { _uiVolume = Mathf.Clamp01(value); ApplyVolume(uiVolumeParam, _uiVolume); OnUIVolumeChanged?.Invoke(_uiVolume); SaveSettings(); }
        }
        
        public float VoiceVolume
        {
            get => _voiceVolume;
            set { _voiceVolume = Mathf.Clamp01(value); ApplyVolume(voiceVolumeParam, _voiceVolume); OnVoiceVolumeChanged?.Invoke(_voiceVolume); SaveSettings(); }
        }
        
        public bool IsMuted
        {
            get => _isMuted;
            set { _isMuted = value; ApplyMute(); OnMuteChanged?.Invoke(_isMuted); OnMuteStateChanged?.Invoke(_isMuted); SaveSettings(); }
        }
        
        public bool HapticEnabled
        {
            get => _hapticEnabled;
            set { _hapticEnabled = value; SaveSettings(); }
        }
        
        public bool SubtitlesEnabled
        {
            get => _subtitlesEnabled;
            set { _subtitlesEnabled = value; OnSubtitlesChanged?.Invoke(_subtitlesEnabled); SaveSettings(); }
        }
        
        public bool Enable3DAudio
        {
            get => _enable3DAudio;
            set { _enable3DAudio = value; SaveSettings(); }
        }
        
        // AudioMixerGroup accessors for other audio managers
        public AudioMixerGroup SFXGroup => sfxGroup;
        public AudioMixerGroup MusicGroup => musicGroup;
        public AudioMixerGroup AmbientGroup => ambientGroup;
        public AudioMixerGroup UIGroup => uiGroup;
        public AudioMixerGroup VoiceGroup => voiceGroup;
        
        // Additional settings state
        private bool _muteWhenUnfocused = true;
        public bool MuteWhenUnfocused
        {
            get => _muteWhenUnfocused;
            set { _muteWhenUnfocused = value; SaveSettings(); }
        }
        
        public bool Is3DAudioEnabled => _enable3DAudio;
        
        #endregion
        
        #region Events
        
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;
        public event Action<float> OnAmbientVolumeChanged;
        public event Action<float> OnUIVolumeChanged;
        public event Action<float> OnVoiceVolumeChanged;
        public event Action<bool> OnMuteChanged;
        public event Action<bool> OnMuteStateChanged; // Alias for OnMuteChanged
        public event Action<bool> OnSubtitlesChanged;
        
        #endregion
        
        #region Audio Clip Cache
        
        private Dictionary<string, AudioClip> _sfxCache = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> _musicCache = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> _ambientCache = new Dictionary<string, AudioClip>();
        private bool _clipsLoaded;
        
        #endregion
        
        #region State
        
        private Coroutine _musicCrossfadeCoroutine;
        private Coroutine _ambientCrossfadeCoroutine;
        private Coroutine _duckCoroutine;
        private AudioSource _secondaryMusicSource;
        private AudioSource _secondaryAmbientSource;
        private float _preDuckMusicVolume;
        private bool _isPaused;
        
        // Pool for 3D sound sources
        private Queue<AudioSource> _sourcePool = new Queue<AudioSource>();
        private List<AudioSource> _activeSourcesFor3D = new List<AudioSource>();
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeAudioSources();
            LoadSettings();
            
            if (loadClipsOnStart)
            {
                LoadAllAudioClips();
            }
            
            ApexLogger.Log("[AudioManager] Initialized", ApexLogger.LogCategory.Audio);
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            // Mute when app loses focus (mobile behavior)
            if (!hasFocus && !_isMuted)
            {
                AudioListener.pause = true;
            }
            else if (hasFocus && !_isMuted)
            {
                AudioListener.pause = false;
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            AudioListener.pause = pauseStatus || _isMuted;
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeAudioSources()
        {
            // Create audio sources if not assigned
            if (musicSource == null)
            {
                var musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                if (musicGroup != null) musicSource.outputAudioMixerGroup = musicGroup;
            }
            
            if (ambientSource == null)
            {
                var ambientObj = new GameObject("AmbientSource");
                ambientObj.transform.SetParent(transform);
                ambientSource = ambientObj.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
                if (ambientGroup != null) ambientSource.outputAudioMixerGroup = ambientGroup;
            }
            
            if (uiSource == null)
            {
                var uiObj = new GameObject("UISource");
                uiObj.transform.SetParent(transform);
                uiSource = uiObj.AddComponent<AudioSource>();
                uiSource.loop = false;
                uiSource.playOnAwake = false;
                if (uiGroup != null) uiSource.outputAudioMixerGroup = uiGroup;
            }
            
            // Create secondary sources for crossfading
            var secondaryMusicObj = new GameObject("SecondaryMusicSource");
            secondaryMusicObj.transform.SetParent(transform);
            _secondaryMusicSource = secondaryMusicObj.AddComponent<AudioSource>();
            _secondaryMusicSource.loop = true;
            _secondaryMusicSource.playOnAwake = false;
            if (musicGroup != null) _secondaryMusicSource.outputAudioMixerGroup = musicGroup;
            
            var secondaryAmbientObj = new GameObject("SecondaryAmbientSource");
            secondaryAmbientObj.transform.SetParent(transform);
            _secondaryAmbientSource = secondaryAmbientObj.AddComponent<AudioSource>();
            _secondaryAmbientSource.loop = true;
            _secondaryAmbientSource.playOnAwake = false;
            if (ambientGroup != null) _secondaryAmbientSource.outputAudioMixerGroup = ambientGroup;
        }
        
        private void LoadAllAudioClips()
        {
            if (_clipsLoaded) return;
            
            // Load SFX
            var sfxClips = UnityEngine.Resources.LoadAll<AudioClip>(sfxResourcePath);
            foreach (var clip in sfxClips)
            {
                _sfxCache[clip.name.ToLower()] = clip;
            }
            
            // Load Music
            var musicClips = UnityEngine.Resources.LoadAll<AudioClip>(musicResourcePath);
            foreach (var clip in musicClips)
            {
                _musicCache[clip.name.ToLower()] = clip;
            }
            
            // Load Ambient
            var ambientClips = UnityEngine.Resources.LoadAll<AudioClip>(ambientResourcePath);
            foreach (var clip in ambientClips)
            {
                _ambientCache[clip.name.ToLower()] = clip;
            }
            
            _clipsLoaded = true;
            ApexLogger.Log($"[AudioManager] Loaded {_sfxCache.Count} SFX, {_musicCache.Count} Music, {_ambientCache.Count} Ambient clips", ApexLogger.LogCategory.Audio);
        }
        
        #endregion
        
        #region SFX Playback
        
        /// <summary>
        /// Play a sound effect by name (searches cache with fuzzy matching).
        /// </summary>
        public void PlaySFX(string clipName, float volumeScale = 1f)
        {
            if (string.IsNullOrEmpty(clipName)) return;
            
            var clip = FindClip(clipName, _sfxCache);
            if (clip != null)
            {
                PlaySFXClip(clip, volumeScale);
            }
            else
            {
                ApexLogger.LogWarning($"[AudioManager] SFX not found: {clipName}", ApexLogger.LogCategory.Audio);
            }
        }
        
        /// <summary>
        /// Play an AudioClip directly as SFX.
        /// </summary>
        public void PlaySFXClip(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || _isMuted) return;
            
            uiSource.PlayOneShot(clip, volumeScale * _sfxVolume);
        }
        
        /// <summary>
        /// Play SFX at a 3D world position.
        /// </summary>
        public void PlaySFXAtPosition(string clipName, Vector3 position, float volumeScale = 1f)
        {
            if (!_enable3DAudio)
            {
                PlaySFX(clipName, volumeScale);
                return;
            }
            
            var clip = FindClip(clipName, _sfxCache);
            if (clip != null)
            {
                PlaySFXClipAtPosition(clip, position, volumeScale);
            }
        }
        
        /// <summary>
        /// Play AudioClip at a 3D world position.
        /// </summary>
        public void PlaySFXClipAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null || _isMuted) return;
            
            var source = GetPooledAudioSource();
            source.transform.position = position;
            source.clip = clip;
            source.volume = volumeScale * _sfxVolume;
            source.spatialBlend = 1f; // Full 3D
            source.minDistance = default3DMinDistance;
            source.maxDistance = default3DMaxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
            
            StartCoroutine(ReturnToPoolWhenDone(source, clip.length + 0.1f));
        }
        
        /// <summary>
        /// Play UI sound effect.
        /// </summary>
        public void PlayUISound(string clipName, float volumeScale = 1f)
        {
            var clip = FindClip(clipName, _sfxCache);
            if (clip != null)
            {
                uiSource.PlayOneShot(clip, volumeScale * _uiVolume);
            }
        }
        
        #endregion
        
        #region Music Playback
        
        /// <summary>
        /// Play music track with crossfade.
        /// </summary>
        public void PlayMusic(string trackName, bool crossfade = true)
        {
            var clip = FindClip(trackName, _musicCache);
            if (clip != null)
            {
                PlayMusicClip(clip, crossfade);
            }
            else
            {
                ApexLogger.LogWarning($"[AudioManager] Music not found: {trackName}", ApexLogger.LogCategory.Audio);
            }
        }
        
        /// <summary>
        /// Play music AudioClip with optional crossfade.
        /// </summary>
        public void PlayMusicClip(AudioClip clip, bool crossfade = true)
        {
            if (clip == null) return;
            
            if (crossfade && musicSource.isPlaying)
            {
                if (_musicCrossfadeCoroutine != null)
                    StopCoroutine(_musicCrossfadeCoroutine);
                _musicCrossfadeCoroutine = StartCoroutine(CrossfadeMusic(clip));
            }
            else
            {
                musicSource.clip = clip;
                musicSource.volume = _musicVolume;
                musicSource.Play();
            }
        }
        
        /// <summary>
        /// Stop music with optional fade out.
        /// </summary>
        public void StopMusic(bool fadeOut = true)
        {
            if (fadeOut)
            {
                StartCoroutine(FadeOutSource(musicSource, musicCrossfadeDuration));
            }
            else
            {
                musicSource.Stop();
            }
        }
        
        /// <summary>
        /// Duck music volume temporarily (for voice lines, cutscenes).
        /// </summary>
        public void DuckMusic(float duckAmount = 0.3f, float duration = 2f)
        {
            if (_duckCoroutine != null)
                StopCoroutine(_duckCoroutine);
            _duckCoroutine = StartCoroutine(DuckMusicCoroutine(duckAmount, duration));
        }
        
        private IEnumerator DuckMusicCoroutine(float duckAmount, float duration)
        {
            _preDuckMusicVolume = musicSource.volume;
            float targetVolume = _preDuckMusicVolume * duckAmount;
            
            // Fade down
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(_preDuckMusicVolume, targetVolume, elapsed / 0.3f);
                yield return null;
            }
            
            // Wait
            yield return new WaitForSeconds(duration);
            
            // Fade back up
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(targetVolume, _preDuckMusicVolume, elapsed / 0.5f);
                yield return null;
            }
            
            musicSource.volume = _preDuckMusicVolume;
        }
        
        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            _secondaryMusicSource.clip = newClip;
            _secondaryMusicSource.volume = 0f;
            _secondaryMusicSource.Play();
            
            float elapsed = 0f;
            float startVolume = musicSource.volume;
            
            while (elapsed < musicCrossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / musicCrossfadeDuration;
                
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                _secondaryMusicSource.volume = Mathf.Lerp(0f, _musicVolume, t);
                
                yield return null;
            }
            
            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.volume = _musicVolume;
            musicSource.Play();
            
            _secondaryMusicSource.Stop();
            _secondaryMusicSource.volume = 0f;
        }
        
        #endregion
        
        #region Ambient Playback
        
        /// <summary>
        /// Play ambient sound with crossfade.
        /// </summary>
        public void PlayAmbient(string ambientName, bool crossfade = true)
        {
            var clip = FindClip(ambientName, _ambientCache);
            if (clip != null)
            {
                PlayAmbientClip(clip, crossfade);
            }
        }
        
        /// <summary>
        /// Play ambient AudioClip.
        /// </summary>
        public void PlayAmbientClip(AudioClip clip, bool crossfade = true)
        {
            if (clip == null) return;
            
            if (crossfade && ambientSource.isPlaying)
            {
                if (_ambientCrossfadeCoroutine != null)
                    StopCoroutine(_ambientCrossfadeCoroutine);
                _ambientCrossfadeCoroutine = StartCoroutine(CrossfadeAmbient(clip));
            }
            else
            {
                ambientSource.clip = clip;
                ambientSource.volume = _ambientVolume;
                ambientSource.Play();
            }
        }
        
        /// <summary>
        /// Stop ambient with optional fade out.
        /// </summary>
        public void StopAmbient(bool fadeOut = true)
        {
            if (fadeOut)
            {
                StartCoroutine(FadeOutSource(ambientSource, ambientCrossfadeDuration));
            }
            else
            {
                ambientSource.Stop();
            }
        }
        
        private IEnumerator CrossfadeAmbient(AudioClip newClip)
        {
            _secondaryAmbientSource.clip = newClip;
            _secondaryAmbientSource.volume = 0f;
            _secondaryAmbientSource.Play();
            
            float elapsed = 0f;
            float startVolume = ambientSource.volume;
            
            while (elapsed < ambientCrossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / ambientCrossfadeDuration;
                
                ambientSource.volume = Mathf.Lerp(startVolume, 0f, t);
                _secondaryAmbientSource.volume = Mathf.Lerp(0f, _ambientVolume, t);
                
                yield return null;
            }
            
            ambientSource.Stop();
            ambientSource.clip = newClip;
            ambientSource.volume = _ambientVolume;
            ambientSource.Play();
            
            _secondaryAmbientSource.Stop();
        }
        
        #endregion
        
        #region Convenience Methods
        
        // UI Sounds
        public void PlayButtonClick() => PlayUISound("button_click");
        public void PlayButtonHover() => PlayUISound("button_hover");
        public void PlayPanelOpen() => PlayUISound("panel_open");
        public void PlayPanelClose() => PlayUISound("panel_close");
        public void PlaySuccess() => PlayUISound("success");
        public void PlayError() => PlayUISound("error");
        public void PlayNotification() => PlayUISound("notification");
        
        // Game Events
        public void PlayTerritoryCapture() => PlaySFX("territory_capture");
        public void PlayBuildingComplete() => PlaySFX("building_complete");
        public void PlayResourceCollect() => PlaySFX("resource_collect");
        public void PlayLevelUp() => PlaySFX("level_up");
        public void PlayAchievement() => PlaySFX("achievement");
        
        // Combat
        public void PlayAttack() => PlaySFX("attack");
        public void PlayHit() => PlaySFX("hit");
        public void PlayDefend() => PlaySFX("defend");
        public void PlayVictory() => PlaySFX("victory");
        public void PlayDefeat() => PlaySFX("defeat");
        
        #endregion
        
        #region Volume Setter Methods
        
        // Method-style setters for compatibility with UI callbacks
        public void SetMasterVolume(float value) => MasterVolume = value;
        public void SetMusicVolume(float value) => MusicVolume = value;
        public void SetSFXVolume(float value) => SFXVolume = value;
        public void SetAmbientVolume(float value) => AmbientVolume = value;
        public void SetUIVolume(float value) => UIVolume = value;
        public void SetVoiceVolume(float value) => VoiceVolume = value;
        
        public void SetVibrationEnabled(bool enabled) => HapticEnabled = enabled;
        public void Set3DAudioEnabled(bool enabled) => Enable3DAudio = enabled;
        public void SetMuteWhenUnfocused(bool enabled) => MuteWhenUnfocused = enabled;
        
        public void ToggleMute() => IsMuted = !IsMuted;
        
        public void ResetToDefaults()
        {
            ApplySettings(AudioSettings.Default);
        }
        
        // Haptic convenience methods
        public void PlayHapticLight() => PlayHaptic(HapticType.Light);
        public void PlayHapticMedium() => PlayHaptic(HapticType.Medium);
        public void PlayHapticHeavy() => PlayHaptic(HapticType.Heavy);
        
        /// <summary>
        /// Play audio clip at 3D position with specified mixer group.
        /// </summary>
        public AudioSource PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f, AudioMixerGroup mixerGroup = null)
        {
            if (clip == null) return null;
            
            var source = GetPooledAudioSource();
            source.transform.position = position;
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = _enable3DAudio ? 1f : 0f;
            source.minDistance = default3DMinDistance;
            source.maxDistance = default3DMaxDistance;
            if (mixerGroup != null) source.outputAudioMixerGroup = mixerGroup;
            source.Play();
            
            StartCoroutine(ReturnToPoolWhenDone(source, clip.length + 0.1f));
            return source;
        }
        
        #endregion
        
        #region Scene Transitions
        
        /// <summary>
        /// Fade out all audio for scene transition.
        /// </summary>
        public void FadeOutAll(float duration = 1f)
        {
            StartCoroutine(FadeOutSource(musicSource, duration));
            StartCoroutine(FadeOutSource(ambientSource, duration));
        }
        
        /// <summary>
        /// Fade in all audio after scene transition.
        /// </summary>
        public void FadeInAll(float duration = 1f)
        {
            StartCoroutine(FadeInSource(musicSource, _musicVolume, duration));
            StartCoroutine(FadeInSource(ambientSource, _ambientVolume, duration));
        }
        
        /// <summary>
        /// Pause all audio.
        /// </summary>
        public void PauseAll()
        {
            _isPaused = true;
            AudioListener.pause = true;
        }
        
        /// <summary>
        /// Resume all audio.
        /// </summary>
        public void ResumeAll()
        {
            _isPaused = false;
            if (!_isMuted)
            {
                AudioListener.pause = false;
            }
        }
        
        private IEnumerator FadeOutSource(AudioSource source, float duration)
        {
            if (source == null) yield break;
            
            float startVolume = source.volume;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
            
            source.Stop();
            source.volume = startVolume;
        }
        
        private IEnumerator FadeInSource(AudioSource source, float targetVolume, float duration)
        {
            if (source == null || source.clip == null) yield break;
            
            source.volume = 0f;
            source.Play();
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
                yield return null;
            }
            
            source.volume = targetVolume;
        }
        
        #endregion
        
        #region Haptic Feedback
        
        /// <summary>
        /// Trigger haptic feedback on supported devices.
        /// </summary>
        public void PlayHaptic(HapticType type = HapticType.Light)
        {
            if (!_hapticEnabled) return;
            
            #if UNITY_IOS
            PlayIOSHaptic(type);
            #elif UNITY_ANDROID
            PlayAndroidHaptic(type);
            #endif
        }
        
        public enum HapticType
        {
            Light,
            Medium,
            Heavy,
            Success,
            Warning,
            Error
        }
        
        #if UNITY_IOS
        private void PlayIOSHaptic(HapticType type)
        {
            // iOS Taptic Engine feedback
            // Requires native plugin or Unity's Handheld.Vibrate
            switch (type)
            {
                case HapticType.Light:
                case HapticType.Medium:
                case HapticType.Heavy:
                case HapticType.Success:
                case HapticType.Warning:
                case HapticType.Error:
                    // Simplified - in production, use native iOS haptic API
                    Handheld.Vibrate();
                    break;
            }
        }
        #endif
        
        #if UNITY_ANDROID
        private void PlayAndroidHaptic(HapticType type)
        {
            long duration = type switch
            {
                HapticType.Light => 10,
                HapticType.Medium => 25,
                HapticType.Heavy => 50,
                HapticType.Success => 30,
                HapticType.Warning => 40,
                HapticType.Error => 60,
                _ => 20
            };
            
            // Android vibration
            #if !UNITY_EDITOR
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                vibrator.Call("vibrate", duration);
            }
            #endif
        }
        #endif
        
        #endregion
        
        #region Audio Source Pool
        
        private AudioSource GetPooledAudioSource()
        {
            AudioSource source;
            
            if (_sourcePool.Count > 0)
            {
                source = _sourcePool.Dequeue();
            }
            else
            {
                var obj = new GameObject("PooledAudioSource");
                obj.transform.SetParent(transform);
                source = obj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                if (sfxGroup != null) source.outputAudioMixerGroup = sfxGroup;
            }
            
            source.gameObject.SetActive(true);
            _activeSourcesFor3D.Add(source);
            return source;
        }
        
        private IEnumerator ReturnToPoolWhenDone(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
            _activeSourcesFor3D.Remove(source);
            _sourcePool.Enqueue(source);
        }
        
        #endregion
        
        #region Clip Finding
        
        private AudioClip FindClip(string name, Dictionary<string, AudioClip> cache)
        {
            if (string.IsNullOrEmpty(name)) return null;
            
            string lowerName = name.ToLower();
            
            // Direct match
            if (cache.TryGetValue(lowerName, out var clip))
                return clip;
            
            // Fuzzy match - try common prefixes/suffixes
            string[] prefixes = { "sfx_", "ui_", "mus_", "amb_", "cmb_", "bld_", "env_", "fx_" };
            string[] suffixes = { "_01", "_02", "_loop", "_short", "_long" };
            
            foreach (var prefix in prefixes)
            {
                if (cache.TryGetValue(prefix + lowerName, out clip))
                    return clip;
            }
            
            foreach (var suffix in suffixes)
            {
                if (cache.TryGetValue(lowerName + suffix, out clip))
                    return clip;
            }
            
            // Partial match
            foreach (var kvp in cache)
            {
                if (kvp.Key.Contains(lowerName))
                    return kvp.Value;
            }
            
            return null;
        }
        
        #endregion
        
        #region Volume Application
        
        private void ApplyVolume(string param, float value)
        {
            if (masterMixer == null) return;
            
            // Convert linear volume to dB (-80 to 0)
            float dB = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
            masterMixer.SetFloat(param, dB);
        }
        
        private void ApplyMute()
        {
            AudioListener.pause = _isMuted || _isPaused;
        }
        
        private void ApplyAllVolumes()
        {
            ApplyVolume(masterVolumeParam, _masterVolume);
            ApplyVolume(musicVolumeParam, _musicVolume);
            ApplyVolume(sfxVolumeParam, _sfxVolume);
            ApplyVolume(ambientVolumeParam, _ambientVolume);
            ApplyVolume(uiVolumeParam, _uiVolume);
            ApplyVolume(voiceVolumeParam, _voiceVolume);
            ApplyMute();
        }
        
        #endregion
        
        #region Settings Persistence
        
        private const string SETTINGS_KEY = "AudioSettings";
        
        public AudioSettings GetSettings()
        {
            return new AudioSettings
            {
                MasterVolume = _masterVolume,
                MusicVolume = _musicVolume,
                SFXVolume = _sfxVolume,
                AmbientVolume = _ambientVolume,
                UIVolume = _uiVolume,
                VoiceVolume = _voiceVolume,
                IsMuted = _isMuted,
                HapticEnabled = _hapticEnabled,
                SubtitlesEnabled = _subtitlesEnabled,
                Enable3DAudio = _enable3DAudio,
                MuteWhenUnfocused = _muteWhenUnfocused
            };
        }
        
        public void ApplySettings(AudioSettings settings)
        {
            _masterVolume = settings.MasterVolume;
            _musicVolume = settings.MusicVolume;
            _sfxVolume = settings.SFXVolume;
            _ambientVolume = settings.AmbientVolume;
            _uiVolume = settings.UIVolume;
            _voiceVolume = settings.VoiceVolume;
            _isMuted = settings.IsMuted;
            _hapticEnabled = settings.HapticEnabled;
            _subtitlesEnabled = settings.SubtitlesEnabled;
            _enable3DAudio = settings.Enable3DAudio;
            _muteWhenUnfocused = settings.MuteWhenUnfocused;
            
            ApplyAllVolumes();
            SaveSettings();
        }
        
        private void SaveSettings()
        {
            var settings = GetSettings();
            string json = JsonUtility.ToJson(settings);
            PlayerPrefs.SetString(SETTINGS_KEY, json);
            PlayerPrefs.Save();
        }
        
        private void LoadSettings()
        {
            if (PlayerPrefs.HasKey(SETTINGS_KEY))
            {
                string json = PlayerPrefs.GetString(SETTINGS_KEY);
                var settings = JsonUtility.FromJson<AudioSettings>(json);
                ApplySettings(settings);
            }
            else
            {
                ApplySettings(AudioSettings.Default);
            }
        }
        
        #endregion
    }
}
