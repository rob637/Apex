// ============================================================================
// APEX CITADELS - AUDIO MANAGER
// Manages all game audio: music, SFX, ambient sounds
// ============================================================================
using UnityEngine;
using System.Collections.Generic;
using ApexCitadels.Core;

namespace ApexCitadels.PC.Visual
{
    /// <summary>
    /// Centralized audio management for all game sounds.
    /// Handles music, sound effects, ambient audio, and UI sounds.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        private AudioSource musicSource;
        private AudioSource ambientSource;
        private AudioSource sfxSource;
        private AudioSource uiSource;

        [Header("Volume Settings")]
        public float masterVolume = 1f;
        public float musicVolume = 0.5f;
        public float sfxVolume = 0.8f;
        public float ambientVolume = 0.6f;
        public float uiVolume = 0.7f;

        [Header("Audio Clips Cache")]
        private Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> musicCache = new Dictionary<string, AudioClip>();

        [Header("Ambient State")]
        private string currentAmbient = "";
        private bool isPlayingMusic = false;

        // Sound categories for organization
        public enum SFXCategory
        {
            UI,
            Combat,
            Building,
            Character,
            Environment,
            Effects
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Move to root before DontDestroyOnLoad (required by Unity)
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);
                InitializeAudioSources();
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            LoadAudioClips();
            StartAmbient("ENV25_village_day");
            
            ApexLogger.Log("[OK] Audio manager initialized", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Creates all required audio sources
        /// </summary>
        private void InitializeAudioSources()
        {
            // Music source - loops, lower priority
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.priority = 128;
            musicSource.volume = musicVolume * masterVolume;

            // Ambient source - loops
            GameObject ambientObj = new GameObject("AmbientSource");
            ambientObj.transform.SetParent(transform);
            ambientSource = ambientObj.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
            ambientSource.priority = 100;
            ambientSource.volume = ambientVolume * masterVolume;

            // SFX source - one-shots
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.priority = 50;
            sfxSource.volume = sfxVolume * masterVolume;

            // UI source - immediate, high priority
            GameObject uiObj = new GameObject("UISource");
            uiObj.transform.SetParent(transform);
            uiSource = uiObj.AddComponent<AudioSource>();
            uiSource.loop = false;
            uiSource.playOnAwake = false;
            uiSource.priority = 10;
            uiSource.volume = uiVolume * masterVolume;

            ApexLogger.LogVerbose("Created 4 audio sources", ApexLogger.LogCategory.General);
        }

        /// <summary>
        /// Loads all audio clips from Resources
        /// </summary>
        private void LoadAudioClips()
        {
            // Load all SFX from Resources/Audio/SFX
            AudioClip[] clips = UnityEngine.Resources.LoadAll<AudioClip>("Audio/SFX");
            foreach (var clip in clips)
            {
                string key = clip.name.Replace("SFX-", "");
                sfxCache[key] = clip;
            }

            // Load music
            AudioClip[] music = UnityEngine.Resources.LoadAll<AudioClip>("Audio/Music");
            foreach (var clip in music)
            {
                musicCache[clip.name] = clip;
            }

            ApexLogger.LogVerbose($"Loaded {sfxCache.Count} SFX, {musicCache.Count} music tracks", ApexLogger.LogCategory.General);
        }

        #region Public API - Sound Playback

        /// <summary>
        /// Plays a UI sound effect
        /// </summary>
        public void PlayUI(string soundId)
        {
            string key = soundId.StartsWith("UI") ? soundId : $"UI{soundId}";
            PlaySFX(key, uiSource);
        }

        /// <summary>
        /// Play common UI sounds by action
        /// </summary>
        public void PlayButtonClick() => PlayUI("UI01_button_click_standard");
        public void PlayButtonConfirm() => PlayUI("UI02_button_click_confirm");
        public void PlayButtonCancel() => PlayUI("UI03_button_click_cancel");
        public void PlayButtonHover() => PlayUI("UI05_button_hover");
        public void PlayPanelOpen() => PlayUI("UI11_panel_open");
        public void PlayPanelClose() => PlayUI("UI12_panel_close");
        public void PlayNotification() => PlayUI("UI19_notification_info");
        public void PlaySuccess() => PlayUI("UI20_notification_success");
        public void PlayWarning() => PlayUI("UI21_notification_warning");
        public void PlayError() => PlayUI("UI22_notification_error");
        public void PlayLevelUp() => PlayUI("UI33_level_up");
        public void PlayReward() => PlayUI("UI34_reward_chest_open");
        public void PlayCoinCollect() => PlayUI("UI28_coin_multiple");

        /// <summary>
        /// Plays a combat sound effect
        /// </summary>
        public void PlayCombat(string soundId)
        {
            string key = soundId.StartsWith("CMB") ? soundId : $"CMB{soundId}";
            PlaySFX(key, sfxSource);
        }

        /// <summary>
        /// Play common combat sounds
        /// </summary>
        public void PlaySwordSwing() => PlayCombat("CMB01_sword_swing");
        public void PlaySwordHit() => PlayCombat("CMB03_sword_hit_flesh");
        public void PlayArrowFire() => PlayCombat("CMB14_bow_release");
        public void PlayArrowHit() => PlayCombat("CMB18_arrow_hit_flesh");
        public void PlayShieldBlock() => PlayCombat("CMB32_shield_block_sword");
        public void PlaySpellCast() => PlayCombat("CMB38_spell_cast_fire");
        public void PlayCriticalHit() => PlayCombat("CMB51_critical_hit");
        public void PlayVictory() => PlayCombat("CMB60_victory_horn");
        public void PlayWarHorn() => PlayCombat("CMB59_war_horn");

        /// <summary>
        /// Plays a building sound effect
        /// </summary>
        public void PlayBuilding(string soundId)
        {
            string key = soundId.StartsWith("BLD") ? soundId : $"BLD{soundId}";
            PlaySFX(key, sfxSource);
        }

        /// <summary>
        /// Play common building sounds
        /// </summary>
        public void PlayHammerHit() => PlayBuilding("BLD01_hammer_hit_wood");
        public void PlayConstruction() => PlayBuilding("BLD06_construction_ambient");
        public void PlayBuildingComplete() => PlayBuilding("BLD13_building_complete");
        public void PlayBuildingUpgrade() => PlayBuilding("BLD15_building_upgrade_complete");
        public void PlayGateOpen() => PlayBuilding("BLD27_gate_open");
        public void PlayGateClose() => PlayBuilding("BLD28_gate_close");
        public void PlayForge() => PlayBuilding("BLD35_forge_fire");

        /// <summary>
        /// Plays an environment sound effect
        /// </summary>
        public void PlayEnvironment(string soundId)
        {
            string key = soundId.StartsWith("ENV") ? soundId : $"ENV{soundId}";
            PlaySFX(key, sfxSource);
        }

        /// <summary>
        /// Plays an effects sound
        /// </summary>
        public void PlayEffect(string soundId)
        {
            string key = soundId.StartsWith("FX") ? soundId : $"FX{soundId}";
            PlaySFX(key, sfxSource);
        }

        /// <summary>
        /// Play common effect sounds
        /// </summary>
        public void PlayMagicSparkle() => PlayEffect("FX01_magic_sparkle");
        public void PlayTeleport() => PlayEffect("FX04_teleport_in");
        public void PlayPowerUp() => PlayEffect("FX06_power_up");
        public void PlayQuestComplete() => PlayEffect("FX19_quest_complete");
        public void PlaySecretFound() => PlayEffect("FX24_secret_found");

        /// <summary>
        /// Generic SFX playback
        /// </summary>
        public void PlaySFX(string soundId, AudioSource source = null)
        {
            if (source == null) source = sfxSource;
            
            // Try direct key first, then with prefix stripped
            if (!sfxCache.TryGetValue(soundId, out AudioClip clip))
            {
                // Try without prefix
                foreach (var kvp in sfxCache)
                {
                    if (kvp.Key.EndsWith(soundId) || kvp.Key.Contains(soundId))
                    {
                        clip = kvp.Value;
                        break;
                    }
                }
            }

            if (clip != null)
            {
                source.PlayOneShot(clip, source.volume);
            }
            else
            {
                ApexLogger.LogWarning($"SFX not found: {soundId}", ApexLogger.LogCategory.General);
            }
        }

        /// <summary>
        /// Plays a 3D positioned sound
        /// </summary>
        public void PlaySFXAtPosition(string soundId, Vector3 position, float maxDistance = 50f)
        {
            if (!sfxCache.TryGetValue(soundId, out AudioClip clip))
            {
                foreach (var kvp in sfxCache)
                {
                    if (kvp.Key.Contains(soundId))
                    {
                        clip = kvp.Value;
                        break;
                    }
                }
            }

            if (clip != null)
            {
                GameObject tempAudio = new GameObject("TempAudio");
                tempAudio.transform.position = position;
                
                AudioSource source = tempAudio.AddComponent<AudioSource>();
                source.clip = clip;
                source.volume = sfxVolume * masterVolume;
                source.spatialBlend = 1f; // Full 3D
                source.maxDistance = maxDistance;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.Play();
                
                Destroy(tempAudio, clip.length + 0.1f);
            }
        }

        #endregion

        #region Music Control

        /// <summary>
        /// Starts playing background music
        /// </summary>
        public void PlayMusic(string trackName, bool fadeIn = true)
        {
            if (musicCache.TryGetValue(trackName, out AudioClip clip))
            {
                if (fadeIn && isPlayingMusic)
                {
                    StartCoroutine(CrossfadeMusic(clip));
                }
                else
                {
                    musicSource.clip = clip;
                    musicSource.Play();
                    isPlayingMusic = true;
                }
            }
            else
            {
                ApexLogger.LogWarning($"Music not found: {trackName}", ApexLogger.LogCategory.General);
            }
        }

        /// <summary>
        /// Stops music with optional fade out
        /// </summary>
        public void StopMusic(bool fadeOut = true)
        {
            if (fadeOut)
            {
                StartCoroutine(FadeOutMusic());
            }
            else
            {
                musicSource.Stop();
                isPlayingMusic = false;
            }
        }

        private System.Collections.IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            float duration = 2f;
            float startVolume = musicSource.volume;
            
            // Fade out
            for (float t = 0; t < duration / 2f; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0, t / (duration / 2f));
                yield return null;
            }
            
            // Switch clip
            musicSource.clip = newClip;
            musicSource.Play();
            
            // Fade in
            for (float t = 0; t < duration / 2f; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(0, musicVolume * masterVolume, t / (duration / 2f));
                yield return null;
            }
            
            musicSource.volume = musicVolume * masterVolume;
        }

        private System.Collections.IEnumerator FadeOutMusic()
        {
            float duration = 1.5f;
            float startVolume = musicSource.volume;
            
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0, t / duration);
                yield return null;
            }
            
            musicSource.Stop();
            musicSource.volume = musicVolume * masterVolume;
            isPlayingMusic = false;
        }

        #endregion

        #region Ambient Audio

        /// <summary>
        /// Starts ambient audio loop
        /// </summary>
        public void StartAmbient(string ambientId)
        {
            if (currentAmbient == ambientId) return;
            
            string key = ambientId.StartsWith("ENV") ? ambientId : $"ENV{ambientId}";
            
            if (!sfxCache.TryGetValue(key, out AudioClip clip))
            {
                foreach (var kvp in sfxCache)
                {
                    if (kvp.Key.Contains(ambientId))
                    {
                        clip = kvp.Value;
                        break;
                    }
                }
            }

            if (clip != null)
            {
                StartCoroutine(CrossfadeAmbient(clip));
                currentAmbient = ambientId;
            }
        }

        private System.Collections.IEnumerator CrossfadeAmbient(AudioClip newClip)
        {
            float duration = 3f;
            float startVolume = ambientSource.volume;
            
            // Fade out current
            if (ambientSource.isPlaying)
            {
                for (float t = 0; t < duration / 2f; t += Time.deltaTime)
                {
                    ambientSource.volume = Mathf.Lerp(startVolume, 0, t / (duration / 2f));
                    yield return null;
                }
            }
            
            // Switch
            ambientSource.clip = newClip;
            ambientSource.Play();
            
            // Fade in
            for (float t = 0; t < duration / 2f; t += Time.deltaTime)
            {
                ambientSource.volume = Mathf.Lerp(0, ambientVolume * masterVolume, t / (duration / 2f));
                yield return null;
            }
            
            ambientSource.volume = ambientVolume * masterVolume;
        }

        /// <summary>
        /// Sets ambient based on time of day
        /// </summary>
        public void SetAmbientForTimeOfDay(bool isDaytime)
        {
            if (isDaytime)
            {
                StartAmbient("ENV25_village_day");
            }
            else
            {
                StartAmbient("ENV26_village_night");
            }
        }

        /// <summary>
        /// Sets ambient for location
        /// </summary>
        public void SetAmbientForLocation(string location)
        {
            switch (location.ToLower())
            {
                case "forest":
                    StartAmbient("ENV14_forest_ambient");
                    break;
                case "castle":
                    StartAmbient("ENV19_castle_interior");
                    break;
                case "dungeon":
                    StartAmbient("ENV21_dungeon_ambient");
                    break;
                case "market":
                    StartAmbient("CHR15_market_ambient");
                    break;
                case "tavern":
                    StartAmbient("ENV28_tavern_ambient");
                    break;
                case "blacksmith":
                    StartAmbient("ENV27_blacksmith_ambient");
                    break;
                case "battle":
                    StartAmbient("CMB56_battle_ambient_light");
                    break;
                default:
                    StartAmbient("ENV25_village_day");
                    break;
            }
        }

        #endregion

        #region Volume Control

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            musicSource.volume = musicVolume * masterVolume;
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            sfxSource.volume = sfxVolume * masterVolume;
        }

        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            ambientSource.volume = ambientVolume * masterVolume;
        }

        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            uiSource.volume = uiVolume * masterVolume;
        }

        private void UpdateAllVolumes()
        {
            musicSource.volume = musicVolume * masterVolume;
            sfxSource.volume = sfxVolume * masterVolume;
            ambientSource.volume = ambientVolume * masterVolume;
            uiSource.volume = uiVolume * masterVolume;
        }

        public void MuteAll(bool mute)
        {
            musicSource.mute = mute;
            sfxSource.mute = mute;
            ambientSource.mute = mute;
            uiSource.mute = mute;
        }

        #endregion
    }
}
