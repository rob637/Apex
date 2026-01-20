using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using ApexCitadels.Environment;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// UI Controller for fantasy map overlay settings.
    /// Allows players to customize their visual experience with
    /// fantasy styles, atmosphere presets, day/night control, and particle effects.
    /// </summary>
    public class FantasyMapUI : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        
        [Header("Fantasy Style")]
        [SerializeField] private TMP_Dropdown styleDropdown;
        [SerializeField] private Slider intensitySlider;
        [SerializeField] private TMP_Text intensityLabel;
        
        [Header("Atmosphere")]
        [SerializeField] private TMP_Dropdown atmosphereDropdown;
        [SerializeField] private Slider bloomSlider;
        [SerializeField] private Slider vignetteSlider;
        [SerializeField] private Toggle filmGrainToggle;
        
        [Header("Day/Night")]
        [SerializeField] private Toggle enableCycleToggle;
        [SerializeField] private Toggle realTimeToggle;
        [SerializeField] private Slider timeSlider;
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private Slider timeScaleSlider;
        [SerializeField] private TMP_Text timeScaleLabel;
        [SerializeField] private Button[] periodButtons; // Dawn, Day, Dusk, Night
        
        [Header("Particles")]
        [SerializeField] private Toggle enableParticlesToggle;
        [SerializeField] private Slider particleIntensitySlider;
        [SerializeField] private TMP_Dropdown weatherDropdown;
        
        [Header("Fog of War")]
        [SerializeField] private Toggle enableFogToggle;
        [SerializeField] private Slider explorationRadiusSlider;
        
        [Header("Quick Presets")]
        [SerializeField] private Button presetDayButton;
        [SerializeField] private Button presetNightButton;
        [SerializeField] private Button presetMysticalButton;
        [SerializeField] private Button presetDarkButton;
        
        [Header("Input")]
        [SerializeField] private KeyCode toggleKey = KeyCode.M;
        [SerializeField] private float fadeDuration = 0.3f;
        
        // System references
        private FantasyMapOverlay _mapOverlay;
        private FantasyAtmosphere _atmosphere;
        private DayNightCycle _dayNight;
        private MapMagicParticles _particles;
        
        // State
        private bool _isVisible;
        private float _fadeProgress;
        
        private void Start()
        {
            // Find systems
            _mapOverlay = FantasyMapOverlay.Instance ?? FindFirstObjectByType<FantasyMapOverlay>();
            _atmosphere = FantasyAtmosphere.Instance ?? FindFirstObjectByType<FantasyAtmosphere>();
            _dayNight = DayNightCycle.Instance ?? FindFirstObjectByType<DayNightCycle>();
            _particles = MapMagicParticles.Instance ?? FindFirstObjectByType<MapMagicParticles>();
            
            SetupUI();
            
            // Start hidden
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
                _isVisible = false;
            }
        }
        
        private void Update()
        {
            // Toggle panel with key
            if (Input.GetKeyDown(toggleKey))
            {
                TogglePanel();
            }
            
            // Update time display if cycle is running
            if (_isVisible && _dayNight != null && timeLabel != null)
            {
                UpdateTimeDisplay();
            }
            
            // Handle fade animation
            if (panelCanvasGroup != null)
            {
                float targetAlpha = _isVisible ? 1f : 0f;
                if (Mathf.Abs(panelCanvasGroup.alpha - targetAlpha) > 0.01f)
                {
                    panelCanvasGroup.alpha = Mathf.MoveTowards(
                        panelCanvasGroup.alpha, 
                        targetAlpha, 
                        Time.unscaledDeltaTime / fadeDuration
                    );
                    
                    if (panelCanvasGroup.alpha < 0.01f && !_isVisible)
                    {
                        settingsPanel.SetActive(false);
                    }
                }
            }
        }
        
        private void SetupUI()
        {
            // ===== Fantasy Style =====
            if (styleDropdown != null)
            {
                styleDropdown.ClearOptions();
                styleDropdown.AddOptions(new System.Collections.Generic.List<string>(
                    Enum.GetNames(typeof(FantasyMapStyle))
                ));
                styleDropdown.onValueChanged.AddListener(OnStyleChanged);
                
                if (_mapOverlay != null)
                {
                    styleDropdown.value = (int)_mapOverlay.GetCurrentStyle();
                }
            }
            
            if (intensitySlider != null)
            {
                intensitySlider.minValue = 0f;
                intensitySlider.maxValue = 1f;
                intensitySlider.value = 0.7f;
                intensitySlider.onValueChanged.AddListener(OnIntensityChanged);
            }
            
            // ===== Atmosphere =====
            if (atmosphereDropdown != null)
            {
                atmosphereDropdown.ClearOptions();
                atmosphereDropdown.AddOptions(new System.Collections.Generic.List<string>(
                    Enum.GetNames(typeof(AtmospherePreset))
                ));
                atmosphereDropdown.onValueChanged.AddListener(OnAtmosphereChanged);
                
                if (_atmosphere != null)
                {
                    atmosphereDropdown.value = (int)_atmosphere.GetCurrentPreset();
                }
            }
            
            if (bloomSlider != null)
            {
                bloomSlider.minValue = 0f;
                bloomSlider.maxValue = 2f;
                bloomSlider.value = 0.8f;
                bloomSlider.onValueChanged.AddListener(OnBloomChanged);
            }
            
            if (vignetteSlider != null)
            {
                vignetteSlider.minValue = 0f;
                vignetteSlider.maxValue = 1f;
                vignetteSlider.value = 0.35f;
                vignetteSlider.onValueChanged.AddListener(OnVignetteChanged);
            }
            
            if (filmGrainToggle != null)
            {
                filmGrainToggle.isOn = true;
                filmGrainToggle.onValueChanged.AddListener(OnFilmGrainToggled);
            }
            
            // ===== Day/Night =====
            if (enableCycleToggle != null)
            {
                enableCycleToggle.isOn = true;
                enableCycleToggle.onValueChanged.AddListener(OnCycleToggled);
            }
            
            if (realTimeToggle != null)
            {
                realTimeToggle.isOn = false;
                realTimeToggle.onValueChanged.AddListener(OnRealTimeToggled);
            }
            
            if (timeSlider != null)
            {
                timeSlider.minValue = 0f;
                timeSlider.maxValue = 24f;
                timeSlider.value = 12f;
                timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
            }
            
            if (timeScaleSlider != null)
            {
                timeScaleSlider.minValue = 0f;
                timeScaleSlider.maxValue = 10f;
                timeScaleSlider.value = 1f;
                timeScaleSlider.onValueChanged.AddListener(OnTimeScaleChanged);
            }
            
            // Period buttons
            if (periodButtons != null && periodButtons.Length >= 4)
            {
                periodButtons[0]?.onClick.AddListener(() => JumpToPeriod(TimePeriod.Dawn));
                periodButtons[1]?.onClick.AddListener(() => JumpToPeriod(TimePeriod.Day));
                periodButtons[2]?.onClick.AddListener(() => JumpToPeriod(TimePeriod.Dusk));
                periodButtons[3]?.onClick.AddListener(() => JumpToPeriod(TimePeriod.Night));
            }
            
            // ===== Particles =====
            if (enableParticlesToggle != null)
            {
                enableParticlesToggle.isOn = true;
                enableParticlesToggle.onValueChanged.AddListener(OnParticlesToggled);
            }
            
            if (particleIntensitySlider != null)
            {
                particleIntensitySlider.minValue = 0f;
                particleIntensitySlider.maxValue = 1f;
                particleIntensitySlider.value = 1f;
                particleIntensitySlider.onValueChanged.AddListener(OnParticleIntensityChanged);
            }
            
            if (weatherDropdown != null)
            {
                weatherDropdown.ClearOptions();
                weatherDropdown.AddOptions(new System.Collections.Generic.List<string>(
                    Enum.GetNames(typeof(WeatherEffect))
                ));
                weatherDropdown.onValueChanged.AddListener(OnWeatherChanged);
            }
            
            // ===== Fog of War =====
            if (enableFogToggle != null)
            {
                enableFogToggle.isOn = true;
                enableFogToggle.onValueChanged.AddListener(OnFogToggled);
            }
            
            // ===== Quick Presets =====
            presetDayButton?.onClick.AddListener(ApplyDayPreset);
            presetNightButton?.onClick.AddListener(ApplyNightPreset);
            presetMysticalButton?.onClick.AddListener(ApplyMysticalPreset);
            presetDarkButton?.onClick.AddListener(ApplyDarkPreset);
        }
        
        #region Event Handlers
        
        private void OnStyleChanged(int index)
        {
            if (_mapOverlay != null)
            {
                _mapOverlay.SetStyle((FantasyMapStyle)index);
            }
        }
        
        private void OnIntensityChanged(float value)
        {
            if (_mapOverlay != null)
            {
                _mapOverlay.SetIntensity(value);
            }
            
            if (intensityLabel != null)
            {
                intensityLabel.text = $"{value:P0}";
            }
        }
        
        private void OnAtmosphereChanged(int index)
        {
            if (_atmosphere != null)
            {
                _atmosphere.ApplyPreset((AtmospherePreset)index);
            }
        }
        
        private void OnBloomChanged(float value)
        {
            if (_atmosphere != null)
            {
                _atmosphere.SetBloomIntensity(value);
            }
        }
        
        private void OnVignetteChanged(float value)
        {
            if (_atmosphere != null)
            {
                _atmosphere.SetVignetteIntensity(value);
            }
        }
        
        private void OnFilmGrainToggled(bool enabled)
        {
            if (_atmosphere != null)
            {
                _atmosphere.ToggleFilmGrain();
            }
        }
        
        private void OnCycleToggled(bool enabled)
        {
            if (_dayNight != null)
            {
                _dayNight.SetCycleEnabled(enabled);
            }
            
            // Enable/disable time slider based on cycle
            if (timeSlider != null)
            {
                timeSlider.interactable = !enabled;
            }
        }
        
        private void OnRealTimeToggled(bool useRealTime)
        {
            if (_dayNight != null)
            {
                _dayNight.SetRealTimeMode(useRealTime);
            }
        }
        
        private void OnTimeSliderChanged(float time)
        {
            if (_dayNight != null && !enableCycleToggle.isOn)
            {
                _dayNight.SetTime(time);
            }
            
            UpdateTimeDisplay();
        }
        
        private void OnTimeScaleChanged(float scale)
        {
            if (_dayNight != null)
            {
                _dayNight.SetTimeScale(scale);
            }
            
            if (timeScaleLabel != null)
            {
                timeScaleLabel.text = $"{scale:F1}x";
            }
        }
        
        private void JumpToPeriod(TimePeriod period)
        {
            if (_dayNight != null)
            {
                _dayNight.JumpToPeriod(period);
                
                if (timeSlider != null)
                {
                    timeSlider.SetValueWithoutNotify(_dayNight.GetCurrentTime());
                }
            }
        }
        
        private void OnParticlesToggled(bool enabled)
        {
            if (_particles != null)
            {
                _particles.ToggleParticles();
            }
        }
        
        private void OnParticleIntensityChanged(float value)
        {
            if (_particles != null)
            {
                _particles.SetIntensity(value);
            }
        }
        
        private void OnWeatherChanged(int index)
        {
            if (_particles != null)
            {
                _particles.SetWeather((WeatherEffect)index);
            }
        }
        
        private void OnFogToggled(bool enabled)
        {
            if (_mapOverlay != null)
            {
                _mapOverlay.SetFogOfWar(enabled);
            }
        }
        
        #endregion
        
        #region Quick Presets
        
        private void ApplyDayPreset()
        {
            _mapOverlay?.SetStyle(FantasyMapStyle.AncientParchment);
            _mapOverlay?.SetIntensity(0.6f);
            _atmosphere?.ApplyPreset(AtmospherePreset.MagicalDaylight, immediate: true);
            _dayNight?.SetTime(12f);
            _particles?.SetWeather(WeatherEffect.Clear);
            
            UpdateUIFromSystems();
        }
        
        private void ApplyNightPreset()
        {
            _mapOverlay?.SetStyle(FantasyMapStyle.MysticalGlow);
            _mapOverlay?.SetIntensity(0.8f);
            _atmosphere?.ApplyPreset(AtmospherePreset.EnchantedNight, immediate: true);
            _dayNight?.SetTime(0f);
            _particles?.SetWeather(WeatherEffect.Fireflies);
            
            UpdateUIFromSystems();
        }
        
        private void ApplyMysticalPreset()
        {
            _mapOverlay?.SetStyle(FantasyMapStyle.MysticalGlow);
            _mapOverlay?.SetIntensity(0.85f);
            _atmosphere?.ApplyPreset(AtmospherePreset.MysticalTwilight, immediate: true);
            _dayNight?.SetTime(18.5f);
            _particles?.SetWeather(WeatherEffect.Mystical);
            
            UpdateUIFromSystems();
        }
        
        private void ApplyDarkPreset()
        {
            _mapOverlay?.SetStyle(FantasyMapStyle.DarkRealm);
            _mapOverlay?.SetIntensity(0.9f);
            _atmosphere?.ApplyPreset(AtmospherePreset.DarkFantasy, immediate: true);
            _dayNight?.SetTime(22f);
            _particles?.SetWeather(WeatherEffect.Clear);
            
            UpdateUIFromSystems();
        }
        
        private void UpdateUIFromSystems()
        {
            if (_mapOverlay != null && styleDropdown != null)
            {
                styleDropdown.SetValueWithoutNotify((int)_mapOverlay.GetCurrentStyle());
            }
            
            if (_atmosphere != null && atmosphereDropdown != null)
            {
                atmosphereDropdown.SetValueWithoutNotify((int)_atmosphere.GetCurrentPreset());
            }
            
            if (_dayNight != null && timeSlider != null)
            {
                timeSlider.SetValueWithoutNotify(_dayNight.GetCurrentTime());
            }
            
            if (_particles != null && weatherDropdown != null)
            {
                weatherDropdown.SetValueWithoutNotify((int)_particles.GetCurrentWeather());
            }
        }
        
        #endregion
        
        #region UI Helpers
        
        private void UpdateTimeDisplay()
        {
            if (_dayNight != null && timeLabel != null)
            {
                timeLabel.text = _dayNight.GetFormattedTime();
            }
        }
        
        /// <summary>
        /// Toggle settings panel visibility
        /// </summary>
        public void TogglePanel()
        {
            _isVisible = !_isVisible;
            
            if (_isVisible)
            {
                settingsPanel.SetActive(true);
                UpdateUIFromSystems();
            }
            
            // Fade will handle hiding
        }
        
        /// <summary>
        /// Show settings panel
        /// </summary>
        public void ShowPanel()
        {
            if (!_isVisible)
            {
                TogglePanel();
            }
        }
        
        /// <summary>
        /// Hide settings panel
        /// </summary>
        public void HidePanel()
        {
            if (_isVisible)
            {
                TogglePanel();
            }
        }
        
        #endregion
    }
}
