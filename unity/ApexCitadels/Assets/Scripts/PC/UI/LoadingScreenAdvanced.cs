using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Animated background for loading screens.
    /// Features:
    /// - Parallax scrolling layers
    /// - Particle effects
    /// - Dynamic color shifts
    /// - Context-aware themes
    /// </summary>
    public class LoadingBackground : MonoBehaviour
    {
        [Header("Layers")]
        [SerializeField] private List<ParallaxLayer> layers;
        [SerializeField] private RectTransform layerContainer;
        
        [Header("Color Theme")]
        [SerializeField] private BackgroundTheme currentTheme = BackgroundTheme.Fantasy;
        [SerializeField] private float colorShiftSpeed = 0.5f;
        [SerializeField] private List<ThemeData> themes;
        
        [Header("Particles")]
        [SerializeField] private ParticleSystem backgroundParticles;
        [SerializeField] private bool enableParticles = true;
        
        [Header("Vignette")]
        [SerializeField] private Image vignetteOverlay;
        [SerializeField] private float vignetteIntensity = 0.3f;
        [SerializeField] private float vignettePulseSpeed = 0.5f;
        
        // State
        private float _colorPhase;
        private ThemeData _activeTheme;
        private Color _currentTint;
        
        private void Awake()
        {
            InitializeThemes();
            ApplyTheme(currentTheme);
        }
        
        private void InitializeThemes()
        {
            if (themes == null || themes.Count == 0)
            {
                themes = new List<ThemeData>
                {
                    new ThemeData
                    {
                        theme = BackgroundTheme.Fantasy,
                        primaryColor = new Color(0.1f, 0.2f, 0.4f),
                        secondaryColor = new Color(0.3f, 0.1f, 0.4f),
                        accentColor = new Color(0.6f, 0.4f, 0.9f),
                        particleColor = new Color(0.8f, 0.7f, 1f, 0.5f)
                    },
                    new ThemeData
                    {
                        theme = BackgroundTheme.Combat,
                        primaryColor = new Color(0.4f, 0.1f, 0.1f),
                        secondaryColor = new Color(0.5f, 0.2f, 0.1f),
                        accentColor = new Color(1f, 0.5f, 0.2f),
                        particleColor = new Color(1f, 0.4f, 0.2f, 0.5f)
                    },
                    new ThemeData
                    {
                        theme = BackgroundTheme.Territory,
                        primaryColor = new Color(0.1f, 0.3f, 0.2f),
                        secondaryColor = new Color(0.2f, 0.4f, 0.3f),
                        accentColor = new Color(0.4f, 0.8f, 0.5f),
                        particleColor = new Color(0.5f, 1f, 0.6f, 0.5f)
                    },
                    new ThemeData
                    {
                        theme = BackgroundTheme.Citadel,
                        primaryColor = new Color(0.2f, 0.2f, 0.3f),
                        secondaryColor = new Color(0.3f, 0.25f, 0.35f),
                        accentColor = new Color(0.9f, 0.8f, 0.5f),
                        particleColor = new Color(1f, 0.9f, 0.6f, 0.5f)
                    },
                    new ThemeData
                    {
                        theme = BackgroundTheme.Night,
                        primaryColor = new Color(0.05f, 0.05f, 0.15f),
                        secondaryColor = new Color(0.1f, 0.1f, 0.2f),
                        accentColor = new Color(0.3f, 0.4f, 0.8f),
                        particleColor = new Color(0.5f, 0.6f, 1f, 0.3f)
                    }
                };
            }
        }
        
        private void Update()
        {
            UpdateParallax();
            UpdateColorShift();
            UpdateVignette();
        }
        
        private void UpdateParallax()
        {
            if (layers == null) return;
            
            foreach (var layer in layers)
            {
                if (layer.image == null) continue;
                
                // Scroll based on speed
                float xOffset = Time.unscaledTime * layer.scrollSpeed.x * 0.01f;
                float yOffset = Time.unscaledTime * layer.scrollSpeed.y * 0.01f;
                
                // Apply scrolling (wrap using modulo)
                if (layer.image.material != null)
                {
                    Vector2 offset = new Vector2(xOffset % 1f, yOffset % 1f);
                    layer.image.material.SetTextureOffset("_MainTex", offset);
                }
                else
                {
                    // For images without material, use anchor position
                    var rect = layer.image.rectTransform;
                    rect.anchoredPosition = new Vector2(
                        Mathf.Sin(Time.unscaledTime * layer.scrollSpeed.x * 0.1f) * layer.scrollAmplitude.x,
                        Mathf.Sin(Time.unscaledTime * layer.scrollSpeed.y * 0.1f) * layer.scrollAmplitude.y
                    );
                }
                
                // Apply color tint with alpha fade
                if (layer.useTint)
                {
                    float alpha = Mathf.Lerp(layer.alphaRange.x, layer.alphaRange.y,
                        (Mathf.Sin(Time.unscaledTime * layer.alphaPulseSpeed) + 1) * 0.5f);
                    
                    Color tint = _currentTint;
                    tint.a = alpha;
                    layer.image.color = tint;
                }
            }
        }
        
        private void UpdateColorShift()
        {
            if (_activeTheme == null) return;
            
            _colorPhase += Time.unscaledDeltaTime * colorShiftSpeed;
            float t = (Mathf.Sin(_colorPhase) + 1) * 0.5f;
            
            _currentTint = Color.Lerp(_activeTheme.primaryColor, _activeTheme.secondaryColor, t);
        }
        
        private void UpdateVignette()
        {
            if (vignetteOverlay == null) return;
            
            float pulse = (Mathf.Sin(Time.unscaledTime * vignettePulseSpeed * Mathf.PI) + 1) * 0.5f;
            float intensity = Mathf.Lerp(vignetteIntensity * 0.8f, vignetteIntensity * 1.2f, pulse);
            
            Color vignetteColor = Color.black;
            vignetteColor.a = intensity;
            vignetteOverlay.color = vignetteColor;
        }
        
        #region Public API
        
        public void SetTheme(BackgroundTheme theme)
        {
            currentTheme = theme;
            ApplyTheme(theme);
        }
        
        public void ApplyTheme(BackgroundTheme theme)
        {
            _activeTheme = themes.Find(t => t.theme == theme);
            
            if (_activeTheme == null && themes.Count > 0)
            {
                _activeTheme = themes[0];
            }
            
            if (_activeTheme != null)
            {
                _currentTint = _activeTheme.primaryColor;
                
                // Update particles
                if (backgroundParticles != null && enableParticles)
                {
                    var main = backgroundParticles.main;
                    main.startColor = _activeTheme.particleColor;
                }
            }
        }
        
        public void TransitionToTheme(BackgroundTheme theme, float duration = 1f)
        {
            StartCoroutine(TransitionThemeCoroutine(theme, duration));
        }
        
        private IEnumerator TransitionThemeCoroutine(BackgroundTheme newTheme, float duration)
        {
            var oldTheme = _activeTheme;
            var targetTheme = themes.Find(t => t.theme == newTheme);
            
            if (targetTheme == null) yield break;
            
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                // Lerp colors
                _currentTint = Color.Lerp(
                    Color.Lerp(oldTheme.primaryColor, oldTheme.secondaryColor, 0.5f),
                    Color.Lerp(targetTheme.primaryColor, targetTheme.secondaryColor, 0.5f),
                    t
                );
                
                yield return null;
            }
            
            _activeTheme = targetTheme;
            currentTheme = newTheme;
        }
        
        public void SetParticlesEnabled(bool enabled)
        {
            enableParticles = enabled;
            if (backgroundParticles != null)
            {
                if (enabled)
                {
                    backgroundParticles.Play();
                }
                else
                {
                    backgroundParticles.Stop();
                }
            }
        }
        
        #endregion
        
        [Serializable]
        public class ParallaxLayer
        {
            public string name;
            public Image image;
            public Vector2 scrollSpeed = new Vector2(10f, 0);
            public Vector2 scrollAmplitude = new Vector2(20f, 10f);
            public bool useTint = true;
            public Vector2 alphaRange = new Vector2(0.3f, 0.7f);
            public float alphaPulseSpeed = 0.5f;
        }
        
        [Serializable]
        public class ThemeData
        {
            public BackgroundTheme theme;
            public Color primaryColor;
            public Color secondaryColor;
            public Color accentColor;
            public Color particleColor;
        }
    }
    
    public enum BackgroundTheme
    {
        Fantasy,
        Combat,
        Territory,
        Citadel,
        Night
    }
    
    /// <summary>
    /// Cinematic loading sequence with scene setup info.
    /// Used for major scene transitions with narrative context.
    /// </summary>
    public class CinematicLoadingSequence : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup mainCanvasGroup;
        [SerializeField] private RectTransform contentContainer;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI flavorText;
        [SerializeField] private Image sceneImage;
        [SerializeField] private LoadingProgressBar progressBar;
        
        [Header("Letterbox")]
        [SerializeField] private RectTransform topLetterbox;
        [SerializeField] private RectTransform bottomLetterbox;
        [SerializeField] private float letterboxHeight = 100f;
        
        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 1f;
        [SerializeField] private float elementDelay = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.8f;
        [SerializeField] private AnimationCurve easeCurve;
        
        [Header("Scene Presets")]
        [SerializeField] private List<SceneLoadingPreset> presets;
        
        // State
        private Coroutine _activeSequence;
        
        private void Awake()
        {
            if (easeCurve == null || easeCurve.length == 0)
            {
                easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
            
            InitializePresets();
        }
        
        private void InitializePresets()
        {
            if (presets == null || presets.Count == 0)
            {
                presets = new List<SceneLoadingPreset>
                {
                    new SceneLoadingPreset
                    {
                        sceneId = "WorldMap",
                        title = "THE REALM AWAITS",
                        subtitle = "World Map",
                        flavorText = "Empires rise and fall. Your citadel endures."
                    },
                    new SceneLoadingPreset
                    {
                        sceneId = "TerritoryBattle",
                        title = "PREPARE FOR BATTLE",
                        subtitle = "Territory Combat",
                        flavorText = "Victory favors the bold."
                    },
                    new SceneLoadingPreset
                    {
                        sceneId = "CitadelView",
                        title = "YOUR CITADEL",
                        subtitle = "City Builder",
                        flavorText = "From these walls, an empire is born."
                    }
                };
            }
        }
        
        #region Public API
        
        public void PlaySequence(string sceneId, Action onReady = null)
        {
            if (_activeSequence != null)
            {
                StopCoroutine(_activeSequence);
            }
            
            var preset = presets.Find(p => p.sceneId == sceneId);
            if (preset == null)
            {
                preset = new SceneLoadingPreset
                {
                    sceneId = sceneId,
                    title = sceneId.ToUpper(),
                    subtitle = "Loading...",
                    flavorText = ""
                };
            }
            
            _activeSequence = StartCoroutine(RunSequence(preset, onReady));
        }
        
        public void UpdateProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.Value = progress;
            }
        }
        
        public void Complete(Action onComplete)
        {
            StartCoroutine(CompleteSequence(onComplete));
        }
        
        #endregion
        
        private IEnumerator RunSequence(SceneLoadingPreset preset, Action onReady)
        {
            // Initialize
            gameObject.SetActive(true);
            mainCanvasGroup.alpha = 0;
            
            SetContent(preset);
            HideAllElements();
            
            // Fade in
            yield return StartCoroutine(FadeCanvasGroup(mainCanvasGroup, 0, 1, fadeInDuration));
            
            // Animate letterbox
            yield return StartCoroutine(AnimateLetterbox(true));
            
            // Reveal elements sequentially
            yield return StartCoroutine(RevealElement(sceneImage));
            yield return new WaitForSecondsRealtime(elementDelay);
            
            yield return StartCoroutine(RevealElement(titleText));
            yield return new WaitForSecondsRealtime(elementDelay);
            
            yield return StartCoroutine(RevealElement(subtitleText));
            yield return new WaitForSecondsRealtime(elementDelay);
            
            yield return StartCoroutine(RevealElement(flavorText));
            yield return new WaitForSecondsRealtime(elementDelay);
            
            // Show progress bar
            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(true);
            }
            
            onReady?.Invoke();
        }
        
        private IEnumerator CompleteSequence(Action onComplete)
        {
            // Flash progress bar
            if (progressBar != null)
            {
                progressBar.Complete();
            }
            
            yield return new WaitForSecondsRealtime(0.5f);
            
            // Animate letterbox close
            yield return StartCoroutine(AnimateLetterbox(false));
            
            // Fade out
            yield return StartCoroutine(FadeCanvasGroup(mainCanvasGroup, 1, 0, fadeOutDuration));
            
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }
        
        private void SetContent(SceneLoadingPreset preset)
        {
            if (titleText != null) titleText.text = preset.title;
            if (subtitleText != null) subtitleText.text = preset.subtitle;
            if (flavorText != null) flavorText.text = preset.flavorText;
            if (sceneImage != null && preset.sceneSprite != null) sceneImage.sprite = preset.sceneSprite;
        }
        
        private void HideAllElements()
        {
            SetElementAlpha(sceneImage, 0);
            SetElementAlpha(titleText, 0);
            SetElementAlpha(subtitleText, 0);
            SetElementAlpha(flavorText, 0);
            
            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(false);
            }
        }
        
        private void SetElementAlpha(Graphic element, float alpha)
        {
            if (element == null) return;
            Color c = element.color;
            c.a = alpha;
            element.color = c;
        }
        
        private IEnumerator RevealElement(Graphic element)
        {
            if (element == null) yield break;
            
            float elapsed = 0;
            float duration = 0.4f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = easeCurve.Evaluate(elapsed / duration);
                SetElementAlpha(element, t);
                yield return null;
            }
            
            SetElementAlpha(element, 1);
        }
        
        private IEnumerator AnimateLetterbox(bool show)
        {
            if (topLetterbox == null || bottomLetterbox == null) yield break;
            
            float startHeight = show ? 0 : letterboxHeight;
            float endHeight = show ? letterboxHeight : 0;
            float duration = 0.5f;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = easeCurve.Evaluate(elapsed / duration);
                float height = Mathf.Lerp(startHeight, endHeight, t);
                
                topLetterbox.sizeDelta = new Vector2(topLetterbox.sizeDelta.x, height);
                bottomLetterbox.sizeDelta = new Vector2(bottomLetterbox.sizeDelta.x, height);
                
                yield return null;
            }
            
            topLetterbox.sizeDelta = new Vector2(topLetterbox.sizeDelta.x, endHeight);
            bottomLetterbox.sizeDelta = new Vector2(bottomLetterbox.sizeDelta.x, endHeight);
        }
        
        private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = easeCurve.Evaluate(elapsed / duration);
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            
            group.alpha = to;
        }
        
        [Serializable]
        public class SceneLoadingPreset
        {
            public string sceneId;
            public string title;
            public string subtitle;
            public string flavorText;
            public Sprite sceneSprite;
        }
    }
    
    /// <summary>
    /// Quick loading indicator for minor transitions.
    /// Minimal UI footprint for fast loads.
    /// </summary>
    public class QuickLoadIndicator : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform container;
        [SerializeField] private LoadingSpinner spinner;
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        [SerializeField] private float minimumDisplayTime = 0.5f;
        
        [Header("Position")]
        [SerializeField] private LoadingPosition position = LoadingPosition.Center;
        
        // State
        private float _showTime;
        private bool _isShowing;
        private Action _onHideComplete;
        
        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            }
            
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }
        
        public void Show(string status = "Loading...")
        {
            gameObject.SetActive(true);
            SetPosition();
            
            if (statusText != null)
            {
                statusText.text = status;
            }
            
            if (spinner != null)
            {
                spinner.Play();
            }
            
            _showTime = Time.unscaledTime;
            _isShowing = true;
            
            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }
        
        public void Hide(Action onComplete = null)
        {
            _onHideComplete = onComplete;
            
            // Ensure minimum display time
            float elapsed = Time.unscaledTime - _showTime;
            float remaining = minimumDisplayTime - elapsed;
            
            if (remaining > 0)
            {
                StartCoroutine(DelayedHide(remaining));
            }
            else
            {
                StartCoroutine(FadeOut());
            }
        }
        
        public void UpdateStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
        }
        
        private void SetPosition()
        {
            if (container == null) return;
            
            switch (position)
            {
                case LoadingPosition.Center:
                    container.anchorMin = new Vector2(0.5f, 0.5f);
                    container.anchorMax = new Vector2(0.5f, 0.5f);
                    container.anchoredPosition = Vector2.zero;
                    break;
                case LoadingPosition.BottomRight:
                    container.anchorMin = new Vector2(1, 0);
                    container.anchorMax = new Vector2(1, 0);
                    container.anchoredPosition = new Vector2(-50, 50);
                    break;
                case LoadingPosition.BottomLeft:
                    container.anchorMin = new Vector2(0, 0);
                    container.anchorMax = new Vector2(0, 0);
                    container.anchoredPosition = new Vector2(50, 50);
                    break;
                case LoadingPosition.TopRight:
                    container.anchorMin = new Vector2(1, 1);
                    container.anchorMax = new Vector2(1, 1);
                    container.anchoredPosition = new Vector2(-50, -50);
                    break;
            }
        }
        
        private IEnumerator FadeIn()
        {
            float elapsed = 0;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = elapsed / fadeInDuration;
                yield return null;
            }
            
            canvasGroup.alpha = 1;
        }
        
        private IEnumerator DelayedHide(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            StartCoroutine(FadeOut());
        }
        
        private IEnumerator FadeOut()
        {
            _isShowing = false;
            float elapsed = 0;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1 - (elapsed / fadeOutDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 0;
            
            if (spinner != null)
            {
                spinner.Stop();
            }
            
            gameObject.SetActive(false);
            _onHideComplete?.Invoke();
        }
        
        public enum LoadingPosition
        {
            Center,
            BottomRight,
            BottomLeft,
            TopRight
        }
    }
}
