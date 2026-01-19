using Camera = UnityEngine.Camera;
// ============================================================================
// APEX CITADELS - AAA VISUAL EFFECTS SYSTEM
// Post-processing, particles, and cinematic effects for AAA quality
// ============================================================================
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.Environment
{
    /// <summary>
    /// Manages all AAA visual effects including post-processing,
    /// particle systems, and cinematic enhancements.
    /// </summary>
    public class AAAVisualEffects : MonoBehaviour
    {
        public static AAAVisualEffects Instance { get; private set; }

        [Header("Post-Processing")]
        [SerializeField] private bool enablePostProcessing = true;
        [SerializeField] private float bloomIntensity = 0.8f;
        [SerializeField] private float bloomThreshold = 1.1f;
        [SerializeField] private float vignetteIntensity = 0.25f;
        [SerializeField] private float colorGradingSaturation = 10f;
        [SerializeField] private float colorGradingContrast = 10f;

        [Header("Ambient Particles")]
        [SerializeField] private bool enableAmbientParticles = true;
        [SerializeField] private int dustParticleCount = 500;
        [SerializeField] private float dustAreaSize = 200f;

        [Header("Volumetric Effects")]
        [SerializeField] private bool enableVolumetricFog = true;
        [SerializeField] private Color fogColor = new Color(0.7f, 0.8f, 0.9f, 0.3f);

        [Header("God Rays")]
        [SerializeField] private bool enableGodRays = true;
        [SerializeField] private float godRayIntensity = 0.5f;

        // Components
        private Volume _postProcessVolume;
        private VolumeProfile _volumeProfile;
        private GameObject _dustParticles;
        private GameObject _godRays;
        private List<GameObject> _cloudObjects = new List<GameObject>();

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
            StartCoroutine(InitializeEffects());
        }

        private IEnumerator InitializeEffects()
        {
            Debug.Log("[AAA Effects] Initializing visual effects...");

            // Setup post-processing
            if (enablePostProcessing)
            {
                SetupPostProcessing();
                yield return null;
            }

            // Create ambient dust particles
            if (enableAmbientParticles)
            {
                CreateAmbientDust();
                yield return null;
            }

            // Create volumetric fog effect
            if (enableVolumetricFog)
            {
                CreateVolumetricFog();
                yield return null;
            }

            // Create god rays
            if (enableGodRays)
            {
                CreateGodRays();
                yield return null;
            }

            // Create floating clouds
            CreateFloatingClouds();
            yield return null;

            Debug.Log("[AAA Effects] Visual effects initialization complete!");
        }

        #region Post-Processing

        private void SetupPostProcessing()
        {
            // Check if volume already exists
            _postProcessVolume = FindFirstObjectByType<Volume>();
            
            if (_postProcessVolume == null)
            {
                GameObject volumeObj = new GameObject("PostProcessVolume");
                volumeObj.transform.parent = transform;
                _postProcessVolume = volumeObj.AddComponent<Volume>();
                _postProcessVolume.isGlobal = true;
                _postProcessVolume.priority = 1;
            }

            // Create or get volume profile
            if (_postProcessVolume.profile == null)
            {
                _volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                _postProcessVolume.profile = _volumeProfile;
            }
            else
            {
                _volumeProfile = _postProcessVolume.profile;
            }

            // Add Bloom
            if (!_volumeProfile.Has<Bloom>())
            {
                var bloom = _volumeProfile.Add<Bloom>(true);
                bloom.active = true;
                bloom.threshold.Override(bloomThreshold);
                bloom.intensity.Override(bloomIntensity);
                bloom.scatter.Override(0.7f);
                bloom.tint.Override(new Color(1f, 0.95f, 0.9f));
                Debug.Log("[AAA Effects] Bloom added");
            }

            // Add Vignette
            if (!_volumeProfile.Has<Vignette>())
            {
                var vignette = _volumeProfile.Add<Vignette>(true);
                vignette.active = true;
                vignette.intensity.Override(vignetteIntensity);
                vignette.smoothness.Override(0.4f);
                vignette.color.Override(new Color(0f, 0f, 0f));
                Debug.Log("[AAA Effects] Vignette added");
            }

            // Add Color Adjustments (Color Grading)
            if (!_volumeProfile.Has<ColorAdjustments>())
            {
                var colorAdj = _volumeProfile.Add<ColorAdjustments>(true);
                colorAdj.active = true;
                colorAdj.saturation.Override(colorGradingSaturation);
                colorAdj.contrast.Override(colorGradingContrast);
                colorAdj.postExposure.Override(0.2f);
                Debug.Log("[AAA Effects] Color adjustments added");
            }

            // Add Lift Gamma Gain for cinematic color
            if (!_volumeProfile.Has<LiftGammaGain>())
            {
                var lgg = _volumeProfile.Add<LiftGammaGain>(true);
                lgg.active = true;
                // Slight blue in shadows, warm in highlights
                lgg.lift.Override(new Vector4(0.95f, 0.95f, 1.05f, 0f));
                lgg.gamma.Override(new Vector4(1f, 1f, 1f, 0f));
                lgg.gain.Override(new Vector4(1.05f, 1.02f, 0.98f, 0f));
                Debug.Log("[AAA Effects] Lift/Gamma/Gain added");
            }

            // Add Film Grain (subtle)
            if (!_volumeProfile.Has<FilmGrain>())
            {
                var grain = _volumeProfile.Add<FilmGrain>(true);
                grain.active = true;
                grain.type.Override(FilmGrainLookup.Medium1);
                grain.intensity.Override(0.15f);
                grain.response.Override(0.8f);
                Debug.Log("[AAA Effects] Film grain added");
            }

            // Add Chromatic Aberration (very subtle)
            if (!_volumeProfile.Has<ChromaticAberration>())
            {
                var ca = _volumeProfile.Add<ChromaticAberration>(true);
                ca.active = true;
                ca.intensity.Override(0.05f);
                Debug.Log("[AAA Effects] Chromatic aberration added");
            }

            // Add Depth of Field (subtle)
            if (!_volumeProfile.Has<DepthOfField>())
            {
                var dof = _volumeProfile.Add<DepthOfField>(true);
                dof.active = true;
                dof.mode.Override(DepthOfFieldMode.Bokeh);
                dof.focusDistance.Override(100f);
                dof.aperture.Override(5.6f);
                dof.focalLength.Override(50f);
                Debug.Log("[AAA Effects] Depth of field added");
            }

            Debug.Log("[AAA Effects] Post-processing setup complete");
        }

        #endregion

        #region Ambient Particles

        private void CreateAmbientDust()
        {
            _dustParticles = new GameObject("AmbientDust");
            _dustParticles.transform.parent = transform;
            _dustParticles.transform.localPosition = Vector3.zero;

            ParticleSystem ps = _dustParticles.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.maxParticles = dustParticleCount;
            main.startLifetime = 15f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startColor = new Color(1f, 1f, 0.9f, 0.15f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.loop = true;

            var emission = ps.emission;
            emission.rateOverTime = dustParticleCount / 15f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(dustAreaSize, 100f, dustAreaSize);

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.2f, 0.3f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.15f, 0.3f), new GradientAlphaKey(0.15f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            // Create material for particles
            var renderer = _dustParticles.GetComponent<ParticleSystemRenderer>();
            Material dustMat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (dustMat.shader == null || dustMat.shader.name == "Hidden/InternalErrorShader")
            {
                dustMat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
            }
            dustMat.SetColor("_Color", new Color(1f, 1f, 0.9f, 0.3f));
            renderer.material = dustMat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Attach to camera
            FollowCamera dustFollow = _dustParticles.AddComponent<FollowCamera>();
            dustFollow.offset = new Vector3(0, 30f, 0);

            Debug.Log("[AAA Effects] Ambient dust particles created");
        }

        #endregion

        #region Volumetric Fog

        private void CreateVolumetricFog()
        {
            // Create multiple fog planes at different heights for depth effect
            for (int i = 0; i < 3; i++)
            {
                GameObject fogPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                fogPlane.name = $"FogPlane_{i}";
                fogPlane.transform.parent = transform;
                
                float height = 5f + i * 15f;
                fogPlane.transform.localPosition = new Vector3(0, height, 0);
                fogPlane.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                fogPlane.transform.localScale = new Vector3(800f, 800f, 1f);

                Destroy(fogPlane.GetComponent<Collider>());

                // Create transparent fog material
                Material fogMat = new Material(Shader.Find("Sprites/Default"));
                Color layerColor = fogColor;
                layerColor.a = 0.03f - i * 0.008f; // Fade with height
                fogMat.color = layerColor;
                fogMat.renderQueue = 3000 + i;
                
                fogPlane.GetComponent<Renderer>().material = fogMat;
            }

            Debug.Log("[AAA Effects] Volumetric fog layers created");
        }

        #endregion

        #region God Rays

        private void CreateGodRays()
        {
            // Create radial light shafts emanating from sun direction
            _godRays = new GameObject("GodRays");
            _godRays.transform.parent = transform;
            
            ParticleSystem ps = _godRays.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.maxParticles = 50;
            main.startLifetime = 3f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(20f, 40f);
            main.startColor = new Color(1f, 0.95f, 0.8f, 0.08f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 15f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 5f;
            shape.length = 200f;
            shape.position = new Vector3(0, 300f, -200f);
            shape.rotation = new Vector3(60f, 0f, 0f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0f);
            sizeCurve.AddKey(0.2f, 1f);
            sizeCurve.AddKey(0.8f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.9f, 0.7f), 0f), new GradientColorKey(new Color(1f, 1f, 0.9f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(godRayIntensity * 0.1f, 0.3f), new GradientAlphaKey(godRayIntensity * 0.1f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var renderer = _godRays.GetComponent<ParticleSystemRenderer>();
            Material rayMat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (rayMat.shader == null) rayMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            rayMat.SetColor("_Color", new Color(1f, 0.95f, 0.8f, 0.1f));
            renderer.material = rayMat;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 10f;

            Debug.Log("[AAA Effects] God rays created");
        }

        #endregion

        #region Floating Clouds

        private void CreateFloatingClouds()
        {
            int cloudCount = 15;
            
            for (int i = 0; i < cloudCount; i++)
            {
                GameObject cloud = CreateCloudObject(i);
                _cloudObjects.Add(cloud);
            }

            Debug.Log($"[AAA Effects] Created {cloudCount} floating clouds");
        }

        private GameObject CreateCloudObject(int index)
        {
            GameObject cloud = new GameObject($"Cloud_{index}");
            cloud.transform.parent = transform;

            // Random position in sky
            float angle = index * (360f / 15f) * Mathf.Deg2Rad;
            float radius = Random.Range(150f, 400f);
            float x = Mathf.Cos(angle) * radius + Random.Range(-50f, 50f);
            float z = Mathf.Sin(angle) * radius + Random.Range(-50f, 50f);
            float y = Random.Range(80f, 150f);
            
            cloud.transform.localPosition = new Vector3(x, y, z);

            // Create cloud from multiple spheres
            int puffCount = Random.Range(4, 8);
            Material cloudMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (cloudMat.shader == null || cloudMat.shader.name == "Hidden/InternalErrorShader")
            {
                cloudMat = new Material(Shader.Find("Standard"));
            }
            
            // Make cloud semi-transparent white
            cloudMat.SetFloat("_Surface", 1); // Transparent
            cloudMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.7f));
            cloudMat.SetFloat("_Smoothness", 0.1f);
            cloudMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            cloudMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            cloudMat.renderQueue = 3000;

            for (int j = 0; j < puffCount; j++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "CloudPuff";
                puff.transform.parent = cloud.transform;
                
                Vector3 puffPos = new Vector3(
                    Random.Range(-10f, 10f),
                    Random.Range(-3f, 3f),
                    Random.Range(-10f, 10f)
                );
                puff.transform.localPosition = puffPos;
                
                float puffScale = Random.Range(8f, 20f);
                puff.transform.localScale = new Vector3(puffScale, puffScale * 0.6f, puffScale);
                
                puff.GetComponent<Renderer>().material = cloudMat;
                Destroy(puff.GetComponent<Collider>());
            }

            // Add cloud movement
            CloudMover mover = cloud.AddComponent<CloudMover>();
            mover.speed = Random.Range(1f, 3f);
            mover.direction = new Vector3(1f, 0f, 0.3f).normalized;

            return cloud;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set bloom intensity
        /// </summary>
        public void SetBloomIntensity(float intensity)
        {
            bloomIntensity = intensity;
            if (_volumeProfile != null && _volumeProfile.TryGet<Bloom>(out var bloom))
            {
                bloom.intensity.Override(intensity);
            }
        }

        /// <summary>
        /// Set vignette intensity
        /// </summary>
        public void SetVignetteIntensity(float intensity)
        {
            vignetteIntensity = intensity;
            if (_volumeProfile != null && _volumeProfile.TryGet<Vignette>(out var vignette))
            {
                vignette.intensity.Override(intensity);
            }
        }

        /// <summary>
        /// Toggle ambient particles
        /// </summary>
        public void SetAmbientParticles(bool enabled)
        {
            enableAmbientParticles = enabled;
            if (_dustParticles != null)
            {
                _dustParticles.SetActive(enabled);
            }
        }

        /// <summary>
        /// Set time of day effects (warm sunset, cool night, etc.)
        /// </summary>
        public void SetTimeOfDayEffects(float timeOfDay)
        {
            if (_volumeProfile == null) return;

            // Adjust color grading based on time
            if (_volumeProfile.TryGet<ColorAdjustments>(out var colorAdj))
            {
                if (timeOfDay < 6f || timeOfDay >= 20f) // Night
                {
                    colorAdj.saturation.Override(-10f);
                    colorAdj.colorFilter.Override(new Color(0.7f, 0.75f, 1f));
                }
                else if (timeOfDay < 8f || timeOfDay >= 18f) // Dawn/Dusk
                {
                    colorAdj.saturation.Override(20f);
                    colorAdj.colorFilter.Override(new Color(1f, 0.9f, 0.8f));
                }
                else // Day
                {
                    colorAdj.saturation.Override(colorGradingSaturation);
                    colorAdj.colorFilter.Override(Color.white);
                }
            }

            // Adjust bloom for time of day
            if (_volumeProfile.TryGet<Bloom>(out var bloom))
            {
                float timeBloom = (timeOfDay < 8f || timeOfDay >= 17f) ? bloomIntensity * 1.5f : bloomIntensity;
                bloom.intensity.Override(timeBloom);
            }
        }

        #endregion
    }

    /// <summary>
    /// Simple component to follow camera position
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        public Vector3 offset = Vector3.zero;
        
        private void LateUpdate()
        {
            if (Camera.main != null)
            {
                transform.position = Camera.main.transform.position + offset;
            }
        }
    }

    /// <summary>
    /// Moves clouds slowly across the sky
    /// </summary>
    public class CloudMover : MonoBehaviour
    {
        public float speed = 2f;
        public Vector3 direction = Vector3.right;
        public float maxDistance = 500f;

        private Vector3 _startPos;

        private void Start()
        {
            _startPos = transform.position;
        }

        private void Update()
        {
            transform.position += direction * speed * Time.deltaTime;
            
            // Wrap around
            if (Vector3.Distance(transform.position, _startPos) > maxDistance)
            {
                transform.position = _startPos - direction * maxDistance * 0.5f;
            }
        }
    }
}
