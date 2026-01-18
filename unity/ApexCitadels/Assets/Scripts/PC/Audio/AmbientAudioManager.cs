using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.Audio
{
    /// <summary>
    /// Ambient Audio System for world atmosphere.
    /// Creates immersive soundscapes based on location and time.
    /// Features:
    /// - Multi-layer ambient sounds
    /// - Time-of-day variation
    /// - Weather integration
    /// - Location-based ambience
    /// - Random environmental events
    /// </summary>
    public class AmbientAudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource primaryAmbientSource;
        [SerializeField] private AudioSource secondaryAmbientSource;
        [SerializeField] private AudioSource weatherSource;
        [SerializeField] private AudioSource eventSource;
        
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixerGroup ambientMixerGroup;
        
        [Header("Settings")]
        [SerializeField] private float crossfadeDuration = 3f;
        [SerializeField] private float eventMinInterval = 30f;
        [SerializeField] private float eventMaxInterval = 120f;
        [SerializeField] private bool enableRandomEvents = true;
        
        [Header("Ambient Library")]
        [SerializeField] private AmbientSoundLibrary ambientLibrary;
        
        // Singleton
        private static AmbientAudioManager _instance;
        public static AmbientAudioManager Instance => _instance;
        
        // State
        private AmbientZone _currentZone = AmbientZone.Default;
        private TimeOfDay _currentTimeOfDay = TimeOfDay.Day;
        private WeatherType _currentWeather = WeatherType.Clear;
        private AudioSource _activeAmbientSource;
        private AudioSource _inactiveAmbientSource;
        private Coroutine _eventCoroutine;
        
        // Events
        public event Action<AmbientZone> OnZoneChanged;
        public event Action<TimeOfDay> OnTimeChanged;
        
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
            if (primaryAmbientSource == null)
            {
                primaryAmbientSource = gameObject.AddComponent<AudioSource>();
                ConfigureAmbientSource(primaryAmbientSource);
            }
            
            if (secondaryAmbientSource == null)
            {
                secondaryAmbientSource = gameObject.AddComponent<AudioSource>();
                ConfigureAmbientSource(secondaryAmbientSource);
            }
            
            if (weatherSource == null)
            {
                weatherSource = gameObject.AddComponent<AudioSource>();
                ConfigureAmbientSource(weatherSource);
            }
            
            if (eventSource == null)
            {
                eventSource = gameObject.AddComponent<AudioSource>();
                eventSource.playOnAwake = false;
                eventSource.loop = false;
            }
            
            _activeAmbientSource = primaryAmbientSource;
            _inactiveAmbientSource = secondaryAmbientSource;
        }
        
        private void ConfigureAmbientSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0; // 2D
            
            if (ambientMixerGroup != null)
            {
                source.outputAudioMixerGroup = ambientMixerGroup;
            }
        }
        
        private void Start()
        {
            // Start with default ambience
            SetZone(AmbientZone.Default);
            
            // Start random events
            if (enableRandomEvents)
            {
                _eventCoroutine = StartCoroutine(RandomEventCoroutine());
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Set ambient zone (location type)
        /// </summary>
        public void SetZone(AmbientZone zone)
        {
            if (_currentZone == zone && _activeAmbientSource.isPlaying)
                return;
            
            _currentZone = zone;
            OnZoneChanged?.Invoke(zone);
            
            UpdateAmbience();
        }
        
        /// <summary>
        /// Set time of day for ambience variation
        /// </summary>
        public void SetTimeOfDay(TimeOfDay time)
        {
            if (_currentTimeOfDay == time)
                return;
            
            _currentTimeOfDay = time;
            OnTimeChanged?.Invoke(time);
            
            UpdateAmbience();
        }
        
        /// <summary>
        /// Set weather for weather sounds
        /// </summary>
        public void SetWeather(WeatherType weather)
        {
            if (_currentWeather == weather)
                return;
            
            _currentWeather = weather;
            UpdateWeatherSound();
        }
        
        /// <summary>
        /// Play a one-shot environmental event sound
        /// </summary>
        public void PlayEnvironmentalEvent(EnvironmentalEvent eventType)
        {
            var sound = ambientLibrary?.GetEventSound(eventType);
            if (sound != null)
            {
                PlayEventSound(sound);
            }
        }
        
        /// <summary>
        /// Pause all ambient audio
        /// </summary>
        public void Pause()
        {
            _activeAmbientSource.Pause();
            weatherSource.Pause();
        }
        
        /// <summary>
        /// Resume ambient audio
        /// </summary>
        public void Resume()
        {
            _activeAmbientSource.UnPause();
            weatherSource.UnPause();
        }
        
        /// <summary>
        /// Set ambient volume
        /// </summary>
        public void SetVolume(float volume)
        {
            primaryAmbientSource.volume = volume;
            secondaryAmbientSource.volume = volume;
        }
        
        /// <summary>
        /// Set weather volume
        /// </summary>
        public void SetWeatherVolume(float volume)
        {
            weatherSource.volume = volume;
        }
        
        /// <summary>
        /// Fade out all ambience
        /// </summary>
        public void FadeOut(float duration = 2f)
        {
            StartCoroutine(FadeOutAll(duration));
        }
        
        /// <summary>
        /// Fade in ambience
        /// </summary>
        public void FadeIn(float duration = 2f)
        {
            StartCoroutine(FadeInAll(duration));
        }
        
        #endregion
        
        #region Internal
        
        private void UpdateAmbience()
        {
            if (ambientLibrary == null) return;
            
            var ambient = ambientLibrary.GetAmbient(_currentZone, _currentTimeOfDay);
            if (ambient == null || ambient.clip == null) return;
            
            StartCoroutine(CrossfadeToAmbient(ambient));
        }
        
        private IEnumerator CrossfadeToAmbient(AmbientSound ambient)
        {
            // Prepare inactive source
            _inactiveAmbientSource.clip = ambient.clip;
            _inactiveAmbientSource.volume = 0;
            _inactiveAmbientSource.Play();
            
            float elapsed = 0;
            float startVolume = _activeAmbientSource.volume;
            float targetVolume = ambient.volume;
            
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / crossfadeDuration;
                
                _activeAmbientSource.volume = Mathf.Lerp(startVolume, 0, t);
                _inactiveAmbientSource.volume = Mathf.Lerp(0, targetVolume, t);
                
                yield return null;
            }
            
            _activeAmbientSource.Stop();
            
            // Swap sources
            var temp = _activeAmbientSource;
            _activeAmbientSource = _inactiveAmbientSource;
            _inactiveAmbientSource = temp;
        }
        
        private void UpdateWeatherSound()
        {
            if (ambientLibrary == null) return;
            
            var weather = ambientLibrary.GetWeatherSound(_currentWeather);
            
            if (_currentWeather == WeatherType.Clear)
            {
                StartCoroutine(FadeOutWeather(1f));
            }
            else if (weather != null && weather.clip != null)
            {
                StartCoroutine(CrossfadeWeather(weather));
            }
        }
        
        private IEnumerator CrossfadeWeather(AmbientSound weather)
        {
            if (!weatherSource.isPlaying)
            {
                weatherSource.clip = weather.clip;
                weatherSource.volume = 0;
                weatherSource.Play();
            }
            
            float elapsed = 0;
            float startVolume = weatherSource.volume;
            
            while (elapsed < 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                weatherSource.volume = Mathf.Lerp(startVolume, weather.volume, elapsed / 2f);
                yield return null;
            }
        }
        
        private IEnumerator FadeOutWeather(float duration)
        {
            float elapsed = 0;
            float startVolume = weatherSource.volume;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                weatherSource.volume = Mathf.Lerp(startVolume, 0, elapsed / duration);
                yield return null;
            }
            
            weatherSource.Stop();
        }
        
        private void PlayEventSound(AmbientSound sound)
        {
            if (sound.clip == null) return;
            
            eventSource.clip = sound.clip;
            eventSource.volume = sound.volume;
            eventSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            eventSource.Play();
        }
        
        private IEnumerator RandomEventCoroutine()
        {
            while (true)
            {
                float interval = UnityEngine.Random.Range(eventMinInterval, eventMaxInterval);
                yield return new WaitForSeconds(interval);
                
                // Play random event for current zone
                var randomEvent = GetRandomEventForZone(_currentZone);
                PlayEnvironmentalEvent(randomEvent);
            }
        }
        
        private EnvironmentalEvent GetRandomEventForZone(AmbientZone zone)
        {
            // Define events per zone
            EnvironmentalEvent[] events = zone switch
            {
                AmbientZone.Forest => new[] { EnvironmentalEvent.BirdCall, EnvironmentalEvent.WindGust, 
                    EnvironmentalEvent.DistantAnimal, EnvironmentalEvent.LeafRustle },
                AmbientZone.City => new[] { EnvironmentalEvent.DistantVoices, EnvironmentalEvent.Bell, 
                    EnvironmentalEvent.CartPassing, EnvironmentalEvent.DoorCreak },
                AmbientZone.Castle => new[] { EnvironmentalEvent.MetalClang, EnvironmentalEvent.FlagFlap, 
                    EnvironmentalEvent.DistantVoices, EnvironmentalEvent.DoorCreak },
                AmbientZone.Battle => new[] { EnvironmentalEvent.DistantExplosion, EnvironmentalEvent.WarHorn, 
                    EnvironmentalEvent.MetalClang, EnvironmentalEvent.DistantVoices },
                _ => new[] { EnvironmentalEvent.WindGust, EnvironmentalEvent.BirdCall }
            };
            
            return events[UnityEngine.Random.Range(0, events.Length)];
        }
        
        private IEnumerator FadeOutAll(float duration)
        {
            float elapsed = 0;
            float startAmbient = _activeAmbientSource.volume;
            float startWeather = weatherSource.volume;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                _activeAmbientSource.volume = Mathf.Lerp(startAmbient, 0, t);
                weatherSource.volume = Mathf.Lerp(startWeather, 0, t);
                
                yield return null;
            }
        }
        
        private IEnumerator FadeInAll(float duration)
        {
            float elapsed = 0;
            var ambient = ambientLibrary?.GetAmbient(_currentZone, _currentTimeOfDay);
            float targetAmbient = ambient?.volume ?? 0.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                _activeAmbientSource.volume = Mathf.Lerp(0, targetAmbient, t);
                
                yield return null;
            }
        }
        
        #endregion
        
        #region Properties
        
        public AmbientZone CurrentZone => _currentZone;
        public TimeOfDay CurrentTimeOfDay => _currentTimeOfDay;
        public WeatherType CurrentWeather => _currentWeather;
        
        #endregion
    }
    
    #region Enums
    
    public enum AmbientZone
    {
        Default,
        Forest,
        Plains,
        Mountains,
        Coast,
        Desert,
        Swamp,
        City,
        Castle,
        Dungeon,
        Battle,
        Menu
    }
    
    public enum TimeOfDay
    {
        Dawn,
        Day,
        Dusk,
        Night
    }
    
    public enum WeatherType
    {
        Clear,
        Rain,
        Storm,
        Snow,
        Wind,
        Fog
    }
    
    public enum EnvironmentalEvent
    {
        BirdCall,
        WindGust,
        Thunder,
        DistantAnimal,
        LeafRustle,
        WaterSplash,
        DistantVoices,
        Bell,
        MetalClang,
        DoorCreak,
        CartPassing,
        FlagFlap,
        DistantExplosion,
        WarHorn,
        Owl,
        Wolf,
        Cricket
    }
    
    #endregion
    
    /// <summary>
    /// Individual ambient sound data
    /// </summary>
    [Serializable]
    public class AmbientSound
    {
        public string name;
        public AudioClip clip;
        [Range(0, 1)] public float volume = 0.5f;
        public AmbientZone zone;
        public TimeOfDay[] timesOfDay;
    }
    
    /// <summary>
    /// Scriptable Object for ambient sound library
    /// </summary>
    [CreateAssetMenu(fileName = "AmbientSoundLibrary", menuName = "Apex Citadels/Ambient Sound Library")]
    public class AmbientSoundLibrary : ScriptableObject
    {
        [Header("Zone Ambients")]
        [SerializeField] private List<AmbientSound> zoneAmbients;
        
        [Header("Weather Sounds")]
        [SerializeField] private List<WeatherSound> weatherSounds;
        
        [Header("Environmental Events")]
        [SerializeField] private List<EventSound> eventSounds;
        
        // Caches
        private Dictionary<(AmbientZone, TimeOfDay), AmbientSound> _ambientCache;
        private Dictionary<WeatherType, AmbientSound> _weatherCache;
        private Dictionary<EnvironmentalEvent, AmbientSound> _eventCache;
        
        private void OnEnable()
        {
            BuildCaches();
        }
        
        private void BuildCaches()
        {
            // Build ambient cache
            _ambientCache = new Dictionary<(AmbientZone, TimeOfDay), AmbientSound>();
            if (zoneAmbients != null)
            {
                foreach (var ambient in zoneAmbients)
                {
                    if (ambient.timesOfDay != null)
                    {
                        foreach (var time in ambient.timesOfDay)
                        {
                            _ambientCache[(ambient.zone, time)] = ambient;
                        }
                    }
                }
            }
            
            // Build weather cache
            _weatherCache = new Dictionary<WeatherType, AmbientSound>();
            if (weatherSounds != null)
            {
                foreach (var weather in weatherSounds)
                {
                    _weatherCache[weather.type] = weather.sound;
                }
            }
            
            // Build event cache
            _eventCache = new Dictionary<EnvironmentalEvent, AmbientSound>();
            if (eventSounds != null)
            {
                foreach (var evt in eventSounds)
                {
                    _eventCache[evt.type] = evt.sound;
                }
            }
        }
        
        public AmbientSound GetAmbient(AmbientZone zone, TimeOfDay time)
        {
            if (_ambientCache == null) BuildCaches();
            
            // Try exact match
            if (_ambientCache.TryGetValue((zone, time), out var ambient))
            {
                return ambient;
            }
            
            // Fall back to any time for this zone
            foreach (var kvp in _ambientCache)
            {
                if (kvp.Key.Item1 == zone)
                {
                    return kvp.Value;
                }
            }
            
            // Fall back to default zone
            _ambientCache.TryGetValue((AmbientZone.Default, time), out ambient);
            return ambient;
        }
        
        public AmbientSound GetWeatherSound(WeatherType weather)
        {
            if (_weatherCache == null) BuildCaches();
            _weatherCache.TryGetValue(weather, out var sound);
            return sound;
        }
        
        public AmbientSound GetEventSound(EnvironmentalEvent eventType)
        {
            if (_eventCache == null) BuildCaches();
            _eventCache.TryGetValue(eventType, out var sound);
            return sound;
        }
    }
    
    [Serializable]
    public class WeatherSound
    {
        public WeatherType type;
        public AmbientSound sound;
    }
    
    [Serializable]
    public class EventSound
    {
        public EnvironmentalEvent type;
        public AmbientSound sound;
    }
}
