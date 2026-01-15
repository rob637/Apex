using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ApexCitadels.Audio
{
    /// <summary>
    /// Sound effect manager with object pooling for efficient audio playback.
    /// Handles all game sound effects with categorization and spatial audio support.
    /// </summary>
    public class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance { get; private set; }

        [Header("Audio Source Pool")]
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private int maxPoolSize = 50;
        [SerializeField] private AudioSource audioSourcePrefab;

        [Header("Sound Libraries")]
        [SerializeField] private SFXLibrary sfxLibrary;

        [Header("Settings")]
        [SerializeField] private float defaultPitch = 1f;
        [SerializeField] private float pitchVariation = 0.05f;
        [SerializeField] private float minSoundInterval = 0.05f;

        // Object pool
        private Queue<AudioSource> availableSources = new Queue<AudioSource>();
        private List<AudioSource> activeSources = new List<AudioSource>();
        private Transform poolParent;

        // Sound throttling
        private Dictionary<string, float> lastPlayTime = new Dictionary<string, float>();

        // Events
        public event Action<string> OnSFXPlayed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            
            InitializePool();
        }

        private void Update()
        {
            // Return finished sources to pool
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                if (activeSources[i] != null && !activeSources[i].isPlaying)
                {
                    ReturnToPool(activeSources[i]);
                    activeSources.RemoveAt(i);
                }
            }
        }

        #region Pool Management

        private void InitializePool()
        {
            poolParent = new GameObject("SFX_Pool").transform;
            poolParent.SetParent(transform);

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreatePooledSource();
            }
        }

        private AudioSource CreatePooledSource()
        {
            AudioSource source;
            
            if (audioSourcePrefab != null)
            {
                source = Instantiate(audioSourcePrefab, poolParent);
            }
            else
            {
                GameObject obj = new GameObject("SFX_Source");
                obj.transform.SetParent(poolParent);
                source = obj.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.outputAudioMixerGroup = AudioManager.Instance?.SFXGroup;
            source.gameObject.SetActive(false);
            availableSources.Enqueue(source);
            
            return source;
        }

        private AudioSource GetFromPool()
        {
            if (availableSources.Count == 0)
            {
                if (activeSources.Count < maxPoolSize)
                {
                    return CreatePooledSource();
                }
                else
                {
                    // Steal oldest active source
                    AudioSource oldest = activeSources[0];
                    oldest.Stop();
                    activeSources.RemoveAt(0);
                    return oldest;
                }
            }

            AudioSource source = availableSources.Dequeue();
            source.gameObject.SetActive(true);
            return source;
        }

        private void ReturnToPool(AudioSource source)
        {
            if (source == null) return;

            source.Stop();
            source.clip = null;
            source.transform.SetParent(poolParent);
            source.transform.localPosition = Vector3.zero;
            source.gameObject.SetActive(false);
            availableSources.Enqueue(source);
        }

        #endregion

        #region Play Methods

        /// <summary>
        /// Play a sound effect by ID
        /// </summary>
        public AudioSource Play(string sfxId, float volumeMultiplier = 1f)
        {
            SFXEntry entry = GetSFXEntry(sfxId);
            if (entry == null) return null;

            return PlayInternal(entry, Vector3.zero, false, volumeMultiplier);
        }

        /// <summary>
        /// Play a sound effect at a 3D position
        /// </summary>
        public AudioSource PlayAt(string sfxId, Vector3 position, float volumeMultiplier = 1f)
        {
            SFXEntry entry = GetSFXEntry(sfxId);
            if (entry == null) return null;

            return PlayInternal(entry, position, true, volumeMultiplier);
        }

        /// <summary>
        /// Play a sound effect attached to a transform
        /// </summary>
        public AudioSource PlayAttached(string sfxId, Transform parent, float volumeMultiplier = 1f)
        {
            SFXEntry entry = GetSFXEntry(sfxId);
            if (entry == null) return null;

            AudioSource source = PlayInternal(entry, parent.position, true, volumeMultiplier);
            if (source != null)
            {
                source.transform.SetParent(parent);
            }
            return source;
        }

        /// <summary>
        /// Play a random sound from a category
        /// </summary>
        public AudioSource PlayRandom(SFXCategory category, float volumeMultiplier = 1f)
        {
            if (sfxLibrary == null) return null;

            List<SFXEntry> entries = sfxLibrary.GetByCategory(category);
            if (entries == null || entries.Count == 0) return null;

            SFXEntry entry = entries[UnityEngine.Random.Range(0, entries.Count)];
            return PlayInternal(entry, Vector3.zero, false, volumeMultiplier);
        }

        /// <summary>
        /// Play a random sound from a category at a position
        /// </summary>
        public AudioSource PlayRandomAt(SFXCategory category, Vector3 position, float volumeMultiplier = 1f)
        {
            if (sfxLibrary == null) return null;

            List<SFXEntry> entries = sfxLibrary.GetByCategory(category);
            if (entries == null || entries.Count == 0) return null;

            SFXEntry entry = entries[UnityEngine.Random.Range(0, entries.Count)];
            return PlayInternal(entry, position, true, volumeMultiplier);
        }

        /// <summary>
        /// Play a clip directly (without library lookup)
        /// </summary>
        public AudioSource PlayClip(AudioClip clip, float volume = 1f, float pitch = 1f, bool is3D = false, Vector3 position = default)
        {
            if (clip == null) return null;

            AudioSource source = GetFromPool();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = is3D && AudioManager.Instance.Is3DAudioEnabled ? 1f : 0f;
            
            if (is3D)
            {
                source.transform.position = position;
            }

            source.Play();
            activeSources.Add(source);

            return source;
        }

        private AudioSource PlayInternal(SFXEntry entry, Vector3 position, bool is3D, float volumeMultiplier)
        {
            // Throttle rapid sounds
            if (!CanPlaySound(entry.id))
            {
                return null;
            }

            AudioClip clip = entry.GetRandomClip();
            if (clip == null) return null;

            AudioSource source = GetFromPool();
            
            // Configure source
            source.clip = clip;
            source.volume = entry.volume * volumeMultiplier;
            source.pitch = entry.randomizePitch 
                ? defaultPitch + UnityEngine.Random.Range(-pitchVariation, pitchVariation)
                : entry.pitch;
            source.loop = entry.loop;
            source.priority = (int)entry.priority;
            source.spatialBlend = is3D && AudioManager.Instance.Is3DAudioEnabled ? 1f : 0f;
            
            // 3D sound settings
            if (is3D)
            {
                source.transform.position = position;
                source.minDistance = entry.minDistance;
                source.maxDistance = entry.maxDistance;
                source.rolloffMode = AudioRolloffMode.Linear;
            }

            // Set mixer group based on category
            source.outputAudioMixerGroup = GetMixerGroupForCategory(entry.category);

            source.Play();
            activeSources.Add(source);

            // Record play time for throttling
            lastPlayTime[entry.id] = Time.time;

            // Fire event
            OnSFXPlayed?.Invoke(entry.id);

            // Haptic feedback for impact sounds
            if (entry.triggerHaptic)
            {
                TriggerHapticForCategory(entry.category);
            }

            return source;
        }

        private bool CanPlaySound(string sfxId)
        {
            if (!lastPlayTime.ContainsKey(sfxId)) return true;
            return Time.time - lastPlayTime[sfxId] >= minSoundInterval;
        }

        private SFXEntry GetSFXEntry(string sfxId)
        {
            if (sfxLibrary == null)
            {
                Debug.LogWarning($"[SFXManager] No SFX library assigned");
                return null;
            }

            SFXEntry entry = sfxLibrary.GetEntry(sfxId);
            if (entry == null)
            {
                Debug.LogWarning($"[SFXManager] SFX not found: {sfxId}");
            }
            return entry;
        }

        private AudioMixerGroup GetMixerGroupForCategory(SFXCategory category)
        {
            if (AudioManager.Instance == null) return null;

            switch (category)
            {
                case SFXCategory.UI:
                    return AudioManager.Instance.UIGroup;
                case SFXCategory.Voice:
                    return AudioManager.Instance.VoiceGroup;
                case SFXCategory.Ambient:
                    return AudioManager.Instance.AmbientGroup;
                default:
                    return AudioManager.Instance.SFXGroup;
            }
        }

        private void TriggerHapticForCategory(SFXCategory category)
        {
            if (AudioManager.Instance == null) return;

            switch (category)
            {
                case SFXCategory.Combat:
                case SFXCategory.Explosion:
                    AudioManager.Instance.PlayHapticHeavy();
                    break;
                case SFXCategory.Building:
                case SFXCategory.Resource:
                    AudioManager.Instance.PlayHapticMedium();
                    break;
                case SFXCategory.UI:
                    AudioManager.Instance.PlayHapticLight();
                    break;
            }
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// Stop all playing sound effects
        /// </summary>
        public void StopAll()
        {
            foreach (var source in activeSources)
            {
                if (source != null)
                {
                    ReturnToPool(source);
                }
            }
            activeSources.Clear();
        }

        /// <summary>
        /// Stop a specific sound effect
        /// </summary>
        public void Stop(AudioSource source)
        {
            if (source == null) return;
            
            if (activeSources.Contains(source))
            {
                activeSources.Remove(source);
                ReturnToPool(source);
            }
        }

        /// <summary>
        /// Pause all playing sound effects
        /// </summary>
        public void PauseAll()
        {
            foreach (var source in activeSources)
            {
                if (source != null && source.isPlaying)
                {
                    source.Pause();
                }
            }
        }

        /// <summary>
        /// Resume all paused sound effects
        /// </summary>
        public void ResumeAll()
        {
            foreach (var source in activeSources)
            {
                if (source != null)
                {
                    source.UnPause();
                }
            }
        }

        /// <summary>
        /// Get number of currently playing sounds
        /// </summary>
        public int GetPlayingCount()
        {
            return activeSources.Count;
        }

        #endregion

        #region Convenience Methods - Combat

        public void PlayAttack() => Play("attack_swing");
        public void PlayAttackHit() => Play("attack_hit");
        public void PlayAttackMiss() => Play("attack_miss");
        public void PlayDefenseBlock() => Play("defense_block");
        public void PlayDamage() => Play("damage_received");
        public void PlayDeath() => Play("death");
        public void PlayVictory() => Play("victory");
        public void PlayDefeat() => Play("defeat");
        public void PlayExplosion(Vector3 position) => PlayAt("explosion", position);
        public void PlayTurretFire(Vector3 position) => PlayAt("turret_fire", position);

        #endregion

        #region Convenience Methods - Building

        public void PlayBlockPlace() => Play("block_place");
        public void PlayBlockRemove() => Play("block_remove");
        public void PlayBlockRotate() => Play("block_rotate");
        public void PlayBlockSnap() => Play("block_snap");
        public void PlayBuildingComplete() => Play("building_complete");
        public void PlayBuildingDestroy(Vector3 position) => PlayAt("building_destroy", position);

        #endregion

        #region Convenience Methods - Resources

        public void PlayResourceCollect() => Play("resource_collect");
        public void PlayResourceDrop() => Play("resource_drop");
        public void PlayGoldCollect() => Play("gold_collect");
        public void PlayGemCollect() => Play("gem_collect");
        public void PlayXPGain() => Play("xp_gain");

        #endregion

        #region Convenience Methods - UI

        public void PlayButtonClick() => Play("ui_click");
        public void PlayButtonHover() => Play("ui_hover");
        public void PlayPanelOpen() => Play("ui_panel_open");
        public void PlayPanelClose() => Play("ui_panel_close");
        public void PlayTabSwitch() => Play("ui_tab_switch");
        public void PlayToggle() => Play("ui_toggle");
        public void PlaySlider() => Play("ui_slider");
        public void PlayNotification() => Play("ui_notification");
        public void PlayError() => Play("ui_error");
        public void PlaySuccess() => Play("ui_success");
        public void PlayPurchase() => Play("ui_purchase");
        public void PlayRewardClaim() => Play("ui_reward_claim");

        #endregion

        #region Convenience Methods - Social

        public void PlayMessageSent() => Play("message_sent");
        public void PlayMessageReceived() => Play("message_received");
        public void PlayFriendOnline() => Play("friend_online");
        public void PlayGiftReceived() => Play("gift_received");
        public void PlayAllianceJoin() => Play("alliance_join");

        #endregion

        #region Convenience Methods - Achievements

        public void PlayAchievementUnlock() => Play("achievement_unlock");
        public void PlayLevelUp() => Play("level_up");
        public void PlayDailyReward() => Play("daily_reward");
        public void PlaySeasonReward() => Play("season_reward");
        public void PlayMilestone() => Play("milestone");

        #endregion

        #region Convenience Methods - Territory

        public void PlayTerritoryClaim() => Play("territory_claim");
        public void PlayTerritoryLost() => Play("territory_lost");
        public void PlayTerritoryUnderAttack() => Play("territory_under_attack");
        public void PlayWarStart() => Play("war_start");
        public void PlayWarEnd() => Play("war_end");

        #endregion

        #region Convenience Methods - Misc

        public void PlayCountdown() => Play("countdown");
        public void PlayTimerWarning() => Play("timer_warning");
        public void PlayEventStart() => Play("event_start");
        public void PlayEventEnd() => Play("event_end");
        public void PlayTutorialStep() => Play("tutorial_step");
        public void PlayTypewriter() => Play("typewriter");

        #endregion
    }

    /// <summary>
    /// Categories for sound effects
    /// </summary>
    public enum SFXCategory
    {
        Combat,
        Building,
        Resource,
        UI,
        Social,
        Achievement,
        Territory,
        Ambient,
        Voice,
        Explosion,
        Music,
        Misc
    }

    /// <summary>
    /// Priority levels for sound effects
    /// </summary>
    public enum SFXPriority
    {
        Low = 256,
        Normal = 128,
        High = 64,
        Critical = 0
    }

    /// <summary>
    /// Individual sound effect entry
    /// </summary>
    [Serializable]
    public class SFXEntry
    {
        public string id;
        public string displayName;
        public SFXCategory category;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool randomizePitch = true;
        public bool loop = false;
        public bool triggerHaptic = false;
        public SFXPriority priority = SFXPriority.Normal;
        public float minDistance = 1f;
        public float maxDistance = 50f;

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }
    }

    /// <summary>
    /// ScriptableObject containing all game sound effects
    /// </summary>
    [CreateAssetMenu(fileName = "SFXLibrary", menuName = "Apex Citadels/Audio/SFX Library")]
    public class SFXLibrary : ScriptableObject
    {
        [SerializeField] private List<SFXEntry> entries = new List<SFXEntry>();
        
        private Dictionary<string, SFXEntry> entryLookup;
        private Dictionary<SFXCategory, List<SFXEntry>> categoryLookup;

        private void OnEnable()
        {
            BuildLookups();
        }

        private void BuildLookups()
        {
            entryLookup = new Dictionary<string, SFXEntry>();
            categoryLookup = new Dictionary<SFXCategory, List<SFXEntry>>();

            foreach (SFXCategory category in Enum.GetValues(typeof(SFXCategory)))
            {
                categoryLookup[category] = new List<SFXEntry>();
            }

            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.id))
                {
                    entryLookup[entry.id] = entry;
                    categoryLookup[entry.category].Add(entry);
                }
            }
        }

        public SFXEntry GetEntry(string id)
        {
            if (entryLookup == null) BuildLookups();
            
            entryLookup.TryGetValue(id, out SFXEntry entry);
            return entry;
        }

        public List<SFXEntry> GetByCategory(SFXCategory category)
        {
            if (categoryLookup == null) BuildLookups();
            
            categoryLookup.TryGetValue(category, out List<SFXEntry> entries);
            return entries;
        }

        public List<SFXEntry> GetAllEntries() => entries;

        #if UNITY_EDITOR
        /// <summary>
        /// Create default entries for all expected sounds
        /// </summary>
        [ContextMenu("Generate Default Entries")]
        public void GenerateDefaultEntries()
        {
            entries.Clear();

            // Combat sounds
            AddEntry("attack_swing", "Attack Swing", SFXCategory.Combat);
            AddEntry("attack_hit", "Attack Hit", SFXCategory.Combat, triggerHaptic: true);
            AddEntry("attack_miss", "Attack Miss", SFXCategory.Combat);
            AddEntry("defense_block", "Defense Block", SFXCategory.Combat, triggerHaptic: true);
            AddEntry("damage_received", "Damage Received", SFXCategory.Combat, triggerHaptic: true);
            AddEntry("death", "Death", SFXCategory.Combat);
            AddEntry("victory", "Victory", SFXCategory.Combat);
            AddEntry("defeat", "Defeat", SFXCategory.Combat);
            AddEntry("explosion", "Explosion", SFXCategory.Explosion, triggerHaptic: true);
            AddEntry("turret_fire", "Turret Fire", SFXCategory.Combat);

            // Building sounds
            AddEntry("block_place", "Block Place", SFXCategory.Building, triggerHaptic: true);
            AddEntry("block_remove", "Block Remove", SFXCategory.Building);
            AddEntry("block_rotate", "Block Rotate", SFXCategory.Building);
            AddEntry("block_snap", "Block Snap", SFXCategory.Building);
            AddEntry("building_complete", "Building Complete", SFXCategory.Building);
            AddEntry("building_destroy", "Building Destroy", SFXCategory.Building, triggerHaptic: true);

            // Resource sounds
            AddEntry("resource_collect", "Resource Collect", SFXCategory.Resource);
            AddEntry("resource_drop", "Resource Drop", SFXCategory.Resource);
            AddEntry("gold_collect", "Gold Collect", SFXCategory.Resource);
            AddEntry("gem_collect", "Gem Collect", SFXCategory.Resource);
            AddEntry("xp_gain", "XP Gain", SFXCategory.Resource);

            // UI sounds
            AddEntry("ui_click", "UI Click", SFXCategory.UI);
            AddEntry("ui_hover", "UI Hover", SFXCategory.UI, volume: 0.5f);
            AddEntry("ui_panel_open", "Panel Open", SFXCategory.UI);
            AddEntry("ui_panel_close", "Panel Close", SFXCategory.UI);
            AddEntry("ui_tab_switch", "Tab Switch", SFXCategory.UI);
            AddEntry("ui_toggle", "Toggle", SFXCategory.UI);
            AddEntry("ui_slider", "Slider", SFXCategory.UI, volume: 0.3f);
            AddEntry("ui_notification", "Notification", SFXCategory.UI);
            AddEntry("ui_error", "Error", SFXCategory.UI, triggerHaptic: true);
            AddEntry("ui_success", "Success", SFXCategory.UI, triggerHaptic: true);
            AddEntry("ui_purchase", "Purchase", SFXCategory.UI);
            AddEntry("ui_reward_claim", "Reward Claim", SFXCategory.UI, triggerHaptic: true);

            // Social sounds
            AddEntry("message_sent", "Message Sent", SFXCategory.Social);
            AddEntry("message_received", "Message Received", SFXCategory.Social);
            AddEntry("friend_online", "Friend Online", SFXCategory.Social);
            AddEntry("gift_received", "Gift Received", SFXCategory.Social);
            AddEntry("alliance_join", "Alliance Join", SFXCategory.Social);

            // Achievement sounds
            AddEntry("achievement_unlock", "Achievement Unlock", SFXCategory.Achievement, triggerHaptic: true);
            AddEntry("level_up", "Level Up", SFXCategory.Achievement, triggerHaptic: true);
            AddEntry("daily_reward", "Daily Reward", SFXCategory.Achievement);
            AddEntry("season_reward", "Season Reward", SFXCategory.Achievement, triggerHaptic: true);
            AddEntry("milestone", "Milestone", SFXCategory.Achievement, triggerHaptic: true);

            // Territory sounds
            AddEntry("territory_claim", "Territory Claim", SFXCategory.Territory, triggerHaptic: true);
            AddEntry("territory_lost", "Territory Lost", SFXCategory.Territory, triggerHaptic: true);
            AddEntry("territory_under_attack", "Territory Under Attack", SFXCategory.Territory);
            AddEntry("war_start", "War Start", SFXCategory.Territory, triggerHaptic: true);
            AddEntry("war_end", "War End", SFXCategory.Territory);

            // Misc sounds
            AddEntry("countdown", "Countdown", SFXCategory.Misc);
            AddEntry("timer_warning", "Timer Warning", SFXCategory.Misc);
            AddEntry("event_start", "Event Start", SFXCategory.Misc);
            AddEntry("event_end", "Event End", SFXCategory.Misc);
            AddEntry("tutorial_step", "Tutorial Step", SFXCategory.Misc);
            AddEntry("typewriter", "Typewriter", SFXCategory.Misc, volume: 0.5f);

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[SFXLibrary] Generated {entries.Count} default entries");
        }

        private void AddEntry(string id, string name, SFXCategory category, float volume = 1f, bool triggerHaptic = false)
        {
            entries.Add(new SFXEntry
            {
                id = id,
                displayName = name,
                category = category,
                volume = volume,
                pitch = 1f,
                randomizePitch = true,
                loop = false,
                triggerHaptic = triggerHaptic,
                priority = SFXPriority.Normal,
                minDistance = 1f,
                maxDistance = 50f,
                clips = new AudioClip[0]
            });
        }
        #endif
    }
}
