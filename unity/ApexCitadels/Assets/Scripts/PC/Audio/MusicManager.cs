using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using ApexCitadels.Core;

namespace ApexCitadels.PC.Audio
{
    /// <summary>
    /// AAA Music Manager for Apex Citadels.
    /// Features:
    /// - Dynamic music layers based on game state
    /// - Smooth crossfading between tracks
    /// - Context-aware music selection
    /// - Intensity system for combat/exploration
    /// - Stinger support for events
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource primarySource;
        [SerializeField] private AudioSource secondarySource;
        [SerializeField] private AudioSource stingerSource;
        [SerializeField] private AudioSource ambientLayerSource;
        
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        
        [Header("Settings")]
        [SerializeField] private float crossfadeDuration = 2f;
        [SerializeField] private float stingerDuckAmount = 0.5f;
        [SerializeField] private float stingerDuckDuration = 0.3f;
        [SerializeField] private float intensityTransitionSpeed = 1f;
        
        [Header("Music Library")]
        [SerializeField] private MusicTrackLibrary trackLibrary;
        
        // Singleton
        private static MusicManager _instance;
        public static MusicManager Instance => _instance;
        
        // State
        private AudioSource _activeSource;
        private AudioSource _inactiveSource;
        private MusicContext _currentContext = MusicContext.MainMenu;
        private float _currentIntensity;
        private float _targetIntensity;
        private bool _isCrossfading;
        private MusicTrack _currentTrack;
        private Coroutine _crossfadeCoroutine;
        private Coroutine _intensityCoroutine;
        
        // Events
        public event Action<MusicTrack> OnTrackChanged;
        public event Action<MusicContext> OnContextChanged;
        
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
        }
        
        private void InitializeAudioSources()
        {
            // Create audio sources if not assigned
            if (primarySource == null)
            {
                primarySource = gameObject.AddComponent<AudioSource>();
                primarySource.playOnAwake = false;
                primarySource.loop = true;
            }
            
            if (secondarySource == null)
            {
                secondarySource = gameObject.AddComponent<AudioSource>();
                secondarySource.playOnAwake = false;
                secondarySource.loop = true;
            }
            
            if (stingerSource == null)
            {
                stingerSource = gameObject.AddComponent<AudioSource>();
                stingerSource.playOnAwake = false;
                stingerSource.loop = false;
            }
            
            if (ambientLayerSource == null)
            {
                ambientLayerSource = gameObject.AddComponent<AudioSource>();
                ambientLayerSource.playOnAwake = false;
                ambientLayerSource.loop = true;
                ambientLayerSource.volume = 0.3f;
            }
            
            _activeSource = primarySource;
            _inactiveSource = secondarySource;
        }
        
        private void Start()
        {
            // Initialize with main menu music
            SetContext(MusicContext.MainMenu);
        }
        
        private void Update()
        {
            UpdateIntensity();
        }
        
        #region Public API
        
        /// <summary>
        /// Set the current music context (triggers appropriate track)
        /// </summary>
        public void SetContext(MusicContext context)
        {
            if (_currentContext == context && _activeSource.isPlaying)
                return;
            
            _currentContext = context;
            OnContextChanged?.Invoke(context);
            
            var track = GetTrackForContext(context);
            if (track != null)
            {
                PlayTrack(track);
            }
        }
        
        /// <summary>
        /// Set music intensity (0-1, affects layer mixing)
        /// </summary>
        public void SetIntensity(float intensity)
        {
            _targetIntensity = Mathf.Clamp01(intensity);
        }
        
        /// <summary>
        /// Play a specific track with crossfade
        /// </summary>
        public void PlayTrack(MusicTrack track, bool immediate = false)
        {
            if (track == null || track.clip == null)
            {
                ApexLogger.LogWarning("MusicManager: Attempted to play null track", ApexLogger.LogCategory.Audio);
                return;
            }
            
            if (_currentTrack == track && _activeSource.isPlaying)
                return;
            
            _currentTrack = track;
            OnTrackChanged?.Invoke(track);
            
            if (immediate || !_activeSource.isPlaying)
            {
                PlayImmediate(track);
            }
            else
            {
                if (_crossfadeCoroutine != null)
                {
                    StopCoroutine(_crossfadeCoroutine);
                }
                _crossfadeCoroutine = StartCoroutine(CrossfadeToTrack(track));
            }
        }
        
        /// <summary>
        /// Play a stinger (short music sting for events)
        /// </summary>
        public void PlayStinger(AudioClip stinger, bool duckMusic = true)
        {
            if (stinger == null) return;
            
            stingerSource.clip = stinger;
            stingerSource.Play();
            
            if (duckMusic)
            {
                StartCoroutine(DuckMusicForStinger(stinger.length));
            }
        }
        
        /// <summary>
        /// Play a stinger by type
        /// </summary>
        public void PlayStinger(StingerType type)
        {
            if (trackLibrary == null) return;
            
            var stinger = trackLibrary.GetStinger(type);
            if (stinger != null)
            {
                PlayStinger(stinger.clip, stinger.duckMusic);
            }
        }
        
        /// <summary>
        /// Pause music
        /// </summary>
        public void Pause()
        {
            _activeSource.Pause();
            ambientLayerSource.Pause();
        }
        
        /// <summary>
        /// Resume music
        /// </summary>
        public void Resume()
        {
            _activeSource.UnPause();
            ambientLayerSource.UnPause();
        }
        
        /// <summary>
        /// Stop all music
        /// </summary>
        public void Stop()
        {
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
            }
            
            _activeSource.Stop();
            _inactiveSource.Stop();
            ambientLayerSource.Stop();
            _currentTrack = null;
        }
        
        /// <summary>
        /// Set master music volume
        /// </summary>
        public void SetVolume(float volume)
        {
            if (audioMixer != null)
            {
                // Convert to dB (logarithmic)
                float db = volume > 0 ? Mathf.Log10(volume) * 20f : -80f;
                audioMixer.SetFloat(musicVolumeParam, db);
            }
            else
            {
                primarySource.volume = volume;
                secondarySource.volume = volume;
            }
        }
        
        /// <summary>
        /// Fade out current music
        /// </summary>
        public void FadeOut(float duration = 2f)
        {
            StartCoroutine(FadeOutCoroutine(duration));
        }
        
        /// <summary>
        /// Fade in current music
        /// </summary>
        public void FadeIn(float duration = 2f)
        {
            StartCoroutine(FadeInCoroutine(duration));
        }
        
        #endregion
        
        #region Track Selection
        
        private MusicTrack GetTrackForContext(MusicContext context)
        {
            if (trackLibrary == null)
            {
                ApexLogger.LogWarning("MusicManager: No track library assigned", ApexLogger.LogCategory.Audio);
                return null;
            }
            
            return trackLibrary.GetTrackForContext(context, _currentIntensity);
        }
        
        #endregion
        
        #region Playback
        
        private void PlayImmediate(MusicTrack track)
        {
            _activeSource.clip = track.clip;
            _activeSource.volume = track.volume;
            _activeSource.pitch = track.pitch;
            _activeSource.Play();
            
            // Play ambient layer if available
            if (track.ambientLayer != null)
            {
                ambientLayerSource.clip = track.ambientLayer;
                ambientLayerSource.volume = track.ambientLayerVolume;
                ambientLayerSource.Play();
            }
            else
            {
                ambientLayerSource.Stop();
            }
        }
        
        private IEnumerator CrossfadeToTrack(MusicTrack track)
        {
            _isCrossfading = true;
            
            // Prepare inactive source
            _inactiveSource.clip = track.clip;
            _inactiveSource.volume = 0;
            _inactiveSource.pitch = track.pitch;
            _inactiveSource.Play();
            
            // Sync playback position if same BPM
            if (_currentTrack != null && track.bpm > 0 && _currentTrack.bpm == track.bpm)
            {
                _inactiveSource.time = _activeSource.time % track.clip.length;
            }
            
            float elapsed = 0;
            float startVolume = _activeSource.volume;
            float targetVolume = track.volume;
            
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / crossfadeDuration;
                
                // Smooth crossfade curve
                float curveT = Mathf.SmoothStep(0, 1, t);
                
                _activeSource.volume = Mathf.Lerp(startVolume, 0, curveT);
                _inactiveSource.volume = Mathf.Lerp(0, targetVolume, curveT);
                
                yield return null;
            }
            
            // Complete crossfade
            _activeSource.Stop();
            _activeSource.volume = 0;
            _inactiveSource.volume = targetVolume;
            
            // Swap sources
            var temp = _activeSource;
            _activeSource = _inactiveSource;
            _inactiveSource = temp;
            
            // Handle ambient layer
            if (track.ambientLayer != null)
            {
                ambientLayerSource.clip = track.ambientLayer;
                ambientLayerSource.volume = track.ambientLayerVolume;
                if (!ambientLayerSource.isPlaying)
                {
                    ambientLayerSource.Play();
                }
            }
            else
            {
                StartCoroutine(FadeOutAmbient(1f));
            }
            
            _isCrossfading = false;
        }
        
        private IEnumerator FadeOutAmbient(float duration)
        {
            float startVolume = ambientLayerSource.volume;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                ambientLayerSource.volume = Mathf.Lerp(startVolume, 0, elapsed / duration);
                yield return null;
            }
            
            ambientLayerSource.Stop();
        }
        
        private IEnumerator DuckMusicForStinger(float stingerLength)
        {
            float originalVolume = _activeSource.volume;
            float duckedVolume = originalVolume * stingerDuckAmount;
            
            // Duck
            float elapsed = 0;
            while (elapsed < stingerDuckDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _activeSource.volume = Mathf.Lerp(originalVolume, duckedVolume, elapsed / stingerDuckDuration);
                yield return null;
            }
            
            // Wait for stinger
            yield return new WaitForSecondsRealtime(stingerLength - stingerDuckDuration * 2);
            
            // Restore
            elapsed = 0;
            while (elapsed < stingerDuckDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _activeSource.volume = Mathf.Lerp(duckedVolume, originalVolume, elapsed / stingerDuckDuration);
                yield return null;
            }
            
            _activeSource.volume = originalVolume;
        }
        
        private IEnumerator FadeOutCoroutine(float duration)
        {
            float startVolume = _activeSource.volume;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                _activeSource.volume = Mathf.Lerp(startVolume, 0, t);
                ambientLayerSource.volume = Mathf.Lerp(ambientLayerSource.volume, 0, t);
                yield return null;
            }
            
            _activeSource.Pause();
            ambientLayerSource.Pause();
        }
        
        private IEnumerator FadeInCoroutine(float duration)
        {
            if (_currentTrack == null) yield break;
            
            _activeSource.UnPause();
            ambientLayerSource.UnPause();
            
            float targetVolume = _currentTrack.volume;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                _activeSource.volume = Mathf.Lerp(0, targetVolume, t);
                yield return null;
            }
            
            _activeSource.volume = targetVolume;
        }
        
        #endregion
        
        #region Intensity System
        
        private void UpdateIntensity()
        {
            if (Mathf.Abs(_currentIntensity - _targetIntensity) > 0.01f)
            {
                _currentIntensity = Mathf.MoveTowards(_currentIntensity, _targetIntensity, 
                    intensityTransitionSpeed * Time.unscaledDeltaTime);
                
                ApplyIntensity();
            }
        }
        
        private void ApplyIntensity()
        {
            // Modify music based on intensity
            // Higher intensity = faster tempo, louder
            if (_activeSource.isPlaying && _currentTrack != null)
            {
                float pitchMod = Mathf.Lerp(1f, _currentTrack.intensityPitchMax, _currentIntensity);
                _activeSource.pitch = _currentTrack.pitch * pitchMod;
            }
            
            // Ambient layer fades out at high intensity
            if (ambientLayerSource.isPlaying && _currentTrack != null)
            {
                float ambientVolume = Mathf.Lerp(_currentTrack.ambientLayerVolume, 0, _currentIntensity);
                ambientLayerSource.volume = ambientVolume;
            }
        }
        
        #endregion
        
        #region Properties
        
        public MusicContext CurrentContext => _currentContext;
        public MusicTrack CurrentTrack => _currentTrack;
        public float CurrentIntensity => _currentIntensity;
        public bool IsPlaying => _activeSource != null && _activeSource.isPlaying;
        public bool IsCrossfading => _isCrossfading;
        
        #endregion
    }
    
    /// <summary>
    /// Music context determines which track category to play
    /// </summary>
    public enum MusicContext
    {
        MainMenu,
        WorldMap,
        TerritoryView,
        CityBuilder,
        Combat,
        CombatVictory,
        CombatDefeat,
        BossEncounter,
        Alliance,
        Event,
        Shop,
        Cinematic,
        Credits
    }
    
    /// <summary>
    /// Stinger types for event sounds
    /// </summary>
    public enum StingerType
    {
        Victory,
        Defeat,
        LevelUp,
        Achievement,
        TerritoryCapture,
        TerritoryLost,
        AllianceJoin,
        EventStart,
        BossAppear,
        Legendary,
        Warning,
        Reward
    }
    
    /// <summary>
    /// Individual music track data
    /// </summary>
    [Serializable]
    public class MusicTrack
    {
        public string name;
        public AudioClip clip;
        [Range(0, 1)] public float volume = 0.8f;
        [Range(0.5f, 2f)] public float pitch = 1f;
        public float bpm = 120f;
        public MusicContext[] contexts;
        
        [Header("Intensity")]
        [Range(0, 1)] public float minIntensity = 0f;
        [Range(0, 1)] public float maxIntensity = 1f;
        [Range(1f, 1.5f)] public float intensityPitchMax = 1.1f;
        
        [Header("Ambient Layer")]
        public AudioClip ambientLayer;
        [Range(0, 1)] public float ambientLayerVolume = 0.3f;
    }
    
    /// <summary>
    /// Stinger data for event sounds
    /// </summary>
    [Serializable]
    public class Stinger
    {
        public StingerType type;
        public AudioClip clip;
        public bool duckMusic = true;
    }
}
