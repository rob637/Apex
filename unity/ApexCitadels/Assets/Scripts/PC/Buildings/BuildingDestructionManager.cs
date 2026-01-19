using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApexCitadels.PC.Buildings
{
    /// <summary>
    /// Building Destruction Manager for PC client.
    /// Handles visually appealing destruction sequences:
    /// - Progressive damage states
    /// - Collapse physics simulation
    /// - Debris and particle effects
    /// - Rubble persistence
    /// </summary>
    public class BuildingDestructionManager : MonoBehaviour
    {
        [Header("Destruction Settings")]
        [SerializeField] private bool enableDestructionPhysics = true;
        [SerializeField] private float collapseSpeed = 2f;
        [SerializeField] private float debrisDuration = 10f;
        [SerializeField] private int maxDebrisPieces = 50;
        
        [Header("Damage Visuals")]
        [SerializeField] private Material damagedMaterial;
        [SerializeField] private Material burntMaterial;
        [SerializeField] private Color damageColor = new Color(0.3f, 0.25f, 0.2f);
        
        [Header("Effects")]
        [SerializeField] private Color smokeColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        [SerializeField] private Color dustColor = new Color(0.6f, 0.55f, 0.5f, 0.4f);
        [SerializeField] private Color fireColor = new Color(1f, 0.5f, 0.1f);
        
        [Header("Sound Integration")]
        [SerializeField] private AudioClip[] collapseClips;
        [SerializeField] private AudioClip[] debrisClips;
        [SerializeField] private AudioClip[] creakClips;
        
        // Singleton
        private static BuildingDestructionManager _instance;
        public static BuildingDestructionManager Instance => _instance;
        
        // Active destructions
        private Dictionary<string, DestructionData> _activeDestructions = new Dictionary<string, DestructionData>();
        
        // Object pools
        private Queue<GameObject> _debrisPool = new Queue<GameObject>();
        private Queue<ParticleSystem> _dustPool = new Queue<ParticleSystem>();
        
        // Events
        public event Action<string> OnDestructionStarted;
        public event Action<string> OnDestructionComplete;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializePools();
        }
        
        private void Update()
        {
            // Update active destructions
            var keys = _activeDestructions.Keys.ToList();
            foreach (var key in keys)
            {
                if (_activeDestructions.TryGetValue(key, out var data))
                {
                    UpdateDestruction(data);
                }
            }
        }
        
        #region Pool Management
        
        private void InitializePools()
        {
            // Pre-create debris pieces
            for (int i = 0; i < 20; i++)
            {
                var debris = CreateDebrisPiece();
                debris.SetActive(false);
                _debrisPool.Enqueue(debris);
            }
            
            // Pre-create dust effects
            for (int i = 0; i < 10; i++)
            {
                var dust = CreateDustEffect();
                dust.gameObject.SetActive(false);
                _dustPool.Enqueue(dust);
            }
        }
        
        private GameObject GetDebrisPiece()
        {
            if (_debrisPool.Count > 0)
            {
                var debris = _debrisPool.Dequeue();
                debris.SetActive(true);
                return debris;
            }
            return CreateDebrisPiece();
        }
        
        private void ReturnDebrisPiece(GameObject debris)
        {
            debris.SetActive(false);
            debris.transform.SetParent(transform);
            _debrisPool.Enqueue(debris);
        }
        
        private ParticleSystem GetDustEffect()
        {
            if (_dustPool.Count > 0)
            {
                var dust = _dustPool.Dequeue();
                dust.gameObject.SetActive(true);
                return dust;
            }
            return CreateDustEffect();
        }
        
        private void ReturnDustEffect(ParticleSystem dust)
        {
            dust.Stop();
            dust.gameObject.SetActive(false);
            dust.transform.SetParent(transform);
            _dustPool.Enqueue(dust);
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Apply damage to a building visually
        /// </summary>
        public void ApplyDamage(string id, GameObject building, float damagePercent)
        {
            if (!_activeDestructions.ContainsKey(id))
            {
                _activeDestructions[id] = new DestructionData
                {
                    Id = id,
                    Building = building,
                    State = DestructionState.Intact,
                    DamagePercent = 0,
                    OriginalMaterials = CacheMaterials(building)
                };
            }
            
            var data = _activeDestructions[id];
            data.DamagePercent = Mathf.Clamp01(damagePercent);
            
            // Update visual state based on damage
            if (data.DamagePercent >= 1f && data.State != DestructionState.Destroyed)
            {
                BeginDestruction(data);
            }
            else if (data.DamagePercent >= 0.7f && data.State == DestructionState.Intact)
            {
                data.State = DestructionState.HeavilyDamaged;
                ApplyHeavyDamageVisuals(data);
            }
            else if (data.DamagePercent >= 0.3f && data.State == DestructionState.Intact)
            {
                data.State = DestructionState.Damaged;
                ApplyDamageVisuals(data);
            }
        }
        
        /// <summary>
        /// Immediately destroy a building
        /// </summary>
        public void DestroyBuilding(string id, GameObject building, DestructionStyle style = DestructionStyle.Collapse)
        {
            if (!_activeDestructions.ContainsKey(id))
            {
                _activeDestructions[id] = new DestructionData
                {
                    Id = id,
                    Building = building,
                    State = DestructionState.Intact,
                    DamagePercent = 1f,
                    OriginalMaterials = CacheMaterials(building)
                };
            }
            
            var data = _activeDestructions[id];
            data.DamagePercent = 1f;
            data.Style = style;
            
            BeginDestruction(data);
        }
        
        /// <summary>
        /// Reset building to undamaged state
        /// </summary>
        public void RepairBuilding(string id)
        {
            if (_activeDestructions.TryGetValue(id, out var data))
            {
                // Restore original materials
                RestoreMaterials(data);
                
                // Clean up effects
                CleanupEffects(data);
                
                _activeDestructions.Remove(id);
            }
        }
        
        #endregion
        
        #region Damage Visuals
        
        private void ApplyDamageVisuals(DestructionData data)
        {
            var renderers = data.Building.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    var mat = renderer.materials[i];
                    mat.color = Color.Lerp(mat.color, damageColor, 0.3f);
                }
            }
            
            // Add light smoke
            var bounds = CalculateBounds(data.Building);
            var smoke = CreateSmokeEffect(data.Building.transform, 
                Vector3.up * bounds.extents.y, 
                smokeColor, 5f);
            data.ActiveEffects.Add(smoke);
            
            // Add damage cracks (if we had crack decals)
            AddDamageCracks(data, 3);
        }
        
        private void ApplyHeavyDamageVisuals(DestructionData data)
        {
            var renderers = data.Building.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    var mat = renderer.materials[i];
                    mat.color = Color.Lerp(mat.color, damageColor, 0.6f);
                }
            }
            
            // Add more smoke
            var bounds = CalculateBounds(data.Building);
            var smoke = CreateSmokeEffect(data.Building.transform,
                Vector3.up * bounds.extents.y * 0.5f,
                new Color(0.15f, 0.15f, 0.15f, 0.7f), 15f);
            data.ActiveEffects.Add(smoke);
            
            // Add fire
            if (data.Style == DestructionStyle.Fire)
            {
                var fire = CreateFireEffect(data.Building.transform, Vector3.up * bounds.extents.y * 0.3f);
                data.ActiveEffects.Add(fire);
            }
            
            // More cracks
            AddDamageCracks(data, 5);
            
            // Play creaking sound
            PlaySound(creakClips, data.Building.transform.position);
        }
        
        private void AddDamageCracks(DestructionData data, int count)
        {
            var bounds = CalculateBounds(data.Building);
            
            for (int i = 0; i < count; i++)
            {
                var crack = GameObject.CreatePrimitive(PrimitiveType.Quad);
                crack.name = "DamageCrack";
                crack.transform.SetParent(data.Building.transform);
                
                // Random position on building surface
                Vector3 localPos = new Vector3(
                    UnityEngine.Random.Range(-bounds.extents.x, bounds.extents.x),
                    UnityEngine.Random.Range(0, bounds.size.y),
                    bounds.extents.z + 0.01f
                );
                
                crack.transform.localPosition = localPos;
                crack.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.value > 0.5f ? 0 : 180, UnityEngine.Random.Range(-15f, 15f));
                crack.transform.localScale = new Vector3(
                    UnityEngine.Random.Range(0.3f, 0.8f),
                    UnityEngine.Random.Range(0.3f, 0.8f),
                    1f
                );
                
                Destroy(crack.GetComponent<Collider>());
                
                var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
                mat.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                crack.GetComponent<Renderer>().material = mat;
                
                data.CrackObjects.Add(crack);
            }
        }
        
        #endregion
        
        #region Destruction Sequence
        
        private void BeginDestruction(DestructionData data)
        {
            data.State = DestructionState.Collapsing;
            data.CollapseStartTime = Time.time;
            
            OnDestructionStarted?.Invoke(data.Id);
            
            // Play collapse sound
            PlaySound(collapseClips, data.Building.transform.position);
            
            // Add heavy dust cloud
            var bounds = CalculateBounds(data.Building);
            var dust = GetDustEffect();
            dust.transform.position = data.Building.transform.position;
            var shape = dust.shape;
            shape.scale = new Vector3(bounds.size.x * 2, 0.5f, bounds.size.z * 2);
            dust.Play();
            data.ActiveEffects.Add(dust);
            
            // Based on destruction style
            switch (data.Style)
            {
                case DestructionStyle.Collapse:
                    StartCollapseSequence(data);
                    break;
                case DestructionStyle.Explosion:
                    StartExplosionSequence(data);
                    break;
                case DestructionStyle.Fire:
                    StartFireSequence(data);
                    break;
                case DestructionStyle.Crumble:
                    StartCrumbleSequence(data);
                    break;
            }
        }
        
        private void StartCollapseSequence(DestructionData data)
        {
            if (!enableDestructionPhysics)
            {
                // Simple fade out
                data.FadeOut = true;
                return;
            }
            
            // Break building into chunks
            var bounds = CalculateBounds(data.Building);
            int chunkCountX = Mathf.CeilToInt(bounds.size.x / 2);
            int chunkCountY = Mathf.CeilToInt(bounds.size.y / 2);
            int chunkCountZ = Mathf.CeilToInt(bounds.size.z / 2);
            
            // Hide original
            SetBuildingVisible(data.Building, false);
            
            // Create chunks
            for (int x = 0; x < chunkCountX; x++)
            {
                for (int y = 0; y < chunkCountY; y++)
                {
                    for (int z = 0; z < chunkCountZ; z++)
                    {
                        if (data.DebrisPieces.Count >= maxDebrisPieces) break;
                        
                        var chunk = GetDebrisPiece();
                        
                        Vector3 offset = new Vector3(
                            (x - chunkCountX / 2f + 0.5f) * 2,
                            y * 2 + 1,
                            (z - chunkCountZ / 2f + 0.5f) * 2
                        );
                        
                        chunk.transform.position = data.Building.transform.position + offset;
                        chunk.transform.localScale = Vector3.one * 1.5f;
                        chunk.GetComponent<Renderer>().material.color = damageColor;
                        
                        var rb = chunk.GetComponent<Rigidbody>();
                        if (rb == null) rb = chunk.AddComponent<Rigidbody>();
                        rb.mass = 5f;
                        rb.linearDamping = 0.5f;
                        rb.useGravity = true;
                        rb.isKinematic = false;
                        
                        // Add some initial force
                        rb.AddForce(UnityEngine.Random.insideUnitSphere * 2f, ForceMode.Impulse);
                        rb.AddTorque(UnityEngine.Random.insideUnitSphere * 5f, ForceMode.Impulse);
                        
                        data.DebrisPieces.Add(new DebrisData
                        {
                            Object = chunk,
                            Rigidbody = rb,
                            SpawnTime = Time.time
                        });
                    }
                }
            }
        }
        
        private void StartExplosionSequence(DestructionData data)
        {
            var bounds = CalculateBounds(data.Building);
            Vector3 center = data.Building.transform.position + Vector3.up * bounds.extents.y;
            
            // Hide original
            SetBuildingVisible(data.Building, false);
            
            // Create explosion effect
            var explosion = CreateExplosionEffect(center, bounds.size.magnitude);
            data.ActiveEffects.Add(explosion);
            
            // Spawn debris flying outward
            int debrisCount = Mathf.Min(30, maxDebrisPieces);
            for (int i = 0; i < debrisCount; i++)
            {
                var debris = GetDebrisPiece();
                debris.transform.position = center + UnityEngine.Random.insideUnitSphere * 2f;
                debris.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.5f, 1.5f);
                debris.GetComponent<Renderer>().material.color = Color.Lerp(damageColor, Color.black, 0.3f);
                
                var rb = debris.GetComponent<Rigidbody>();
                if (rb == null) rb = debris.AddComponent<Rigidbody>();
                rb.mass = 2f;
                rb.useGravity = true;
                rb.isKinematic = false;
                
                // Explosive force outward
                Vector3 dir = (debris.transform.position - center).normalized;
                rb.AddForce(dir * UnityEngine.Random.Range(10f, 25f), ForceMode.Impulse);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 10f, ForceMode.Impulse);
                
                data.DebrisPieces.Add(new DebrisData
                {
                    Object = debris,
                    Rigidbody = rb,
                    SpawnTime = Time.time
                });
            }
        }
        
        private void StartFireSequence(DestructionData data)
        {
            var bounds = CalculateBounds(data.Building);
            
            // Cover building in fire
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos = new Vector3(
                    UnityEngine.Random.Range(-bounds.extents.x, bounds.extents.x),
                    UnityEngine.Random.Range(0, bounds.size.y),
                    UnityEngine.Random.Range(-bounds.extents.z, bounds.extents.z)
                );
                
                var fire = CreateFireEffect(data.Building.transform, pos);
                data.ActiveEffects.Add(fire);
            }
            
            // Heavy smoke
            var smoke = CreateSmokeEffect(data.Building.transform, 
                Vector3.up * bounds.size.y,
                new Color(0.1f, 0.1f, 0.1f, 0.8f), 50f);
            data.ActiveEffects.Add(smoke);
            
            // Gradual charring of materials
            data.FireProgress = 0f;
        }
        
        private void StartCrumbleSequence(DestructionData data)
        {
            // Gradual disintegration from top to bottom
            data.CrumbleProgress = 0f;
            data.CrumbleFromTop = true;
        }
        
        #endregion
        
        #region Update Logic
        
        private void UpdateDestruction(DestructionData data)
        {
            if (data.State != DestructionState.Collapsing) return;
            
            float elapsed = Time.time - data.CollapseStartTime;
            
            // Update based on style
            switch (data.Style)
            {
                case DestructionStyle.Collapse:
                    UpdateCollapseDestruction(data, elapsed);
                    break;
                case DestructionStyle.Fire:
                    UpdateFireDestruction(data, elapsed);
                    break;
                case DestructionStyle.Crumble:
                    UpdateCrumbleDestruction(data, elapsed);
                    break;
            }
            
            // Update debris
            UpdateDebris(data);
            
            // Check if destruction is complete
            if (elapsed > debrisDuration)
            {
                CompleteDestruction(data);
            }
        }
        
        private void UpdateCollapseDestruction(DestructionData data, float elapsed)
        {
            if (data.FadeOut)
            {
                // Simple alpha fade
                float alpha = 1f - (elapsed / 2f);
                if (alpha <= 0)
                {
                    SetBuildingVisible(data.Building, false);
                }
                else
                {
                    SetBuildingAlpha(data.Building, alpha);
                }
            }
        }
        
        private void UpdateFireDestruction(DestructionData data, float elapsed)
        {
            data.FireProgress = Mathf.Clamp01(elapsed / 5f);
            
            // Gradually char materials
            var renderers = data.Building.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    var mat = renderer.materials[i];
                    mat.color = Color.Lerp(damageColor, Color.black, data.FireProgress);
                }
            }
            
            // After burning, collapse
            if (data.FireProgress >= 1f && !data.CollapseTriggered)
            {
                data.CollapseTriggered = true;
                StartCollapseSequence(data);
            }
        }
        
        private void UpdateCrumbleDestruction(DestructionData data, float elapsed)
        {
            data.CrumbleProgress = Mathf.Clamp01(elapsed / 3f);
            
            // Spawn debris particles falling
            if (UnityEngine.Random.value < data.CrumbleProgress * 0.3f)
            {
                var bounds = CalculateBounds(data.Building);
                float y = data.CrumbleFromTop ? 
                    bounds.max.y - data.CrumbleProgress * bounds.size.y : 
                    bounds.min.y + data.CrumbleProgress * bounds.size.y;
                
                Vector3 pos = new Vector3(
                    UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                    y,
                    UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
                );
                
                if (data.DebrisPieces.Count < maxDebrisPieces)
                {
                    var debris = GetDebrisPiece();
                    debris.transform.position = pos;
                    debris.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.3f, 0.8f);
                    
                    var rb = debris.GetComponent<Rigidbody>();
                    if (rb == null) rb = debris.AddComponent<Rigidbody>();
                    rb.useGravity = true;
                    rb.isKinematic = false;
                    
                    data.DebrisPieces.Add(new DebrisData
                    {
                        Object = debris,
                        Rigidbody = rb,
                        SpawnTime = Time.time
                    });
                }
            }
            
            // Gradually hide building
            SetBuildingAlpha(data.Building, 1f - data.CrumbleProgress);
        }
        
        private void UpdateDebris(DestructionData data)
        {
            // Remove old debris
            for (int i = data.DebrisPieces.Count - 1; i >= 0; i--)
            {
                var debris = data.DebrisPieces[i];
                if (Time.time - debris.SpawnTime > debrisDuration)
                {
                    ReturnDebrisPiece(debris.Object);
                    data.DebrisPieces.RemoveAt(i);
                }
            }
            
            // Play impact sounds for debris hitting ground
            foreach (var debris in data.DebrisPieces)
            {
                if (!debris.HitGround && debris.Object.transform.position.y < 0.5f)
                {
                    debris.HitGround = true;
                    PlaySound(debrisClips, debris.Object.transform.position, 0.3f);
                }
            }
        }
        
        private void CompleteDestruction(DestructionData data)
        {
            data.State = DestructionState.Destroyed;
            
            // Cleanup effects
            CleanupEffects(data);
            
            // Leave rubble (optional)
            CreateRubblePile(data);
            
            OnDestructionComplete?.Invoke(data.Id);
        }
        
        #endregion
        
        #region Effect Creation
        
        private ParticleSystem CreateSmokeEffect(Transform parent, Vector3 offset, Color color, float rate)
        {
            var smokeObj = new GameObject("DestructionSmoke");
            smokeObj.transform.SetParent(parent);
            smokeObj.transform.localPosition = offset;
            
            var ps = smokeObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            
            var emission = ps.emission;
            emission.rateOverTime = rate;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;
            
            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(2f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);
            
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.3f;
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0), new GradientColorKey(color, 1) },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0, 0), 
                    new GradientAlphaKey(color.a, 0.2f),
                    new GradientAlphaKey(color.a * 0.5f, 0.8f),
                    new GradientAlphaKey(0, 1) 
                }
            );
            colorOverLifetime.color = gradient;
            
            var renderer = smokeObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial();
            
            return ps;
        }
        
        private ParticleSystem CreateFireEffect(Transform parent, Vector3 offset)
        {
            var fireObj = new GameObject("DestructionFire");
            fireObj.transform.SetParent(parent);
            fireObj.transform.localPosition = offset;
            
            var ps = fireObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.8f, 0.2f),
                new Color(1f, 0.4f, 0.1f)
            );
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;
            
            var emission = ps.emission;
            emission.rateOverTime = 40;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25;
            shape.radius = 0.5f;
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0),
                    new GradientColorKey(new Color(1f, 0.3f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.3f, 0.1f, 0.05f), 1)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1, 0),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0, 1)
                }
            );
            colorOverLifetime.color = gradient;
            
            var renderer = fireObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial();
            
            // Add light
            var lightObj = new GameObject("FireLight");
            lightObj.transform.SetParent(fireObj.transform);
            lightObj.transform.localPosition = Vector3.zero;
            var light = lightObj.AddComponent<Light>();
            light.color = fireColor;
            light.intensity = 3f;
            light.range = 8f;
            light.type = LightType.Point;
            
            return ps;
        }
        
        private ParticleSystem CreateExplosionEffect(Vector3 position, float scale)
        {
            var explosionObj = new GameObject("Explosion");
            explosionObj.transform.position = position;
            
            var ps = explosionObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = scale * 2;
            main.startSize = scale;
            main.startColor = new Color(1f, 0.6f, 0.2f);
            
            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0, 50)
            });
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0),
                    new GradientColorKey(new Color(1f, 0.3f, 0.1f), 0.3f),
                    new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1, 0),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0, 1)
                }
            );
            colorOverLifetime.color = gradient;
            
            var renderer = explosionObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial();
            
            // Add bright flash
            var flashLight = explosionObj.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(1f, 0.8f, 0.4f);
            flashLight.intensity = 10f;
            flashLight.range = scale * 3;
            
            // Fade light
            StartCoroutine(FadeLight(flashLight, 0.5f));
            
            Destroy(explosionObj, 3f);
            
            return ps;
        }
        
        private System.Collections.IEnumerator FadeLight(Light light, float duration)
        {
            float startIntensity = light.intensity;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                light.intensity = Mathf.Lerp(startIntensity, 0, elapsed / duration);
                yield return null;
            }
            
            light.intensity = 0;
        }
        
        private ParticleSystem CreateDustEffect()
        {
            var dustObj = new GameObject("DustCloud");
            dustObj.transform.SetParent(transform);
            
            var ps = dustObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startColor = dustColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 500;
            
            var emission = ps.emission;
            emission.rateOverTime = 100;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10, 1, 10);
            
            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-1f, 1f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
            
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1f;
            noise.frequency = 0.2f;
            
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(dustColor, 0), new GradientColorKey(dustColor, 1) },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0, 0),
                    new GradientAlphaKey(dustColor.a, 0.1f),
                    new GradientAlphaKey(dustColor.a * 0.3f, 0.9f),
                    new GradientAlphaKey(0, 1)
                }
            );
            colorOverLifetime.color = gradient;
            
            var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial();
            
            return ps;
        }
        
        private GameObject CreateDebrisPiece()
        {
            // Create random debris shape
            var debris = GameObject.CreatePrimitive(
                UnityEngine.Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere
            );
            debris.name = "Debris";
            debris.transform.SetParent(transform);
            
            var mat = new Material(Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
            mat.color = damageColor;
            debris.GetComponent<Renderer>().material = mat;
            
            // Add rigidbody
            var rb = debris.AddComponent<Rigidbody>();
            rb.mass = 5f;
            rb.isKinematic = true;
            
            return debris;
        }
        
        private void CreateRubblePile(DestructionData data)
        {
            var bounds = CalculateBounds(data.Building);
            
            var rubble = new GameObject($"Rubble_{data.Id}");
            rubble.transform.position = data.Building.transform.position;
            
            // Create pile of simple shapes
            int pieceCount = 10;
            for (int i = 0; i < pieceCount; i++)
            {
                var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.transform.SetParent(rubble.transform);
                piece.transform.localPosition = new Vector3(
                    UnityEngine.Random.Range(-bounds.extents.x, bounds.extents.x),
                    UnityEngine.Random.Range(0, 1f),
                    UnityEngine.Random.Range(-bounds.extents.z, bounds.extents.z)
                );
                piece.transform.localRotation = UnityEngine.Random.rotation;
                piece.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.5f, 1.5f);
                
                Destroy(piece.GetComponent<Collider>());
                
                var mat = new Material(Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                mat.color = Color.Lerp(damageColor, Color.black, UnityEngine.Random.Range(0, 0.3f));
                piece.GetComponent<Renderer>().material = mat;
            }
            
            data.RubblePile = rubble;
        }
        
        #endregion
        
        #region Helpers
        
        private Dictionary<Renderer, Material[]> CacheMaterials(GameObject building)
        {
            var cache = new Dictionary<Renderer, Material[]>();
            var renderers = building.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                cache[renderer] = renderer.sharedMaterials.ToArray();
            }
            return cache;
        }
        
        private void RestoreMaterials(DestructionData data)
        {
            foreach (var kvp in data.OriginalMaterials)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.sharedMaterials = kvp.Value;
                }
            }
        }
        
        private void CleanupEffects(DestructionData data)
        {
            foreach (var effect in data.ActiveEffects)
            {
                if (effect is ParticleSystem ps && _dustPool.Count < 20)
                {
                    ReturnDustEffect(ps);
                }
                else if (effect != null)
                {
                    Destroy((effect as Component)?.gameObject);
                }
            }
            data.ActiveEffects.Clear();
            
            foreach (var debris in data.DebrisPieces)
            {
                ReturnDebrisPiece(debris.Object);
            }
            data.DebrisPieces.Clear();
            
            foreach (var crack in data.CrackObjects)
            {
                if (crack != null) Destroy(crack);
            }
            data.CrackObjects.Clear();
        }
        
        private void SetBuildingVisible(GameObject building, bool visible)
        {
            var renderers = building.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = visible;
            }
        }
        
        private void SetBuildingAlpha(GameObject building, float alpha)
        {
            var renderers = building.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    var mat = renderer.materials[i];
                    var color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                }
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
            return bounds;
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
        
        private void PlaySound(AudioClip[] clips, Vector3 position, float volume = 1f)
        {
            if (clips == null || clips.Length == 0) return;
            
            var clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, volume);
            }
        }
        
        #endregion
    }
    
    #region Data Types
    
    public enum DestructionStyle
    {
        Collapse,
        Explosion,
        Fire,
        Crumble
    }
    
    public enum DestructionState
    {
        Intact,
        Damaged,
        HeavilyDamaged,
        Collapsing,
        Destroyed
    }
    
    public class DestructionData
    {
        public string Id;
        public GameObject Building;
        public DestructionState State;
        public DestructionStyle Style = DestructionStyle.Collapse;
        public float DamagePercent;
        public float CollapseStartTime;
        public Dictionary<Renderer, Material[]> OriginalMaterials = new Dictionary<Renderer, Material[]>();
        public List<object> ActiveEffects = new List<object>();
        public List<DebrisData> DebrisPieces = new List<DebrisData>();
        public List<GameObject> CrackObjects = new List<GameObject>();
        public GameObject RubblePile;
        public bool FadeOut;
        public float FireProgress;
        public bool CollapseTriggered;
        public float CrumbleProgress;
        public bool CrumbleFromTop;
    }
    
    public class DebrisData
    {
        public GameObject Object;
        public Rigidbody Rigidbody;
        public float SpawnTime;
        public bool HitGround;
    }
    
    #endregion
}
