using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.Localization
{
    /// <summary>
    /// Component that automatically updates UI text based on localization key.
    /// Supports both legacy Text and TextMeshPro components.
    /// </summary>
    [AddComponentMenu("Apex/Localization/Localized Text")]
    public class LocalizedText : MonoBehaviour
    {
        [Header("Localization")]
        [SerializeField] private string localizationKey;
        [SerializeField] private bool updateOnLanguageChange = true;
        [SerializeField] private bool updateOnStart = true;

        [Header("Formatting")]
        [SerializeField] private bool useFormatting = false;
        [SerializeField] private string[] formatArguments;

        [Header("Text Case")]
        [SerializeField] private TextCase textCase = TextCase.None;

        [Header("RTL Support")]
        [SerializeField] private bool autoReverseForRTL = true;

        // Text components
        private Text legacyText;
        private TextMeshProUGUI tmpText;
        private TextMeshPro tmpText3D;

        // Original values for animation
        private string originalKey;

        public enum TextCase
        {
            None,
            Upper,
            Lower,
            Title,
            Sentence
        }

        public string Key
        {
            get => localizationKey;
            set
            {
                localizationKey = value;
                UpdateText();
            }
        }

        private void Awake()
        {
            // Cache text components
            legacyText = GetComponent<Text>();
            tmpText = GetComponent<TextMeshProUGUI>();
            tmpText3D = GetComponent<TextMeshPro>();

            originalKey = localizationKey;
        }

        private void OnEnable()
        {
            // Subscribe to language changes
            if (updateOnLanguageChange && LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }

        private void Start()
        {
            if (updateOnStart)
            {
                // Wait for localization to be ready
                if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsInitialized)
                {
                    UpdateText();
                }
                else if (LocalizationManager.Instance != null)
                {
                    LocalizationManager.Instance.OnLocalizationReady += OnLocalizationReady;
                }
            }
        }

        private void OnLocalizationReady()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLocalizationReady -= OnLocalizationReady;
            }
            UpdateText();
        }

        private void OnLanguageChanged(SystemLanguage newLanguage)
        {
            UpdateText();
        }

        /// <summary>
        /// Update the text with current localization
        /// </summary>
        public void UpdateText()
        {
            if (string.IsNullOrEmpty(localizationKey)) return;
            if (LocalizationManager.Instance == null) return;

            string localizedText;

            if (useFormatting && formatArguments != null && formatArguments.Length > 0)
            {
                // Get format arguments as objects
                object[] args = new object[formatArguments.Length];
                for (int i = 0; i < formatArguments.Length; i++)
                {
                    args[i] = formatArguments[i];
                }
                localizedText = LocalizationManager.Instance.GetFormat(localizationKey, args);
            }
            else
            {
                localizedText = LocalizationManager.Instance.Get(localizationKey);
            }

            // Apply text case
            localizedText = ApplyTextCase(localizedText);

            // Handle RTL
            if (autoReverseForRTL && LocalizationManager.Instance.IsRTL)
            {
                localizedText = ReverseTextForRTL(localizedText);
                SetRTLAlignment();
            }

            // Set text
            SetText(localizedText);
        }

        /// <summary>
        /// Update text with dynamic format arguments
        /// </summary>
        public void UpdateTextWithArgs(params object[] args)
        {
            if (string.IsNullOrEmpty(localizationKey)) return;
            if (LocalizationManager.Instance == null) return;

            string localizedText = LocalizationManager.Instance.GetFormat(localizationKey, args);
            localizedText = ApplyTextCase(localizedText);

            if (autoReverseForRTL && LocalizationManager.Instance.IsRTL)
            {
                localizedText = ReverseTextForRTL(localizedText);
            }

            SetText(localizedText);
        }

        /// <summary>
        /// Set a new key and update immediately
        /// </summary>
        public void SetKey(string newKey)
        {
            localizationKey = newKey;
            UpdateText();
        }

        /// <summary>
        /// Set a new key with format arguments
        /// </summary>
        public void SetKeyWithArgs(string newKey, params object[] args)
        {
            localizationKey = newKey;
            UpdateTextWithArgs(args);
        }

        /// <summary>
        /// Reset to original key
        /// </summary>
        public void ResetToOriginalKey()
        {
            localizationKey = originalKey;
            UpdateText();
        }

        private void SetText(string text)
        {
            if (tmpText != null)
            {
                tmpText.text = text;
            }
            else if (tmpText3D != null)
            {
                tmpText3D.text = text;
            }
            else if (legacyText != null)
            {
                legacyText.text = text;
            }
        }

        private string ApplyTextCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            switch (textCase)
            {
                case TextCase.Upper:
                    return text.ToUpper();
                case TextCase.Lower:
                    return text.ToLower();
                case TextCase.Title:
                    return ToTitleCase(text);
                case TextCase.Sentence:
                    return ToSentenceCase(text);
                default:
                    return text;
            }
        }

        private string ToTitleCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            char[] chars = text.ToLower().ToCharArray();
            bool capitalizeNext = true;

            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsWhiteSpace(chars[i]))
                {
                    capitalizeNext = true;
                }
                else if (capitalizeNext && char.IsLetter(chars[i]))
                {
                    chars[i] = char.ToUpper(chars[i]);
                    capitalizeNext = false;
                }
            }

            return new string(chars);
        }

        private string ToSentenceCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            char[] chars = text.ToLower().ToCharArray();
            bool capitalizeNext = true;

            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] == '.' || chars[i] == '!' || chars[i] == '?')
                {
                    capitalizeNext = true;
                }
                else if (capitalizeNext && char.IsLetter(chars[i]))
                {
                    chars[i] = char.ToUpper(chars[i]);
                    capitalizeNext = false;
                }
            }

            return new string(chars);
        }

        private string ReverseTextForRTL(string text)
        {
            // Basic RTL reversal - for production, use proper Unicode bidirectional algorithm
            char[] chars = text.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        private void SetRTLAlignment()
        {
            // Flip horizontal alignment for RTL languages
            if (tmpText != null)
            {
                if (tmpText.alignment == TextAlignmentOptions.Left)
                    tmpText.alignment = TextAlignmentOptions.Right;
                else if (tmpText.alignment == TextAlignmentOptions.Right)
                    tmpText.alignment = TextAlignmentOptions.Left;
            }
            else if (legacyText != null)
            {
                if (legacyText.alignment == TextAnchor.UpperLeft ||
                    legacyText.alignment == TextAnchor.MiddleLeft ||
                    legacyText.alignment == TextAnchor.LowerLeft)
                {
                    legacyText.alignment = (TextAnchor)((int)legacyText.alignment + 2);
                }
                else if (legacyText.alignment == TextAnchor.UpperRight ||
                         legacyText.alignment == TextAnchor.MiddleRight ||
                         legacyText.alignment == TextAnchor.LowerRight)
                {
                    legacyText.alignment = (TextAnchor)((int)legacyText.alignment - 2);
                }
            }
        }
    }

    /// <summary>
    /// Attribute to mark string fields as localization keys in the editor
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LocalizationKeyAttribute : PropertyAttribute
    {
        public string Category { get; private set; }

        public LocalizationKeyAttribute(string category = null)
        {
            Category = category;
        }
    }

    /// <summary>
    /// Component for localizing images based on language
    /// </summary>
    [AddComponentMenu("Apex/Localization/Localized Image")]
    public class LocalizedImage : MonoBehaviour
    {
        [Header("Localization")]
        [SerializeField] private string imageKey;
        [SerializeField] private bool updateOnLanguageChange = true;

        [Header("Image Variants")]
        [SerializeField] private LocalizedSprite[] sprites;

        private Image image;
        private SpriteRenderer spriteRenderer;

        [Serializable]
        public class LocalizedSprite
        {
            public SystemLanguage language;
            public Sprite sprite;
        }

        private void Awake()
        {
            image = GetComponent<Image>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (updateOnLanguageChange && LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }

        private void Start()
        {
            UpdateImage();
        }

        private void OnLanguageChanged(SystemLanguage newLanguage)
        {
            UpdateImage();
        }

        public void UpdateImage()
        {
            if (LocalizationManager.Instance == null) return;

            SystemLanguage currentLang = LocalizationManager.Instance.CurrentLanguage;
            Sprite targetSprite = null;

            // Find matching sprite
            foreach (var localizedSprite in sprites)
            {
                if (localizedSprite.language == currentLang)
                {
                    targetSprite = localizedSprite.sprite;
                    break;
                }
            }

            // Fall back to English
            if (targetSprite == null)
            {
                foreach (var localizedSprite in sprites)
                {
                    if (localizedSprite.language == SystemLanguage.English)
                    {
                        targetSprite = localizedSprite.sprite;
                        break;
                    }
                }
            }

            // Apply sprite
            if (targetSprite != null)
            {
                if (image != null)
                {
                    image.sprite = targetSprite;
                }
                else if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = targetSprite;
                }
            }
        }
    }

    /// <summary>
    /// Component for localizing audio clips based on language
    /// </summary>
    [AddComponentMenu("Apex/Localization/Localized Audio")]
    public class LocalizedAudio : MonoBehaviour
    {
        [Header("Localization")]
        [SerializeField] private string audioKey;
        [SerializeField] private bool updateOnLanguageChange = true;
        [SerializeField] private bool playOnStart = false;

        [Header("Audio Variants")]
        [SerializeField] private LocalizedClip[] clips;

        private AudioSource audioSource;

        [Serializable]
        public class LocalizedClip
        {
            public SystemLanguage language;
            public AudioClip clip;
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (updateOnLanguageChange && LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }

        private void Start()
        {
            UpdateAudio();
            
            if (playOnStart && audioSource != null)
            {
                audioSource.Play();
            }
        }

        private void OnLanguageChanged(SystemLanguage newLanguage)
        {
            bool wasPlaying = audioSource != null && audioSource.isPlaying;
            UpdateAudio();
            
            if (wasPlaying && audioSource != null)
            {
                audioSource.Play();
            }
        }

        public void UpdateAudio()
        {
            if (LocalizationManager.Instance == null) return;
            if (audioSource == null) return;

            SystemLanguage currentLang = LocalizationManager.Instance.CurrentLanguage;
            AudioClip targetClip = null;

            foreach (var localizedClip in clips)
            {
                if (localizedClip.language == currentLang)
                {
                    targetClip = localizedClip.clip;
                    break;
                }
            }

            // Fall back to English
            if (targetClip == null)
            {
                foreach (var localizedClip in clips)
                {
                    if (localizedClip.language == SystemLanguage.English)
                    {
                        targetClip = localizedClip.clip;
                        break;
                    }
                }
            }

            if (targetClip != null)
            {
                audioSource.clip = targetClip;
            }
        }

        public void Play()
        {
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }
    }
}
