using UnityEngine;
using ApexCitadels.Core;
using ApexCitadels.Environment;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Master controller for the Fantasy Map Overlay system.
    /// Initializes and coordinates all fantasy visualization components:
    /// - FantasyMapOverlay: Parchment textures, fog of war, map decorations
    /// - FantasyAtmosphere: Post-processing, color grading, bloom
    /// - MapMagicParticles: Floating orbs, sparkles, weather effects
    /// - DayNightCycle: Lighting, time of day, sun/moon
    /// </summary>
    public class FantasyMapController : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private FantasyMapOverlay mapOverlay;
        [SerializeField] private FantasyAtmosphere atmosphere;
        [SerializeField] private MapMagicParticles particles;
        [SerializeField] private DayNightCycle dayNight;
        [SerializeField] private FantasyMapUI ui;
        
        [Header("Auto-Create Components")]
        [SerializeField] private bool autoCreateMissing = true;
        
        [Header("Default Settings")]
        [SerializeField] private FantasyMapStyle defaultStyle = FantasyMapStyle.AncientParchment;
        [SerializeField] private AtmospherePreset defaultAtmosphere = AtmospherePreset.MagicalDaylight;
        [SerializeField] private float defaultTimeOfDay = 12f;
        [SerializeField] private bool enableParticlesByDefault = true;
        
        [Header("Performance")]
        [SerializeField] private bool useSimplifiedParticles = false;
        [SerializeField] private bool disablePostProcessing = false;
        
        // Singleton
        private static FantasyMapController _instance;
        public static FantasyMapController Instance => _instance;
        
        // Public accessors
        public FantasyMapOverlay MapOverlay => mapOverlay;
        public FantasyAtmosphere Atmosphere => atmosphere;
        public MapMagicParticles Particles => particles;
        public DayNightCycle DayNight => dayNight;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeComponents();
        }
        
        private void Start()
        {
            ApplyDefaultSettings();
            
            ApexLogger.Log("Fantasy Map Overlay system initialized", ApexLogger.LogCategory.Map);
            ApexLogger.Log($"  - Map Style: {defaultStyle}", ApexLogger.LogCategory.Map);
            ApexLogger.Log($"  - Atmosphere: {defaultAtmosphere}", ApexLogger.LogCategory.Map);
            ApexLogger.Log($"  - Time: {defaultTimeOfDay:F1}h", ApexLogger.LogCategory.Map);
            ApexLogger.Log($"  - Particles: {(enableParticlesByDefault ? "Enabled" : "Disabled")}", ApexLogger.LogCategory.Map);
        }
        
        private void InitializeComponents()
        {
            // Find or create FantasyMapOverlay
            if (mapOverlay == null)
            {
                mapOverlay = GetComponentInChildren<FantasyMapOverlay>();
                
                if (mapOverlay == null && autoCreateMissing)
                {
                    var overlayObj = new GameObject("FantasyMapOverlay");
                    overlayObj.transform.SetParent(transform);
                    mapOverlay = overlayObj.AddComponent<FantasyMapOverlay>();
                }
            }
            
            // Find or create FantasyAtmosphere
            if (atmosphere == null && !disablePostProcessing)
            {
                atmosphere = GetComponentInChildren<FantasyAtmosphere>();
                
                if (atmosphere == null && autoCreateMissing)
                {
                    var atmosphereObj = new GameObject("FantasyAtmosphere");
                    atmosphereObj.transform.SetParent(transform);
                    atmosphere = atmosphereObj.AddComponent<FantasyAtmosphere>();
                }
            }
            
            // Find or create MapMagicParticles
            if (particles == null)
            {
                particles = GetComponentInChildren<MapMagicParticles>();
                
                if (particles == null && autoCreateMissing)
                {
                    var particlesObj = new GameObject("MapMagicParticles");
                    particlesObj.transform.SetParent(transform);
                    particles = particlesObj.AddComponent<MapMagicParticles>();
                }
            }
            
            // Find or create DayNightCycle
            if (dayNight == null)
            {
                dayNight = GetComponentInChildren<DayNightCycle>();
                
                if (dayNight == null && autoCreateMissing)
                {
                    var dayNightObj = new GameObject("DayNightCycle");
                    dayNightObj.transform.SetParent(transform);
                    dayNight = dayNightObj.AddComponent<DayNightCycle>();
                }
            }
            
            // Find UI (optional)
            if (ui == null)
            {
                ui = FindFirstObjectByType<FantasyMapUI>();
            }
        }
        
        private void ApplyDefaultSettings()
        {
            // Apply default map style
            if (mapOverlay != null)
            {
                mapOverlay.SetStyle(defaultStyle);
            }
            
            // Apply default atmosphere
            if (atmosphere != null)
            {
                atmosphere.ApplyPreset(defaultAtmosphere, immediate: true);
            }
            
            // Set default time
            if (dayNight != null)
            {
                dayNight.SetTime(defaultTimeOfDay);
            }
            
            // Configure particles
            if (particles != null)
            {
                if (!enableParticlesByDefault)
                {
                    particles.ToggleParticles();
                }
                
                if (useSimplifiedParticles)
                {
                    particles.SetIntensity(0.5f);
                }
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Apply a complete visual preset
        /// </summary>
        public void ApplyPreset(FantasyPreset preset)
        {
            switch (preset)
            {
                case FantasyPreset.Daylight:
                    mapOverlay?.SetStyle(FantasyMapStyle.AncientParchment);
                    mapOverlay?.SetIntensity(0.6f);
                    atmosphere?.ApplyPreset(AtmospherePreset.MagicalDaylight);
                    dayNight?.SetTime(12f);
                    particles?.SetWeather(WeatherEffect.Clear);
                    break;
                    
                case FantasyPreset.GoldenHour:
                    mapOverlay?.SetStyle(FantasyMapStyle.DesertKingdom);
                    mapOverlay?.SetIntensity(0.7f);
                    atmosphere?.ApplyPreset(AtmospherePreset.GoldenHour);
                    dayNight?.SetTime(18f);
                    particles?.SetWeather(WeatherEffect.Clear);
                    break;
                    
                case FantasyPreset.Twilight:
                    mapOverlay?.SetStyle(FantasyMapStyle.MysticalGlow);
                    mapOverlay?.SetIntensity(0.8f);
                    atmosphere?.ApplyPreset(AtmospherePreset.MysticalTwilight);
                    dayNight?.SetTime(19f);
                    particles?.SetWeather(WeatherEffect.Mystical);
                    break;
                    
                case FantasyPreset.EnchantedNight:
                    mapOverlay?.SetStyle(FantasyMapStyle.MysticalGlow);
                    mapOverlay?.SetIntensity(0.85f);
                    atmosphere?.ApplyPreset(AtmospherePreset.EnchantedNight);
                    dayNight?.SetTime(0f);
                    particles?.SetWeather(WeatherEffect.Fireflies);
                    break;
                    
                case FantasyPreset.DarkRealm:
                    mapOverlay?.SetStyle(FantasyMapStyle.DarkRealm);
                    mapOverlay?.SetIntensity(0.9f);
                    atmosphere?.ApplyPreset(AtmospherePreset.DarkFantasy);
                    dayNight?.SetTime(22f);
                    particles?.SetWeather(WeatherEffect.Clear);
                    break;
                    
                case FantasyPreset.WinterKingdom:
                    mapOverlay?.SetStyle(FantasyMapStyle.FrozenNorth);
                    mapOverlay?.SetIntensity(0.75f);
                    atmosphere?.ApplyPreset(AtmospherePreset.MagicalDaylight);
                    dayNight?.SetTime(10f);
                    particles?.SetWeather(WeatherEffect.Snow);
                    break;
                    
                case FantasyPreset.StormyWeather:
                    mapOverlay?.SetStyle(FantasyMapStyle.AncientParchment);
                    mapOverlay?.SetIntensity(0.5f);
                    atmosphere?.ApplyPreset(AtmospherePreset.DarkFantasy);
                    dayNight?.SetTime(15f);
                    particles?.SetWeather(WeatherEffect.Rain);
                    break;
                    
                case FantasyPreset.VolcanicLands:
                    mapOverlay?.SetStyle(FantasyMapStyle.VolcanicWastes);
                    mapOverlay?.SetIntensity(0.85f);
                    atmosphere?.ApplyPreset(AtmospherePreset.CombatIntense);
                    dayNight?.SetTime(20f);
                    particles?.SetWeather(WeatherEffect.Clear);
                    break;
            }
            
            ApexLogger.Log($"Applied preset: {preset}", ApexLogger.LogCategory.Map);
        }
        
        /// <summary>
        /// Toggle all fantasy effects
        /// </summary>
        public void ToggleFantasyMode()
        {
            mapOverlay?.ToggleFantasyOverlay();
            particles?.ToggleParticles();
        }
        
        /// <summary>
        /// Set performance mode (reduces particle count and disables some effects)
        /// </summary>
        public void SetPerformanceMode(bool enabled)
        {
            if (enabled)
            {
                particles?.SetIntensity(0.3f);
                atmosphere?.SetBloomIntensity(0.3f);
            }
            else
            {
                particles?.SetIntensity(1f);
                atmosphere?.SetBloomIntensity(0.8f);
            }
        }
        
        /// <summary>
        /// Trigger combat visual mode
        /// </summary>
        public void TriggerCombatMode()
        {
            atmosphere?.TriggerCombatAtmosphere();
        }
        
        /// <summary>
        /// Restore normal visual mode after combat
        /// </summary>
        public void RestoreNormalMode()
        {
            atmosphere?.RestoreNormalAtmosphere();
        }
        
        /// <summary>
        /// Mark a location as explored (removes fog)
        /// </summary>
        public void MarkExplored(Vector3 worldPosition)
        {
            mapOverlay?.MarkExplored(worldPosition);
        }
        
        /// <summary>
        /// Add territory aura effect
        /// </summary>
        public void AddTerritoryAura(string id, Vector3 center, float radius, bool isOwned)
        {
            var ownership = isOwned ? TerritoryOwnership.Owned : TerritoryOwnership.Neutral;
            particles?.AddTerritoryAura(id, center, radius, ownership);
        }
        
        /// <summary>
        /// Remove territory aura
        /// </summary>
        public void RemoveTerritoryAura(string id)
        {
            particles?.RemoveTerritoryAura(id);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Complete visual presets combining all systems
    /// </summary>
    public enum FantasyPreset
    {
        Daylight,       // Bright fantasy day
        GoldenHour,     // Warm sunset
        Twilight,       // Purple dusk
        EnchantedNight, // Magical night
        DarkRealm,      // Gothic dark
        WinterKingdom,  // Snowy fantasy
        StormyWeather,  // Rainy atmosphere
        VolcanicLands   // Fire and brimstone
    }
}
