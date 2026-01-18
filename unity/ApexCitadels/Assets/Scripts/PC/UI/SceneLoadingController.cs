using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Async scene loading with visual feedback.
    /// Integrates with LoadingScreenManager for seamless transitions.
    /// Features:
    /// - Async scene loading with progress
    /// - Additive scene loading support
    /// - Scene transition callbacks
    /// - Preloading for instant transitions
    /// </summary>
    public class SceneLoadingController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float minimumLoadTime = 1.5f;
        [SerializeField] private bool useLoadingScreen = true;
        
        [Header("Progress Weights")]
        [SerializeField] private float sceneLoadWeight = 0.7f;
        [SerializeField] private float initializationWeight = 0.3f;
        
        // Singleton
        private static SceneLoadingController _instance;
        public static SceneLoadingController Instance => _instance;
        
        // State
        private bool _isLoading;
        private AsyncOperation _currentLoadOperation;
        private Dictionary<string, AsyncOperation> _preloadedScenes = new Dictionary<string, AsyncOperation>();
        
        // Events
        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadComplete;
        public event Action<float> OnLoadProgress;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        #region Public API
        
        /// <summary>
        /// Load a scene with loading screen
        /// </summary>
        public void LoadScene(string sceneName, LoadingContext loadingContext = null, Action onComplete = null)
        {
            if (_isLoading)
            {
                Debug.LogWarning("Already loading a scene");
                return;
            }
            
            StartCoroutine(LoadSceneAsync(sceneName, LoadSceneMode.Single, loadingContext, onComplete));
        }
        
        /// <summary>
        /// Load a scene additively
        /// </summary>
        public void LoadSceneAdditive(string sceneName, LoadingContext loadingContext = null, Action onComplete = null)
        {
            if (_isLoading)
            {
                Debug.LogWarning("Already loading a scene");
                return;
            }
            
            StartCoroutine(LoadSceneAsync(sceneName, LoadSceneMode.Additive, loadingContext, onComplete));
        }
        
        /// <summary>
        /// Unload a scene
        /// </summary>
        public void UnloadScene(string sceneName, Action onComplete = null)
        {
            StartCoroutine(UnloadSceneAsync(sceneName, onComplete));
        }
        
        /// <summary>
        /// Preload a scene for instant transition later
        /// </summary>
        public void PreloadScene(string sceneName, Action onReady = null)
        {
            if (_preloadedScenes.ContainsKey(sceneName))
            {
                onReady?.Invoke();
                return;
            }
            
            StartCoroutine(PreloadSceneAsync(sceneName, onReady));
        }
        
        /// <summary>
        /// Activate a preloaded scene
        /// </summary>
        public void ActivatePreloadedScene(string sceneName, LoadingContext loadingContext = null, Action onComplete = null)
        {
            if (!_preloadedScenes.TryGetValue(sceneName, out var operation))
            {
                Debug.LogWarning($"Scene {sceneName} is not preloaded");
                LoadScene(sceneName, loadingContext, onComplete);
                return;
            }
            
            StartCoroutine(ActivatePreloadedSceneAsync(sceneName, operation, loadingContext, onComplete));
        }
        
        /// <summary>
        /// Check if a scene is preloaded
        /// </summary>
        public bool IsScenePreloaded(string sceneName)
        {
            return _preloadedScenes.ContainsKey(sceneName);
        }
        
        /// <summary>
        /// Check if currently loading
        /// </summary>
        public bool IsLoading => _isLoading;
        
        /// <summary>
        /// Cancel preloaded scene
        /// </summary>
        public void CancelPreload(string sceneName)
        {
            if (_preloadedScenes.ContainsKey(sceneName))
            {
                _preloadedScenes.Remove(sceneName);
            }
        }
        
        #endregion
        
        #region Loading Coroutines
        
        private IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode mode, 
            LoadingContext loadingContext, Action onComplete)
        {
            _isLoading = true;
            OnSceneLoadStarted?.Invoke(sceneName);
            
            // Show loading screen
            if (useLoadingScreen && LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.Show(loadingContext);
                
                // Register progress steps
                LoadingScreenManager.Instance.RegisterStep("scene_load", sceneLoadWeight);
                LoadingScreenManager.Instance.RegisterStep("initialization", initializationWeight);
            }
            
            // Small delay to ensure loading screen is visible
            yield return new WaitForSecondsRealtime(0.1f);
            
            // Start async load
            _currentLoadOperation = SceneManager.LoadSceneAsync(sceneName, mode);
            _currentLoadOperation.allowSceneActivation = false;
            
            float startTime = Time.unscaledTime;
            
            // Wait for load to reach 90% (Unity holds at 90% until allowSceneActivation)
            while (_currentLoadOperation.progress < 0.9f)
            {
                float progress = _currentLoadOperation.progress / 0.9f;
                
                if (useLoadingScreen && LoadingScreenManager.Instance != null)
                {
                    LoadingScreenManager.Instance.UpdateStep("scene_load", progress, 
                        $"Loading {sceneName}... {Mathf.RoundToInt(progress * 100)}%");
                }
                
                OnLoadProgress?.Invoke(progress * sceneLoadWeight);
                
                yield return null;
            }
            
            // Complete scene load step
            if (useLoadingScreen && LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.CompleteStep("scene_load", "Initializing...");
            }
            
            // Enforce minimum load time
            float elapsed = Time.unscaledTime - startTime;
            if (elapsed < minimumLoadTime)
            {
                // Simulate initialization progress during wait
                float remaining = minimumLoadTime - elapsed;
                float initElapsed = 0;
                
                while (initElapsed < remaining)
                {
                    initElapsed += Time.unscaledDeltaTime;
                    float initProgress = initElapsed / remaining;
                    
                    if (useLoadingScreen && LoadingScreenManager.Instance != null)
                    {
                        LoadingScreenManager.Instance.UpdateStep("initialization", initProgress, 
                            "Preparing scene...");
                    }
                    
                    yield return null;
                }
            }
            
            // Complete initialization
            if (useLoadingScreen && LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.CompleteStep("initialization", "Ready!");
            }
            
            // Activate scene
            _currentLoadOperation.allowSceneActivation = true;
            
            // Wait for scene activation
            while (!_currentLoadOperation.isDone)
            {
                yield return null;
            }
            
            // Hide loading screen
            if (useLoadingScreen && LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.Hide(() =>
                {
                    _isLoading = false;
                    _currentLoadOperation = null;
                    OnSceneLoadComplete?.Invoke(sceneName);
                    onComplete?.Invoke();
                });
            }
            else
            {
                _isLoading = false;
                _currentLoadOperation = null;
                OnSceneLoadComplete?.Invoke(sceneName);
                onComplete?.Invoke();
            }
        }
        
        private IEnumerator UnloadSceneAsync(string sceneName, Action onComplete)
        {
            var operation = SceneManager.UnloadSceneAsync(sceneName);
            
            if (operation == null)
            {
                Debug.LogWarning($"Could not unload scene: {sceneName}");
                onComplete?.Invoke();
                yield break;
            }
            
            while (!operation.isDone)
            {
                yield return null;
            }
            
            onComplete?.Invoke();
        }
        
        private IEnumerator PreloadSceneAsync(string sceneName, Action onReady)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            operation.allowSceneActivation = false;
            
            // Wait for 90% load
            while (operation.progress < 0.9f)
            {
                yield return null;
            }
            
            _preloadedScenes[sceneName] = operation;
            onReady?.Invoke();
        }
        
        private IEnumerator ActivatePreloadedSceneAsync(string sceneName, AsyncOperation operation,
            LoadingContext loadingContext, Action onComplete)
        {
            _isLoading = true;
            
            // Show brief loading screen
            if (useLoadingScreen && LoadingScreenManager.Instance != null)
            {
                var quickContext = loadingContext ?? LoadingContext.QuickLoad;
                LoadingScreenManager.Instance.Show(quickContext);
            }
            
            // Small delay
            yield return new WaitForSecondsRealtime(0.2f);
            
            // Activate
            operation.allowSceneActivation = true;
            
            while (!operation.isDone)
            {
                yield return null;
            }
            
            _preloadedScenes.Remove(sceneName);
            
            // Hide loading screen
            if (useLoadingScreen && LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.Hide(() =>
                {
                    _isLoading = false;
                    onComplete?.Invoke();
                });
            }
            else
            {
                _isLoading = false;
                onComplete?.Invoke();
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Splash screen controller for game startup.
    /// Handles logo display, version info, and initial loading.
    /// </summary>
    public class SplashScreenController : MonoBehaviour
    {
        [Header("Splash Elements")]
        [SerializeField] private RectTransform splashContainer;
        [SerializeField] private CanvasGroup splashCanvasGroup;
        [SerializeField] private Image logoImage;
        [SerializeField] private Image companyLogoImage;
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private TextMeshProUGUI copyrightText;
        [SerializeField] private Image loadingIndicator;
        
        [Header("Settings")]
        [SerializeField] private float companyLogoDuration = 2f;
        [SerializeField] private float gameLogoDuration = 2f;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private AnimationCurve fadeCurve;
        
        [Header("Background Loading")]
        [SerializeField] private bool preloadMainMenu = true;
        
        // State
        private bool _isComplete;
        
        // Events
        public event Action OnSplashComplete;
        
        private void Awake()
        {
            if (fadeCurve == null || fadeCurve.length == 0)
            {
                fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
        }
        
        private void Start()
        {
            SetVersion();
            StartCoroutine(PlaySplashSequence());
        }
        
        private void SetVersion()
        {
            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
            }
        }
        
        private IEnumerator PlaySplashSequence()
        {
            // Start background preloading
            if (preloadMainMenu && SceneLoadingController.Instance != null)
            {
                SceneLoadingController.Instance.PreloadScene(mainMenuSceneName);
            }
            
            // Hide everything initially
            splashCanvasGroup.alpha = 0;
            if (companyLogoImage != null) companyLogoImage.gameObject.SetActive(false);
            if (logoImage != null) logoImage.gameObject.SetActive(false);
            
            // Show splash container
            splashCanvasGroup.alpha = 1;
            
            // Company logo sequence
            if (companyLogoImage != null)
            {
                yield return StartCoroutine(ShowElement(companyLogoImage.GetComponent<CanvasGroup>() ?? 
                    companyLogoImage.gameObject.AddComponent<CanvasGroup>(), companyLogoDuration));
            }
            
            // Game logo sequence
            if (logoImage != null)
            {
                yield return StartCoroutine(ShowElement(logoImage.GetComponent<CanvasGroup>() ?? 
                    logoImage.gameObject.AddComponent<CanvasGroup>(), gameLogoDuration));
            }
            
            // Fade out splash
            yield return StartCoroutine(FadeOut());
            
            _isComplete = true;
            OnSplashComplete?.Invoke();
            
            // Transition to main menu
            TransitionToMainMenu();
        }
        
        private IEnumerator ShowElement(CanvasGroup element, float duration)
        {
            element.gameObject.SetActive(true);
            element.alpha = 0;
            
            // Fade in
            float elapsed = 0;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                element.alpha = fadeCurve.Evaluate(elapsed / fadeInDuration);
                yield return null;
            }
            element.alpha = 1;
            
            // Hold
            yield return new WaitForSeconds(duration - fadeInDuration - fadeOutDuration);
            
            // Fade out
            elapsed = 0;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                element.alpha = 1 - fadeCurve.Evaluate(elapsed / fadeOutDuration);
                yield return null;
            }
            element.alpha = 0;
            element.gameObject.SetActive(false);
        }
        
        private IEnumerator FadeOut()
        {
            float elapsed = 0;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                splashCanvasGroup.alpha = 1 - fadeCurve.Evaluate(elapsed / fadeOutDuration);
                yield return null;
            }
            splashCanvasGroup.alpha = 0;
        }
        
        private void TransitionToMainMenu()
        {
            if (SceneLoadingController.Instance != null)
            {
                if (SceneLoadingController.Instance.IsScenePreloaded(mainMenuSceneName))
                {
                    SceneLoadingController.Instance.ActivatePreloadedScene(mainMenuSceneName);
                }
                else
                {
                    SceneLoadingController.Instance.LoadScene(mainMenuSceneName, LoadingContext.GameStart);
                }
            }
            else
            {
                // Fallback to direct scene load
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }
        
        /// <summary>
        /// Skip splash (for development)
        /// </summary>
        public void Skip()
        {
            if (_isComplete) return;
            
            StopAllCoroutines();
            splashCanvasGroup.alpha = 0;
            _isComplete = true;
            OnSplashComplete?.Invoke();
            TransitionToMainMenu();
        }
        
        private void Update()
        {
            // Skip on any key/click (development convenience)
            #if UNITY_EDITOR
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                Skip();
            }
            #endif
            
            // Animate loading indicator
            if (loadingIndicator != null)
            {
                loadingIndicator.transform.Rotate(0, 0, -180f * Time.deltaTime);
            }
        }
    }
}
