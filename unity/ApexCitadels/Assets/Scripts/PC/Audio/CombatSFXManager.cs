using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.Audio
{
    /// <summary>
    /// Enhanced Combat Sound Effects Manager.
    /// Provides impactful audio feedback for all combat actions.
    /// Features:
    /// - Layered sound design (impact + sweetener)
    /// - Distance attenuation
    /// - Sound variation to prevent repetition
    /// - Dynamic mixing based on combat intensity
    /// </summary>
    public class CombatSFXManager : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private AudioMixerGroup combatMixerGroup;
        [SerializeField] private int poolSize = 20;
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float maxDistance = 50f;
        
        [Header("Sound Library")]
        [SerializeField] private CombatSFXLibrary sfxLibrary;
        
        [Header("Reverb")]
        [SerializeField] private AudioReverbFilter reverbFilter;
        [SerializeField] private bool enableReverb = true;
        
        // Singleton
        private static CombatSFXManager _instance;
        public static CombatSFXManager Instance => _instance;
        
        // Sound pools
        private List<AudioSource> _sourcePool;
        private int _poolIndex;
        
        // Combat state
        private float _combatIntensity;
        private int _activeImpactCount;
        private const int MAX_SIMULTANEOUS_IMPACTS = 10;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializePool();
        }
        
        private void InitializePool()
        {
            _sourcePool = new List<AudioSource>();
            
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"CombatSFX_{i}");
                go.transform.SetParent(transform);
                
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f; // 3D sound
                source.rolloffMode = AudioRolloffMode.Linear;
                source.maxDistance = maxDistance;
                source.dopplerLevel = 0;
                
                if (combatMixerGroup != null)
                {
                    source.outputAudioMixerGroup = combatMixerGroup;
                }
                
                _sourcePool.Add(source);
            }
        }
        
        #region Public API - Combat Actions
        
        /// <summary>
        /// Play weapon attack sound
        /// </summary>
        public void PlayAttack(WeaponType weapon, Vector3 position)
        {
            var sound = sfxLibrary?.GetAttackSound(weapon);
            if (sound != null)
            {
                PlayAtPosition(sound, position);
            }
        }
        
        /// <summary>
        /// Play impact/hit sound
        /// </summary>
        public void PlayImpact(ImpactType impact, Vector3 position, float damage = 0)
        {
            if (_activeImpactCount >= MAX_SIMULTANEOUS_IMPACTS) return;
            
            var sound = sfxLibrary?.GetImpactSound(impact);
            if (sound != null)
            {
                // Scale volume by damage
                float damageScale = damage > 0 ? Mathf.Clamp(damage / 100f, 0.5f, 1.5f) : 1f;
                PlayAtPosition(sound, position, damageScale);
                
                _activeImpactCount++;
                StartCoroutine(DecrementImpactCount(0.1f));
            }
        }
        
        /// <summary>
        /// Play projectile sound
        /// </summary>
        public void PlayProjectile(ProjectileType projectile, Vector3 position)
        {
            var sound = sfxLibrary?.GetProjectileSound(projectile);
            if (sound != null)
            {
                PlayAtPosition(sound, position);
            }
        }
        
        /// <summary>
        /// Play ability/spell sound
        /// </summary>
        public void PlayAbility(AbilityType ability, Vector3 position)
        {
            var sound = sfxLibrary?.GetAbilitySound(ability);
            if (sound != null)
            {
                PlayAtPosition(sound, position);
            }
        }
        
        /// <summary>
        /// Play unit voice line
        /// </summary>
        public void PlayUnitVoice(UnitVoiceType voice, Vector3 position)
        {
            var sound = sfxLibrary?.GetUnitVoice(voice);
            if (sound != null)
            {
                PlayAtPosition(sound, position);
            }
        }
        
        /// <summary>
        /// Play building/structure sound
        /// </summary>
        public void PlayStructure(StructureSoundType structure, Vector3 position)
        {
            var sound = sfxLibrary?.GetStructureSound(structure);
            if (sound != null)
            {
                PlayAtPosition(sound, position);
            }
        }
        
        /// <summary>
        /// Play ambient battle sound
        /// </summary>
        public void PlayBattleAmbient(BattleAmbientType ambient)
        {
            var sound = sfxLibrary?.GetBattleAmbient(ambient);
            if (sound != null)
            {
                PlayGlobal(sound);
            }
        }
        
        /// <summary>
        /// Play death/defeat sound
        /// </summary>
        public void PlayDeath(UnitType unit, Vector3 position)
        {
            var sound = sfxLibrary?.GetDeathSound(unit);
            if (sound != null)
            {
                PlayAtPosition(sound, position);
            }
        }
        
        #endregion
        
        #region Public API - Utility
        
        /// <summary>
        /// Set combat intensity (affects mixing)
        /// </summary>
        public void SetCombatIntensity(float intensity)
        {
            _combatIntensity = Mathf.Clamp01(intensity);
            ApplyIntensityMixing();
        }
        
        /// <summary>
        /// Play explosion with layered sounds
        /// </summary>
        public void PlayExplosion(Vector3 position, float size = 1f)
        {
            // Play layered explosion
            var explosion = sfxLibrary?.GetExplosionSound(size);
            if (explosion != null)
            {
                PlayAtPosition(explosion, position, size);
            }
            
            // Add debris/shrapnel
            if (size > 0.5f)
            {
                var debris = sfxLibrary?.GetDebrisSound();
                if (debris != null)
                {
                    StartCoroutine(PlayDelayed(debris, position, 0.1f, size * 0.7f));
                }
            }
            
            // Add reverb tail for big explosions
            if (size > 0.8f && enableReverb)
            {
                StartCoroutine(ApplyExplosionReverb(size));
            }
        }
        
        /// <summary>
        /// Set master combat volume
        /// </summary>
        public void SetVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
        }
        
        #endregion
        
        #region Internal
        
        private void PlayAtPosition(CombatSound sound, Vector3 position, float volumeScale = 1f)
        {
            var source = GetNextSource();
            
            // Position the source
            source.transform.position = position;
            
            // Get random variation
            var clip = sound.GetRandomClip();
            if (clip == null) return;
            
            source.clip = clip;
            source.volume = sound.volume * masterVolume * volumeScale;
            source.pitch = sound.GetRandomPitch();
            source.spatialBlend = sound.is3D ? 1f : 0f;
            source.minDistance = sound.minDistance;
            source.maxDistance = sound.maxDistance;
            
            source.Play();
            
            // Play sweetener if available
            if (sound.sweetener != null && UnityEngine.Random.value < sound.sweetenerChance)
            {
                StartCoroutine(PlaySweetener(sound.sweetener, position, sound.sweetenerDelay, sound.sweetenerVolume));
            }
        }
        
        private void PlayGlobal(CombatSound sound, float volumeScale = 1f)
        {
            var source = GetNextSource();
            source.transform.position = transform.position;
            
            var clip = sound.GetRandomClip();
            if (clip == null) return;
            
            source.clip = clip;
            source.volume = sound.volume * masterVolume * volumeScale;
            source.pitch = sound.GetRandomPitch();
            source.spatialBlend = 0f; // 2D
            
            source.Play();
        }
        
        private IEnumerator PlaySweetener(AudioClip sweetener, Vector3 position, float delay, float volume)
        {
            yield return new WaitForSeconds(delay);
            
            var source = GetNextSource();
            source.transform.position = position;
            source.clip = sweetener;
            source.volume = volume * masterVolume;
            source.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            source.Play();
        }
        
        private IEnumerator PlayDelayed(CombatSound sound, Vector3 position, float delay, float volumeScale = 1f)
        {
            yield return new WaitForSeconds(delay);
            PlayAtPosition(sound, position, volumeScale);
        }
        
        private IEnumerator ApplyExplosionReverb(float intensity)
        {
            if (reverbFilter == null) yield break;
            
            float originalDecay = reverbFilter.decayTime;
            reverbFilter.decayTime = Mathf.Lerp(1f, 3f, intensity);
            
            yield return new WaitForSeconds(2f);
            
            reverbFilter.decayTime = originalDecay;
        }
        
        private void ApplyIntensityMixing()
        {
            // Could adjust mixer parameters based on intensity
            // Higher intensity = more compression, louder overall
        }
        
        private IEnumerator DecrementImpactCount(float delay)
        {
            yield return new WaitForSeconds(delay);
            _activeImpactCount = Mathf.Max(0, _activeImpactCount - 1);
        }
        
        private AudioSource GetNextSource()
        {
            var source = _sourcePool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _sourcePool.Count;
            return source;
        }
        
        #endregion
    }
    
    #region Enums
    
    public enum WeaponType
    {
        Sword,
        Axe,
        Mace,
        Spear,
        Bow,
        Crossbow,
        Staff,
        Dagger,
        Hammer,
        Shield
    }
    
    public enum ImpactType
    {
        MetalOnMetal,
        MetalOnFlesh,
        MetalOnWood,
        MetalOnStone,
        ArrowHit,
        BoltHit,
        MagicHit,
        Blocked,
        Parried,
        Critical,
        Kill
    }
    
    public enum ProjectileType
    {
        Arrow,
        Bolt,
        Fireball,
        IceShard,
        Lightning,
        Rock,
        CannonBall,
        MagicMissile
    }
    
    public enum AbilityType
    {
        FireCast,
        IceCast,
        LightningCast,
        HealCast,
        BuffCast,
        DebuffCast,
        Summon,
        Teleport,
        Shield,
        AreaEffect
    }
    
    public enum UnitVoiceType
    {
        Select,
        Move,
        Attack,
        Hurt,
        Death,
        Victory,
        Retreat,
        Charge,
        Ready,
        Acknowledge
    }
    
    public enum StructureSoundType
    {
        Build,
        Upgrade,
        Damage,
        Destroy,
        Repair,
        Activate,
        Deactivate
    }
    
    public enum BattleAmbientType
    {
        BattleStart,
        BattleIntense,
        Victory,
        Defeat,
        Siege,
        Charge
    }
    
    public enum UnitType
    {
        Infantry,
        Archer,
        Cavalry,
        Siege,
        Mage,
        Guardian,
        Hero
    }
    
    #endregion
    
    /// <summary>
    /// Individual combat sound data with variations
    /// </summary>
    [Serializable]
    public class CombatSound
    {
        public string name;
        public AudioClip[] clips; // Multiple clips for variation
        
        [Range(0, 1)] public float volume = 1f;
        [Range(0.5f, 2f)] public float basePitch = 1f;
        [Range(0, 0.3f)] public float pitchVariation = 0.1f;
        
        [Header("3D Settings")]
        public bool is3D = true;
        public float minDistance = 1f;
        public float maxDistance = 50f;
        
        [Header("Sweetener")]
        public AudioClip sweetener;
        [Range(0, 1)] public float sweetenerChance = 0.3f;
        public float sweetenerDelay = 0.05f;
        [Range(0, 1)] public float sweetenerVolume = 0.5f;
        
        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }
        
        public float GetRandomPitch()
        {
            return basePitch + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
        }
    }
    
    /// <summary>
    /// Scriptable Object for combat SFX library
    /// </summary>
    [CreateAssetMenu(fileName = "CombatSFXLibrary", menuName = "Apex Citadels/Combat SFX Library")]
    public class CombatSFXLibrary : ScriptableObject
    {
        [Header("Weapon Attacks")]
        [SerializeField] private List<WeaponSound> weaponSounds;
        
        [Header("Impacts")]
        [SerializeField] private List<ImpactSound> impactSounds;
        
        [Header("Projectiles")]
        [SerializeField] private List<ProjectileSound> projectileSounds;
        
        [Header("Abilities")]
        [SerializeField] private List<AbilitySound> abilitySounds;
        
        [Header("Unit Voices")]
        [SerializeField] private List<UnitVoiceSound> voiceSounds;
        
        [Header("Structures")]
        [SerializeField] private List<StructureSound> structureSounds;
        
        [Header("Battle Ambients")]
        [SerializeField] private List<BattleAmbientSound> ambientSounds;
        
        [Header("Deaths")]
        [SerializeField] private List<DeathSound> deathSounds;
        
        [Header("Explosions")]
        [SerializeField] private CombatSound smallExplosion;
        [SerializeField] private CombatSound mediumExplosion;
        [SerializeField] private CombatSound largeExplosion;
        [SerializeField] private CombatSound debris;
        
        // Cached lookups
        private Dictionary<WeaponType, CombatSound> _weaponCache;
        private Dictionary<ImpactType, CombatSound> _impactCache;
        private Dictionary<ProjectileType, CombatSound> _projectileCache;
        private Dictionary<AbilityType, CombatSound> _abilityCache;
        private Dictionary<UnitVoiceType, CombatSound> _voiceCache;
        private Dictionary<StructureSoundType, CombatSound> _structureCache;
        private Dictionary<BattleAmbientType, CombatSound> _ambientCache;
        private Dictionary<UnitType, CombatSound> _deathCache;
        
        private void OnEnable()
        {
            BuildCaches();
        }
        
        private void BuildCaches()
        {
            _weaponCache = BuildCache(weaponSounds, s => s.weapon, s => s.sound);
            _impactCache = BuildCache(impactSounds, s => s.impact, s => s.sound);
            _projectileCache = BuildCache(projectileSounds, s => s.projectile, s => s.sound);
            _abilityCache = BuildCache(abilitySounds, s => s.ability, s => s.sound);
            _voiceCache = BuildCache(voiceSounds, s => s.voice, s => s.sound);
            _structureCache = BuildCache(structureSounds, s => s.structure, s => s.sound);
            _ambientCache = BuildCache(ambientSounds, s => s.ambient, s => s.sound);
            _deathCache = BuildCache(deathSounds, s => s.unit, s => s.sound);
        }
        
        private Dictionary<T, CombatSound> BuildCache<TItem, T>(List<TItem> items, 
            Func<TItem, T> keySelector, Func<TItem, CombatSound> valueSelector)
        {
            var cache = new Dictionary<T, CombatSound>();
            if (items == null) return cache;
            
            foreach (var item in items)
            {
                cache[keySelector(item)] = valueSelector(item);
            }
            return cache;
        }
        
        public CombatSound GetAttackSound(WeaponType weapon)
        {
            if (_weaponCache == null) BuildCaches();
            _weaponCache.TryGetValue(weapon, out var sound);
            return sound;
        }
        
        public CombatSound GetImpactSound(ImpactType impact)
        {
            if (_impactCache == null) BuildCaches();
            _impactCache.TryGetValue(impact, out var sound);
            return sound;
        }
        
        public CombatSound GetProjectileSound(ProjectileType projectile)
        {
            if (_projectileCache == null) BuildCaches();
            _projectileCache.TryGetValue(projectile, out var sound);
            return sound;
        }
        
        public CombatSound GetAbilitySound(AbilityType ability)
        {
            if (_abilityCache == null) BuildCaches();
            _abilityCache.TryGetValue(ability, out var sound);
            return sound;
        }
        
        public CombatSound GetUnitVoice(UnitVoiceType voice)
        {
            if (_voiceCache == null) BuildCaches();
            _voiceCache.TryGetValue(voice, out var sound);
            return sound;
        }
        
        public CombatSound GetStructureSound(StructureSoundType structure)
        {
            if (_structureCache == null) BuildCaches();
            _structureCache.TryGetValue(structure, out var sound);
            return sound;
        }
        
        public CombatSound GetBattleAmbient(BattleAmbientType ambient)
        {
            if (_ambientCache == null) BuildCaches();
            _ambientCache.TryGetValue(ambient, out var sound);
            return sound;
        }
        
        public CombatSound GetDeathSound(UnitType unit)
        {
            if (_deathCache == null) BuildCaches();
            _deathCache.TryGetValue(unit, out var sound);
            return sound;
        }
        
        public CombatSound GetExplosionSound(float size)
        {
            if (size < 0.4f) return smallExplosion;
            if (size < 0.7f) return mediumExplosion;
            return largeExplosion;
        }
        
        public CombatSound GetDebrisSound() => debris;
    }
    
    #region Sound Type Wrappers
    
    [Serializable] public class WeaponSound { public WeaponType weapon; public CombatSound sound; }
    [Serializable] public class ImpactSound { public ImpactType impact; public CombatSound sound; }
    [Serializable] public class ProjectileSound { public ProjectileType projectile; public CombatSound sound; }
    [Serializable] public class AbilitySound { public AbilityType ability; public CombatSound sound; }
    [Serializable] public class UnitVoiceSound { public UnitVoiceType voice; public CombatSound sound; }
    [Serializable] public class StructureSound { public StructureSoundType structure; public CombatSound sound; }
    [Serializable] public class BattleAmbientSound { public BattleAmbientType ambient; public CombatSound sound; }
    [Serializable] public class DeathSound { public UnitType unit; public CombatSound sound; }
    
    #endregion
}
