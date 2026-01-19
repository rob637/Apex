using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.Audio
{
    /// <summary>
    /// AAA UI Sound Effects Manager.
    /// Provides satisfying audio feedback for all UI interactions.
    /// Features:
    /// - Categorized UI sounds
    /// - Pitch variation for variety
    /// - Volume scaling by importance
    /// - Sound pooling for performance
    /// </summary>
    public class UISoundManager : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private AudioMixerGroup uiMixerGroup;
        [SerializeField] private int poolSize = 10;
        [SerializeField] private float masterVolume = 1f;
        
        [Header("Sound Library")]
        [SerializeField] private UISoundLibrary soundLibrary;
        
        // Singleton
        private static UISoundManager _instance;
        public static UISoundManager Instance => _instance;
        
        // Sound pool
        private List<AudioSource> _sourcePool;
        private int _poolIndex;
        
        // Cooldown to prevent sound spam
        private Dictionary<UISoundType, float> _lastPlayTime;
        private const float MIN_REPEAT_INTERVAL = 0.05f;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializePool();
            _lastPlayTime = new Dictionary<UISoundType, float>();
        }
        
        private void InitializePool()
        {
            _sourcePool = new List<AudioSource>();
            
            for (int i = 0; i < poolSize; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0; // 2D sound
                
                if (uiMixerGroup != null)
                {
                    source.outputAudioMixerGroup = uiMixerGroup;
                }
                
                _sourcePool.Add(source);
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Play a UI sound by type
        /// </summary>
        public void Play(UISoundType type)
        {
            if (soundLibrary == null) return;
            
            // Check cooldown
            if (_lastPlayTime.TryGetValue(type, out float lastTime))
            {
                if (Time.unscaledTime - lastTime < MIN_REPEAT_INTERVAL)
                {
                    return;
                }
            }
            _lastPlayTime[type] = Time.unscaledTime;
            
            var sound = soundLibrary.GetSound(type);
            if (sound == null || sound.clip == null) return;
            
            PlaySound(sound);
        }
        
        /// <summary>
        /// Play a custom audio clip
        /// </summary>
        public void PlayClip(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            
            var source = GetNextSource();
            source.clip = clip;
            source.volume = volume * masterVolume;
            source.pitch = pitch;
            source.Play();
        }
        
        /// <summary>
        /// Play button click
        /// </summary>
        public void PlayClick() => Play(UISoundType.ButtonClick);
        
        /// <summary>
        /// Play button hover
        /// </summary>
        public void PlayHover() => Play(UISoundType.ButtonHover);
        
        /// <summary>
        /// Play error/invalid action
        /// </summary>
        public void PlayError() => Play(UISoundType.Error);
        
        /// <summary>
        /// Play success/confirm
        /// </summary>
        public void PlaySuccess() => Play(UISoundType.Success);
        
        /// <summary>
        /// Play notification
        /// </summary>
        public void PlayNotification() => Play(UISoundType.Notification);
        
        /// <summary>
        /// Play purchase/spend
        /// </summary>
        public void PlayPurchase() => Play(UISoundType.Purchase);
        
        /// <summary>
        /// Set master UI volume
        /// </summary>
        public void SetVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
        }
        
        #endregion
        
        #region Internal
        
        private void PlaySound(UISound sound)
        {
            var source = GetNextSource();
            
            source.clip = sound.clip;
            source.volume = sound.volume * masterVolume;
            
            // Apply pitch variation
            float pitchVariation = UnityEngine.Random.Range(-sound.pitchVariation, sound.pitchVariation);
            source.pitch = sound.pitch + pitchVariation;
            
            source.Play();
        }
        
        private AudioSource GetNextSource()
        {
            var source = _sourcePool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _sourcePool.Count;
            return source;
        }
        
        #endregion
    }
    
    /// <summary>
    /// UI Sound types
    /// </summary>
    public enum UISoundType
    {
        // Buttons (SFX-UI01-06)
        ButtonClick,
        Confirm,
        Cancel,
        ButtonSpecial,
        ButtonHover,
        ButtonDisabled,
        
        // Toggles & Sliders (SFX-UI07-10)
        ToggleOn,
        ToggleOff,
        SliderTick,
        SliderEnd,
        
        // Panels & Navigation (SFX-UI11-18)
        PanelOpen,
        PanelClose,
        PopupAppear,
        PopupDismiss,
        TabSwitch,
        ScrollTick,
        DrawerOpen,
        DrawerClose,
        
        // Notifications (SFX-UI19-26)
        NotificationInfo,
        Success,
        Warning,
        NotificationError,
        Message,
        FriendNotification,
        Achievement,
        QuestComplete,
        
        // Currency & Rewards (SFX-UI27-37)
        CoinSingle,
        CoinMultiple,
        CoinLarge,
        GemCollect,
        ResourceCollect,
        XPGain,
        LevelUp,
        ChestOpen,
        ItemReveal,
        RareReward,
        LegendaryReward,
        
        // Menu Navigation (SFX-UI38-40)
        MenuNavigate,
        MenuSelect,
        MenuBack,
        
        // System (SFX-UI41-48)
        LoadingStart,
        LoadingComplete,
        Screenshot,
        CountdownTick,
        CountdownFinal,
        TypingKey,
        ErrorBuzz,
        ConfirmChime,
        
        // Legacy (for backward compatibility)
        Error,
        Notification,
        Collect,
        Spend,
        Purchase,
        Insufficient,
        PlaceBlock,
        RotateBlock,
        DeleteBlock,
        SnapBlock,
        InvalidPlacement,
        SelectUnit,
        DeployUnit,
        AttackOrder,
        DefendOrder,
        MessageReceived,
        FriendOnline,
        AllianceAlert,
        QueueAdd,
        QueueComplete,
        UnlockNew,
        MenuOpen,
        MenuClose,
        SliderMove,
        DropdownOpen,
        DropdownSelect,
        Typing,
        Countdown,
        TimerTick,
        Fanfare,
        ButtonToggleOn,
        ButtonToggleOff,
        PopupOpen,
        PopupClose
    }
    
    /// <summary>
    /// Individual UI sound data
    /// </summary>
    [Serializable]
    public class UISound
    {
        public UISoundType type;
        public AudioClip clip;
        [Range(0, 1)] public float volume = 1f;
        [Range(0.5f, 2f)] public float pitch = 1f;
        [Range(0, 0.3f)] public float pitchVariation = 0.05f;
    }
    
    /// <summary>
    /// Scriptable Object for UI sound library
    /// </summary>
    [CreateAssetMenu(fileName = "UISoundLibrary", menuName = "Apex Citadels/UI Sound Library")]
    public class UISoundLibrary : ScriptableObject
    {
        [SerializeField] private List<UISound> sounds = new List<UISound>();
        
        private Dictionary<UISoundType, UISound> _soundCache;
        
        private void OnEnable()
        {
            BuildCache();
        }
        
        private void BuildCache()
        {
            _soundCache = new Dictionary<UISoundType, UISound>();
            foreach (var sound in sounds)
            {
                _soundCache[sound.type] = sound;
            }
        }
        
        public UISound GetSound(UISoundType type)
        {
            if (_soundCache == null) BuildCache();
            _soundCache.TryGetValue(type, out var sound);
            return sound;
        }
        
        public void SetSound(UISoundType type, AudioClip clip)
        {
            if (_soundCache == null) BuildCache();
            
            if (_soundCache.TryGetValue(type, out var sound))
            {
                sound.clip = clip;
            }
            else
            {
                var newSound = new UISound
                {
                    type = type,
                    clip = clip,
                    volume = 1f,
                    pitch = 1f
                };
                sounds.Add(newSound);
                _soundCache[type] = newSound;
            }
        }
    }
    
    /// <summary>
    /// Helper component for UI elements to play sounds on interaction.
    /// Attach to any UI element with events.
    /// </summary>
    public class UIElementSound : MonoBehaviour
    {
        [Header("Sound Settings")]
        [SerializeField] private UISoundType clickSound = UISoundType.ButtonClick;
        [SerializeField] private UISoundType hoverSound = UISoundType.ButtonHover;
        [SerializeField] private bool playOnClick = true;
        [SerializeField] private bool playOnHover = true;
        
        public void OnClick()
        {
            if (playOnClick && UISoundManager.Instance != null)
            {
                UISoundManager.Instance.Play(clickSound);
            }
        }
        
        public void OnHover()
        {
            if (playOnHover && UISoundManager.Instance != null)
            {
                UISoundManager.Instance.Play(hoverSound);
            }
        }
        
        public void PlaySound(UISoundType type)
        {
            if (UISoundManager.Instance != null)
            {
                UISoundManager.Instance.Play(type);
            }
        }
    }
    
    /// <summary>
    /// Notification sound queue to prevent overlap.
    /// </summary>
    public class NotificationSoundQueue : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float minInterval = 0.3f;
        [SerializeField] private int maxQueueSize = 5;
        
        private Queue<UISoundType> _pendingQueue = new Queue<UISoundType>();
        private float _lastPlayTime;
        private bool _isProcessing;
        
        // Singleton
        private static NotificationSoundQueue _instance;
        public static NotificationSoundQueue Instance => _instance;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        /// <summary>
        /// Queue a notification sound
        /// </summary>
        public void Queue(UISoundType type)
        {
            if (_pendingQueue.Count < maxQueueSize)
            {
                _pendingQueue.Enqueue(type);
                
                if (!_isProcessing)
                {
                    StartCoroutine(ProcessQueue());
                }
            }
        }
        
        private IEnumerator ProcessQueue()
        {
            _isProcessing = true;
            
            while (_pendingQueue.Count > 0)
            {
                float elapsed = Time.unscaledTime - _lastPlayTime;
                if (elapsed < minInterval)
                {
                    yield return new WaitForSecondsRealtime(minInterval - elapsed);
                }
                
                var sound = _pendingQueue.Dequeue();
                UISoundManager.Instance?.Play(sound);
                _lastPlayTime = Time.unscaledTime;
                
                yield return null;
            }
            
            _isProcessing = false;
        }
    }
}
