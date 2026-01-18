using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.Audio
{
    /// <summary>
    /// Voice Line Manager for character and narrator speech.
    /// Supports ElevenLabs-generated voice lines.
    /// Features:
    /// - Character voice lines with lip sync markers
    /// - Narrator for tutorials and events
    /// - Voice queuing and interruption
    /// - Subtitles integration
    /// </summary>
    public class VoiceLineManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioSource narratorSource;
        
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixerGroup voiceMixerGroup;
        
        [Header("Settings")]
        [SerializeField] private float duckMusicAmount = 0.3f;
        [SerializeField] private float duckDuration = 0.3f;
        [SerializeField] private bool enableSubtitles = true;
        
        [Header("Voice Library")]
        [SerializeField] private VoiceLineLibrary voiceLibrary;
        
        // Singleton
        private static VoiceLineManager _instance;
        public static VoiceLineManager Instance => _instance;
        
        // State
        private Queue<VoiceLine> _voiceQueue = new Queue<VoiceLine>();
        private VoiceLine _currentLine;
        private bool _isPlaying;
        private Coroutine _playbackCoroutine;
        
        // Events
        public event Action<VoiceLine> OnVoiceLineStarted;
        public event Action<VoiceLine> OnVoiceLineEnded;
        public event Action<string> OnSubtitleChanged;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeAudioSources();
        }
        
        private void InitializeAudioSources()
        {
            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.playOnAwake = false;
                voiceSource.spatialBlend = 0;
            }
            
            if (narratorSource == null)
            {
                narratorSource = gameObject.AddComponent<AudioSource>();
                narratorSource.playOnAwake = false;
                narratorSource.spatialBlend = 0;
            }
            
            if (voiceMixerGroup != null)
            {
                voiceSource.outputAudioMixerGroup = voiceMixerGroup;
                narratorSource.outputAudioMixerGroup = voiceMixerGroup;
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Play a voice line by ID
        /// </summary>
        public void PlayVoiceLine(string lineId, bool interrupt = false)
        {
            var line = voiceLibrary?.GetLine(lineId);
            if (line != null)
            {
                PlayVoiceLine(line, interrupt);
            }
        }
        
        /// <summary>
        /// Play a voice line
        /// </summary>
        public void PlayVoiceLine(VoiceLine line, bool interrupt = false)
        {
            if (line == null || line.clip == null) return;
            
            if (interrupt && _isPlaying)
            {
                StopCurrentLine();
            }
            
            if (_isPlaying && !interrupt)
            {
                _voiceQueue.Enqueue(line);
                return;
            }
            
            StartVoiceLine(line);
        }
        
        /// <summary>
        /// Play a character voice line
        /// </summary>
        public void PlayCharacterVoice(CharacterType character, VoiceLineType type)
        {
            var line = voiceLibrary?.GetCharacterLine(character, type);
            if (line != null)
            {
                PlayVoiceLine(line, false);
            }
        }
        
        /// <summary>
        /// Play narrator line
        /// </summary>
        public void PlayNarrator(NarratorLineType type)
        {
            var line = voiceLibrary?.GetNarratorLine(type);
            if (line != null)
            {
                PlayNarratorLine(line);
            }
        }
        
        /// <summary>
        /// Play tutorial narration
        /// </summary>
        public void PlayTutorial(string tutorialId)
        {
            var line = voiceLibrary?.GetTutorialLine(tutorialId);
            if (line != null)
            {
                PlayNarratorLine(line);
            }
        }
        
        /// <summary>
        /// Stop current voice line
        /// </summary>
        public void Stop()
        {
            StopCurrentLine();
            _voiceQueue.Clear();
        }
        
        /// <summary>
        /// Skip current line and play next in queue
        /// </summary>
        public void SkipCurrent()
        {
            StopCurrentLine();
            PlayNextInQueue();
        }
        
        /// <summary>
        /// Set subtitles enabled
        /// </summary>
        public void SetSubtitlesEnabled(bool enabled)
        {
            enableSubtitles = enabled;
            if (!enabled)
            {
                OnSubtitleChanged?.Invoke(null);
            }
        }
        
        /// <summary>
        /// Set voice volume
        /// </summary>
        public void SetVolume(float volume)
        {
            voiceSource.volume = volume;
            narratorSource.volume = volume;
        }
        
        #endregion
        
        #region Internal
        
        private void StartVoiceLine(VoiceLine line)
        {
            _currentLine = line;
            _isPlaying = true;
            
            OnVoiceLineStarted?.Invoke(line);
            
            // Duck music
            if (MusicManager.Instance != null)
            {
                // Could implement ducking via mixer snapshots
            }
            
            _playbackCoroutine = StartCoroutine(PlayLineCoroutine(line));
        }
        
        private void PlayNarratorLine(VoiceLine line)
        {
            if (line == null || line.clip == null) return;
            
            // Stop any current narrator line
            if (narratorSource.isPlaying)
            {
                narratorSource.Stop();
            }
            
            narratorSource.clip = line.clip;
            narratorSource.volume = line.volume;
            narratorSource.Play();
            
            // Show subtitle
            if (enableSubtitles && !string.IsNullOrEmpty(line.subtitle))
            {
                OnSubtitleChanged?.Invoke(line.subtitle);
                StartCoroutine(ClearSubtitleAfterDelay(line.clip.length));
            }
        }
        
        private IEnumerator PlayLineCoroutine(VoiceLine line)
        {
            voiceSource.clip = line.clip;
            voiceSource.volume = line.volume;
            voiceSource.Play();
            
            // Show subtitle
            if (enableSubtitles && !string.IsNullOrEmpty(line.subtitle))
            {
                OnSubtitleChanged?.Invoke(line.subtitle);
            }
            
            // Wait for completion
            yield return new WaitForSeconds(line.clip.length);
            
            // Clear subtitle
            if (enableSubtitles)
            {
                OnSubtitleChanged?.Invoke(null);
            }
            
            OnVoiceLineEnded?.Invoke(line);
            
            _currentLine = null;
            _isPlaying = false;
            
            // Play next in queue
            PlayNextInQueue();
        }
        
        private IEnumerator ClearSubtitleAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            OnSubtitleChanged?.Invoke(null);
        }
        
        private void StopCurrentLine()
        {
            if (_playbackCoroutine != null)
            {
                StopCoroutine(_playbackCoroutine);
            }
            
            voiceSource.Stop();
            
            if (enableSubtitles)
            {
                OnSubtitleChanged?.Invoke(null);
            }
            
            if (_currentLine != null)
            {
                OnVoiceLineEnded?.Invoke(_currentLine);
            }
            
            _currentLine = null;
            _isPlaying = false;
        }
        
        private void PlayNextInQueue()
        {
            if (_voiceQueue.Count > 0)
            {
                var nextLine = _voiceQueue.Dequeue();
                StartVoiceLine(nextLine);
            }
        }
        
        #endregion
        
        #region Properties
        
        public bool IsPlaying => _isPlaying;
        public VoiceLine CurrentLine => _currentLine;
        public int QueuedCount => _voiceQueue.Count;
        
        #endregion
    }
    
    #region Enums
    
    public enum CharacterType
    {
        Player,
        Advisor,
        General,
        Merchant,
        Scout,
        Enemy,
        AllyLeader,
        NPC
    }
    
    public enum VoiceLineType
    {
        // General
        Greeting,
        Farewell,
        Acknowledgment,
        Question,
        
        // Combat
        AttackOrder,
        DefendOrder,
        Victory,
        Defeat,
        UnitLost,
        EnemySighted,
        Retreat,
        Charge,
        
        // Building
        ConstructionStart,
        ConstructionComplete,
        UpgradeComplete,
        NotEnoughResources,
        
        // Events
        TerritoryCapture,
        TerritoryLost,
        AllianceMessage,
        EventStart,
        Warning,
        
        // Social
        FriendRequest,
        GiftReceived,
        ChatMessage
    }
    
    public enum NarratorLineType
    {
        Welcome,
        TutorialIntro,
        TutorialComplete,
        FirstVictory,
        FirstTerritory,
        AllianceJoined,
        SeasonStart,
        SeasonEnd,
        EventAnnouncement,
        Warning,
        Achievement
    }
    
    #endregion
    
    /// <summary>
    /// Individual voice line data
    /// </summary>
    [Serializable]
    public class VoiceLine
    {
        public string id;
        public AudioClip clip;
        public string subtitle;
        [Range(0, 1)] public float volume = 1f;
        public CharacterType character;
        public VoiceLineType type;
        
        [Header("Optional")]
        public string speakerName;
        public Sprite speakerPortrait;
    }
    
    /// <summary>
    /// Scriptable Object for voice line library
    /// </summary>
    [CreateAssetMenu(fileName = "VoiceLineLibrary", menuName = "Apex Citadels/Voice Line Library")]
    public class VoiceLineLibrary : ScriptableObject
    {
        [Header("Character Lines")]
        [SerializeField] private List<CharacterVoiceSet> characterVoices;
        
        [Header("Narrator Lines")]
        [SerializeField] private List<NarratorLine> narratorLines;
        
        [Header("Tutorial Lines")]
        [SerializeField] private List<TutorialLine> tutorialLines;
        
        // Caches
        private Dictionary<string, VoiceLine> _lineCache;
        private Dictionary<(CharacterType, VoiceLineType), List<VoiceLine>> _characterCache;
        private Dictionary<NarratorLineType, VoiceLine> _narratorCache;
        private Dictionary<string, VoiceLine> _tutorialCache;
        
        private void OnEnable()
        {
            BuildCaches();
        }
        
        private void BuildCaches()
        {
            _lineCache = new Dictionary<string, VoiceLine>();
            _characterCache = new Dictionary<(CharacterType, VoiceLineType), List<VoiceLine>>();
            _narratorCache = new Dictionary<NarratorLineType, VoiceLine>();
            _tutorialCache = new Dictionary<string, VoiceLine>();
            
            // Build character cache
            if (characterVoices != null)
            {
                foreach (var charVoice in characterVoices)
                {
                    if (charVoice.lines == null) continue;
                    
                    foreach (var line in charVoice.lines)
                    {
                        // Add to ID cache
                        if (!string.IsNullOrEmpty(line.id))
                        {
                            _lineCache[line.id] = line;
                        }
                        
                        // Add to character cache
                        var key = (charVoice.character, line.type);
                        if (!_characterCache.ContainsKey(key))
                        {
                            _characterCache[key] = new List<VoiceLine>();
                        }
                        _characterCache[key].Add(line);
                    }
                }
            }
            
            // Build narrator cache
            if (narratorLines != null)
            {
                foreach (var narr in narratorLines)
                {
                    _narratorCache[narr.type] = narr.line;
                    if (!string.IsNullOrEmpty(narr.line.id))
                    {
                        _lineCache[narr.line.id] = narr.line;
                    }
                }
            }
            
            // Build tutorial cache
            if (tutorialLines != null)
            {
                foreach (var tut in tutorialLines)
                {
                    _tutorialCache[tut.tutorialId] = tut.line;
                    if (!string.IsNullOrEmpty(tut.line.id))
                    {
                        _lineCache[tut.line.id] = tut.line;
                    }
                }
            }
        }
        
        public VoiceLine GetLine(string id)
        {
            if (_lineCache == null) BuildCaches();
            _lineCache.TryGetValue(id, out var line);
            return line;
        }
        
        public VoiceLine GetCharacterLine(CharacterType character, VoiceLineType type)
        {
            if (_characterCache == null) BuildCaches();
            
            if (_characterCache.TryGetValue((character, type), out var lines) && lines.Count > 0)
            {
                // Return random variation
                return lines[UnityEngine.Random.Range(0, lines.Count)];
            }
            return null;
        }
        
        public VoiceLine GetNarratorLine(NarratorLineType type)
        {
            if (_narratorCache == null) BuildCaches();
            _narratorCache.TryGetValue(type, out var line);
            return line;
        }
        
        public VoiceLine GetTutorialLine(string tutorialId)
        {
            if (_tutorialCache == null) BuildCaches();
            _tutorialCache.TryGetValue(tutorialId, out var line);
            return line;
        }
    }
    
    [Serializable]
    public class CharacterVoiceSet
    {
        public CharacterType character;
        public string characterName;
        public List<VoiceLine> lines;
    }
    
    [Serializable]
    public class NarratorLine
    {
        public NarratorLineType type;
        public VoiceLine line;
    }
    
    [Serializable]
    public class TutorialLine
    {
        public string tutorialId;
        public VoiceLine line;
    }
    
    /// <summary>
    /// Subtitle display component.
    /// Listens to VoiceLineManager for subtitle updates.
    /// </summary>
    public class SubtitleDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMPro.TextMeshProUGUI subtitleText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform container;
        
        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        
        private Coroutine _fadeCoroutine;
        
        private void Awake()
        {
            if (canvasGroup == null && container != null)
            {
                canvasGroup = container.GetComponent<CanvasGroup>() ?? 
                    container.gameObject.AddComponent<CanvasGroup>();
            }
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
            }
        }
        
        private void OnEnable()
        {
            if (VoiceLineManager.Instance != null)
            {
                VoiceLineManager.Instance.OnSubtitleChanged += HandleSubtitleChanged;
            }
        }
        
        private void OnDisable()
        {
            if (VoiceLineManager.Instance != null)
            {
                VoiceLineManager.Instance.OnSubtitleChanged -= HandleSubtitleChanged;
            }
        }
        
        private void HandleSubtitleChanged(string text)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            
            if (string.IsNullOrEmpty(text))
            {
                _fadeCoroutine = StartCoroutine(FadeOut());
            }
            else
            {
                subtitleText.text = text;
                _fadeCoroutine = StartCoroutine(FadeIn());
            }
        }
        
        private IEnumerator FadeIn()
        {
            float elapsed = 0;
            float startAlpha = canvasGroup.alpha;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1, elapsed / fadeInDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1;
        }
        
        private IEnumerator FadeOut()
        {
            float elapsed = 0;
            float startAlpha = canvasGroup.alpha;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, elapsed / fadeOutDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 0;
        }
    }
}
