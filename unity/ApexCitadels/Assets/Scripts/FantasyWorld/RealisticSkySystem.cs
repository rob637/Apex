// ============================================================================
// REALISTIC SKY SYSTEM - Real sun position, clouds, weather
// ============================================================================
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Calculates real sun position based on time and location.
    /// Adds procedural clouds and weather effects.
    /// </summary>
    public class RealisticSkySystem : MonoBehaviour
    {
        [Header("Location")]
        public double latitude = 38.9065;  // Vienna, VA
        public double longitude = -77.2477;
        
        [Header("Time")]
        public bool useRealTime = true;
        public float timeMultiplier = 1f; // Speed up time for testing
        [Range(0, 24)]
        public float manualHour = 12f;
        
        [Header("Sun")]
        public Light sunLight;
        public Color sunriseColor = new Color(1f, 0.6f, 0.3f);
        public Color noonColor = new Color(1f, 0.98f, 0.95f);
        public Color sunsetColor = new Color(1f, 0.5f, 0.2f);
        public Color nightColor = new Color(0.1f, 0.1f, 0.2f);
        
        [Header("Sky Colors")]
        public Color daySkyColor = new Color(0.4f, 0.6f, 0.9f);
        public Color sunriseSkyColor = new Color(0.9f, 0.6f, 0.5f);
        public Color nightSkyColor = new Color(0.05f, 0.05f, 0.15f);
        
        [Header("Weather")]
        public bool enableWeather = true;
        public WeatherType currentWeather = WeatherType.Clear;
        [Range(0, 1)]
        public float cloudCoverage = 0.3f;
        
        [Header("Clouds")]
        public bool generateClouds = true;
        public int cloudCount = 20;
        public float cloudHeight = 200f;
        public float cloudSpread = 500f;
        
        private Material _skyMaterial;
        private GameObject _cloudParent;
        private ParticleSystem _rainSystem;
        private ParticleSystem _snowSystem;
        private float _currentHour;
        
        public enum WeatherType
        {
            Clear,
            Cloudy,
            Overcast,
            Rain,
            Snow,
            Fog
        }
        
        private void Start()
        {
            SetupSun();
            SetupProceduralSky();
            
            if (generateClouds)
                GenerateClouds();
                
            if (enableWeather)
                SetupWeatherEffects();
        }
        
        private void Update()
        {
            UpdateTime();
            UpdateSunPosition();
            UpdateSkyColors();
            UpdateWeather();
        }
        
        private void SetupSun()
        {
            if (sunLight == null)
            {
                // Find existing directional light
                var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        sunLight = light;
                        break;
                    }
                }
                
                // Create one if none exists
                if (sunLight == null)
                {
                    GameObject sunObj = new GameObject("Sun");
                    sunLight = sunObj.AddComponent<Light>();
                    sunLight.type = LightType.Directional;
                    sunLight.shadows = LightShadows.Soft;
                }
            }
            
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.8f;
        }
        
        private void SetupProceduralSky()
        {
            // Use gradient ambient instead of skybox for now
            RenderSettings.ambientMode = AmbientMode.Trilight;
            
            // Fix dark scenes: Boost ambient light intensity
            RenderSettings.ambientSkyColor = daySkyColor;
            RenderSettings.ambientEquatorColor = Color.Lerp(daySkyColor, Color.gray, 0.5f);
            RenderSettings.ambientGroundColor = Color.gray;
            RenderSettings.ambientIntensity = 1.2f;

            // Disable any existing skybox to show procedural gradient
            // We'll use fog to create atmosphere
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.002f;
        }
        
        private void UpdateTime()
        {
            if (useRealTime)
            {
                DateTime now = DateTime.Now;
                _currentHour = now.Hour + now.Minute / 60f + now.Second / 3600f;
                _currentHour += Time.deltaTime * (timeMultiplier - 1f) / 3600f;
            }
            else
            {
                _currentHour = manualHour;
            }
        }
        
        private void UpdateSunPosition()
        {
            if (sunLight == null) return;
            
            // Calculate sun position using solar equations
            var sunPos = CalculateSunPosition(_currentHour, latitude, longitude);
            
            // Apply to light
            sunLight.transform.rotation = Quaternion.Euler(sunPos.altitude, sunPos.azimuth, 0);
            
            // Adjust intensity based on altitude
            float normalizedAltitude = Mathf.Clamp01(sunPos.altitude / 90f);
            sunLight.intensity = Mathf.Lerp(0.1f, 1.5f, normalizedAltitude);
            
            // Night time - very dim
            if (sunPos.altitude < 0)
            {
                sunLight.intensity = 0.05f;
            }
        }
        
        private (float altitude, float azimuth) CalculateSunPosition(float hour, double lat, double lon)
        {
            // Simplified sun position calculation
            int dayOfYear = DateTime.Now.DayOfYear;
            
            // Solar declination
            double declination = 23.45 * Math.Sin(2 * Math.PI * (dayOfYear - 81) / 365);
            
            // Hour angle (15 degrees per hour from solar noon)
            double solarNoon = 12.0 - lon / 15.0; // Approximate
            double hourAngle = 15 * (hour - solarNoon);
            
            // Convert to radians
            double latRad = lat * Math.PI / 180;
            double decRad = declination * Math.PI / 180;
            double haRad = hourAngle * Math.PI / 180;
            
            // Solar altitude
            double sinAlt = Math.Sin(latRad) * Math.Sin(decRad) + 
                           Math.Cos(latRad) * Math.Cos(decRad) * Math.Cos(haRad);
            double altitude = Math.Asin(sinAlt) * 180 / Math.PI;
            
            // Solar azimuth
            double cosAz = (Math.Sin(decRad) - Math.Sin(latRad) * sinAlt) / 
                          (Math.Cos(latRad) * Math.Cos(altitude * Math.PI / 180));
            cosAz = Math.Max(-1, Math.Min(1, cosAz)); // Clamp
            double azimuth = Math.Acos(cosAz) * 180 / Math.PI;
            
            if (hourAngle > 0) azimuth = 360 - azimuth;
            
            return ((float)altitude, (float)azimuth);
        }
        
        private void UpdateSkyColors()
        {
            if (sunLight == null) return;
            
            // Determine time of day
            float t;
            Color skyColor, sunColor;
            
            if (_currentHour < 6) // Night
            {
                skyColor = nightSkyColor;
                sunColor = nightColor;
            }
            else if (_currentHour < 8) // Sunrise
            {
                t = (_currentHour - 6) / 2f;
                skyColor = Color.Lerp(nightSkyColor, sunriseSkyColor, t);
                sunColor = Color.Lerp(nightColor, sunriseColor, t);
            }
            else if (_currentHour < 10) // Morning transition
            {
                t = (_currentHour - 8) / 2f;
                skyColor = Color.Lerp(sunriseSkyColor, daySkyColor, t);
                sunColor = Color.Lerp(sunriseColor, noonColor, t);
            }
            else if (_currentHour < 16) // Day
            {
                skyColor = daySkyColor;
                sunColor = noonColor;
            }
            else if (_currentHour < 18) // Afternoon transition
            {
                t = (_currentHour - 16) / 2f;
                skyColor = Color.Lerp(daySkyColor, sunriseSkyColor, t);
                sunColor = Color.Lerp(noonColor, sunsetColor, t);
            }
            else if (_currentHour < 20) // Sunset
            {
                t = (_currentHour - 18) / 2f;
                skyColor = Color.Lerp(sunriseSkyColor, nightSkyColor, t);
                sunColor = Color.Lerp(sunsetColor, nightColor, t);
            }
            else // Night
            {
                skyColor = nightSkyColor;
                sunColor = nightColor;
            }
            
            // Apply weather modifications
            if (currentWeather == WeatherType.Cloudy || currentWeather == WeatherType.Overcast)
            {
                skyColor = Color.Lerp(skyColor, Color.gray, cloudCoverage);
                sunColor = Color.Lerp(sunColor, Color.gray, cloudCoverage * 0.5f);
            }
            else if (currentWeather == WeatherType.Rain)
            {
                skyColor = Color.Lerp(skyColor, new Color(0.3f, 0.35f, 0.4f), 0.7f);
            }
            
            // Apply colors
            sunLight.color = sunColor;
            RenderSettings.ambientSkyColor = skyColor;
            RenderSettings.ambientEquatorColor = Color.Lerp(skyColor, Color.white, 0.3f);
            RenderSettings.ambientGroundColor = Color.Lerp(skyColor, Color.black, 0.5f);
            RenderSettings.fogColor = Color.Lerp(skyColor, Color.white, 0.2f);
            
            // Update camera background
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = skyColor;
            }
        }
        
        private void GenerateClouds()
        {
            _cloudParent = new GameObject("Clouds");
            _cloudParent.transform.SetParent(transform);
            
            for (int i = 0; i < cloudCount; i++)
            {
                CreateCloud(
                    new Vector3(
                        UnityEngine.Random.Range(-cloudSpread, cloudSpread),
                        cloudHeight + UnityEngine.Random.Range(-20f, 20f),
                        UnityEngine.Random.Range(-cloudSpread, cloudSpread)
                    )
                );
            }
        }
        
        private void CreateCloud(Vector3 position)
        {
            GameObject cloud = new GameObject("Cloud");
            cloud.transform.SetParent(_cloudParent.transform);
            cloud.transform.position = position;
            
            // Create cloud using multiple spheres
            int puffCount = UnityEngine.Random.Range(3, 7);
            
            for (int i = 0; i < puffCount; i++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.transform.SetParent(cloud.transform);
                
                // Remove collider
                var collider = puff.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                
                // Random position within cloud
                puff.transform.localPosition = new Vector3(
                    UnityEngine.Random.Range(-15f, 15f),
                    UnityEngine.Random.Range(-3f, 3f),
                    UnityEngine.Random.Range(-10f, 10f)
                );
                
                // Random scale
                float scale = UnityEngine.Random.Range(8f, 20f);
                puff.transform.localScale = new Vector3(scale, scale * 0.6f, scale);
                
                // Cloud material
                var renderer = puff.GetComponent<Renderer>();
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    renderer.material = new Material(shader);
                    renderer.material.color = new Color(1f, 1f, 1f, 0.9f);
                    renderer.material.SetFloat("_Smoothness", 0f);
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
            
            // Add slow drift
            cloud.AddComponent<CloudDrift>();
        }
        
        private void SetupWeatherEffects()
        {
            // Rain particles
            GameObject rainObj = new GameObject("Rain");
            rainObj.transform.SetParent(transform);
            _rainSystem = rainObj.AddComponent<ParticleSystem>();
            
            var main = _rainSystem.main;
            main.loop = true;
            main.startLifetime = 2f;
            main.startSpeed = 25f;
            main.startSize = 0.1f;
            main.maxParticles = 5000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = _rainSystem.emission;
            emission.rateOverTime = 0;
            
            var shape = _rainSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(100, 1, 100);
            shape.position = new Vector3(0, 50, 0);
            
            var renderer = rainObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = new Color(0.7f, 0.7f, 0.8f, 0.5f);
            
            _rainSystem.Stop();
        }
        
        private void UpdateWeather()
        {
            if (_rainSystem == null) return;
            
            var emission = _rainSystem.emission;
            
            switch (currentWeather)
            {
                case WeatherType.Rain:
                    emission.rateOverTime = 2000;
                    if (!_rainSystem.isPlaying) _rainSystem.Play();
                    RenderSettings.fogDensity = 0.008f;
                    break;
                    
                case WeatherType.Fog:
                    emission.rateOverTime = 0;
                    _rainSystem.Stop();
                    RenderSettings.fogDensity = 0.02f;
                    break;
                    
                default:
                    emission.rateOverTime = 0;
                    _rainSystem.Stop();
                    RenderSettings.fogDensity = 0.002f;
                    break;
            }
        }
        
        /// <summary>
        /// Set weather from external source (e.g., weather API)
        /// </summary>
        public void SetWeather(WeatherType weather, float clouds = 0.3f)
        {
            currentWeather = weather;
            cloudCoverage = clouds;
        }
        
        /// <summary>
        /// Set location for sun calculations
        /// </summary>
        public void SetLocation(double lat, double lon)
        {
            latitude = lat;
            longitude = lon;
        }
    }
    
    /// <summary>
    /// Simple cloud drift behavior
    /// </summary>
    public class CloudDrift : MonoBehaviour
    {
        public float speed = 2f;
        public float bounds = 600f;
        
        private Vector3 _direction;
        
        private void Start()
        {
            _direction = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                0,
                UnityEngine.Random.Range(-1f, 1f)
            ).normalized;
            
            speed = UnityEngine.Random.Range(1f, 3f);
        }
        
        private void Update()
        {
            transform.position += _direction * speed * Time.deltaTime;
            
            // Wrap around
            if (transform.position.x > bounds) transform.position -= new Vector3(bounds * 2, 0, 0);
            if (transform.position.x < -bounds) transform.position += new Vector3(bounds * 2, 0, 0);
            if (transform.position.z > bounds) transform.position -= new Vector3(0, 0, bounds * 2);
            if (transform.position.z < -bounds) transform.position += new Vector3(0, 0, bounds * 2);
        }
    }
}
