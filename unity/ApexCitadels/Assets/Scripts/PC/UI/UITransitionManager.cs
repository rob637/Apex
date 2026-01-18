using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Manages screen transitions and complex UI state changes.
    /// Provides cinematic transitions between game states:
    /// - Screen fades and wipes
    /// - Complex multi-element transitions
    /// - State machine for UI flow
    /// - Cinematic cutscene support
    /// </summary>
    public class UITransitionManager : MonoBehaviour
    {
        [Header("Transition Overlays")]
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private Image wipeOverlay;
        [SerializeField] private RectTransform circleWipeMask;
        
        [Header("Transition Settings")]
        [SerializeField] private float defaultTransitionDuration = 0.5f;
        [SerializeField] private AnimationCurve transitionCurve;
        [SerializeField] private Color fadeColor = Color.black;
        
        [Header("Screen References")]
        [SerializeField] private List<UIScreenConfig> screenConfigs = new List<UIScreenConfig>();
        
        // Singleton
        private static UITransitionManager _instance;
        public static UITransitionManager Instance => _instance;
        
        // State tracking
        private UIScreen _currentScreen = UIScreen.None;
        private bool _isTransitioning;
        private Dictionary<UIScreen, RectTransform> _screens = new Dictionary<UIScreen, RectTransform>();
        
        // Events
        public event Action<UIScreen, UIScreen> OnScreenChanged;
        public event Action<bool> OnTransitionStarted;
        public event Action OnTransitionCompleted;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeScreens();
            InitializeTransitionCurve();
            SetupOverlays();
        }
        
        private void InitializeScreens()
        {
            foreach (var config in screenConfigs)
            {
                if (config.screenRect != null)
                {
                    _screens[config.screen] = config.screenRect;
                }
            }
        }
        
        private void InitializeTransitionCurve()
        {
            if (transitionCurve == null || transitionCurve.length == 0)
            {
                transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
        }
        
        private void SetupOverlays()
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0);
                fadeOverlay.raycastTarget = false;
            }
            
            if (wipeOverlay != null)
            {
                wipeOverlay.gameObject.SetActive(false);
            }
        }
        
        #region Screen Transitions
        
        /// <summary>
        /// Transition to a new screen
        /// </summary>
        public void TransitionTo(UIScreen targetScreen, TransitionType transition = TransitionType.Fade,
            float duration = -1, Action onComplete = null)
        {
            if (_isTransitioning || targetScreen == _currentScreen)
            {
                onComplete?.Invoke();
                return;
            }
            
            if (duration < 0) duration = defaultTransitionDuration;
            
            StartCoroutine(ExecuteTransition(_currentScreen, targetScreen, transition, duration, onComplete));
        }
        
        private IEnumerator ExecuteTransition(UIScreen fromScreen, UIScreen toScreen, 
            TransitionType transition, float duration, Action onComplete)
        {
            _isTransitioning = true;
            OnTransitionStarted?.Invoke(true);
            
            float halfDuration = duration * 0.5f;
            
            switch (transition)
            {
                case TransitionType.Fade:
                    yield return StartCoroutine(FadeTransition(fromScreen, toScreen, duration));
                    break;
                    
                case TransitionType.CrossFade:
                    yield return StartCoroutine(CrossFadeTransition(fromScreen, toScreen, duration));
                    break;
                    
                case TransitionType.SlideLeft:
                    yield return StartCoroutine(SlideTransition(fromScreen, toScreen, Vector2.left, duration));
                    break;
                    
                case TransitionType.SlideRight:
                    yield return StartCoroutine(SlideTransition(fromScreen, toScreen, Vector2.right, duration));
                    break;
                    
                case TransitionType.SlideUp:
                    yield return StartCoroutine(SlideTransition(fromScreen, toScreen, Vector2.up, duration));
                    break;
                    
                case TransitionType.SlideDown:
                    yield return StartCoroutine(SlideTransition(fromScreen, toScreen, Vector2.down, duration));
                    break;
                    
                case TransitionType.CircleWipe:
                    yield return StartCoroutine(CircleWipeTransition(fromScreen, toScreen, duration));
                    break;
                    
                case TransitionType.ZoomIn:
                    yield return StartCoroutine(ZoomTransition(fromScreen, toScreen, true, duration));
                    break;
                    
                case TransitionType.ZoomOut:
                    yield return StartCoroutine(ZoomTransition(fromScreen, toScreen, false, duration));
                    break;
                    
                case TransitionType.Instant:
                    HideScreen(fromScreen);
                    ShowScreen(toScreen);
                    break;
            }
            
            UIScreen previousScreen = _currentScreen;
            _currentScreen = toScreen;
            
            _isTransitioning = false;
            OnTransitionCompleted?.Invoke();
            OnScreenChanged?.Invoke(previousScreen, toScreen);
            
            onComplete?.Invoke();
        }
        
        #endregion
        
        #region Fade Transitions
        
        private IEnumerator FadeTransition(UIScreen fromScreen, UIScreen toScreen, float duration)
        {
            float halfDuration = duration * 0.5f;
            
            // Fade to black
            yield return StartCoroutine(FadeOverlay(0, 1, halfDuration));
            
            // Switch screens
            HideScreen(fromScreen);
            ShowScreen(toScreen);
            
            yield return new WaitForSecondsRealtime(0.1f);
            
            // Fade from black
            yield return StartCoroutine(FadeOverlay(1, 0, halfDuration));
        }
        
        private IEnumerator CrossFadeTransition(UIScreen fromScreen, UIScreen toScreen, float duration)
        {
            RectTransform fromRect = GetScreenRect(fromScreen);
            RectTransform toRect = GetScreenRect(toScreen);
            
            if (toRect != null)
            {
                toRect.gameObject.SetActive(true);
                var toGroup = GetOrAddCanvasGroup(toRect);
                toGroup.alpha = 0;
            }
            
            float elapsed = 0;
            var fromGroup = fromRect != null ? GetOrAddCanvasGroup(fromRect) : null;
            var toGroupRef = toRect != null ? GetOrAddCanvasGroup(toRect) : null;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = transitionCurve.Evaluate(elapsed / duration);
                
                if (fromGroup != null) fromGroup.alpha = 1 - t;
                if (toGroupRef != null) toGroupRef.alpha = t;
                
                yield return null;
            }
            
            if (fromGroup != null)
            {
                fromGroup.alpha = 1;
                fromRect.gameObject.SetActive(false);
            }
            
            if (toGroupRef != null) toGroupRef.alpha = 1;
        }
        
        private IEnumerator FadeOverlay(float fromAlpha, float toAlpha, float duration)
        {
            if (fadeOverlay == null) yield break;
            
            fadeOverlay.raycastTarget = true;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = transitionCurve.Evaluate(elapsed / duration);
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }
            
            fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, toAlpha);
            fadeOverlay.raycastTarget = toAlpha > 0;
        }
        
        #endregion
        
        #region Slide Transitions
        
        private IEnumerator SlideTransition(UIScreen fromScreen, UIScreen toScreen, Vector2 direction, float duration)
        {
            RectTransform fromRect = GetScreenRect(fromScreen);
            RectTransform toRect = GetScreenRect(toScreen);
            
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 slideOffset = direction * screenSize;
            
            Vector2 fromStartPos = fromRect != null ? fromRect.anchoredPosition : Vector2.zero;
            Vector2 fromEndPos = fromStartPos - slideOffset;
            
            Vector2 toEndPos = toRect != null ? toRect.anchoredPosition : Vector2.zero;
            Vector2 toStartPos = toEndPos + slideOffset;
            
            if (toRect != null)
            {
                toRect.anchoredPosition = toStartPos;
                toRect.gameObject.SetActive(true);
            }
            
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = transitionCurve.Evaluate(elapsed / duration);
                
                if (fromRect != null)
                {
                    fromRect.anchoredPosition = Vector2.Lerp(fromStartPos, fromEndPos, t);
                }
                
                if (toRect != null)
                {
                    toRect.anchoredPosition = Vector2.Lerp(toStartPos, toEndPos, t);
                }
                
                yield return null;
            }
            
            if (fromRect != null)
            {
                fromRect.anchoredPosition = fromStartPos;
                fromRect.gameObject.SetActive(false);
            }
            
            if (toRect != null)
            {
                toRect.anchoredPosition = toEndPos;
            }
        }
        
        #endregion
        
        #region Circle Wipe Transition
        
        private IEnumerator CircleWipeTransition(UIScreen fromScreen, UIScreen toScreen, float duration)
        {
            if (circleWipeMask == null)
            {
                yield return StartCoroutine(FadeTransition(fromScreen, toScreen, duration));
                yield break;
            }
            
            float halfDuration = duration * 0.5f;
            
            // Prepare circle mask
            circleWipeMask.gameObject.SetActive(true);
            Vector2 maxSize = new Vector2(Screen.width * 2, Screen.height * 2);
            
            // Shrink circle to close
            float elapsed = 0;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = transitionCurve.Evaluate(elapsed / halfDuration);
                float scale = Mathf.Lerp(1, 0, t);
                circleWipeMask.sizeDelta = maxSize * scale;
                yield return null;
            }
            
            circleWipeMask.sizeDelta = Vector2.zero;
            
            // Switch screens
            HideScreen(fromScreen);
            ShowScreen(toScreen);
            
            yield return new WaitForSecondsRealtime(0.05f);
            
            // Expand circle to open
            elapsed = 0;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = transitionCurve.Evaluate(elapsed / halfDuration);
                float scale = Mathf.Lerp(0, 1, t);
                circleWipeMask.sizeDelta = maxSize * scale;
                yield return null;
            }
            
            circleWipeMask.sizeDelta = maxSize;
            circleWipeMask.gameObject.SetActive(false);
        }
        
        #endregion
        
        #region Zoom Transitions
        
        private IEnumerator ZoomTransition(UIScreen fromScreen, UIScreen toScreen, bool zoomIn, float duration)
        {
            RectTransform fromRect = GetScreenRect(fromScreen);
            RectTransform toRect = GetScreenRect(toScreen);
            
            float halfDuration = duration * 0.5f;
            
            // Prepare groups
            var fromGroup = fromRect != null ? GetOrAddCanvasGroup(fromRect) : null;
            var toGroup = toRect != null ? GetOrAddCanvasGroup(toRect) : null;
            
            Vector3 normalScale = Vector3.one;
            Vector3 zoomedScale = zoomIn ? Vector3.one * 2f : Vector3.one * 0.5f;
            
            if (toRect != null)
            {
                toRect.localScale = zoomIn ? Vector3.one * 0.5f : Vector3.one * 2f;
                toRect.gameObject.SetActive(true);
                if (toGroup != null) toGroup.alpha = 0;
            }
            
            // Zoom out from screen and fade
            float elapsed = 0;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = transitionCurve.Evaluate(elapsed / halfDuration);
                
                if (fromRect != null)
                {
                    fromRect.localScale = Vector3.Lerp(normalScale, zoomedScale, t);
                }
                if (fromGroup != null) fromGroup.alpha = 1 - t;
                
                yield return null;
            }
            
            HideScreen(fromScreen);
            if (fromRect != null) fromRect.localScale = normalScale;
            if (fromGroup != null) fromGroup.alpha = 1;
            
            // Zoom in to new screen
            elapsed = 0;
            Vector3 toStartScale = toRect != null ? toRect.localScale : normalScale;
            
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = transitionCurve.Evaluate(elapsed / halfDuration);
                
                if (toRect != null)
                {
                    toRect.localScale = Vector3.Lerp(toStartScale, normalScale, t);
                }
                if (toGroup != null) toGroup.alpha = t;
                
                yield return null;
            }
            
            if (toRect != null) toRect.localScale = normalScale;
            if (toGroup != null) toGroup.alpha = 1;
        }
        
        #endregion
        
        #region Cinematic Transitions
        
        /// <summary>
        /// Play cinematic letterbox transition
        /// </summary>
        public void PlayLetterbox(float targetHeight = 0.15f, float duration = 0.5f, Action onComplete = null)
        {
            StartCoroutine(AnimateLetterbox(targetHeight, duration, onComplete));
        }
        
        /// <summary>
        /// Remove letterbox
        /// </summary>
        public void RemoveLetterbox(float duration = 0.5f, Action onComplete = null)
        {
            StartCoroutine(AnimateLetterbox(0, duration, onComplete));
        }
        
        private IEnumerator AnimateLetterbox(float targetHeight, float duration, Action onComplete)
        {
            // This would animate top/bottom black bars for cinematic effect
            // Implementation depends on having letterbox UI elements
            
            yield return new WaitForSecondsRealtime(duration);
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Flash screen white (damage/impact)
        /// </summary>
        public void FlashScreen(Color flashColor, float duration = 0.3f)
        {
            StartCoroutine(AnimateScreenFlash(flashColor, duration));
        }
        
        private IEnumerator AnimateScreenFlash(Color flashColor, float duration)
        {
            if (fadeOverlay == null) yield break;
            
            Color originalColor = fadeOverlay.color;
            fadeOverlay.color = flashColor;
            
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float alpha = Mathf.Lerp(1, 0, t * t); // Quadratic falloff
                fadeOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
                yield return null;
            }
            
            fadeOverlay.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);
        }
        
        /// <summary>
        /// Camera shake effect for UI
        /// </summary>
        public void ShakeScreen(float intensity = 10f, float duration = 0.3f)
        {
            StartCoroutine(AnimateScreenShake(intensity, duration));
        }
        
        private IEnumerator AnimateScreenShake(float intensity, float duration)
        {
            RectTransform canvas = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            if (canvas == null) yield break;
            
            Vector2 originalPos = canvas.anchoredPosition;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float dampening = 1 - progress;
                
                float x = UnityEngine.Random.Range(-1f, 1f) * intensity * dampening;
                float y = UnityEngine.Random.Range(-1f, 1f) * intensity * dampening;
                
                canvas.anchoredPosition = originalPos + new Vector2(x, y);
                yield return null;
            }
            
            canvas.anchoredPosition = originalPos;
        }
        
        #endregion
        
        #region Multi-Element Transitions
        
        /// <summary>
        /// Stagger animate multiple elements
        /// </summary>
        public void StaggeredShow(List<RectTransform> elements, float staggerDelay = 0.1f, 
            SlideDirection direction = SlideDirection.Up, Action onComplete = null)
        {
            StartCoroutine(AnimateStaggeredShow(elements, staggerDelay, direction, onComplete));
        }
        
        private IEnumerator AnimateStaggeredShow(List<RectTransform> elements, float staggerDelay,
            SlideDirection direction, Action onComplete)
        {
            foreach (var element in elements)
            {
                element.gameObject.SetActive(true);
                var group = GetOrAddCanvasGroup(element);
                group.alpha = 0;
            }
            
            Vector2 offset = GetDirectionOffset(direction) * 50f;
            
            foreach (var element in elements)
            {
                StartCoroutine(AnimateElementShow(element, offset, 0.3f));
                yield return new WaitForSecondsRealtime(staggerDelay);
            }
            
            // Wait for last animation to complete
            yield return new WaitForSecondsRealtime(0.3f);
            onComplete?.Invoke();
        }
        
        private IEnumerator AnimateElementShow(RectTransform element, Vector2 offset, float duration)
        {
            Vector2 targetPos = element.anchoredPosition;
            Vector2 startPos = targetPos + offset;
            element.anchoredPosition = startPos;
            
            var group = GetOrAddCanvasGroup(element);
            
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = transitionCurve.Evaluate(elapsed / duration);
                
                element.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                group.alpha = t;
                
                yield return null;
            }
            
            element.anchoredPosition = targetPos;
            group.alpha = 1;
        }
        
        /// <summary>
        /// Stagger hide multiple elements
        /// </summary>
        public void StaggeredHide(List<RectTransform> elements, float staggerDelay = 0.05f,
            Action onComplete = null)
        {
            StartCoroutine(AnimateStaggeredHide(elements, staggerDelay, onComplete));
        }
        
        private IEnumerator AnimateStaggeredHide(List<RectTransform> elements, float staggerDelay,
            Action onComplete)
        {
            foreach (var element in elements)
            {
                StartCoroutine(AnimateElementHide(element, 0.2f));
                yield return new WaitForSecondsRealtime(staggerDelay);
            }
            
            yield return new WaitForSecondsRealtime(0.2f);
            onComplete?.Invoke();
        }
        
        private IEnumerator AnimateElementHide(RectTransform element, float duration)
        {
            var group = GetOrAddCanvasGroup(element);
            Vector3 startScale = element.localScale;
            
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                element.localScale = Vector3.Lerp(startScale, startScale * 0.8f, t);
                group.alpha = 1 - t;
                
                yield return null;
            }
            
            element.localScale = startScale;
            group.alpha = 1;
            element.gameObject.SetActive(false);
        }
        
        private Vector2 GetDirectionOffset(SlideDirection direction)
        {
            switch (direction)
            {
                case SlideDirection.Up: return Vector2.down;
                case SlideDirection.Down: return Vector2.up;
                case SlideDirection.Left: return Vector2.right;
                case SlideDirection.Right: return Vector2.left;
                default: return Vector2.down;
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Simple fade to black
        /// </summary>
        public void FadeToBlack(float duration = 0.5f, Action onComplete = null)
        {
            StartCoroutine(FadeToBlackCoroutine(duration, onComplete));
        }
        
        private IEnumerator FadeToBlackCoroutine(float duration, Action onComplete)
        {
            yield return StartCoroutine(FadeOverlay(0, 1, duration));
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Fade from black
        /// </summary>
        public void FadeFromBlack(float duration = 0.5f, Action onComplete = null)
        {
            StartCoroutine(FadeFromBlackCoroutine(duration, onComplete));
        }
        
        private IEnumerator FadeFromBlackCoroutine(float duration, Action onComplete)
        {
            yield return StartCoroutine(FadeOverlay(1, 0, duration));
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Check if currently transitioning
        /// </summary>
        public bool IsTransitioning => _isTransitioning;
        
        /// <summary>
        /// Get current screen
        /// </summary>
        public UIScreen CurrentScreen => _currentScreen;
        
        /// <summary>
        /// Force set current screen without transition
        /// </summary>
        public void SetScreen(UIScreen screen)
        {
            if (_isTransitioning) return;
            
            HideScreen(_currentScreen);
            ShowScreen(screen);
            
            UIScreen previous = _currentScreen;
            _currentScreen = screen;
            OnScreenChanged?.Invoke(previous, screen);
        }
        
        #endregion
        
        #region Helpers
        
        private void ShowScreen(UIScreen screen)
        {
            if (_screens.TryGetValue(screen, out var rect) && rect != null)
            {
                rect.gameObject.SetActive(true);
            }
        }
        
        private void HideScreen(UIScreen screen)
        {
            if (_screens.TryGetValue(screen, out var rect) && rect != null)
            {
                rect.gameObject.SetActive(false);
            }
        }
        
        private RectTransform GetScreenRect(UIScreen screen)
        {
            if (_screens.TryGetValue(screen, out var rect))
            {
                return rect;
            }
            return null;
        }
        
        private CanvasGroup GetOrAddCanvasGroup(RectTransform target)
        {
            var group = target.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = target.gameObject.AddComponent<CanvasGroup>();
            }
            return group;
        }
        
        #endregion
    }
    
    #region Data Types
    
    [Serializable]
    public class UIScreenConfig
    {
        public UIScreen screen;
        public RectTransform screenRect;
    }
    
    public enum UIScreen
    {
        None,
        MainMenu,
        WorldMap,
        TerritoryView,
        CityBuilder,
        Combat,
        Inventory,
        Research,
        Diplomacy,
        Settings,
        Profile,
        Leaderboard,
        Store,
        Loading
    }
    
    public enum TransitionType
    {
        Instant,
        Fade,
        CrossFade,
        SlideLeft,
        SlideRight,
        SlideUp,
        SlideDown,
        CircleWipe,
        ZoomIn,
        ZoomOut
    }
    
    #endregion
}
