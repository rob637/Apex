using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_PIPELINE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace ApexCitadels.Core
{
    /// <summary>
    /// Quality presets manager for Apex Citadels.
    /// Provides Low/Medium/High/Ultra quality settings.
    /// 
    /// Usage:
    ///   QualityPresetsManager.SetQuality(QualityLevel.High);
    ///   QualityPresetsManager.AutoDetectQuality();
    ///   var current = QualityPresetsManager.CurrentLevel;
    /// </summary>
    public class QualityPresetsManager : MonoBehaviour
    {
        public static QualityPresetsManager Instance { get; private set; }

        #region Enums

        public enum QualityLevel
        {
            Low,
            Medium,
            High,
            Ultra,
            Custom
        }

        public enum AntiAliasingMode
        {
            None,
            FXAA,
            SMAA,
            MSAA2x,
            MSAA4x,
            MSAA8x
        }

        #endregion

        #region Serialized Fields

        [Header("Current Settings")]
        [SerializeField] private QualityLevel currentLevel = QualityLevel.High;

        [Header("Presets")]
        [SerializeField] private QualityPreset lowPreset;
        [SerializeField] private QualityPreset mediumPreset;
        [SerializeField] private QualityPreset highPreset;
        [SerializeField] private QualityPreset ultraPreset;

        [Header("Auto-Detect")]
        [SerializeField] private bool autoDetectOnStart = true;
        [SerializeField] private int lowMemoryThresholdMB = 2048;
        [SerializeField] private int mediumMemoryThresholdMB = 4096;
        [SerializeField] private int highMemoryThresholdMB = 8192;

        #endregion

        #region Events

        public static event Action<QualityLevel> OnQualityChanged;
        public static event Action<QualityPreset> OnPresetApplied;

        #endregion

        #region Properties

        public static QualityLevel CurrentLevel => Instance?.currentLevel ?? QualityLevel.Medium;
        public static QualityPreset CurrentPreset => Instance?.GetPreset(CurrentLevel);

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize default presets if not set
            InitializeDefaultPresets();

            // Load saved quality level
            LoadSavedQuality();

            if (autoDetectOnStart && !HasSavedQuality())
            {
                AutoDetectQuality();
            }
            else
            {
                ApplyPreset(currentLevel);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Set quality level
        /// </summary>
        public static void SetQuality(QualityLevel level)
        {
            EnsureInstance();
            Instance.SetQualityInternal(level);
        }

        /// <summary>
        /// Auto-detect best quality based on hardware
        /// </summary>
        public static QualityLevel AutoDetectQuality()
        {
            EnsureInstance();
            return Instance.AutoDetectQualityInternal();
        }

        /// <summary>
        /// Get preset for a quality level
        /// </summary>
        public static QualityPreset GetPresetFor(QualityLevel level)
        {
            return Instance?.GetPreset(level);
        }

        /// <summary>
        /// Apply custom preset
        /// </summary>
        public static void ApplyCustomPreset(QualityPreset preset)
        {
            EnsureInstance();
            Instance.ApplyPresetInternal(preset);
            Instance.currentLevel = QualityLevel.Custom;
        }

        /// <summary>
        /// Get quality level names
        /// </summary>
        public static string[] GetQualityLevelNames()
        {
            return Enum.GetNames(typeof(QualityLevel));
        }

        #endregion

        #region Instance Methods

        private void SetQualityInternal(QualityLevel level)
        {
            if (level == currentLevel && level != QualityLevel.Custom)
                return;

            currentLevel = level;
            ApplyPreset(level);
            SaveQuality(level);

            ApexLogger.Log($"Quality set to: {level}", ApexLogger.LogCategory.Performance);
            OnQualityChanged?.Invoke(level);
        }

        private QualityLevel AutoDetectQualityInternal()
        {
            int systemMemory = SystemInfo.systemMemorySize;
            int graphicsMemory = SystemInfo.graphicsMemorySize;
            int processorCount = SystemInfo.processorCount;
            string graphicsDevice = SystemInfo.graphicsDeviceName.ToLower();

            QualityLevel detected;

            // Check for integrated graphics
            bool isIntegrated = graphicsDevice.Contains("intel") ||
                               graphicsDevice.Contains("integrated") ||
                               graphicsMemory < 1024;

            if (isIntegrated || systemMemory < lowMemoryThresholdMB)
            {
                detected = QualityLevel.Low;
            }
            else if (systemMemory < mediumMemoryThresholdMB || graphicsMemory < 2048)
            {
                detected = QualityLevel.Medium;
            }
            else if (systemMemory < highMemoryThresholdMB || graphicsMemory < 4096)
            {
                detected = QualityLevel.High;
            }
            else
            {
                detected = QualityLevel.Ultra;
            }

            ApexLogger.Log($"Auto-detected quality: {detected} (RAM: {systemMemory}MB, VRAM: {graphicsMemory}MB, Cores: {processorCount})", ApexLogger.LogCategory.Performance);

            SetQualityInternal(detected);
            return detected;
        }

        private void ApplyPreset(QualityLevel level)
        {
            var preset = GetPreset(level);
            if (preset != null)
            {
                ApplyPresetInternal(preset);
            }
        }

        private void ApplyPresetInternal(QualityPreset preset)
        {
            if (preset == null) return;

            // Unity Quality Settings
            QualitySettings.SetQualityLevel(preset.UnityQualityLevel, true);

            // Resolution
            if (preset.ResolutionScale < 1f)
            {
                // Apply resolution scaling
                int width = Mathf.RoundToInt(Screen.currentResolution.width * preset.ResolutionScale);
                int height = Mathf.RoundToInt(Screen.currentResolution.height * preset.ResolutionScale);
                Screen.SetResolution(width, height, Screen.fullScreenMode);
            }

            // Shadows
            QualitySettings.shadows = preset.ShadowQuality;
            QualitySettings.shadowResolution = preset.ShadowResolution;
            QualitySettings.shadowDistance = preset.ShadowDistance;
            QualitySettings.shadowCascades = preset.ShadowCascades;

            // Textures
            QualitySettings.globalTextureMipmapLimit = preset.TextureQuality;
            QualitySettings.anisotropicFiltering = preset.AnisotropicFiltering;

            // Anti-aliasing
            QualitySettings.antiAliasing = GetMSAALevel(preset.AntiAliasing);

            // Other
            QualitySettings.lodBias = preset.LODBias;
            QualitySettings.maximumLODLevel = preset.MaxLODLevel;
            QualitySettings.vSyncCount = preset.VSync ? 1 : 0;

            // Frame rate
            Application.targetFrameRate = preset.TargetFrameRate;

            // Post-processing (URP specific)
            ApplyPostProcessingSettings(preset);

            ApexLogger.Log($"Applied preset: {preset.Name} (Unity Level: {preset.UnityQualityLevel})", ApexLogger.LogCategory.Performance);
            OnPresetApplied?.Invoke(preset);
        }

        private void ApplyPostProcessingSettings(QualityPreset preset)
        {
#if UNITY_PIPELINE_URP
            // Find URP asset and modify
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                // These would need to be set via a custom URP asset per quality level
                // For now, log what we'd want to set
                ApexLogger.LogVerbose($"URP Settings - SSAO: {preset.EnableSSAO}, Bloom: {preset.EnableBloom}");
            }
#endif
        }

        private QualityPreset GetPreset(QualityLevel level)
        {
            return level switch
            {
                QualityLevel.Low => lowPreset,
                QualityLevel.Medium => mediumPreset,
                QualityLevel.High => highPreset,
                QualityLevel.Ultra => ultraPreset,
                _ => mediumPreset
            };
        }

        private int GetMSAALevel(AntiAliasingMode mode)
        {
            return mode switch
            {
                AntiAliasingMode.MSAA2x => 2,
                AntiAliasingMode.MSAA4x => 4,
                AntiAliasingMode.MSAA8x => 8,
                _ => 0
            };
        }

        private void InitializeDefaultPresets()
        {
            if (lowPreset == null)
                lowPreset = CreateLowPreset();
            if (mediumPreset == null)
                mediumPreset = CreateMediumPreset();
            if (highPreset == null)
                highPreset = CreateHighPreset();
            if (ultraPreset == null)
                ultraPreset = CreateUltraPreset();
        }

        private QualityPreset CreateLowPreset()
        {
            return new QualityPreset
            {
                Name = "Low",
                UnityQualityLevel = 0,
                ResolutionScale = 0.75f,
                ShadowQuality = ShadowQuality.Disable,
                ShadowResolution = ShadowResolution.Low,
                ShadowDistance = 30f,
                ShadowCascades = 1,
                TextureQuality = 2, // Quarter res
                AnisotropicFiltering = AnisotropicFiltering.Disable,
                AntiAliasing = AntiAliasingMode.None,
                LODBias = 0.5f,
                MaxLODLevel = 2,
                VSync = false,
                TargetFrameRate = 30,
                EnableSSAO = false,
                EnableBloom = false,
                EnableMotionBlur = false,
                EnableDepthOfField = false,
                ParticleQuality = 0.25f,
                DrawDistance = 500f
            };
        }

        private QualityPreset CreateMediumPreset()
        {
            return new QualityPreset
            {
                Name = "Medium",
                UnityQualityLevel = 2,
                ResolutionScale = 1f,
                ShadowQuality = ShadowQuality.HardOnly,
                ShadowResolution = ShadowResolution.Medium,
                ShadowDistance = 75f,
                ShadowCascades = 2,
                TextureQuality = 1, // Half res
                AnisotropicFiltering = AnisotropicFiltering.Enable,
                AntiAliasing = AntiAliasingMode.FXAA,
                LODBias = 1f,
                MaxLODLevel = 1,
                VSync = false,
                TargetFrameRate = 60,
                EnableSSAO = false,
                EnableBloom = true,
                EnableMotionBlur = false,
                EnableDepthOfField = false,
                ParticleQuality = 0.5f,
                DrawDistance = 1000f
            };
        }

        private QualityPreset CreateHighPreset()
        {
            return new QualityPreset
            {
                Name = "High",
                UnityQualityLevel = 4,
                ResolutionScale = 1f,
                ShadowQuality = ShadowQuality.All,
                ShadowResolution = ShadowResolution.High,
                ShadowDistance = 150f,
                ShadowCascades = 4,
                TextureQuality = 0, // Full res
                AnisotropicFiltering = AnisotropicFiltering.ForceEnable,
                AntiAliasing = AntiAliasingMode.SMAA,
                LODBias = 1.5f,
                MaxLODLevel = 0,
                VSync = true,
                TargetFrameRate = -1, // Unlimited
                EnableSSAO = true,
                EnableBloom = true,
                EnableMotionBlur = false,
                EnableDepthOfField = true,
                ParticleQuality = 0.75f,
                DrawDistance = 2000f
            };
        }

        private QualityPreset CreateUltraPreset()
        {
            return new QualityPreset
            {
                Name = "Ultra",
                UnityQualityLevel = 5,
                ResolutionScale = 1f,
                ShadowQuality = ShadowQuality.All,
                ShadowResolution = ShadowResolution.VeryHigh,
                ShadowDistance = 300f,
                ShadowCascades = 4,
                TextureQuality = 0, // Full res
                AnisotropicFiltering = AnisotropicFiltering.ForceEnable,
                AntiAliasing = AntiAliasingMode.MSAA4x,
                LODBias = 2f,
                MaxLODLevel = 0,
                VSync = true,
                TargetFrameRate = -1, // Unlimited
                EnableSSAO = true,
                EnableBloom = true,
                EnableMotionBlur = true,
                EnableDepthOfField = true,
                ParticleQuality = 1f,
                DrawDistance = 5000f
            };
        }

        #endregion

        #region Persistence

        private const string QualityPrefKey = "ApexQualityLevel";

        private void SaveQuality(QualityLevel level)
        {
            PlayerPrefs.SetInt(QualityPrefKey, (int)level);
            PlayerPrefs.Save();
        }

        private void LoadSavedQuality()
        {
            if (PlayerPrefs.HasKey(QualityPrefKey))
            {
                currentLevel = (QualityLevel)PlayerPrefs.GetInt(QualityPrefKey);
            }
        }

        private bool HasSavedQuality()
        {
            return PlayerPrefs.HasKey(QualityPrefKey);
        }

        #endregion

        #region Utility

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("QualityPresetsManager");
                Instance = go.AddComponent<QualityPresetsManager>();
            }
        }

        [ContextMenu("Apply Low")]
        private void ApplyLow() => SetQuality(QualityLevel.Low);

        [ContextMenu("Apply Medium")]
        private void ApplyMedium() => SetQuality(QualityLevel.Medium);

        [ContextMenu("Apply High")]
        private void ApplyHigh() => SetQuality(QualityLevel.High);

        [ContextMenu("Apply Ultra")]
        private void ApplyUltra() => SetQuality(QualityLevel.Ultra);

        [ContextMenu("Auto-Detect")]
        private void AutoDetect() => AutoDetectQuality();

        [ContextMenu("Log Current Settings")]
        private void LogCurrentSettings()
        {
            var preset = GetPreset(currentLevel);
            ApexLogger.Log($"Current Quality: {currentLevel}\n" +
                $"  Shadows: {preset?.ShadowQuality}\n" +
                $"  Shadow Distance: {preset?.ShadowDistance}\n" +
                $"  Textures: {preset?.TextureQuality}\n" +
                $"  AA: {preset?.AntiAliasing}\n" +
                $"  VSync: {preset?.VSync}\n" +
                $"  Target FPS: {preset?.TargetFrameRate}");
        }

        #endregion
    }

    #region Quality Preset Data

    /// <summary>
    /// Quality preset configuration
    /// </summary>
    [Serializable]
    public class QualityPreset
    {
        [Header("General")]
        public string Name = "Custom";
        public int UnityQualityLevel = 2;
        public float ResolutionScale = 1f;
        public int TargetFrameRate = 60;
        public bool VSync = false;

        [Header("Shadows")]
        public ShadowQuality ShadowQuality = ShadowQuality.All;
        public ShadowResolution ShadowResolution = ShadowResolution.High;
        public float ShadowDistance = 150f;
        public int ShadowCascades = 4;

        [Header("Textures")]
        [Range(0, 3)]
        public int TextureQuality = 0; // 0 = full, 1 = half, 2 = quarter, 3 = eighth
        public AnisotropicFiltering AnisotropicFiltering = AnisotropicFiltering.ForceEnable;

        [Header("Anti-Aliasing")]
        public QualityPresetsManager.AntiAliasingMode AntiAliasing = QualityPresetsManager.AntiAliasingMode.SMAA;

        [Header("Level of Detail")]
        public float LODBias = 1.5f;
        public int MaxLODLevel = 0;
        public float DrawDistance = 2000f;

        [Header("Post-Processing")]
        public bool EnableSSAO = true;
        public bool EnableBloom = true;
        public bool EnableMotionBlur = false;
        public bool EnableDepthOfField = true;

        [Header("Effects")]
        [Range(0f, 1f)]
        public float ParticleQuality = 1f;
    }

    #endregion
}
