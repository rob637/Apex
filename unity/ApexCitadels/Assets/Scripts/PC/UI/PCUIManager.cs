using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Territory;
using ApexCitadels.Alliance;
using ApexCitadels.Resources;

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
                Debug.LogError("[PCUI] No Canvas found!");
                return;
            }

            // Create panels that are null
            if (mainMenuPanel == null)
                mainMenuPanel = CreatePlaceholderPanel(canvas.transform, "MainMenu", "Main Menu", new Color(0.1f, 0.1f, 0.15f, 0.95f));
            if (worldMapPanel == null)
                worldMapPanel = CreatePlaceholderPanel(canvas.transform, "WorldMap", "World Map", new Color(0.05f, 0.1f, 0.05f, 0.9f));
            if (territoryDetailPanel == null)
                territoryDetailPanel = CreatePlaceholderPanel(canvas.transform, "TerritoryDetail", "Territory Details", new Color(0.1f, 0.1f, 0.1f, 0.95f));
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

            Debug.Log("[PCUI] Created placeholder panels for missing UI elements");
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

            // Add background image
            UnityEngine.UI.Image bg = panel.AddComponent<UnityEngine.UI.Image>();
            bg.color = bgColor;

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
            contentText.text = $"[{title} Panel]\n\nThis is a placeholder panel.\nPress ESC to close or click the X button.\n\nKeyboard shortcuts:\n• B - Build Menu\n• Tab - Alliance\n• M - World Map\n• I - Inventory\n• Esc - Main Menu";
            contentText.fontSize = 20;
            contentText.alignment = TextAlignmentOptions.Center;
            contentText.color = new Color(0.8f, 0.8f, 0.8f, 1f);

            // Wire up close button
            string panelName = name;
            closeButton.onClick.AddListener(() => {
                if (Enum.TryParse<PCUIPanel>(panelName, out PCUIPanel p))
                    ClosePanel(p);
            });

            Debug.Log($"[PCUI] Created placeholder panel: {name}");
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
                Debug.Log("[PCUI] PCInputManager not ready, will retry...");
                return;
            }

            if (_inputBindingsSetup) return;

            Debug.Log("[PCUI] Setting up input bindings...");
            
            PCInputManager.Instance.OnOpenMenu += () => {
                Debug.Log("[PCUI] ESC pressed - toggling MainMenu");
                TogglePanel(PCUIPanel.MainMenu);
            };
            PCInputManager.Instance.OnOpenAlliancePanel += () => {
                Debug.Log("[PCUI] TAB pressed - toggling Alliance");
                TogglePanel(PCUIPanel.Alliance);
            };
            PCInputManager.Instance.OnOpenBuildingMenu += () => {
                Debug.Log("[PCUI] B pressed - toggling BuildMenu");
                TogglePanel(PCUIPanel.BuildMenu);
            };
            PCInputManager.Instance.OnOpenInventory += () => {
                Debug.Log("[PCUI] I pressed - toggling Inventory");
                TogglePanel(PCUIPanel.Inventory);
            };
            PCInputManager.Instance.OnOpenWorldMap += () => {
                Debug.Log("[PCUI] M pressed - opening WorldMap");
                OpenPanel(PCUIPanel.WorldMap);
            };

            _inputBindingsSetup = true;
            Debug.Log("[PCUI] Input bindings setup complete!");
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
        /// Open a UI panel
        /// </summary>
        public void OpenPanel(PCUIPanel panel)
        {
            if (_currentPanel == panel) return;

            // Close current panel (unless it's the base map)
            if (_currentPanel != PCUIPanel.None && _currentPanel != PCUIPanel.WorldMap)
            {
                _panelHistory.Push(_currentPanel);
                CloseCurrentPanel();
            }

            // Open new panel
            if (_panelMap.TryGetValue(panel, out GameObject panelObj) && panelObj != null)
            {
                panelObj.SetActive(true);
                _currentPanel = panel;
                OnPanelOpened?.Invoke(panel);
                Debug.Log($"[PCUI] Opened panel: {panel}");
            }
        }

        /// <summary>
        /// Close a specific panel
        /// </summary>
        public void ClosePanel(PCUIPanel panel)
        {
            if (_panelMap.TryGetValue(panel, out GameObject panelObj) && panelObj != null)
            {
                panelObj.SetActive(false);
                OnPanelClosed?.Invoke(panel);
                Debug.Log($"[PCUI] Closed panel: {panel}");

                if (_currentPanel == panel)
                {
                    _currentPanel = PCUIPanel.None;

                    // Return to previous panel if available
                    if (_panelHistory.Count > 0)
                    {
                        OpenPanel(_panelHistory.Pop());
                    }
                    else
                    {
                        OpenPanel(PCUIPanel.WorldMap);
                    }
                }
            }
        }

        /// <summary>
        /// Toggle a panel on/off
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
                Debug.LogWarning($"[PCUI] Unknown panel name: {panelName}");
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
        public void ShowNotification(string message, NotificationType type = NotificationType.Info)
        {
            Debug.Log($"[PCUI] Notification ({type}): {message}");
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
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Combat
    }
}
