using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ApexCitadels.UI
{
    /// <summary>
    /// Reusable UI animation utilities for Apex Citadels.
    /// Provides fade, slide, scale, and bounce animations for UI elements.
    /// 
    /// Usage:
    ///   UIAnimator.FadeIn(panel);
    ///   UIAnimator.SlideIn(panel, SlideDirection.Left);
    ///   UIAnimator.ScaleBounce(button);
    ///   UIAnimator.Shake(errorPanel);
    ///   
    /// Or with callbacks:
    ///   UIAnimator.FadeOut(panel, onComplete: () => panel.SetActive(false));
    /// </summary>
    public class UIAnimator : MonoBehaviour
    {
        public static UIAnimator Instance { get; private set; }

        #region Enums

        public enum SlideDirection
        {
            Left,
            Right,
            Top,
            Bottom
        }

        public enum EaseType
        {
            Linear,
            EaseIn,
            EaseOut,
            EaseInOut,
            Bounce,
            Elastic,
            Back,
            Overshoot
        }

        #endregion

        #region Serialized Fields

        [Header("Default Settings")]
        [SerializeField] private float defaultDuration = 0.3f;
        [SerializeField] private EaseType defaultEase = EaseType.EaseOut;

        [Header("Button Press")]
        [SerializeField] private float buttonPressScale = 0.92f;
        [SerializeField] private float buttonPressDuration = 0.1f;

        [Header("Bounce")]
        [SerializeField] private float bounceOvershoot = 1.2f;
        [SerializeField] private float bounceDuration = 0.4f;

        [Header("Shake")]
        [SerializeField] private float shakeIntensity = 10f;
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private int shakeCount = 10;

        #endregion

        #region Private Fields

        private Dictionary<int, Coroutine> _activeAnimations = new Dictionary<int, Coroutine>();

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Static Methods - Fade

        /// <summary>
        /// Fade a UI element in (alpha 0 to 1)
        /// </summary>
        public static Coroutine FadeIn(GameObject target, float duration = -1, EaseType ease = EaseType.EaseOut, Action onComplete = null)
        {
            EnsureInstance();
            return Instance.StartFadeAnimation(target, 0f, 1f, duration, ease, onComplete, true);
        }

        /// <summary>
        /// Fade a UI element out (alpha 1 to 0)
        /// </summary>
        public static Coroutine FadeOut(GameObject target, float duration = -1, EaseType ease = EaseType.EaseIn, Action onComplete = null)
        {
            EnsureInstance();
            return Instance.StartFadeAnimation(target, 1f, 0f, duration, ease, onComplete, false);
        }

        /// <summary>
        /// Fade a CanvasGroup to a specific alpha
        /// </summary>
        public static Coroutine FadeTo(GameObject target, float targetAlpha, float duration = -1, EaseType ease = EaseType.EaseOut, Action onComplete = null)
        {
            EnsureInstance();
            var cg = target.GetComponent<CanvasGroup>();
            float startAlpha = cg != null ? cg.alpha : 1f;
            return Instance.StartFadeAnimation(target, startAlpha, targetAlpha, duration, ease, onComplete, targetAlpha > 0.5f);
        }

        #endregion

        #region Static Methods - Slide

        /// <summary>
        /// Slide a UI element in from a direction
        /// </summary>
        public static Coroutine SlideIn(GameObject target, SlideDirection direction = SlideDirection.Bottom, float duration = -1, EaseType ease = EaseType.EaseOut, Action onComplete = null)
        {
            EnsureInstance();
            return Instance.StartSlideAnimation(target, direction, true, duration, ease, onComplete);
        }

        /// <summary>
        /// Slide a UI element out to a direction
        /// </summary>
        public static Coroutine SlideOut(GameObject target, SlideDirection direction = SlideDirection.Bottom, float duration = -1, EaseType ease = EaseType.EaseIn, Action onComplete = null)
        {
            EnsureInstance();
            return Instance.StartSlideAnimation(target, direction, false, duration, ease, onComplete);
        }

        #endregion

        #region Static Methods - Scale

        /// <summary>
        /// Scale a UI element with bounce effect
        /// </summary>
        public static Coroutine ScaleBounce(GameObject target, float duration = -1, Action onComplete = null)
        {
            EnsureInstance();
            return Instance.StartScaleBounceAnimation(target, duration, onComplete);
        }

        /// <summary>
        /// Scale a UI element from 0 to 1 (pop in)
        /// </summary>
        public static Coroutine ScaleIn(GameObject target, float duration = -1, EaseType ease = EaseType.Back, Action onComplete = null)
        {
            EnsureInstance();
            return Instance.StartScaleAnimation(target, Vector3.zero, Vector3.one, duration, ease, onComplete);
        }

        /// <summary>
        /// Scale a UI element from 1 to 0 (pop out)
        /// </summary>
        public static Coroutine ScaleOut(GameObject target, float duration = -1, EaseType ease = EaseType.EaseIn, Action onComplete = null)
        {
            EnsureInstance();
            return Instance.StartScaleAnimation(target, Vector3.one, Vector3.zero, duration, ease, onComplete);
        }

        /// <summary>
        /// Quick button press animation
        /// </summary>
        public static Coroutine ButtonPress(GameObject target, Action onComplete = null)
        {
            EnsureInstance();
            return Instance.StartButtonPressAnimation(target, onComplete);
        }

        #endregion

        #region Static Methods - Shake

        /// <summary>
        /// Shake a UI element (for errors/invalid input)
        /// </summary>
        public static Coroutine Shake(GameObject target, float intensity = -1, float duration = -1, Action onComplete = null)
        {
            EnsureInstance();
            return Instance.StartShakeAnimation(target, intensity, duration, onComplete);
        }

        #endregion

        #region Static Methods - Pulse

        /// <summary>
        /// Pulse a UI element (scale up and down continuously)
        /// </summary>
        public static Coroutine PulseStart(GameObject target, float minScale = 0.95f, float maxScale = 1.05f, float duration = 1f)
        {
            EnsureInstance();
            return Instance.StartPulseAnimation(target, minScale, maxScale, duration);
        }

        /// <summary>
        /// Stop pulsing animation
        /// </summary>
        public static void PulseStop(GameObject target)
        {
            EnsureInstance();
            Instance.StopAnimationForTarget(target);
            target.transform.localScale = Vector3.one;
        }

        #endregion

        #region Static Methods - Combined

        /// <summary>
        /// Fade and slide in together
        /// </summary>
        public static void FadeSlideIn(GameObject target, SlideDirection direction = SlideDirection.Bottom, float duration = -1, EaseType ease = EaseType.EaseOut, Action onComplete = null)
        {
            FadeIn(target, duration, ease);
            SlideIn(target, direction, duration, ease, onComplete);
        }

        /// <summary>
        /// Fade and slide out together
        /// </summary>
        public static void FadeSlideOut(GameObject target, SlideDirection direction = SlideDirection.Bottom, float duration = -1, EaseType ease = EaseType.EaseIn, Action onComplete = null)
        {
            FadeOut(target, duration, ease);
            SlideOut(target, direction, duration, ease, onComplete);
        }

        /// <summary>
        /// Pop in with scale and fade
        /// </summary>
        public static void PopIn(GameObject target, float duration = -1, Action onComplete = null)
        {
            target.SetActive(true);
            FadeIn(target, duration);
            ScaleIn(target, duration, EaseType.Back, onComplete);
        }

        /// <summary>
        /// Pop out with scale and fade
        /// </summary>
        public static void PopOut(GameObject target, float duration = -1, Action onComplete = null)
        {
            FadeOut(target, duration);
            ScaleOut(target, duration, EaseType.EaseIn, () =>
            {
                target.SetActive(false);
                onComplete?.Invoke();
            });
        }

        #endregion

        #region Static Methods - Utility

        /// <summary>
        /// Stop all animations on a target
        /// </summary>
        public static void StopAll(GameObject target)
        {
            Instance?.StopAnimationForTarget(target);
        }

        /// <summary>
        /// Check if target has active animation
        /// </summary>
        public static bool IsAnimating(GameObject target)
        {
            if (Instance == null) return false;
            return Instance._activeAnimations.ContainsKey(target.GetInstanceID());
        }

        #endregion

        #region Instance Animation Methods

        private Coroutine StartFadeAnimation(GameObject target, float startAlpha, float endAlpha, float duration, EaseType ease, Action onComplete, bool enableOnStart)
        {
            StopAnimationForTarget(target);

            if (duration < 0) duration = defaultDuration;

            var coroutine = StartCoroutine(FadeCoroutine(target, startAlpha, endAlpha, duration, ease, onComplete, enableOnStart));
            _activeAnimations[target.GetInstanceID()] = coroutine;
            return coroutine;
        }

        private IEnumerator FadeCoroutine(GameObject target, float startAlpha, float endAlpha, float duration, EaseType ease, Action onComplete, bool enableOnStart)
        {
            if (target == null) yield break;

            if (enableOnStart)
                target.SetActive(true);

            // Get or add CanvasGroup
            var cg = target.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = target.AddComponent<CanvasGroup>();

            cg.alpha = startAlpha;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = ApplyEase(t, ease);
                cg.alpha = Mathf.Lerp(startAlpha, endAlpha, easedT);
                yield return null;
            }

            cg.alpha = endAlpha;
            _activeAnimations.Remove(target.GetInstanceID());

            onComplete?.Invoke();
        }

        private Coroutine StartSlideAnimation(GameObject target, SlideDirection direction, bool slideIn, float duration, EaseType ease, Action onComplete)
        {
            StopAnimationForTarget(target);

            if (duration < 0) duration = defaultDuration;

            var coroutine = StartCoroutine(SlideCoroutine(target, direction, slideIn, duration, ease, onComplete));
            _activeAnimations[target.GetInstanceID()] = coroutine;
            return coroutine;
        }

        private IEnumerator SlideCoroutine(GameObject target, SlideDirection direction, bool slideIn, float duration, EaseType ease, Action onComplete)
        {
            if (target == null) yield break;

            var rect = target.GetComponent<RectTransform>();
            if (rect == null) yield break;

            target.SetActive(true);

            Vector2 originalPos = rect.anchoredPosition;
            Vector2 offset = GetSlideOffset(rect, direction);

            Vector2 startPos = slideIn ? originalPos + offset : originalPos;
            Vector2 endPos = slideIn ? originalPos : originalPos + offset;

            rect.anchoredPosition = startPos;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = ApplyEase(t, ease);
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);
                yield return null;
            }

            rect.anchoredPosition = endPos;
            _activeAnimations.Remove(target.GetInstanceID());

            onComplete?.Invoke();
        }

        private Vector2 GetSlideOffset(RectTransform rect, SlideDirection direction)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            float screenWidth = canvas != null ? ((RectTransform)canvas.transform).rect.width : Screen.width;
            float screenHeight = canvas != null ? ((RectTransform)canvas.transform).rect.height : Screen.height;

            return direction switch
            {
                SlideDirection.Left => new Vector2(-screenWidth, 0),
                SlideDirection.Right => new Vector2(screenWidth, 0),
                SlideDirection.Top => new Vector2(0, screenHeight),
                SlideDirection.Bottom => new Vector2(0, -screenHeight),
                _ => Vector2.zero
            };
        }

        private Coroutine StartScaleBounceAnimation(GameObject target, float duration, Action onComplete)
        {
            StopAnimationForTarget(target);

            if (duration < 0) duration = bounceDuration;

            var coroutine = StartCoroutine(ScaleBounceCoroutine(target, duration, onComplete));
            _activeAnimations[target.GetInstanceID()] = coroutine;
            return coroutine;
        }

        private IEnumerator ScaleBounceCoroutine(GameObject target, float duration, Action onComplete)
        {
            if (target == null) yield break;

            target.SetActive(true);

            Vector3 originalScale = target.transform.localScale;
            Vector3 overshootScale = originalScale * bounceOvershoot;

            // First half: scale up to overshoot
            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                if (target == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float easedT = ApplyEase(t, EaseType.EaseOut);
                target.transform.localScale = Vector3.Lerp(Vector3.zero, overshootScale, easedT);
                yield return null;
            }

            // Second half: settle to original
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                if (target == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float easedT = ApplyEase(t, EaseType.Bounce);
                target.transform.localScale = Vector3.Lerp(overshootScale, originalScale, easedT);
                yield return null;
            }

            target.transform.localScale = originalScale;
            _activeAnimations.Remove(target.GetInstanceID());

            onComplete?.Invoke();
        }

        private Coroutine StartScaleAnimation(GameObject target, Vector3 startScale, Vector3 endScale, float duration, EaseType ease, Action onComplete)
        {
            StopAnimationForTarget(target);

            if (duration < 0) duration = defaultDuration;

            var coroutine = StartCoroutine(ScaleCoroutine(target, startScale, endScale, duration, ease, onComplete));
            _activeAnimations[target.GetInstanceID()] = coroutine;
            return coroutine;
        }

        private IEnumerator ScaleCoroutine(GameObject target, Vector3 startScale, Vector3 endScale, float duration, EaseType ease, Action onComplete)
        {
            if (target == null) yield break;

            target.transform.localScale = startScale;
            target.SetActive(true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = ApplyEase(t, ease);
                target.transform.localScale = Vector3.Lerp(startScale, endScale, easedT);
                yield return null;
            }

            target.transform.localScale = endScale;
            _activeAnimations.Remove(target.GetInstanceID());

            onComplete?.Invoke();
        }

        private Coroutine StartButtonPressAnimation(GameObject target, Action onComplete)
        {
            StopAnimationForTarget(target);

            var coroutine = StartCoroutine(ButtonPressCoroutine(target, onComplete));
            _activeAnimations[target.GetInstanceID()] = coroutine;
            return coroutine;
        }

        private IEnumerator ButtonPressCoroutine(GameObject target, Action onComplete)
        {
            if (target == null) yield break;

            Vector3 originalScale = target.transform.localScale;
            Vector3 pressedScale = originalScale * buttonPressScale;

            // Press down
            float elapsed = 0f;
            while (elapsed < buttonPressDuration)
            {
                if (target == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / buttonPressDuration);
                target.transform.localScale = Vector3.Lerp(originalScale, pressedScale, t);
                yield return null;
            }

            // Release
            elapsed = 0f;
            while (elapsed < buttonPressDuration)
            {
                if (target == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / buttonPressDuration);
                target.transform.localScale = Vector3.Lerp(pressedScale, originalScale, t);
                yield return null;
            }

            target.transform.localScale = originalScale;
            _activeAnimations.Remove(target.GetInstanceID());

            onComplete?.Invoke();
        }

        private Coroutine StartShakeAnimation(GameObject target, float intensity, float duration, Action onComplete)
        {
            StopAnimationForTarget(target);

            if (intensity < 0) intensity = shakeIntensity;
            if (duration < 0) duration = shakeDuration;

            var coroutine = StartCoroutine(ShakeCoroutine(target, intensity, duration, onComplete));
            _activeAnimations[target.GetInstanceID()] = coroutine;
            return coroutine;
        }

        private IEnumerator ShakeCoroutine(GameObject target, float intensity, float duration, Action onComplete)
        {
            if (target == null) yield break;

            var rect = target.GetComponent<RectTransform>();
            Vector2 originalPos = rect != null ? rect.anchoredPosition : (Vector2)target.transform.localPosition;

            float elapsed = 0f;
            float interval = duration / shakeCount;

            while (elapsed < duration)
            {
                if (target == null) yield break;

                float progress = elapsed / duration;
                float currentIntensity = intensity * (1f - progress); // Decay

                Vector2 offset = new Vector2(
                    UnityEngine.Random.Range(-currentIntensity, currentIntensity),
                    UnityEngine.Random.Range(-currentIntensity, currentIntensity)
                );

                if (rect != null)
                    rect.anchoredPosition = originalPos + offset;
                else
                    target.transform.localPosition = (Vector3)(originalPos + offset);

                elapsed += interval;
                yield return new WaitForSecondsRealtime(interval);
            }

            // Reset position
            if (rect != null)
                rect.anchoredPosition = originalPos;
            else
                target.transform.localPosition = (Vector3)originalPos;

            _activeAnimations.Remove(target.GetInstanceID());

            onComplete?.Invoke();
        }

        private Coroutine StartPulseAnimation(GameObject target, float minScale, float maxScale, float duration)
        {
            StopAnimationForTarget(target);

            var coroutine = StartCoroutine(PulseCoroutine(target, minScale, maxScale, duration));
            _activeAnimations[target.GetInstanceID()] = coroutine;
            return coroutine;
        }

        private IEnumerator PulseCoroutine(GameObject target, float minScale, float maxScale, float duration)
        {
            if (target == null) yield break;

            Vector3 minScaleVec = Vector3.one * minScale;
            Vector3 maxScaleVec = Vector3.one * maxScale;

            while (true)
            {
                if (target == null) yield break;

                // Scale up
                float elapsed = 0f;
                float halfDuration = duration * 0.5f;

                while (elapsed < halfDuration)
                {
                    if (target == null) yield break;

                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / halfDuration);
                    float easedT = ApplyEase(t, EaseType.EaseInOut);
                    target.transform.localScale = Vector3.Lerp(minScaleVec, maxScaleVec, easedT);
                    yield return null;
                }

                // Scale down
                elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    if (target == null) yield break;

                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / halfDuration);
                    float easedT = ApplyEase(t, EaseType.EaseInOut);
                    target.transform.localScale = Vector3.Lerp(maxScaleVec, minScaleVec, easedT);
                    yield return null;
                }
            }
        }

        private void StopAnimationForTarget(GameObject target)
        {
            int id = target.GetInstanceID();
            if (_activeAnimations.TryGetValue(id, out var coroutine))
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
                _activeAnimations.Remove(id);
            }
        }

        #endregion

        #region Easing Functions

        private float ApplyEase(float t, EaseType ease)
        {
            return ease switch
            {
                EaseType.Linear => t,
                EaseType.EaseIn => EaseIn(t),
                EaseType.EaseOut => EaseOut(t),
                EaseType.EaseInOut => EaseInOut(t),
                EaseType.Bounce => EaseBounce(t),
                EaseType.Elastic => EaseElastic(t),
                EaseType.Back => EaseBack(t),
                EaseType.Overshoot => EaseOvershoot(t),
                _ => t
            };
        }

        private float EaseIn(float t)
        {
            return t * t;
        }

        private float EaseOut(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        private float EaseInOut(float t)
        {
            return t < 0.5f
                ? 2f * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        private float EaseBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }
            else if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }
            else
            {
                t -= 2.625f / d1;
                return n1 * t * t + 0.984375f;
            }
        }

        private float EaseElastic(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;

            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;

            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        private float EaseBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;

            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private float EaseOvershoot(float t)
        {
            return 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
        }

        #endregion

        #region Utility

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("UIAnimator");
                Instance = go.AddComponent<UIAnimator>();
            }
        }

        #endregion
    }

    #region Extension Methods

    /// <summary>
    /// Extension methods for easy animation access from UI components
    /// </summary>
    public static class UIAnimatorExtensions
    {
        public static void AnimateFadeIn(this GameObject go, float duration = 0.3f, Action onComplete = null)
        {
            UIAnimator.FadeIn(go, duration, UIAnimator.EaseType.EaseOut, onComplete);
        }

        public static void AnimateFadeOut(this GameObject go, float duration = 0.3f, Action onComplete = null)
        {
            UIAnimator.FadeOut(go, duration, UIAnimator.EaseType.EaseIn, onComplete);
        }

        public static void AnimateSlideIn(this GameObject go, UIAnimator.SlideDirection direction = UIAnimator.SlideDirection.Bottom, float duration = 0.3f, Action onComplete = null)
        {
            UIAnimator.SlideIn(go, direction, duration, UIAnimator.EaseType.EaseOut, onComplete);
        }

        public static void AnimateSlideOut(this GameObject go, UIAnimator.SlideDirection direction = UIAnimator.SlideDirection.Bottom, float duration = 0.3f, Action onComplete = null)
        {
            UIAnimator.SlideOut(go, direction, duration, UIAnimator.EaseType.EaseIn, onComplete);
        }

        public static void AnimatePopIn(this GameObject go, float duration = 0.3f, Action onComplete = null)
        {
            UIAnimator.PopIn(go, duration, onComplete);
        }

        public static void AnimatePopOut(this GameObject go, float duration = 0.3f, Action onComplete = null)
        {
            UIAnimator.PopOut(go, duration, onComplete);
        }

        public static void AnimateBounce(this GameObject go, Action onComplete = null)
        {
            UIAnimator.ScaleBounce(go, -1, onComplete);
        }

        public static void AnimateShake(this GameObject go, Action onComplete = null)
        {
            UIAnimator.Shake(go, -1, -1, onComplete);
        }

        public static void AnimateButtonPress(this Button button, Action onComplete = null)
        {
            UIAnimator.ButtonPress(button.gameObject, onComplete);
        }
    }

    #endregion
}
