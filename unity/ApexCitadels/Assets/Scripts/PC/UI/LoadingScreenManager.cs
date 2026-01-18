using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Comprehensive loading screen system for PC client.
    /// Features:
    /// - Multiple loading screen styles
    /// - Dynamic progress tracking
    /// - Animated backgrounds
    /// - Gameplay tips display
    /// - Scene-specific loading contexts
    /// - Minimum display time enforcement
    /// </summary>
    public class LoadingScreenManager : MonoBehaviour
    {
        [Header("Loading Screen Container")]
        [SerializeField] private RectTransform loadingScreenContainer;
        [SerializeField] private CanvasGroup loadingCanvasGroup;
        
        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private List<Sprite> backgroundImages = new List<Sprite>();
        [SerializeField] private Image overlayImage;
        [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.5f);
        
        [Header("Logo/Title")]
        [SerializeField] private RectTransform logoContainer;
        [SerializeField] private Image logoImage;
        [SerializeField] private TextMeshProUGUI titleText;
        
        [Header("Progress Bar")]
        [SerializeField] private RectTransform progressBarContainer;
        [SerializeField] private Image progressBarBackground;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private Image progressBarGlow;
        [SerializeField] private TextMeshProUGUI progressPercentText;
        [SerializeField] private TextMeshProUGUI progressStatusText;
        
        [Header("Spinner")]
        [SerializeField] private RectTransform spinnerContainer;
        [SerializeField] private Image spinnerImage;
        [SerializeField] private float spinnerSpeed = 180f;
        
        [Header("Tips Display")]
        [SerializeField] private RectTransform tipsContainer;
        [SerializeField] private TextMeshProUGUI tipText;
        [SerializeField] private TextMeshProUGUI tipLabelText;
        [SerializeField] private List<string> gameplayTips = new List<string>();
        [SerializeField] private float tipChangeInterval = 5f;
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float minimumDisplayTime = 2f;
        [SerializeField] private float progressSmoothSpeed = 3f;
        [SerializeField] private AnimationCurve fadeCurve;
        
        [Header("Particle Effects")]
        [SerializeField] private ParticleSystem backgroundParticles;
        [SerializeField] private ParticleSystem progressCompleteParticles;
        
        // Singleton
        private static LoadingScreenManager _instance;
        public static LoadingScreenManager Instance => _instance;
        
        // State
        private bool _isLoading;
        private float _currentProgress;
        private float _targetProgress;
        private float _displayStartTime;
        private string _currentStatus;
        private LoadingContext _currentContext;
        private Coroutine _tipRotationCoroutine;
        private Coroutine _spinnerCoroutine;
        private Coroutine _backgroundAnimationCoroutine;
        
        // Progress tracking
        private Dictionary<string, float> _progressSteps = new Dictionary<string, float>();
        private float _totalWeight;
        
        // Events
        public event Action OnLoadingStarted;
        public event Action OnLoadingComplete;
        public event Action<float> OnProgressChanged;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeCurve();
            InitializeDefaultTips();
            HideImmediate();
        }
        
        private void InitializeCurve()
        {
            if (fadeCurve == null || fadeCurve.length == 0)
            {
                fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
        }
        
        private void InitializeDefaultTips()
        {
            if (gameplayTips.Count == 0)
            {
                gameplayTips = new List<string>
                {
                    "Build walls to protect your citadel from enemy attacks.",
                    "Upgrade your resource buildings to increase production rates.",
                    "Form alliances with other players for mutual defense.",
                    "Scout enemy territories before launching attacks.",
                    "Complete daily quests for bonus rewards.",
                    "Research new technologies to unlock powerful buildings.",
                    "Place defensive towers at strategic chokepoints.",
                    "Trade resources with allies to optimize your economy.",
                    "Participate in territory wars to expand your empire.",
                    "Train specialized troops for different combat situations."
                };
            }
        }
        
        private void Update()
        {
            if (_isLoading)
            {
                UpdateProgress();
                UpdateSpinner();
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Show loading screen with context
        /// </summary>
        public void Show(LoadingContext context = null)
        {
            if (_isLoading) return;
            
            _currentContext = context ?? new LoadingContext();
            _isLoading = true;
            _currentProgress = 0;
            _targetProgress = 0;
            _displayStartTime = Time.unscaledTime;
            _progressSteps.Clear();
            _totalWeight = 0;
            
            ConfigureForContext(_currentContext);
            StartCoroutine(FadeIn());
            
            OnLoadingStarted?.Invoke();
        }
        
        /// <summary>
        /// Hide loading screen
        /// </summary>
        public void Hide(Action onComplete = null)
        {
            if (!_isLoading) 
            {
                onComplete?.Invoke();
                return;
            }
            
            StartCoroutine(HideWithMinimumTime(onComplete));
        }
        
        /// <summary>
        /// Set progress directly (0-1)
        /// </summary>
        public void SetProgress(float progress, string status = null)
        {
            _targetProgress = Mathf.Clamp01(progress);
            
            if (!string.IsNullOrEmpty(status))
            {
                _currentStatus = status;
                if (progressStatusText != null)
                {
                    progressStatusText.text = status;
                }
            }
            
            OnProgressChanged?.Invoke(_targetProgress);
        }
        
        /// <summary>
        /// Register a progress step with weight
        /// </summary>
        public void RegisterStep(string stepId, float weight = 1f)
        {
            if (!_progressSteps.ContainsKey(stepId))
            {
                _progressSteps[stepId] = 0;
                _totalWeight += weight;
            }
        }
        
        /// <summary>
        /// Complete a progress step
        /// </summary>
        public void CompleteStep(string stepId, string status = null)
        {
            if (_progressSteps.ContainsKey(stepId))
            {
                _progressSteps[stepId] = 1f;
                CalculateOverallProgress();
            }
            
            if (!string.IsNullOrEmpty(status))
            {
                SetStatus(status);
            }
        }
        
        /// <summary>
        /// Update a step's progress (0-1)
        /// </summary>
        public void UpdateStep(string stepId, float progress, string status = null)
        {
            if (_progressSteps.ContainsKey(stepId))
            {
                _progressSteps[stepId] = Mathf.Clamp01(progress);
                CalculateOverallProgress();
            }
            
            if (!string.IsNullOrEmpty(status))
            {
                SetStatus(status);
            }
        }
        
        /// <summary>
        /// Set status text without changing progress
        /// </summary>
        public void SetStatus(string status)
        {
            _currentStatus = status;
            if (progressStatusText != null)
            {
                progressStatusText.text = status;
            }
        }
        
        /// <summary>
        /// Check if loading screen is visible
        /// </summary>
        public bool IsLoading => _isLoading;
        
        /// <summary>
        /// Get current progress
        /// </summary>
        public float Progress => _currentProgress;
        
        #endregion
        
        #region Context Configuration
        
        private void ConfigureForContext(LoadingContext context)
        {
            // Set background
            if (context.backgroundImage != null)
            {
                backgroundImage.sprite = context.backgroundImage;
            }
            else if (backgroundImages.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, backgroundImages.Count);
                backgroundImage.sprite = backgroundImages[randomIndex];
            }
            
            // Set title
            if (titleText != null)
            {
                titleText.text = !string.IsNullOrEmpty(context.title) ? 
                    context.title : "Loading...";
            }
            
            // Configure progress bar visibility
            if (progressBarContainer != null)
            {
                progressBarContainer.gameObject.SetActive(context.showProgressBar);
            }
            
            // Configure spinner
            if (spinnerContainer != null)
            {
                spinnerContainer.gameObject.SetActive(context.showSpinner);
            }
            
            // Configure tips
            if (tipsContainer != null)
            {
                tipsContainer.gameObject.SetActive(context.showTips);
                
                if (context.showTips)
                {
                    StartTipRotation(context.customTips?.Count > 0 ? context.customTips : gameplayTips);
                }
            }
            
            // Set initial status
            SetStatus(context.initialStatus ?? "Loading...");
            
            // Start background animation
            if (context.animateBackground)
            {
                StartBackgroundAnimation();
            }
            
            // Start particles
            if (backgroundParticles != null && context.showParticles)
            {
                backgroundParticles.Play();
            }
        }
        
        #endregion
        
        #region Progress Updates
        
        private void CalculateOverallProgress()
        {
            if (_totalWeight <= 0)
            {
                _targetProgress = 0;
                return;
            }
            
            float weightedSum = 0;
            float currentWeight = 0;
            
            foreach (var kvp in _progressSteps)
            {
                weightedSum += kvp.Value;
                currentWeight += 1f; // Assuming equal weights for simplicity
            }
            
            _targetProgress = weightedSum / _progressSteps.Count;
            OnProgressChanged?.Invoke(_targetProgress);
        }
        
        private void UpdateProgress()
        {
            // Smooth progress bar movement
            if (Mathf.Abs(_currentProgress - _targetProgress) > 0.001f)
            {
                _currentProgress = Mathf.Lerp(_currentProgress, _targetProgress, 
                    Time.unscaledDeltaTime * progressSmoothSpeed);
                
                // Clamp to prevent overshoot
                if (_currentProgress > _targetProgress - 0.01f && _currentProgress < _targetProgress + 0.01f)
                {
                    _currentProgress = _targetProgress;
                }
                
                UpdateProgressUI();
            }
        }
        
        private void UpdateProgressUI()
        {
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = _currentProgress;
            }
            
            if (progressPercentText != null)
            {
                progressPercentText.text = $"{Mathf.RoundToInt(_currentProgress * 100)}%";
            }
            
            // Glow effect based on progress
            if (progressBarGlow != null)
            {
                float glowAlpha = Mathf.PingPong(Time.unscaledTime * 2f, 0.5f) + 0.3f;
                progressBarGlow.color = new Color(1, 1, 1, glowAlpha * _currentProgress);
            }
        }
        
        private void UpdateSpinner()
        {
            if (spinnerImage != null && spinnerContainer != null && spinnerContainer.gameObject.activeSelf)
            {
                spinnerImage.transform.Rotate(0, 0, -spinnerSpeed * Time.unscaledDeltaTime);
            }
        }
        
        #endregion
        
        #region Animation Coroutines
        
        private IEnumerator FadeIn()
        {
            loadingScreenContainer.gameObject.SetActive(true);
            
            float elapsed = 0;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = fadeCurve.Evaluate(elapsed / fadeInDuration);
                
                loadingCanvasGroup.alpha = t;
                
                // Logo animation
                if (logoContainer != null)
                {
                    logoContainer.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
                }
                
                yield return null;
            }
            
            loadingCanvasGroup.alpha = 1;
            if (logoContainer != null) logoContainer.localScale = Vector3.one;
        }
        
        private IEnumerator HideWithMinimumTime(Action onComplete)
        {
            // Enforce minimum display time
            float elapsedDisplayTime = Time.unscaledTime - _displayStartTime;
            if (elapsedDisplayTime < minimumDisplayTime)
            {
                float waitTime = minimumDisplayTime - elapsedDisplayTime;
                
                // Ensure progress reaches 100% during wait
                _targetProgress = 1f;
                
                yield return new WaitForSecondsRealtime(waitTime);
            }
            
            // Ensure progress is at 100%
            _currentProgress = 1f;
            UpdateProgressUI();
            
            // Play completion particles
            if (progressCompleteParticles != null)
            {
                progressCompleteParticles.Play();
            }
            
            // Brief pause at 100%
            yield return new WaitForSecondsRealtime(0.3f);
            
            // Fade out
            float elapsed = 0;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = fadeCurve.Evaluate(elapsed / fadeOutDuration);
                
                loadingCanvasGroup.alpha = 1 - t;
                
                yield return null;
            }
            
            HideImmediate();
            
            OnLoadingComplete?.Invoke();
            onComplete?.Invoke();
        }
        
        private void HideImmediate()
        {
            _isLoading = false;
            loadingCanvasGroup.alpha = 0;
            loadingScreenContainer.gameObject.SetActive(false);
            
            StopTipRotation();
            StopBackgroundAnimation();
            
            if (backgroundParticles != null)
            {
                backgroundParticles.Stop();
            }
        }
        
        #endregion
        
        #region Tip Rotation
        
        private void StartTipRotation(List<string> tips)
        {
            StopTipRotation();
            _tipRotationCoroutine = StartCoroutine(RotateTips(tips));
        }
        
        private void StopTipRotation()
        {
            if (_tipRotationCoroutine != null)
            {
                StopCoroutine(_tipRotationCoroutine);
                _tipRotationCoroutine = null;
            }
        }
        
        private IEnumerator RotateTips(List<string> tips)
        {
            if (tips.Count == 0) yield break;
            
            int currentTipIndex = UnityEngine.Random.Range(0, tips.Count);
            
            while (_isLoading)
            {
                // Show current tip with fade
                yield return StartCoroutine(ShowTip(tips[currentTipIndex]));
                
                // Wait
                yield return new WaitForSecondsRealtime(tipChangeInterval);
                
                // Move to next tip
                currentTipIndex = (currentTipIndex + 1) % tips.Count;
            }
        }
        
        private IEnumerator ShowTip(string tip)
        {
            if (tipText == null) yield break;
            
            // Fade out old tip
            float elapsed = 0;
            float fadeDuration = 0.3f;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                tipText.alpha = 1 - (elapsed / fadeDuration);
                yield return null;
            }
            
            // Set new tip
            tipText.text = tip;
            
            // Fade in new tip
            elapsed = 0;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                tipText.alpha = elapsed / fadeDuration;
                yield return null;
            }
            
            tipText.alpha = 1;
        }
        
        #endregion
        
        #region Background Animation
        
        private void StartBackgroundAnimation()
        {
            StopBackgroundAnimation();
            _backgroundAnimationCoroutine = StartCoroutine(AnimateBackground());
        }
        
        private void StopBackgroundAnimation()
        {
            if (_backgroundAnimationCoroutine != null)
            {
                StopCoroutine(_backgroundAnimationCoroutine);
                _backgroundAnimationCoroutine = null;
            }
        }
        
        private IEnumerator AnimateBackground()
        {
            if (backgroundImage == null) yield break;
            
            Vector2 originalSize = backgroundImage.rectTransform.sizeDelta;
            float zoomAmount = 0.05f;
            float zoomSpeed = 0.2f;
            
            while (_isLoading)
            {
                // Gentle zoom pulse
                float scale = 1 + Mathf.Sin(Time.unscaledTime * zoomSpeed) * zoomAmount;
                backgroundImage.rectTransform.localScale = Vector3.one * scale;
                
                // Very slow pan (optional)
                float panAmount = 10f;
                float panX = Mathf.Sin(Time.unscaledTime * 0.1f) * panAmount;
                float panY = Mathf.Cos(Time.unscaledTime * 0.08f) * panAmount * 0.5f;
                backgroundImage.rectTransform.anchoredPosition = new Vector2(panX, panY);
                
                yield return null;
            }
            
            // Reset
            backgroundImage.rectTransform.localScale = Vector3.one;
            backgroundImage.rectTransform.anchoredPosition = Vector2.zero;
        }
        
        #endregion
    }
    
    #region Data Types
    
    [Serializable]
    public class LoadingContext
    {
        public string title = "Loading...";
        public string initialStatus = "Initializing...";
        public Sprite backgroundImage;
        public bool showProgressBar = true;
        public bool showSpinner = true;
        public bool showTips = true;
        public bool showParticles = true;
        public bool animateBackground = true;
        public List<string> customTips;
        
        // Preset contexts
        public static LoadingContext GameStart => new LoadingContext
        {
            title = "Apex Citadels",
            initialStatus = "Starting game...",
            showProgressBar = true,
            showTips = true
        };
        
        public static LoadingContext WorldLoad => new LoadingContext
        {
            title = "Entering World",
            initialStatus = "Loading world data...",
            showProgressBar = true,
            showTips = true
        };
        
        public static LoadingContext TerritoryLoad => new LoadingContext
        {
            title = "Loading Territory",
            initialStatus = "Fetching territory data...",
            showProgressBar = true,
            showSpinner = true
        };
        
        public static LoadingContext QuickLoad => new LoadingContext
        {
            title = "",
            showProgressBar = false,
            showSpinner = true,
            showTips = false,
            animateBackground = false
        };
    }
    
    #endregion
}
