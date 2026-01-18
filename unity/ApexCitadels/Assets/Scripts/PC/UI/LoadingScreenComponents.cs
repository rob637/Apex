using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Progress bar with multiple visual styles and animations.
    /// Used in loading screens and UI progress indicators.
    /// Features:
    /// - Multiple bar styles (solid, striped, animated)
    /// - Smooth value interpolation
    /// - Glow effects on progress
    /// - Sub-progress indicators
    /// </summary>
    public class LoadingProgressBar : MonoBehaviour
    {
        [Header("Core Components")]
        [SerializeField] private RectTransform container;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image secondaryFillImage; // For sub-progress
        [SerializeField] private Image glowImage;
        [SerializeField] private TextMeshProUGUI percentageText;
        
        [Header("Style")]
        [SerializeField] private ProgressBarStyle style = ProgressBarStyle.Modern;
        [SerializeField] private Color fillColor = new Color(0.2f, 0.7f, 1f);
        [SerializeField] private Color secondaryFillColor = new Color(0.1f, 0.5f, 0.8f, 0.5f);
        [SerializeField] private Color glowColor = new Color(0.2f, 0.7f, 1f, 0.5f);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        [Header("Animation")]
        [SerializeField] private float fillSpeed = 3f;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseIntensity = 0.2f;
        [SerializeField] private bool animateStripes = true;
        [SerializeField] private float stripeSpeed = 50f;
        
        [Header("Glow Settings")]
        [SerializeField] private bool enableGlow = true;
        [SerializeField] private float glowPulseSpeed = 1.5f;
        [SerializeField] private float glowMinAlpha = 0.3f;
        [SerializeField] private float glowMaxAlpha = 0.7f;
        
        // State
        private float _currentValue;
        private float _targetValue;
        private float _secondaryValue;
        private Material _fillMaterial;
        private bool _isIndeterminate;
        private float _indeterminatePhase;
        
        // Properties
        public float Value
        {
            get => _targetValue;
            set => _targetValue = Mathf.Clamp01(value);
        }
        
        public float CurrentDisplayValue => _currentValue;
        
        private void Awake()
        {
            InitializeMaterials();
            ApplyStyle();
        }
        
        private void InitializeMaterials()
        {
            if (fillImage != null && fillImage.material != null)
            {
                // Create instance of material to avoid modifying shared material
                _fillMaterial = new Material(fillImage.material);
                fillImage.material = _fillMaterial;
            }
        }
        
        private void ApplyStyle()
        {
            if (fillImage != null)
            {
                fillImage.color = fillColor;
            }
            
            if (secondaryFillImage != null)
            {
                secondaryFillImage.color = secondaryFillColor;
            }
            
            if (glowImage != null)
            {
                glowImage.color = glowColor;
                glowImage.gameObject.SetActive(enableGlow);
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }
        }
        
        private void Update()
        {
            UpdateFillValue();
            UpdateAnimations();
            UpdatePercentageText();
        }
        
        private void UpdateFillValue()
        {
            if (_isIndeterminate)
            {
                UpdateIndeterminate();
                return;
            }
            
            // Smooth interpolation
            if (Mathf.Abs(_currentValue - _targetValue) > 0.001f)
            {
                _currentValue = Mathf.Lerp(_currentValue, _targetValue, Time.unscaledDeltaTime * fillSpeed);
            }
            else
            {
                _currentValue = _targetValue;
            }
            
            // Apply to fill image
            if (fillImage != null)
            {
                fillImage.fillAmount = _currentValue;
            }
            
            // Apply to secondary fill (slightly behind main)
            if (secondaryFillImage != null)
            {
                float secondaryTarget = Mathf.Max(_secondaryValue, _currentValue - 0.05f);
                secondaryFillImage.fillAmount = secondaryTarget;
            }
        }
        
        private void UpdateIndeterminate()
        {
            _indeterminatePhase += Time.unscaledDeltaTime * 2f;
            
            // Create a sliding effect
            float t = (_indeterminatePhase % 2f) / 2f;
            
            if (fillImage != null && fillImage.type == Image.Type.Filled)
            {
                // Pulse effect
                float pulse = (Mathf.Sin(_indeterminatePhase * Mathf.PI) + 1) * 0.5f;
                fillImage.fillAmount = Mathf.Lerp(0.2f, 0.5f, pulse);
            }
        }
        
        private void UpdateAnimations()
        {
            // Stripe animation
            if (animateStripes && _fillMaterial != null)
            {
                float offset = (Time.unscaledTime * stripeSpeed) % 100f;
                _fillMaterial.SetFloat("_Offset", offset);
            }
            
            // Glow pulse
            if (enableGlow && glowImage != null && _currentValue > 0)
            {
                float glowAlpha = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, 
                    (Mathf.Sin(Time.unscaledTime * glowPulseSpeed * Mathf.PI) + 1) * 0.5f);
                
                Color glow = glowColor;
                glow.a = glowAlpha;
                glowImage.color = glow;
                
                // Position glow at fill edge
                var glowRect = glowImage.rectTransform;
                if (container != null)
                {
                    float containerWidth = container.rect.width;
                    float xPos = Mathf.Lerp(-containerWidth/2, containerWidth/2, _currentValue);
                    glowRect.anchoredPosition = new Vector2(xPos, glowRect.anchoredPosition.y);
                }
            }
            
            // Fill color pulse when active
            if (fillImage != null && _targetValue < 1f && _targetValue > 0)
            {
                float pulse = Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI);
                Color pulsedColor = Color.Lerp(fillColor, fillColor * 1.2f, pulse * pulseIntensity);
                fillImage.color = pulsedColor;
            }
        }
        
        private void UpdatePercentageText()
        {
            if (percentageText != null)
            {
                if (_isIndeterminate)
                {
                    percentageText.text = "Loading...";
                }
                else
                {
                    percentageText.text = $"{Mathf.RoundToInt(_currentValue * 100)}%";
                }
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Set progress value instantly
        /// </summary>
        public void SetValueInstant(float value)
        {
            _targetValue = Mathf.Clamp01(value);
            _currentValue = _targetValue;
            
            if (fillImage != null)
            {
                fillImage.fillAmount = _currentValue;
            }
        }
        
        /// <summary>
        /// Set secondary progress (for buffer/sub-progress)
        /// </summary>
        public void SetSecondaryValue(float value)
        {
            _secondaryValue = Mathf.Clamp01(value);
        }
        
        /// <summary>
        /// Enable indeterminate mode (loading state with unknown duration)
        /// </summary>
        public void SetIndeterminate(bool indeterminate)
        {
            _isIndeterminate = indeterminate;
            _indeterminatePhase = 0;
        }
        
        /// <summary>
        /// Change fill color
        /// </summary>
        public void SetFillColor(Color color)
        {
            fillColor = color;
            if (fillImage != null)
            {
                fillImage.color = fillColor;
            }
        }
        
        /// <summary>
        /// Flash the progress bar
        /// </summary>
        public void Flash()
        {
            StartCoroutine(FlashCoroutine());
        }
        
        private IEnumerator FlashCoroutine()
        {
            Color originalColor = fillColor;
            
            for (int i = 0; i < 3; i++)
            {
                if (fillImage != null)
                {
                    fillImage.color = Color.white;
                }
                yield return new WaitForSecondsRealtime(0.1f);
                
                if (fillImage != null)
                {
                    fillImage.color = originalColor;
                }
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
        
        /// <summary>
        /// Complete with celebration effect
        /// </summary>
        public void Complete()
        {
            SetValueInstant(1f);
            Flash();
        }
        
        #endregion
    }
    
    public enum ProgressBarStyle
    {
        Modern,
        Classic,
        Minimal,
        Striped,
        Gradient
    }
    
    /// <summary>
    /// Animated loading spinner with multiple styles.
    /// </summary>
    public class LoadingSpinner : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private RectTransform spinnerTransform;
        [SerializeField] private Image spinnerImage;
        [SerializeField] private Image[] ringImages;
        [SerializeField] private Image[] dotImages;
        
        [Header("Style")]
        [SerializeField] private SpinnerStyle style = SpinnerStyle.Circle;
        [SerializeField] private Color primaryColor = new Color(0.2f, 0.7f, 1f);
        [SerializeField] private Color secondaryColor = new Color(0.1f, 0.4f, 0.7f);
        
        [Header("Animation")]
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private bool reverseDirection;
        [SerializeField] private AnimationCurve pulseCurve;
        [SerializeField] private float pulseSpeed = 1f;
        
        private float _currentAngle;
        private float _pulsePhase;
        private bool _isAnimating = true;
        
        private void Awake()
        {
            if (pulseCurve == null || pulseCurve.length == 0)
            {
                pulseCurve = AnimationCurve.EaseInOut(0, 0.8f, 1, 1.2f);
            }
            
            ApplyStyle();
        }
        
        private void Update()
        {
            if (!_isAnimating) return;
            
            switch (style)
            {
                case SpinnerStyle.Circle:
                    AnimateCircle();
                    break;
                case SpinnerStyle.Rings:
                    AnimateRings();
                    break;
                case SpinnerStyle.Dots:
                    AnimateDots();
                    break;
                case SpinnerStyle.Pulse:
                    AnimatePulse();
                    break;
            }
        }
        
        private void ApplyStyle()
        {
            if (spinnerImage != null)
            {
                spinnerImage.color = primaryColor;
            }
            
            if (ringImages != null)
            {
                foreach (var ring in ringImages)
                {
                    if (ring != null)
                    {
                        ring.color = primaryColor;
                    }
                }
            }
            
            if (dotImages != null)
            {
                for (int i = 0; i < dotImages.Length; i++)
                {
                    if (dotImages[i] != null)
                    {
                        dotImages[i].color = Color.Lerp(primaryColor, secondaryColor, 
                            (float)i / dotImages.Length);
                    }
                }
            }
        }
        
        private void AnimateCircle()
        {
            if (spinnerTransform == null) return;
            
            float direction = reverseDirection ? 1 : -1;
            _currentAngle += rotationSpeed * Time.unscaledDeltaTime * direction;
            spinnerTransform.localRotation = Quaternion.Euler(0, 0, _currentAngle);
        }
        
        private void AnimateRings()
        {
            if (ringImages == null) return;
            
            for (int i = 0; i < ringImages.Length; i++)
            {
                if (ringImages[i] == null) continue;
                
                // Each ring rotates at different speed
                float speed = rotationSpeed * (1 + i * 0.3f);
                float direction = (i % 2 == 0) ? -1 : 1;
                
                var rect = ringImages[i].rectTransform;
                rect.Rotate(0, 0, speed * Time.unscaledDeltaTime * direction);
                
                // Pulse alpha
                float phase = Time.unscaledTime * pulseSpeed + i * 0.5f;
                float alpha = Mathf.Lerp(0.5f, 1f, (Mathf.Sin(phase * Mathf.PI) + 1) * 0.5f);
                
                Color c = ringImages[i].color;
                c.a = alpha;
                ringImages[i].color = c;
            }
        }
        
        private void AnimateDots()
        {
            if (dotImages == null) return;
            
            for (int i = 0; i < dotImages.Length; i++)
            {
                if (dotImages[i] == null) continue;
                
                // Each dot pulses with offset
                float phase = Time.unscaledTime * pulseSpeed * 3 + i * (Mathf.PI / dotImages.Length);
                float scale = pulseCurve.Evaluate((Mathf.Sin(phase) + 1) * 0.5f);
                
                dotImages[i].transform.localScale = Vector3.one * scale;
                
                // Also fade
                float alpha = Mathf.Lerp(0.3f, 1f, (Mathf.Sin(phase) + 1) * 0.5f);
                Color c = dotImages[i].color;
                c.a = alpha;
                dotImages[i].color = c;
            }
        }
        
        private void AnimatePulse()
        {
            if (spinnerTransform == null) return;
            
            _pulsePhase += Time.unscaledDeltaTime * pulseSpeed;
            float scale = pulseCurve.Evaluate((_pulsePhase % 1f));
            spinnerTransform.localScale = Vector3.one * scale;
            
            // Rotate slowly
            _currentAngle += rotationSpeed * 0.1f * Time.unscaledDeltaTime;
            spinnerTransform.localRotation = Quaternion.Euler(0, 0, _currentAngle);
        }
        
        #region Public API
        
        public void Play()
        {
            _isAnimating = true;
            gameObject.SetActive(true);
        }
        
        public void Stop()
        {
            _isAnimating = false;
        }
        
        public void SetColor(Color color)
        {
            primaryColor = color;
            ApplyStyle();
        }
        
        public void SetStyle(SpinnerStyle newStyle)
        {
            style = newStyle;
            _currentAngle = 0;
            _pulsePhase = 0;
        }
        
        #endregion
    }
    
    public enum SpinnerStyle
    {
        Circle,
        Rings,
        Dots,
        Pulse
    }
    
    /// <summary>
    /// Animated tips carousel for loading screens.
    /// </summary>
    public class LoadingTipsCarousel : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private TextMeshProUGUI tipsText;
        [SerializeField] private CanvasGroup textCanvasGroup;
        [SerializeField] private RectTransform iconContainer;
        [SerializeField] private Image tipIcon;
        
        [Header("Animation")]
        [SerializeField] private float displayDuration = 5f;
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private TransitionType transitionType = TransitionType.Fade;
        [SerializeField] private AnimationCurve easeCurve;
        
        [Header("Default Tips")]
        [SerializeField] private List<TipData> defaultTips;
        
        // State
        private List<TipData> _activeTips;
        private int _currentTipIndex;
        private Coroutine _cycleCoroutine;
        
        private void Awake()
        {
            if (easeCurve == null || easeCurve.length == 0)
            {
                easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
            
            if (textCanvasGroup == null && tipsText != null)
            {
                textCanvasGroup = tipsText.GetComponent<CanvasGroup>() ?? 
                    tipsText.gameObject.AddComponent<CanvasGroup>();
            }
            
            InitializeDefaultTips();
        }
        
        private void InitializeDefaultTips()
        {
            if (defaultTips == null || defaultTips.Count == 0)
            {
                defaultTips = new List<TipData>
                {
                    new TipData { text = "Capture territories to expand your influence." },
                    new TipData { text = "Build defenses to protect your citadel." },
                    new TipData { text = "Form alliances with other players for strength." },
                    new TipData { text = "Manage your resources wisely for growth." },
                    new TipData { text = "Scout enemy territories before attacking." }
                };
            }
            
            _activeTips = new List<TipData>(defaultTips);
        }
        
        private void OnEnable()
        {
            StartCycle();
        }
        
        private void OnDisable()
        {
            StopCycle();
        }
        
        public void StartCycle()
        {
            StopCycle();
            _cycleCoroutine = StartCoroutine(CycleTips());
        }
        
        public void StopCycle()
        {
            if (_cycleCoroutine != null)
            {
                StopCoroutine(_cycleCoroutine);
                _cycleCoroutine = null;
            }
        }
        
        private IEnumerator CycleTips()
        {
            if (_activeTips == null || _activeTips.Count == 0)
            {
                yield break;
            }
            
            // Shuffle tips
            ShuffleTips();
            _currentTipIndex = 0;
            
            // Show first tip immediately
            ShowTipInstant(_activeTips[_currentTipIndex]);
            
            while (true)
            {
                // Wait display duration
                yield return new WaitForSecondsRealtime(displayDuration);
                
                // Move to next tip
                _currentTipIndex = (_currentTipIndex + 1) % _activeTips.Count;
                
                // Transition to new tip
                yield return StartCoroutine(TransitionToTip(_activeTips[_currentTipIndex]));
            }
        }
        
        private void ShuffleTips()
        {
            for (int i = _activeTips.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = _activeTips[i];
                _activeTips[i] = _activeTips[j];
                _activeTips[j] = temp;
            }
        }
        
        private void ShowTipInstant(TipData tip)
        {
            if (tipsText != null)
            {
                tipsText.text = tip.text;
            }
            
            if (tipIcon != null && tip.icon != null)
            {
                tipIcon.sprite = tip.icon;
                tipIcon.gameObject.SetActive(true);
            }
            else if (tipIcon != null)
            {
                tipIcon.gameObject.SetActive(false);
            }
            
            if (textCanvasGroup != null)
            {
                textCanvasGroup.alpha = 1;
            }
        }
        
        private IEnumerator TransitionToTip(TipData tip)
        {
            switch (transitionType)
            {
                case TransitionType.Fade:
                    yield return StartCoroutine(FadeTransition(tip));
                    break;
                case TransitionType.Slide:
                    yield return StartCoroutine(SlideTransition(tip));
                    break;
                case TransitionType.Typewriter:
                    yield return StartCoroutine(TypewriterTransition(tip));
                    break;
            }
        }
        
        private IEnumerator FadeTransition(TipData tip)
        {
            // Fade out
            float elapsed = 0;
            while (elapsed < transitionDuration / 2)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = easeCurve.Evaluate(elapsed / (transitionDuration / 2));
                textCanvasGroup.alpha = 1 - t;
                yield return null;
            }
            
            // Change content
            ShowTipInstant(tip);
            textCanvasGroup.alpha = 0;
            
            // Fade in
            elapsed = 0;
            while (elapsed < transitionDuration / 2)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = easeCurve.Evaluate(elapsed / (transitionDuration / 2));
                textCanvasGroup.alpha = t;
                yield return null;
            }
            
            textCanvasGroup.alpha = 1;
        }
        
        private IEnumerator SlideTransition(TipData tip)
        {
            if (tipsText == null) yield break;
            
            var textRect = tipsText.rectTransform;
            Vector2 originalPos = textRect.anchoredPosition;
            float slideDistance = 100f;
            
            // Slide out
            float elapsed = 0;
            while (elapsed < transitionDuration / 2)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = easeCurve.Evaluate(elapsed / (transitionDuration / 2));
                textRect.anchoredPosition = originalPos + Vector2.left * slideDistance * t;
                textCanvasGroup.alpha = 1 - t;
                yield return null;
            }
            
            // Change content
            ShowTipInstant(tip);
            textRect.anchoredPosition = originalPos + Vector2.right * slideDistance;
            textCanvasGroup.alpha = 0;
            
            // Slide in
            elapsed = 0;
            while (elapsed < transitionDuration / 2)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = easeCurve.Evaluate(elapsed / (transitionDuration / 2));
                textRect.anchoredPosition = Vector2.Lerp(originalPos + Vector2.right * slideDistance, 
                    originalPos, t);
                textCanvasGroup.alpha = t;
                yield return null;
            }
            
            textRect.anchoredPosition = originalPos;
            textCanvasGroup.alpha = 1;
        }
        
        private IEnumerator TypewriterTransition(TipData tip)
        {
            // Fade out existing
            float elapsed = 0;
            while (elapsed < transitionDuration / 4)
            {
                elapsed += Time.unscaledDeltaTime;
                textCanvasGroup.alpha = 1 - (elapsed / (transitionDuration / 4));
                yield return null;
            }
            
            // Type new text
            if (tipIcon != null && tip.icon != null)
            {
                tipIcon.sprite = tip.icon;
                tipIcon.gameObject.SetActive(true);
            }
            
            textCanvasGroup.alpha = 1;
            string fullText = tip.text;
            int charIndex = 0;
            float charDelay = (transitionDuration * 0.75f) / fullText.Length;
            charDelay = Mathf.Max(charDelay, 0.02f);
            
            while (charIndex < fullText.Length)
            {
                charIndex++;
                tipsText.text = fullText.Substring(0, charIndex);
                yield return new WaitForSecondsRealtime(charDelay);
            }
        }
        
        #region Public API
        
        public void SetTips(List<string> tips)
        {
            _activeTips = new List<TipData>();
            foreach (var tip in tips)
            {
                _activeTips.Add(new TipData { text = tip });
            }
            
            // Restart cycle with new tips
            if (gameObject.activeInHierarchy)
            {
                StartCycle();
            }
        }
        
        public void AddTip(string tip, Sprite icon = null)
        {
            _activeTips.Add(new TipData { text = tip, icon = icon });
        }
        
        public void ClearTips()
        {
            _activeTips.Clear();
        }
        
        public void ForceNextTip()
        {
            if (_cycleCoroutine != null)
            {
                StopCoroutine(_cycleCoroutine);
            }
            
            _currentTipIndex = (_currentTipIndex + 1) % _activeTips.Count;
            StartCoroutine(TransitionToTip(_activeTips[_currentTipIndex]));
            
            _cycleCoroutine = StartCoroutine(CycleTips());
        }
        
        #endregion
        
        public enum TransitionType
        {
            Fade,
            Slide,
            Typewriter
        }
        
        [Serializable]
        public class TipData
        {
            public string text;
            public Sprite icon;
        }
    }
}
