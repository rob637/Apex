using UnityEngine;
using System;
using System.Collections.Generic;
using ApexCitadels.Core;

namespace ApexCitadels.PC.Audio
{
    /// <summary>
    /// Scriptable Object for organizing music tracks and stingers.
    /// Create via Assets > Create > Apex Citadels > Music Track Library
    /// </summary>
    [CreateAssetMenu(fileName = "MusicTrackLibrary", menuName = "Apex Citadels/Music Track Library")]
    public class MusicTrackLibrary : ScriptableObject
    {
        [Header("Main Tracks")]
        [SerializeField] private List<MusicTrack> tracks = new List<MusicTrack>();
        
        [Header("Stingers")]
        [SerializeField] private List<Stinger> stingers = new List<Stinger>();
        
        [Header("Fallbacks")]
        [SerializeField] private MusicTrack defaultTrack;
        
        // Cached lookups
        private Dictionary<MusicContext, List<MusicTrack>> _contextCache;
        private Dictionary<StingerType, Stinger> _stingerCache;
        
        private void OnEnable()
        {
            BuildCaches();
        }
        
        private void BuildCaches()
        {
            // Build context cache
            _contextCache = new Dictionary<MusicContext, List<MusicTrack>>();
            
            foreach (var track in tracks)
            {
                if (track.contexts == null) continue;
                
                foreach (var context in track.contexts)
                {
                    if (!_contextCache.ContainsKey(context))
                    {
                        _contextCache[context] = new List<MusicTrack>();
                    }
                    _contextCache[context].Add(track);
                }
            }
            
            // Build stinger cache
            _stingerCache = new Dictionary<StingerType, Stinger>();
            
            foreach (var stinger in stingers)
            {
                _stingerCache[stinger.type] = stinger;
            }
        }
        
        /// <summary>
        /// Get an appropriate track for the given context and intensity
        /// </summary>
        public MusicTrack GetTrackForContext(MusicContext context, float intensity = 0.5f)
        {
            if (_contextCache == null)
            {
                BuildCaches();
            }
            
            if (!_contextCache.TryGetValue(context, out var contextTracks) || contextTracks.Count == 0)
            {
                return defaultTrack;
            }
            
            // Filter by intensity range
            var validTracks = new List<MusicTrack>();
            foreach (var track in contextTracks)
            {
                if (intensity >= track.minIntensity && intensity <= track.maxIntensity)
                {
                    validTracks.Add(track);
                }
            }
            
            if (validTracks.Count == 0)
            {
                return contextTracks[0];
            }
            
            // Random selection from valid tracks
            return validTracks[UnityEngine.Random.Range(0, validTracks.Count)];
        }
        
        /// <summary>
        /// Get a stinger by type
        /// </summary>
        public Stinger GetStinger(StingerType type)
        {
            if (_stingerCache == null)
            {
                BuildCaches();
            }
            
            _stingerCache.TryGetValue(type, out var stinger);
            return stinger;
        }
        
        /// <summary>
        /// Get all tracks for a context
        /// </summary>
        public List<MusicTrack> GetAllTracksForContext(MusicContext context)
        {
            if (_contextCache == null)
            {
                BuildCaches();
            }
            
            if (_contextCache.TryGetValue(context, out var tracks))
            {
                return new List<MusicTrack>(tracks);
            }
            
            return new List<MusicTrack>();
        }
        
        /// <summary>
        /// Get all available stingers
        /// </summary>
        public List<Stinger> GetAllStingers()
        {
            return new List<Stinger>(stingers);
        }
        
        #region Editor Support
        
        public void AddTrack(MusicTrack track)
        {
            if (!tracks.Contains(track))
            {
                tracks.Add(track);
                BuildCaches();
            }
        }
        
        public void RemoveTrack(MusicTrack track)
        {
            if (tracks.Remove(track))
            {
                BuildCaches();
            }
        }
        
        public void AddStinger(Stinger stinger)
        {
            stingers.Add(stinger);
            BuildCaches();
        }
        
        public int TrackCount => tracks.Count;
        public int StingerCount => stingers.Count;
        
        #endregion
    }
    
    /// <summary>
    /// Dynamic music system for combat that layers and mixes based on battle state.
    /// </summary>
    public class DynamicCombatMusic : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource baseLayer;
        [SerializeField] private AudioSource percussionLayer;
        [SerializeField] private AudioSource melodyLayer;
        [SerializeField] private AudioSource intensityLayer;
        
        [Header("Track Sets")]
        [SerializeField] private CombatMusicSet normalCombat;
        [SerializeField] private CombatMusicSet bossCombat;
        [SerializeField] private CombatMusicSet siegeCombat;
        
        [Header("Settings")]
        [SerializeField] private float layerFadeDuration = 1f;
        [SerializeField] private AnimationCurve intensityCurve;
        
        // State
        private CombatMusicSet _currentSet;
        private float _currentIntensity;
        private float _targetIntensity;
        private bool _isPlaying;
        
        private void Awake()
        {
            if (intensityCurve == null || intensityCurve.length == 0)
            {
                intensityCurve = AnimationCurve.Linear(0, 0, 1, 1);
            }
            
            InitializeLayers();
        }
        
        private void InitializeLayers()
        {
            ConfigureLayer(baseLayer);
            ConfigureLayer(percussionLayer);
            ConfigureLayer(melodyLayer);
            ConfigureLayer(intensityLayer);
        }
        
        private void ConfigureLayer(AudioSource source)
        {
            if (source == null) return;
            source.playOnAwake = false;
            source.loop = true;
            source.volume = 0;
        }
        
        private void Update()
        {
            if (!_isPlaying) return;
            
            UpdateIntensity();
            UpdateLayerVolumes();
        }
        
        #region Public API
        
        public void StartCombatMusic(CombatType type = CombatType.Normal)
        {
            _currentSet = type switch
            {
                CombatType.Boss => bossCombat ?? normalCombat,
                CombatType.Siege => siegeCombat ?? normalCombat,
                _ => normalCombat
            };
            
            if (_currentSet == null)
            {
                ApexLogger.LogWarning("DynamicCombatMusic: No music set available", ApexLogger.LogCategory.Audio);
                return;
            }
            
            LoadMusicSet(_currentSet);
            PlayAllLayers();
            _isPlaying = true;
        }
        
        public void StopCombatMusic(float fadeOut = 2f)
        {
            StartCoroutine(FadeOutAll(fadeOut));
        }
        
        public void SetIntensity(float intensity)
        {
            _targetIntensity = Mathf.Clamp01(intensity);
        }
        
        /// <summary>
        /// Set intensity based on combat events
        /// </summary>
        public void OnCombatEvent(CombatMusicEvent evt)
        {
            switch (evt)
            {
                case CombatMusicEvent.BattleStart:
                    SetIntensity(0.3f);
                    break;
                case CombatMusicEvent.EnemyWave:
                    SetIntensity(Mathf.Min(_currentIntensity + 0.2f, 1f));
                    break;
                case CombatMusicEvent.LowHealth:
                    SetIntensity(0.9f);
                    break;
                case CombatMusicEvent.Winning:
                    SetIntensity(0.7f);
                    break;
                case CombatMusicEvent.Losing:
                    SetIntensity(1f);
                    break;
                case CombatMusicEvent.VictoryImminent:
                    SetIntensity(0.5f);
                    break;
            }
        }
        
        #endregion
        
        #region Internal
        
        private void LoadMusicSet(CombatMusicSet set)
        {
            if (baseLayer != null && set.baseTrack != null)
            {
                baseLayer.clip = set.baseTrack;
            }
            
            if (percussionLayer != null && set.percussionTrack != null)
            {
                percussionLayer.clip = set.percussionTrack;
            }
            
            if (melodyLayer != null && set.melodyTrack != null)
            {
                melodyLayer.clip = set.melodyTrack;
            }
            
            if (intensityLayer != null && set.intensityTrack != null)
            {
                intensityLayer.clip = set.intensityTrack;
            }
        }
        
        private void PlayAllLayers()
        {
            // Start all at same time for sync
            float startTime = 0;
            
            if (baseLayer != null && baseLayer.clip != null)
            {
                baseLayer.time = startTime;
                baseLayer.Play();
            }
            
            if (percussionLayer != null && percussionLayer.clip != null)
            {
                percussionLayer.time = startTime;
                percussionLayer.Play();
            }
            
            if (melodyLayer != null && melodyLayer.clip != null)
            {
                melodyLayer.time = startTime;
                melodyLayer.Play();
            }
            
            if (intensityLayer != null && intensityLayer.clip != null)
            {
                intensityLayer.time = startTime;
                intensityLayer.Play();
            }
        }
        
        private void UpdateIntensity()
        {
            _currentIntensity = Mathf.MoveTowards(_currentIntensity, _targetIntensity, 
                Time.unscaledDeltaTime / layerFadeDuration);
        }
        
        private void UpdateLayerVolumes()
        {
            if (_currentSet == null) return;
            
            float intensity = intensityCurve.Evaluate(_currentIntensity);
            
            // Base layer always on
            if (baseLayer != null)
            {
                baseLayer.volume = _currentSet.baseVolume;
            }
            
            // Percussion fades in at low intensity
            if (percussionLayer != null)
            {
                float percVol = intensity > 0.2f ? _currentSet.percussionVolume : 
                    Mathf.Lerp(0, _currentSet.percussionVolume, intensity / 0.2f);
                percussionLayer.volume = percVol;
            }
            
            // Melody fades in at medium intensity
            if (melodyLayer != null)
            {
                float melodyVol = intensity > 0.5f ? 
                    Mathf.Lerp(0, _currentSet.melodyVolume, (intensity - 0.5f) / 0.5f) : 0;
                melodyLayer.volume = melodyVol;
            }
            
            // Intensity layer for high combat
            if (intensityLayer != null)
            {
                float intVol = intensity > 0.7f ? 
                    Mathf.Lerp(0, _currentSet.intensityVolume, (intensity - 0.7f) / 0.3f) : 0;
                intensityLayer.volume = intVol;
            }
        }
        
        private System.Collections.IEnumerator FadeOutAll(float duration)
        {
            float elapsed = 0;
            float startBase = baseLayer?.volume ?? 0;
            float startPerc = percussionLayer?.volume ?? 0;
            float startMelody = melodyLayer?.volume ?? 0;
            float startIntensity = intensityLayer?.volume ?? 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                if (baseLayer != null) baseLayer.volume = Mathf.Lerp(startBase, 0, t);
                if (percussionLayer != null) percussionLayer.volume = Mathf.Lerp(startPerc, 0, t);
                if (melodyLayer != null) melodyLayer.volume = Mathf.Lerp(startMelody, 0, t);
                if (intensityLayer != null) intensityLayer.volume = Mathf.Lerp(startIntensity, 0, t);
                
                yield return null;
            }
            
            _isPlaying = false;
            baseLayer?.Stop();
            percussionLayer?.Stop();
            melodyLayer?.Stop();
            intensityLayer?.Stop();
        }
        
        #endregion
    }
    
    public enum CombatType
    {
        Normal,
        Boss,
        Siege
    }
    
    public enum CombatMusicEvent
    {
        BattleStart,
        EnemyWave,
        LowHealth,
        Winning,
        Losing,
        VictoryImminent
    }
    
    [Serializable]
    public class CombatMusicSet
    {
        public string name;
        
        [Header("Tracks")]
        public AudioClip baseTrack;
        public AudioClip percussionTrack;
        public AudioClip melodyTrack;
        public AudioClip intensityTrack;
        
        [Header("Volumes")]
        [Range(0, 1)] public float baseVolume = 0.6f;
        [Range(0, 1)] public float percussionVolume = 0.5f;
        [Range(0, 1)] public float melodyVolume = 0.4f;
        [Range(0, 1)] public float intensityVolume = 0.7f;
    }
}
