using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// UI Animation System for PC client.
    /// Provides polished, responsive UI animations:
    /// - Panel transitions (slide, fade, scale)
    /// - Button effects (hover, press, release)
    /// - Notification animations
    /// - Number counters and progress bars
    /// - Tooltip animations
    /// </summary>
    public class UIAnimationSystem : MonoBehaviour
    {
        [Header("Global Settings")]
        [SerializeField] private float defaultAnimationDuration = 0.3f;
        [SerializeField] private AnimationCurve defaultEasingCurve;
        [SerializeField] private bool enableAnimations = true;
        
        [Header("Panel Animations")]
        [SerializeField] private float panelSlideDuration = 0.4f;
        [SerializeField] private AnimationCurve panelEasing;
        [SerializeField] private float panelSlideDistance = 100f;
        
        [Header("Button Animations")]
        [SerializeField] private float buttonHoverScale = 1.05f;
        [SerializeField] private float buttonPressScale = 0.95f;
        [SerializeField] private float buttonAnimationSpeed = 8f;
        [SerializeField] private Color buttonHoverTint = new Color(1.1f, 1.1f, 1.1f, 1f);
        
        [Header("Notification Animations")]
        [SerializeField] private float notificationSlideDistance = 300f;
        [SerializeField] private float notificationShowDuration = 0.5f;
        [SerializeField] private float notificationHideDuration = 0.3f;
        
        [Header("Number Animations")]
        [SerializeField] private float numberCountDuration = 1f;
        [SerializeField] private AnimationCurve numberEasing;
        
        [Header("Tooltip Settings")]
        [SerializeField] private float tooltipDelay = 0.5f;
        [SerializeField] private float tooltipFadeDuration = 0.2f;
        [SerializeField] private Vector2 tooltipOffset = new Vector2(15, -15);
        
        // Singleton
        private static UIAnimationSystem _instance;
        public static UIAnimationSystem Instance => _instance;
        
        // Active animations tracking
        private Dictionary<int, Coroutine> _activeAnimations = new Dictionary<int, Coroutine>();
        
        // Preset easing curves
        public static class Easing
        {
            public static AnimationCurve EaseOutBack => AnimationCurve.EaseInOut(0, 0, 1, 1);
            public static AnimationCurve EaseOutElastic => CreateElasticCurve();
            public static AnimationCurve EaseOutBounce => CreateBounceCurve();
            public static AnimationCurve EaseInOutQuad => AnimationCurve.EaseInOut(0, 0, 1, 1);
            public static AnimationCurve Linear => AnimationCurve.Linear(0, 0, 1, 1);
            
            private static AnimationCurve CreateElasticCurve()
            {
                var curve = new AnimationCurve();
                curve.AddKey(0, 0);
                curve.AddKey(0.4f, 1.1f);
                curve.AddKey(0.6f, 0.95f);
                curve.AddKey(0.8f, 1.02f);
                curve.AddKey(1, 1);
                return curve;
            }
            
            private static AnimationCurve CreateBounceCurve()
            {
                var curve = new AnimationCurve();
                curve.AddKey(0, 0);
                curve.AddKey(0.36f, 1f);
                curve.AddKey(0.54f, 0.9f);
                curve.AddKey(0.74f, 1f);
                curve.AddKey(0.82f, 0.95f);
                curve.AddKey(1, 1);
                return curve;
            }
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeEasingCurves();
        }
        
        private void InitializeEasingCurves()
        {
            if (defaultEasingCurve == null || defaultEasingCurve.length == 0)
            {
                defaultEasingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
            
            if (panelEasing == null || panelEasing.length == 0)
            {
                panelEasing = Easing.EaseOutBack;
            }
            
            if (numberEasing == null || numberEasing.length == 0)
            {
                numberEasing = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
        }
        
        #region Panel Animations
        
        /// <summary>
        /// Show panel with slide animation
        /// </summary>
        public void ShowPanel(RectTransform panel, SlideDirection direction = SlideDirection.Up, 
            Action onComplete = null)
        {
            if (!enableAnimations)
            {
                panel.gameObject.SetActive(true);
                onComplete?.Invoke();
                return;
            }
            
            StopExistingAnimation(panel);
            
            var anim = StartCoroutine(AnimatePanelShow(panel, direction, onComplete));
            _activeAnimations[panel.GetInstanceID()] = anim;
        }
        
        /// <summary>
        /// Hide panel with slide animation
        /// </summary>
        public void HidePanel(RectTransform panel, SlideDirection direction = SlideDirection.Down, 
            Action onComplete = null)
        {
            if (!enableAnimations)
            {
                panel.gameObject.SetActive(false);
                onComplete?.Invoke();
                return;
            }
            
            StopExistingAnimation(panel);
            
            var anim = StartCoroutine(AnimatePanelHide(panel, direction, onComplete));
            _activeAnimations[panel.GetInstanceID()] = anim;
        }
        
        private IEnumerator AnimatePanelShow(RectTransform panel, SlideDirection direction, Action onComplete)
        {
            panel.gameObject.SetActive(true);
            
            Vector2 startOffset = GetSlideOffset(direction);
            Vector2 targetPos = panel.anchoredPosition;
            Vector2 startPos = targetPos + startOffset * panelSlideDistance;
            
            var canvasGroup = GetOrAddCanvasGroup(panel);
            canvasGroup.alpha = 0;
            panel.anchoredPosition = startPos;
            
            float elapsed = 0;
            while (elapsed < panelSlideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = panelEasing.Evaluate(elapsed / panelSlideDuration);
                
                panel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                canvasGroup.alpha = t;
                
                yield return null;
            }
            
            panel.anchoredPosition = targetPos;
            canvasGroup.alpha = 1;
            
            _activeAnimations.Remove(panel.GetInstanceID());
            onComplete?.Invoke();
        }
        
        private IEnumerator AnimatePanelHide(RectTransform panel, SlideDirection direction, Action onComplete)
        {
            Vector2 endOffset = GetSlideOffset(direction);
            Vector2 startPos = panel.anchoredPosition;
            Vector2 endPos = startPos + endOffset * panelSlideDistance;
            
            var canvasGroup = GetOrAddCanvasGroup(panel);
            
            float elapsed = 0;
            while (elapsed < panelSlideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = panelEasing.Evaluate(elapsed / panelSlideDuration);
                
                panel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                canvasGroup.alpha = 1 - t;
                
                yield return null;
            }
            
            panel.anchoredPosition = startPos; // Reset position
            canvasGroup.alpha = 1;
            panel.gameObject.SetActive(false);
            
            _activeAnimations.Remove(panel.GetInstanceID());
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Show panel with scale animation
        /// </summary>
        public void ShowPanelScale(RectTransform panel, Action onComplete = null)
        {
            if (!enableAnimations)
            {
                panel.gameObject.SetActive(true);
                onComplete?.Invoke();
                return;
            }
            
            StopExistingAnimation(panel);
            
            var anim = StartCoroutine(AnimatePanelScaleShow(panel, onComplete));
            _activeAnimations[panel.GetInstanceID()] = anim;
        }
        
        private IEnumerator AnimatePanelScaleShow(RectTransform panel, Action onComplete)
        {
            panel.gameObject.SetActive(true);
            
            var canvasGroup = GetOrAddCanvasGroup(panel);
            canvasGroup.alpha = 0;
            panel.localScale = Vector3.one * 0.8f;
            
            float elapsed = 0;
            while (elapsed < panelSlideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = panelEasing.Evaluate(elapsed / panelSlideDuration);
                
                panel.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
                canvasGroup.alpha = t;
                
                yield return null;
            }
            
            panel.localScale = Vector3.one;
            canvasGroup.alpha = 1;
            
            _activeAnimations.Remove(panel.GetInstanceID());
            onComplete?.Invoke();
        }
        
        private Vector2 GetSlideOffset(SlideDirection direction)
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
        
        #region Button Animations
        
        /// <summary>
        /// Setup automatic button animations
        /// </summary>
        public void SetupButtonAnimations(Button button)
        {
            var handler = button.gameObject.GetComponent<AnimatedButton>();
            if (handler == null)
            {
                handler = button.gameObject.AddComponent<AnimatedButton>();
            }
            
            handler.Initialize(this, buttonHoverScale, buttonPressScale, 
                buttonAnimationSpeed, buttonHoverTint);
        }
        
        /// <summary>
        /// Apply hover effect to button
        /// </summary>
        public void ButtonHover(RectTransform button, bool isHovered)
        {
            StopExistingAnimation(button);
            
            var anim = StartCoroutine(AnimateButtonScale(button, 
                isHovered ? buttonHoverScale : 1f));
            _activeAnimations[button.GetInstanceID()] = anim;
        }
        
        /// <summary>
        /// Apply press effect to button
        /// </summary>
        public void ButtonPress(RectTransform button)
        {
            StopExistingAnimation(button);
            
            var anim = StartCoroutine(AnimateButtonPressRelease(button));
            _activeAnimations[button.GetInstanceID()] = anim;
        }
        
        private IEnumerator AnimateButtonScale(RectTransform button, float targetScale)
        {
            Vector3 startScale = button.localScale;
            Vector3 endScale = Vector3.one * targetScale;
            
            float speed = buttonAnimationSpeed;
            while (Vector3.Distance(button.localScale, endScale) > 0.01f)
            {
                button.localScale = Vector3.Lerp(button.localScale, endScale, Time.unscaledDeltaTime * speed);
                yield return null;
            }
            
            button.localScale = endScale;
            _activeAnimations.Remove(button.GetInstanceID());
        }
        
        private IEnumerator AnimateButtonPressRelease(RectTransform button)
        {
            // Quick press
            Vector3 pressScale = Vector3.one * buttonPressScale;
            
            float elapsed = 0;
            float pressDuration = 0.1f;
            Vector3 startScale = button.localScale;
            
            while (elapsed < pressDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / pressDuration;
                button.localScale = Vector3.Lerp(startScale, pressScale, t);
                yield return null;
            }
            
            // Release with overshoot
            elapsed = 0;
            float releaseDuration = 0.2f;
            Vector3 releaseScale = Vector3.one * (buttonHoverScale * 1.1f);
            
            while (elapsed < releaseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Easing.EaseOutBack.Evaluate(elapsed / releaseDuration);
                button.localScale = Vector3.Lerp(pressScale, releaseScale, t);
                yield return null;
            }
            
            // Settle to hover
            button.localScale = Vector3.one * buttonHoverScale;
            _activeAnimations.Remove(button.GetInstanceID());
        }
        
        #endregion
        
        #region Notification Animations
        
        /// <summary>
        /// Show notification with slide in animation
        /// </summary>
        public void ShowNotification(RectTransform notification, UINotificationPosition position = UINotificationPosition.TopRight,
            Action onComplete = null)
        {
            if (!enableAnimations)
            {
                notification.gameObject.SetActive(true);
                onComplete?.Invoke();
                return;
            }
            
            StopExistingAnimation(notification);
            
            var anim = StartCoroutine(AnimateNotificationShow(notification, position, onComplete));
            _activeAnimations[notification.GetInstanceID()] = anim;
        }
        
        /// <summary>
        /// Hide notification with fade/slide animation
        /// </summary>
        public void HideNotification(RectTransform notification, Action onComplete = null)
        {
            if (!enableAnimations)
            {
                notification.gameObject.SetActive(false);
                onComplete?.Invoke();
                return;
            }
            
            StopExistingAnimation(notification);
            
            var anim = StartCoroutine(AnimateNotificationHide(notification, onComplete));
            _activeAnimations[notification.GetInstanceID()] = anim;
        }
        
        private IEnumerator AnimateNotificationShow(RectTransform notification, UINotificationPosition position, Action onComplete)
        {
            notification.gameObject.SetActive(true);
            
            Vector2 startPos = notification.anchoredPosition;
            Vector2 offset = GetNotificationOffset(position);
            Vector2 hidePos = startPos + offset * notificationSlideDistance;
            
            notification.anchoredPosition = hidePos;
            var canvasGroup = GetOrAddCanvasGroup(notification);
            canvasGroup.alpha = 0;
            
            // Slide in with bounce
            float elapsed = 0;
            var curve = Easing.EaseOutBack;
            
            while (elapsed < notificationShowDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = curve.Evaluate(elapsed / notificationShowDuration);
                
                notification.anchoredPosition = Vector2.Lerp(hidePos, startPos, t);
                canvasGroup.alpha = Mathf.Clamp01(t * 2); // Fade in faster
                
                yield return null;
            }
            
            notification.anchoredPosition = startPos;
            canvasGroup.alpha = 1;
            
            _activeAnimations.Remove(notification.GetInstanceID());
            onComplete?.Invoke();
        }
        
        private IEnumerator AnimateNotificationHide(RectTransform notification, Action onComplete)
        {
            Vector2 startPos = notification.anchoredPosition;
            var canvasGroup = GetOrAddCanvasGroup(notification);
            
            // Fade out and slight movement
            float elapsed = 0;
            while (elapsed < notificationHideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / notificationHideDuration;
                
                canvasGroup.alpha = 1 - t;
                notification.anchoredPosition = startPos + Vector2.right * t * 50f;
                
                yield return null;
            }
            
            notification.anchoredPosition = startPos;
            canvasGroup.alpha = 1;
            notification.gameObject.SetActive(false);
            
            _activeAnimations.Remove(notification.GetInstanceID());
            onComplete?.Invoke();
        }
        
        private Vector2 GetNotificationOffset(UINotificationPosition position)
        {
            switch (position)
            {
                case UINotificationPosition.TopLeft: return new Vector2(-1, 0);
                case UINotificationPosition.TopRight: return new Vector2(1, 0);
                case UINotificationPosition.BottomLeft: return new Vector2(-1, 0);
                case UINotificationPosition.BottomRight: return new Vector2(1, 0);
                case UINotificationPosition.Top: return new Vector2(0, 1);
                case UINotificationPosition.Bottom: return new Vector2(0, -1);
                default: return new Vector2(1, 0);
            }
        }
        
        /// <summary>
        /// Flash notification for attention
        /// </summary>
        public void FlashNotification(RectTransform notification, int flashCount = 3, Color? flashColor = null)
        {
            StartCoroutine(AnimateNotificationFlash(notification, flashCount, flashColor ?? Color.white));
        }
        
        private IEnumerator AnimateNotificationFlash(RectTransform notification, int flashCount, Color flashColor)
        {
            var image = notification.GetComponent<Image>();
            if (image == null) yield break;
            
            Color originalColor = image.color;
            float flashDuration = 0.15f;
            
            for (int i = 0; i < flashCount; i++)
            {
                // Flash on
                float elapsed = 0;
                while (elapsed < flashDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / flashDuration;
                    image.color = Color.Lerp(originalColor, flashColor, t);
                    yield return null;
                }
                
                // Flash off
                elapsed = 0;
                while (elapsed < flashDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / flashDuration;
                    image.color = Color.Lerp(flashColor, originalColor, t);
                    yield return null;
                }
            }
            
            image.color = originalColor;
        }
        
        #endregion
        
        #region Number Animations
        
        /// <summary>
        /// Animate number counting up/down
        /// </summary>
        public void AnimateNumber(Text textComponent, float startValue, float endValue, 
            string format = "N0", Action onComplete = null)
        {
            StopExistingAnimation(textComponent.rectTransform);
            
            var anim = StartCoroutine(AnimateNumberCount(textComponent, startValue, endValue, format, onComplete));
            _activeAnimations[textComponent.GetInstanceID()] = anim;
        }
        
        /// <summary>
        /// Animate number with TMPro
        /// </summary>
        public void AnimateNumber(TMPro.TextMeshProUGUI textComponent, float startValue, float endValue,
            string format = "N0", Action onComplete = null)
        {
            StopExistingAnimation(textComponent.rectTransform);
            
            var anim = StartCoroutine(AnimateNumberCountTMP(textComponent, startValue, endValue, format, onComplete));
            _activeAnimations[textComponent.GetInstanceID()] = anim;
        }
        
        private IEnumerator AnimateNumberCount(Text textComponent, float startValue, float endValue,
            string format, Action onComplete)
        {
            float elapsed = 0;
            
            while (elapsed < numberCountDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = numberEasing.Evaluate(elapsed / numberCountDuration);
                
                float currentValue = Mathf.Lerp(startValue, endValue, t);
                textComponent.text = currentValue.ToString(format);
                
                yield return null;
            }
            
            textComponent.text = endValue.ToString(format);
            _activeAnimations.Remove(textComponent.GetInstanceID());
            onComplete?.Invoke();
        }
        
        private IEnumerator AnimateNumberCountTMP(TMPro.TextMeshProUGUI textComponent, float startValue, 
            float endValue, string format, Action onComplete)
        {
            float elapsed = 0;
            
            // Determine if going up or down
            bool increasing = endValue > startValue;
            Color positiveColor = new Color(0.3f, 0.8f, 0.3f);
            Color negativeColor = new Color(0.8f, 0.3f, 0.3f);
            Color originalColor = textComponent.color;
            
            while (elapsed < numberCountDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = numberEasing.Evaluate(elapsed / numberCountDuration);
                
                float currentValue = Mathf.Lerp(startValue, endValue, t);
                textComponent.text = currentValue.ToString(format);
                
                // Color pulse based on direction
                if (t < 0.5f)
                {
                    Color targetColor = increasing ? positiveColor : negativeColor;
                    textComponent.color = Color.Lerp(originalColor, targetColor, t * 2);
                }
                else
                {
                    Color targetColor = increasing ? positiveColor : negativeColor;
                    textComponent.color = Color.Lerp(targetColor, originalColor, (t - 0.5f) * 2);
                }
                
                yield return null;
            }
            
            textComponent.text = endValue.ToString(format);
            textComponent.color = originalColor;
            _activeAnimations.Remove(textComponent.GetInstanceID());
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Animate number with pop effect
        /// </summary>
        public void AnimateNumberPop(RectTransform numberContainer, float scalePop = 1.3f)
        {
            StartCoroutine(AnimateNumberPopEffect(numberContainer, scalePop));
        }
        
        private IEnumerator AnimateNumberPopEffect(RectTransform container, float scalePop)
        {
            Vector3 originalScale = container.localScale;
            Vector3 popScale = originalScale * scalePop;
            
            // Pop up
            float elapsed = 0;
            float popDuration = 0.15f;
            
            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / popDuration;
                container.localScale = Vector3.Lerp(originalScale, popScale, t);
                yield return null;
            }
            
            // Settle back with bounce
            elapsed = 0;
            float settleDuration = 0.25f;
            var curve = Easing.EaseOutElastic;
            
            while (elapsed < settleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = curve.Evaluate(elapsed / settleDuration);
                container.localScale = Vector3.Lerp(popScale, originalScale, t);
                yield return null;
            }
            
            container.localScale = originalScale;
        }
        
        #endregion
        
        #region Progress Bar Animations
        
        /// <summary>
        /// Animate progress bar fill
        /// </summary>
        public void AnimateProgressBar(Image fillImage, float targetFill, float duration = 0.5f,
            Action onComplete = null)
        {
            StopExistingAnimation(fillImage.rectTransform);
            
            var anim = StartCoroutine(AnimateProgressFill(fillImage, targetFill, duration, onComplete));
            _activeAnimations[fillImage.GetInstanceID()] = anim;
        }
        
        private IEnumerator AnimateProgressFill(Image fillImage, float targetFill, float duration, Action onComplete)
        {
            float startFill = fillImage.fillAmount;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = defaultEasingCurve.Evaluate(elapsed / duration);
                fillImage.fillAmount = Mathf.Lerp(startFill, targetFill, t);
                yield return null;
            }
            
            fillImage.fillAmount = targetFill;
            _activeAnimations.Remove(fillImage.GetInstanceID());
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Animate progress bar with glow pulse on complete
        /// </summary>
        public void AnimateProgressBarComplete(Image fillImage, Image glowImage)
        {
            StartCoroutine(AnimateProgressComplete(fillImage, glowImage));
        }
        
        private IEnumerator AnimateProgressComplete(Image fillImage, Image glowImage)
        {
            // Flash the fill
            Color originalColor = fillImage.color;
            Color flashColor = Color.white;
            
            fillImage.color = flashColor;
            if (glowImage != null)
            {
                glowImage.gameObject.SetActive(true);
                glowImage.color = new Color(1, 1, 1, 0);
            }
            
            float elapsed = 0;
            float duration = 0.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                fillImage.color = Color.Lerp(flashColor, originalColor, t);
                
                if (glowImage != null)
                {
                    float glowAlpha = Mathf.Sin(t * Mathf.PI);
                    glowImage.color = new Color(1, 1, 1, glowAlpha * 0.5f);
                }
                
                yield return null;
            }
            
            fillImage.color = originalColor;
            if (glowImage != null)
            {
                glowImage.gameObject.SetActive(false);
            }
        }
        
        #endregion
        
        #region Tooltip Animations
        
        /// <summary>
        /// Show tooltip with fade and position animation
        /// </summary>
        public void ShowTooltip(RectTransform tooltip, Vector2 position, Action onComplete = null)
        {
            StopExistingAnimation(tooltip);
            
            var anim = StartCoroutine(AnimateTooltipShow(tooltip, position, onComplete));
            _activeAnimations[tooltip.GetInstanceID()] = anim;
        }
        
        /// <summary>
        /// Hide tooltip
        /// </summary>
        public void HideTooltip(RectTransform tooltip, Action onComplete = null)
        {
            if (!tooltip.gameObject.activeInHierarchy)
            {
                onComplete?.Invoke();
                return;
            }
            
            StopExistingAnimation(tooltip);
            
            var anim = StartCoroutine(AnimateTooltipHide(tooltip, onComplete));
            _activeAnimations[tooltip.GetInstanceID()] = anim;
        }
        
        private IEnumerator AnimateTooltipShow(RectTransform tooltip, Vector2 position, Action onComplete)
        {
            // Delay before showing
            yield return new WaitForSecondsRealtime(tooltipDelay);
            
            tooltip.gameObject.SetActive(true);
            tooltip.anchoredPosition = position + tooltipOffset;
            
            var canvasGroup = GetOrAddCanvasGroup(tooltip);
            canvasGroup.alpha = 0;
            
            // Slight upward movement while fading in
            Vector2 startPos = tooltip.anchoredPosition - Vector2.up * 10f;
            Vector2 endPos = tooltip.anchoredPosition;
            
            float elapsed = 0;
            while (elapsed < tooltipFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / tooltipFadeDuration;
                
                canvasGroup.alpha = t;
                tooltip.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                
                yield return null;
            }
            
            canvasGroup.alpha = 1;
            tooltip.anchoredPosition = endPos;
            
            _activeAnimations.Remove(tooltip.GetInstanceID());
            onComplete?.Invoke();
        }
        
        private IEnumerator AnimateTooltipHide(RectTransform tooltip, Action onComplete)
        {
            var canvasGroup = GetOrAddCanvasGroup(tooltip);
            
            float elapsed = 0;
            while (elapsed < tooltipFadeDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / (tooltipFadeDuration * 0.5f);
                canvasGroup.alpha = 1 - t;
                yield return null;
            }
            
            canvasGroup.alpha = 1;
            tooltip.gameObject.SetActive(false);
            
            _activeAnimations.Remove(tooltip.GetInstanceID());
            onComplete?.Invoke();
        }
        
        #endregion
        
        #region Utility Animations
        
        /// <summary>
        /// Generic fade animation
        /// </summary>
        public void Fade(RectTransform target, float targetAlpha, float duration, Action onComplete = null)
        {
            StopExistingAnimation(target);
            
            var anim = StartCoroutine(AnimateFade(target, targetAlpha, duration, onComplete));
            _activeAnimations[target.GetInstanceID()] = anim;
        }
        
        private IEnumerator AnimateFade(RectTransform target, float targetAlpha, float duration, Action onComplete)
        {
            var canvasGroup = GetOrAddCanvasGroup(target);
            float startAlpha = canvasGroup.alpha;
            
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }
            
            canvasGroup.alpha = targetAlpha;
            _activeAnimations.Remove(target.GetInstanceID());
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Shake animation for errors/warnings
        /// </summary>
        public void Shake(RectTransform target, float intensity = 10f, float duration = 0.3f)
        {
            StartCoroutine(AnimateShake(target, intensity, duration));
        }
        
        private IEnumerator AnimateShake(RectTransform target, float intensity, float duration)
        {
            Vector2 originalPos = target.anchoredPosition;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float dampening = 1 - progress;
                
                float x = UnityEngine.Random.Range(-1f, 1f) * intensity * dampening;
                float y = UnityEngine.Random.Range(-1f, 1f) * intensity * dampening * 0.5f;
                
                target.anchoredPosition = originalPos + new Vector2(x, y);
                yield return null;
            }
            
            target.anchoredPosition = originalPos;
        }
        
        /// <summary>
        /// Pulse scale animation
        /// </summary>
        public void Pulse(RectTransform target, float pulseScale = 1.1f, int pulseCount = 2)
        {
            StartCoroutine(AnimatePulse(target, pulseScale, pulseCount));
        }
        
        private IEnumerator AnimatePulse(RectTransform target, float pulseScale, int pulseCount)
        {
            Vector3 originalScale = target.localScale;
            Vector3 targetScale = originalScale * pulseScale;
            float pulseDuration = 0.2f;
            
            for (int i = 0; i < pulseCount; i++)
            {
                // Scale up
                float elapsed = 0;
                while (elapsed < pulseDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / pulseDuration;
                    target.localScale = Vector3.Lerp(originalScale, targetScale, t);
                    yield return null;
                }
                
                // Scale down
                elapsed = 0;
                while (elapsed < pulseDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / pulseDuration;
                    target.localScale = Vector3.Lerp(targetScale, originalScale, t);
                    yield return null;
                }
            }
            
            target.localScale = originalScale;
        }
        
        #endregion
        
        #region Helpers
        
        private void StopExistingAnimation(Component target)
        {
            int id = target.GetInstanceID();
            if (_activeAnimations.TryGetValue(id, out var existing))
            {
                if (existing != null)
                {
                    StopCoroutine(existing);
                }
                _activeAnimations.Remove(id);
            }
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
    
    #region Helper Components
    
    /// <summary>
    /// Automatically animates button interactions
    /// </summary>
    public class AnimatedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, 
        IPointerDownHandler, IPointerUpHandler
    {
        private UIAnimationSystem _system;
        private RectTransform _rectTransform;
        private float _hoverScale;
        private float _pressScale;
        private float _animSpeed;
        private Color _hoverTint;
        private Image _image;
        private Color _originalColor;
        private bool _isHovered;
        private bool _isPressed;
        
        public void Initialize(UIAnimationSystem system, float hoverScale, float pressScale, 
            float animSpeed, Color hoverTint)
        {
            _system = system;
            _rectTransform = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            _hoverScale = hoverScale;
            _pressScale = pressScale;
            _animSpeed = animSpeed;
            _hoverTint = hoverTint;
            
            if (_image != null)
            {
                _originalColor = _image.color;
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            if (!_isPressed && _system != null)
            {
                _system.ButtonHover(_rectTransform, true);
            }
            
            if (_image != null)
            {
                _image.color = _originalColor * _hoverTint;
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            if (!_isPressed && _system != null)
            {
                _system.ButtonHover(_rectTransform, false);
            }
            
            if (_image != null)
            {
                _image.color = _originalColor;
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            if (_system != null)
            {
                _system.ButtonPress(_rectTransform);
            }
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            if (_isHovered && _system != null)
            {
                _system.ButtonHover(_rectTransform, true);
            }
        }
    }
    
    #endregion
    
    #region Enums
    
    public enum SlideDirection
    {
        Up,
        Down,
        Left,
        Right
    }
    
    public enum UINotificationPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Top,
        Bottom
    }
    
    #endregion
}
