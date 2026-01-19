// ============================================================================
// APEX CITADELS - COMBAT AUDIO SFX
// Sound effects for combat: attacks, impacts, abilities, victory/defeat
// ============================================================================
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ApexCitadels.Core;

namespace ApexCitadels.PC.Combat
{
    /// <summary>
    /// Manages combat sound effects with pooling and variation.
    /// Procedurally generates placeholder sounds when audio files aren't available.
    /// </summary>
    public class CombatAudioSFX : MonoBehaviour
    {
        public static CombatAudioSFX Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int audioSourcePoolSize = 20;
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 0.8f;
        [SerializeField] private bool useProcedural = true; // Use generated sounds as placeholders

        [Header("Volume Levels")]
        [SerializeField] [Range(0f, 1f)] private float attackVolume = 0.7f;
        [SerializeField] [Range(0f, 1f)] private float impactVolume = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float explosionVolume = 0.9f;
        [SerializeField] [Range(0f, 1f)] private float uiVolume = 0.5f;

        // Audio source pool
        private Queue<AudioSource> _audioPool = new Queue<AudioSource>();
        private Transform _audioContainer;

        // Generated clips cache
        private Dictionary<string, AudioClip> _generatedClips = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateAudioContainer();
            InitializePool();
            GeneratePlaceholderSounds();
        }

        private void CreateAudioContainer()
        {
            _audioContainer = new GameObject("CombatAudio_Pool").transform;
            _audioContainer.SetParent(transform);
        }

        private void InitializePool()
        {
            for (int i = 0; i < audioSourcePoolSize; i++)
            {
                _audioPool.Enqueue(CreateAudioSource());
            }
        }

        private AudioSource CreateAudioSource()
        {
            GameObject obj = new GameObject("CombatAudioSource");
            obj.transform.SetParent(_audioContainer);
            
            AudioSource source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D by default
            
            return source;
        }

        private void GeneratePlaceholderSounds()
        {
            if (!useProcedural) return;

            // Generate various combat sounds procedurally
            _generatedClips["sword_swing"] = GenerateSwordSwing();
            _generatedClips["sword_hit"] = GenerateSwordHit();
            _generatedClips["arrow_fire"] = GenerateArrowFire();
            _generatedClips["arrow_hit"] = GenerateArrowHit();
            _generatedClips["explosion_small"] = GenerateExplosion(0.3f);
            _generatedClips["explosion_medium"] = GenerateExplosion(0.5f);
            _generatedClips["explosion_large"] = GenerateExplosion(1f);
            _generatedClips["shield_block"] = GenerateShieldBlock();
            _generatedClips["shield_activate"] = GenerateShieldActivate();
            _generatedClips["heal"] = GenerateHeal();
            _generatedClips["buff"] = GenerateBuff();
            _generatedClips["critical"] = GenerateCritical();
            _generatedClips["death"] = GenerateDeath();
            _generatedClips["victory_fanfare"] = GenerateVictoryFanfare();
            _generatedClips["defeat_stinger"] = GenerateDefeatStinger();
            _generatedClips["battle_horn"] = GenerateBattleHorn();
            _generatedClips["march"] = GenerateMarch();
            _generatedClips["charge"] = GenerateCharge();
            _generatedClips["ui_click"] = GenerateUIClick();
            _generatedClips["ui_confirm"] = GenerateUIConfirm();
            _generatedClips["ui_cancel"] = GenerateUICancel();
            _generatedClips["coins"] = GenerateCoins();
            _generatedClips["level_up"] = GenerateLevelUp();
        }

        #region Public API

        // ===================== ATTACK SOUNDS =====================

        public void PlaySwordSwing(Vector3? position = null)
        {
            PlaySound("sword_swing", attackVolume, position, 0.9f, 1.1f);
        }

        public void PlaySwordHit(Vector3? position = null)
        {
            PlaySound("sword_hit", impactVolume, position, 0.9f, 1.1f);
        }

        public void PlayArrowFire(Vector3? position = null)
        {
            PlaySound("arrow_fire", attackVolume * 0.7f, position, 0.95f, 1.05f);
        }

        public void PlayArrowHit(Vector3? position = null)
        {
            PlaySound("arrow_hit", impactVolume * 0.6f, position, 0.9f, 1.1f);
        }

        // ===================== EXPLOSION SOUNDS =====================

        public void PlayExplosion(ExplosionSize size, Vector3? position = null)
        {
            string clipName = size switch
            {
                ExplosionSize.Small => "explosion_small",
                ExplosionSize.Medium => "explosion_medium",
                _ => "explosion_large"
            };
            PlaySound(clipName, explosionVolume, position, 0.9f, 1.1f);
        }

        // ===================== DEFENSE SOUNDS =====================

        public void PlayShieldBlock(Vector3? position = null)
        {
            PlaySound("shield_block", impactVolume, position, 0.95f, 1.05f);
        }

        public void PlayShieldActivate(Vector3? position = null)
        {
            PlaySound("shield_activate", attackVolume * 0.8f, position);
        }

        // ===================== ABILITY SOUNDS =====================

        public void PlayHeal(Vector3? position = null)
        {
            PlaySound("heal", attackVolume * 0.7f, position);
        }

        public void PlayBuff(Vector3? position = null)
        {
            PlaySound("buff", attackVolume * 0.6f, position);
        }

        public void PlayCriticalHit(Vector3? position = null)
        {
            PlaySound("critical", impactVolume * 1.1f, position);
        }

        // ===================== UNIT SOUNDS =====================

        public void PlayDeath(Vector3? position = null)
        {
            PlaySound("death", impactVolume * 0.8f, position, 0.8f, 1.2f);
        }

        public void PlayBattleHorn()
        {
            PlaySound("battle_horn", attackVolume * 1.2f);
        }

        public void PlayMarch()
        {
            PlaySound("march", attackVolume * 0.5f);
        }

        public void PlayCharge()
        {
            PlaySound("charge", attackVolume * 1.1f);
        }

        // ===================== RESULT SOUNDS =====================

        public void PlayVictory()
        {
            PlaySound("victory_fanfare", masterVolume);
        }

        public void PlayDefeat()
        {
            PlaySound("defeat_stinger", masterVolume * 0.9f);
        }

        // ===================== UI SOUNDS =====================

        public void PlayUIClick()
        {
            PlaySound("ui_click", uiVolume);
        }

        public void PlayUIConfirm()
        {
            PlaySound("ui_confirm", uiVolume);
        }

        public void PlayUICancel()
        {
            PlaySound("ui_cancel", uiVolume * 0.8f);
        }

        // ===================== REWARD SOUNDS =====================

        public void PlayCoins()
        {
            PlaySound("coins", uiVolume * 0.9f);
        }

        public void PlayLevelUp()
        {
            PlaySound("level_up", masterVolume);
        }

        #endregion

        #region Core Play Function

        private void PlaySound(string clipName, float volume, Vector3? position = null, float pitchMin = 1f, float pitchMax = 1f)
        {
            AudioClip clip = GetClip(clipName);
            if (clip == null) return;

            AudioSource source = GetFromPool();
            if (source == null) return;

            source.clip = clip;
            source.volume = volume * masterVolume;
            source.pitch = Random.Range(pitchMin, pitchMax);
            
            if (position.HasValue)
            {
                source.transform.position = position.Value;
                source.spatialBlend = 0.5f; // Partial 3D
            }
            else
            {
                source.spatialBlend = 0f; // 2D
            }

            source.Play();
            StartCoroutine(ReturnToPool(source, clip.length + 0.1f));
        }

        private AudioClip GetClip(string name)
        {
            if (_generatedClips.TryGetValue(name, out AudioClip clip))
            {
                return clip;
            }
            
            // Could add Resources.Load fallback here for real audio files
            ApexLogger.LogWarning($"Clip not found: {name}", ApexLogger.LogCategory.Audio);
            return null;
        }

        #endregion

        #region Pool Management

        private AudioSource GetFromPool()
        {
            if (_audioPool.Count > 0)
            {
                return _audioPool.Dequeue();
            }
            // Pool exhausted, create new
            return CreateAudioSource();
        }

        private IEnumerator ReturnToPool(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            source.Stop();
            source.clip = null;
            _audioPool.Enqueue(source);
        }

        #endregion

        #region Procedural Audio Generation

        private AudioClip GenerateSwordSwing()
        {
            return GenerateNoise(0.15f, noise =>
            {
                float env = 1f - noise; // Quick attack, slower decay
                float freq = 300f + noise * 200f;
                return Mathf.Sin(noise * freq * Mathf.PI * 2) * env * 0.3f + 
                       Random.Range(-1f, 1f) * 0.1f * (1f - noise); // Add whoosh noise
            });
        }

        private AudioClip GenerateSwordHit()
        {
            return GenerateNoise(0.2f, noise =>
            {
                float env = Mathf.Exp(-noise * 15f);
                return (Random.Range(-1f, 1f) * 0.4f + 
                        Mathf.Sin(noise * 150f * Mathf.PI * 2) * 0.3f) * env;
            });
        }

        private AudioClip GenerateArrowFire()
        {
            return GenerateNoise(0.1f, noise =>
            {
                float env = 1f - noise * noise;
                return Random.Range(-1f, 1f) * 0.2f * env + 
                       Mathf.Sin(noise * 800f * Mathf.PI * 2) * 0.1f * (1f - noise);
            });
        }

        private AudioClip GenerateArrowHit()
        {
            return GenerateNoise(0.08f, noise =>
            {
                float env = Mathf.Exp(-noise * 20f);
                return Random.Range(-1f, 1f) * 0.3f * env;
            });
        }

        private AudioClip GenerateExplosion(float intensity)
        {
            return GenerateNoise(0.5f * intensity + 0.3f, noise =>
            {
                float env = Mathf.Exp(-noise * (3f / intensity));
                float rumble = Mathf.Sin(noise * 50f * Mathf.PI * 2) * 0.3f;
                float crackle = Random.Range(-1f, 1f) * 0.4f;
                return (rumble + crackle) * env * intensity;
            });
        }

        private AudioClip GenerateShieldBlock()
        {
            return GenerateNoise(0.25f, noise =>
            {
                float env = Mathf.Exp(-noise * 8f);
                float ring = Mathf.Sin(noise * 400f * Mathf.PI * 2) * 0.4f;
                float impact = Random.Range(-1f, 1f) * 0.2f * Mathf.Exp(-noise * 20f);
                return (ring + impact) * env;
            });
        }

        private AudioClip GenerateShieldActivate()
        {
            return GenerateNoise(0.4f, noise =>
            {
                float env = noise < 0.2f ? noise / 0.2f : 1f - (noise - 0.2f) / 0.8f;
                float hum = Mathf.Sin(noise * 200f * Mathf.PI * 2) * 0.2f;
                float shimmer = Mathf.Sin(noise * 1500f * Mathf.PI * 2) * 0.1f * (1f - noise);
                return (hum + shimmer) * env;
            });
        }

        private AudioClip GenerateHeal()
        {
            return GenerateNoise(0.5f, noise =>
            {
                float env = Mathf.Sin(noise * Mathf.PI);
                float tone = Mathf.Sin(noise * 600f * Mathf.PI * 2) * 0.2f;
                float sparkle = Mathf.Sin(noise * 1200f * Mathf.PI * 2) * 0.1f * (1f - noise * 0.5f);
                return (tone + sparkle) * env;
            });
        }

        private AudioClip GenerateBuff()
        {
            return GenerateNoise(0.4f, noise =>
            {
                float env = Mathf.Sin(noise * Mathf.PI);
                float sweep = Mathf.Sin(noise * (300f + noise * 500f) * Mathf.PI * 2) * 0.25f;
                return sweep * env;
            });
        }

        private AudioClip GenerateCritical()
        {
            return GenerateNoise(0.3f, noise =>
            {
                float env = noise < 0.1f ? noise / 0.1f : Mathf.Exp(-(noise - 0.1f) * 5f);
                float hit = Random.Range(-1f, 1f) * 0.3f * Mathf.Exp(-noise * 15f);
                float ring = Mathf.Sin(noise * 500f * Mathf.PI * 2) * 0.3f;
                return (hit + ring) * env;
            });
        }

        private AudioClip GenerateDeath()
        {
            return GenerateNoise(0.4f, noise =>
            {
                float env = Mathf.Exp(-noise * 4f);
                float groan = Mathf.Sin(noise * (150f - noise * 50f) * Mathf.PI * 2) * 0.3f;
                return groan * env;
            });
        }

        private AudioClip GenerateVictoryFanfare()
        {
            return GenerateNoise(1.2f, noise =>
            {
                float env = noise < 0.1f ? noise / 0.1f : (noise < 0.8f ? 1f : (1f - noise) / 0.2f);
                
                // Major chord progression
                float note1 = Mathf.Sin(noise * 440f * Mathf.PI * 2) * 0.15f;
                float note2 = Mathf.Sin(noise * 554f * Mathf.PI * 2) * 0.12f;
                float note3 = Mathf.Sin(noise * 659f * Mathf.PI * 2) * 0.1f;
                
                return (note1 + note2 + note3) * env;
            });
        }

        private AudioClip GenerateDefeatStinger()
        {
            return GenerateNoise(1f, noise =>
            {
                float env = Mathf.Exp(-noise * 2f);
                
                // Minor/diminished chord
                float note1 = Mathf.Sin(noise * 220f * Mathf.PI * 2) * 0.15f;
                float note2 = Mathf.Sin(noise * 261f * Mathf.PI * 2) * 0.12f;
                float note3 = Mathf.Sin(noise * 311f * Mathf.PI * 2) * 0.1f;
                
                return (note1 + note2 + note3) * env;
            });
        }

        private AudioClip GenerateBattleHorn()
        {
            return GenerateNoise(0.8f, noise =>
            {
                float env = noise < 0.1f ? noise / 0.1f : (noise < 0.7f ? 1f : (1f - noise) / 0.3f);
                float horn = Mathf.Sin(noise * 300f * Mathf.PI * 2) * 0.3f;
                float harmonics = Mathf.Sin(noise * 600f * Mathf.PI * 2) * 0.1f +
                                 Mathf.Sin(noise * 900f * Mathf.PI * 2) * 0.05f;
                return (horn + harmonics) * env;
            });
        }

        private AudioClip GenerateMarch()
        {
            return GenerateNoise(0.3f, noise =>
            {
                float beat = noise < 0.1f ? 1f : 0f;
                float drum = Mathf.Sin(noise * 80f * Mathf.PI * 2) * beat * 0.4f;
                return drum + Random.Range(-1f, 1f) * 0.1f * beat;
            });
        }

        private AudioClip GenerateCharge()
        {
            return GenerateNoise(0.5f, noise =>
            {
                float env = noise * (1f - noise) * 4f;
                float rumble = Mathf.Sin(noise * 60f * Mathf.PI * 2) * 0.2f;
                float hooves = (Random.Range(-1f, 1f) * 0.15f + 
                               Mathf.Sin(noise * 200f * Mathf.PI * 2) * 0.1f) * (noise > 0.3f ? 1f : noise / 0.3f);
                return (rumble + hooves) * env;
            });
        }

        private AudioClip GenerateUIClick()
        {
            return GenerateNoise(0.05f, noise =>
            {
                float env = Mathf.Exp(-noise * 40f);
                return Mathf.Sin(noise * 1000f * Mathf.PI * 2) * 0.3f * env;
            });
        }

        private AudioClip GenerateUIConfirm()
        {
            return GenerateNoise(0.15f, noise =>
            {
                float env = Mathf.Exp(-noise * 10f);
                float tone1 = Mathf.Sin(noise * 800f * Mathf.PI * 2) * 0.2f;
                float tone2 = Mathf.Sin(noise * 1200f * Mathf.PI * 2) * 0.15f * (noise > 0.05f ? 1f : 0f);
                return (tone1 + tone2) * env;
            });
        }

        private AudioClip GenerateUICancel()
        {
            return GenerateNoise(0.1f, noise =>
            {
                float env = Mathf.Exp(-noise * 15f);
                float sweep = Mathf.Sin(noise * (500f - noise * 200f) * Mathf.PI * 2) * 0.25f;
                return sweep * env;
            });
        }

        private AudioClip GenerateCoins()
        {
            return GenerateNoise(0.3f, noise =>
            {
                // Multiple small "clink" sounds
                float clink1 = Mathf.Sin(noise * 2000f * Mathf.PI * 2) * Mathf.Exp(-noise * 20f) * 0.15f;
                float clink2 = Mathf.Sin((noise - 0.1f) * 2200f * Mathf.PI * 2) * Mathf.Exp(-(noise - 0.1f) * 20f) * (noise > 0.1f ? 0.12f : 0f);
                float clink3 = Mathf.Sin((noise - 0.2f) * 1800f * Mathf.PI * 2) * Mathf.Exp(-(noise - 0.2f) * 20f) * (noise > 0.2f ? 0.1f : 0f);
                return clink1 + clink2 + clink3;
            });
        }

        private AudioClip GenerateLevelUp()
        {
            return GenerateNoise(0.8f, noise =>
            {
                float env = noise < 0.1f ? noise / 0.1f : 1f - (noise - 0.1f) / 0.9f;
                
                // Rising arpeggio
                float freq = 400f + noise * 800f;
                float tone = Mathf.Sin(noise * freq * Mathf.PI * 2) * 0.2f;
                float sparkle = Mathf.Sin(noise * freq * 2f * Mathf.PI * 2) * 0.1f;
                
                return (tone + sparkle) * env;
            });
        }

        private AudioClip GenerateNoise(float duration, System.Func<float, float> generator)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                samples[i] = Mathf.Clamp(generator(t), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Generated", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
