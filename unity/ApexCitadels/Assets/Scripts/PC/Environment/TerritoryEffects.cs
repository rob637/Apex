// ============================================================================
// APEX CITADELS - TERRITORY EFFECTS SYSTEM
// Magic auras, selection highlights, and ambient territory effects
// ============================================================================
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.Environment
{
    /// <summary>
    /// Adds visual effects to territories: auras, selection rings,
    /// campfires, banners, and ambient particles.
    /// </summary>
    public class TerritoryEffects : MonoBehaviour
    {
        [Header("Aura Settings")]
        [SerializeField] private bool enableAuras = true;
        [SerializeField] private float auraIntensity = 0.5f;
        [SerializeField] private float auraHeight = 30f;
        [SerializeField] private float auraPulseSpeed = 2f;

        [Header("Selection Effects")]
        [SerializeField] private Color selectionColor = new Color(1f, 1f, 0f, 0.8f);
        [SerializeField] private float selectionRingSize = 35f;

        [Header("Campfire Settings")]
        [SerializeField] private bool enableCampfires = true;
        [SerializeField] private int campfiresPerTerritory = 2;

        [Header("Banner Settings")]
        [SerializeField] private bool enableBanners = true;

        // References
        private EnhancedTerritoryVisual _territory;
        private ParticleSystem _auraParticles;
        private ParticleSystem _selectionParticles;
        private List<GameObject> _campfires = new List<GameObject>();
        private List<GameObject> _banners = new List<GameObject>();
        private GameObject _beamEffect;

        // State
        private bool _isSelected = false;
        private float _pulsePhase = 0f;

        private void Start()
        {
            _territory = GetComponent<EnhancedTerritoryVisual>();
            
            if (_territory != null)
            {
                StartCoroutine(SetupEffects());
            }
        }

        private IEnumerator SetupEffects()
        {
            yield return null; // Wait for territory to build

            if (enableAuras)
            {
                CreateAuraEffect();
            }
            
            CreateSelectionEffect();
            
            if (enableCampfires && _territory.Level >= 2)
            {
                CreateCampfires();
            }
            
            if (enableBanners)
            {
                CreateBanners();
            }
            
            CreateVerticalBeam();
        }

        private void Update()
        {
            // Pulse effect
            _pulsePhase += Time.deltaTime * auraPulseSpeed;
            
            if (_auraParticles != null && _auraParticles.isPlaying)
            {
                float pulse = (Mathf.Sin(_pulsePhase) + 1f) * 0.5f;
                var emission = _auraParticles.emission;
                emission.rateOverTime = 20f + pulse * 30f;
            }

            // Selection ring rotation
            if (_isSelected && _selectionParticles != null)
            {
                _selectionParticles.transform.Rotate(0, 30f * Time.deltaTime, 0);
            }
        }

        #region Aura Effect

        private void CreateAuraEffect()
        {
            GameObject auraObj = new GameObject("TerritoryAura");
            auraObj.transform.parent = transform;
            auraObj.transform.localPosition = Vector3.zero;

            _auraParticles = auraObj.AddComponent<ParticleSystem>();
            
            Color auraColor = GetTerritoryColor();
            
            var main = _auraParticles.main;
            main.maxParticles = 200;
            main.startLifetime = 3f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startColor = new Color(auraColor.r, auraColor.g, auraColor.b, 0.3f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.1f; // Float upward

            var emission = _auraParticles.emission;
            emission.rateOverTime = 30f;

            var shape = _auraParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 15f + _territory.Level * 2f;
            shape.radiusThickness = 0.1f;

            var colorOverLifetime = _auraParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(auraColor, 0f), 
                    new GradientColorKey(auraColor, 0.5f),
                    new GradientColorKey(auraColor * 0.5f, 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0f, 0f), 
                    new GradientAlphaKey(auraIntensity * 0.5f, 0.2f), 
                    new GradientAlphaKey(auraIntensity * 0.3f, 0.8f), 
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = _auraParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.5f);
            sizeCurve.AddKey(0.5f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Renderer
            var renderer = auraObj.GetComponent<ParticleSystemRenderer>();
            Material auraMat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (auraMat.shader == null) auraMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            auraMat.SetColor("_Color", auraColor);
            renderer.material = auraMat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        #endregion

        #region Selection Effect

        private void CreateSelectionEffect()
        {
            GameObject selectObj = new GameObject("SelectionRing");
            selectObj.transform.parent = transform;
            selectObj.transform.localPosition = new Vector3(0, 1f, 0);

            _selectionParticles = selectObj.AddComponent<ParticleSystem>();

            var main = _selectionParticles.main;
            main.maxParticles = 100;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 1.5f;
            main.startColor = selectionColor;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = false;

            var emission = _selectionParticles.emission;
            emission.rateOverTime = 60f;

            var shape = _selectionParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = selectionRingSize;
            shape.radiusThickness = 0f;
            shape.arc = 360f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Loop;

            var colorOverLifetime = _selectionParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(selectionColor, 0f), new GradientColorKey(selectionColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var renderer = selectObj.GetComponent<ParticleSystemRenderer>();
            Material selectMat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (selectMat.shader == null) selectMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            selectMat.SetColor("_Color", selectionColor);
            renderer.material = selectMat;

            selectObj.SetActive(false);
        }

        #endregion

        #region Campfires

        private void CreateCampfires()
        {
            float territoryRadius = 12f + _territory.Level * 2f;
            
            for (int i = 0; i < campfiresPerTerritory; i++)
            {
                float angle = (i + 0.5f) * (360f / campfiresPerTerritory) * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * territoryRadius * 0.8f,
                    0,
                    Mathf.Sin(angle) * territoryRadius * 0.8f
                );

                GameObject campfire = CreateCampfire(i, pos);
                _campfires.Add(campfire);
            }
        }

        private GameObject CreateCampfire(int index, Vector3 localPos)
        {
            GameObject campfire = new GameObject($"Campfire_{index}");
            campfire.transform.parent = transform;
            campfire.transform.localPosition = localPos;

            // Fire logs (base)
            for (int i = 0; i < 3; i++)
            {
                GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                log.name = "Log";
                log.transform.parent = campfire.transform;
                log.transform.localPosition = Vector3.zero;
                log.transform.localRotation = Quaternion.Euler(90f, i * 60f, 0);
                log.transform.localScale = new Vector3(0.2f, 0.8f, 0.2f);
                
                Material logMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (logMat.shader == null) logMat = new Material(Shader.Find("Standard"));
                logMat.SetColor("_BaseColor", new Color(0.25f, 0.15f, 0.1f));
                log.GetComponent<Renderer>().material = logMat;
                Destroy(log.GetComponent<Collider>());
            }

            // Fire particles
            GameObject fireObj = new GameObject("FireParticles");
            fireObj.transform.parent = campfire.transform;
            fireObj.transform.localPosition = new Vector3(0, 0.3f, 0);

            ParticleSystem fire = fireObj.AddComponent<ParticleSystem>();
            
            var main = fire.main;
            main.maxParticles = 50;
            main.startLifetime = 0.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startColor = new Color(1f, 0.5f, 0f, 0.8f);
            main.gravityModifier = -0.5f;

            var emission = fire.emission;
            emission.rateOverTime = 30f;

            var shape = fire.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.2f;

            var colorOverLifetime = fire.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f), 
                    new GradientColorKey(new Color(1f, 0.4f, 0f), 0.5f),
                    new GradientColorKey(new Color(0.5f, 0.1f, 0f), 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.8f, 0f), 
                    new GradientAlphaKey(0.5f, 0.5f), 
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            colorOverLifetime.color = gradient;

            var renderer = fireObj.GetComponent<ParticleSystemRenderer>();
            Material fireMat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (fireMat.shader == null) fireMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            renderer.material = fireMat;

            // Point light
            GameObject lightObj = new GameObject("FireLight");
            lightObj.transform.parent = campfire.transform;
            lightObj.transform.localPosition = new Vector3(0, 1f, 0);

            Light fireLight = lightObj.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.6f, 0.2f);
            fireLight.intensity = 1f;
            fireLight.range = 8f;
            fireLight.shadows = LightShadows.None;

            // Add flicker
            lightObj.AddComponent<FireFlicker>();

            return campfire;
        }

        #endregion

        #region Banners

        private void CreateBanners()
        {
            Color bannerColor = GetTerritoryColor();
            float territoryRadius = 12f + _territory.Level * 2f;
            
            // Create banners on walls
            int bannerCount = Mathf.Min(4, _territory.Level);
            
            for (int i = 0; i < bannerCount; i++)
            {
                float angle = i * (360f / bannerCount) * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * territoryRadius,
                    6f + _territory.Level * 0.5f,
                    Mathf.Sin(angle) * territoryRadius
                );

                GameObject banner = CreateBanner(i, pos, bannerColor);
                banner.transform.LookAt(transform.position);
                _banners.Add(banner);
            }
        }

        private GameObject CreateBanner(int index, Vector3 localPos, Color color)
        {
            GameObject banner = new GameObject($"Banner_{index}");
            banner.transform.parent = transform;
            banner.transform.localPosition = localPos;

            // Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.parent = banner.transform;
            pole.transform.localPosition = new Vector3(0, 2f, 0);
            pole.transform.localScale = new Vector3(0.15f, 4f, 0.15f);
            
            Material poleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (poleMat.shader == null) poleMat = new Material(Shader.Find("Standard"));
            poleMat.SetColor("_BaseColor", new Color(0.3f, 0.2f, 0.15f));
            pole.GetComponent<Renderer>().material = poleMat;
            Destroy(pole.GetComponent<Collider>());

            // Banner cloth
            GameObject cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cloth.name = "BannerCloth";
            cloth.transform.parent = banner.transform;
            cloth.transform.localPosition = new Vector3(0.6f, 3f, 0);
            cloth.transform.localScale = new Vector3(1.2f, 1.8f, 0.05f);
            
            Material clothMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (clothMat.shader == null) clothMat = new Material(Shader.Find("Standard"));
            clothMat.SetColor("_BaseColor", color);
            clothMat.EnableKeyword("_EMISSION");
            clothMat.SetColor("_EmissionColor", color * 0.2f);
            cloth.GetComponent<Renderer>().material = clothMat;
            Destroy(cloth.GetComponent<Collider>());

            // Add wave animation
            banner.AddComponent<BannerWave>();

            return banner;
        }

        #endregion

        #region Vertical Beam

        private void CreateVerticalBeam()
        {
            if (_territory.Ownership != TerritoryOwnership.Owned &&
                _territory.Ownership != TerritoryOwnership.Contested)
                return;

            _beamEffect = new GameObject("VerticalBeam");
            _beamEffect.transform.parent = transform;
            _beamEffect.transform.localPosition = Vector3.zero;

            ParticleSystem beam = _beamEffect.AddComponent<ParticleSystem>();
            Color beamColor = GetTerritoryColor();

            var main = beam.main;
            main.maxParticles = 100;
            main.startLifetime = 2f;
            main.startSpeed = 10f;
            main.startSize = new ParticleSystem.MinMaxCurve(1f, 2f);
            main.startColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0.2f);

            var emission = beam.emission;
            emission.rateOverTime = 30f;

            var shape = beam.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 3f;

            var velocityOverLifetime = beam.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(10f, 15f);

            var colorOverLifetime = beam.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(beamColor, 0f), new GradientColorKey(beamColor * 0.5f, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.3f, 0f), new GradientAlphaKey(0.1f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var renderer = _beamEffect.GetComponent<ParticleSystemRenderer>();
            Material beamMat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (beamMat.shader == null) beamMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            beamMat.SetColor("_Color", beamColor);
            renderer.material = beamMat;
        }

        #endregion

        #region Public API

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            
            if (_selectionParticles != null)
            {
                _selectionParticles.gameObject.SetActive(selected);
                if (selected)
                {
                    _selectionParticles.Play();
                }
                else
                {
                    _selectionParticles.Stop();
                }
            }
        }

        private Color GetTerritoryColor()
        {
            if (_territory == null) return Color.gray;

            return _territory.Ownership switch
            {
                TerritoryOwnership.Owned => new Color(0.2f, 0.9f, 0.3f),
                TerritoryOwnership.Alliance => new Color(0.3f, 0.5f, 1f),
                TerritoryOwnership.Enemy => new Color(1f, 0.2f, 0.2f),
                TerritoryOwnership.Contested => new Color(1f, 0.6f, 0.1f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
        }

        #endregion
    }

    /// <summary>
    /// Simple fire light flicker effect
    /// </summary>
    public class FireFlicker : MonoBehaviour
    {
        private Light _light;
        private float _baseIntensity;

        private void Start()
        {
            _light = GetComponent<Light>();
            if (_light != null)
            {
                _baseIntensity = _light.intensity;
            }
        }

        private void Update()
        {
            if (_light != null)
            {
                float flicker = Mathf.PerlinNoise(Time.time * 8f, 0f);
                _light.intensity = _baseIntensity * (0.8f + flicker * 0.4f);
            }
        }
    }

    /// <summary>
    /// Banner waving animation
    /// </summary>
    public class BannerWave : MonoBehaviour
    {
        private Transform _cloth;
        private Vector3 _baseRotation;

        private void Start()
        {
            _cloth = transform.Find("BannerCloth");
            if (_cloth != null)
            {
                _baseRotation = _cloth.localEulerAngles;
            }
        }

        private void Update()
        {
            if (_cloth != null)
            {
                float wave = Mathf.Sin(Time.time * 3f + transform.position.x) * 10f;
                _cloth.localRotation = Quaternion.Euler(_baseRotation.x, _baseRotation.y + wave, _baseRotation.z);
            }
        }
    }
}
