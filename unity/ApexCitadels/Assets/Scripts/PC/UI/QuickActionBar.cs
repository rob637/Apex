using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Quick action toolbar for common combat/building actions.
    /// Positioned at bottom center for easy access.
    /// </summary>
    public class QuickActionBar : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float cooldownDuration = 5f;
        
        // UI
        private GameObject _barPanel;
        private List<QuickActionButton> _actionButtons = new List<QuickActionButton>();
        
        // State
        private Dictionary<string, float> _cooldowns = new Dictionary<string, float>();
        
        public static QuickActionBar Instance { get; private set; }
        
        public event Action<string> OnActionTriggered;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            CreateActionBar();
        }

        private void Update()
        {
            UpdateCooldowns();
            HandleHotkeys();
        }

        /// <summary>
        /// Trigger an action by ID
        /// </summary>
        public bool TriggerAction(string actionId)
        {
            if (IsOnCooldown(actionId))
            {
                ApexLogger.Log($"[QuickAction] {actionId} is on cooldown", ApexLogger.LogCategory.UI);
                return false;
            }
            
            // Start cooldown
            _cooldowns[actionId] = Time.time;
            
            // Fire event
            OnActionTriggered?.Invoke(actionId);
            
            // Execute action
            ExecuteAction(actionId);
            
            return true;
        }

        /// <summary>
        /// Check if action is on cooldown
        /// </summary>
        public bool IsOnCooldown(string actionId)
        {
            if (!_cooldowns.TryGetValue(actionId, out float startTime))
                return false;
            
            return Time.time - startTime < cooldownDuration;
        }

        /// <summary>
        /// Get remaining cooldown time
        /// </summary>
        public float GetCooldownRemaining(string actionId)
        {
            if (!_cooldowns.TryGetValue(actionId, out float startTime))
                return 0;
            
            float elapsed = Time.time - startTime;
            return Mathf.Max(0, cooldownDuration - elapsed);
        }

        private void CreateActionBar()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main bar at bottom center
            _barPanel = new GameObject("QuickActionBar");
            _barPanel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _barPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 10);
            rect.sizeDelta = new Vector2(500, 70);
            
            // Background
            Image bg = _barPanel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
            
            // Layout
            HorizontalLayoutGroup layout = _barPanel.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 8;
            layout.padding = new RectOffset(10, 10, 8, 8);
            
            // Create action buttons (using ASCII-friendly text icons)
            CreateActionButton("attack", "ATK", "Attack", KeyCode.Alpha1, "Launch attack on target");
            CreateActionButton("defend", "DEF", "Defend", KeyCode.Alpha2, "Fortify defenses");
            CreateActionButton("scout", "SCT", "Scout", KeyCode.Alpha3, "Send scouts");
            CreateActionButton("build", "BLD", "Build", KeyCode.Alpha4, "Open build menu");
            CreateActionButton("special", "SPL", "Special", KeyCode.Alpha5, "Use special ability");
            CreateActionButton("rally", "RLY", "Rally", KeyCode.Alpha6, "Rally troops");
        }

        private void CreateActionButton(string id, string icon, string label, KeyCode hotkey, string tooltip)
        {
            GameObject btnObj = new GameObject($"Action_{id}");
            btnObj.transform.SetParent(_barPanel.transform, false);
            
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(70, 54);
            
            // Background
            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.25f, 0.35f);
            
            // Button
            Button btn = btnObj.AddComponent<Button>();
            string actionId = id; // Capture for closure
            btn.onClick.AddListener(() => TriggerAction(actionId));
            
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.4f, 0.5f);
            colors.pressedColor = new Color(0.25f, 0.35f, 0.45f);
            colors.disabledColor = new Color(0.15f, 0.15f, 0.2f);
            btn.colors = colors;
            
            // Vertical layout
            VerticalLayoutGroup vlayout = btnObj.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 2;
            vlayout.padding = new RectOffset(4, 4, 4, 4);
            
            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            
            TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
            iconText.text = icon;
            iconText.fontSize = 22;
            iconText.alignment = TextAlignmentOptions.Center;
            
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredHeight = 26;
            
            // Label + hotkey
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);
            
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            int hotkeyNum = hotkey - KeyCode.Alpha0;
            labelText.text = $"{label} [{hotkeyNum}]";
            labelText.fontSize = 9;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.7f, 0.7f, 0.7f);
            
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredHeight = 12;
            
            // Cooldown overlay
            GameObject cooldownObj = new GameObject("Cooldown");
            cooldownObj.transform.SetParent(btnObj.transform, false);
            
            RectTransform cdRect = cooldownObj.AddComponent<RectTransform>();
            cdRect.anchorMin = Vector2.zero;
            cdRect.anchorMax = Vector2.one;
            cdRect.offsetMin = Vector2.zero;
            cdRect.offsetMax = Vector2.zero;
            
            Image cdImg = cooldownObj.AddComponent<Image>();
            cdImg.color = new Color(0, 0, 0, 0.7f);
            cdImg.fillMethod = Image.FillMethod.Radial360;
            cdImg.fillOrigin = (int)Image.Origin360.Top;
            cdImg.fillClockwise = false;
            cdImg.type = Image.Type.Filled;
            cdImg.fillAmount = 0;
            
            // Cooldown text
            GameObject cdTextObj = new GameObject("CooldownText");
            cdTextObj.transform.SetParent(cooldownObj.transform, false);
            
            RectTransform cdTextRect = cdTextObj.AddComponent<RectTransform>();
            cdTextRect.anchorMin = Vector2.zero;
            cdTextRect.anchorMax = Vector2.one;
            cdTextRect.offsetMin = Vector2.zero;
            cdTextRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI cdText = cdTextObj.AddComponent<TextMeshProUGUI>();
            cdText.text = "";
            cdText.fontSize = 18;
            cdText.fontStyle = FontStyles.Bold;
            cdText.alignment = TextAlignmentOptions.Center;
            cdText.color = Color.white;
            
            // Store reference
            _actionButtons.Add(new QuickActionButton
            {
                Id = id,
                Button = btn,
                Hotkey = hotkey,
                CooldownImage = cdImg,
                CooldownText = cdText,
                Tooltip = tooltip
            });
        }

        private void UpdateCooldowns()
        {
            foreach (var action in _actionButtons)
            {
                float remaining = GetCooldownRemaining(action.Id);
                bool onCooldown = remaining > 0;
                
                // Update cooldown visual
                if (action.CooldownImage != null)
                {
                    action.CooldownImage.fillAmount = remaining / cooldownDuration;
                }
                
                // Update cooldown text
                if (action.CooldownText != null)
                {
                    action.CooldownText.text = onCooldown ? Mathf.CeilToInt(remaining).ToString() : "";
                }
                
                // Enable/disable button
                if (action.Button != null)
                {
                    action.Button.interactable = !onCooldown;
                }
            }
        }

        private void HandleHotkeys()
        {
            foreach (var action in _actionButtons)
            {
                if (Input.GetKeyDown(action.Hotkey))
                {
                    TriggerAction(action.Id);
                }
            }
        }

        private void ExecuteAction(string actionId)
        {
            switch (actionId)
            {
                case "attack":
                    ApexLogger.Log("[QuickAction] Attack mode - select target territory", ApexLogger.LogCategory.UI);
                    // Would enable attack targeting mode
                    break;
                    
                case "defend":
                    ApexLogger.Log("[QuickAction] Fortifying defenses...", ApexLogger.LogCategory.UI);
                    // Would open defense options
                    break;
                    
                case "scout":
                    ApexLogger.Log("[QuickAction] Sending scouts...", ApexLogger.LogCategory.UI);
                    // Would send scouts to reveal territory
                    break;
                    
                case "build":
                    ApexLogger.Log("[QuickAction] Opening build menu", ApexLogger.LogCategory.UI);
                    // Open build menu
                    if (PCUIManager.Instance != null)
                    {
                        PCUIManager.Instance.TogglePanel(PCUIPanel.BuildMenu);
                    }
                    break;
                    
                case "special":
                    ApexLogger.Log("[QuickAction] Special ability activated!", ApexLogger.LogCategory.UI);
                    // Use special ability
                    if (SeasonPassPanel.Instance != null)
                    {
                        // Grant bonus XP for using abilities
                        SeasonPassPanel.Instance.AddXP(50);
                    }
                    break;
                    
                case "rally":
                    ApexLogger.Log("[QuickAction] Rally point set!", ApexLogger.LogCategory.UI);
                    // Set rally point for troops
                    if (MiniMapPanel.Instance != null && Camera.main != null)
                    {
                        MiniMapPanel.Instance.AddPing(Camera.main.transform.position, Color.green, "Rally");
                    }
                    break;
            }
            
            // Add activity to feed
            if (ActivityFeedPanel.Instance != null)
            {
                // ActivityFeedPanel.Instance.AddActivity(...)
            }
        }

        /// <summary>
        /// Set cooldown duration for an action
        /// </summary>
        public void SetCooldown(string actionId, float duration)
        {
            var action = _actionButtons.Find(a => a.Id == actionId);
            if (action != null)
            {
                // Would store per-action cooldown durations
            }
        }

        /// <summary>
        /// Reset cooldown for an action
        /// </summary>
        public void ResetCooldown(string actionId)
        {
            _cooldowns.Remove(actionId);
        }
    }

    public class QuickActionButton
    {
        public string Id;
        public Button Button;
        public KeyCode Hotkey;
        public Image CooldownImage;
        public TextMeshProUGUI CooldownText;
        public string Tooltip;
    }
}
