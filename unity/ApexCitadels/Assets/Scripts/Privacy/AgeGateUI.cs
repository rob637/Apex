using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;

namespace ApexCitadels.Privacy
{
    /// <summary>
    /// Age Gate UI for COPPA compliance
    /// Must be shown before accessing game features
    /// </summary>
    public class AgeGateUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject ageGatePanel;
        [SerializeField] private TMP_Dropdown dayDropdown;
        [SerializeField] private TMP_Dropdown monthDropdown;
        [SerializeField] private TMP_Dropdown yearDropdown;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Settings")]
        [SerializeField] private int minimumAge = 13;
        [SerializeField] private int maxYearRange = 100;
        [SerializeField] private string underageMessage = "You must be at least {0} years old to play this game.";
        [SerializeField] private string invalidDateMessage = "Please enter a valid date of birth.";

        // Events
        public event Action OnAgeVerified;
        public event Action OnAgeRejected;
        public event Action OnCancelled;

        private const string AGE_VERIFIED_KEY = "age_verified";
        private const string DOB_HASH_KEY = "dob_hash";

        private void Start()
        {
            SetupDropdowns();
            SetupButtons();
            
            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Check if age verification is needed
        /// </summary>
        public bool NeedsAgeVerification()
        {
            return PlayerPrefs.GetInt(AGE_VERIFIED_KEY, 0) != 1;
        }

        /// <summary>
        /// Show the age gate UI
        /// </summary>
        public void Show()
        {
            if (ageGatePanel != null)
            {
                ageGatePanel.SetActive(true);
            }
            
            ResetForm();
        }

        /// <summary>
        /// Hide the age gate UI
        /// </summary>
        public void Hide()
        {
            if (ageGatePanel != null)
            {
                ageGatePanel.SetActive(false);
            }
        }

        private void SetupDropdowns()
        {
            // Setup days (1-31)
            if (dayDropdown != null)
            {
                dayDropdown.ClearOptions();
                dayDropdown.options.Add(new TMP_Dropdown.OptionData("Day"));
                for (int i = 1; i <= 31; i++)
                {
                    dayDropdown.options.Add(new TMP_Dropdown.OptionData(i.ToString("00")));
                }
                dayDropdown.RefreshShownValue();
            }

            // Setup months
            if (monthDropdown != null)
            {
                monthDropdown.ClearOptions();
                monthDropdown.options.Add(new TMP_Dropdown.OptionData("Month"));
                string[] months = { "January", "February", "March", "April", "May", "June",
                                   "July", "August", "September", "October", "November", "December" };
                foreach (string month in months)
                {
                    monthDropdown.options.Add(new TMP_Dropdown.OptionData(month));
                }
                monthDropdown.RefreshShownValue();
            }

            // Setup years (current year down to maxYearRange years ago)
            if (yearDropdown != null)
            {
                yearDropdown.ClearOptions();
                yearDropdown.options.Add(new TMP_Dropdown.OptionData("Year"));
                int currentYear = DateTime.Now.Year;
                for (int year = currentYear; year >= currentYear - maxYearRange; year--)
                {
                    yearDropdown.options.Add(new TMP_Dropdown.OptionData(year.ToString()));
                }
                yearDropdown.RefreshShownValue();
            }
        }

        private void SetupButtons()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        private void ResetForm()
        {
            if (dayDropdown != null) dayDropdown.value = 0;
            if (monthDropdown != null) monthDropdown.value = 0;
            if (yearDropdown != null) yearDropdown.value = 0;
            
            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }
        }

        private void OnConfirmClicked()
        {
            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }

            // Validate selection
            if (dayDropdown == null || monthDropdown == null || yearDropdown == null)
            {
                ShowError(invalidDateMessage);
                return;
            }

            if (dayDropdown.value == 0 || monthDropdown.value == 0 || yearDropdown.value == 0)
            {
                ShowError(invalidDateMessage);
                return;
            }

            // Parse date
            int day = dayDropdown.value;
            int month = monthDropdown.value;
            int year = DateTime.Now.Year - (yearDropdown.value - 1);

            // Validate date
            if (!IsValidDate(day, month, year))
            {
                ShowError(invalidDateMessage);
                return;
            }

            DateTime birthDate = new DateTime(year, month, day);
            int age = CalculateAge(birthDate);

            if (age < minimumAge)
            {
                // Under age - reject access
                ShowError(string.Format(underageMessage, minimumAge));
                
                // Store rejection (prevent retry abuse)
                PlayerPrefs.SetInt(AGE_VERIFIED_KEY, -1); // -1 = rejected
                PlayerPrefs.SetString(DOB_HASH_KEY, HashDate(birthDate));
                PlayerPrefs.Save();
                
                OnAgeRejected?.Invoke();
                return;
            }

            // Age verified successfully
            PlayerPrefs.SetInt(AGE_VERIFIED_KEY, 1);
            PlayerPrefs.SetString(DOB_HASH_KEY, HashDate(birthDate));
            PlayerPrefs.Save();

            ApexLogger.Log(LogCategory.General, $"Age verified: {age} years old");
            
            Hide();
            OnAgeVerified?.Invoke();
        }

        private void OnCancelClicked()
        {
            Hide();
            OnCancelled?.Invoke();
        }

        private bool IsValidDate(int day, int month, int year)
        {
            try
            {
                DateTime date = new DateTime(year, month, day);
                return date <= DateTime.Now;
            }
            catch
            {
                return false;
            }
        }

        private int CalculateAge(DateTime birthDate)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - birthDate.Year;
            
            // Adjust if birthday hasn't occurred this year
            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }
            
            return age;
        }

        private string HashDate(DateTime date)
        {
            // Simple hash for verification (not secure, just anti-tamper)
            string dateStr = date.ToString("yyyyMMdd");
            int hash = dateStr.GetHashCode();
            return hash.ToString("X8");
        }

        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
            }
            else
            {
                ApexLogger.LogWarning(LogCategory.General, message);
            }
        }

        /// <summary>
        /// Check if user was previously rejected for being underage
        /// </summary>
        public bool WasRejected()
        {
            return PlayerPrefs.GetInt(AGE_VERIFIED_KEY, 0) == -1;
        }

        /// <summary>
        /// Reset age verification (for testing)
        /// </summary>
        public void ResetVerification()
        {
            PlayerPrefs.DeleteKey(AGE_VERIFIED_KEY);
            PlayerPrefs.DeleteKey(DOB_HASH_KEY);
            PlayerPrefs.Save();
        }
    }
}
