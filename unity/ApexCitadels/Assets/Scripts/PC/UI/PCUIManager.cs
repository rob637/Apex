using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Territory;
using ApexCitadels.Alliance;
using ApexCitadels.Resources;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Main UI manager for the PC client.
    /// Handles all UI panels and their visibility.
    /// </summary>
    public class PCUIManager : MonoBehaviour
    {
        public static PCUIManager Instance { get; private set; }

        [Header("Main Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject worldMapPanel;
        [SerializeField] private GameObject territoryDetailPanel;
        [SerializeField] private GameObject alliancePanel;
        [SerializeField] private GameObject buildMenuPanel;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject statisticsPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject chatPanel;

        [Header("HUD Elements")]
        [SerializeField] private GameObject resourceBar;
        [SerializeField] private GameObject minimap;
        [SerializeField] private GameObject notificationArea;
        [SerializeField] private GameObject tooltipPanel;

        [Header("Resource Display")]
        [SerializeField] private TextMeshProUGUI stoneText;
        [SerializeField] private TextMeshProUGUI woodText;
        [SerializeField] private TextMeshProUGUI metalText;
        [SerializeField] private TextMeshProUGUI crystalText;
        [SerializeField] private TextMeshProUGUI apexCoinsText;

        [Header("Navigation")]
        [SerializeField] private Button mapButton;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button allianceButton;
        [SerializeField] private Button craftingButton;
        [SerializeField] private Button marketButton;
        [SerializeField] private Button statsButton;
        [SerializeField] private Button profileButton;

        // Events
        public event Action<PCUIPanel> OnPanelOpened;
        public event Action<PCUIPanel> OnPanelClosed;

        // State
        private PCUIPanel _currentPanel = PCUIPanel.None;
        private Stack<PCUIPanel> _panelHistory = new Stack<PCUIPanel>();
        private Dictionary<PCUIPanel, GameObject> _panelMap;
        private bool _isTooltipVisible;
        private bool _inputBindingsSetup = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-create missing panels
            CreateMissingPanels();
            InitializePanelMap();
        }

        private void Start()
        {
            SetupInputBindings();
            SetupButtonListeners();

            // Start with world map open
            OpenPanel(PCUIPanel.WorldMap);
        }

        /// <summary>
        /// Creates placeholder panels for any that are not assigned
        /// </summary>
        private void CreateMissingPanels()
        {
            // Find or create canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                ApexLogger.LogError("[PCUI] No Canvas found!", ApexLogger.LogCategory.UI);
                return;
            }

            // Create panels that are null
            if (mainMenuPanel == null)
                mainMenuPanel = CreatePlaceholderPanel(canvas.transform, "MainMenu", "Main Menu", new Color(0.1f, 0.1f, 0.15f, 0.95f));
            if (worldMapPanel == null)
                worldMapPanel = CreateWorldMapHUD(canvas.transform); // Special minimal HUD for world map
            if (territoryDetailPanel == null)
                territoryDetailPanel = CreateTerritoryDetailPanel(canvas.transform);
            if (alliancePanel == null)
                alliancePanel = CreatePlaceholderPanel(canvas.transform, "Alliance", "Alliance", new Color(0.1f, 0.05f, 0.15f, 0.95f));
            if (buildMenuPanel == null)
                buildMenuPanel = CreatePlaceholderPanel(canvas.transform, "BuildMenu", "Build Menu", new Color(0.15f, 0.1f, 0.05f, 0.95f));
            if (inventoryPanel == null)
                inventoryPanel = CreatePlaceholderPanel(canvas.transform, "Inventory", "Inventory", new Color(0.1f, 0.1f, 0.05f, 0.95f));
            if (statisticsPanel == null)
                statisticsPanel = CreatePlaceholderPanel(canvas.transform, "Statistics", "Statistics", new Color(0.05f, 0.1f, 0.1f, 0.95f));
            if (settingsPanel == null)
                settingsPanel = CreatePlaceholderPanel(canvas.transform, "Settings", "Settings", new Color(0.1f, 0.1f, 0.1f, 0.95f));
            if (chatPanel == null)
                chatPanel = CreatePlaceholderPanel(canvas.transform, "Chat", "Chat", new Color(0.05f, 0.05f, 0.1f, 0.9f));

            ApexLogger.Log("[PCUI] Created placeholder panels for missing UI elements", ApexLogger.LogCategory.UI);
        }

        private GameObject CreatePlaceholderPanel(Transform parent, string name, string title, Color bgColor)
        {
            // Create panel
            GameObject panel = new GameObject(name + "Panel");
            panel.transform.SetParent(parent, false);

            // Add RectTransform and stretch to fill
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.1f);
            rect.anchorMax = new Vector2(0.85f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Add background image (SEMI-TRANSPARENT DARK)
            UnityEngine.UI.Image bg = panel.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0, 0, 0, 0.85f); // Darker and more opaque for popups

            // Create title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 60);
            titleRect.anchoredPosition = new Vector2(0, -10);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            // Create close button
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(panel.transform, false);
            RectTransform closeBtnRect = closeBtn.AddComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 1);
            closeBtnRect.anchorMax = new Vector2(1, 1);
            closeBtnRect.pivot = new Vector2(1, 1);
            closeBtnRect.sizeDelta = new Vector2(50, 50);
            closeBtnRect.anchoredPosition = new Vector2(-10, -10);

            UnityEngine.UI.Image closeBtnImg = closeBtn.AddComponent<UnityEngine.UI.Image>();
            closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            Button closeButton = closeBtn.AddComponent<Button>();
            closeButton.targetGraphic = closeBtnImg;

            // Add X text
            GameObject xText = new GameObject("X");
            xText.transform.SetParent(closeBtn.transform, false);
            RectTransform xRect = xText.AddComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
            xRect.offsetMin = Vector2.zero;
            xRect.offsetMax = Vector2.zero;

            TextMeshProUGUI xTmp = xText.AddComponent<TextMeshProUGUI>();
            xTmp.text = "X";
            xTmp.fontSize = 28;
            xTmp.alignment = TextAlignmentOptions.Center;
            xTmp.color = Color.white;

            // Create content area with placeholder text
            GameObject content = new GameObject("Content");
            content.transform.SetParent(panel.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.05f, 0.1f);
            contentRect.anchorMax = new Vector2(0.95f, 0.85f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            TextMeshProUGUI contentText = content.AddComponent<TextMeshProUGUI>();
            contentText.text = $"[{title} Panel]\n\nThis is a placeholder panel.\nPress ESC to close or click the X button.\n\nKeyboard shortcuts:\n• B - Build Menu\n• Tab - Alliance\n• M - World Map\n• I - Inventory\n• Esc - Close / Main Menu";
            contentText.fontSize = 20;
            contentText.alignment = TextAlignmentOptions.Center;
            contentText.color = new Color(0.8f, 0.8f, 0.8f, 1f);

            // Wire up close button
            string panelName = name;
            closeButton.onClick.AddListener(() => {
                if (Enum.TryParse<PCUIPanel>(panelName, out PCUIPanel p))
                    ClosePanel(p);
            });

            ApexLogger.Log($"[PCUI] Created placeholder panel: {name}", ApexLogger.LogCategory.UI);
            return panel;
        }

        /// <summary>
        /// Creates a minimal/invisible HUD for World Map mode (no blocking panel)
        /// </summary>
        private GameObject CreateWorldMapHUD(Transform parent)
        {
            // Create an empty panel that doesn't block the view
            GameObject panel = new GameObject("WorldMapHUD");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Important: Make sure this panel does NOT have an image that blocks raycasts
            // We only want the children (indicators) to be visible

            // Small indicator at bottom showing current mode
            GameObject indicator = new GameObject("ModeIndicator");
            indicator.transform.SetParent(panel.transform, false);
            RectTransform indRect = indicator.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0.5f, 0);
            indRect.anchorMax = new Vector2(0.5f, 0);
            indRect.pivot = new Vector2(0.5f, 0);
            indRect.sizeDelta = new Vector2(200, 30);
            indRect.anchoredPosition = new Vector2(0, 10);

            // Background
            UnityEngine.UI.Image indBg = indicator.AddComponent<UnityEngine.UI.Image>();
            indBg.color = new Color(0, 0, 0, 0.5f);

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(indicator.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "World Map View";
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            ApexLogger.Log("[PCUI] Created World Map HUD (minimal)", ApexLogger.LogCategory.UI);
            return panel;
        }

        /// <summary>
        /// Creates a functional Territory Detail panel with TerritoryDetailPanel component
        /// </summary>
        private GameObject CreateTerritoryDetailPanel(Transform parent)
        {
            GameObject panel = new GameObject("TerritoryDetailPanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.6f, 0.1f); // Right side of screen
            rect.anchorMax = new Vector2(0.98f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Background
            UnityEngine.UI.Image bg = panel.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.05f, 0.08f, 0.12f, 0.95f);

            // Create header area
            GameObject header = new GameObject("Header");
            header.transform.SetParent(panel.transform, false);
            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.85f);
            headerRect.anchorMax = new Vector2(1, 1f);
            headerRect.offsetMin = new Vector2(10, 5);
            headerRect.offsetMax = new Vector2(-10, -5);

            // Territory Name
            GameObject nameObj = new GameObject("TerritoryName");
            nameObj.transform.SetParent(header.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(0.8f, 1f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Territory Name";
            nameText.fontSize = 32;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = Color.white;

            // Level
            GameObject levelObj = new GameObject("Level");
            levelObj.transform.SetParent(header.transform, false);
            RectTransform levelRect = levelObj.AddComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0, 0);
            levelRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelRect.offsetMin = Vector2.zero;
            levelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI levelText = levelObj.AddComponent<TextMeshProUGUI>();
            levelText.text = "Level 1";
            levelText.fontSize = 20;
            levelText.color = new Color(1f, 0.9f, 0.4f);

            // Close button
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(header.transform, false);
            RectTransform closeBtnRect = closeBtn.AddComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(0.9f, 0.6f);
            closeBtnRect.anchorMax = new Vector2(1f, 1f);
            closeBtnRect.offsetMin = Vector2.zero;
            closeBtnRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image closeBtnImg = closeBtn.AddComponent<UnityEngine.UI.Image>();
            closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f);
            Button closeButton = closeBtn.AddComponent<Button>();
            closeButton.targetGraphic = closeBtnImg;
            closeButton.onClick.AddListener(() => ClosePanel(PCUIPanel.TerritoryDetail));

            GameObject xText = new GameObject("X");
            xText.transform.SetParent(closeBtn.transform, false);
            RectTransform xRect = xText.AddComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
            xRect.offsetMin = Vector2.zero;
            xRect.offsetMax = Vector2.zero;
            TextMeshProUGUI xTmp = xText.AddComponent<TextMeshProUGUI>();
            xTmp.text = "X";
            xTmp.fontSize = 24;
            xTmp.alignment = TextAlignmentOptions.Center;
            xTmp.color = Color.white;

            // Content area
            GameObject content = new GameObject("Content");
            content.transform.SetParent(panel.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.05f, 0.1f);
            contentRect.anchorMax = new Vector2(0.95f, 0.83f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            // Info text that will be updated
            GameObject infoObj = new GameObject("InfoText");
            infoObj.transform.SetParent(content.transform, false);
            RectTransform infoRect = infoObj.AddComponent<RectTransform>();
            infoRect.anchorMin = Vector2.zero;
            infoRect.anchorMax = Vector2.one;
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = Vector2.zero;
            TextMeshProUGUI infoText = infoObj.AddComponent<TextMeshProUGUI>();
            infoText.text = "Select a territory to view details.\n\nClick on any citadel on the map.";
            infoText.fontSize = 18;
            infoText.alignment = TextAlignmentOptions.TopLeft;
            infoText.color = new Color(0.85f, 0.85f, 0.85f);

            // Action buttons row
            GameObject buttons = new GameObject("ActionButtons");
            buttons.transform.SetParent(panel.transform, false);
            RectTransform buttonsRect = buttons.AddComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0.05f, 0.02f);
            buttonsRect.anchorMax = new Vector2(0.95f, 0.09f);
            buttonsRect.offsetMin = Vector2.zero;
            buttonsRect.offsetMax = Vector2.zero;

            // Create action buttons
            string[] buttonNames = { "Attack", "Defend", "Build", "Upgrade" };
            float buttonWidth = 1f / buttonNames.Length;
            for (int i = 0; i < buttonNames.Length; i++)
            {
                GameObject btnObj = new GameObject(buttonNames[i] + "Button");
                btnObj.transform.SetParent(buttons.transform, false);
                RectTransform btnRect = btnObj.AddComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(i * buttonWidth + 0.01f, 0);
                btnRect.anchorMax = new Vector2((i + 1) * buttonWidth - 0.01f, 1);
                btnRect.offsetMin = Vector2.zero;
                btnRect.offsetMax = Vector2.zero;

                UnityEngine.UI.Image btnImg = btnObj.AddComponent<UnityEngine.UI.Image>();
                btnImg.color = new Color(0.2f, 0.35f, 0.5f);
                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = btnImg;

                GameObject btnTextObj = new GameObject("Text");
                btnTextObj.transform.SetParent(btnObj.transform, false);
                RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
                btnTextRect.anchorMin = Vector2.zero;
                btnTextRect.anchorMax = Vector2.one;
                btnTextRect.offsetMin = Vector2.zero;
                btnTextRect.offsetMax = Vector2.zero;
                TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
                btnText.text = buttonNames[i];
                btnText.fontSize = 16;
                btnText.alignment = TextAlignmentOptions.Center;
                btnText.color = Color.white;
            }

            // Add TerritoryDetailPanel component and wire references
            TerritoryDetailPanel detailPanel = panel.AddComponent<TerritoryDetailPanel>();
            // Wire up serialized fields via reflection or let the component find them
            
            ApexLogger.Log("[PCUI] Created Territory Detail Panel with full layout", ApexLogger.LogCategory.UI);
            return panel;
        }

        private void Update()
        {
            // Retry input bindings if not set up yet
            if (!_inputBindingsSetup && PCInputManager.Instance != null)
            {
                SetupInputBindings();
            }
        }

        private void InitializePanelMap()
        {
            _panelMap = new Dictionary<PCUIPanel, GameObject>
            {
                { PCUIPanel.MainMenu, mainMenuPanel },
                { PCUIPanel.WorldMap, worldMapPanel },
                { PCUIPanel.TerritoryDetail, territoryDetailPanel },
                { PCUIPanel.Alliance, alliancePanel },
                { PCUIPanel.BuildMenu, buildMenuPanel },
                { PCUIPanel.Inventory, inventoryPanel },
                { PCUIPanel.Statistics, statisticsPanel },
                { PCUIPanel.Settings, settingsPanel },
                { PCUIPanel.Chat, chatPanel }
            };

            // Hide all panels initially
            foreach (var panel in _panelMap.Values)
            {
                if (panel != null)
                    panel.SetActive(false);
            }
        }

        private void SetupInputBindings()
        {
            if (PCInputManager.Instance == null)
            {
                ApexLogger.Log("[PCUI] PCInputManager not ready, will retry...", ApexLogger.LogCategory.UI);
                return;
            }

            if (_inputBindingsSetup) return;

            ApexLogger.Log("[PCUI] Setting up input bindings...", ApexLogger.LogCategory.UI);
            
            // ESC - Close current panel OR open main menu if on world map
            PCInputManager.Instance.OnOpenMenu += () => {
                ApexLogger.Log($"[PCUI] ESC pressed - current panel: {_currentPanel}", ApexLogger.LogCategory.UI);
                if (_currentPanel != PCUIPanel.WorldMap && _currentPanel != PCUIPanel.None)
                {
                    // Close current panel, return to world map
                    CloseCurrentPanel();
                }
                else
                {
                    // On world map, toggle main menu
                    TogglePanel(PCUIPanel.MainMenu);
                }
            };
            PCInputManager.Instance.OnOpenAlliancePanel += () => {
                ApexLogger.Log("[PCUI] TAB pressed - toggling Alliance", ApexLogger.LogCategory.UI);
                TogglePanel(PCUIPanel.Alliance);
            };
            PCInputManager.Instance.OnOpenBuildingMenu += () => {
                ApexLogger.Log("[PCUI] B pressed - toggling BuildMenu", ApexLogger.LogCategory.UI);
                TogglePanel(PCUIPanel.BuildMenu);
            };
            PCInputManager.Instance.OnOpenInventory += () => {
                ApexLogger.Log("[PCUI] I pressed - toggling Inventory", ApexLogger.LogCategory.UI);
                TogglePanel(PCUIPanel.Inventory);
            };
            PCInputManager.Instance.OnOpenWorldMap += () => {
                ApexLogger.Log("[PCUI] M pressed - opening WorldMap", ApexLogger.LogCategory.UI);
                OpenPanel(PCUIPanel.WorldMap);
            };

            _inputBindingsSetup = true;
            ApexLogger.Log("[PCUI] Input bindings setup complete!", ApexLogger.LogCategory.UI);
        }

        private void SetupButtonListeners()
        {
            if (mapButton != null)
                mapButton.onClick.AddListener(() => OpenPanel(PCUIPanel.WorldMap));
            if (buildButton != null)
                buildButton.onClick.AddListener(() => TogglePanel(PCUIPanel.BuildMenu));
            if (allianceButton != null)
                allianceButton.onClick.AddListener(() => TogglePanel(PCUIPanel.Alliance));
            if (statsButton != null)
                statsButton.onClick.AddListener(() => TogglePanel(PCUIPanel.Statistics));
        }

        #region Panel Management

        /// <summary>
        /// Open a UI panel (auto-closes any currently open panel first)
        /// </summary>
        public void OpenPanel(PCUIPanel panel)
        {
            if (_currentPanel == panel) return;

            // Close current panel first (hide it)
            if (_currentPanel != PCUIPanel.None)
            {
                if (_panelMap.TryGetValue(_currentPanel, out GameObject currentObj) && currentObj != null)
                {
                    currentObj.SetActive(false);
                    ApexLogger.Log($"[PCUI] Auto-closed panel: {_currentPanel}", ApexLogger.LogCategory.UI);
                }
            }

            // Open new panel
            if (_panelMap.TryGetValue(panel, out GameObject panelObj) && panelObj != null)
            {
                panelObj.SetActive(true);
                _currentPanel = panel;
                OnPanelOpened?.Invoke(panel);
                ApexLogger.Log($"[PCUI] Opened panel: {panel}", ApexLogger.LogCategory.UI);
            }
        }

        /// <summary>
        /// Close a specific panel and return to World Map
        /// </summary>
        public void ClosePanel(PCUIPanel panel)
        {
            if (_panelMap.TryGetValue(panel, out GameObject panelObj) && panelObj != null)
            {
                panelObj.SetActive(false);
                OnPanelClosed?.Invoke(panel);
                ApexLogger.Log($"[PCUI] Closed panel: {panel}", ApexLogger.LogCategory.UI);

                if (_currentPanel == panel)
                {
                    _currentPanel = PCUIPanel.None;
                    // Always return to World Map
                    OpenPanel(PCUIPanel.WorldMap);
                }
            }
        }

        /// <summary>
        /// Toggle a panel on/off. If same panel, close and go to World Map. If different, switch to it.
        /// </summary>
        public void TogglePanel(PCUIPanel panel)
        {
            if (_currentPanel == panel)
            {
                ClosePanel(panel);
            }
            else
            {
                OpenPanel(panel);
            }
        }

        /// <summary>
        /// Toggle a panel by name (for WebGL bridge)
        /// </summary>
        public void TogglePanel(string panelName)
        {
            if (Enum.TryParse<PCUIPanel>(panelName, true, out PCUIPanel panel))
            {
                TogglePanel(panel);
            }
            else
            {
                ApexLogger.LogWarning($"[PCUI] Unknown panel name: {panelName}", ApexLogger.LogCategory.UI);
            }
        }

        /// <summary>
        /// Close the currently active panel
        /// </summary>
        public void CloseCurrentPanel()
        {
            if (_currentPanel != PCUIPanel.None)
            {
                ClosePanel(_currentPanel);
            }
        }

        /// <summary>
        /// Go back to previous panel
        /// </summary>
        public void GoBack()
        {
            if (_panelHistory.Count > 0)
            {
                CloseCurrentPanel();
                OpenPanel(_panelHistory.Pop());
            }
        }

        #endregion

        #region Resource Display

        /// <summary>
        /// Update resource display in the HUD
        /// </summary>
        public void UpdateResourceDisplay(int stone, int wood, int metal, int crystal, int apexCoins)
        {
            if (stoneText != null) stoneText.text = FormatNumber(stone);
            if (woodText != null) woodText.text = FormatNumber(wood);
            if (metalText != null) metalText.text = FormatNumber(metal);
            if (crystalText != null) crystalText.text = FormatNumber(crystal);
            if (apexCoinsText != null) apexCoinsText.text = FormatNumber(apexCoins);
        }

        /// <summary>
        /// Update resource display from a PlayerProfile
        /// </summary>
        public void UpdateResourceDisplay(Player.PlayerProfile profile)
        {
            if (profile == null) return;

            UpdateResourceDisplay(
                profile.Stone,
                profile.Wood,
                profile.Metal,
                profile.Crystal,
                profile.Gems
            );
        }

        private string FormatNumber(int value)
        {
            if (value >= 1000000)
                return $"{value / 1000000f:F1}M";
            if (value >= 1000)
                return $"{value / 1000f:F1}K";
            return value.ToString();
        }

        #endregion

        #region Tooltips

        /// <summary>
        /// Show a tooltip at the specified position
        /// </summary>
        public void ShowTooltip(string title, string description, Vector2 position)
        {
            if (tooltipPanel == null) return;

            tooltipPanel.SetActive(true);
            _isTooltipVisible = true;

            // Update tooltip content
            var titleText = tooltipPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (titleText != null) titleText.text = $"<b>{title}</b>\n{description}";

            // Position tooltip
            RectTransform rt = tooltipPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.position = position;
            }
        }

        /// <summary>
        /// Hide the tooltip
        /// </summary>
        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
            _isTooltipVisible = false;
        }

        #endregion

        #region Notifications

        /// <summary>
        /// Show a notification message
        /// </summary>
        public void ShowNotification(string message, UINotificationType type = UINotificationType.Info)
        {
            ApexLogger.Log($"[PCUI] Notification ({type}): {message}", ApexLogger.LogCategory.UI);
            // TODO: Implement notification UI
        }

        #endregion

        #region Territory Detail

        /// <summary>
        /// Show territory detail panel
        /// </summary>
        public void ShowTerritoryDetail(Territory.Territory territory)
        {
            OpenPanel(PCUIPanel.TerritoryDetail);

            // Populate territory detail panel
            var detailPanel = territoryDetailPanel?.GetComponent<TerritoryDetailPanel>();
            if (detailPanel != null)
            {
                detailPanel.SetTerritory(territory);
            }
        }

        #endregion
    }

    /// <summary>
    /// UI panel types for PC client
    /// </summary>
    public enum PCUIPanel
    {
        None,
        MainMenu,
        WorldMap,
        TerritoryDetail,
        Alliance,
        BuildMenu,
        Inventory,
        Statistics,
        Settings,
        Chat,
        Market,
        Crafting,
        Profile,
        WarRoom,
        BlueprintEditor,
        BattleReplay
    }

    /// <summary>
    /// Notification types
    /// </summary>
    public enum UINotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Combat
    }
}
