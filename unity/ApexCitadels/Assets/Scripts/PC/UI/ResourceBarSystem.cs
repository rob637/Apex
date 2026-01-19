using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using ApexCitadels.Data;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Animated resource bar system for displaying player resources.
    /// Features:
    /// - Smooth value changes with counting animation
    /// - Visual feedback on gain/loss
    /// - Capacity indicators
    /// - Warning states (low/full)
    /// - Hover details
    /// </summary>
    public class ResourceBarSystem : MonoBehaviour
    {
        [Header("Resource Bar Prefab")]
        [SerializeField] private GameObject resourceBarPrefab;
        [SerializeField] private RectTransform resourceBarContainer;
        
        [Header("Animation Settings")]
        [SerializeField] private float countAnimationDuration = 0.5f;
        [SerializeField] private float flashDuration = 0.3f;
        [SerializeField] private float pulseScale = 1.1f;
        [SerializeField] private AnimationCurve countCurve;
        
        [Header("Visual Settings")]
        [SerializeField] private Color gainColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color lossColor = new Color(0.8f, 0.3f, 0.3f);
        [SerializeField] private Color warningColor = new Color(0.9f, 0.6f, 0.1f);
        [SerializeField] private Color fullColor = new Color(0.2f, 0.8f, 0.9f);
        [SerializeField] private float lowResourceThreshold = 0.2f;
        [SerializeField] private float fullResourceThreshold = 0.95f;
        
        [Header("Resource Configurations")]
        [SerializeField] private List<ResourceBarConfig> resourceConfigs = new List<ResourceBarConfig>();
        
        // Singleton
        private static ResourceBarSystem _instance;
        public static ResourceBarSystem Instance => _instance;
        
        // Active resource bars
        private Dictionary<ResourceType, ResourceBarInstance> _resourceBars = 
            new Dictionary<ResourceType, ResourceBarInstance>();
        
        // Pending animations
        private Dictionary<ResourceType, Coroutine> _activeAnimations =
            new Dictionary<ResourceType, Coroutine>();
        
        // Events
        public event Action<ResourceType, float, float> OnResourceChanged;
        public event Action<ResourceType> OnResourceLow;
        public event Action<ResourceType> OnResourceFull;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeAnimationCurve();
        }
        
        private void InitializeAnimationCurve()
        {
            if (countCurve == null || countCurve.length == 0)
            {
                countCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
        }
        
        /// <summary>
        /// Initialize all configured resource bars
        /// </summary>
        public void Initialize()
        {
            foreach (var config in resourceConfigs)
            {
                CreateResourceBar(config);
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Set resource value with animation
        /// </summary>
        public void SetResource(ResourceType type, float amount, float capacity, bool animate = true)
        {
            if (!_resourceBars.TryGetValue(type, out var instance)) return;
            
            float previousAmount = instance.currentAmount;
            float previousCapacity = instance.currentCapacity;
            
            if (animate && Mathf.Abs(amount - previousAmount) > 0.1f)
            {
                // Cancel existing animation
                if (_activeAnimations.TryGetValue(type, out var existing) && existing != null)
                {
                    StopCoroutine(existing);
                }
                
                _activeAnimations[type] = StartCoroutine(
                    AnimateResourceChange(instance, previousAmount, amount, previousCapacity, capacity));
            }
            else
            {
                // Immediate update
                UpdateResourceBar(instance, amount, capacity);
            }
            
            OnResourceChanged?.Invoke(type, amount, capacity);
        }
        
        /// <summary>
        /// Add to resource (convenience method)
        /// </summary>
        public void AddResource(ResourceType type, float amount)
        {
            if (!_resourceBars.TryGetValue(type, out var instance)) return;
            
            float newAmount = Mathf.Min(instance.currentAmount + amount, instance.currentCapacity);
            SetResource(type, newAmount, instance.currentCapacity);
            
            // Flash positive
            FlashResourceBar(type, true);
        }
        
        /// <summary>
        /// Remove from resource (convenience method)
        /// </summary>
        public void RemoveResource(ResourceType type, float amount)
        {
            if (!_resourceBars.TryGetValue(type, out var instance)) return;
            
            float newAmount = Mathf.Max(instance.currentAmount - amount, 0);
            SetResource(type, newAmount, instance.currentCapacity);
            
            // Flash negative
            FlashResourceBar(type, false);
        }
        
        /// <summary>
        /// Get current resource amount
        /// </summary>
        public float GetResource(ResourceType type)
        {
            if (_resourceBars.TryGetValue(type, out var instance))
            {
                return instance.currentAmount;
            }
            return 0;
        }
        
        /// <summary>
        /// Get resource capacity
        /// </summary>
        public float GetCapacity(ResourceType type)
        {
            if (_resourceBars.TryGetValue(type, out var instance))
            {
                return instance.currentCapacity;
            }
            return 0;
        }
        
        /// <summary>
        /// Flash resource bar to draw attention
        /// </summary>
        public void FlashResourceBar(ResourceType type, bool positive)
        {
            if (!_resourceBars.TryGetValue(type, out var instance)) return;
            
            StartCoroutine(AnimateFlash(instance, positive ? gainColor : lossColor));
        }
        
        /// <summary>
        /// Pulse resource bar (for important events)
        /// </summary>
        public void PulseResourceBar(ResourceType type)
        {
            if (!_resourceBars.TryGetValue(type, out var instance)) return;
            
            StartCoroutine(AnimatePulse(instance));
        }
        
        /// <summary>
        /// Show floating change text (+100, -50, etc.)
        /// </summary>
        public void ShowChangeText(ResourceType type, float amount)
        {
            if (!_resourceBars.TryGetValue(type, out var instance)) return;
            
            StartCoroutine(AnimateFloatingText(instance, amount));
        }
        
        /// <summary>
        /// Set production rate display
        /// </summary>
        public void SetProductionRate(ResourceType type, float ratePerHour)
        {
            if (!_resourceBars.TryGetValue(type, out var instance)) return;
            
            if (instance.rateText != null)
            {
                string sign = ratePerHour >= 0 ? "+" : "";
                string colorHex = ratePerHour >= 0 ? "#4CAF50" : "#F44336";
                instance.rateText.text = $"<color={colorHex}>{sign}{FormatRate(ratePerHour)}/hr</color>";
            }
        }
        
        #endregion
        
        #region Resource Bar Creation
        
        private void CreateResourceBar(ResourceBarConfig config)
        {
            if (resourceBarPrefab == null || resourceBarContainer == null) return;
            
            var go = Instantiate(resourceBarPrefab, resourceBarContainer);
            var rect = go.GetComponent<RectTransform>();
            
            var instance = new ResourceBarInstance
            {
                type = config.type,
                config = config,
                rectTransform = rect,
                canvasGroup = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>(),
                nameText = FindComponent<TextMeshProUGUI>(go, "Name"),
                amountText = FindComponent<TextMeshProUGUI>(go, "Amount"),
                capacityText = FindComponent<TextMeshProUGUI>(go, "Capacity"),
                rateText = FindComponent<TextMeshProUGUI>(go, "Rate"),
                fillImage = FindComponent<Image>(go, "Fill"),
                backgroundImage = FindComponent<Image>(go, "Background"),
                iconImage = FindComponent<Image>(go, "Icon"),
                glowImage = FindComponent<Image>(go, "Glow"),
                warningIndicator = FindTransform(go, "Warning"),
                floatingTextContainer = FindTransform(go, "FloatingTextContainer")
            };
            
            // Configure appearance
            if (instance.nameText != null) instance.nameText.text = config.displayName;
            if (instance.iconImage != null && config.icon != null) instance.iconImage.sprite = config.icon;
            if (instance.fillImage != null) instance.fillImage.color = config.barColor;
            if (instance.warningIndicator != null) instance.warningIndicator.gameObject.SetActive(false);
            if (instance.glowImage != null)
            {
                instance.glowImage.color = config.barColor;
                instance.glowImage.gameObject.SetActive(false);
            }
            
            _resourceBars[config.type] = instance;
            
            // Initialize with zero
            UpdateResourceBar(instance, 0, config.defaultCapacity);
        }
        
        private T FindComponent<T>(GameObject root, string childName) where T : Component
        {
            var child = root.transform.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }
        
        private RectTransform FindTransform(GameObject root, string childName)
        {
            var child = root.transform.Find(childName);
            return child != null ? child.GetComponent<RectTransform>() : null;
        }
        
        #endregion
        
        #region Resource Bar Updates
        
        private void UpdateResourceBar(ResourceBarInstance instance, float amount, float capacity)
        {
            instance.currentAmount = amount;
            instance.currentCapacity = capacity;
            
            // Update text
            if (instance.amountText != null)
            {
                instance.amountText.text = FormatNumber(amount);
            }
            
            if (instance.capacityText != null)
            {
                instance.capacityText.text = $"/ {FormatNumber(capacity)}";
            }
            
            // Update fill
            if (instance.fillImage != null)
            {
                float fillAmount = capacity > 0 ? amount / capacity : 0;
                instance.fillImage.fillAmount = fillAmount;
                
                // Color based on state
                if (fillAmount >= fullResourceThreshold)
                {
                    instance.fillImage.color = fullColor;
                    ShowWarningIndicator(instance, true, "FULL");
                    OnResourceFull?.Invoke(instance.type);
                }
                else if (fillAmount <= lowResourceThreshold)
                {
                    instance.fillImage.color = warningColor;
                    ShowWarningIndicator(instance, true, "LOW");
                    OnResourceLow?.Invoke(instance.type);
                }
                else
                {
                    instance.fillImage.color = instance.config.barColor;
                    ShowWarningIndicator(instance, false);
                }
            }
        }
        
        private void ShowWarningIndicator(ResourceBarInstance instance, bool show, string text = null)
        {
            if (instance.warningIndicator == null) return;
            
            instance.warningIndicator.gameObject.SetActive(show);
            
            if (show)
            {
                var warningText = instance.warningIndicator.GetComponentInChildren<TextMeshProUGUI>();
                if (warningText != null && !string.IsNullOrEmpty(text))
                {
                    warningText.text = text;
                }
            }
        }
        
        #endregion
        
        #region Animations
        
        private IEnumerator AnimateResourceChange(ResourceBarInstance instance, float fromAmount, float toAmount,
            float fromCapacity, float toCapacity)
        {
            float elapsed = 0;
            bool isGain = toAmount > fromAmount;
            
            // Show glow during animation
            if (instance.glowImage != null)
            {
                instance.glowImage.gameObject.SetActive(true);
                instance.glowImage.color = new Color(
                    isGain ? gainColor.r : lossColor.r,
                    isGain ? gainColor.g : lossColor.g,
                    isGain ? gainColor.b : lossColor.b,
                    0
                );
            }
            
            while (elapsed < countAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = countCurve.Evaluate(elapsed / countAnimationDuration);
                
                float currentAmount = Mathf.Lerp(fromAmount, toAmount, t);
                float currentCapacity = Mathf.Lerp(fromCapacity, toCapacity, t);
                
                UpdateResourceBar(instance, currentAmount, currentCapacity);
                
                // Glow pulse
                if (instance.glowImage != null)
                {
                    float glowAlpha = Mathf.Sin(t * Mathf.PI) * 0.5f;
                    var glowColor = instance.glowImage.color;
                    glowColor.a = glowAlpha;
                    instance.glowImage.color = glowColor;
                }
                
                yield return null;
            }
            
            UpdateResourceBar(instance, toAmount, toCapacity);
            
            // Hide glow
            if (instance.glowImage != null)
            {
                instance.glowImage.gameObject.SetActive(false);
            }
            
            _activeAnimations.Remove(instance.type);
        }
        
        private IEnumerator AnimateFlash(ResourceBarInstance instance, Color flashColor)
        {
            if (instance.backgroundImage == null) yield break;
            
            Color originalColor = instance.backgroundImage.color;
            
            float elapsed = 0;
            float halfDuration = flashDuration * 0.5f;
            
            // Flash to color
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                instance.backgroundImage.color = Color.Lerp(originalColor, flashColor, t);
                yield return null;
            }
            
            // Flash back
            elapsed = 0;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                instance.backgroundImage.color = Color.Lerp(flashColor, originalColor, t);
                yield return null;
            }
            
            instance.backgroundImage.color = originalColor;
        }
        
        private IEnumerator AnimatePulse(ResourceBarInstance instance)
        {
            Vector3 originalScale = instance.rectTransform.localScale;
            Vector3 targetScale = originalScale * pulseScale;
            
            float pulseDuration = 0.15f;
            
            // Scale up
            float elapsed = 0;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / pulseDuration;
                instance.rectTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }
            
            // Scale down with slight overshoot
            elapsed = 0;
            float bounceDuration = 0.25f;
            while (elapsed < bounceDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / bounceDuration;
                
                // Bounce curve
                float bounce = 1 + Mathf.Sin(t * Mathf.PI * 2) * (1 - t) * 0.1f;
                instance.rectTransform.localScale = Vector3.Lerp(targetScale, originalScale, t) * bounce;
                yield return null;
            }
            
            instance.rectTransform.localScale = originalScale;
        }
        
        private IEnumerator AnimateFloatingText(ResourceBarInstance instance, float amount)
        {
            if (instance.floatingTextContainer == null) yield break;
            
            // Create floating text
            var textGo = new GameObject("FloatingText");
            textGo.transform.SetParent(instance.floatingTextContainer, false);
            
            var textComponent = textGo.AddComponent<TextMeshProUGUI>();
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontSize = 14;
            
            string sign = amount >= 0 ? "+" : "";
            textComponent.color = amount >= 0 ? gainColor : lossColor;
            textComponent.text = $"{sign}{FormatNumber(amount)}";
            
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(100, 30);
            
            // Animate float up and fade
            float duration = 1f;
            float elapsed = 0;
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0, 50);
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                
                // Fade out in second half
                if (t > 0.5f)
                {
                    float fadeT = (t - 0.5f) * 2f;
                    textComponent.alpha = 1 - fadeT;
                }
                
                yield return null;
            }
            
            Destroy(textGo);
        }
        
        #endregion
        
        #region Helpers
        
        private string FormatNumber(float value)
        {
            if (Mathf.Abs(value) >= 1000000)
            {
                return (value / 1000000f).ToString("F1") + "M";
            }
            else if (Mathf.Abs(value) >= 1000)
            {
                return (value / 1000f).ToString("F1") + "K";
            }
            else
            {
                return Mathf.FloorToInt(value).ToString("N0");
            }
        }
        
        private string FormatRate(float ratePerHour)
        {
            if (Mathf.Abs(ratePerHour) >= 1000000)
            {
                return (ratePerHour / 1000000f).ToString("F1") + "M";
            }
            else if (Mathf.Abs(ratePerHour) >= 1000)
            {
                return (ratePerHour / 1000f).ToString("F1") + "K";
            }
            else
            {
                return ratePerHour.ToString("F0");
            }
        }
        
        #endregion
    }
    
    #region Data Types
    
    [Serializable]
    public class ResourceBarConfig
    {
        public ResourceType type;
        public string displayName;
        public Sprite icon;
        public Color barColor = Color.green;
        public float defaultCapacity = 1000;
    }
    
    public class ResourceBarInstance
    {
        public ResourceType type;
        public ResourceBarConfig config;
        public RectTransform rectTransform;
        public CanvasGroup canvasGroup;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI amountText;
        public TextMeshProUGUI capacityText;
        public TextMeshProUGUI rateText;
        public Image fillImage;
        public Image backgroundImage;
        public Image iconImage;
        public Image glowImage;
        public RectTransform warningIndicator;
        public RectTransform floatingTextContainer;
        
        public float currentAmount;
        public float currentCapacity;
    }
    
    // Note: ResourceType is now defined in ApexCitadels.Data.ResourceTypes
    
    #endregion
}
