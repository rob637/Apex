using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Sound & Music Manager - Handles all audio for the game.
    /// Supports music playlists, sound effects, ambient sounds, and UI audio.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _ambientSource;
        [SerializeField] private AudioSource _uiSource;
        [SerializeField] private int _sfxPoolSize = 10;
        
        [Header("Settings")]
        [SerializeField] private float _musicFadeDuration = 1.5f;
        [SerializeField] private float _crossfadeDuration = 2f;
        
        // Audio pools
        private List<AudioSource> _sfxPool = new List<AudioSource>();
        private int _currentSfxIndex = 0;
        
        // Volume settings (0-1)
        private float _masterVolume = 0.8f;
        private float _musicVolume = 0.7f;
        private float _sfxVolume = 1f;
        private float _uiVolume = 0.8f;
        private float _ambientVolume = 0.6f;
        
        // Music system
        private List<AudioClip> _currentPlaylist = new List<AudioClip>();
        private int _currentTrackIndex = 0;
        private bool _isPlayingPlaylist = false;
        private Coroutine _musicCoroutine;
        
        // Sound libraries (populated at runtime or via inspector)
        private Dictionary<string, AudioClip> _musicTracks = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> _sfxClips = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> _uiClips = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioClip> _ambientClips = new Dictionary<string, AudioClip>();
        
        public static SoundManager Instance { get; private set; }
        
        public event Action<string> OnMusicTrackChanged;
        public event Action<float> OnVolumeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeAudioSources();
            LoadVolumeSettings();
            RegisterDefaultSounds();
        }

        private void InitializeAudioSources()
        {
            // Create music source if not assigned
            if (_musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                _musicSource = musicObj.AddComponent<AudioSource>();
                _musicSource.loop = false;
                _musicSource.playOnAwake = false;
                _musicSource.priority = 0;
            }
            
            // Create ambient source
            if (_ambientSource == null)
            {
                GameObject ambientObj = new GameObject("AmbientSource");
                ambientObj.transform.SetParent(transform);
                _ambientSource = ambientObj.AddComponent<AudioSource>();
                _ambientSource.loop = true;
                _ambientSource.playOnAwake = false;
                _ambientSource.priority = 10;
            }
            
            // Create UI source
            if (_uiSource == null)
            {
                GameObject uiObj = new GameObject("UISource");
                uiObj.transform.SetParent(transform);
                _uiSource = uiObj.AddComponent<AudioSource>();
                _uiSource.loop = false;
                _uiSource.playOnAwake = false;
                _uiSource.priority = 50;
            }
            
            // Create SFX pool
            GameObject sfxPool = new GameObject("SFXPool");
            sfxPool.transform.SetParent(transform);
            
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                GameObject sfxObj = new GameObject($"SFX_{i}");
                sfxObj.transform.SetParent(sfxPool.transform);
                
                AudioSource source = sfxObj.AddComponent<AudioSource>();
                source.loop = false;
                source.playOnAwake = false;
                source.priority = 128;
                source.spatialBlend = 0; // 2D by default
                
                _sfxPool.Add(source);
            }
            
            ApplyVolumeSettings();
        }

        private void LoadVolumeSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            _musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            _sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            _uiVolume = PlayerPrefs.GetFloat("UIVolume", 0.8f);
            _ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 0.6f);
        }

        private void ApplyVolumeSettings()
        {
            AudioListener.volume = _masterVolume;
            
            if (_musicSource != null)
                _musicSource.volume = _musicVolume;
            
            if (_ambientSource != null)
                _ambientSource.volume = _ambientVolume;
            
            if (_uiSource != null)
                _uiSource.volume = _uiVolume;
        }

        private void RegisterDefaultSounds()
        {
            // Register sound effect identifiers (actual clips would be loaded from Resources or assigned)
            // These are placeholders - in production you'd load actual audio files
            
            // UI Sounds
            RegisterUISound("button_click");
            RegisterUISound("button_hover");
            RegisterUISound("panel_open");
            RegisterUISound("panel_close");
            RegisterUISound("notification");
            RegisterUISound("error");
            RegisterUISound("success");
            RegisterUISound("purchase");
            RegisterUISound("level_up");
            RegisterUISound("achievement");
            
            // Combat SFX
            RegisterSFX("sword_slash");
            RegisterSFX("sword_clash");
            RegisterSFX("arrow_fire");
            RegisterSFX("arrow_hit");
            RegisterSFX("cavalry_charge");
            RegisterSFX("siege_fire");
            RegisterSFX("explosion");
            RegisterSFX("battle_horn");
            RegisterSFX("victory_fanfare");
            RegisterSFX("defeat");
            
            // Building SFX
            RegisterSFX("construction");
            RegisterSFX("building_complete");
            RegisterSFX("upgrade");
            RegisterSFX("demolish");
            
            // Resource SFX
            RegisterSFX("gold_collect");
            RegisterSFX("resource_collect");
            RegisterSFX("chest_open");
            
            // Music tracks
            RegisterMusic("main_theme");
            RegisterMusic("battle_theme");
            RegisterMusic("victory_theme");
            RegisterMusic("exploration");
            RegisterMusic("menu");
            RegisterMusic("alliance_hall");
            
            // Ambient
            RegisterAmbient("forest");
            RegisterAmbient("city");
            RegisterAmbient("battlefield");
            RegisterAmbient("mountains");
            RegisterAmbient("desert");
            RegisterAmbient("ocean");
        }

        private void RegisterMusic(string id) => _musicTracks[id] = null;
        private void RegisterSFX(string id) => _sfxClips[id] = null;
        private void RegisterUISound(string id) => _uiClips[id] = null;
        private void RegisterAmbient(string id) => _ambientClips[id] = null;

        #region Music

        /// <summary>
        /// Play a single music track
        /// </summary>
        public void PlayMusic(string trackId, bool fade = true)
        {
            if (!_musicTracks.ContainsKey(trackId))
            {
                Debug.LogWarning($"[SoundManager] Music track not found: {trackId}");
                return;
            }
            
            _isPlayingPlaylist = false;
            
            if (_musicCoroutine != null)
                StopCoroutine(_musicCoroutine);
            
            if (fade && _musicSource.isPlaying)
            {
                _musicCoroutine = StartCoroutine(CrossfadeToTrack(_musicTracks[trackId]));
            }
            else
            {
                _musicSource.clip = _musicTracks[trackId];
                _musicSource.Play();
            }
            
            OnMusicTrackChanged?.Invoke(trackId);
            Debug.Log($"[SoundManager] Playing music: {trackId}");
        }

        /// <summary>
        /// Play a playlist of music tracks
        /// </summary>
        public void PlayPlaylist(List<string> trackIds, bool shuffle = false)
        {
            _currentPlaylist.Clear();
            
            foreach (var id in trackIds)
            {
                if (_musicTracks.ContainsKey(id) && _musicTracks[id] != null)
                {
                    _currentPlaylist.Add(_musicTracks[id]);
                }
            }
            
            if (_currentPlaylist.Count == 0)
            {
                Debug.LogWarning("[SoundManager] No valid tracks in playlist");
                return;
            }
            
            if (shuffle)
            {
                ShufflePlaylist();
            }
            
            _currentTrackIndex = 0;
            _isPlayingPlaylist = true;
            
            PlayCurrentPlaylistTrack();
            
            Debug.Log($"[SoundManager] Playing playlist with {_currentPlaylist.Count} tracks");
        }

        private void PlayCurrentPlaylistTrack()
        {
            if (_currentPlaylist.Count == 0) return;
            
            if (_musicCoroutine != null)
                StopCoroutine(_musicCoroutine);
            
            _musicCoroutine = StartCoroutine(PlayPlaylistTrack());
        }

        private IEnumerator PlayPlaylistTrack()
        {
            AudioClip clip = _currentPlaylist[_currentTrackIndex];
            
            if (_musicSource.isPlaying)
            {
                yield return StartCoroutine(CrossfadeToTrack(clip));
            }
            else
            {
                _musicSource.clip = clip;
                _musicSource.volume = _musicVolume;
                _musicSource.Play();
            }
            
            // Wait for track to finish
            yield return new WaitForSeconds(clip != null ? clip.length : 0);
            
            // Next track
            if (_isPlayingPlaylist)
            {
                _currentTrackIndex = (_currentTrackIndex + 1) % _currentPlaylist.Count;
                PlayCurrentPlaylistTrack();
            }
        }

        private IEnumerator CrossfadeToTrack(AudioClip newClip)
        {
            float elapsed = 0;
            float startVolume = _musicSource.volume;
            
            // Fade out
            while (elapsed < _crossfadeDuration / 2)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0, elapsed / (_crossfadeDuration / 2));
                yield return null;
            }
            
            // Switch track
            _musicSource.Stop();
            _musicSource.clip = newClip;
            _musicSource.Play();
            
            // Fade in
            elapsed = 0;
            while (elapsed < _crossfadeDuration / 2)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(0, _musicVolume, elapsed / (_crossfadeDuration / 2));
                yield return null;
            }
            
            _musicSource.volume = _musicVolume;
        }

        private void ShufflePlaylist()
        {
            for (int i = _currentPlaylist.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = _currentPlaylist[i];
                _currentPlaylist[i] = _currentPlaylist[j];
                _currentPlaylist[j] = temp;
            }
        }

        public void StopMusic(bool fade = true)
        {
            _isPlayingPlaylist = false;
            
            if (_musicCoroutine != null)
                StopCoroutine(_musicCoroutine);
            
            if (fade)
            {
                StartCoroutine(FadeOutMusic());
            }
            else
            {
                _musicSource.Stop();
            }
        }

        private IEnumerator FadeOutMusic()
        {
            float elapsed = 0;
            float startVolume = _musicSource.volume;
            
            while (elapsed < _musicFadeDuration)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0, elapsed / _musicFadeDuration);
                yield return null;
            }
            
            _musicSource.Stop();
            _musicSource.volume = _musicVolume;
        }

        public void PauseMusic() => _musicSource.Pause();
        public void ResumeMusic() => _musicSource.UnPause();

        #endregion

        #region Sound Effects

        /// <summary>
        /// Play a sound effect
        /// </summary>
        public void PlaySFX(string sfxId, float volumeScale = 1f)
        {
            if (!_sfxClips.ContainsKey(sfxId))
            {
                Debug.LogWarning($"[SoundManager] SFX not found: {sfxId}");
                return;
            }
            
            AudioClip clip = _sfxClips[sfxId];
            if (clip == null)
            {
                // Placeholder - would load from Resources in production
                Debug.Log($"[SoundManager] SFX played: {sfxId}");
                return;
            }
            
            AudioSource source = GetAvailableSFXSource();
            source.clip = clip;
            source.volume = _sfxVolume * volumeScale;
            source.pitch = 1f;
            source.Play();
        }

        /// <summary>
        /// Play a sound effect with pitch variation (good for repetitive sounds)
        /// </summary>
        public void PlaySFXWithPitchVariation(string sfxId, float pitchMin = 0.9f, float pitchMax = 1.1f, float volumeScale = 1f)
        {
            if (!_sfxClips.ContainsKey(sfxId))
            {
                Debug.LogWarning($"[SoundManager] SFX not found: {sfxId}");
                return;
            }
            
            AudioClip clip = _sfxClips[sfxId];
            if (clip == null)
            {
                Debug.Log($"[SoundManager] SFX played with pitch variation: {sfxId}");
                return;
            }
            
            AudioSource source = GetAvailableSFXSource();
            source.clip = clip;
            source.volume = _sfxVolume * volumeScale;
            source.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
            source.Play();
        }

        /// <summary>
        /// Play a 3D positioned sound effect
        /// </summary>
        public void PlaySFX3D(string sfxId, Vector3 position, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 50f)
        {
            if (!_sfxClips.ContainsKey(sfxId)) return;
            
            AudioClip clip = _sfxClips[sfxId];
            if (clip == null) return;
            
            AudioSource source = GetAvailableSFXSource();
            source.transform.position = position;
            source.clip = clip;
            source.volume = _sfxVolume * volumeScale;
            source.spatialBlend = 1f; // Full 3D
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.Play();
        }

        private AudioSource GetAvailableSFXSource()
        {
            // Simple round-robin pool
            AudioSource source = _sfxPool[_currentSfxIndex];
            _currentSfxIndex = (_currentSfxIndex + 1) % _sfxPool.Count;
            
            // Reset to 2D for next use
            source.spatialBlend = 0;
            
            return source;
        }

        #endregion

        #region UI Sounds

        public void PlayUISound(string soundId, float volumeScale = 1f)
        {
            if (!_uiClips.ContainsKey(soundId))
            {
                Debug.Log($"[SoundManager] UI sound played: {soundId}");
                return;
            }
            
            AudioClip clip = _uiClips[soundId];
            if (clip != null)
            {
                _uiSource.PlayOneShot(clip, _uiVolume * volumeScale);
            }
        }

        // Convenience methods for common UI sounds
        public void PlayButtonClick() => PlayUISound("button_click");
        public void PlayButtonHover() => PlayUISound("button_hover");
        public void PlayPanelOpen() => PlayUISound("panel_open");
        public void PlayPanelClose() => PlayUISound("panel_close");
        public void PlayNotification() => PlayUISound("notification");
        public void PlayError() => PlayUISound("error");
        public void PlaySuccess() => PlayUISound("success");
        public void PlayPurchase() => PlayUISound("purchase");
        public void PlayLevelUp() => PlayUISound("level_up");
        public void PlayAchievement() => PlayUISound("achievement");

        #endregion

        #region Ambient

        public void PlayAmbient(string ambientId, bool fade = true)
        {
            if (!_ambientClips.ContainsKey(ambientId))
            {
                Debug.Log($"[SoundManager] Ambient sound: {ambientId}");
                return;
            }
            
            AudioClip clip = _ambientClips[ambientId];
            if (clip == null) return;
            
            if (fade && _ambientSource.isPlaying)
            {
                StartCoroutine(CrossfadeAmbient(clip));
            }
            else
            {
                _ambientSource.clip = clip;
                _ambientSource.volume = _ambientVolume;
                _ambientSource.Play();
            }
        }

        private IEnumerator CrossfadeAmbient(AudioClip newClip)
        {
            float elapsed = 0;
            float startVolume = _ambientSource.volume;
            
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                _ambientSource.volume = Mathf.Lerp(startVolume, 0, elapsed);
                yield return null;
            }
            
            _ambientSource.clip = newClip;
            _ambientSource.Play();
            
            elapsed = 0;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                _ambientSource.volume = Mathf.Lerp(0, _ambientVolume, elapsed);
                yield return null;
            }
        }

        public void StopAmbient(bool fade = true)
        {
            if (fade)
            {
                StartCoroutine(FadeOutAmbient());
            }
            else
            {
                _ambientSource.Stop();
            }
        }

        private IEnumerator FadeOutAmbient()
        {
            float elapsed = 0;
            float startVolume = _ambientSource.volume;
            
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                _ambientSource.volume = Mathf.Lerp(startVolume, 0, elapsed);
                yield return null;
            }
            
            _ambientSource.Stop();
            _ambientSource.volume = _ambientVolume;
        }

        #endregion

        #region Volume Control

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = _masterVolume;
            PlayerPrefs.SetFloat("MasterVolume", _masterVolume);
            OnVolumeChanged?.Invoke(_masterVolume);
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            _musicSource.volume = _musicVolume;
            PlayerPrefs.SetFloat("MusicVolume", _musicVolume);
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("SFXVolume", _sfxVolume);
        }

        public void SetUIVolume(float volume)
        {
            _uiVolume = Mathf.Clamp01(volume);
            _uiSource.volume = _uiVolume;
            PlayerPrefs.SetFloat("UIVolume", _uiVolume);
        }

        public void SetAmbientVolume(float volume)
        {
            _ambientVolume = Mathf.Clamp01(volume);
            _ambientSource.volume = _ambientVolume;
            PlayerPrefs.SetFloat("AmbientVolume", _ambientVolume);
        }

        public float GetMasterVolume() => _masterVolume;
        public float GetMusicVolume() => _musicVolume;
        public float GetSFXVolume() => _sfxVolume;
        public float GetUIVolume() => _uiVolume;
        public float GetAmbientVolume() => _ambientVolume;

        public void MuteAll()
        {
            AudioListener.volume = 0;
        }

        public void UnmuteAll()
        {
            AudioListener.volume = _masterVolume;
        }

        #endregion

        #region Asset Loading

        /// <summary>
        /// Load an audio clip from Resources folder
        /// </summary>
        public void LoadClipFromResources(string category, string id, string resourcePath)
        {
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null)
            {
                switch (category.ToLower())
                {
                    case "music":
                        _musicTracks[id] = clip;
                        break;
                    case "sfx":
                        _sfxClips[id] = clip;
                        break;
                    case "ui":
                        _uiClips[id] = clip;
                        break;
                    case "ambient":
                        _ambientClips[id] = clip;
                        break;
                }
                Debug.Log($"[SoundManager] Loaded {category}/{id} from {resourcePath}");
            }
            else
            {
                Debug.LogWarning($"[SoundManager] Failed to load: {resourcePath}");
            }
        }

        /// <summary>
        /// Directly assign an audio clip
        /// </summary>
        public void RegisterClip(string category, string id, AudioClip clip)
        {
            switch (category.ToLower())
            {
                case "music":
                    _musicTracks[id] = clip;
                    break;
                case "sfx":
                    _sfxClips[id] = clip;
                    break;
                case "ui":
                    _uiClips[id] = clip;
                    break;
                case "ambient":
                    _ambientClips[id] = clip;
                    break;
            }
        }

        #endregion
    }
}
