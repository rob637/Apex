using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;
using ApexCitadels.Player;

namespace ApexCitadels.UI
{
    /// <summary>
    /// Controls the main menu UI and navigation
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button quitButton;

        [Header("Login Panel")]
        [SerializeField] private TMP_InputField loginEmailInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button loginGuestButton;
        [SerializeField] private Button goToRegisterButton;
        [SerializeField] private TextMeshProUGUI loginErrorText;

        [Header("Register Panel")]
        [SerializeField] private TMP_InputField registerEmailInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private TMP_InputField registerConfirmPasswordInput;
        [SerializeField] private TMP_InputField registerDisplayNameInput;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button goToLoginButton;
        [SerializeField] private TextMeshProUGUI registerErrorText;

        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private Image playerAvatarImage;

        [Header("Version Info")]
        [SerializeField] private TextMeshProUGUI versionText;

        private void Start()
        {
            SetupButtonListeners();
            UpdateVersionText();
            CheckLoginState();
        }

        private void SetupButtonListeners()
        {
            // Main menu buttons
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            
            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            // Login panel buttons
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);
            
            if (loginGuestButton != null)
                loginGuestButton.onClick.AddListener(OnGuestLoginClicked);
            
            if (goToRegisterButton != null)
                goToRegisterButton.onClick.AddListener(ShowRegisterPanel);

            // Register panel buttons
            if (registerButton != null)
                registerButton.onClick.AddListener(OnRegisterClicked);
            
            if (goToLoginButton != null)
                goToLoginButton.onClick.AddListener(ShowLoginPanel);
        }

        private void CheckLoginState()
        {
            if (PlayerManager.Instance != null && PlayerManager.Instance.IsLoggedIn)
            {
                ShowMainPanel();
                UpdatePlayerInfo();
            }
            else
            {
                ShowLoginPanel();
            }
        }

        private void UpdatePlayerInfo()
        {
            if (PlayerManager.Instance?.CurrentPlayer == null) return;

            var player = PlayerManager.Instance.CurrentPlayer;

            if (playerNameText != null)
                playerNameText.text = player.DisplayName;

            if (playerLevelText != null)
                playerLevelText.text = $"Level {player.Level}";
        }

        private void UpdateVersionText()
        {
            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
            }
        }

        #region Panel Navigation

        private void ShowPanel(GameObject panel)
        {
            // Hide all panels
            if (mainPanel != null) mainPanel.SetActive(false);
            if (loginPanel != null) loginPanel.SetActive(false);
            if (registerPanel != null) registerPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);

            // Show requested panel
            if (panel != null) panel.SetActive(true);
        }

        public void ShowMainPanel() => ShowPanel(mainPanel);
        public void ShowLoginPanel() => ShowPanel(loginPanel);
        public void ShowRegisterPanel() => ShowPanel(registerPanel);
        public void ShowSettingsPanel() => ShowPanel(settingsPanel);
        public void ShowLoadingPanel() => ShowPanel(loadingPanel);

        #endregion

        #region Button Handlers

        private void OnPlayClicked()
        {
            Debug.Log("[MainMenu] Play clicked");
            
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadARGameplay();
            }
            else
            {
                // Fallback direct load
                UnityEngine.SceneManagement.SceneManager.LoadScene("ARGameplay");
            }
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenu] Settings clicked");
            ShowSettingsPanel();
        }

        private void OnLogoutClicked()
        {
            Debug.Log("[MainMenu] Logout clicked");
            
            if (PlayerManager.Instance != null)
            {
                // PlayerManager.Instance.Logout();
            }
            
            ShowLoginPanel();
        }

        private void OnQuitClicked()
        {
            Debug.Log("[MainMenu] Quit clicked");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private async void OnLoginClicked()
        {
            if (loginEmailInput == null || loginPasswordInput == null) return;

            string email = loginEmailInput.text;
            string password = loginPasswordInput.text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowLoginError("Please enter email and password");
                return;
            }

            ShowLoadingPanel();
            ClearLoginError();

            if (PlayerManager.Instance != null)
            {
                var result = await PlayerManager.Instance.Login(email, password);
                
                if (result.Success)
                {
                    ShowMainPanel();
                    UpdatePlayerInfo();
                }
                else
                {
                    ShowLoginPanel();
                    ShowLoginError(result.Message);
                }
            }
            else
            {
                // Mock success for testing
                ShowMainPanel();
            }
        }

        private async void OnGuestLoginClicked()
        {
            ShowLoadingPanel();

            if (PlayerManager.Instance != null)
            {
                var result = await PlayerManager.Instance.LoginAsGuest();
                
                if (result.Success)
                {
                    ShowMainPanel();
                    UpdatePlayerInfo();
                }
                else
                {
                    ShowLoginPanel();
                    ShowLoginError(result.Message);
                }
            }
            else
            {
                // Mock success for testing
                ShowMainPanel();
            }
        }

        private async void OnRegisterClicked()
        {
            if (registerEmailInput == null || registerPasswordInput == null || 
                registerConfirmPasswordInput == null || registerDisplayNameInput == null) return;

            string email = registerEmailInput.text;
            string password = registerPasswordInput.text;
            string confirmPassword = registerConfirmPasswordInput.text;
            string displayName = registerDisplayNameInput.text;

            // Validate inputs
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowRegisterError("Please fill in all fields");
                return;
            }

            if (password != confirmPassword)
            {
                ShowRegisterError("Passwords do not match");
                return;
            }

            if (password.Length < 6)
            {
                ShowRegisterError("Password must be at least 6 characters");
                return;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = email.Split('@')[0];
            }

            ShowLoadingPanel();
            ClearRegisterError();

            if (PlayerManager.Instance != null)
            {
                var result = await PlayerManager.Instance.Register(email, password, displayName);
                
                if (result.Success)
                {
                    ShowMainPanel();
                    UpdatePlayerInfo();
                }
                else
                {
                    ShowRegisterPanel();
                    ShowRegisterError(result.Message);
                }
            }
            else
            {
                ShowMainPanel();
            }
        }

        #endregion

        #region Error Handling

        private void ShowLoginError(string message)
        {
            if (loginErrorText != null)
            {
                loginErrorText.text = message;
                loginErrorText.gameObject.SetActive(true);
            }
        }

        private void ClearLoginError()
        {
            if (loginErrorText != null)
            {
                loginErrorText.text = "";
                loginErrorText.gameObject.SetActive(false);
            }
        }

        private void ShowRegisterError(string message)
        {
            if (registerErrorText != null)
            {
                registerErrorText.text = message;
                registerErrorText.gameObject.SetActive(true);
            }
        }

        private void ClearRegisterError()
        {
            if (registerErrorText != null)
            {
                registerErrorText.text = "";
                registerErrorText.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
