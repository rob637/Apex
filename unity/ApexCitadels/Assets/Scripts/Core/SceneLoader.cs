using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ApexCitadels.Core
{
    /// <summary>
    /// Handles scene transitions with loading screens and fade effects.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [Header("Scene Names")]
        public const string SCENE_BOOTSTRAP = "Bootstrap";
        public const string SCENE_MAIN_MENU = "MainMenu";
        public const string SCENE_AR_GAMEPLAY = "ARGameplay";
        public const string SCENE_MAP_VIEW = "MapView";

        [Header("Transition Settings")]
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private UnityEngine.UI.Slider loadingProgressBar;

        // Events
        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;
        public event Action<float> OnLoadProgress;

        // State
        private bool _isLoading = false;
        private string _currentScene;

        public bool IsLoading => _isLoading;
        public string CurrentScene => _currentScene;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _currentScene = SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// Load a scene with optional fade transition
        /// </summary>
        public void LoadScene(string sceneName, bool useFade = true)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneLoader] Already loading a scene, ignoring request for {sceneName}");
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneName, useFade));
        }

        /// <summary>
        /// Load the main menu scene
        /// </summary>
        public void LoadMainMenu() => LoadScene(SCENE_MAIN_MENU);

        /// <summary>
        /// Load the AR gameplay scene
        /// </summary>
        public void LoadARGameplay() => LoadScene(SCENE_AR_GAMEPLAY);

        /// <summary>
        /// Load the map view scene
        /// </summary>
        public void LoadMapView() => LoadScene(SCENE_MAP_VIEW);

        /// <summary>
        /// Reload the current scene
        /// </summary>
        public void ReloadCurrentScene() => LoadScene(_currentScene);

        private IEnumerator LoadSceneAsync(string sceneName, bool useFade)
        {
            _isLoading = true;
            OnSceneLoadStarted?.Invoke(sceneName);

            Debug.Log($"[SceneLoader] Loading scene: {sceneName}");

            // Fade out
            if (useFade && fadeCanvasGroup != null)
            {
                yield return StartCoroutine(Fade(1f));
            }

            // Show loading screen
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
            }

            // Start async load
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            // Wait for load to complete
            while (asyncLoad.progress < 0.9f)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                OnLoadProgress?.Invoke(progress);

                if (loadingProgressBar != null)
                {
                    loadingProgressBar.value = progress;
                }

                yield return null;
            }

            // Finish loading
            OnLoadProgress?.Invoke(1f);
            if (loadingProgressBar != null)
            {
                loadingProgressBar.value = 1f;
            }

            // Brief pause at 100%
            yield return new WaitForSeconds(0.2f);

            // Activate the scene
            asyncLoad.allowSceneActivation = true;

            // Wait for scene to fully activate
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            _currentScene = sceneName;

            // Hide loading screen
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }

            // Fade in
            if (useFade && fadeCanvasGroup != null)
            {
                yield return StartCoroutine(Fade(0f));
            }

            _isLoading = false;
            OnSceneLoadCompleted?.Invoke(sceneName);

            Debug.Log($"[SceneLoader] Scene loaded: {sceneName}");
        }

        private IEnumerator Fade(float targetAlpha)
        {
            if (fadeCanvasGroup == null) yield break;

            float startAlpha = fadeCanvasGroup.alpha;
            float elapsed = 0f;

            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.blocksRaycasts = true;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = targetAlpha;

            if (targetAlpha <= 0f)
            {
                fadeCanvasGroup.blocksRaycasts = false;
                fadeCanvasGroup.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Additively load a scene (for overlays)
        /// </summary>
        public void LoadSceneAdditive(string sceneName)
        {
            StartCoroutine(LoadAdditiveAsync(sceneName));
        }

        private IEnumerator LoadAdditiveAsync(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            Debug.Log($"[SceneLoader] Additive scene loaded: {sceneName}");
        }

        /// <summary>
        /// Unload an additively loaded scene
        /// </summary>
        public void UnloadScene(string sceneName)
        {
            SceneManager.UnloadSceneAsync(sceneName);
        }
    }
}
