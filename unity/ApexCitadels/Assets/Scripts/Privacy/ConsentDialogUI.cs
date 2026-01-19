using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;

namespace ApexCitadels.Privacy
{
    /// <summary>
    /// Consent dialog UI for GDPR compliance
    /// Shows on first launch and when consent version updates
    /// </summary>
    public class ConsentDialogUI : MonoBehaviour
    {
        [Header("UI References - Main Dialog")]
        [SerializeField] private GameObject consentPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI introText;
        [SerializeField] private ScrollRect scrollRect;
        
        [Header("Toggle References")]
        [SerializeField] private Toggle analyticsToggle;
        [SerializeField] private Toggle marketingToggle;
        [SerializeField] private Toggle personalizationToggle;
        [SerializeField] private Toggle thirdPartyToggle;
        
        [Header("Toggle Labels")]
        [SerializeField] private TextMeshProUGUI analyticsLabel;
        [SerializeField] private TextMeshProUGUI marketingLabel;
        [SerializeField] private TextMeshProUGUI personalizationLabel;
        [SerializeField] private TextMeshProUGUI thirdPartyLabel;
        
        [Header("Toggle Descriptions")]
        [SerializeField] private TextMeshProUGUI analyticsDescription;
        [SerializeField] private TextMeshProUGUI marketingDescription;
        [SerializeField] private TextMeshProUGUI personalizationDescription;
        [SerializeField] private TextMeshProUGUI thirdPartyDescription;

        [Header("Buttons")]
        [SerializeField] private Button acceptAllButton;
        [SerializeField] private Button acceptSelectedButton;
        [SerializeField] private Button rejectAllButton;
        [SerializeField] private Button privacyPolicyButton;
        [SerializeField] private Button manageSettingsButton;

        [Header("Settings")]
        [SerializeField] private string privacyPolicyUrl = "https://apexcitadels.com/privacy";
        [SerializeField] private bool showRejectAllButton = true;

        [Header("Localized Strings")]
        [SerializeField] private string dialogTitle = "Privacy Settings";
        [SerializeField] private string introMessage = "We value your privacy. Please review and manage your data preferences below.";
        [SerializeField] private string analyticsTitle = "Analytics";
        [SerializeField] private string analyticsDesc = "Help us improve the game by allowing anonymous usage data collection.";
        [SerializeField] private string marketingTitle = "Marketing Communications";
        [SerializeField] private string marketingDesc = "Receive news, updates, and special offers via email and push notifications.";
        [SerializeField] private string personalizationTitle = "Personalization";
        [SerializeField] private string personalizationDesc = "Allow us to personalize your experience based on your gameplay patterns.";
        [SerializeField] private string thirdPartyTitle = "Third-Party Sharing";
        [SerializeField] private string thirdPartyDesc = "Allow sharing anonymous data with trusted partners to improve our services.";

        // Events
        public event Action<ConsentPreferences> OnConsentSubmitted;
        public event Action OnConsentCancelled;

        private bool _isSubmitting;

        private void Start()
        {
            SetupUI();
            SetupButtons();
        }

        private void SetupUI()
        {
            // Set titles and descriptions
            if (titleText != null) titleText.text = dialogTitle;
            if (introText != null) introText.text = introMessage;
            
            if (analyticsLabel != null) analyticsLabel.text = analyticsTitle;
            if (analyticsDescription != null) analyticsDescription.text = analyticsDesc;
            
            if (marketingLabel != null) marketingLabel.text = marketingTitle;
            if (marketingDescription != null) marketingDescription.text = marketingDesc;
            
            if (personalizationLabel != null) personalizationLabel.text = personalizationTitle;
            if (personalizationDescription != null) personalizationDescription.text = personalizationDesc;
            
            if (thirdPartyLabel != null) thirdPartyLabel.text = thirdPartyTitle;
            if (thirdPartyDescription != null) thirdPartyDescription.text = thirdPartyDesc;

            // Configure reject button visibility
            if (rejectAllButton != null)
            {
                rejectAllButton.gameObject.SetActive(showRejectAllButton);
            }

            // Set default toggle states
            SetDefaultToggleStates();
        }

        private void SetDefaultToggleStates()
        {
            // Default: analytics and personalization on, marketing and third-party off
            if (analyticsToggle != null) analyticsToggle.isOn = true;
            if (marketingToggle != null) marketingToggle.isOn = false;
            if (personalizationToggle != null) personalizationToggle.isOn = true;
            if (thirdPartyToggle != null) thirdPartyToggle.isOn = false;
        }

        private void SetupButtons()
        {
            if (acceptAllButton != null)
            {
                acceptAllButton.onClick.AddListener(OnAcceptAllClicked);
            }

            if (acceptSelectedButton != null)
            {
                acceptSelectedButton.onClick.AddListener(OnAcceptSelectedClicked);
            }

            if (rejectAllButton != null)
            {
                rejectAllButton.onClick.AddListener(OnRejectAllClicked);
            }

            if (privacyPolicyButton != null)
            {
                privacyPolicyButton.onClick.AddListener(OnPrivacyPolicyClicked);
            }

            if (manageSettingsButton != null)
            {
                manageSettingsButton.onClick.AddListener(OnManageSettingsClicked);
            }
        }

        /// <summary>
        /// Show the consent dialog
        /// </summary>
        public void Show()
        {
            if (consentPanel != null)
            {
                consentPanel.SetActive(true);
            }

            // Reset scroll position
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }

            SetDefaultToggleStates();
            _isSubmitting = false;
        }

        /// <summary>
        /// Show with pre-filled values (for updating consent)
        /// </summary>
        public void Show(ConsentPreferences currentConsent)
        {
            Show();

            if (currentConsent != null)
            {
                if (analyticsToggle != null) analyticsToggle.isOn = currentConsent.Analytics;
                if (marketingToggle != null) marketingToggle.isOn = currentConsent.Marketing;
                if (personalizationToggle != null) personalizationToggle.isOn = currentConsent.Personalization;
                if (thirdPartyToggle != null) thirdPartyToggle.isOn = currentConsent.ThirdPartySharing;
            }
        }

        /// <summary>
        /// Hide the consent dialog
        /// </summary>
        public void Hide()
        {
            if (consentPanel != null)
            {
                consentPanel.SetActive(false);
            }
        }

        private async void OnAcceptAllClicked()
        {
            if (_isSubmitting) return;
            _isSubmitting = true;

            var consent = new ConsentPreferences
            {
                Essential = true,
                Analytics = true,
                Marketing = true,
                Personalization = true,
                ThirdPartySharing = true
            };

            await SubmitConsent(consent);
        }

        private async void OnAcceptSelectedClicked()
        {
            if (_isSubmitting) return;
            _isSubmitting = true;

            var consent = new ConsentPreferences
            {
                Essential = true,
                Analytics = analyticsToggle?.isOn ?? false,
                Marketing = marketingToggle?.isOn ?? false,
                Personalization = personalizationToggle?.isOn ?? false,
                ThirdPartySharing = thirdPartyToggle?.isOn ?? false
            };

            await SubmitConsent(consent);
        }

        private async void OnRejectAllClicked()
        {
            if (_isSubmitting) return;
            _isSubmitting = true;

            var consent = new ConsentPreferences
            {
                Essential = true, // Always required
                Analytics = false,
                Marketing = false,
                Personalization = false,
                ThirdPartySharing = false
            };

            await SubmitConsent(consent);
        }

        private async Task SubmitConsent(ConsentPreferences consent)
        {
            try
            {
                if (GDPRManager.Instance != null)
                {
                    bool success = await GDPRManager.Instance.UpdateConsent(consent);
                    
                    if (success)
                    {
                        ApexLogger.Log(ApexLogger.LogCategory.General, "Consent submitted successfully");
                        Hide();
                        OnConsentSubmitted?.Invoke(consent);
                    }
                    else
                    {
                        ApexLogger.LogError(ApexLogger.LogCategory.General, "Failed to submit consent");
                        _isSubmitting = false;
                    }
                }
                else
                {
                    // Fallback if manager not available
                    ApexLogger.LogWarning(ApexLogger.LogCategory.General, "GDPRManager not found, using local storage");
                    Hide();
                    OnConsentSubmitted?.Invoke(consent);
                }
            }
            catch (Exception ex)
            {
                ApexLogger.LogError(ApexLogger.LogCategory.General, $"Error submitting consent: {ex.Message}");
                _isSubmitting = false;
            }
        }

        private void OnPrivacyPolicyClicked()
        {
            Application.OpenURL(privacyPolicyUrl);
        }

        private void OnManageSettingsClicked()
        {
            // Expand detailed settings if collapsed
            // Or navigate to privacy settings page
            ApexLogger.Log(ApexLogger.LogCategory.General, "Manage settings clicked");
        }
    }
}
