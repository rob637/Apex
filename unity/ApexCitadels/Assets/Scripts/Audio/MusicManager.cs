using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using ApexCitadels.Core;

namespace ApexCitadels.Audio
{
    /// <summary>
    /// Music manager with crossfading, layered tracks, and dynamic music systems.
    /// Handles background music, combat music, and contextual music transitions.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;
        [SerializeField] private AudioSource stingerSource;

        [Header("Music Library")]
        [SerializeField] private MusicLibrary musicLibrary;

        [Header("Crossfade Settings")]
        [SerializeField] private float crossfadeDuration = 2f;
        [SerializeField] private AnimationCurve crossfadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Dynamic Music")]
        [SerializeField] private float combatMusicDelay = 1f;
        [SerializeField] private float peaceMusicDelay = 5f;

        // State
        private AudioSource activeSource;
        private AudioSource inactiveSource;
        private MusicTrack currentTrack;
        private MusicState currentState = MusicState.Peace;
        private bool isCrossfading = false;
        private Coroutine crossfadeCoroutine;
        private Coroutine stateChangeCoroutine;

        // Playlist
        private Queue<MusicTrack> playlist = new Queue<MusicTrack>();
        private bool shufflePlaylist = true;
        private bool loopPlaylist = true;

        // Events
        public event Action<MusicTrack> OnTrackChanged;
        public event Action<MusicState> OnMusicStateChanged;

        // Properties
        public MusicTrack CurrentTrack => currentTrack;
        public MusicState CurrentState => currentState;
        public bool IsPlaying => activeSource != null && activeSource.isPlaying;
        public float CurrentTime => activeSource?.time ?? 0f;
        public float TrackLength => currentTrack?.clip?.length ?? 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeAudioSources();
        }

        private void Start()
        {
            // Start with ambient/menu music
            if (musicLibrary != null)
            {
                var menuTrack = musicLibrary.GetTrack("menu_theme");
                if (menuTrack != null)
                {
                    Play(menuTrack);
                }
            }
        }

        private void Update()
        {
            // Auto-advance playlist when track ends
            if (activeSource != null && currentTrack != null && !currentTrack.loop)
            {
                if (!activeSource.isPlaying && !isCrossfading)
                {
                    PlayNextInPlaylist();
                }
            }
        }

        #region Initialization

        private void InitializeAudioSources()
        {
            if (musicSourceA == null)
            {
                GameObject objA = new GameObject("MusicSource_A");
                objA.transform.SetParent(transform);
                musicSourceA = objA.AddComponent<AudioSource>();
            }

            if (musicSourceB == null)
            {
                GameObject objB = new GameObject("MusicSource_B");
                objB.transform.SetParent(transform);
                musicSourceB = objB.AddComponent<AudioSource>();
            }

            if (stingerSource == null)
            {
                GameObject objStinger = new GameObject("StingerSource");
                objStinger.transform.SetParent(transform);
                stingerSource = objStinger.AddComponent<AudioSource>();
            }

            // Configure sources
            ConfigureMusicSource(musicSourceA);
            ConfigureMusicSource(musicSourceB);
            ConfigureMusicSource(stingerSource);
            stingerSource.loop = false;

            activeSource = musicSourceA;
            inactiveSource = musicSourceB;
        }

        private void ConfigureMusicSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f; // 2D sound
            source.priority = 0; // Highest priority
            source.outputAudioMixerGroup = AudioManager.Instance?.MusicGroup;
        }

        #endregion

        #region Play Methods

        /// <summary>
        /// Play a music track by ID
        /// </summary>
        public void Play(string trackId, bool crossfade = true)
        {
            if (musicLibrary == null) return;

            MusicTrack track = musicLibrary.GetTrack(trackId);
            if (track != null)
            {
                Play(track, crossfade);
            }
            else
            {
                ApexLogger.LogWarning($"[MusicManager] Track not found: {trackId}", ApexLogger.LogCategory.Audio);
            }
        }

        /// <summary>
        /// Play a music track
        /// </summary>
        public void Play(MusicTrack track, bool crossfade = true)
        {
            if (track == null || track.clip == null) return;

            // Don't restart if already playing this track
            if (currentTrack != null && currentTrack.id == track.id && activeSource.isPlaying)
            {
                return;
            }

            if (crossfade && activeSource.isPlaying)
            {
                CrossfadeTo(track);
            }
            else
            {
                PlayImmediate(track);
            }
        }

        /// <summary>
        /// Play a track immediately without crossfade
        /// </summary>
        public void PlayImmediate(MusicTrack track)
        {
            if (track == null || track.clip == null) return;

            // Stop any ongoing crossfade
            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
                isCrossfading = false;
            }

            activeSource.Stop();
            inactiveSource.Stop();

            currentTrack = track;
            activeSource.clip = track.clip;
            activeSource.volume = track.volume;
            activeSource.loop = track.loop;
            activeSource.Play();

            OnTrackChanged?.Invoke(track);
        }

        /// <summary>
        /// Crossfade to a new track
        /// </summary>
        public void CrossfadeTo(MusicTrack track)
        {
            if (track == null || track.clip == null) return;

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }

            crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(track));
        }

        private IEnumerator CrossfadeCoroutine(MusicTrack newTrack)
        {
            isCrossfading = true;

            // Swap sources
            AudioSource oldSource = activeSource;
            AudioSource newSource = inactiveSource;

            // Setup new source
            newSource.clip = newTrack.clip;
            newSource.volume = 0f;
            newSource.loop = newTrack.loop;
            newSource.Play();

            float startVolume = oldSource.volume;
            float targetVolume = newTrack.volume;
            float elapsed = 0f;

            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = crossfadeCurve.Evaluate(elapsed / crossfadeDuration);

                oldSource.volume = Mathf.Lerp(startVolume, 0f, t);
                newSource.volume = Mathf.Lerp(0f, targetVolume, t);

                yield return null;
            }

            // Finalize
            oldSource.Stop();
            oldSource.volume = 0f;
            newSource.volume = targetVolume;

            // Update references
            activeSource = newSource;
            inactiveSource = oldSource;
            currentTrack = newTrack;

            isCrossfading = false;
            crossfadeCoroutine = null;

            OnTrackChanged?.Invoke(newTrack);
        }

        #endregion

        #region Stingers

        /// <summary>
        /// Play a one-shot music stinger (victory, defeat, etc.)
        /// </summary>
        public void PlayStinger(string stingerId, bool duckMusic = true, float duckVolume = 0.3f)
        {
            if (musicLibrary == null) return;

            MusicTrack stinger = musicLibrary.GetStinger(stingerId);
            if (stinger != null)
            {
                PlayStinger(stinger, duckMusic, duckVolume);
            }
        }

        /// <summary>
        /// Play a stinger track
        /// </summary>
        public void PlayStinger(MusicTrack stinger, bool duckMusic = true, float duckVolume = 0.3f)
        {
            if (stinger == null || stinger.clip == null) return;

            stingerSource.clip = stinger.clip;
            stingerSource.volume = stinger.volume;
            stingerSource.Play();

            if (duckMusic && activeSource.isPlaying)
            {
                StartCoroutine(DuckMusicCoroutine(stinger.clip.length, duckVolume));
            }
        }

        private IEnumerator DuckMusicCoroutine(float duration, float duckVolume)
        {
            float originalVolume = activeSource.volume;
            float fadeTime = 0.2f;

            // Fade down
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                activeSource.volume = Mathf.Lerp(originalVolume, duckVolume, elapsed / fadeTime);
                yield return null;
            }

            // Wait for stinger
            yield return new WaitForSeconds(duration - fadeTime * 2);

            // Fade back up
            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                activeSource.volume = Mathf.Lerp(duckVolume, originalVolume, elapsed / fadeTime);
                yield return null;
            }

            activeSource.volume = originalVolume;
        }

        #endregion

        #region Dynamic Music States

        /// <summary>
        /// Set the current music state (triggers appropriate music)
        /// </summary>
        public void SetMusicState(MusicState newState, bool immediate = false)
        {
            if (newState == currentState) return;

            if (stateChangeCoroutine != null)
            {
                StopCoroutine(stateChangeCoroutine);
            }

            if (immediate)
            {
                ApplyMusicState(newState);
            }
            else
            {
                float delay = newState == MusicState.Combat ? combatMusicDelay : peaceMusicDelay;
                stateChangeCoroutine = StartCoroutine(DelayedStateChange(newState, delay));
            }
        }

        private IEnumerator DelayedStateChange(MusicState newState, float delay)
        {
            yield return new WaitForSeconds(delay);
            ApplyMusicState(newState);
            stateChangeCoroutine = null;
        }

        private void ApplyMusicState(MusicState newState)
        {
            currentState = newState;

            if (musicLibrary == null) return;

            MusicTrack track = null;

            switch (newState)
            {
                case MusicState.Menu:
                    track = musicLibrary.GetTrack("menu_theme");
                    break;
                case MusicState.Peace:
                    track = musicLibrary.GetRandomTrack(MusicCategory.Exploration);
                    break;
                case MusicState.Combat:
                    track = musicLibrary.GetRandomTrack(MusicCategory.Combat);
                    break;
                case MusicState.Victory:
                    PlayStinger("victory_stinger");
                    break;
                case MusicState.Defeat:
                    PlayStinger("defeat_stinger");
                    break;
                case MusicState.Boss:
                    track = musicLibrary.GetTrack("boss_battle");
                    break;
                case MusicState.Event:
                    track = musicLibrary.GetRandomTrack(MusicCategory.Event);
                    break;
                case MusicState.Building:
                    track = musicLibrary.GetRandomTrack(MusicCategory.Building);
                    break;
            }

            if (track != null)
            {
                Play(track);
            }

            OnMusicStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Enter combat music
        /// </summary>
        public void EnterCombat()
        {
            SetMusicState(MusicState.Combat);
        }

        /// <summary>
        /// Exit combat, return to peace
        /// </summary>
        public void ExitCombat()
        {
            SetMusicState(MusicState.Peace);
        }

        /// <summary>
        /// Play victory music/stinger
        /// </summary>
        public void PlayVictory()
        {
            SetMusicState(MusicState.Victory, true);
        }

        /// <summary>
        /// Play defeat music/stinger
        /// </summary>
        public void PlayDefeat()
        {
            SetMusicState(MusicState.Defeat, true);
        }

        #endregion

        #region Playlist

        /// <summary>
        /// Set up a playlist of tracks
        /// </summary>
        public void SetPlaylist(List<MusicTrack> tracks, bool shuffle = true, bool loop = true)
        {
            playlist.Clear();
            shufflePlaylist = shuffle;
            loopPlaylist = loop;

            if (shuffle)
            {
                // Fisher-Yates shuffle
                List<MusicTrack> shuffled = new List<MusicTrack>(tracks);
                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    var temp = shuffled[i];
                    shuffled[i] = shuffled[j];
                    shuffled[j] = temp;
                }
                tracks = shuffled;
            }

            foreach (var track in tracks)
            {
                playlist.Enqueue(track);
            }
        }

        /// <summary>
        /// Set playlist by category
        /// </summary>
        public void SetPlaylistByCategory(MusicCategory category, bool shuffle = true, bool loop = true)
        {
            if (musicLibrary == null) return;

            List<MusicTrack> tracks = musicLibrary.GetTracksByCategory(category);
            SetPlaylist(tracks, shuffle, loop);
        }

        /// <summary>
        /// Play next track in playlist
        /// </summary>
        public void PlayNextInPlaylist()
        {
            if (playlist.Count == 0)
            {
                if (loopPlaylist && musicLibrary != null)
                {
                    // Rebuild playlist
                    SetPlaylistByCategory(MusicCategory.Exploration, shufflePlaylist, loopPlaylist);
                }
                else
                {
                    return;
                }
            }

            if (playlist.Count > 0)
            {
                MusicTrack next = playlist.Dequeue();
                
                // Re-add to end if looping
                if (loopPlaylist)
                {
                    playlist.Enqueue(next);
                }

                Play(next);
            }
        }

        /// <summary>
        /// Skip to next track
        /// </summary>
        public void Skip()
        {
            PlayNextInPlaylist();
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// Stop all music
        /// </summary>
        public void Stop(bool fade = true)
        {
            if (fade)
            {
                StartCoroutine(FadeOutCoroutine());
            }
            else
            {
                StopImmediate();
            }
        }

        /// <summary>
        /// Stop immediately without fade
        /// </summary>
        public void StopImmediate()
        {
            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
                isCrossfading = false;
            }

            musicSourceA.Stop();
            musicSourceB.Stop();
            stingerSource.Stop();
            currentTrack = null;
        }

        private IEnumerator FadeOutCoroutine()
        {
            float startVolume = activeSource.volume;
            float elapsed = 0f;
            float fadeDuration = 1f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                activeSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }

            StopImmediate();
        }

        /// <summary>
        /// Pause music
        /// </summary>
        public void Pause()
        {
            musicSourceA.Pause();
            musicSourceB.Pause();
        }

        /// <summary>
        /// Resume paused music
        /// </summary>
        public void Resume()
        {
            if (activeSource.clip != null)
            {
                activeSource.UnPause();
            }
        }

        /// <summary>
        /// Seek to a position in current track
        /// </summary>
        public void Seek(float time)
        {
            if (activeSource.clip != null)
            {
                activeSource.time = Mathf.Clamp(time, 0f, activeSource.clip.length);
            }
        }

        /// <summary>
        /// Set volume multiplier (temporary adjustment)
        /// </summary>
        public void SetVolumeMultiplier(float multiplier)
        {
            if (currentTrack != null)
            {
                activeSource.volume = currentTrack.volume * Mathf.Clamp01(multiplier);
            }
        }

        #endregion

        #region Convenience Methods

        public void PlayMenuMusic() => Play("menu_theme");
        public void PlayGameplayMusic() => SetMusicState(MusicState.Peace, true);
        public void PlayCombatMusic() => SetMusicState(MusicState.Combat, true);
        public void PlayBossMusic() => SetMusicState(MusicState.Boss, true);
        public void PlayEventMusic() => SetMusicState(MusicState.Event, true);
        public void PlayBuildingMusic() => SetMusicState(MusicState.Building, true);

        public void PlayVictoryStinger() => PlayStinger("victory_stinger");
        public void PlayDefeatStinger() => PlayStinger("defeat_stinger");
        public void PlayLevelUpStinger() => PlayStinger("level_up_stinger");
        public void PlayAchievementStinger() => PlayStinger("achievement_stinger");
        public void PlayEventStartStinger() => PlayStinger("event_start_stinger");

        #endregion
    }

    /// <summary>
    /// Music states for dynamic music system
    /// </summary>
    public enum MusicState
    {
        Menu,
        Peace,
        Combat,
        Victory,
        Defeat,
        Boss,
        Event,
        Building,
        Cutscene
    }

    /// <summary>
    /// Categories for music tracks
    /// </summary>
    public enum MusicCategory
    {
        Menu,
        Exploration,
        Combat,
        Boss,
        Event,
        Building,
        Ambient,
        Cutscene,
        Stinger
    }

    /// <summary>
    /// Individual music track entry
    /// </summary>
    [Serializable]
    public class MusicTrack
    {
        public string id;
        public string displayName;
        public MusicCategory category;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = true;
        public float bpm = 120f;
        public float loopStartTime = 0f;
        public float loopEndTime = -1f; // -1 = full track

        public float Duration => clip != null ? clip.length : 0f;
    }

    /// <summary>
    /// ScriptableObject containing all game music
    /// </summary>
    [CreateAssetMenu(fileName = "MusicLibrary", menuName = "Apex Citadels/Audio/Music Library")]
    public class MusicLibrary : ScriptableObject
    {
        [SerializeField] private List<MusicTrack> tracks = new List<MusicTrack>();
        [SerializeField] private List<MusicTrack> stingers = new List<MusicTrack>();

        private Dictionary<string, MusicTrack> trackLookup;
        private Dictionary<string, MusicTrack> stingerLookup;
        private Dictionary<MusicCategory, List<MusicTrack>> categoryLookup;

        private void OnEnable()
        {
            BuildLookups();
        }

        private void BuildLookups()
        {
            trackLookup = new Dictionary<string, MusicTrack>();
            stingerLookup = new Dictionary<string, MusicTrack>();
            categoryLookup = new Dictionary<MusicCategory, List<MusicTrack>>();

            foreach (MusicCategory category in Enum.GetValues(typeof(MusicCategory)))
            {
                categoryLookup[category] = new List<MusicTrack>();
            }

            foreach (var track in tracks)
            {
                if (!string.IsNullOrEmpty(track.id))
                {
                    trackLookup[track.id] = track;
                    categoryLookup[track.category].Add(track);
                }
            }

            foreach (var stinger in stingers)
            {
                if (!string.IsNullOrEmpty(stinger.id))
                {
                    stingerLookup[stinger.id] = stinger;
                }
            }
        }

        public MusicTrack GetTrack(string id)
        {
            if (trackLookup == null) BuildLookups();
            trackLookup.TryGetValue(id, out MusicTrack track);
            return track;
        }

        public MusicTrack GetStinger(string id)
        {
            if (stingerLookup == null) BuildLookups();
            stingerLookup.TryGetValue(id, out MusicTrack stinger);
            return stinger;
        }

        public MusicTrack GetRandomTrack(MusicCategory category)
        {
            if (categoryLookup == null) BuildLookups();

            if (categoryLookup.TryGetValue(category, out List<MusicTrack> categoryTracks))
            {
                if (categoryTracks.Count > 0)
                {
                    return categoryTracks[UnityEngine.Random.Range(0, categoryTracks.Count)];
                }
            }
            return null;
        }

        public List<MusicTrack> GetTracksByCategory(MusicCategory category)
        {
            if (categoryLookup == null) BuildLookups();
            
            categoryLookup.TryGetValue(category, out List<MusicTrack> categoryTracks);
            return categoryTracks ?? new List<MusicTrack>();
        }

        public List<MusicTrack> GetAllTracks() => tracks;
        public List<MusicTrack> GetAllStingers() => stingers;

        #if UNITY_EDITOR
        /// <summary>
        /// Generate default track entries
        /// </summary>
        [ContextMenu("Generate Default Entries")]
        public void GenerateDefaultEntries()
        {
            tracks.Clear();
            stingers.Clear();

            // Menu
            AddTrack("menu_theme", "Main Menu Theme", MusicCategory.Menu);

            // Exploration
            AddTrack("exploration_01", "Peaceful Wandering", MusicCategory.Exploration);
            AddTrack("exploration_02", "Open Horizons", MusicCategory.Exploration);
            AddTrack("exploration_03", "New Discoveries", MusicCategory.Exploration);
            AddTrack("exploration_ambient", "Ambient Exploration", MusicCategory.Exploration);

            // Combat
            AddTrack("combat_01", "Battle Engaged", MusicCategory.Combat);
            AddTrack("combat_02", "Fight for Territory", MusicCategory.Combat);
            AddTrack("combat_03", "Clash of Citadels", MusicCategory.Combat);
            AddTrack("combat_intense", "Intense Combat", MusicCategory.Combat);

            // Boss
            AddTrack("boss_battle", "Boss Battle", MusicCategory.Boss);
            AddTrack("alliance_war", "Alliance War", MusicCategory.Boss);

            // Events
            AddTrack("event_01", "World Event Theme", MusicCategory.Event);
            AddTrack("event_season", "Season Finale", MusicCategory.Event);
            AddTrack("event_special", "Special Event", MusicCategory.Event);

            // Building
            AddTrack("building_01", "Creative Mode", MusicCategory.Building);
            AddTrack("building_peaceful", "Peaceful Building", MusicCategory.Building);

            // Ambient
            AddTrack("ambient_day", "Daytime Ambience", MusicCategory.Ambient);
            AddTrack("ambient_night", "Nighttime Ambience", MusicCategory.Ambient);
            AddTrack("ambient_city", "City Sounds", MusicCategory.Ambient);

            // Stingers
            AddStinger("victory_stinger", "Victory!", 0.8f);
            AddStinger("defeat_stinger", "Defeat", 0.8f);
            AddStinger("level_up_stinger", "Level Up!", 0.9f);
            AddStinger("achievement_stinger", "Achievement Unlocked", 0.8f);
            AddStinger("event_start_stinger", "Event Started", 0.7f);
            AddStinger("territory_claim_stinger", "Territory Claimed", 0.7f);
            AddStinger("territory_lost_stinger", "Territory Lost", 0.7f);
            AddStinger("season_reward_stinger", "Season Reward", 0.9f);

            UnityEditor.EditorUtility.SetDirty(this);
            ApexLogger.Log($"[MusicLibrary] Generated {tracks.Count} tracks and {stingers.Count} stingers", ApexLogger.LogCategory.Audio);
        }

        private void AddTrack(string id, string name, MusicCategory category)
        {
            tracks.Add(new MusicTrack
            {
                id = id,
                displayName = name,
                category = category,
                volume = 1f,
                loop = true,
                bpm = 120f
            });
        }

        private void AddStinger(string id, string name, float volume = 1f)
        {
            stingers.Add(new MusicTrack
            {
                id = id,
                displayName = name,
                category = MusicCategory.Stinger,
                volume = volume,
                loop = false
            });
        }
        #endif
    }
}
