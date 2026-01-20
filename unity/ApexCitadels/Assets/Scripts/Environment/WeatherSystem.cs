// ============================================================================
// APEX CITADELS - WEATHER SYSTEM
// Dynamic weather that affects visuals, gameplay, and atmosphere
// ============================================================================
using System;
using System.Collections;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.Environment
{
    /// <summary>
    /// Weather types that affect the game world
    /// </summary>
    public enum WeatherType
    {
        Clear,
        PartlyCloudy,
        Cloudy,
        Overcast,
        Foggy,
        LightRain,
        Rain,
        HeavyRain,
        Thunderstorm,
        Snow,
        Blizzard
    }
    
    /// <summary>
    /// Weather intensity levels
    /// </summary>
    public enum WeatherIntensity
    {
        None,
        Light,
        Moderate,
        Heavy,
        Extreme
    }
    
    /// <summary>
    /// Weather data snapshot for current conditions
    /// </summary>
    [Serializable]
    public struct WeatherState
    {
        public WeatherType Type;
        public WeatherIntensity Intensity;
        public float CloudCoverage;      // 0-1
        public float Precipitation;       // 0-1
        public float WindSpeed;           // 0-1 normalized
        public float WindDirection;       // Degrees
        public float Visibility;          // 0-1 (1 = clear)
        public float Temperature;         // Celsius (for visual effects)
        public float Humidity;            // 0-1
        
        public static WeatherState Clear => new WeatherState
        {
            Type = WeatherType.Clear,
            Intensity = WeatherIntensity.None,
            CloudCoverage = 0.1f,
            Precipitation = 0f,
            WindSpeed = 0.1f,
            WindDirection = 0f,
            Visibility = 1f,
            Temperature = 20f,
            Humidity = 0.3f
        };
        
        public static WeatherState Rainy => new WeatherState
        {
            Type = WeatherType.Rain,
            Intensity = WeatherIntensity.Moderate,
            CloudCoverage = 0.9f,
            Precipitation = 0.6f,
            WindSpeed = 0.4f,
            WindDirection = 45f,
            Visibility = 0.5f,
            Temperature = 15f,
            Humidity = 0.85f
        };
        
        public static WeatherState Stormy => new WeatherState
        {
            Type = WeatherType.Thunderstorm,
            Intensity = WeatherIntensity.Heavy,
            CloudCoverage = 1f,
            Precipitation = 0.9f,
            WindSpeed = 0.8f,
            WindDirection = 90f,
            Visibility = 0.3f,
            Temperature = 12f,
            Humidity = 0.95f
        };
        
        public static WeatherState Lerp(WeatherState a, WeatherState b, float t)
        {
            return new WeatherState
            {
                Type = t < 0.5f ? a.Type : b.Type,
                Intensity = t < 0.5f ? a.Intensity : b.Intensity,
                CloudCoverage = Mathf.Lerp(a.CloudCoverage, b.CloudCoverage, t),
                Precipitation = Mathf.Lerp(a.Precipitation, b.Precipitation, t),
                WindSpeed = Mathf.Lerp(a.WindSpeed, b.WindSpeed, t),
                WindDirection = Mathf.LerpAngle(a.WindDirection, b.WindDirection, t),
                Visibility = Mathf.Lerp(a.Visibility, b.Visibility, t),
                Temperature = Mathf.Lerp(a.Temperature, b.Temperature, t),
                Humidity = Mathf.Lerp(a.Humidity, b.Humidity, t)
            };
        }
    }
    
    /// <summary>
    /// Dynamic weather system that creates an alive, reactive atmosphere.
    /// Integrates with DayNightCycle and ProceduralSky for seamless visuals.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        #region Singleton
        
        private static WeatherSystem _instance;
        public static WeatherSystem Instance => _instance;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Weather Settings")]
        [SerializeField] private bool enableDynamicWeather = true;
        [SerializeField] private float weatherChangeMinInterval = 120f; // Seconds between weather changes
        [SerializeField] private float weatherChangeMaxInterval = 600f;
        [SerializeField] private float weatherTransitionDuration = 30f; // How long transitions take
        
        [Header("Current Weather")]
        [SerializeField] private WeatherType startingWeather = WeatherType.Clear;
        
        [Header("Weather Probabilities")]
        [SerializeField, Range(0, 1)] private float clearChance = 0.4f;
        [SerializeField, Range(0, 1)] private float cloudyChance = 0.25f;
        [SerializeField, Range(0, 1)] private float rainChance = 0.2f;
        [SerializeField, Range(0, 1)] private float stormChance = 0.1f;
        [SerializeField, Range(0, 1)] private float fogChance = 0.05f;
        
        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem snowParticles;
        [SerializeField] private ParticleSystem dustParticles;
        [SerializeField] private ParticleSystem leafParticles;
        
        [Header("Audio")]
        [SerializeField] private AudioSource weatherAudioSource;
        [SerializeField] private AudioClip rainLoopClip;
        [SerializeField] private AudioClip thunderClip;
        [SerializeField] private AudioClip windLoopClip;
        
        [Header("Lightning")]
        [SerializeField] private Light lightningLight;
        [SerializeField] private float lightningMinInterval = 5f;
        [SerializeField] private float lightningMaxInterval = 30f;
        
        #endregion
        
        #region State
        
        private WeatherState _currentWeather;
        private WeatherState _targetWeather;
        private WeatherState _previousWeather;
        private float _transitionProgress = 1f;
        private float _nextWeatherChangeTime;
        private float _nextLightningTime;
        private bool _isTransitioning;
        
        // Cached references
        private DayNightCycle _dayNightCycle;
        private Transform _cameraTransform;
        
        #endregion
        
        #region Events
        
        public event Action<WeatherState> OnWeatherChanged;
        public event Action<WeatherState, WeatherState> OnWeatherTransitionStart;
        public event Action OnLightningStrike;
        
        #endregion
        
        #region Properties
        
        /// <summary>Current interpolated weather state.</summary>
        public WeatherState CurrentWeather => _currentWeather;
        
        /// <summary>Target weather we're transitioning to.</summary>
        public WeatherState TargetWeather => _targetWeather;
        
        /// <summary>Is weather currently transitioning?</summary>
        public bool IsTransitioning => _isTransitioning;
        
        /// <summary>Transition progress 0-1.</summary>
        public float TransitionProgress => _transitionProgress;
        
        /// <summary>Current cloud coverage 0-1.</summary>
        public float CloudCoverage => _currentWeather.CloudCoverage;
        
        /// <summary>Current visibility 0-1.</summary>
        public float Visibility => _currentWeather.Visibility;
        
        /// <summary>Is it currently precipitating?</summary>
        public bool IsPrecipitating => _currentWeather.Precipitation > 0.1f;
        
        /// <summary>Is it currently stormy?</summary>
        public bool IsStormy => _currentWeather.Type == WeatherType.Thunderstorm;
        
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
        }
        
        private void Start()
        {
            // Get references
            _dayNightCycle = DayNightCycle.Instance;
            _cameraTransform = Camera.main?.transform;
            
            // Initialize weather
            _currentWeather = GetWeatherState(startingWeather);
            _targetWeather = _currentWeather;
            _previousWeather = _currentWeather;
            _transitionProgress = 1f;
            
            // Schedule first weather change
            ScheduleNextWeatherChange();
            
            // Create particle systems if needed
            CreateParticleSystems();
            
            // Create audio source if needed
            if (weatherAudioSource == null)
            {
                weatherAudioSource = gameObject.AddComponent<AudioSource>();
                weatherAudioSource.loop = true;
                weatherAudioSource.spatialBlend = 0f;
                weatherAudioSource.volume = 0f;
            }
            
            // Apply initial weather
            ApplyWeatherEffects();
            
            ApexLogger.Log($"[WeatherSystem] Started with {startingWeather}", ApexLogger.LogCategory.Map);
        }
        
        private void Update()
        {
            // Update transition
            if (_isTransitioning)
            {
                _transitionProgress += Time.deltaTime / weatherTransitionDuration;
                if (_transitionProgress >= 1f)
                {
                    _transitionProgress = 1f;
                    _isTransitioning = false;
                    _currentWeather = _targetWeather;
                    OnWeatherChanged?.Invoke(_currentWeather);
                    ApexLogger.Log($"[WeatherSystem] Weather changed to {_currentWeather.Type}", ApexLogger.LogCategory.Map);
                }
                else
                {
                    _currentWeather = WeatherState.Lerp(_previousWeather, _targetWeather, _transitionProgress);
                }
            }
            
            // Check for weather change
            if (enableDynamicWeather && Time.time >= _nextWeatherChangeTime)
            {
                ChangeToRandomWeather();
                ScheduleNextWeatherChange();
            }
            
            // Update lightning
            if (IsStormy && Time.time >= _nextLightningTime)
            {
                TriggerLightning();
                _nextLightningTime = Time.time + UnityEngine.Random.Range(lightningMinInterval, lightningMaxInterval);
            }
            
            // Update effects
            ApplyWeatherEffects();
            UpdateParticlePosition();
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
        
        #endregion
        
        #region Weather Control
        
        /// <summary>
        /// Set weather to a specific type with optional transition.
        /// </summary>
        public void SetWeather(WeatherType type, bool instant = false)
        {
            _targetWeather = GetWeatherState(type);
            
            if (instant)
            {
                _currentWeather = _targetWeather;
                _transitionProgress = 1f;
                _isTransitioning = false;
                OnWeatherChanged?.Invoke(_currentWeather);
            }
            else
            {
                _previousWeather = _currentWeather;
                _transitionProgress = 0f;
                _isTransitioning = true;
                OnWeatherTransitionStart?.Invoke(_previousWeather, _targetWeather);
            }
        }
        
        /// <summary>
        /// Set weather to a specific state with optional transition.
        /// </summary>
        public void SetWeather(WeatherState state, bool instant = false)
        {
            _targetWeather = state;
            
            if (instant)
            {
                _currentWeather = _targetWeather;
                _transitionProgress = 1f;
                _isTransitioning = false;
                OnWeatherChanged?.Invoke(_currentWeather);
            }
            else
            {
                _previousWeather = _currentWeather;
                _transitionProgress = 0f;
                _isTransitioning = true;
                OnWeatherTransitionStart?.Invoke(_previousWeather, _targetWeather);
            }
        }
        
        /// <summary>
        /// Change to a random weather based on probabilities.
        /// </summary>
        public void ChangeToRandomWeather()
        {
            WeatherType newWeather = GetRandomWeatherType();
            
            // Avoid same weather twice in a row (unless it's clear)
            int attempts = 0;
            while (newWeather == _currentWeather.Type && newWeather != WeatherType.Clear && attempts < 5)
            {
                newWeather = GetRandomWeatherType();
                attempts++;
            }
            
            SetWeather(newWeather);
        }
        
        private WeatherType GetRandomWeatherType()
        {
            float roll = UnityEngine.Random.value;
            float cumulative = 0f;
            
            cumulative += clearChance;
            if (roll < cumulative) return WeatherType.Clear;
            
            cumulative += cloudyChance;
            if (roll < cumulative) return UnityEngine.Random.value > 0.5f ? WeatherType.PartlyCloudy : WeatherType.Cloudy;
            
            cumulative += rainChance;
            if (roll < cumulative) return UnityEngine.Random.value > 0.5f ? WeatherType.LightRain : WeatherType.Rain;
            
            cumulative += stormChance;
            if (roll < cumulative) return WeatherType.Thunderstorm;
            
            cumulative += fogChance;
            if (roll < cumulative) return WeatherType.Foggy;
            
            return WeatherType.Clear;
        }
        
        private void ScheduleNextWeatherChange()
        {
            _nextWeatherChangeTime = Time.time + UnityEngine.Random.Range(weatherChangeMinInterval, weatherChangeMaxInterval);
        }
        
        private WeatherState GetWeatherState(WeatherType type)
        {
            return type switch
            {
                WeatherType.Clear => new WeatherState
                {
                    Type = type,
                    Intensity = WeatherIntensity.None,
                    CloudCoverage = 0.05f,
                    Precipitation = 0f,
                    WindSpeed = 0.1f,
                    Visibility = 1f,
                    Temperature = 22f,
                    Humidity = 0.3f
                },
                WeatherType.PartlyCloudy => new WeatherState
                {
                    Type = type,
                    Intensity = WeatherIntensity.Light,
                    CloudCoverage = 0.3f,
                    Precipitation = 0f,
                    WindSpeed = 0.2f,
                    Visibility = 0.95f,
                    Temperature = 20f,
                    Humidity = 0.4f
                },
                WeatherType.Cloudy => new WeatherState
                {
                    Type = type,
                    Intensity = WeatherIntensity.Moderate,
                    CloudCoverage = 0.6f,
                    Precipitation = 0f,
                    WindSpeed = 0.25f,
                    Visibility = 0.85f,
                    Temperature = 18f,
                    Humidity = 0.5f
                },
                WeatherType.Overcast => new WeatherState
                {
                    Type = type,
                    Intensity = WeatherIntensity.Heavy,
                    CloudCoverage = 0.95f,
                    Precipitation = 0f,
                    WindSpeed = 0.3f,
                    Visibility = 0.7f,
                    Temperature = 16f,
                    Humidity = 0.65f
                },
                WeatherType.Foggy => new WeatherState
                {
                    Type = type,
                    Intensity = WeatherIntensity.Moderate,
                    CloudCoverage = 0.4f,
                    Precipitation = 0f,
                    WindSpeed = 0.05f,
                    Visibility = 0.2f,
                    Temperature = 14f,
                    Humidity = 0.95f
                },
                WeatherType.LightRain => new WeatherState
                {
                    Type = type,
                    Intensity = WeatherIntensity.Light,
                    CloudCoverage = 0.7f,
                    Precipitation = 0.3f,
                    WindSpeed = 0.2f,
                    Visibility = 0.7f,
                    Temperature = 16f,
                    Humidity = 0.8f
                },
                WeatherType.Rain => new WeatherState
                {
                    Type = type,
                    Intensity = WeatherIntensity.Moderate,
                    CloudCoverage = 0.85f,
                    Precipitation = 0.6f,
                    WindSpeed = 0.35f,
                    Visibility = 0.5f,
                    Temperature = 14f,
                    Humidity = 0.9f
                },
                WeatherType.HeavyRain => new WeatherState
                {
                    Type = type,
                    Intensity = WeatherIntensity.Heavy,
                    CloudCoverage = 1f,
                    Precipitation = 0.9f,
                    WindSpeed = 0.5f,
                    Visibility = 0.3f,
                    Temperature = 12f,
                    Humidity = 0.98f
                },
                WeatherType.Thunderstorm => new WeatherState
                {
                    Type = type,
                    Intensity = WeatherIntensity.Extreme,
                    CloudCoverage = 1f,
                    Precipitation = 0.85f,
                    WindSpeed = 0.7f,
                    Visibility = 0.25f,
                    Temperature = 10f,
                    Humidity = 0.98f
                },
                WeatherType.Snow => new WeatherState
                {
                    Type = type,
                    Intensity = WeatherIntensity.Moderate,
                    CloudCoverage = 0.8f,
                    Precipitation = 0.5f,
                    WindSpeed = 0.2f,
                    Visibility = 0.4f,
                    Temperature = -2f,
                    Humidity = 0.7f
                },
                WeatherType.Blizzard => new WeatherState
                {
                    Type = type,
                    Intensity = WeatherIntensity.Extreme,
                    CloudCoverage = 1f,
                    Precipitation = 0.9f,
                    WindSpeed = 0.9f,
                    Visibility = 0.1f,
                    Temperature = -10f,
                    Humidity = 0.6f
                },
                _ => WeatherState.Clear
            };
        }
        
        #endregion
        
        #region Effects
        
        private void ApplyWeatherEffects()
        {
            // Update particle systems
            UpdateRainParticles();
            UpdateSnowParticles();
            UpdateWindParticles();
            
            // Update audio
            UpdateWeatherAudio();
            
            // Update fog based on visibility
            UpdateFog();
        }
        
        private void UpdateRainParticles()
        {
            if (rainParticles == null) return;
            
            bool shouldRain = _currentWeather.Type is WeatherType.LightRain or WeatherType.Rain 
                or WeatherType.HeavyRain or WeatherType.Thunderstorm;
            
            var emission = rainParticles.emission;
            
            if (shouldRain)
            {
                if (!rainParticles.isPlaying)
                    rainParticles.Play();
                
                // Scale emission with precipitation intensity
                float baseRate = 500f;
                emission.rateOverTime = baseRate * _currentWeather.Precipitation;
            }
            else
            {
                emission.rateOverTime = 0f;
                if (rainParticles.isPlaying && rainParticles.particleCount == 0)
                    rainParticles.Stop();
            }
        }
        
        private void UpdateSnowParticles()
        {
            if (snowParticles == null) return;
            
            bool shouldSnow = _currentWeather.Type is WeatherType.Snow or WeatherType.Blizzard;
            
            var emission = snowParticles.emission;
            
            if (shouldSnow)
            {
                if (!snowParticles.isPlaying)
                    snowParticles.Play();
                
                float baseRate = 200f;
                emission.rateOverTime = baseRate * _currentWeather.Precipitation;
            }
            else
            {
                emission.rateOverTime = 0f;
                if (snowParticles.isPlaying && snowParticles.particleCount == 0)
                    snowParticles.Stop();
            }
        }
        
        private void UpdateWindParticles()
        {
            if (dustParticles != null)
            {
                var emission = dustParticles.emission;
                bool shouldShowDust = _currentWeather.WindSpeed > 0.5f && !IsPrecipitating;
                
                if (shouldShowDust)
                {
                    if (!dustParticles.isPlaying)
                        dustParticles.Play();
                    emission.rateOverTime = 50f * _currentWeather.WindSpeed;
                }
                else
                {
                    emission.rateOverTime = 0f;
                }
            }
            
            if (leafParticles != null)
            {
                var emission = leafParticles.emission;
                bool shouldShowLeaves = _currentWeather.WindSpeed > 0.3f;
                
                if (shouldShowLeaves)
                {
                    if (!leafParticles.isPlaying)
                        leafParticles.Play();
                    emission.rateOverTime = 20f * _currentWeather.WindSpeed;
                }
                else
                {
                    emission.rateOverTime = 0f;
                }
            }
        }
        
        private void UpdateWeatherAudio()
        {
            if (weatherAudioSource == null) return;
            
            // Determine target volume and clip
            float targetVolume = 0f;
            AudioClip targetClip = null;
            
            if (IsPrecipitating && rainLoopClip != null)
            {
                targetClip = rainLoopClip;
                targetVolume = _currentWeather.Precipitation * 0.5f;
            }
            else if (_currentWeather.WindSpeed > 0.4f && windLoopClip != null)
            {
                targetClip = windLoopClip;
                targetVolume = (_currentWeather.WindSpeed - 0.4f) * 0.5f;
            }
            
            // Crossfade audio
            if (targetClip != null && weatherAudioSource.clip != targetClip)
            {
                weatherAudioSource.clip = targetClip;
                weatherAudioSource.Play();
            }
            
            weatherAudioSource.volume = Mathf.Lerp(weatherAudioSource.volume, targetVolume, Time.deltaTime * 2f);
        }
        
        private void UpdateFog()
        {
            // Get base fog from DayNightCycle or use defaults
            float baseDensity = 0.002f;
            
            // Modify based on visibility
            float visibilityMultiplier = 1f / Mathf.Max(0.1f, _currentWeather.Visibility);
            RenderSettings.fogDensity = baseDensity * visibilityMultiplier;
            
            // Tint fog color based on weather
            Color fogTint = GetWeatherFogTint();
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, fogTint, Time.deltaTime * 2f);
        }
        
        private Color GetWeatherFogTint()
        {
            // Base fog color from time of day (assume white midday)
            Color baseColor = Color.white;
            
            // Modify based on weather
            return _currentWeather.Type switch
            {
                WeatherType.Foggy => new Color(0.85f, 0.85f, 0.9f),
                WeatherType.Rain or WeatherType.HeavyRain => new Color(0.6f, 0.65f, 0.7f),
                WeatherType.Thunderstorm => new Color(0.4f, 0.42f, 0.5f),
                WeatherType.Snow => new Color(0.9f, 0.92f, 0.95f),
                WeatherType.Blizzard => new Color(0.95f, 0.95f, 0.98f),
                _ => baseColor
            };
        }
        
        private void UpdateParticlePosition()
        {
            // Keep particles following the camera
            if (_cameraTransform == null)
            {
                _cameraTransform = Camera.main?.transform;
                if (_cameraTransform == null) return;
            }
            
            Vector3 particlePos = _cameraTransform.position + Vector3.up * 20f;
            
            if (rainParticles != null)
                rainParticles.transform.position = particlePos;
            if (snowParticles != null)
                snowParticles.transform.position = particlePos;
            if (dustParticles != null)
                dustParticles.transform.position = _cameraTransform.position + Vector3.up * 2f;
            if (leafParticles != null)
                leafParticles.transform.position = _cameraTransform.position + Vector3.up * 5f;
        }
        
        #endregion
        
        #region Lightning
        
        private void TriggerLightning()
        {
            StartCoroutine(LightningFlashRoutine());
            OnLightningStrike?.Invoke();
            
            // Play thunder after delay (sound travels slower than light)
            if (thunderClip != null)
            {
                float delay = UnityEngine.Random.Range(0.5f, 3f);
                StartCoroutine(PlayThunderDelayed(delay));
            }
        }
        
        private IEnumerator LightningFlashRoutine()
        {
            if (lightningLight == null)
            {
                // Create temporary light
                GameObject lightObj = new GameObject("LightningFlash");
                lightObj.transform.position = Vector3.up * 100f;
                lightningLight = lightObj.AddComponent<Light>();
                lightningLight.type = LightType.Directional;
                lightningLight.color = new Color(0.9f, 0.9f, 1f);
                lightningLight.intensity = 0f;
            }
            
            // Flash pattern
            float[] intensities = { 3f, 0f, 2f, 0f, 1f, 0f };
            float[] durations = { 0.05f, 0.05f, 0.03f, 0.1f, 0.05f, 0f };
            
            for (int i = 0; i < intensities.Length; i++)
            {
                lightningLight.intensity = intensities[i];
                yield return new WaitForSeconds(durations[i]);
            }
            
            lightningLight.intensity = 0f;
        }
        
        private IEnumerator PlayThunderDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (thunderClip != null)
            {
                AudioSource.PlayClipAtPoint(thunderClip, _cameraTransform?.position ?? Vector3.zero, 0.8f);
            }
        }
        
        #endregion
        
        #region Particle System Creation
        
        private void CreateParticleSystems()
        {
            // Create rain particles if not assigned
            if (rainParticles == null)
            {
                rainParticles = CreateRainSystem();
            }
            
            // Create snow particles if not assigned  
            if (snowParticles == null)
            {
                snowParticles = CreateSnowSystem();
            }
        }
        
        private ParticleSystem CreateRainSystem()
        {
            GameObject rainObj = new GameObject("RainParticles");
            rainObj.transform.parent = transform;
            
            var ps = rainObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.maxParticles = 5000;
            main.startLifetime = 1.5f;
            main.startSpeed = 25f;
            main.startSize = 0.05f;
            main.startColor = new Color(0.7f, 0.7f, 0.8f, 0.6f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1f;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(50f, 1f, 50f);
            
            var emission = ps.emission;
            emission.rateOverTime = 0f; // Controlled by weather
            
            // Use default material
            var renderer = rainObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = new Color(0.7f, 0.7f, 0.8f, 0.4f);
            
            ps.Stop();
            return ps;
        }
        
        private ParticleSystem CreateSnowSystem()
        {
            GameObject snowObj = new GameObject("SnowParticles");
            snowObj.transform.parent = transform;
            
            var ps = snowObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.maxParticles = 3000;
            main.startLifetime = 8f;
            main.startSpeed = 2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.1f;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(50f, 1f, 50f);
            
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            
            // Add noise for drifting
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1f;
            noise.frequency = 0.5f;
            
            var renderer = snowObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = new Color(1f, 1f, 1f, 0.8f);
            
            ps.Stop();
            return ps;
        }
        
        #endregion
        
        #region Public Utilities
        
        /// <summary>
        /// Get a readable description of current weather.
        /// </summary>
        public string GetWeatherDescription()
        {
            string desc = _currentWeather.Type switch
            {
                WeatherType.Clear => "Clear skies",
                WeatherType.PartlyCloudy => "Partly cloudy",
                WeatherType.Cloudy => "Cloudy",
                WeatherType.Overcast => "Overcast",
                WeatherType.Foggy => "Foggy",
                WeatherType.LightRain => "Light rain",
                WeatherType.Rain => "Raining",
                WeatherType.HeavyRain => "Heavy rain",
                WeatherType.Thunderstorm => "Thunderstorm",
                WeatherType.Snow => "Snowing",
                WeatherType.Blizzard => "Blizzard",
                _ => "Unknown"
            };
            
            if (_currentWeather.WindSpeed > 0.5f)
            {
                desc += ", windy";
            }
            
            return desc;
        }
        
        /// <summary>
        /// Get icon name for current weather (for UI).
        /// </summary>
        public string GetWeatherIcon()
        {
            bool isNight = _dayNightCycle != null && _dayNightCycle.IsNight;
            
            return _currentWeather.Type switch
            {
                WeatherType.Clear => isNight ? "☾" : "☀",
                WeatherType.PartlyCloudy => isNight ? "☁" : "⛅",
                WeatherType.Cloudy or WeatherType.Overcast => "☁",
                WeatherType.Foggy => "🌫",
                WeatherType.LightRain or WeatherType.Rain => "🌧",
                WeatherType.HeavyRain => "🌧",
                WeatherType.Thunderstorm => "⛈",
                WeatherType.Snow => "🌨",
                WeatherType.Blizzard => "❄",
                _ => "?"
            };
        }
        
        #endregion
    }
}
