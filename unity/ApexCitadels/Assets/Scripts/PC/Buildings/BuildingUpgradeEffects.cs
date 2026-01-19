using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApexCitadels.PC.Buildings
{
    /// <summary>
    /// Building Upgrade Visual Effects for PC client.
    /// Handles visual transformation during upgrades:
    /// - Scaffolding appearance
    /// - Worker activity particles
    /// - Progress indicators
    /// - Transformation effects
    /// </summary>
    public class BuildingUpgradeEffects : MonoBehaviour
    {
        [Header("Scaffolding")]
        [SerializeField] private bool showScaffolding = true;
        [SerializeField] private Color scaffoldingColor = new Color(0.6f, 0.5f, 0.3f);
        [SerializeField] private float scaffoldingHeight = 1.5f;
        [SerializeField] private float poleSpacing = 2f;
        
        [Header("Worker Effects")]
        [SerializeField] private bool showWorkerActivity = true;
        [SerializeField] private int maxWorkerParticles = 30;
        [SerializeField] private Color hammerSparkColor = new Color(1f, 0.8f, 0.4f);
        [SerializeField] private float activityRate = 5f;
        
        [Header("Progress Indicator")]
        [SerializeField] private bool showProgressRing = true;
        [SerializeField] private Color progressColorStart = new Color(0.3f, 0.3f, 1f);
        [SerializeField] private Color progressColorEnd = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private float ringRadius = 2f;
        [SerializeField] private float ringHeight = 0.5f;
        
        [Header("Transformation")]
        [SerializeField] private float transformDuration = 2f;
        [SerializeField] private Color glowColor = new Color(0.5f, 0.7f, 1f, 0.5f);
        [SerializeField] private float glowIntensity = 2f;
        
        [Header("Sound Integration")]
        [SerializeField] private AudioClip[] constructionClips;
        [SerializeField] private AudioClip upgradeCompleteClip;
        
        // Singleton
        private static BuildingUpgradeEffects _instance;
        public static BuildingUpgradeEffects Instance => _instance;
        
        // Active upgrades
        private Dictionary<string, UpgradeEffectData> _activeUpgrades = new Dictionary<string, UpgradeEffectData>();
        
        // Events
        public event Action<string> OnUpgradeVisualsComplete;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        private void Update()
        {
            foreach (var kvp in _activeUpgrades)
            {
                UpdateUpgradeEffects(kvp.Value);
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Start upgrade visuals for a building
        /// </summary>
        public void StartUpgrade(string id, GameObject building, float duration)
        {
            if (_activeUpgrades.ContainsKey(id))
            {
                CancelUpgrade(id);
            }
            
            var data = new UpgradeEffectData
            {
                Id = id,
                Building = building,
                Duration = duration,
                StartTime = Time.time,
                Progress = 0,
                State = UpgradeState.Starting
            };
            
            // Cache original state
            CacheOriginalState(data);
            
            // Create visual elements
            if (showScaffolding)
            {
                CreateScaffolding(data);
            }
            
            if (showWorkerActivity)
            {
                CreateWorkerActivity(data);
            }
            
            if (showProgressRing)
            {
                CreateProgressRing(data);
            }
            
            _activeUpgrades[id] = data;
            
            // Start construction sounds
            if (constructionClips != null && constructionClips.Length > 0)
            {
                data.AudioSource = data.Building.AddComponent<AudioSource>();
                data.AudioSource.clip = constructionClips[UnityEngine.Random.Range(0, constructionClips.Length)];
                data.AudioSource.loop = true;
                data.AudioSource.volume = 0.3f;
                data.AudioSource.spatialBlend = 1f;
                data.AudioSource.Play();
            }
        }
        
        /// <summary>
        /// Update upgrade progress (0-1)
        /// </summary>
        public void UpdateProgress(string id, float progress)
        {
            if (_activeUpgrades.TryGetValue(id, out var data))
            {
                data.Progress = Mathf.Clamp01(progress);
                
                if (data.Progress >= 1f && data.State != UpgradeState.Completing)
                {
                    data.State = UpgradeState.Completing;
                    CompleteUpgrade(data);
                }
            }
        }
        
        /// <summary>
        /// Cancel upgrade and cleanup visuals
        /// </summary>
        public void CancelUpgrade(string id)
        {
            if (_activeUpgrades.TryGetValue(id, out var data))
            {
                CleanupUpgrade(data);
                _activeUpgrades.Remove(id);
            }
        }
        
        /// <summary>
        /// Complete upgrade with transformation effect
        /// </summary>
        public void CompleteUpgradeNow(string id, GameObject newBuildingPrefab = null)
        {
            if (_activeUpgrades.TryGetValue(id, out var data))
            {
                data.Progress = 1f;
                data.NewBuildingPrefab = newBuildingPrefab;
                CompleteUpgrade(data);
            }
        }
        
        #endregion
        
        #region Visual Elements
        
        private void CreateScaffolding(UpgradeEffectData data)
        {
            var bounds = CalculateBounds(data.Building);
            
            var scaffolding = new GameObject("Scaffolding");
            scaffolding.transform.SetParent(data.Building.transform);
            scaffolding.transform.localPosition = Vector3.zero;
            
            // Create poles at corners and along edges
            CreateScaffoldingPoles(scaffolding.transform, bounds);
            
            // Create horizontal beams
            CreateScaffoldingBeams(scaffolding.transform, bounds);
            
            // Create platforms
            CreateScaffoldingPlatforms(scaffolding.transform, bounds);
            
            data.Scaffolding = scaffolding;
            
            // Animate scaffolding appearance
            StartCoroutine(AnimateScaffoldingIn(scaffolding));
        }
        
        private void CreateScaffoldingPoles(Transform parent, Bounds bounds)
        {
            float minX = -bounds.extents.x - 0.5f;
            float maxX = bounds.extents.x + 0.5f;
            float minZ = -bounds.extents.z - 0.5f;
            float maxZ = bounds.extents.z + 0.5f;
            float height = bounds.size.y + scaffoldingHeight;
            
            // Corner poles
            Vector3[] corners = new Vector3[]
            {
                new Vector3(minX, 0, minZ),
                new Vector3(maxX, 0, minZ),
                new Vector3(minX, 0, maxZ),
                new Vector3(maxX, 0, maxZ)
            };
            
            foreach (var corner in corners)
            {
                CreatePole(parent, corner, height);
            }
            
            // Additional poles along edges
            int polesX = Mathf.CeilToInt((maxX - minX) / poleSpacing);
            int polesZ = Mathf.CeilToInt((maxZ - minZ) / poleSpacing);
            
            for (int i = 1; i < polesX; i++)
            {
                float x = Mathf.Lerp(minX, maxX, (float)i / polesX);
                CreatePole(parent, new Vector3(x, 0, minZ), height);
                CreatePole(parent, new Vector3(x, 0, maxZ), height);
            }
            
            for (int i = 1; i < polesZ; i++)
            {
                float z = Mathf.Lerp(minZ, maxZ, (float)i / polesZ);
                CreatePole(parent, new Vector3(minX, 0, z), height);
                CreatePole(parent, new Vector3(maxX, 0, z), height);
            }
        }
        
        private void CreatePole(Transform parent, Vector3 position, float height)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "ScaffoldPole";
            pole.transform.SetParent(parent);
            pole.transform.localPosition = position + Vector3.up * (height / 2);
            pole.transform.localScale = new Vector3(0.1f, height / 2, 0.1f);
            
            Destroy(pole.GetComponent<Collider>());
            pole.GetComponent<Renderer>().material = CreateScaffoldMaterial();
        }
        
        private void CreateScaffoldingBeams(Transform parent, Bounds bounds)
        {
            float minX = -bounds.extents.x - 0.5f;
            float maxX = bounds.extents.x + 0.5f;
            float minZ = -bounds.extents.z - 0.5f;
            float maxZ = bounds.extents.z + 0.5f;
            float height = bounds.size.y + scaffoldingHeight;
            
            // Horizontal beams at each level
            int levels = Mathf.CeilToInt(height / scaffoldingHeight);
            for (int level = 1; level <= levels; level++)
            {
                float y = level * scaffoldingHeight;
                
                // Front and back beams
                CreateBeam(parent, new Vector3(0, y, minZ), maxX - minX, 0);
                CreateBeam(parent, new Vector3(0, y, maxZ), maxX - minX, 0);
                
                // Left and right beams
                CreateBeam(parent, new Vector3(minX, y, 0), maxZ - minZ, 90);
                CreateBeam(parent, new Vector3(maxX, y, 0), maxZ - minZ, 90);
            }
            
            // Diagonal braces
            CreateDiagonalBraces(parent, bounds);
        }
        
        private void CreateBeam(Transform parent, Vector3 position, float length, float rotationY)
        {
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "ScaffoldBeam";
            beam.transform.SetParent(parent);
            beam.transform.localPosition = position;
            beam.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
            beam.transform.localScale = new Vector3(length, 0.08f, 0.08f);
            
            Destroy(beam.GetComponent<Collider>());
            beam.GetComponent<Renderer>().material = CreateScaffoldMaterial();
        }
        
        private void CreateDiagonalBraces(Transform parent, Bounds bounds)
        {
            float minX = -bounds.extents.x - 0.5f;
            float maxX = bounds.extents.x + 0.5f;
            float minZ = -bounds.extents.z - 0.5f;
            float maxZ = bounds.extents.z + 0.5f;
            float height = bounds.size.y + scaffoldingHeight;
            
            // Add X-braces on each side
            int levels = Mathf.CeilToInt(height / scaffoldingHeight);
            for (int level = 0; level < levels; level++)
            {
                float y1 = level * scaffoldingHeight;
                float y2 = (level + 1) * scaffoldingHeight;
                
                // Front face diagonal
                CreateDiagonal(parent, 
                    new Vector3(minX, y1, minZ), 
                    new Vector3(maxX, y2, minZ));
            }
        }
        
        private void CreateDiagonal(Transform parent, Vector3 start, Vector3 end)
        {
            var diagonal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diagonal.name = "ScaffoldDiagonal";
            diagonal.transform.SetParent(parent);
            
            Vector3 center = (start + end) / 2;
            float length = Vector3.Distance(start, end);
            Vector3 direction = (end - start).normalized;
            
            diagonal.transform.localPosition = center;
            diagonal.transform.localRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0);
            diagonal.transform.localScale = new Vector3(0.05f, length / 2, 0.05f);
            
            Destroy(diagonal.GetComponent<Collider>());
            diagonal.GetComponent<Renderer>().material = CreateScaffoldMaterial();
        }
        
        private void CreateScaffoldingPlatforms(Transform parent, Bounds bounds)
        {
            float minX = -bounds.extents.x - 0.5f;
            float maxX = bounds.extents.x + 0.5f;
            float minZ = -bounds.extents.z - 0.5f;
            float maxZ = bounds.extents.z + 0.5f;
            float height = bounds.size.y + scaffoldingHeight;
            
            // Platform at top
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "ScaffoldPlatform";
            platform.transform.SetParent(parent);
            platform.transform.localPosition = new Vector3(0, height, 0);
            platform.transform.localScale = new Vector3(maxX - minX + 0.5f, 0.1f, maxZ - minZ + 0.5f);
            
            Destroy(platform.GetComponent<Collider>());
            
            var mat = CreateScaffoldMaterial();
            mat.color = new Color(0.5f, 0.4f, 0.3f);
            platform.GetComponent<Renderer>().material = mat;
        }
        
        private Material CreateScaffoldMaterial()
        {
            var mat = new Material(Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
            mat.color = scaffoldingColor;
            return mat;
        }
        
        private System.Collections.IEnumerator AnimateScaffoldingIn(GameObject scaffolding)
        {
            var renderers = scaffolding.GetComponentsInChildren<Renderer>();
            
            // Start invisible
            foreach (var renderer in renderers)
            {
                var mat = renderer.material;
                mat.SetFloat("_Mode", 2); // Fade mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                
                var color = mat.color;
                color.a = 0;
                mat.color = color;
            }
            
            // Fade in over time
            float duration = 1f;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.SmoothStep(0, 1, elapsed / duration);
                
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                    {
                        var color = renderer.material.color;
                        color.a = alpha;
                        renderer.material.color = color;
                    }
                }
                
                yield return null;
            }
        }
        
        private void CreateWorkerActivity(UpgradeEffectData data)
        {
            var bounds = CalculateBounds(data.Building);
            
            var activity = new GameObject("WorkerActivity");
            activity.transform.SetParent(data.Building.transform);
            activity.transform.localPosition = Vector3.up * bounds.extents.y;
            
            var ps = activity.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 2f;
            main.startSize = 0.1f;
            main.startColor = hammerSparkColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxWorkerParticles;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0, 5, 10, 1, activityRate)
            });
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(bounds.size.x, bounds.size.y, bounds.size.z);
            
            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-1f, 1f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-2f, 2f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(hammerSparkColor, 0),
                    new GradientColorKey(hammerSparkColor * 0.5f, 1)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1, 0),
                    new GradientAlphaKey(0, 1)
                }
            );
            colorOverLifetime.color = gradient;
            
            var renderer = activity.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial();
            
            data.WorkerParticles = ps;
        }
        
        private void CreateProgressRing(UpgradeEffectData data)
        {
            var bounds = CalculateBounds(data.Building);
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + ringRadius;
            
            var ring = new GameObject("ProgressRing");
            ring.transform.SetParent(data.Building.transform);
            ring.transform.localPosition = Vector3.up * ringHeight;
            
            // Create ring segments
            int segments = 36;
            for (int i = 0; i < segments; i++)
            {
                var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = $"RingSegment_{i}";
                segment.transform.SetParent(ring.transform);
                
                float angle = (i / (float)segments) * 360f * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                
                segment.transform.localPosition = new Vector3(x, 0, z);
                segment.transform.localRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg + 90, 0);
                segment.transform.localScale = new Vector3(radius * 2 * Mathf.PI / segments * 0.9f, 0.1f, 0.3f);
                
                Destroy(segment.GetComponent<Collider>());
                
                var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
                mat.color = Color.Lerp(progressColorStart, progressColorEnd, i / (float)segments);
                mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.2f);
                segment.GetComponent<Renderer>().material = mat;
            }
            
            data.ProgressRing = ring;
            data.RingSegments = ring.GetComponentsInChildren<Renderer>();
        }
        
        #endregion
        
        #region Update Logic
        
        private void UpdateUpgradeEffects(UpgradeEffectData data)
        {
            if (data.State == UpgradeState.Complete) return;
            
            // Auto-progress if duration set
            if (data.Duration > 0)
            {
                data.Progress = (Time.time - data.StartTime) / data.Duration;
            }
            
            // Update progress ring
            UpdateProgressRing(data);
            
            // Update worker activity intensity based on progress
            UpdateWorkerActivity(data);
            
            // Check completion
            if (data.Progress >= 1f && data.State == UpgradeState.InProgress)
            {
                data.State = UpgradeState.Completing;
                CompleteUpgrade(data);
            }
            else if (data.State == UpgradeState.Starting)
            {
                data.State = UpgradeState.InProgress;
            }
        }
        
        private void UpdateProgressRing(UpgradeEffectData data)
        {
            if (data.RingSegments == null) return;
            
            int activeSegments = Mathf.FloorToInt(data.Progress * data.RingSegments.Length);
            
            for (int i = 0; i < data.RingSegments.Length; i++)
            {
                if (data.RingSegments[i] == null) continue;
                
                var mat = data.RingSegments[i].material;
                
                if (i < activeSegments)
                {
                    // Filled segment
                    Color color = Color.Lerp(progressColorStart, progressColorEnd, i / (float)data.RingSegments.Length);
                    color.a = 0.8f;
                    mat.color = color;
                }
                else
                {
                    // Unfilled segment
                    Color color = Color.Lerp(progressColorStart, progressColorEnd, i / (float)data.RingSegments.Length);
                    color.a = 0.2f;
                    mat.color = color;
                }
            }
            
            // Rotate ring slowly
            data.ProgressRing.transform.Rotate(Vector3.up, Time.deltaTime * 10f);
        }
        
        private void UpdateWorkerActivity(UpgradeEffectData data)
        {
            if (data.WorkerParticles == null) return;
            
            var emission = data.WorkerParticles.emission;
            
            // More activity in middle of construction
            float activityMultiplier = Mathf.Sin(data.Progress * Mathf.PI);
            
            var burst = emission.GetBurst(0);
            burst.count = new ParticleSystem.MinMaxCurve(5 + activityMultiplier * 10);
            emission.SetBurst(0, burst);
        }
        
        #endregion
        
        #region Completion
        
        private void CompleteUpgrade(UpgradeEffectData data)
        {
            StartCoroutine(UpgradeCompleteSequence(data));
        }
        
        private System.Collections.IEnumerator UpgradeCompleteSequence(UpgradeEffectData data)
        {
            // Stop construction sounds
            if (data.AudioSource != null)
            {
                data.AudioSource.Stop();
            }
            
            // Flash effect
            yield return StartCoroutine(PlayCompletionFlash(data));
            
            // Fade out scaffolding
            if (data.Scaffolding != null)
            {
                yield return StartCoroutine(AnimateScaffoldingOut(data.Scaffolding));
            }
            
            // Transform to new building if provided
            if (data.NewBuildingPrefab != null)
            {
                yield return StartCoroutine(TransformBuilding(data));
            }
            
            // Play completion sound
            if (upgradeCompleteClip != null)
            {
                AudioSource.PlayClipAtPoint(upgradeCompleteClip, data.Building.transform.position);
            }
            
            // Cleanup
            CleanupUpgrade(data);
            data.State = UpgradeState.Complete;
            
            OnUpgradeVisualsComplete?.Invoke(data.Id);
            _activeUpgrades.Remove(data.Id);
        }
        
        private System.Collections.IEnumerator PlayCompletionFlash(UpgradeEffectData data)
        {
            // Create glow effect
            var bounds = CalculateBounds(data.Building);
            
            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "CompletionGlow";
            glow.transform.position = data.Building.transform.position + Vector3.up * bounds.extents.y;
            glow.transform.localScale = Vector3.one * bounds.size.magnitude;
            
            Destroy(glow.GetComponent<Collider>());
            
            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
            mat.color = glowColor;
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", glowColor * glowIntensity);
            glow.GetComponent<Renderer>().material = mat;
            
            // Add light
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = glowColor;
            light.intensity = glowIntensity * 3;
            light.range = bounds.size.magnitude * 2;
            
            // Flash animation
            float duration = 0.5f;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Expand and fade
                float scale = 1f + t * 0.5f;
                glow.transform.localScale = Vector3.one * bounds.size.magnitude * scale;
                
                float alpha = 1f - t;
                var color = mat.color;
                color.a = glowColor.a * alpha;
                mat.color = color;
                mat.SetColor("_EmissionColor", glowColor * glowIntensity * alpha);
                
                light.intensity = glowIntensity * 3 * alpha;
                
                yield return null;
            }
            
            Destroy(glow);
        }
        
        private System.Collections.IEnumerator AnimateScaffoldingOut(GameObject scaffolding)
        {
            var renderers = scaffolding.GetComponentsInChildren<Renderer>();
            
            float duration = 0.5f;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.SmoothStep(0, 1, elapsed / duration);
                
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                    {
                        var color = renderer.material.color;
                        color.a = alpha;
                        renderer.material.color = color;
                    }
                }
                
                yield return null;
            }
            
            Destroy(scaffolding);
        }
        
        private System.Collections.IEnumerator TransformBuilding(UpgradeEffectData data)
        {
            // Store transform
            Vector3 position = data.Building.transform.position;
            Quaternion rotation = data.Building.transform.rotation;
            Transform parent = data.Building.transform.parent;
            
            // Fade out old building
            var renderers = data.Building.GetComponentsInChildren<Renderer>();
            float duration = transformDuration / 2;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - elapsed / duration;
                
                foreach (var renderer in renderers)
                {
                    SetRendererAlpha(renderer, alpha);
                }
                
                yield return null;
            }
            
            // Instantiate new building
            var newBuilding = Instantiate(data.NewBuildingPrefab, position, rotation, parent);
            
            // Fade in new building
            var newRenderers = newBuilding.GetComponentsInChildren<Renderer>();
            foreach (var renderer in newRenderers)
            {
                SetRendererAlpha(renderer, 0);
            }
            
            elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = elapsed / duration;
                
                foreach (var renderer in newRenderers)
                {
                    SetRendererAlpha(renderer, alpha);
                }
                
                yield return null;
            }
            
            // Destroy old building
            Destroy(data.Building);
            data.Building = newBuilding;
        }
        
        private void SetRendererAlpha(Renderer renderer, float alpha)
        {
            if (renderer == null) return;
            
            foreach (var mat in renderer.materials)
            {
                var color = mat.color;
                color.a = alpha;
                mat.color = color;
            }
        }
        
        #endregion
        
        #region Helpers
        
        private void CacheOriginalState(UpgradeEffectData data)
        {
            data.OriginalMaterials = new Dictionary<Renderer, Material[]>();
            var renderers = data.Building.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                data.OriginalMaterials[renderer] = renderer.sharedMaterials.ToArray();
            }
        }
        
        private void CleanupUpgrade(UpgradeEffectData data)
        {
            if (data.Scaffolding != null)
            {
                Destroy(data.Scaffolding);
            }
            
            if (data.WorkerParticles != null)
            {
                Destroy(data.WorkerParticles.gameObject);
            }
            
            if (data.ProgressRing != null)
            {
                Destroy(data.ProgressRing);
            }
            
            if (data.AudioSource != null)
            {
                Destroy(data.AudioSource);
            }
        }
        
        private Bounds CalculateBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(obj.transform.position, Vector3.one);
            }
            
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            
            // Convert to local bounds
            var center = obj.transform.InverseTransformPoint(bounds.center);
            return new Bounds(center, bounds.size);
        }
        
        private Material CreateParticleMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            
            var mat = new Material(shader);
            mat.SetFloat("_SurfaceType", 1);
            mat.EnableKeyword("_ALPHABLEND_ON");
            return mat;
        }
        
        #endregion
    }
    
    #region Data Types
    
    public enum UpgradeState
    {
        Starting,
        InProgress,
        Completing,
        Complete
    }
    
    public class UpgradeEffectData
    {
        public string Id;
        public GameObject Building;
        public float Duration;
        public float StartTime;
        public float Progress;
        public UpgradeState State;
        
        public Dictionary<Renderer, Material[]> OriginalMaterials;
        
        public GameObject Scaffolding;
        public ParticleSystem WorkerParticles;
        public GameObject ProgressRing;
        public Renderer[] RingSegments;
        public AudioSource AudioSource;
        
        public GameObject NewBuildingPrefab;
    }
    
    #endregion
}
