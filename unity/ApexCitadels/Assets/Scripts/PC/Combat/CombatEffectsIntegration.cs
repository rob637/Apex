// ============================================================================
// APEX CITADELS - COMBAT EFFECTS INTEGRATION
// Coordinates all combat visual, audio, and camera effects
// ============================================================================
using UnityEngine;
using System.Collections;
using ApexCitadels.Core;

namespace ApexCitadels.PC.Combat
{
    /// <summary>
    /// Central coordinator for combat effects.
    /// Call these methods from CombatPanel/CombatManager to trigger synchronized effects.
    /// </summary>
    public class CombatEffectsIntegration : MonoBehaviour
    {
        public static CombatEffectsIntegration Instance { get; private set; }

        [Header("Auto-Setup")]
        [SerializeField] private bool autoCreateSystems = true;

        // References
        private CombatVFX _vfx;
        private DamageNumbers _damageNumbers;
        private CombatCameraEffects _cameraEffects;
        private CombatAudioSFX _audioSFX;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (autoCreateSystems)
            {
                SetupSystems();
            }
        }

        private void SetupSystems()
        {
            // Create VFX system
            if (CombatVFX.Instance == null)
            {
                GameObject vfxObj = new GameObject("CombatVFX");
                vfxObj.transform.SetParent(transform);
                _vfx = vfxObj.AddComponent<CombatVFX>();
            }
            else
            {
                _vfx = CombatVFX.Instance;
            }

            // Create Damage Numbers system
            if (DamageNumbers.Instance == null)
            {
                GameObject dmgObj = new GameObject("DamageNumbers");
                dmgObj.transform.SetParent(transform);
                _damageNumbers = dmgObj.AddComponent<DamageNumbers>();
            }
            else
            {
                _damageNumbers = DamageNumbers.Instance;
            }

            // Create Camera Effects system
            if (CombatCameraEffects.Instance == null)
            {
                GameObject camObj = new GameObject("CombatCameraEffects");
                camObj.transform.SetParent(transform);
                _cameraEffects = camObj.AddComponent<CombatCameraEffects>();
            }
            else
            {
                _cameraEffects = CombatCameraEffects.Instance;
            }

            // Create Audio SFX system
            if (CombatAudioSFX.Instance == null)
            {
                GameObject audioObj = new GameObject("CombatAudioSFX");
                audioObj.transform.SetParent(transform);
                _audioSFX = audioObj.AddComponent<CombatAudioSFX>();
            }
            else
            {
                _audioSFX = CombatAudioSFX.Instance;
            }
        }

        #region High-Level Combat Events

        /// <summary>
        /// Called when battle starts - horn, UI effects
        /// </summary>
        public void OnBattleStart()
        {
            _audioSFX?.PlayBattleHorn();
            _cameraEffects?.Shake(0.2f);
            ApexLogger.Log("Battle started!", ApexLogger.LogCategory.Combat);
        }

        /// <summary>
        /// Called when troops charge into battle
        /// </summary>
        public void OnCharge(Vector3? position = null)
        {
            _audioSFX?.PlayCharge();
            _cameraEffects?.Shake(0.15f);
            
            if (position.HasValue)
            {
                _vfx?.PlayBattleAura(position.Value, AuraType.Attack, 2f);
            }
        }

        /// <summary>
        /// Melee attack (sword, infantry)
        /// </summary>
        public void OnMeleeAttack(Vector3 position, int damage, bool critical = false)
        {
            _audioSFX?.PlaySwordSwing(position);
            
            StartCoroutine(DelayedEffect(0.1f, () =>
            {
                _audioSFX?.PlaySwordHit(position);
                _vfx?.PlayImpact(position, critical ? ImpactType.Critical : ImpactType.Normal);
                _damageNumbers?.ShowDamage(position, damage, critical);
                _cameraEffects?.Shake(critical ? 0.25f : 0.1f);
                
                if (critical)
                {
                    _audioSFX?.PlayCriticalHit(position);
                    _cameraEffects?.ChromaticPulse();
                }
            }));
        }

        /// <summary>
        /// Ranged attack (archer, arrow)
        /// </summary>
        public void OnRangedAttack(Vector3 from, Vector3 to, int damage, bool critical = false)
        {
            _audioSFX?.PlayArrowFire(from);
            
            _vfx?.FireProjectile(from, to, ProjectileType.Arrow, () =>
            {
                _audioSFX?.PlayArrowHit(to);
                _damageNumbers?.ShowDamage(to, damage, critical);
                _cameraEffects?.Shake(0.08f);
                
                if (critical)
                {
                    _audioSFX?.PlayCriticalHit(to);
                }
            });
        }

        /// <summary>
        /// Siege attack (catapult, battering ram)
        /// </summary>
        public void OnSiegeAttack(Vector3 from, Vector3 to, int damage)
        {
            _vfx?.FireProjectile(from, to, ProjectileType.Siege, () =>
            {
                _audioSFX?.PlayExplosion(ExplosionSize.Large, to);
                _vfx?.PlayExplosion(to, ExplosionSize.Large);
                _damageNumbers?.ShowDamage(to, damage, true);
                _cameraEffects?.ExplosionImpact(0.6f);
            });
        }

        /// <summary>
        /// Magic/ability attack
        /// </summary>
        public void OnMagicAttack(Vector3 from, Vector3 to, int damage, Color? effectColor = null)
        {
            _vfx?.FireProjectile(from, to, ProjectileType.Magic, () =>
            {
                _vfx?.PlayExplosion(to, ExplosionSize.Medium, effectColor ?? new Color(0.8f, 0.2f, 1f));
                _damageNumbers?.ShowDamage(to, damage, true);
                _cameraEffects?.ChromaticPulse();
                _cameraEffects?.Shake(0.2f);
            });
        }

        /// <summary>
        /// Attack was blocked
        /// </summary>
        public void OnAttackBlocked(Vector3 position)
        {
            _audioSFX?.PlayShieldBlock(position);
            _vfx?.PlayImpact(position, ImpactType.Blocked);
            _damageNumbers?.ShowBlock(position);
            _cameraEffects?.Shake(0.1f);
        }

        /// <summary>
        /// Attack missed
        /// </summary>
        public void OnAttackMissed(Vector3 position)
        {
            _damageNumbers?.ShowMiss(position);
        }

        /// <summary>
        /// Shield activated
        /// </summary>
        public void OnShieldActivated(Vector3 position, float duration = 3f)
        {
            _audioSFX?.PlayShieldActivate(position);
            _vfx?.PlayShieldEffect(position, duration);
        }

        /// <summary>
        /// Heal received
        /// </summary>
        public void OnHeal(Vector3 position, int amount)
        {
            _audioSFX?.PlayHeal(position);
            _vfx?.PlayImpact(position, ImpactType.Heal);
            _damageNumbers?.ShowHeal(position, amount);
            _cameraEffects?.HealFlash();
        }

        /// <summary>
        /// Buff/rally applied
        /// </summary>
        public void OnBuff(Vector3 position, string buffName = null)
        {
            _audioSFX?.PlayBuff(position);
            _vfx?.PlayBattleAura(position, AuraType.Rally, 2f);
            
            if (!string.IsNullOrEmpty(buffName))
            {
                _damageNumbers?.ShowCombatText(position, buffName, new Color(1f, 0.85f, 0.2f), 1.5f);
            }
        }

        /// <summary>
        /// Unit killed
        /// </summary>
        public void OnUnitKilled(Vector3 position, string unitName = null, int xpGain = 0)
        {
            _audioSFX?.PlayDeath(position);
            _vfx?.PlayExplosion(position, ExplosionSize.Small, new Color(0.6f, 0.1f, 0.1f));
            _damageNumbers?.ShowKill(position, unitName);
            
            if (xpGain > 0)
            {
                StartCoroutine(DelayedEffect(0.3f, () =>
                {
                    _damageNumbers?.ShowXPGain(position + Vector3.up, xpGain);
                }));
            }
        }

        /// <summary>
        /// Player took damage
        /// </summary>
        public void OnPlayerDamaged(int damage, float damagePercent)
        {
            _cameraEffects?.PlayerDamageEffect(damagePercent);
            
            if (damagePercent > 0.2f)
            {
                _audioSFX?.PlaySwordHit();
            }
        }

        /// <summary>
        /// Territory captured
        /// </summary>
        public void OnTerritoryCapture(Vector3 center, float radius)
        {
            _vfx?.PlayTerritoryCapture(center, radius);
            _audioSFX?.PlayVictory();
            _cameraEffects?.VictoryEffect();
        }

        /// <summary>
        /// Battle won
        /// </summary>
        public void OnVictory(Vector3 position)
        {
            _vfx?.PlayVictoryEffect(position);
            _audioSFX?.PlayVictory();
            _cameraEffects?.VictoryEffect();
        }

        /// <summary>
        /// Battle lost
        /// </summary>
        public void OnDefeat(Vector3 position)
        {
            _vfx?.PlayDefeatEffect(position);
            _audioSFX?.PlayDefeat();
            _cameraEffects?.DefeatEffect();
        }

        /// <summary>
        /// Resource gained from battle
        /// </summary>
        public void OnResourceGain(Vector3 position, string resourceType, int amount)
        {
            _audioSFX?.PlayCoins();
            _damageNumbers?.ShowResourceGain(position, resourceType, amount);
        }

        /// <summary>
        /// Level up!
        /// </summary>
        public void OnLevelUp(Vector3 position, int newLevel)
        {
            _audioSFX?.PlayLevelUp();
            _vfx?.PlayExplosion(position, ExplosionSize.Medium, new Color(1f, 0.85f, 0.2f));
            _damageNumbers?.ShowCombatText(position, $"LEVEL {newLevel}!", new Color(1f, 0.85f, 0.2f), 3f, true);
            _cameraEffects?.ChromaticPulse();
        }

        /// <summary>
        /// Combo hit
        /// </summary>
        public void OnCombo(Vector3 position, int comboCount)
        {
            _damageNumbers?.ShowCombo(position, comboCount);
            _cameraEffects?.Shake(0.1f + comboCount * 0.02f);
        }

        #endregion

        #region UI Events

        public void OnUIClick()
        {
            _audioSFX?.PlayUIClick();
        }

        public void OnUIConfirm()
        {
            _audioSFX?.PlayUIConfirm();
        }

        public void OnUICancel()
        {
            _audioSFX?.PlayUICancel();
        }

        #endregion

        #region Direct Access (for custom effects)

        public CombatVFX VFX => _vfx ?? CombatVFX.Instance;
        public DamageNumbers DamageNumbers => _damageNumbers ?? DamageNumbers.Instance;
        public CombatCameraEffects CameraEffects => _cameraEffects ?? CombatCameraEffects.Instance;
        public CombatAudioSFX AudioSFX => _audioSFX ?? CombatAudioSFX.Instance;

        #endregion

        #region Utility

        private IEnumerator DelayedEffect(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        #endregion
    }
}
