using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;

namespace ApexCitadels.Notifications
{
    /// <summary>
    /// Notification Settings UI
    /// Allows users to manage their push notification preferences
    /// </summary>
    public class NotificationSettingsUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeButton;

        [Header("Master Toggle")]
        [SerializeField] private Toggle masterToggle;
        [SerializeField] private TextMeshProUGUI masterStatusText;

        [Header("Category Toggles")]
        [SerializeField] private Toggle combatToggle;
        [SerializeField] private Toggle allianceToggle;
        [SerializeField] private Toggle socialToggle;
        [SerializeField] private Toggle rewardsToggle;
        [SerializeField] private Toggle eventsToggle;
        [SerializeField] private Toggle marketingToggle;

        [Header("Category Labels")]
        [SerializeField] private TextMeshProUGUI combatLabel;
        [SerializeField] private TextMeshProUGUI allianceLabel;
        [SerializeField] private TextMeshProUGUI socialLabel;
        [SerializeField] private TextMeshProUGUI rewardsLabel;
        [SerializeField] private TextMeshProUGUI eventsLabel;
        [SerializeField] private TextMeshProUGUI marketingLabel;

        [Header("Quiet Hours")]
        [SerializeField] private Toggle quietHoursToggle;
        [SerializeField] private TMP_Dropdown startHourDropdown;
        [SerializeField] private TMP_Dropdown endHourDropdown;
        [SerializeField] private GameObject quietHoursContainer;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI tokenStatusText;
        [SerializeField] private Button testNotificationButton;

        private bool _isUpdating;

        private void Start()
        {
            SetupEventListeners();
            PopulateHourDropdowns();

            if (PushNotificationManager.Instance != null)
            {
                PushNotificationManager.Instance.OnSettingsUpdated += OnSettingsUpdated;
                UpdateUI(PushNotificationManager.Instance.Settings);
            }

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (testNotificationButton != null)
                testNotificationButton.onClick.AddListener(SendTestNotification);
        }

        private void OnDestroy()
        {
            if (PushNotificationManager.Instance != null)
            {
                PushNotificationManager.Instance.OnSettingsUpdated -= OnSettingsUpdated;
            }
        }

        private void SetupEventListeners()
        {
            // Master toggle
            if (masterToggle != null)
                masterToggle.onValueChanged.AddListener(OnMasterToggleChanged);

            // Category toggles
            if (combatToggle != null)
                combatToggle.onValueChanged.AddListener(_ => OnPreferencesChanged());

            if (allianceToggle != null)
                allianceToggle.onValueChanged.AddListener(_ => OnPreferencesChanged());

            if (socialToggle != null)
                socialToggle.onValueChanged.AddListener(_ => OnPreferencesChanged());

            if (rewardsToggle != null)
                rewardsToggle.onValueChanged.AddListener(_ => OnPreferencesChanged());

            if (eventsToggle != null)
                eventsToggle.onValueChanged.AddListener(_ => OnPreferencesChanged());

            if (marketingToggle != null)
                marketingToggle.onValueChanged.AddListener(_ => OnPreferencesChanged());

            // Quiet hours
            if (quietHoursToggle != null)
                quietHoursToggle.onValueChanged.AddListener(OnQuietHoursToggleChanged);

            if (startHourDropdown != null)
                startHourDropdown.onValueChanged.AddListener(_ => OnQuietHoursChanged());

            if (endHourDropdown != null)
                endHourDropdown.onValueChanged.AddListener(_ => OnQuietHoursChanged());
        }

        private void PopulateHourDropdowns()
        {
            var hours = new List<string>();
            for (int i = 0; i < 24; i++)
            {
                string ampm = i < 12 ? "AM" : "PM";
                int displayHour = i % 12;
                if (displayHour == 0) displayHour = 12;
                hours.Add($"{displayHour}:00 {ampm}");
            }

            if (startHourDropdown != null)
            {
                startHourDropdown.ClearOptions();
                startHourDropdown.AddOptions(hours);
            }

            if (endHourDropdown != null)
            {
                endHourDropdown.ClearOptions();
                endHourDropdown.AddOptions(hours);
            }
        }

        public void Open()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(true);

            // Refresh settings from server
            PushNotificationManager.Instance?.LoadSettings();
        }

        public void Close()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void OnSettingsUpdated(NotificationSettings settings)
        {
            UpdateUI(settings);
        }

        private void UpdateUI(NotificationSettings settings)
        {
            if (settings == null) return;

            _isUpdating = true;

            // Master toggle
            if (masterToggle != null)
                masterToggle.isOn = settings.Enabled;

            if (masterStatusText != null)
                masterStatusText.text = settings.Enabled ? "ON" : "OFF";

            // Update category toggles
            bool categoriesEnabled = settings.Enabled;

            if (combatToggle != null)
            {
                combatToggle.isOn = settings.Preferences?.Combat ?? true;
                combatToggle.interactable = categoriesEnabled;
            }

            if (allianceToggle != null)
            {
                allianceToggle.isOn = settings.Preferences?.Alliance ?? true;
                allianceToggle.interactable = categoriesEnabled;
            }

            if (socialToggle != null)
            {
                socialToggle.isOn = settings.Preferences?.Social ?? true;
                socialToggle.interactable = categoriesEnabled;
            }

            if (rewardsToggle != null)
            {
                rewardsToggle.isOn = settings.Preferences?.Rewards ?? true;
                rewardsToggle.interactable = categoriesEnabled;
            }

            if (eventsToggle != null)
            {
                eventsToggle.isOn = settings.Preferences?.Events ?? true;
                eventsToggle.interactable = categoriesEnabled;
            }

            if (marketingToggle != null)
            {
                marketingToggle.isOn = settings.Preferences?.Marketing ?? false;
                marketingToggle.interactable = categoriesEnabled;
            }

            // Quiet hours
            if (quietHoursToggle != null)
            {
                quietHoursToggle.isOn = settings.QuietHours?.Enabled ?? false;
                quietHoursToggle.interactable = categoriesEnabled;
            }

            if (quietHoursContainer != null)
                quietHoursContainer.SetActive(settings.QuietHours?.Enabled ?? false);

            if (startHourDropdown != null)
                startHourDropdown.value = settings.QuietHours?.StartHour ?? 22;

            if (endHourDropdown != null)
                endHourDropdown.value = settings.QuietHours?.EndHour ?? 8;

            // Token status
            if (tokenStatusText != null)
            {
                tokenStatusText.text = settings.HasToken 
                    ? "[OK] Device registered for notifications" 
                    : "[!] Device not registered";
                tokenStatusText.color = settings.HasToken ? Color.green : Color.yellow;
            }

            _isUpdating = false;
        }

        private async void OnMasterToggleChanged(bool isOn)
        {
            if (_isUpdating) return;

            if (masterStatusText != null)
                masterStatusText.text = isOn ? "ON" : "OFF";

            // Update category toggle interactivity
            SetCategoryTogglesInteractable(isOn);

            await PushNotificationManager.Instance?.SetNotificationsEnabled(isOn);
        }

        private void SetCategoryTogglesInteractable(bool interactable)
        {
            if (combatToggle != null) combatToggle.interactable = interactable;
            if (allianceToggle != null) allianceToggle.interactable = interactable;
            if (socialToggle != null) socialToggle.interactable = interactable;
            if (rewardsToggle != null) rewardsToggle.interactable = interactable;
            if (eventsToggle != null) eventsToggle.interactable = interactable;
            if (marketingToggle != null) marketingToggle.interactable = interactable;
            if (quietHoursToggle != null) quietHoursToggle.interactable = interactable;
        }

        private async void OnPreferencesChanged()
        {
            if (_isUpdating) return;

            var preferences = new NotificationPreferences
            {
                Combat = combatToggle?.isOn ?? true,
                Alliance = allianceToggle?.isOn ?? true,
                Social = socialToggle?.isOn ?? true,
                Rewards = rewardsToggle?.isOn ?? true,
                Events = eventsToggle?.isOn ?? true,
                Marketing = marketingToggle?.isOn ?? false
            };

            await PushNotificationManager.Instance?.UpdatePreferences(preferences);
        }

        private void OnQuietHoursToggleChanged(bool isOn)
        {
            if (_isUpdating) return;

            if (quietHoursContainer != null)
                quietHoursContainer.SetActive(isOn);

            OnQuietHoursChanged();
        }

        private async void OnQuietHoursChanged()
        {
            if (_isUpdating) return;

            var quietHours = new QuietHoursSettings
            {
                Enabled = quietHoursToggle?.isOn ?? false,
                StartHour = startHourDropdown?.value ?? 22,
                EndHour = endHourDropdown?.value ?? 8,
                Timezone = TimeZoneInfo.Local.Id
            };

            await PushNotificationManager.Instance?.UpdateQuietHours(quietHours);
        }

        private void SendTestNotification()
        {
            // This would typically be an admin function or debug feature
            ApexLogger.Log("[NotificationSettingsUI] Test notification requested", ApexLogger.LogCategory.Events);
            
            // For testing, we can simulate a local notification
            var testNotification = new ReceivedNotification
            {
                Type = PushNotificationType.DailyRewardAvailable,
                Title = "[?] Test Notification",
                Body = "This is a test notification from Apex Citadels!",
                Data = new Dictionary<string, string>(),
                ReceivedAt = DateTime.Now,
                WasTapped = false
            };

            // Show toast or banner
            // ToastManager.Instance?.ShowToast(testNotification.Title, testNotification.Body);
        }
    }
}
