using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
#if FIREBASE_ENABLED
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
#endif

namespace ApexCitadels.Core
{
    /// <summary>
    /// Main game initialization and lifecycle manager
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Initialization Settings")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string gameplayScene = "Gameplay";
        [SerializeField] private float splashMinDuration = 2f;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugMode = false;
        [SerializeField] private bool skipAuthentication = false;

        // Events
        public event Action OnInitializationStarted;
        public event Action<float> OnInitializationProgress;
        public event Action OnInitializationComplete;
        public event Action<string> OnInitializationFailed;
        public event Action OnUserAuthenticated;
        public event Action OnUserSignedOut;

        // State
#if FIREBASE_ENABLED
        private FirebaseApp _firebaseApp;
        private FirebaseAuth _auth;
        private FirebaseUser _currentUser;
#else
        private bool _firebaseAppStub;
        private string _currentUserIdStub;
#endif
        private bool _isInitialized;
        private float _initStartTime;

        public bool IsInitialized => _isInitialized;
#if FIREBASE_ENABLED
        public bool IsAuthenticated => _currentUser != null;
        public string UserId => _currentUser?.UserId;
        public string UserDisplayName => _currentUser?.DisplayName;
        public string UserEmail => _currentUser?.Email;
#else
        public bool IsAuthenticated => !string.IsNullOrEmpty(_currentUserIdStub);
        public string UserId => _currentUserIdStub;
        public string UserDisplayName => "StubUser";
        public string UserEmail => "stub@example.com";
#endif
        public bool IsDebugMode => enableDebugMode;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (autoInitialize)
            {
                StartCoroutine(InitializeGame());
            }
        }

#if FIREBASE_ENABLED
        /// <summary>
        /// Initialize all game systems
        /// </summary>
        public IEnumerator InitializeGame()
        {
            _initStartTime = Time.time;
            OnInitializationStarted?.Invoke();

            Debug.Log("[GameManager] Starting initialization...");

            // Step 1: Initialize Firebase (20%)
            OnInitializationProgress?.Invoke(0.05f);
            yield return StartCoroutine(InitializeFirebase());

            if (_firebaseApp == null)
            {
                OnInitializationFailed?.Invoke("Failed to initialize Firebase");
                yield break;
            }
            OnInitializationProgress?.Invoke(0.20f);

            // Step 2: Check Authentication (40%)
            yield return StartCoroutine(CheckAuthentication());
            OnInitializationProgress?.Invoke(0.40f);

            // Step 3: Initialize Core Managers (60%)
            yield return StartCoroutine(InitializeCoreManagers());
            OnInitializationProgress?.Invoke(0.60f);

            // Step 4: Load Player Data (80%)
            if (IsAuthenticated)
            {
                yield return StartCoroutine(LoadPlayerData());
            }
            OnInitializationProgress?.Invoke(0.80f);

            // Step 5: Final Setup (100%)
            yield return StartCoroutine(FinalSetup());
            OnInitializationProgress?.Invoke(1.0f);

            // Ensure minimum splash duration
            float elapsed = Time.time - _initStartTime;
            if (elapsed < splashMinDuration)
            {
                yield return new WaitForSeconds(splashMinDuration - elapsed);
            }

            _isInitialized = true;
            OnInitializationComplete?.Invoke();

            Debug.Log("[GameManager] Initialization complete!");

            // Navigate to appropriate scene
            if (IsAuthenticated)
            {
                LoadMainMenu();
            }
            else
            {
                // Show login/onboarding
            }
        }

        private IEnumerator InitializeFirebase()
        {
            var task = FirebaseApp.CheckAndFixDependenciesAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Result == DependencyStatus.Available)
            {
                _firebaseApp = FirebaseApp.DefaultInstance;
                _auth = FirebaseAuth.DefaultInstance;
                
                // Subscribe to auth state changes
                _auth.StateChanged += OnAuthStateChanged;

                Debug.Log("[GameManager] Firebase initialized successfully");
            }
            else
            {
                Debug.LogError($"[GameManager] Firebase initialization failed: {task.Result}");
            }
        }

        private IEnumerator CheckAuthentication()
        {
            if (skipAuthentication && enableDebugMode)
            {
                Debug.Log("[GameManager] Skipping authentication (debug mode)");
                yield break;
            }

            _currentUser = _auth.CurrentUser;

            if (_currentUser != null)
            {
                Debug.Log($"[GameManager] User already signed in: {_currentUser.UserId}");
                OnUserAuthenticated?.Invoke();
            }
            else
            {
                Debug.Log("[GameManager] No user signed in");
            }

            yield return null;
        }

        private void OnAuthStateChanged(object sender, EventArgs e)
        {
            var newUser = _auth.CurrentUser;
            
            if (newUser != _currentUser)
            {
                bool wasSignedIn = _currentUser != null;
                bool isSignedIn = newUser != null;
                
                _currentUser = newUser;

                if (!wasSignedIn && isSignedIn)
                {
                    Debug.Log($"[GameManager] User signed in: {_currentUser.UserId}");
                    OnUserAuthenticated?.Invoke();
                    
                    // Track analytics
                    Analytics.AnalyticsManager.Instance?.SetUserProperty("user_id", _currentUser.UserId);
                    Analytics.AnalyticsManager.Instance?.TrackEvent("user_signed_in");
                }
                else if (wasSignedIn && !isSignedIn)
                {
                    Debug.Log("[GameManager] User signed out");
                    OnUserSignedOut?.Invoke();
                    
                    Analytics.AnalyticsManager.Instance?.TrackEvent("user_signed_out");
                }
            }
        }

        /// <summary>
        /// Sign in with email and password
        /// </summary>
        public async Task<bool> SignInWithEmail(string email, string password)
        {
            try
            {
                var result = await _auth.SignInWithEmailAndPasswordAsync(email, password);
                _currentUser = result.User;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] Sign in failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create account with email and password
        /// </summary>
        public async Task<bool> CreateAccount(string email, string password, string displayName)
        {
            try
            {
                var result = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
                _currentUser = result.User;

                // Update display name
                var profile = new UserProfile { DisplayName = displayName };
                await _currentUser.UpdateUserProfileAsync(profile);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] Account creation failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sign out current user
        /// </summary>
        public void SignOut()
        {
            _auth.SignOut();
            _currentUser = null;
        }

        private void OnDestroy()
        {
            if (_auth != null)
            {
                _auth.StateChanged -= OnAuthStateChanged;
            }
        }
#else
        /// <summary>
        /// Initialize all game systems (stub mode)
        /// </summary>
        public IEnumerator InitializeGame()
        {
            _initStartTime = Time.time;
            OnInitializationStarted?.Invoke();

            Debug.LogWarning("[GameManager] Firebase SDK not installed. Running in stub mode.");

            OnInitializationProgress?.Invoke(0.20f);
            yield return null;

            OnInitializationProgress?.Invoke(0.40f);
            yield return StartCoroutine(InitializeCoreManagers());

            OnInitializationProgress?.Invoke(0.60f);
            yield return StartCoroutine(FinalSetup());

            OnInitializationProgress?.Invoke(1.0f);

            // Ensure minimum splash duration
            float elapsed = Time.time - _initStartTime;
            if (elapsed < splashMinDuration)
            {
                yield return new WaitForSeconds(splashMinDuration - elapsed);
            }

            _isInitialized = true;
            _firebaseAppStub = true;
            OnInitializationComplete?.Invoke();

            Debug.Log("[GameManager] Initialization complete (stub mode)!");
        }

        public Task<bool> SignInWithEmail(string email, string password)
        {
            Debug.LogWarning("[GameManager] Firebase SDK not installed. SignInWithEmail is a stub.");
            _currentUserIdStub = "stub_user_id";
            OnUserAuthenticated?.Invoke();
            return Task.FromResult(true);
        }

        public Task<bool> CreateAccount(string email, string password, string displayName)
        {
            Debug.LogWarning("[GameManager] Firebase SDK not installed. CreateAccount is a stub.");
            _currentUserIdStub = "stub_user_id";
            OnUserAuthenticated?.Invoke();
            return Task.FromResult(true);
        }

        public void SignOut()
        {
            Debug.LogWarning("[GameManager] Firebase SDK not installed. SignOut is a stub.");
            _currentUserIdStub = null;
            OnUserSignedOut?.Invoke();
        }

        private void OnDestroy()
        {
            // No Firebase cleanup needed in stub mode
        }
#endif

        private IEnumerator InitializeCoreManagers()
        {
            // Initialize managers that are already in the scene
            // They self-initialize in their Start() methods

            // Ensure AntiCheat is running
            if (AntiCheat.AntiCheatManager.Instance != null)
            {
                AntiCheat.AntiCheatManager.Instance.StartLocationValidation();
            }

            // Initialize Analytics
            if (Analytics.AnalyticsManager.Instance != null)
            {
                Analytics.AnalyticsManager.Instance.SetUserProperty("initialized", true);
            }

            yield return null;
        }

        private IEnumerator LoadPlayerData()
        {
            var tasks = new System.Collections.Generic.List<Task>();

            // Load player profile
            if (Player.PlayerManager.Instance != null)
            {
                // PlayerManager handles its own loading
            }

            // Load season pass progress
            if (SeasonPass.SeasonPassManager.Instance != null)
            {
                SeasonPass.SeasonPassManager.Instance.LoadCurrentSeason();
            }

            // Load friends list
            if (Social.FriendsManager.Instance != null)
            {
                Social.FriendsManager.Instance.LoadFriendsList();
            }

            // Load referral data
            if (Referrals.ReferralManager.Instance != null)
            {
                Referrals.ReferralManager.Instance.LoadReferralData();
            }

            // Apply pending referral code (from deep link)
            if (Referrals.ReferralManager.Instance != null)
            {
                _ = Referrals.ReferralManager.Instance.ApplyPendingReferralCode();
            }

            yield return new WaitForSeconds(0.5f); // Allow async operations to start
        }

        private IEnumerator FinalSetup()
        {
            // Register for push notifications
            // Initialize in-app purchases
            // Load remote config

            yield return null;
        }

        /// <summary>
        /// Load main menu scene
        /// </summary>
        public void LoadMainMenu()
        {
            SceneManager.LoadScene(mainMenuScene);
        }

        /// <summary>
        /// Load gameplay scene
        /// </summary>
        public void LoadGameplay()
        {
            SceneManager.LoadScene(gameplayScene);
        }

        /// <summary>
        /// Quit the application
        /// </summary>
        public void QuitGame()
        {
            Analytics.AnalyticsManager.Instance?.EndSession();
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                // App going to background
                Analytics.AnalyticsManager.Instance?.TrackEvent("app_background");
            }
            else
            {
                // App coming to foreground
                Analytics.AnalyticsManager.Instance?.TrackEvent("app_foreground");
                
                // Refresh data
                if (IsAuthenticated)
                {
                    WorldEvents.WorldEventManager.Instance?.RefreshEvents();
                    SeasonPass.SeasonPassManager.Instance?.RefreshProgress();
                }
            }
        }
    }
}
