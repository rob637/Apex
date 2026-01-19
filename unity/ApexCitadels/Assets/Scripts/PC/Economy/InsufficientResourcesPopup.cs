// ============================================================================
// APEX CITADELS - INSUFFICIENT RESOURCES POPUP
// Shows what's missing when player can't afford something
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using ApexCitadels.Data;
using ApexCitadels.Core;
using ApexCitadels.UI;

namespace ApexCitadels.PC.Economy
{
    /// <summary>
    /// Popup showing missing resources when purchase fails
    /// </summary>
    public class InsufficientResourcesPopup : MonoBehaviour
    {
        public static InsufficientResourcesPopup Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private bool showShortcutToShop = true;

        // UI References
        private Canvas parentCanvas;
        private GameObject popupRoot;
        private RectTransform popupPanel;
        private TextMeshProUGUI titleText;
        private RectTransform missingContainer;
        private Button closeButton;
        private Button shopButton;

        // Animation
        private float displayTimer;
        private bool isShowing = false;
        private Vector2 targetPosition;
        private Vector2 startPosition;
        private float animProgress;

        // Colors
        private readonly Color PANEL_BG = new Color(0.12f, 0.08f, 0.08f, 0.95f);
        private readonly Color HEADER_BG = new Color(0.6f, 0.2f, 0.2f, 1f);
        private readonly Color TEXT_PRIMARY = new Color(0.95f, 0.95f, 0.95f, 1f);
        private readonly Color TEXT_WARNING = new Color(1f, 0.7f, 0.4f, 1f);
        private readonly Color MISSING_COLOR = new Color(0.9f, 0.4f, 0.4f, 1f);

        // Resource icons
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
                CreatePopup();
                SubscribeToEvents();
                Hide();
            }
        }

        private void Update()
        {
            if (isShowing)
            {
                // Auto-hide timer
                displayTimer -= Time.deltaTime;
                if (displayTimer <= 0)
                {
                    Hide();
                }

                // Animation
                if (animProgress < 1f)
                {
                    animProgress += Time.deltaTime * 4f;
                    float t = EaseOutBack(Mathf.Min(animProgress, 1f));
                    popupPanel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                }
            }
        }

        #region UI Creation

        private void CreatePopup()
        {
            // Main popup
            popupRoot = new GameObject("InsufficientResourcesPopup");
            popupRoot.transform.SetParent(parentCanvas.transform, false);
            popupPanel = popupRoot.AddComponent<RectTransform>();

            // Center of screen
            popupPanel.anchorMin = new Vector2(0.5f, 0.5f);
            popupPanel.anchorMax = new Vector2(0.5f, 0.5f);
            popupPanel.sizeDelta = new Vector2(350, 250);
            targetPosition = Vector2.zero;
            startPosition = new Vector2(0, -50);
            popupPanel.anchoredPosition = startPosition;

            Image bg = popupRoot.AddComponent<Image>();
            bg.color = PANEL_BG;

            // Header
            CreateHeader();

            // Missing resources container
            CreateMissingContainer();

            // Buttons
            CreateButtons();
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(popupPanel, false);
            RectTransform headerRT = header.AddComponent<RectTransform>();
            SetAnchors(headerRT, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, 0));

            Image headerBg = header.AddComponent<Image>();
            headerBg.color = HEADER_BG;

            // Warning icon + title
            titleText = CreateText(headerRT, "⚠️ INSUFFICIENT RESOURCES", 16, FontStyles.Bold);
            SetAnchors(titleText.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-40, 0));
            titleText.alignment = TextAlignmentOptions.MidlineLeft;

            // Close button
            closeButton = CreateButton(headerRT, "✕", Hide);
            SetAnchors(closeButton.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 1), 
                new Vector2(-40, 5), new Vector2(-5, -5));
        }

        private void CreateMissingContainer()
        {
            GameObject container = new GameObject("MissingContainer");
            container.transform.SetParent(popupPanel, false);
            missingContainer = container.AddComponent<RectTransform>();
            SetAnchors(missingContainer, new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 60), new Vector2(-10, -55));

            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void CreateButtons()
        {
            GameObject buttonContainer = new GameObject("Buttons");
            buttonContainer.transform.SetParent(popupPanel, false);
            RectTransform buttonsRT = buttonContainer.AddComponent<RectTransform>();
            SetAnchors(buttonsRT, new Vector2(0, 0), new Vector2(1, 0), new Vector2(10, 10), new Vector2(-10, 55));

            HorizontalLayoutGroup layout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Shop button
            if (showShortcutToShop)
            {
                shopButton = CreateActionButton(buttonsRT, "💎 GET RESOURCES", OpenShop, 
                    new Color(0.3f, 0.5f, 0.4f));
            }

            // OK button
            CreateActionButton(buttonsRT, "OK", Hide, new Color(0.3f, 0.3f, 0.35f));
        }

        private Button CreateButton(RectTransform parent, string text, Action onClick)
        {
            GameObject btnObj = new GameObject("Button");
            btnObj.transform.SetParent(parent, false);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            TextMeshProUGUI btnText = CreateText(btnObj.GetComponent<RectTransform>(), text, 14);
            SetAnchors(btnText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            btn.onClick.AddListener(() => onClick?.Invoke());

            return btn;
        }

        private Button CreateActionButton(RectTransform parent, string text, Action onClick, Color color)
        {
            GameObject btnObj = new GameObject("ActionButton");
            btnObj.transform.SetParent(parent, false);
            RectTransform btnRT = btnObj.AddComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(140, 35);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = color;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            btn.colors = colors;

            TextMeshProUGUI btnText = CreateText(btnRT, text, 13, FontStyles.Bold);
            SetAnchors(btnText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            btn.onClick.AddListener(() => onClick?.Invoke());

            return btn;
        }

        #endregion

        #region Show/Hide

        public void Show(ResourceCost required, ResourceCost missing, string itemName = "")
        {
            // Clear previous content
            ClearMissingContainer();

            // Add header text
            string headerText = string.IsNullOrEmpty(itemName) 
                ? "You don't have enough resources:" 
                : $"Can't afford {itemName}:";
            
            var headerLabel = CreateMissingRow("", headerText, false);
            headerLabel.GetComponent<TextMeshProUGUI>().color = TEXT_WARNING;

            // Add missing resources
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                int missingAmount = missing.GetAmount(type);
                int requiredAmount = required.GetAmount(type);
                int haveAmount = ResourceSpendingManager.Instance?.GetResource(type) ?? 0;

                if (requiredAmount > 0)
                {
                    string icon = resourceIcons.TryGetValue(type, out string i) ? i : "?";
                    string rowText = missingAmount > 0 
                        ? $"{icon} {type}: {haveAmount} / {requiredAmount} (need {missingAmount} more)"
                        : $"{icon} {type}: {haveAmount} / {requiredAmount} ✓";
                    
                    bool isMissing = missingAmount > 0;
                    CreateMissingRow(icon, rowText.Replace($"{icon} ", ""), isMissing);
                }
            }

            // Show and animate
            popupRoot.SetActive(true);
            isShowing = true;
            displayTimer = displayDuration;
            animProgress = 0f;
            popupPanel.anchoredPosition = startPosition;

            // Play sound
            // AudioManager.Instance?.PlaySFX("error");
        }

        public void Hide()
        {
            isShowing = false;
            if (popupRoot != null)
                popupRoot.SetActive(false);
        }

        private void ClearMissingContainer()
        {
            foreach (Transform child in missingContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private GameObject CreateMissingRow(string icon, string text, bool isMissing)
        {
            GameObject row = new GameObject("Row");
            row.transform.SetParent(missingContainer, false);
            RectTransform rowRT = row.AddComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(300, 25);

            TextMeshProUGUI rowText = CreateText(rowRT, text, 13);
            SetAnchors(rowText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rowText.alignment = TextAlignmentOptions.MidlineLeft;
            rowText.color = isMissing ? MISSING_COLOR : TEXT_PRIMARY;

            return row;
        }

        #endregion

        #region Events

        private void SubscribeToEvents()
        {
            if (ResourceSpendingManager.Instance != null)
            {
                ResourceSpendingManager.Instance.OnInsufficientResources += HandleInsufficientResources;
            }
        }

        private void HandleInsufficientResources(ResourceCost cost)
        {
            var missing = ResourceSpendingManager.Instance?.GetMissingResources(cost) ?? cost;
            Show(cost, missing);
        }

        private void OpenShop()
        {
            ApexLogger.Log(ApexLogger.LogCategory.Economy, "Opening IAP store...");
            
            // Find and open the IAP store UI
            var storeUI = UnityEngine.Object.FindObjectOfType<IAPStoreUI>();
            if (storeUI != null)
            {
                storeUI.OpenStore();
            }
            else
            {
                ApexLogger.LogWarning(ApexLogger.LogCategory.Economy, "IAPStoreUI not found in scene!");
            }
            
            Hide();
        }

        #endregion

        #region Utility

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
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
    }
}
