using System;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Controls the day/night cycle, syncing with real-time or running at a scaled speed.
    /// Updates lighting, sky colors, and ambient effects.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance { get; private set; }
        
        [Header("Time Settings")]
        [SerializeField] private bool syncWithRealTime = true;
        [SerializeField] private float timeScale = 1f; // Game hours per real second (when not syncing)
        [SerializeField] private float updateInterval = 1f; // Update frequency in seconds
        
        [Header("Override")]
        [SerializeField] private bool useOverrideTime = false;
        [SerializeField] [Range(0f, 24f)] private float overrideHour = 12f;

        [Header("Sun Colors")]
        [SerializeField] private Gradient sunColorGradient;
        [SerializeField] private AnimationCurve sunIntensityCurve;
        
        [Header("Sky Colors")]
        [SerializeField] private Color daySkyColor = new Color(0.4f, 0.6f, 0.9f);
        [SerializeField] private Color sunsetSkyColor = new Color(0.9f, 0.5f, 0.3f);
        [SerializeField] private Color nightSkyColor = new Color(0.05f, 0.05f, 0.15f);
        
        [Header("Ambient")]
        [SerializeField] private Color dayAmbient = new Color(0.6f, 0.7f, 0.8f);
        [SerializeField] private Color nightAmbient = new Color(0.1f, 0.1f, 0.2f);

        // State
        private Light _sun;
        private float _lastUpdate;
        private float _currentHour = 12f;
        private float _gameTimeHours = 12f; // For non-real-time mode
        
        // Events
        public event Action<float> OnTimeChanged; // 0-24 hour
        public event Action OnSunrise;
        public event Action OnSunset;
        
        public float CurrentHour => _currentHour;
        public float NormalizedTime => _currentHour / 24f; // 0-1
        public bool IsDay => _currentHour >= 6f && _currentHour < 18f;
        public bool IsNight => !IsDay;

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
            _sun = GetComponent<Light>();
            
            // Setup default gradients if not set
            if (sunColorGradient == null || sunColorGradient.colorKeys.Length == 0)
            {
                sunColorGradient = CreateDefaultSunGradient();
            }
            if (sunIntensityCurve == null || sunIntensityCurve.keys.Length == 0)
            {
                sunIntensityCurve = CreateDefaultIntensityCurve();
            }
            
            // Initialize to current time
            if (syncWithRealTime)
            {
                _currentHour = (float)DateTime.Now.TimeOfDay.TotalHours;
            }
            else
            {
                _currentHour = _gameTimeHours;
            }
            
            UpdateSunPosition();
            UpdateLighting();
            
            ApexLogger.Log($"[DayNight] Started at {_currentHour:F1}:00 - IsDay: {IsDay}", ApexLogger.LogCategory.General);
        }

        private void Update()
        {
            if (Time.time - _lastUpdate > updateInterval)
            {
                UpdateTime();
                UpdateSunPosition();
                UpdateLighting();
                _lastUpdate = Time.time;
            }
        }

        private void UpdateTime()
        {
            float previousHour = _currentHour;
            
            if (useOverrideTime)
            {
                _currentHour = overrideHour;
            }
            else if (syncWithRealTime)
            {
                _currentHour = (float)DateTime.Now.TimeOfDay.TotalHours;
            }
            else
            {
                // Advance game time
                _gameTimeHours += timeScale * updateInterval;
                if (_gameTimeHours >= 24f) _gameTimeHours -= 24f;
                _currentHour = _gameTimeHours;
            }
            
            // Check for sunrise/sunset events
            if (previousHour < 6f && _currentHour >= 6f)
            {
                OnSunrise?.Invoke();
                ApexLogger.Log("[DayNight] Sunrise!", ApexLogger.LogCategory.General);
            }
            if (previousHour < 18f && _currentHour >= 18f)
            {
                OnSunset?.Invoke();
                ApexLogger.Log("[DayNight] Sunset!", ApexLogger.LogCategory.General);
            }
            
            OnTimeChanged?.Invoke(_currentHour);
        }

        private void UpdateSunPosition()
        {
            // Map 0..24 hours to sun rotation
            // 6:00 AM -> 0 degrees (Sunrise, horizon)
            // 12:00 PM -> 90 degrees (Noon, highest)
            // 18:00 PM -> 180 degrees (Sunset, horizon)
            // 00:00 AM -> 270 degrees (Midnight, below horizon)
            
            float angle = (_currentHour - 6f) * 15f;
            
            // Add slight wobble to Y for more natural look
            float yAngle = -30f + Mathf.Sin(_currentHour * 0.5f) * 5f;
            
            transform.rotation = Quaternion.Euler(angle, yAngle, 0f);
        }

        private void UpdateLighting()
        {
            if (_sun == null) return;
            
            // Normalized time for gradients (0 = midnight, 0.5 = noon)
            float t = _currentHour / 24f;
            
            // Sun color from gradient
            _sun.color = sunColorGradient.Evaluate(t);
            
            // Sun intensity from curve
            _sun.intensity = sunIntensityCurve.Evaluate(t);
            
            // Determine sky color based on time of day
            Color skyColor;
            if (_currentHour >= 5f && _currentHour < 7f)
            {
                // Sunrise transition
                float st = (_currentHour - 5f) / 2f;
                skyColor = Color.Lerp(nightSkyColor, sunsetSkyColor, st);
            }
            else if (_currentHour >= 7f && _currentHour < 17f)
            {
                // Daytime
                skyColor = daySkyColor;
            }
            else if (_currentHour >= 17f && _currentHour < 19f)
            {
                // Sunset transition
                float st = (_currentHour - 17f) / 2f;
                skyColor = Color.Lerp(daySkyColor, sunsetSkyColor, st);
            }
            else if (_currentHour >= 19f && _currentHour < 21f)
            {
                // Dusk
                float st = (_currentHour - 19f) / 2f;
                skyColor = Color.Lerp(sunsetSkyColor, nightSkyColor, st);
            }
            else
            {
                // Night
                skyColor = nightSkyColor;
            }
            
            // Apply to camera background
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = skyColor;
            }
            
            // Update ambient lighting
            Color ambient = Color.Lerp(nightAmbient, dayAmbient, _sun.intensity);
            RenderSettings.ambientSkyColor = ambient;
            RenderSettings.ambientEquatorColor = ambient * 0.8f;
            RenderSettings.ambientGroundColor = ambient * 0.4f;
            
            // Update fog color
            RenderSettings.fogColor = Color.Lerp(skyColor, ambient, 0.5f);
            
            // Notify VisualEnhancements if available
            if (VisualEnhancements.Instance != null)
            {
                VisualEnhancements.Instance.UpdateSkyColors(NormalizedTime);
            }
        }

        private Gradient CreateDefaultSunGradient()
        {
            Gradient g = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 0f);      // Midnight - blue
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.25f);     // Sunrise - orange
            colorKeys[2] = new GradientColorKey(new Color(1f, 0.98f, 0.9f), 0.5f);     // Noon - white
            colorKeys[3] = new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.75f);     // Sunset - orange
            colorKeys[4] = new GradientColorKey(new Color(0.2f, 0.2f, 0.4f), 1f);      // Midnight - blue
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            g.SetKeys(colorKeys, alphaKeys);
            return g;
        }

        private AnimationCurve CreateDefaultIntensityCurve()
        {
            AnimationCurve c = new AnimationCurve();
            // Night (0-5): very low
            c.AddKey(0f, 0.05f);
            c.AddKey(0.2f, 0.05f);  // 4:48 AM
            // Sunrise (5-7): ramp up
            c.AddKey(0.25f, 0.3f);  // 6 AM
            c.AddKey(0.29f, 0.8f);  // 7 AM
            // Day (7-17): full
            c.AddKey(0.5f, 1.2f);   // Noon
            c.AddKey(0.71f, 0.8f);  // 5 PM
            // Sunset (17-19): ramp down
            c.AddKey(0.75f, 0.3f);  // 6 PM
            c.AddKey(0.79f, 0.1f);  // 7 PM
            // Night (19-24): very low
            c.AddKey(0.83f, 0.05f); // 8 PM
            c.AddKey(1f, 0.05f);
            return c;
        }

        /// <summary>
        /// Set time to a specific hour (for testing/debugging)
        /// </summary>
        public void SetTime(float hour)
        {
            useOverrideTime = true;
            overrideHour = Mathf.Clamp(hour, 0f, 24f);
            _currentHour = overrideHour;
            UpdateSunPosition();
            UpdateLighting();
        }

        /// <summary>
        /// Resume real-time sync
        /// </summary>
        public void ResumeRealTime()
        {
            useOverrideTime = false;
            syncWithRealTime = true;
        }

        /// <summary>
        /// Start accelerated game-time
        /// </summary>
        public void StartGameTime(float hoursPerSecond = 1f)
        {
            useOverrideTime = false;
            syncWithRealTime = false;
            timeScale = hoursPerSecond;
            _gameTimeHours = _currentHour;
        }
    }
}
