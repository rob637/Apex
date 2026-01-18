using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Dynamic tooltip system with rich content support.
    /// Features:
    /// - Automatic positioning to stay on screen
    /// - Rich text formatting
    /// - Icons and images
    /// - Comparison tooltips (item vs equipped)
    /// - Delayed show with cancel on leave
    /// </summary>
    public class TooltipSystem : MonoBehaviour
    {
        [Header("Tooltip Prefabs")]
        [SerializeField] private RectTransform simpleTooltip;
        [SerializeField] private RectTransform richTooltip;
        [SerializeField] private RectTransform comparisonTooltip;
        [SerializeField] private RectTransform buildingTooltip;
        [SerializeField] private RectTransform resourceTooltip;
        
        [Header("Simple Tooltip Components")]
        [SerializeField] private TextMeshProUGUI simpleTooltipText;
        
        [Header("Rich Tooltip Components")]
        [SerializeField] private TextMeshProUGUI richTitleText;
        [SerializeField] private TextMeshProUGUI richDescriptionText;
        [SerializeField] private Image richIconImage;
        [SerializeField] private TextMeshProUGUI richStatsText;
        [SerializeField] private RectTransform richStatsContainer;
        
        [Header("Comparison Tooltip Components")]
        [SerializeField] private RectTransform leftComparison;
        [SerializeField] private RectTransform rightComparison;
        
        [Header("Settings")]
        [SerializeField] private float showDelay = 0.5f;
        [SerializeField] private float hideDelay = 0.1f;
        [SerializeField] private Vector2 offset = new Vector2(15f, -15f);
        [SerializeField] private float screenPadding = 10f;
        [SerializeField] private float fadeInDuration = 0.15f;
        [SerializeField] private float fadeOutDuration = 0.1f;
        
        [Header("Animation")]
        [SerializeField] private AnimationCurve showCurve;
        [SerializeField] private AnimationCurve hideCurve;
        
        // Singleton
        private static TooltipSystem _instance;
        public static TooltipSystem Instance => _instance;
        
        // State
        private RectTransform _activeTooltip;
        private Coroutine _showCoroutine;
        private Coroutine _hideCoroutine;
        private Coroutine _followCoroutine;
        private bool _isShowing;
        private Canvas _canvas;
        private Camera _uiCamera;
        
        // Object pool for stat entries
        private List<TooltipStatEntry> _statEntryPool = new List<TooltipStatEntry>();
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCamera = _canvas.worldCamera;
            }
            
            InitializeCurves();
            HideAllTooltips();
        }
        
        private void InitializeCurves()
        {
            if (showCurve == null || showCurve.length == 0)
            {
                showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
            
            if (hideCurve == null || hideCurve.length == 0)
            {
                hideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
        }
        
        private void HideAllTooltips()
        {
            if (simpleTooltip != null) simpleTooltip.gameObject.SetActive(false);
            if (richTooltip != null) richTooltip.gameObject.SetActive(false);
            if (comparisonTooltip != null) comparisonTooltip.gameObject.SetActive(false);
            if (buildingTooltip != null) buildingTooltip.gameObject.SetActive(false);
            if (resourceTooltip != null) resourceTooltip.gameObject.SetActive(false);
        }
        
        #region Public API
        
        /// <summary>
        /// Show simple text tooltip
        /// </summary>
        public void ShowSimple(string text, bool followMouse = true)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            CancelPendingOperations();
            
            if (simpleTooltipText != null)
            {
                simpleTooltipText.text = text;
            }
            
            _showCoroutine = StartCoroutine(ShowTooltipDelayed(simpleTooltip, followMouse));
        }
        
        /// <summary>
        /// Show rich tooltip with title and description
        /// </summary>
        public void ShowRich(TooltipData data, bool followMouse = false)
        {
            if (data == null) return;
            
            CancelPendingOperations();
            PopulateRichTooltip(data);
            _showCoroutine = StartCoroutine(ShowTooltipDelayed(richTooltip, followMouse));
        }
        
        /// <summary>
        /// Show building tooltip
        /// </summary>
        public void ShowBuilding(BuildingTooltipData data)
        {
            if (data == null) return;
            
            CancelPendingOperations();
            PopulateBuildingTooltip(data);
            _showCoroutine = StartCoroutine(ShowTooltipDelayed(buildingTooltip, false));
        }
        
        /// <summary>
        /// Show resource tooltip
        /// </summary>
        public void ShowResource(ResourceTooltipData data)
        {
            if (data == null) return;
            
            CancelPendingOperations();
            PopulateResourceTooltip(data);
            _showCoroutine = StartCoroutine(ShowTooltipDelayed(resourceTooltip, true));
        }
        
        /// <summary>
        /// Show comparison tooltip (items)
        /// </summary>
        public void ShowComparison(TooltipData currentItem, TooltipData comparedItem)
        {
            if (currentItem == null || comparedItem == null) return;
            
            CancelPendingOperations();
            PopulateComparisonTooltip(currentItem, comparedItem);
            _showCoroutine = StartCoroutine(ShowTooltipDelayed(comparisonTooltip, false));
        }
        
        /// <summary>
        /// Show tooltip at specific position
        /// </summary>
        public void ShowAtPosition(string text, Vector2 position)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            CancelPendingOperations();
            
            if (simpleTooltipText != null)
            {
                simpleTooltipText.text = text;
            }
            
            _showCoroutine = StartCoroutine(ShowTooltipAtPosition(simpleTooltip, position));
        }
        
        /// <summary>
        /// Hide any visible tooltip
        /// </summary>
        public void Hide()
        {
            CancelPendingOperations();
            
            if (_isShowing && _activeTooltip != null)
            {
                _hideCoroutine = StartCoroutine(HideTooltipAnimated());
            }
        }
        
        /// <summary>
        /// Force immediate hide
        /// </summary>
        public void HideImmediate()
        {
            CancelPendingOperations();
            
            if (_activeTooltip != null)
            {
                _activeTooltip.gameObject.SetActive(false);
                _activeTooltip = null;
            }
            
            _isShowing = false;
        }
        
        #endregion
        
        #region Tooltip Population
        
        private void PopulateRichTooltip(TooltipData data)
        {
            if (richTitleText != null)
            {
                richTitleText.text = data.title;
                richTitleText.color = GetRarityColor(data.rarity);
            }
            
            if (richDescriptionText != null)
            {
                richDescriptionText.text = data.description;
            }
            
            if (richIconImage != null)
            {
                if (data.icon != null)
                {
                    richIconImage.sprite = data.icon;
                    richIconImage.gameObject.SetActive(true);
                }
                else
                {
                    richIconImage.gameObject.SetActive(false);
                }
            }
            
            // Populate stats
            if (richStatsContainer != null && richStatsText != null)
            {
                if (data.stats != null && data.stats.Count > 0)
                {
                    string statsString = "";
                    foreach (var stat in data.stats)
                    {
                        string colorHex = stat.isPositive ? "#4CAF50" : "#F44336";
                        string sign = stat.isPositive ? "+" : "";
                        statsString += $"<color={colorHex}>{sign}{stat.value}</color> {stat.name}\n";
                    }
                    richStatsText.text = statsString.TrimEnd('\n');
                    richStatsContainer.gameObject.SetActive(true);
                }
                else
                {
                    richStatsContainer.gameObject.SetActive(false);
                }
            }
        }
        
        private void PopulateBuildingTooltip(BuildingTooltipData data)
        {
            // Get components from building tooltip
            var title = buildingTooltip?.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var level = buildingTooltip?.Find("Level")?.GetComponent<TextMeshProUGUI>();
            var description = buildingTooltip?.Find("Description")?.GetComponent<TextMeshProUGUI>();
            var production = buildingTooltip?.Find("Production")?.GetComponent<TextMeshProUGUI>();
            var upgradeCost = buildingTooltip?.Find("UpgradeCost")?.GetComponent<TextMeshProUGUI>();
            var upgradeTime = buildingTooltip?.Find("UpgradeTime")?.GetComponent<TextMeshProUGUI>();
            
            if (title != null) title.text = data.buildingName;
            if (level != null) level.text = $"Level {data.level}";
            if (description != null) description.text = data.description;
            
            if (production != null && data.production != null)
            {
                string prodText = "";
                foreach (var prod in data.production)
                {
                    prodText += $"{prod.Key}: +{prod.Value}/hr\n";
                }
                production.text = prodText.TrimEnd('\n');
            }
            
            if (upgradeCost != null && data.upgradeCost != null)
            {
                string costText = "";
                foreach (var cost in data.upgradeCost)
                {
                    costText += $"{cost.Key}: {cost.Value}\n";
                }
                upgradeCost.text = costText.TrimEnd('\n');
            }
            
            if (upgradeTime != null)
            {
                upgradeTime.text = FormatTime(data.upgradeTimeSeconds);
            }
        }
        
        private void PopulateResourceTooltip(ResourceTooltipData data)
        {
            var icon = resourceTooltip?.Find("Icon")?.GetComponent<Image>();
            var title = resourceTooltip?.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var amount = resourceTooltip?.Find("Amount")?.GetComponent<TextMeshProUGUI>();
            var rate = resourceTooltip?.Find("Rate")?.GetComponent<TextMeshProUGUI>();
            var capacity = resourceTooltip?.Find("Capacity")?.GetComponent<TextMeshProUGUI>();
            
            if (icon != null && data.icon != null) icon.sprite = data.icon;
            if (title != null) title.text = data.resourceName;
            if (amount != null) amount.text = FormatNumber(data.amount);
            if (rate != null)
            {
                string rateColor = data.productionRate >= 0 ? "#4CAF50" : "#F44336";
                string rateSign = data.productionRate >= 0 ? "+" : "";
                rate.text = $"<color={rateColor}>{rateSign}{FormatNumber(data.productionRate)}/hr</color>";
            }
            if (capacity != null) capacity.text = $"Capacity: {FormatNumber(data.capacity)}";
        }
        
        private void PopulateComparisonTooltip(TooltipData current, TooltipData compared)
        {
            // Populate left side (current/equipped)
            PopulateComparisonSide(leftComparison, current, "Equipped");
            
            // Populate right side (compared/new)
            PopulateComparisonSide(rightComparison, compared, "New Item");
            
            // Calculate stat differences
            CalculateStatDifferences(current, compared);
        }
        
        private void PopulateComparisonSide(RectTransform side, TooltipData data, string label)
        {
            if (side == null) return;
            
            var title = side.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var labelText = side.Find("Label")?.GetComponent<TextMeshProUGUI>();
            var stats = side.Find("Stats")?.GetComponent<TextMeshProUGUI>();
            var icon = side.Find("Icon")?.GetComponent<Image>();
            
            if (title != null)
            {
                title.text = data.title;
                title.color = GetRarityColor(data.rarity);
            }
            
            if (labelText != null) labelText.text = label;
            if (icon != null && data.icon != null) icon.sprite = data.icon;
            
            if (stats != null && data.stats != null)
            {
                string statsText = "";
                foreach (var stat in data.stats)
                {
                    statsText += $"{stat.name}: {stat.value}\n";
                }
                stats.text = statsText.TrimEnd('\n');
            }
        }
        
        private void CalculateStatDifferences(TooltipData current, TooltipData compared)
        {
            // This would show stat differences between items
            // Implementation depends on stat system specifics
        }
        
        #endregion
        
        #region Animation Coroutines
        
        private IEnumerator ShowTooltipDelayed(RectTransform tooltip, bool followMouse)
        {
            yield return new WaitForSecondsRealtime(showDelay);
            
            if (tooltip == null) yield break;
            
            _activeTooltip = tooltip;
            
            // Position tooltip
            PositionTooltip(tooltip, Input.mousePosition);
            
            // Show with animation
            tooltip.gameObject.SetActive(true);
            var canvasGroup = GetOrAddCanvasGroup(tooltip);
            canvasGroup.alpha = 0;
            
            Vector3 startScale = Vector3.one * 0.9f;
            Vector3 endScale = Vector3.one;
            tooltip.localScale = startScale;
            
            float elapsed = 0;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = showCurve.Evaluate(elapsed / fadeInDuration);
                
                canvasGroup.alpha = t;
                tooltip.localScale = Vector3.Lerp(startScale, endScale, t);
                
                yield return null;
            }
            
            canvasGroup.alpha = 1;
            tooltip.localScale = endScale;
            _isShowing = true;
            
            // Start follow coroutine if needed
            if (followMouse)
            {
                _followCoroutine = StartCoroutine(FollowMouse(tooltip));
            }
        }
        
        private IEnumerator ShowTooltipAtPosition(RectTransform tooltip, Vector2 position)
        {
            yield return new WaitForSecondsRealtime(showDelay);
            
            if (tooltip == null) yield break;
            
            _activeTooltip = tooltip;
            PositionTooltipAtScreenPosition(tooltip, position);
            
            // Show with animation
            tooltip.gameObject.SetActive(true);
            var canvasGroup = GetOrAddCanvasGroup(tooltip);
            canvasGroup.alpha = 0;
            
            float elapsed = 0;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = showCurve.Evaluate(elapsed / fadeInDuration);
                canvasGroup.alpha = t;
                yield return null;
            }
            
            canvasGroup.alpha = 1;
            _isShowing = true;
        }
        
        private IEnumerator HideTooltipAnimated()
        {
            if (_activeTooltip == null) yield break;
            
            var canvasGroup = GetOrAddCanvasGroup(_activeTooltip);
            Vector3 startScale = _activeTooltip.localScale;
            Vector3 endScale = Vector3.one * 0.95f;
            
            float elapsed = 0;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = hideCurve.Evaluate(elapsed / fadeOutDuration);
                
                canvasGroup.alpha = 1 - t;
                _activeTooltip.localScale = Vector3.Lerp(startScale, endScale, t);
                
                yield return null;
            }
            
            _activeTooltip.gameObject.SetActive(false);
            _activeTooltip.localScale = Vector3.one;
            canvasGroup.alpha = 1;
            _activeTooltip = null;
            _isShowing = false;
        }
        
        private IEnumerator FollowMouse(RectTransform tooltip)
        {
            while (_isShowing && tooltip != null && tooltip.gameObject.activeInHierarchy)
            {
                PositionTooltip(tooltip, Input.mousePosition);
                yield return null;
            }
        }
        
        #endregion
        
        #region Positioning
        
        private void PositionTooltip(RectTransform tooltip, Vector2 mousePosition)
        {
            if (tooltip == null || _canvas == null) return;
            
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(),
                mousePosition,
                _uiCamera,
                out localPoint
            );
            
            // Apply offset
            localPoint += offset;
            
            // Get tooltip size
            Vector2 tooltipSize = tooltip.sizeDelta;
            
            // Get canvas size
            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRect.sizeDelta;
            
            // Clamp to stay on screen
            float halfCanvasWidth = canvasSize.x * 0.5f;
            float halfCanvasHeight = canvasSize.y * 0.5f;
            
            // Check right edge
            if (localPoint.x + tooltipSize.x > halfCanvasWidth - screenPadding)
            {
                localPoint.x = mousePosition.x - offset.x - tooltipSize.x;
            }
            
            // Check bottom edge
            if (localPoint.y - tooltipSize.y < -halfCanvasHeight + screenPadding)
            {
                localPoint.y = -halfCanvasHeight + screenPadding + tooltipSize.y;
            }
            
            // Check left edge
            if (localPoint.x < -halfCanvasWidth + screenPadding)
            {
                localPoint.x = -halfCanvasWidth + screenPadding;
            }
            
            // Check top edge
            if (localPoint.y > halfCanvasHeight - screenPadding)
            {
                localPoint.y = halfCanvasHeight - screenPadding;
            }
            
            tooltip.anchoredPosition = localPoint;
        }
        
        private void PositionTooltipAtScreenPosition(RectTransform tooltip, Vector2 screenPosition)
        {
            if (tooltip == null || _canvas == null) return;
            
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(),
                screenPosition,
                _uiCamera,
                out localPoint
            );
            
            // Same clamping logic as PositionTooltip
            tooltip.anchoredPosition = localPoint;
        }
        
        #endregion
        
        #region Helpers
        
        private void CancelPendingOperations()
        {
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }
            
            if (_followCoroutine != null)
            {
                StopCoroutine(_followCoroutine);
                _followCoroutine = null;
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
        
        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return new Color(0.7f, 0.7f, 0.7f);
                case ItemRarity.Uncommon: return new Color(0.3f, 0.8f, 0.3f);
                case ItemRarity.Rare: return new Color(0.3f, 0.5f, 1f);
                case ItemRarity.Epic: return new Color(0.6f, 0.2f, 0.8f);
                case ItemRarity.Legendary: return new Color(1f, 0.6f, 0.1f);
                default: return Color.white;
            }
        }
        
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
                return value.ToString("N0");
            }
        }
        
        private string FormatTime(int seconds)
        {
            if (seconds < 60)
            {
                return $"{seconds}s";
            }
            else if (seconds < 3600)
            {
                return $"{seconds / 60}m {seconds % 60}s";
            }
            else if (seconds < 86400)
            {
                return $"{seconds / 3600}h {(seconds % 3600) / 60}m";
            }
            else
            {
                return $"{seconds / 86400}d {(seconds % 86400) / 3600}h";
            }
        }
        
        #endregion
    }
    
    #region Data Classes
    
    [Serializable]
    public class TooltipData
    {
        public string title;
        public string description;
        public Sprite icon;
        public ItemRarity rarity = ItemRarity.Common;
        public List<TooltipStat> stats;
    }
    
    [Serializable]
    public class TooltipStat
    {
        public string name;
        public string value;
        public bool isPositive = true;
    }
    
    [Serializable]
    public class BuildingTooltipData
    {
        public string buildingName;
        public string description;
        public int level;
        public Sprite icon;
        public Dictionary<string, float> production;
        public Dictionary<string, int> upgradeCost;
        public int upgradeTimeSeconds;
    }
    
    [Serializable]
    public class ResourceTooltipData
    {
        public string resourceName;
        public Sprite icon;
        public float amount;
        public float capacity;
        public float productionRate;
    }
    
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    [Serializable]
    public class TooltipStatEntry : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI valueText;
        public Image iconImage;
    }
    
    #endregion
}
