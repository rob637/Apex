using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.Localization
{
    /// <summary>
    /// UI panel for language selection and localization settings.
    /// </summary>
    public class LanguageSettingsUI : MonoBehaviour
    {
        [Header("UI References - Language Selection")]
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private Transform languageButtonContainer;
        [SerializeField] private GameObject languageButtonPrefab;

        [Header("UI References - Preview")]
        [SerializeField] private TextMeshProUGUI previewText;
        [SerializeField] private string[] previewKeys = { "common.hello", "common.welcome", "common.thank_you" };

        [Header("UI References - Info")]
        [SerializeField] private TextMeshProUGUI currentLanguageLabel;
        [SerializeField] private TextMeshProUGUI languageCodeLabel;
        [SerializeField] private Image rtlIndicator;

        [Header("UI References - Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button resetButton;

        [Header("Settings")]
        [SerializeField] private bool showNativeNames = true;
        [SerializeField] private bool showLanguageFlags = true;
        [SerializeField] private bool confirmBeforeChange = false;

        [Header("Flag Sprites")]
        [SerializeField] private LanguageFlag[] languageFlags;

        // State
        private SystemLanguage pendingLanguage;
        private SystemLanguage originalLanguage;
        private List<GameObject> languageButtons = new List<GameObject>();

        [Serializable]
        public class LanguageFlag
        {
            public SystemLanguage language;
            public Sprite flag;
        }

        private void Awake()
        {
            SetupButtons();
        }

        private void OnEnable()
        {
            if (LocalizationManager.Instance != null)
            {
                originalLanguage = LocalizationManager.Instance.CurrentLanguage;
                pendingLanguage = originalLanguage;
                
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }

            PopulateLanguageOptions();
            UpdateUI();
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }

        private void SetupButtons()
        {
            if (applyButton != null)
            {
                applyButton.onClick.AddListener(OnApplyClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OnResetClicked);
            }
        }

        #region Language Population

        private void PopulateLanguageOptions()
        {
            // Clear existing
            ClearLanguageButtons();

            if (languageDropdown != null)
            {
                PopulateDropdown();
            }
            else if (languageButtonContainer != null && languageButtonPrefab != null)
            {
                PopulateButtons();
            }
        }

        private void PopulateDropdown()
        {
            languageDropdown.ClearOptions();

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            int currentIndex = 0;

            for (int i = 0; i < LocalizationManager.SupportedLanguages.Length; i++)
            {
                SystemLanguage lang = LocalizationManager.SupportedLanguages[i];
                
                string displayName = showNativeNames 
                    ? LocalizationManager.GetLanguageNativeName(lang)
                    : lang.ToString();

                Sprite flagSprite = showLanguageFlags ? GetFlagForLanguage(lang) : null;

                var optionData = new TMP_Dropdown.OptionData();
                optionData.text = displayName;
                optionData.image = flagSprite;
                options.Add(optionData);

                if (LocalizationManager.Instance != null && lang == LocalizationManager.Instance.CurrentLanguage)
                {
                    currentIndex = i;
                }
            }

            languageDropdown.AddOptions(options);
            languageDropdown.value = currentIndex;
            languageDropdown.onValueChanged.RemoveAllListeners();
            languageDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        private void PopulateButtons()
        {
            foreach (SystemLanguage lang in LocalizationManager.SupportedLanguages)
            {
                GameObject buttonObj = Instantiate(languageButtonPrefab, languageButtonContainer);
                languageButtons.Add(buttonObj);

                // Setup button
                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    SystemLanguage capturedLang = lang;
                    button.onClick.AddListener(() => OnLanguageButtonClicked(capturedLang));
                }

                // Setup text
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = showNativeNames 
                        ? LocalizationManager.GetLanguageNativeName(lang)
                        : lang.ToString();
                }

                // Setup flag
                if (showLanguageFlags)
                {
                    Image flagImage = buttonObj.transform.Find("Flag")?.GetComponent<Image>();
                    if (flagImage != null)
                    {
                        flagImage.sprite = GetFlagForLanguage(lang);
                    }
                }

                // Highlight current language
                if (LocalizationManager.Instance != null && lang == LocalizationManager.Instance.CurrentLanguage)
                {
                    HighlightButton(buttonObj, true);
                }
            }
        }

        private void ClearLanguageButtons()
        {
            foreach (GameObject button in languageButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }
            languageButtons.Clear();
        }

        private Sprite GetFlagForLanguage(SystemLanguage language)
        {
            foreach (var flag in languageFlags)
            {
                if (flag.language == language)
                {
                    return flag.flag;
                }
            }
            return null;
        }

        #endregion

        #region UI Updates

        private void UpdateUI()
        {
            if (LocalizationManager.Instance == null) return;

            SystemLanguage currentLang = LocalizationManager.Instance.CurrentLanguage;

            // Update current language label
            if (currentLanguageLabel != null)
            {
                currentLanguageLabel.text = showNativeNames
                    ? LocalizationManager.GetLanguageNativeName(currentLang)
                    : currentLang.ToString();
            }

            // Update language code
            if (languageCodeLabel != null)
            {
                languageCodeLabel.text = LocalizationManager.GetLanguageCode(currentLang).ToUpper();
            }

            // Update RTL indicator
            if (rtlIndicator != null)
            {
                rtlIndicator.gameObject.SetActive(LocalizationManager.IsRightToLeft(currentLang));
            }

            // Update preview text
            UpdatePreviewText();

            // Update button highlights
            UpdateButtonHighlights();

            // Update apply button state
            if (applyButton != null)
            {
                applyButton.interactable = pendingLanguage != LocalizationManager.Instance.CurrentLanguage;
            }
        }

        private void UpdatePreviewText()
        {
            if (previewText == null) return;
            if (LocalizationManager.Instance == null) return;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            foreach (string key in previewKeys)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(LocalizationManager.Instance.Get(key));
            }

            previewText.text = sb.ToString();
        }

        private void UpdateButtonHighlights()
        {
            for (int i = 0; i < languageButtons.Count && i < LocalizationManager.SupportedLanguages.Length; i++)
            {
                bool isSelected = LocalizationManager.SupportedLanguages[i] == pendingLanguage;
                HighlightButton(languageButtons[i], isSelected);
            }
        }

        private void HighlightButton(GameObject buttonObj, bool highlight)
        {
            if (buttonObj == null) return;

            // Try to find and update selection indicator
            Transform indicator = buttonObj.transform.Find("SelectionIndicator");
            if (indicator != null)
            {
                indicator.gameObject.SetActive(highlight);
            }

            // Update button colors
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = highlight ? new Color(0.8f, 0.9f, 1f) : Color.white;
                button.colors = colors;
            }
        }

        #endregion

        #region Event Handlers

        private void OnDropdownValueChanged(int index)
        {
            if (index >= 0 && index < LocalizationManager.SupportedLanguages.Length)
            {
                pendingLanguage = LocalizationManager.SupportedLanguages[index];
                
                if (!confirmBeforeChange)
                {
                    ApplyLanguageChange();
                }
                else
                {
                    UpdateUI();
                }
            }
        }

        private void OnLanguageButtonClicked(SystemLanguage language)
        {
            pendingLanguage = language;
            
            if (!confirmBeforeChange)
            {
                ApplyLanguageChange();
            }
            else
            {
                UpdateUI();
            }
        }

        private void OnLanguageChanged(SystemLanguage newLanguage)
        {
            pendingLanguage = newLanguage;
            UpdateUI();
        }

        private void OnApplyClicked()
        {
            ApplyLanguageChange();
        }

        private void OnCancelClicked()
        {
            // Revert to original language
            pendingLanguage = originalLanguage;
            
            if (LocalizationManager.Instance != null && 
                LocalizationManager.Instance.CurrentLanguage != originalLanguage)
            {
                LocalizationManager.Instance.SetLanguage(originalLanguage);
            }

            UpdateUI();
            
            // Close panel
            gameObject.SetActive(false);
        }

        private void OnResetClicked()
        {
            // Reset to system language or default
            if (LocalizationManager.Instance != null)
            {
                SystemLanguage systemLang = Application.systemLanguage;
                
                if (LocalizationManager.Instance.IsLanguageSupported(systemLang))
                {
                    pendingLanguage = systemLang;
                }
                else
                {
                    pendingLanguage = SystemLanguage.English;
                }

                if (!confirmBeforeChange)
                {
                    ApplyLanguageChange();
                }
                else
                {
                    UpdateUI();
                }
            }
        }

        #endregion

        #region Language Application

        private void ApplyLanguageChange()
        {
            if (LocalizationManager.Instance == null) return;
            if (pendingLanguage == LocalizationManager.Instance.CurrentLanguage) return;

            LocalizationManager.Instance.SetLanguage(pendingLanguage);
            originalLanguage = pendingLanguage;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set language directly from external code
        /// </summary>
        public void SetLanguage(SystemLanguage language)
        {
            if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsLanguageSupported(language))
            {
                pendingLanguage = language;
                ApplyLanguageChange();
            }
        }

        /// <summary>
        /// Set language by index (for UI buttons)
        /// </summary>
        public void SetLanguageByIndex(int index)
        {
            if (index >= 0 && index < LocalizationManager.SupportedLanguages.Length)
            {
                SetLanguage(LocalizationManager.SupportedLanguages[index]);
            }
        }

        /// <summary>
        /// Cycle to next language
        /// </summary>
        public void NextLanguage()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.CycleLanguage();
            }
        }

        /// <summary>
        /// Get current language display name
        /// </summary>
        public string GetCurrentLanguageDisplayName()
        {
            if (LocalizationManager.Instance == null) return "Unknown";

            return showNativeNames
                ? LocalizationManager.GetLanguageNativeName(LocalizationManager.Instance.CurrentLanguage)
                : LocalizationManager.Instance.CurrentLanguage.ToString();
        }

        #endregion
    }
}
