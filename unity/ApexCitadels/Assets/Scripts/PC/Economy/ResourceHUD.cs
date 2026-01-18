// ============================================================================
// APEX CITADELS - RESOURCE HUD DISPLAY
// Always-visible resource bar with animated updates and tooltips
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace ApexCitadels.PC.Economy
{
    /// <summary>
    /// HUD display for player resources with animations
    /// </summary>
    public class ResourceHUD : MonoBehaviour
    {
        public static ResourceHUD Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private bool showAllResources = false; // Show only non-zero when false
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private bool showChangePopups = true;

        // UI References
        private Canvas parentCanvas;
        private GameObject hudRoot;
        private RectTransform hudPanel;
        private Dictionary<ResourceType, ResourceSlot> resourceSlots = new Dictionary<ResourceType, ResourceSlot>();

        // Change popup pool
        private List<GameObject> popupPool = new List<GameObject>();

        // Colors
        private readonly Color PANEL_BG = new Color(0.08f, 0.1f, 0.12f, 0.85f);
        private readonly Color SLOT_BG = new Color(0.12f, 0.15f, 0.18f, 0.9f);
        private readonly Color TEXT_PRIMARY = new Color(0.95f, 0.95f, 0.95f, 1f);
        private readonly Color TEXT_SECONDARY = new Color(0.7f, 0.7f, 0.7f, 1f);
        private readonly Color POSITIVE_CHANGE = new Color(0.4f, 0.9f, 0.5f, 1f);
        private readonly Color NEGATIVE_CHANGE = new Color(0.9f, 0.4f, 0.4f, 1f);
        private readonly Color BAR_BG = new Color(0.2f, 0.2f, 0.2f, 1f);
        private readonly Color BAR_FILL = new Color(0.3f, 0.6f, 0.4f, 1f);

        // Resource icons and colors
        private readonly Dictionary<ResourceType, string> resourceIcons = new Dictionary<ResourceType, string>
        {
            { ResourceType.Stone, "🪨" },
            { ResourceType.Wood, "🪵" },
            { ResourceType.Iron, "🔩" },
            { ResourceType.Crystal, "💎" },
            { ResourceType.ArcaneEssence, "✨" },
            { ResourceType.Gems, "💠" },
            { ResourceType.Gold, "🪙" },
            { ResourceType.Food, "🍖" },
            { ResourceType.Energy, "⚡" }
        };

        private readonly Dictionary<ResourceType, Color> resourceColors = new Dictionary<ResourceType, Color>
        {
            { ResourceType.Stone, new Color(0.5f, 0.5f, 0.5f) },
            { ResourceType.Wood, new Color(0.6f, 0.4f, 0.2f) },
            { ResourceType.Iron, new Color(0.4f, 0.4f, 0.5f) },
            { ResourceType.Crystal, new Color(0.5f, 0.7f, 0.9f) },
            { ResourceType.ArcaneEssence, new Color(0.7f, 0.5f, 0.9f) },
            { ResourceType.Gems, new Color(0.3f, 0.8f, 0.5f) },
            { ResourceType.Gold, new Color(0.9f, 0.8f, 0.3f) },
            { ResourceType.Food, new Color(0.8f, 0.5f, 0.3f) },
            { ResourceType.Energy, new Color(0.4f, 0.7f, 0.9f) }
        };

        private class ResourceSlot
        {
            public GameObject Root;
            public TextMeshProUGUI IconText;
            public TextMeshProUGUI AmountText;
            public TextMeshProUGUI CapacityText;
            public Image FillBar;
            public int DisplayedAmount;
            public int TargetAmount;
            public float AnimationProgress;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            parentCanvas = FindFirstObjectByType<Canvas>();
            if (parentCanvas != null)
            {
                CreateHUD();
                SubscribeToEvents();
                RefreshAllResources();
            }
        }

        private void Update()
        {
            AnimateResourceChanges();
        }

        #region UI Creation

        private void CreateHUD()
        {
            // Main HUD root (top of screen)
            hudRoot = new GameObject("ResourceHUD");
            hudRoot.transform.SetParent(parentCanvas.transform, false);
            hudPanel = hudRoot.AddComponent<RectTransform>();

            // Position at top-center
            hudPanel.anchorMin = new Vector2(0.5f, 1f);
            hudPanel.anchorMax = new Vector2(0.5f, 1f);
            hudPanel.pivot = new Vector2(0.5f, 1f);
            hudPanel.sizeDelta = new Vector2(800, 55);
            hudPanel.anchoredPosition = new Vector2(0, -5);

            Image bg = hudRoot.AddComponent<Image>();
            bg.color = PANEL_BG;

            // Horizontal layout
            HorizontalLayoutGroup layout = hudRoot.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // Create resource slots for main resources
            ResourceType[] displayOrder = new[]
            {
                ResourceType.Stone, ResourceType.Wood, ResourceType.Iron,
                ResourceType.Crystal, ResourceType.ArcaneEssence,
                ResourceType.Gold, ResourceType.Gems
            };

            foreach (var type in displayOrder)
            {
                CreateResourceSlot(type);
            }
        }

        private void CreateResourceSlot(ResourceType type)
        {
            GameObject slotObj = new GameObject($"Slot_{type}");
            slotObj.transform.SetParent(hudPanel, false);
            RectTransform slotRT = slotObj.AddComponent<RectTransform>();
            slotRT.sizeDelta = new Vector2(100, 45);

            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = SLOT_BG;

            var slot = new ResourceSlot { Root = slotObj };

            // Icon
            string icon = resourceIcons.TryGetValue(type, out string i) ? i : "?";
            slot.IconText = CreateText(slotRT, icon, 20);
            SetAnchors(slot.IconText.rectTransform, new Vector2(0, 0), new Vector2(0.3f, 1), 
                new Vector2(5, 0), new Vector2(0, 0));

            // Amount
            slot.AmountText = CreateText(slotRT, "0", 16, FontStyles.Bold);
            SetAnchors(slot.AmountText.rectTransform, new Vector2(0.3f, 0.4f), new Vector2(1, 1), 
                new Vector2(0, 0), new Vector2(-5, -2));
            slot.AmountText.alignment = TextAlignmentOptions.MidlineRight;

            // Capacity bar background
            GameObject barBg = new GameObject("BarBg");
            barBg.transform.SetParent(slotRT, false);
            RectTransform barBgRT = barBg.AddComponent<RectTransform>();
            SetAnchors(barBgRT, new Vector2(0.3f, 0.1f), new Vector2(0.95f, 0.35f), Vector2.zero, Vector2.zero);
            Image barBgImg = barBg.AddComponent<Image>();
            barBgImg.color = BAR_BG;

            // Capacity bar fill
            GameObject barFill = new GameObject("BarFill");
            barFill.transform.SetParent(barBgRT, false);
            RectTransform fillRT = barFill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0.5f, 1);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            slot.FillBar = barFill.AddComponent<Image>();
            slot.FillBar.color = resourceColors.TryGetValue(type, out Color c) ? c : BAR_FILL;

            // Tooltip
            AddTooltip(slotObj, type);

            resourceSlots[type] = slot;
        }

        private void AddTooltip(GameObject obj, ResourceType type)
        {
            var trigger = obj.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => ShowResourceTooltip(type));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => HideTooltip());
            trigger.triggers.Add(exitEntry);
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            if (ResourceSpendingManager.Instance != null)
            {
                ResourceSpendingManager.Instance.OnResourceChanged += HandleResourceChanged;
                ResourceSpendingManager.Instance.OnResourcesLoaded += RefreshAllResources;
            }
        }

        private void HandleResourceChanged(object sender, ResourceChangeEventArgs e)
        {
            if (resourceSlots.TryGetValue(e.Type, out var slot))
            {
                slot.TargetAmount = e.NewAmount;
                slot.AnimationProgress = 0f;

                // Show change popup
                if (showChangePopups && e.Delta != 0)
                {
                    ShowChangePopup(e.Type, e.Delta);
                }
            }
        }

        private void RefreshAllResources()
        {
            if (ResourceSpendingManager.Instance == null) return;

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (resourceSlots.TryGetValue(type, out var slot))
                {
                    int amount = ResourceSpendingManager.Instance.GetResource(type);
                    int max = ResourceSpendingManager.Instance.GetMaxResource(type);
                    
                    slot.DisplayedAmount = amount;
                    slot.TargetAmount = amount;
                    slot.AmountText.text = FormatNumber(amount);
                    
                    float fill = max > 0 ? (float)amount / max : 0;
                    slot.FillBar.rectTransform.anchorMax = new Vector2(fill, 1);
                }
            }
        }

        #endregion

        #region Animation

        private void AnimateResourceChanges()
        {
            foreach (var kvp in resourceSlots)
            {
                var slot = kvp.Value;
                
                if (slot.DisplayedAmount != slot.TargetAmount)
                {
                    slot.AnimationProgress += Time.deltaTime / animationDuration;
                    
                    if (slot.AnimationProgress >= 1f)
                    {
                        slot.DisplayedAmount = slot.TargetAmount;
                        slot.AnimationProgress = 1f;
                    }
                    else
                    {
                        float t = EaseOutCubic(slot.AnimationProgress);
                        slot.DisplayedAmount = Mathf.RoundToInt(
                            Mathf.Lerp(slot.DisplayedAmount, slot.TargetAmount, t));
                    }

                    slot.AmountText.text = FormatNumber(slot.DisplayedAmount);
                    
                    // Update fill bar
                    int max = ResourceSpendingManager.Instance?.GetMaxResource(kvp.Key) ?? 10000;
                    float fill = max > 0 ? (float)slot.DisplayedAmount / max : 0;
                    slot.FillBar.rectTransform.anchorMax = new Vector2(fill, 1);
                }
            }
        }

        private float EaseOutCubic(float t)
        {
            return 1 - Mathf.Pow(1 - t, 3);
        }

        #endregion

        #region Change Popups

        private void ShowChangePopup(ResourceType type, int delta)
        {
            if (!resourceSlots.TryGetValue(type, out var slot)) return;

            GameObject popup = GetPopupFromPool();
            popup.transform.SetParent(slot.Root.transform, false);
            
            RectTransform popupRT = popup.GetComponent<RectTransform>();
            popupRT.anchoredPosition = new Vector2(0, 30);

            TextMeshProUGUI text = popup.GetComponent<TextMeshProUGUI>();
            string sign = delta > 0 ? "+" : "";
            text.text = $"{sign}{delta}";
            text.color = delta > 0 ? POSITIVE_CHANGE : NEGATIVE_CHANGE;

            popup.SetActive(true);

            // Animate and hide
            StartCoroutine(AnimatePopup(popup));
        }

        private System.Collections.IEnumerator AnimatePopup(GameObject popup)
        {
            RectTransform rt = popup.GetComponent<RectTransform>();
            TextMeshProUGUI text = popup.GetComponent<TextMeshProUGUI>();
            Color startColor = text.color;
            Vector2 startPos = rt.anchoredPosition;

            float duration = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Move up
                rt.anchoredPosition = startPos + new Vector2(0, t * 40);

                // Fade out
                text.color = new Color(startColor.r, startColor.g, startColor.b, 1 - t);

                yield return null;
            }

            popup.SetActive(false);
            ReturnPopupToPool(popup);
        }

        private GameObject GetPopupFromPool()
        {
            foreach (var popup in popupPool)
            {
                if (!popup.activeInHierarchy)
                    return popup;
            }

            // Create new popup
            GameObject newPopup = new GameObject("ChangePopup");
            RectTransform rt = newPopup.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60, 25);

            TextMeshProUGUI text = newPopup.AddComponent<TextMeshProUGUI>();
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;

            popupPool.Add(newPopup);
            return newPopup;
        }

        private void ReturnPopupToPool(GameObject popup)
        {
            popup.transform.SetParent(hudRoot.transform, false);
        }

        #endregion

        #region Tooltip

        private GameObject tooltipObj;
        private TextMeshProUGUI tooltipText;

        private void ShowResourceTooltip(ResourceType type)
        {
            if (ResourceSpendingManager.Instance == null) return;

            int current = ResourceSpendingManager.Instance.GetResource(type);
            int max = ResourceSpendingManager.Instance.GetMaxResource(type);
            float rate = ResourceSpendingManager.Instance.GetGenerationRate(type);

            string tooltip = $"{type}\n\n";
            tooltip += $"Amount: {current:N0} / {max:N0}\n";
            
            if (rate > 0)
            {
                tooltip += $"Generation: +{rate:F1}/min";
            }
            else
            {
                tooltip += "No passive generation";
            }

            ShowTooltip(tooltip);
        }

        private void ShowTooltip(string text)
        {
            if (tooltipObj == null)
            {
                tooltipObj = new GameObject("Tooltip");
                tooltipObj.transform.SetParent(parentCanvas.transform, false);
                
                RectTransform rt = tooltipObj.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(180, 80);
                rt.pivot = new Vector2(0.5f, 1);

                Image bg = tooltipObj.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

                tooltipText = CreateText(rt, "", 11);
                SetAnchors(tooltipText.rectTransform, Vector2.zero, Vector2.one, 
                    new Vector2(8, 5), new Vector2(-8, -5));
                tooltipText.alignment = TextAlignmentOptions.TopLeft;
            }

            tooltipText.text = text;
            tooltipObj.SetActive(true);

            // Position near mouse
            Vector2 mousePos = Input.mousePosition;
            tooltipObj.GetComponent<RectTransform>().position = mousePos + new Vector2(0, -10);
        }

        private void HideTooltip()
        {
            if (tooltipObj != null)
                tooltipObj.SetActive(false);
        }

        #endregion

        #region Utility

        private string FormatNumber(int amount)
        {
            if (amount >= 1000000)
                return $"{amount / 1000000f:F1}M";
            if (amount >= 10000)
                return $"{amount / 1000f:F1}K";
            return amount.ToString("N0");
        }

        private TextMeshProUGUI CreateText(RectTransform parent, string text, int fontSize, FontStyles style = FontStyles.Normal)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = TEXT_PRIMARY;
            tmp.alignment = TextAlignmentOptions.Center;

            return tmp;
        }

        private void SetAnchors(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        #endregion

        #region Visibility

        public void Show()
        {
            if (hudRoot != null)
                hudRoot.SetActive(true);
        }

        public void Hide()
        {
            if (hudRoot != null)
                hudRoot.SetActive(false);
        }

        public void SetCompactMode(bool compact)
        {
            // Compact mode shows only icons, no bars
            foreach (var slot in resourceSlots.Values)
            {
                if (slot.FillBar != null)
                    slot.FillBar.transform.parent.gameObject.SetActive(!compact);
            }
        }

        #endregion
    }
}
