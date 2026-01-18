// ============================================================================
// APEX CITADELS - COMBAT VFX SYSTEM
// AAA-quality visual effects for combat: explosions, projectiles, shields, impacts
// ============================================================================
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.Combat
{
    /// <summary>
    /// Central manager for all combat visual effects.
    /// Creates and pools particle systems for reuse.
    /// </summary>
    public class CombatVFX : MonoBehaviour
    {
        public static CombatVFX Instance { get; private set; }

        [Header("Effect Colors")]
        [SerializeField] private Color attackColor = new Color(1f, 0.4f, 0.1f); // Orange-red
        [SerializeField] private Color defenseColor = new Color(0.2f, 0.6f, 1f); // Blue
        [SerializeField] private Color criticalColor = new Color(1f, 0.9f, 0.1f); // Gold
        [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.4f); // Green
        [SerializeField] private Color siegeColor = new Color(0.6f, 0.1f, 0.1f); // Dark red

        [Header("Effect Settings")]
        [SerializeField] private int poolSize = 20;
        [SerializeField] private float effectScale = 1f;

        // Particle pools
        private Queue<ParticleSystem> _explosionPool = new Queue<ParticleSystem>();
        private Queue<ParticleSystem> _impactPool = new Queue<ParticleSystem>();
        private Queue<ParticleSystem> _projectilePool = new Queue<ParticleSystem>();
        private Queue<ParticleSystem> _shieldPool = new Queue<ParticleSystem>();
        private Queue<ParticleSystem> _auraPool = new Queue<ParticleSystem>();

        // Effect containers
        private Transform _effectsContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateEffectsContainer();
            InitializePools();
        }

        private void CreateEffectsContainer()
        {
            _effectsContainer = new GameObject("CombatVFX_Container").transform;
            _effectsContainer.SetParent(transform);
        }

        private void InitializePools()
        {
            for (int i = 0; i < poolSize; i++)
            {
                _explosionPool.Enqueue(CreateExplosionEffect());
                _impactPool.Enqueue(CreateImpactEffect());
                _projectilePool.Enqueue(CreateProjectileEffect());
                _shieldPool.Enqueue(CreateShieldEffect());
                _auraPool.Enqueue(CreateAuraEffect());
            }
        }

        #region Public API

        /// <summary>
        /// Play a large explosion at the target position (siege weapons, territory capture)
        /// </summary>
        public void PlayExplosion(Vector3 position, ExplosionSize size = ExplosionSize.Medium, Color? color = null)
        {
            ParticleSystem ps = GetFromPool(_explosionPool, CreateExplosionEffect);
            ps.transform.position = position;
            
            var main = ps.main;
            float scale = size switch
            {
                ExplosionSize.Small => 0.5f,
                ExplosionSize.Medium => 1f,
                ExplosionSize.Large => 2f,
                ExplosionSize.Massive => 4f,
                _ => 1f
            };
            main.startSize = new ParticleSystem.MinMaxCurve(2f * scale, 4f * scale);
            main.startColor = color ?? siegeColor;
            
            ps.Play();
            StartCoroutine(ReturnToPool(ps, _explosionPool, main.duration + main.startLifetime.constantMax));
        }

        /// <summary>
        /// Play impact spark when attack hits (sword clash, arrow hit)
        /// </summary>
        public void PlayImpact(Vector3 position, ImpactType type = ImpactType.Normal)
        {
            ParticleSystem ps = GetFromPool(_impactPool, CreateImpactEffect);
            ps.transform.position = position;
            
            var main = ps.main;
            main.startColor = type switch
            {
                ImpactType.Normal => attackColor,
                ImpactType.Critical => criticalColor,
                ImpactType.Blocked => defenseColor,
                ImpactType.Heal => healColor,
                _ => attackColor
            };
            
            ps.Play();
            StartCoroutine(ReturnToPool(ps, _impactPool, main.duration + main.startLifetime.constantMax));
        }

        /// <summary>
        /// Fire a projectile from source to target (arrows, fireballs)
        /// </summary>
        public void FireProjectile(Vector3 from, Vector3 to, ProjectileType type = ProjectileType.Arrow, System.Action onHit = null)
        {
            ParticleSystem ps = GetFromPool(_projectilePool, CreateProjectileEffect);
            
            var main = ps.main;
            main.startColor = type switch
            {
                ProjectileType.Arrow => new Color(0.6f, 0.4f, 0.2f),
                ProjectileType.Fireball => attackColor,
                ProjectileType.Magic => new Color(0.8f, 0.2f, 1f),
                ProjectileType.Siege => siegeColor,
                _ => Color.white
            };
            
            StartCoroutine(AnimateProjectile(ps, from, to, type, onHit));
        }

        /// <summary>
        /// Show shield effect around a position (defense active)
        /// </summary>
        public void PlayShieldEffect(Vector3 position, float duration = 2f, Color? color = null)
        {
            ParticleSystem ps = GetFromPool(_shieldPool, CreateShieldEffect);
            ps.transform.position = position;
            
            var main = ps.main;
            main.startColor = color ?? defenseColor;
            main.duration = duration;
            
            ps.Play();
            StartCoroutine(ReturnToPool(ps, _shieldPool, duration + 1f));
        }

        /// <summary>
        /// Play battle aura around units (buff/rally effect)
        /// </summary>
        public void PlayBattleAura(Vector3 position, AuraType type = AuraType.Rally, float duration = 3f)
        {
            ParticleSystem ps = GetFromPool(_auraPool, CreateAuraEffect);
            ps.transform.position = position;
            
            var main = ps.main;
            main.startColor = type switch
            {
                AuraType.Rally => criticalColor,
                AuraType.Attack => attackColor,
                AuraType.Defense => defenseColor,
                AuraType.Heal => healColor,
                _ => Color.white
            };
            main.duration = duration;
            
            ps.Play();
            StartCoroutine(ReturnToPool(ps, _auraPool, duration + 1f));
        }

        /// <summary>
        /// Play territory capture sequence
        /// </summary>
        public void PlayTerritoryCapture(Vector3 center, float radius)
        {
            StartCoroutine(CapturSequence(center, radius));
        }

        /// <summary>
        /// Play defeat effect (territory lost)
        /// </summary>
        public void PlayDefeatEffect(Vector3 position)
        {
            StartCoroutine(DefeatSequence(position));
        }

        /// <summary>
        /// Play victory effect (territory captured)
        /// </summary>
        public void PlayVictoryEffect(Vector3 position)
        {
            StartCoroutine(VictorySequence(position));
        }

        #endregion

        #region Effect Creation

        private ParticleSystem CreateExplosionEffect()
        {
            GameObject obj = new GameObject("Explosion");
            obj.transform.SetParent(_effectsContainer);
            obj.SetActive(false);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
            main.startSize = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startColor = siegeColor;
            main.gravityModifier = 0.5f;
            main.maxParticles = 100;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50, 80) });
            emission.rateOverTime = 0;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(attackColor, 0.3f), new GradientColorKey(Color.black, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Sub-emitter for sparks
            GameObject sparks = new GameObject("Sparks");
            sparks.transform.SetParent(obj.transform);
            ParticleSystem sparkPS = sparks.AddComponent<ParticleSystem>();
            
            var sparkMain = sparkPS.main;
            sparkMain.duration = 0.3f;
            sparkMain.startLifetime = 0.5f;
            sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(10f, 20f);
            sparkMain.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            sparkMain.startColor = criticalColor;
            sparkMain.gravityModifier = 1f;
            sparkMain.maxParticles = 50;
            sparkMain.playOnAwake = false;

            var sparkEmission = sparkPS.emission;
            sparkEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30, 50) });

            // Renderer
            SetupParticleRenderer(obj);
            SetupParticleRenderer(sparks);

            return ps;
        }

        private ParticleSystem CreateImpactEffect()
        {
            GameObject obj = new GameObject("Impact");
            obj.transform.SetParent(_effectsContainer);
            obj.SetActive(false);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 0.2f;
            main.startLifetime = 0.3f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startColor = attackColor;
            main.maxParticles = 30;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15, 25) });
            emission.rateOverTime = 0;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.1f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(attackColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            SetupParticleRenderer(obj);
            return ps;
        }

        private ParticleSystem CreateProjectileEffect()
        {
            GameObject obj = new GameObject("Projectile");
            obj.transform.SetParent(_effectsContainer);
            obj.SetActive(false);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 5f;
            main.startLifetime = 0.2f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.maxParticles = 50;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 50f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(attackColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            SetupParticleRenderer(obj);
            return ps;
        }

        private ParticleSystem CreateShieldEffect()
        {
            GameObject obj = new GameObject("Shield");
            obj.transform.SetParent(_effectsContainer);
            obj.SetActive(false);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 2f;
            main.startLifetime = 1f;
            main.startSpeed = 0.5f;
            main.startSize = 0.5f;
            main.startColor = defenseColor;
            main.maxParticles = 100;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 40f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 3f;
            shape.radiusThickness = 0f; // Only on surface

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(defenseColor, 0f), new GradientColorKey(Color.cyan, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 2f;

            SetupParticleRenderer(obj);
            return ps;
        }

        private ParticleSystem CreateAuraEffect()
        {
            GameObject obj = new GameObject("Aura");
            obj.transform.SetParent(_effectsContainer);
            obj.SetActive(false);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.duration = 3f;
            main.startLifetime = 2f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.gravityModifier = -0.2f; // Float up
            main.maxParticles = 60;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 20f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 4f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(criticalColor, 0.5f), new GradientColorKey(criticalColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.6f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            SetupParticleRenderer(obj);
            return ps;
        }

        private void SetupParticleRenderer(GameObject obj)
        {
            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            if (renderer == null) renderer = obj.AddComponent<ParticleSystemRenderer>();
            
            Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (mat.shader == null) mat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        #endregion

        #region Pool Management

        private ParticleSystem GetFromPool(Queue<ParticleSystem> pool, System.Func<ParticleSystem> createFunc)
        {
            ParticleSystem ps;
            if (pool.Count > 0)
            {
                ps = pool.Dequeue();
            }
            else
            {
                ps = createFunc();
            }
            ps.gameObject.SetActive(true);
            return ps;
        }

        private IEnumerator ReturnToPool(ParticleSystem ps, Queue<ParticleSystem> pool, float delay)
        {
            yield return new WaitForSeconds(delay);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
            pool.Enqueue(ps);
        }

        #endregion

        #region Animation Coroutines

        private IEnumerator AnimateProjectile(ParticleSystem ps, Vector3 from, Vector3 to, ProjectileType type, System.Action onHit)
        {
            ps.gameObject.SetActive(true);
            ps.transform.position = from;
            ps.Play();

            float speed = type switch
            {
                ProjectileType.Arrow => 30f,
                ProjectileType.Fireball => 20f,
                ProjectileType.Magic => 25f,
                ProjectileType.Siege => 15f,
                _ => 25f
            };

            float distance = Vector3.Distance(from, to);
            float duration = distance / speed;
            float elapsed = 0f;

            Vector3 direction = (to - from).normalized;
            float arc = type == ProjectileType.Siege ? 5f : 2f; // Siege has higher arc

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Parabolic arc
                float height = 4f * arc * t * (1f - t);
                Vector3 pos = Vector3.Lerp(from, to, t) + Vector3.up * height;
                ps.transform.position = pos;
                ps.transform.LookAt(pos + direction);
                
                yield return null;
            }

            ps.Stop();
            ps.gameObject.SetActive(false);
            _projectilePool.Enqueue(ps);

            // Play impact at destination
            PlayImpact(to, ImpactType.Normal);
            onHit?.Invoke();
        }

        private IEnumerator CapturSequence(Vector3 center, float radius)
        {
            // Ring of explosions
            int explosionCount = 8;
            for (int i = 0; i < explosionCount; i++)
            {
                float angle = (360f / explosionCount) * i * Mathf.Deg2Rad;
                Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius * 0.7f;
                PlayExplosion(pos, ExplosionSize.Small, attackColor);
                yield return new WaitForSeconds(0.1f);
            }

            // Central big explosion
            yield return new WaitForSeconds(0.2f);
            PlayExplosion(center, ExplosionSize.Large, criticalColor);
            
            // Victory aura
            yield return new WaitForSeconds(0.3f);
            PlayBattleAura(center, AuraType.Rally, 3f);
        }

        private IEnumerator DefeatSequence(Vector3 position)
        {
            // Fading shield
            PlayShieldEffect(position, 1f, Color.red);
            yield return new WaitForSeconds(0.5f);
            
            // Multiple small explosions
            for (int i = 0; i < 5; i++)
            {
                Vector3 offset = Random.insideUnitSphere * 5f;
                offset.y = Mathf.Abs(offset.y);
                PlayExplosion(position + offset, ExplosionSize.Small, Color.gray);
                yield return new WaitForSeconds(0.15f);
            }
        }

        private IEnumerator VictorySequence(Vector3 position)
        {
            // Shield burst
            PlayShieldEffect(position, 0.5f, criticalColor);
            yield return new WaitForSeconds(0.3f);
            
            // Multiple colorful explosions
            Color[] colors = { criticalColor, attackColor, healColor };
            for (int i = 0; i < 6; i++)
            {
                Vector3 offset = Random.insideUnitSphere * 3f;
                offset.y = Mathf.Abs(offset.y) + 2f;
                PlayExplosion(position + offset, ExplosionSize.Medium, colors[i % colors.Length]);
                yield return new WaitForSeconds(0.2f);
            }
            
            // Final big golden explosion
            yield return new WaitForSeconds(0.3f);
            PlayExplosion(position + Vector3.up * 5f, ExplosionSize.Massive, criticalColor);
        }

        #endregion
    }

    #region Enums

    public enum ExplosionSize
    {
        Small,
        Medium,
        Large,
        Massive
    }

    public enum ImpactType
    {
        Normal,
        Critical,
        Blocked,
        Heal
    }

    public enum ProjectileType
    {
        Arrow,
        Fireball,
        Magic,
        Siege
    }

    public enum AuraType
    {
        Rally,
        Attack,
        Defense,
        Heal
    }

    #endregion
}
