using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Dynamic Weather System - Adds atmosphere and strategic gameplay elements.
    /// Weather affects visibility, combat bonuses, resource gathering, and mood.
    /// 
    /// Weather Types:
    /// - Clear: Normal conditions
    /// - Cloudy: Reduced solar panel efficiency
    /// - Rain: Faster crop growth, reduced fire spread
    /// - Storm: Combat penalties, building damage risk
    /// - Fog: Reduced visibility, ambush bonus
    /// - Snow: Movement penalties, increased heating costs
    /// - Sandstorm: Massive visibility reduction, equipment damage
    /// - Heatwave: Increased water consumption, fire risk
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        [Header("Weather Settings")]
        [SerializeField] private float weatherCheckInterval = 300f; // Check every 5 minutes
        [SerializeField] private float transitionDuration = 30f; // Weather transition time
        [SerializeField] private bool enableDynamicWeather = true;
        
        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem snowParticles;
        [SerializeField] private ParticleSystem fogParticles;
        [SerializeField] private ParticleSystem dustParticles;
        [SerializeField] private ParticleSystem lightningFlash;
        
        [Header("Audio")]
        [SerializeField] private AudioSource weatherAudioSource;
        [SerializeField] private AudioClip rainSound;
        [SerializeField] private AudioClip thunderSound;
        [SerializeField] private AudioClip windSound;
        [SerializeField] private AudioClip snowAmbience;
        
        [Header("Visual Settings")]
        [SerializeField] private Light sunLight;
        [SerializeField] private float fogDensityMultiplier = 1f;
        
        // Current state
        private WeatherType _currentWeather = WeatherType.Clear;
        private WeatherType _targetWeather = WeatherType.Clear;
        private float _transitionProgress = 1f;
        private float _weatherTimer;
        private float _intensity = 0f;
        private float _targetIntensity = 0f;
        
        // Cached values
        private Color _baseSunColor;
        private float _baseSunIntensity;
        private Color _baseAmbientColor;
        private float _baseFogDensity;
        
        // Weather effects on gameplay
        private Dictionary<WeatherType, WeatherEffects> _weatherEffects;
        
        public static WeatherSystem Instance { get; private set; }
        
        // Events
        public event Action<WeatherType> OnWeatherChanged;
        public event Action<WeatherType, WeatherType, float> OnWeatherTransition;
        public event Action OnLightningStrike;
        
        // Properties
        public WeatherType CurrentWeather => _currentWeather;
        public float Intensity => _intensity;
        public WeatherEffects CurrentEffects => GetEffectsForWeather(_currentWeather);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeWeatherEffects();
            CacheLightingValues();
            CreateParticleSystems();
            
            if (enableDynamicWeather)
            {
                StartCoroutine(WeatherCycle());
            }
            
            ApexLogger.Log("[Weather] System initialized", ApexLogger.LogCategory.General);
        }

        private void Update()
        {
            if (_transitionProgress < 1f)
            {
                UpdateWeatherTransition();
            }
            
            UpdateParticles();
            UpdateLightning();
        }

        private void InitializeWeatherEffects()
        {
            _weatherEffects = new Dictionary<WeatherType, WeatherEffects>
            {
                { WeatherType.Clear, new WeatherEffects
                    {
                        VisibilityMultiplier = 1f,
                        MovementMultiplier = 1f,
                        CombatAttackBonus = 0,
                        CombatDefenseBonus = 0,
                        ResourceGatherBonus = 0f,
                        CropGrowthMultiplier = 1f,
                        FireRiskMultiplier = 1f,
                        SolarEfficiency = 1f,
                        AmbushBonus = 0,
                        Description = "Clear skies - perfect conditions"
                    }
                },
                { WeatherType.Cloudy, new WeatherEffects
                    {
                        VisibilityMultiplier = 0.9f,
                        MovementMultiplier = 1f,
                        CombatAttackBonus = 0,
                        CombatDefenseBonus = 0,
                        ResourceGatherBonus = 0f,
                        CropGrowthMultiplier = 1f,
                        FireRiskMultiplier = 0.8f,
                        SolarEfficiency = 0.6f,
                        AmbushBonus = 5,
                        Description = "Overcast - reduced solar efficiency"
                    }
                },
                { WeatherType.Rain, new WeatherEffects
                    {
                        VisibilityMultiplier = 0.7f,
                        MovementMultiplier = 0.9f,
                        CombatAttackBonus = -5,
                        CombatDefenseBonus = 0,
                        ResourceGatherBonus = -0.1f,
                        CropGrowthMultiplier = 1.5f,
                        FireRiskMultiplier = 0.2f,
                        SolarEfficiency = 0.3f,
                        AmbushBonus = 10,
                        Description = "Rain - faster crop growth, fire suppression"
                    }
                },
                { WeatherType.Storm, new WeatherEffects
                    {
                        VisibilityMultiplier = 0.4f,
                        MovementMultiplier = 0.7f,
                        CombatAttackBonus = -15,
                        CombatDefenseBonus = -10,
                        ResourceGatherBonus = -0.3f,
                        CropGrowthMultiplier = 1.2f,
                        FireRiskMultiplier = 0.1f,
                        SolarEfficiency = 0.1f,
                        AmbushBonus = 20,
                        BuildingDamageRisk = 0.05f,
                        Description = "Storm - dangerous conditions, seek shelter!"
                    }
                },
                { WeatherType.Fog, new WeatherEffects
                    {
                        VisibilityMultiplier = 0.3f,
                        MovementMultiplier = 0.85f,
                        CombatAttackBonus = -10,
                        CombatDefenseBonus = 5,
                        ResourceGatherBonus = -0.15f,
                        CropGrowthMultiplier = 1.1f,
                        FireRiskMultiplier = 0.5f,
                        SolarEfficiency = 0.4f,
                        AmbushBonus = 30,
                        Description = "Fog - perfect for ambushes"
                    }
                },
                { WeatherType.Snow, new WeatherEffects
                    {
                        VisibilityMultiplier = 0.6f,
                        MovementMultiplier = 0.6f,
                        CombatAttackBonus = -5,
                        CombatDefenseBonus = 0,
                        ResourceGatherBonus = -0.25f,
                        CropGrowthMultiplier = 0f,
                        FireRiskMultiplier = 0.3f,
                        SolarEfficiency = 0.5f,
                        AmbushBonus = 15,
                        HeatingCostMultiplier = 2f,
                        Description = "Snow - movement impaired, crops dormant"
                    }
                },
                { WeatherType.Sandstorm, new WeatherEffects
                    {
                        VisibilityMultiplier = 0.2f,
                        MovementMultiplier = 0.5f,
                        CombatAttackBonus = -20,
                        CombatDefenseBonus = -15,
                        ResourceGatherBonus = -0.5f,
                        CropGrowthMultiplier = 0.5f,
                        FireRiskMultiplier = 0.8f,
                        SolarEfficiency = 0.2f,
                        AmbushBonus = 25,
                        EquipmentDamageRate = 0.02f,
                        Description = "Sandstorm - devastating conditions!"
                    }
                },
                { WeatherType.Heatwave, new WeatherEffects
                    {
                        VisibilityMultiplier = 0.95f,
                        MovementMultiplier = 0.85f,
                        CombatAttackBonus = -5,
                        CombatDefenseBonus = -5,
                        ResourceGatherBonus = -0.2f,
                        CropGrowthMultiplier = 0.7f,
                        FireRiskMultiplier = 2f,
                        SolarEfficiency = 1.3f,
                        AmbushBonus = 0,
                        WaterConsumptionMultiplier = 2f,
                        Description = "Heatwave - high fire risk, water scarce"
                    }
                }
            };
        }

        private void CacheLightingValues()
        {
            if (sunLight == null)
            {
                sunLight = FindFirstObjectByType<Light>();
            }
            
            if (sunLight != null)
            {
                _baseSunColor = sunLight.color;
                _baseSunIntensity = sunLight.intensity;
            }
            
            _baseAmbientColor = RenderSettings.ambientSkyColor;
            _baseFogDensity = RenderSettings.fogDensity;
        }

        private void CreateParticleSystems()
        {
            // Create rain particles if not assigned
            if (rainParticles == null)
            {
                var rainObj = new GameObject("RainParticles");
                rainObj.transform.SetParent(transform);
                rainObj.transform.localPosition = new Vector3(0, 50, 0);
                
                rainParticles = rainObj.AddComponent<ParticleSystem>();
                var main = rainParticles.main;
                main.maxParticles = 10000;
                main.startLifetime = 2f;
                main.startSpeed = 25f;
                main.startSize = 0.05f;
                main.startColor = new Color(0.7f, 0.8f, 1f, 0.5f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.loop = true;
                main.playOnAwake = false;
                
                var emission = rainParticles.emission;
                emission.rateOverTime = 0;
                
                var shape = rainParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(100, 1, 100);
                
                var velocity = rainParticles.velocityOverLifetime;
                velocity.enabled = true;
                velocity.x = new ParticleSystem.MinMaxCurve(0f);
                velocity.y = new ParticleSystem.MinMaxCurve(-25f);
                velocity.z = new ParticleSystem.MinMaxCurve(0f);
                
                ApexLogger.Log("[Weather] Created rain particle system", ApexLogger.LogCategory.General);
            }
            
            // Create snow particles if not assigned
            if (snowParticles == null)
            {
                var snowObj = new GameObject("SnowParticles");
                snowObj.transform.SetParent(transform);
                snowObj.transform.localPosition = new Vector3(0, 50, 0);
                
                snowParticles = snowObj.AddComponent<ParticleSystem>();
                var main = snowParticles.main;
                main.maxParticles = 5000;
                main.startLifetime = 8f;
                main.startSpeed = 3f;
                main.startSize = 0.15f;
                main.startColor = Color.white;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.loop = true;
                main.playOnAwake = false;
                
                var emission = snowParticles.emission;
                emission.rateOverTime = 0;
                
                var shape = snowParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(100, 1, 100);
                
                var velocity = snowParticles.velocityOverLifetime;
                velocity.enabled = true;
                velocity.x = new ParticleSystem.MinMaxCurve(-1f, 1f);
                velocity.y = new ParticleSystem.MinMaxCurve(-3f);
                velocity.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
                
                ApexLogger.Log("[Weather] Created snow particle system", ApexLogger.LogCategory.General);
            }
            
            // Create fog particles if not assigned
            if (fogParticles == null)
            {
                var fogObj = new GameObject("FogParticles");
                fogObj.transform.SetParent(transform);
                fogObj.transform.localPosition = new Vector3(0, 5, 0);
                
                fogParticles = fogObj.AddComponent<ParticleSystem>();
                var main = fogParticles.main;
                main.maxParticles = 500;
                main.startLifetime = 10f;
                main.startSpeed = 0.5f;
                main.startSize = 20f;
                main.startColor = new Color(0.8f, 0.8f, 0.9f, 0.3f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.loop = true;
                main.playOnAwake = false;
                
                var emission = fogParticles.emission;
                emission.rateOverTime = 0;
                
                var shape = fogParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(200, 10, 200);
                
                ApexLogger.Log("[Weather] Created fog particle system", ApexLogger.LogCategory.General);
            }
            
            // Create dust/sand particles if not assigned
            if (dustParticles == null)
            {
                var dustObj = new GameObject("DustParticles");
                dustObj.transform.SetParent(transform);
                dustObj.transform.localPosition = new Vector3(0, 10, 0);
                
                dustParticles = dustObj.AddComponent<ParticleSystem>();
                var main = dustParticles.main;
                main.maxParticles = 3000;
                main.startLifetime = 5f;
                main.startSpeed = 15f;
                main.startSize = 0.5f;
                main.startColor = new Color(0.8f, 0.7f, 0.5f, 0.4f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.loop = true;
                main.playOnAwake = false;
                
                var emission = dustParticles.emission;
                emission.rateOverTime = 0;
                
                var shape = dustParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(150, 20, 150);
                
                var velocity = dustParticles.velocityOverLifetime;
                velocity.enabled = true;
                velocity.x = new ParticleSystem.MinMaxCurve(15f);
                velocity.y = new ParticleSystem.MinMaxCurve(-2f, 2f);
                velocity.z = new ParticleSystem.MinMaxCurve(0f);
                
                ApexLogger.Log("[Weather] Created dust particle system", ApexLogger.LogCategory.General);
            }
        }

        private IEnumerator WeatherCycle()
        {
            while (true)
            {
                yield return new WaitForSeconds(weatherCheckInterval);
                
                if (enableDynamicWeather)
                {
                    // Determine next weather based on current weather and random chance
                    WeatherType nextWeather = DetermineNextWeather();
                    
                    if (nextWeather != _currentWeather)
                    {
                        TransitionToWeather(nextWeather);
                    }
                }
            }
        }

        private WeatherType DetermineNextWeather()
        {
            // Weather transition probabilities based on current weather
            float roll = UnityEngine.Random.value;
            
            switch (_currentWeather)
            {
                case WeatherType.Clear:
                    if (roll < 0.4f) return WeatherType.Cloudy;
                    if (roll < 0.5f) return WeatherType.Heatwave;
                    return WeatherType.Clear;
                    
                case WeatherType.Cloudy:
                    if (roll < 0.3f) return WeatherType.Rain;
                    if (roll < 0.4f) return WeatherType.Fog;
                    if (roll < 0.6f) return WeatherType.Clear;
                    return WeatherType.Cloudy;
                    
                case WeatherType.Rain:
                    if (roll < 0.2f) return WeatherType.Storm;
                    if (roll < 0.5f) return WeatherType.Cloudy;
                    return WeatherType.Rain;
                    
                case WeatherType.Storm:
                    if (roll < 0.4f) return WeatherType.Rain;
                    if (roll < 0.6f) return WeatherType.Cloudy;
                    return WeatherType.Storm;
                    
                case WeatherType.Fog:
                    if (roll < 0.5f) return WeatherType.Cloudy;
                    if (roll < 0.6f) return WeatherType.Rain;
                    return WeatherType.Fog;
                    
                case WeatherType.Snow:
                    if (roll < 0.3f) return WeatherType.Cloudy;
                    if (roll < 0.4f) return WeatherType.Storm;
                    return WeatherType.Snow;
                    
                case WeatherType.Sandstorm:
                    if (roll < 0.5f) return WeatherType.Clear;
                    if (roll < 0.6f) return WeatherType.Heatwave;
                    return WeatherType.Sandstorm;
                    
                case WeatherType.Heatwave:
                    if (roll < 0.4f) return WeatherType.Clear;
                    if (roll < 0.5f) return WeatherType.Sandstorm;
                    return WeatherType.Heatwave;
                    
                default:
                    return WeatherType.Clear;
            }
        }

        public void TransitionToWeather(WeatherType newWeather, float customDuration = -1)
        {
            if (newWeather == _currentWeather) return;
            
            _targetWeather = newWeather;
            _transitionProgress = 0f;
            
            if (customDuration > 0)
            {
                StartCoroutine(TransitionCoroutine(customDuration));
            }
            else
            {
                StartCoroutine(TransitionCoroutine(transitionDuration));
            }
            
            ApexLogger.Log($"[Weather] Transitioning from {_currentWeather} to {_targetWeather}", ApexLogger.LogCategory.General);
        }

        private IEnumerator TransitionCoroutine(float duration)
        {
            float elapsed = 0f;
            WeatherType fromWeather = _currentWeather;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _transitionProgress = Mathf.Clamp01(elapsed / duration);
                
                OnWeatherTransition?.Invoke(fromWeather, _targetWeather, _transitionProgress);
                
                yield return null;
            }
            
            _transitionProgress = 1f;
            _currentWeather = _targetWeather;
            _intensity = GetDefaultIntensity(_currentWeather);
            
            OnWeatherChanged?.Invoke(_currentWeather);
            
            // Notify UI
            if (UI.NotificationSystem.Instance != null)
            {
                var effects = CurrentEffects;
                UI.NotificationSystem.Instance.ShowToast(
                    $"Weather: {GetWeatherDisplayName(_currentWeather)}\n{effects.Description}",
                    UI.ToastNotificationType.Info,
                    "🌤 Weather Update"
                );
            }
            
            ApexLogger.Log($"[Weather] Now: {_currentWeather}", ApexLogger.LogCategory.General);
        }

        private void UpdateWeatherTransition()
        {
            // Interpolate intensity
            float fromIntensity = GetDefaultIntensity(_currentWeather);
            float toIntensity = GetDefaultIntensity(_targetWeather);
            _intensity = Mathf.Lerp(fromIntensity, toIntensity, _transitionProgress);
            _targetIntensity = toIntensity;
            
            // Update lighting
            UpdateLighting();
            
            // Update fog
            UpdateFog();
        }

        private void UpdateLighting()
        {
            if (sunLight == null) return;
            
            // Get lighting modifiers for current/target weather
            Color fromColor = GetSunColorForWeather(_currentWeather);
            Color toColor = GetSunColorForWeather(_targetWeather);
            float fromIntensity = GetSunIntensityForWeather(_currentWeather);
            float toIntensity = GetSunIntensityForWeather(_targetWeather);
            
            sunLight.color = Color.Lerp(fromColor, toColor, _transitionProgress);
            sunLight.intensity = Mathf.Lerp(fromIntensity, toIntensity, _transitionProgress);
            
            // Update ambient
            Color fromAmbient = GetAmbientColorForWeather(_currentWeather);
            Color toAmbient = GetAmbientColorForWeather(_targetWeather);
            RenderSettings.ambientSkyColor = Color.Lerp(fromAmbient, toAmbient, _transitionProgress);
        }

        private void UpdateFog()
        {
            float fromFog = GetFogDensityForWeather(_currentWeather);
            float toFog = GetFogDensityForWeather(_targetWeather);
            
            RenderSettings.fog = fromFog > 0.001f || toFog > 0.001f;
            RenderSettings.fogDensity = Mathf.Lerp(fromFog, toFog, _transitionProgress) * fogDensityMultiplier;
            
            // Fog color
            Color fromFogColor = GetFogColorForWeather(_currentWeather);
            Color toFogColor = GetFogColorForWeather(_targetWeather);
            RenderSettings.fogColor = Color.Lerp(fromFogColor, toFogColor, _transitionProgress);
        }

        private void UpdateParticles()
        {
            // Rain
            if (rainParticles != null)
            {
                bool shouldRain = _currentWeather == WeatherType.Rain || _currentWeather == WeatherType.Storm ||
                                  _targetWeather == WeatherType.Rain || _targetWeather == WeatherType.Storm;
                
                if (shouldRain)
                {
                    if (!rainParticles.isPlaying) rainParticles.Play();
                    
                    var emission = rainParticles.emission;
                    float targetRate = _currentWeather == WeatherType.Storm ? 8000f : 4000f;
                    emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, targetRate * _intensity, Time.deltaTime * 2f);
                }
                else
                {
                    var emission = rainParticles.emission;
                    emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, 0f, Time.deltaTime * 2f);
                    if (emission.rateOverTime.constant < 1f && rainParticles.isPlaying)
                    {
                        rainParticles.Stop();
                    }
                }
            }
            
            // Snow
            if (snowParticles != null)
            {
                bool shouldSnow = _currentWeather == WeatherType.Snow || _targetWeather == WeatherType.Snow;
                
                if (shouldSnow)
                {
                    if (!snowParticles.isPlaying) snowParticles.Play();
                    
                    var emission = snowParticles.emission;
                    emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, 2000f * _intensity, Time.deltaTime * 2f);
                }
                else
                {
                    var emission = snowParticles.emission;
                    emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, 0f, Time.deltaTime * 2f);
                    if (emission.rateOverTime.constant < 1f && snowParticles.isPlaying)
                    {
                        snowParticles.Stop();
                    }
                }
            }
            
            // Fog
            if (fogParticles != null)
            {
                bool shouldFog = _currentWeather == WeatherType.Fog || _targetWeather == WeatherType.Fog;
                
                if (shouldFog)
                {
                    if (!fogParticles.isPlaying) fogParticles.Play();
                    
                    var emission = fogParticles.emission;
                    emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, 50f * _intensity, Time.deltaTime);
                }
                else
                {
                    var emission = fogParticles.emission;
                    emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, 0f, Time.deltaTime);
                    if (emission.rateOverTime.constant < 0.1f && fogParticles.isPlaying)
                    {
                        fogParticles.Stop();
                    }
                }
            }
            
            // Dust/Sandstorm
            if (dustParticles != null)
            {
                bool shouldDust = _currentWeather == WeatherType.Sandstorm || _targetWeather == WeatherType.Sandstorm;
                
                if (shouldDust)
                {
                    if (!dustParticles.isPlaying) dustParticles.Play();
                    
                    var emission = dustParticles.emission;
                    emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, 1500f * _intensity, Time.deltaTime * 2f);
                }
                else
                {
                    var emission = dustParticles.emission;
                    emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, 0f, Time.deltaTime * 2f);
                    if (emission.rateOverTime.constant < 1f && dustParticles.isPlaying)
                    {
                        dustParticles.Stop();
                    }
                }
            }
        }

        private float _lightningTimer;
        private void UpdateLightning()
        {
            if (_currentWeather != WeatherType.Storm) return;
            
            _lightningTimer -= Time.deltaTime;
            
            if (_lightningTimer <= 0)
            {
                // Random lightning
                if (UnityEngine.Random.value < 0.3f)
                {
                    StartCoroutine(LightningFlashCoroutine());
                }
                
                _lightningTimer = UnityEngine.Random.Range(3f, 15f);
            }
        }

        private IEnumerator LightningFlashCoroutine()
        {
            if (sunLight == null) yield break;
            
            float originalIntensity = sunLight.intensity;
            
            // Flash 1
            sunLight.intensity = 3f;
            yield return new WaitForSeconds(0.05f);
            sunLight.intensity = originalIntensity;
            yield return new WaitForSeconds(0.1f);
            
            // Flash 2
            sunLight.intensity = 2.5f;
            yield return new WaitForSeconds(0.08f);
            sunLight.intensity = originalIntensity;
            
            // Play thunder after delay
            if (weatherAudioSource != null && thunderSound != null)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 3f));
                weatherAudioSource.PlayOneShot(thunderSound, UnityEngine.Random.Range(0.5f, 1f));
            }
            
            OnLightningStrike?.Invoke();
        }

        #region Weather Properties

        private float GetDefaultIntensity(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => 0f,
                WeatherType.Cloudy => 0.3f,
                WeatherType.Rain => 0.7f,
                WeatherType.Storm => 1f,
                WeatherType.Fog => 0.8f,
                WeatherType.Snow => 0.7f,
                WeatherType.Sandstorm => 0.9f,
                WeatherType.Heatwave => 0.6f,
                _ => 0f
            };
        }

        private Color GetSunColorForWeather(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => _baseSunColor,
                WeatherType.Cloudy => new Color(0.8f, 0.85f, 0.9f),
                WeatherType.Rain => new Color(0.6f, 0.65f, 0.75f),
                WeatherType.Storm => new Color(0.4f, 0.45f, 0.55f),
                WeatherType.Fog => new Color(0.75f, 0.78f, 0.85f),
                WeatherType.Snow => new Color(0.85f, 0.9f, 1f),
                WeatherType.Sandstorm => new Color(0.9f, 0.75f, 0.5f),
                WeatherType.Heatwave => new Color(1f, 0.95f, 0.8f),
                _ => _baseSunColor
            };
        }

        private float GetSunIntensityForWeather(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => _baseSunIntensity,
                WeatherType.Cloudy => _baseSunIntensity * 0.7f,
                WeatherType.Rain => _baseSunIntensity * 0.4f,
                WeatherType.Storm => _baseSunIntensity * 0.2f,
                WeatherType.Fog => _baseSunIntensity * 0.5f,
                WeatherType.Snow => _baseSunIntensity * 0.6f,
                WeatherType.Sandstorm => _baseSunIntensity * 0.3f,
                WeatherType.Heatwave => _baseSunIntensity * 1.2f,
                _ => _baseSunIntensity
            };
        }

        private Color GetAmbientColorForWeather(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => _baseAmbientColor,
                WeatherType.Cloudy => new Color(0.5f, 0.55f, 0.65f),
                WeatherType.Rain => new Color(0.4f, 0.45f, 0.55f),
                WeatherType.Storm => new Color(0.3f, 0.32f, 0.4f),
                WeatherType.Fog => new Color(0.55f, 0.58f, 0.65f),
                WeatherType.Snow => new Color(0.6f, 0.65f, 0.75f),
                WeatherType.Sandstorm => new Color(0.6f, 0.5f, 0.35f),
                WeatherType.Heatwave => new Color(0.7f, 0.65f, 0.55f),
                _ => _baseAmbientColor
            };
        }

        private float GetFogDensityForWeather(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => 0f,
                WeatherType.Cloudy => 0.002f,
                WeatherType.Rain => 0.01f,
                WeatherType.Storm => 0.02f,
                WeatherType.Fog => 0.05f,
                WeatherType.Snow => 0.015f,
                WeatherType.Sandstorm => 0.04f,
                WeatherType.Heatwave => 0.005f,
                _ => 0f
            };
        }

        private Color GetFogColorForWeather(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => Color.white,
                WeatherType.Cloudy => new Color(0.7f, 0.75f, 0.8f),
                WeatherType.Rain => new Color(0.5f, 0.55f, 0.65f),
                WeatherType.Storm => new Color(0.35f, 0.4f, 0.5f),
                WeatherType.Fog => new Color(0.8f, 0.82f, 0.88f),
                WeatherType.Snow => new Color(0.9f, 0.92f, 0.95f),
                WeatherType.Sandstorm => new Color(0.8f, 0.65f, 0.4f),
                WeatherType.Heatwave => new Color(0.95f, 0.9f, 0.8f),
                _ => Color.white
            };
        }

        private string GetWeatherDisplayName(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => "[D] Clear",
                WeatherType.Cloudy => "[C] Cloudy",
                WeatherType.Rain => "🌧 Rain",
                WeatherType.Storm => "[S] Storm",
                WeatherType.Fog => "🌫 Fog",
                WeatherType.Snow => "[C] Snow",
                WeatherType.Sandstorm => "🏜 Sandstorm",
                WeatherType.Heatwave => "[*] Heatwave",
                _ => "Unknown"
            };
        }

        #endregion

        #region Public API

        public WeatherEffects GetEffectsForWeather(WeatherType weather)
        {
            return _weatherEffects.TryGetValue(weather, out var effects) ? effects : new WeatherEffects();
        }

        public void SetWeather(WeatherType weather, float duration = -1)
        {
            TransitionToWeather(weather, duration);
        }

        public void SetIntensity(float intensity)
        {
            _targetIntensity = Mathf.Clamp01(intensity);
        }

        public float GetVisibilityMultiplier()
        {
            return Mathf.Lerp(
                _weatherEffects[_currentWeather].VisibilityMultiplier,
                _weatherEffects[_targetWeather].VisibilityMultiplier,
                _transitionProgress
            );
        }

        public float GetMovementMultiplier()
        {
            return Mathf.Lerp(
                _weatherEffects[_currentWeather].MovementMultiplier,
                _weatherEffects[_targetWeather].MovementMultiplier,
                _transitionProgress
            );
        }

        public int GetCombatAttackBonus()
        {
            return (int)Mathf.Lerp(
                _weatherEffects[_currentWeather].CombatAttackBonus,
                _weatherEffects[_targetWeather].CombatAttackBonus,
                _transitionProgress
            );
        }

        public int GetCombatDefenseBonus()
        {
            return (int)Mathf.Lerp(
                _weatherEffects[_currentWeather].CombatDefenseBonus,
                _weatherEffects[_targetWeather].CombatDefenseBonus,
                _transitionProgress
            );
        }

        public int GetAmbushBonus()
        {
            return (int)Mathf.Lerp(
                _weatherEffects[_currentWeather].AmbushBonus,
                _weatherEffects[_targetWeather].AmbushBonus,
                _transitionProgress
            );
        }

        #endregion
    }

    #region Data Classes

    public enum WeatherType
    {
        Clear,
        Cloudy,
        Rain,
        Storm,
        Fog,
        Snow,
        Sandstorm,
        Heatwave
    }

    [System.Serializable]
    public class WeatherEffects
    {
        public float VisibilityMultiplier = 1f;
        public float MovementMultiplier = 1f;
        public int CombatAttackBonus = 0;
        public int CombatDefenseBonus = 0;
        public float ResourceGatherBonus = 0f;
        public float CropGrowthMultiplier = 1f;
        public float FireRiskMultiplier = 1f;
        public float SolarEfficiency = 1f;
        public int AmbushBonus = 0;
        public float BuildingDamageRisk = 0f;
        public float EquipmentDamageRate = 0f;
        public float HeatingCostMultiplier = 1f;
        public float WaterConsumptionMultiplier = 1f;
        public string Description = "";
    }

    #endregion
}
