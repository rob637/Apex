using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ApexCitadels.Audio
{
    /// <summary>
    /// Central audio management system for Apex Citadels.
    /// Handles audio mixer, volume controls, and coordinates all audio subsystems.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        
        [Header("Mixer Groups")]
        [SerializeField] private AudioMixerGroup masterGroup;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup ambientGroup;
        [SerializeField] private AudioMixerGroup uiGroup;
        [SerializeField] private AudioMixerGroup voiceGroup;

        [Header("Volume Settings")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float ambientVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float uiVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float voiceVolume = 1f;

        [Header("Settings")]
        [SerializeField] private bool muteWhenUnfocused = true;
        [SerializeField] private bool enableVibration = true;
        [SerializeField] private bool enable3DAudio = true;

        // Mixer parameter names
        private const string MASTER_VOLUME_PARAM = "MasterVolume";
        private const string MUSIC_VOLUME_PARAM = "MusicVolume";
        private const string SFX_VOLUME_PARAM = "SFXVolume";
        private const string AMBIENT_VOLUME_PARAM = "AmbientVolume";
        private const string UI_VOLUME_PARAM = "UIVolume";
        private const string VOICE_VOLUME_PARAM = "VoiceVolume";

        // Player prefs keys
        private const string PREF_MASTER_VOLUME = "Audio_MasterVolume";
        private const string PREF_MUSIC_VOLUME = "Audio_MusicVolume";
        private const string PREF_SFX_VOLUME = "Audio_SFXVolume";
        private const string PREF_AMBIENT_VOLUME = "Audio_AmbientVolume";
        private const string PREF_UI_VOLUME = "Audio_UIVolume";
        private const string PREF_VOICE_VOLUME = "Audio_VoiceVolume";
        private const string PREF_MUTE_UNFOCUSED = "Audio_MuteUnfocused";
        private const string PREF_VIBRATION = "Audio_Vibration";
        private const string PREF_3D_AUDIO = "Audio_3DAudio";

        // Events
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;
        public event Action<float> OnAmbientVolumeChanged;
        public event Action<float> OnUIVolumeChanged;
        public event Action<float> OnVoiceVolumeChanged;
        public event Action<bool> OnMuteStateChanged;

        // State
        private bool isMuted = false;
        private float previousMasterVolume;
        private bool isInitialized = false;

        // Properties
        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;
        public float AmbientVolume => ambientVolume;
        public float UIVolume => uiVolume;
        public float VoiceVolume => voiceVolume;
        public bool IsMuted => isMuted;

        /// <summary>
        /// Play a sound effect by name (stub method for compatibility)
        /// </summary>
        public void PlaySFX(string soundName)
        {
            // Stub implementation - can be enhanced with actual SFX playback
            ApexLogger.Log($"[AudioManager] PlaySFX: {soundName}", ApexLogger.LogCategory.Audio);
        }
        public bool VibrationEnabled => enableVibration;
        public bool Is3DAudioEnabled => enable3DAudio;

        public AudioMixerGroup MasterGroup => masterGroup;
        public AudioMixerGroup MusicGroup => musicGroup;
        public AudioMixerGroup SFXGroup => sfxGroup;
        public AudioMixerGroup AmbientGroup => ambientGroup;
        public AudioMixerGroup UIGroup => uiGroup;
        public AudioMixerGroup VoiceGroup => voiceGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettings();
            ApplyAllVolumes();
            isInitialized = true;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!isInitialized) return;

            if (muteWhenUnfocused)
            {
                if (!hasFocus && !isMuted)
                {
                    SetMixerVolume(MASTER_VOLUME_PARAM, 0f);
                }
                else if (hasFocus && !isMuted)
                {
                    SetMixerVolume(MASTER_VOLUME_PARAM, masterVolume);
                }
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!isInitialized) return;

            if (muteWhenUnfocused)
            {
                if (isPaused && !isMuted)
                {
                    SetMixerVolume(MASTER_VOLUME_PARAM, 0f);
                }
                else if (!isPaused && !isMuted)
                {
                    SetMixerVolume(MASTER_VOLUME_PARAM, masterVolume);
                }
            }
        }

        #region Volume Control

        /// <summary>
        /// Set the master volume (0-1)
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            SetMixerVolume(MASTER_VOLUME_PARAM, isMuted ? 0f : masterVolume);
            PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, masterVolume);
            OnMasterVolumeChanged?.Invoke(masterVolume);
        }

        /// <summary>
        /// Set the music volume (0-1)
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            SetMixerVolume(MUSIC_VOLUME_PARAM, musicVolume);
            PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, musicVolume);
            OnMusicVolumeChanged?.Invoke(musicVolume);
        }

        /// <summary>
        /// Set the SFX volume (0-1)
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            SetMixerVolume(SFX_VOLUME_PARAM, sfxVolume);
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, sfxVolume);
            OnSFXVolumeChanged?.Invoke(sfxVolume);
        }

        /// <summary>
        /// Set the ambient volume (0-1)
        /// </summary>
        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            SetMixerVolume(AMBIENT_VOLUME_PARAM, ambientVolume);
            PlayerPrefs.SetFloat(PREF_AMBIENT_VOLUME, ambientVolume);
            OnAmbientVolumeChanged?.Invoke(ambientVolume);
        }

        /// <summary>
        /// Set the UI volume (0-1)
        /// </summary>
        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            SetMixerVolume(UI_VOLUME_PARAM, uiVolume);
            PlayerPrefs.SetFloat(PREF_UI_VOLUME, uiVolume);
            OnUIVolumeChanged?.Invoke(uiVolume);
        }

        /// <summary>
        /// Set the voice volume (0-1)
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            voiceVolume = Mathf.Clamp01(volume);
            SetMixerVolume(VOICE_VOLUME_PARAM, voiceVolume);
            PlayerPrefs.SetFloat(PREF_VOICE_VOLUME, voiceVolume);
            OnVoiceVolumeChanged?.Invoke(voiceVolume);
        }

        /// <summary>
        /// Toggle mute state
        /// </summary>
        public void ToggleMute()
        {
            SetMuted(!isMuted);
        }

        /// <summary>
        /// Set mute state
        /// </summary>
        public void SetMuted(bool muted)
        {
            isMuted = muted;
            SetMixerVolume(MASTER_VOLUME_PARAM, isMuted ? 0f : masterVolume);
            OnMuteStateChanged?.Invoke(isMuted);
        }

        /// <summary>
        /// Enable or disable vibration/haptics
        /// </summary>
        public void SetVibrationEnabled(bool enabled)
        {
            enableVibration = enabled;
            PlayerPrefs.SetInt(PREF_VIBRATION, enabled ? 1 : 0);
        }

        /// <summary>
        /// Enable or disable 3D spatial audio
        /// </summary>
        public void Set3DAudioEnabled(bool enabled)
        {
            enable3DAudio = enabled;
            PlayerPrefs.SetInt(PREF_3D_AUDIO, enabled ? 1 : 0);
        }

        /// <summary>
        /// Enable or disable muting when app loses focus
        /// </summary>
        public void SetMuteWhenUnfocused(bool enabled)
        {
            muteWhenUnfocused = enabled;
            PlayerPrefs.SetInt(PREF_MUTE_UNFOCUSED, enabled ? 1 : 0);
        }

        #endregion

        #region Haptic Feedback

        /// <summary>
        /// Trigger light haptic feedback
        /// </summary>
        public void PlayHapticLight()
        {
            if (!enableVibration) return;
            
            #if UNITY_IOS
            // iOS Taptic Engine - Light
            iOSHapticFeedback.Trigger(iOSHapticFeedback.Type.Light);
            #elif UNITY_ANDROID
            Handheld.Vibrate();
            #endif
        }

        /// <summary>
        /// Trigger medium haptic feedback
        /// </summary>
        public void PlayHapticMedium()
        {
            if (!enableVibration) return;
            
            #if UNITY_IOS
            iOSHapticFeedback.Trigger(iOSHapticFeedback.Type.Medium);
            #elif UNITY_ANDROID
            Handheld.Vibrate();
            #endif
        }

        /// <summary>
        /// Trigger heavy haptic feedback
        /// </summary>
        public void PlayHapticHeavy()
        {
            if (!enableVibration) return;
            
            #if UNITY_IOS
            iOSHapticFeedback.Trigger(iOSHapticFeedback.Type.Heavy);
            #elif UNITY_ANDROID
            Handheld.Vibrate();
            #endif
        }

        /// <summary>
        /// Trigger success haptic pattern
        /// </summary>
        public void PlayHapticSuccess()
        {
            if (!enableVibration) return;
            
            #if UNITY_IOS
            iOSHapticFeedback.Trigger(iOSHapticFeedback.Type.Success);
            #elif UNITY_ANDROID
            Handheld.Vibrate();
            #endif
        }

        /// <summary>
        /// Trigger error haptic pattern
        /// </summary>
        public void PlayHapticError()
        {
            if (!enableVibration) return;
            
            #if UNITY_IOS
            iOSHapticFeedback.Trigger(iOSHapticFeedback.Type.Error);
            #elif UNITY_ANDROID
            Handheld.Vibrate();
            #endif
        }

        /// <summary>
        /// Trigger warning haptic pattern
        /// </summary>
        public void PlayHapticWarning()
        {
            if (!enableVibration) return;
            
            #if UNITY_IOS
            iOSHapticFeedback.Trigger(iOSHapticFeedback.Type.Warning);
            #elif UNITY_ANDROID
            Handheld.Vibrate();
            #endif
        }

        #endregion

        #region Audio Settings

        /// <summary>
        /// Get all current audio settings
        /// </summary>
        public AudioSettings GetSettings()
        {
            return new AudioSettings
            {
                masterVolume = masterVolume,
                musicVolume = musicVolume,
                sfxVolume = sfxVolume,
                ambientVolume = ambientVolume,
                uiVolume = uiVolume,
                voiceVolume = voiceVolume,
                muteWhenUnfocused = muteWhenUnfocused,
                vibrationEnabled = enableVibration,
                enable3DAudio = enable3DAudio
            };
        }

        /// <summary>
        /// Apply audio settings
        /// </summary>
        public void ApplySettings(AudioSettings settings)
        {
            SetMasterVolume(settings.masterVolume);
            SetMusicVolume(settings.musicVolume);
            SetSFXVolume(settings.sfxVolume);
            SetAmbientVolume(settings.ambientVolume);
            SetUIVolume(settings.uiVolume);
            SetVoiceVolume(settings.voiceVolume);
            SetMuteWhenUnfocused(settings.muteWhenUnfocused);
            SetVibrationEnabled(settings.vibrationEnabled);
            Set3DAudioEnabled(settings.enable3DAudio);
        }

        /// <summary>
        /// Reset all audio settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            SetMasterVolume(1f);
            SetMusicVolume(0.7f);
            SetSFXVolume(1f);
            SetAmbientVolume(0.5f);
            SetUIVolume(1f);
            SetVoiceVolume(1f);
            SetMuteWhenUnfocused(true);
            SetVibrationEnabled(true);
            Set3DAudioEnabled(true);
            SetMuted(false);
        }

        #endregion

        #region Private Methods

        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
            musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, 0.7f);
            sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f);
            ambientVolume = PlayerPrefs.GetFloat(PREF_AMBIENT_VOLUME, 0.5f);
            uiVolume = PlayerPrefs.GetFloat(PREF_UI_VOLUME, 1f);
            voiceVolume = PlayerPrefs.GetFloat(PREF_VOICE_VOLUME, 1f);
            muteWhenUnfocused = PlayerPrefs.GetInt(PREF_MUTE_UNFOCUSED, 1) == 1;
            enableVibration = PlayerPrefs.GetInt(PREF_VIBRATION, 1) == 1;
            enable3DAudio = PlayerPrefs.GetInt(PREF_3D_AUDIO, 1) == 1;
        }

        private void ApplyAllVolumes()
        {
            SetMixerVolume(MASTER_VOLUME_PARAM, masterVolume);
            SetMixerVolume(MUSIC_VOLUME_PARAM, musicVolume);
            SetMixerVolume(SFX_VOLUME_PARAM, sfxVolume);
            SetMixerVolume(AMBIENT_VOLUME_PARAM, ambientVolume);
            SetMixerVolume(UI_VOLUME_PARAM, uiVolume);
            SetMixerVolume(VOICE_VOLUME_PARAM, voiceVolume);
        }

        private void SetMixerVolume(string parameter, float linearVolume)
        {
            if (audioMixer == null) return;

            // Convert linear (0-1) to decibels (-80 to 0)
            float dbVolume = linearVolume > 0.0001f 
                ? Mathf.Log10(linearVolume) * 20f 
                : -80f;

            audioMixer.SetFloat(parameter, dbVolume);
        }

        private float GetMixerVolume(string parameter)
        {
            if (audioMixer == null) return 1f;

            float dbVolume;
            if (audioMixer.GetFloat(parameter, out dbVolume))
            {
                // Convert decibels to linear
                return Mathf.Pow(10f, dbVolume / 20f);
            }
            return 1f;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Create a one-shot audio source at a position
        /// </summary>
        public AudioSource CreateOneShotSource(Vector3 position, AudioMixerGroup group = null)
        {
            GameObject obj = new GameObject("OneShot_Audio");
            obj.transform.position = position;
            
            AudioSource source = obj.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = group ?? sfxGroup;
            source.playOnAwake = false;
            source.spatialBlend = enable3DAudio ? 1f : 0f;
            
            return source;
        }

        /// <summary>
        /// Play a clip at a position (one-shot, auto-destroys)
        /// </summary>
        public void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f, AudioMixerGroup group = null)
        {
            if (clip == null) return;

            GameObject obj = new GameObject("OneShot_" + clip.name);
            obj.transform.position = position;
            
            AudioSource source = obj.AddComponent<AudioSource>();
            source.clip = clip;
            source.outputAudioMixerGroup = group ?? sfxGroup;
            source.volume = volume;
            source.spatialBlend = enable3DAudio ? 1f : 0f;
            source.Play();
            
            Destroy(obj, clip.length + 0.1f);
        }

        /// <summary>
        /// Get the linear volume for a mixer group
        /// </summary>
        public float GetVolumeForGroup(AudioMixerGroup group)
        {
            if (group == masterGroup) return masterVolume;
            if (group == musicGroup) return musicVolume;
            if (group == sfxGroup) return sfxVolume;
            if (group == ambientGroup) return ambientVolume;
            if (group == uiGroup) return uiVolume;
            if (group == voiceGroup) return voiceVolume;
            return 1f;
        }

        #endregion
    }

    /// <summary>
    /// Audio settings data structure
    /// </summary>
    [Serializable]
    public struct AudioSettings
    {
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;
        public float ambientVolume;
        public float uiVolume;
        public float voiceVolume;
        public bool muteWhenUnfocused;
        public bool vibrationEnabled;
        public bool enable3DAudio;
    }

    #if UNITY_IOS
    /// <summary>
    /// iOS Haptic Feedback helper (requires native plugin for full implementation)
    /// </summary>
    public static class iOSHapticFeedback
    {
        public enum Type
        {
            Light,
            Medium,
            Heavy,
            Success,
            Warning,
            Error,
            Selection
        }

        public static void Trigger(Type type)
        {
            // In a real implementation, this would call native iOS code
            // For now, use basic vibration as fallback
            #if !UNITY_EDITOR
            Handheld.Vibrate();
            #endif
        }
    }
    #endif
}
