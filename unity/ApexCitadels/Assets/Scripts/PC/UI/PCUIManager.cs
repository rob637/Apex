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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePanelMap();
        }

        private void Start()
        {
            SetupInputBindings();
            SetupButtonListeners();

            // Start with world map open
            OpenPanel(PCUIPanel.WorldMap);
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
            if (PCInputManager.Instance == null) return;

            PCInputManager.Instance.OnOpenMenu += () => TogglePanel(PCUIPanel.MainMenu);
            PCInputManager.Instance.OnOpenAlliancePanel += () => TogglePanel(PCUIPanel.Alliance);
            PCInputManager.Instance.OnOpenBuildingMenu += () => TogglePanel(PCUIPanel.BuildMenu);
            PCInputManager.Instance.OnOpenInventory += () => TogglePanel(PCUIPanel.Inventory);
            PCInputManager.Instance.OnOpenWorldMap += () => OpenPanel(PCUIPanel.WorldMap);
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
