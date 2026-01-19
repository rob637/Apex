using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Alliance;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// PC Alliance panel with War Room functionality.
    /// Displays alliance info, members, chat, and strategic planning.
    /// </summary>
    public class AlliancePanel : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private Button overviewTab;
        [SerializeField] private Button membersTab;
        [SerializeField] private Button warRoomTab;
        [SerializeField] private Button chatTab;

        [Header("Tab Contents")]
        [SerializeField] private GameObject overviewContent;
        [SerializeField] private GameObject membersContent;
        [SerializeField] private GameObject warRoomContent;
        [SerializeField] private GameObject chatContent;

        [Header("Overview")]
        [SerializeField] private TextMeshProUGUI allianceNameText;
        [SerializeField] private TextMeshProUGUI allianceLevelText;
        [SerializeField] private TextMeshProUGUI memberCountText;
        [SerializeField] private TextMeshProUGUI territoryCountText;
        [SerializeField] private Image allianceIcon;

        [Header("Members List")]
        [SerializeField] private Transform memberListContainer;
        [SerializeField] private GameObject memberEntryPrefab;

        [Header("War Room")]
        [SerializeField] private Transform targetListContainer;
        [SerializeField] private GameObject targetEntryPrefab;
        [SerializeField] private Button planAttackButton;
        [SerializeField] private Button setDefenseButton;
        [SerializeField] private TextMeshProUGUI warStatusText;

        [Header("Chat")]
        [SerializeField] private Transform chatMessageContainer;
        [SerializeField] private GameObject chatMessagePrefab;
        [SerializeField] private TMP_InputField chatInputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private ScrollRect chatScrollRect;

        // Events
        public event Action<string> OnMemberSelected;
        public event Action<string> OnTargetSelected;
        public event Action<string> OnChatMessageSent;

        // State
        private AlliancePanelTab _currentTab = AlliancePanelTab.Overview;
        private string _currentAllianceId;
        private List<AllianceMemberEntry> _memberEntries = new List<AllianceMemberEntry>();
        private List<ChatMessageEntry> _chatMessages = new List<ChatMessageEntry>();

        private void Awake()
        {
            SetupTabButtons();
            SetupChat();
        }

        private void OnEnable()
        {
            RefreshData();
        }

        private void SetupTabButtons()
        {
            if (overviewTab != null)
                overviewTab.onClick.AddListener(() => SwitchTab(AlliancePanelTab.Overview));
            if (membersTab != null)
                membersTab.onClick.AddListener(() => SwitchTab(AlliancePanelTab.Members));
            if (warRoomTab != null)
                warRoomTab.onClick.AddListener(() => SwitchTab(AlliancePanelTab.WarRoom));
            if (chatTab != null)
                chatTab.onClick.AddListener(() => SwitchTab(AlliancePanelTab.Chat));
        }

        private void SetupChat()
        {
            if (sendButton != null)
                sendButton.onClick.AddListener(SendChatMessage);

            if (chatInputField != null)
            {
                chatInputField.onSubmit.AddListener((text) => SendChatMessage());
            }
        }

        #region Tab Management

        private void SwitchTab(AlliancePanelTab tab)
        {
            _currentTab = tab;

            // Hide all content
            if (overviewContent != null) overviewContent.SetActive(false);
            if (membersContent != null) membersContent.SetActive(false);
            if (warRoomContent != null) warRoomContent.SetActive(false);
            if (chatContent != null) chatContent.SetActive(false);

            // Show selected tab
            switch (tab)
            {
                case AlliancePanelTab.Overview:
                    if (overviewContent != null) overviewContent.SetActive(true);
                    break;
                case AlliancePanelTab.Members:
                    if (membersContent != null) membersContent.SetActive(true);
                    RefreshMemberList();
                    break;
                case AlliancePanelTab.WarRoom:
                    if (warRoomContent != null) warRoomContent.SetActive(true);
                    RefreshWarRoom();
                    break;
                case AlliancePanelTab.Chat:
                    if (chatContent != null) chatContent.SetActive(true);
                    ScrollToLatestMessage();
                    break;
            }
        }

        #endregion

        #region Data Display

        public void RefreshData()
        {
            // Load from AllianceManager
            var alliance = AllianceManager.Instance?.CurrentAlliance;
            if (alliance != null)
            {
                _currentAllianceId = alliance.Id;
                ApexLogger.Log($"[AlliancePanel] Refreshing data for alliance: {alliance.Name}", ApexLogger.LogCategory.UI);
            }
            else
            {
                ApexLogger.Log("[AlliancePanel] No alliance - showing placeholder data", ApexLogger.LogCategory.UI);
            }
            
            RefreshOverview();
        }

        private void RefreshOverview()
        {
            var alliance = AllianceManager.Instance?.CurrentAlliance;
            
            if (alliance != null)
            {
                // Real alliance data
                if (allianceNameText != null) allianceNameText.text = $"{alliance.Name} [{alliance.Tag}]";
                if (allianceLevelText != null) allianceLevelText.text = $"Level {alliance.Level}";
                if (memberCountText != null) memberCountText.text = $"{alliance.Members.Count}/{alliance.MaxMembers} Members";
                if (territoryCountText != null) territoryCountText.text = $"{alliance.TerritoryCount} Territories";
            }
            else
            {
                // Placeholder for when not in alliance
                if (allianceNameText != null) allianceNameText.text = "No Alliance";
                if (allianceLevelText != null) allianceLevelText.text = "Join or Create";
                if (memberCountText != null) memberCountText.text = "0/0 Members";
                if (territoryCountText != null) territoryCountText.text = "0 Territories";
            }
        }

        private void RefreshMemberList()
        {
            if (memberListContainer == null) return;

            // Clear existing
            foreach (Transform child in memberListContainer)
            {
                Destroy(child.gameObject);
            }
            _memberEntries.Clear();

            var alliance = AllianceManager.Instance?.CurrentAlliance;
            if (alliance != null && alliance.Members != null)
            {
                // Real member data from AllianceManager
                foreach (var member in alliance.Members)
                {
                    bool isOnline = member.IsOnline;
                    string roleName = member.Role.ToString();
                    CreateMemberEntry(member.PlayerName, roleName, isOnline);
                }
            }
            else
            {
                // Placeholder data when not in alliance
                ApexLogger.Log("[AlliancePanel] No alliance members to display", ApexLogger.LogCategory.UI);
            }
        }

        private void CreateMemberEntry(string name, string role, bool isOnline)
        {
            if (memberEntryPrefab == null || memberListContainer == null) return;

            GameObject entryObj = Instantiate(memberEntryPrefab, memberListContainer);
            var texts = entryObj.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 2)
            {
                texts[0].text = name;
                texts[1].text = role;
            }

            // Online indicator
            var images = entryObj.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img.name.Contains("Online") || img.name.Contains("Status"))
                {
                    img.color = isOnline ? Color.green : Color.gray;
                }
            }

            // Click handler
            var button = entryObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnMemberSelected?.Invoke(name));
            }
        }

        #endregion

        #region War Room (PC Exclusive)

        private void RefreshWarRoom()
        {
            if (!PlatformManager.IsFeatureAvailable(GameFeature.AllianceWarRoom))
            {
                if (warStatusText != null)
                    warStatusText.text = "War Room is only available on PC";
                return;
            }

            // Load active war from AllianceManager
            var activeWar = AllianceManager.Instance?.ActiveWar;
            if (activeWar != null && activeWar.IsActive)
            {
                if (warStatusText != null)
                {
                    string timeRemaining = activeWar.TimeRemaining.ToString(@"hh\:mm\:ss");
                    warStatusText.text = $"WAR: vs {activeWar.DefenderName}\nScore: {activeWar.AttackerScore} - {activeWar.DefenderScore}\nTime: {timeRemaining}";
                }
                ApexLogger.Log($"[AlliancePanel] Displaying active war vs {activeWar.DefenderName}", ApexLogger.LogCategory.UI);
            }
            else
            {
                if (warStatusText != null)
                    warStatusText.text = "No active war operations";
            }
        }

        /// <summary>
        /// Add a war target to the list
        /// </summary>
        public void AddWarTarget(string targetName, string location, int estimatedDifficulty)
        {
            if (targetEntryPrefab == null || targetListContainer == null) return;

            GameObject entryObj = Instantiate(targetEntryPrefab, targetListContainer);
            var texts = entryObj.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 2)
            {
                texts[0].text = targetName;
                texts[1].text = $"Location: {location} | Difficulty: {estimatedDifficulty}/10";
            }

            var button = entryObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnTargetSelected?.Invoke(targetName));
            }
        }

        #endregion

        #region Chat

        private void SendChatMessage()
        {
            if (chatInputField == null || string.IsNullOrWhiteSpace(chatInputField.text))
                return;

            string message = chatInputField.text.Trim();
            chatInputField.text = "";

            // Add to local display
            AddChatMessage("You", message, DateTime.Now);

            // Send to server
            OnChatMessageSent?.Invoke(message);

            // Focus back on input
            chatInputField.ActivateInputField();
        }

        /// <summary>
        /// Add a chat message to the display
        /// </summary>
        public void AddChatMessage(string sender, string message, DateTime timestamp)
        {
            if (chatMessagePrefab == null || chatMessageContainer == null) return;

            GameObject msgObj = Instantiate(chatMessagePrefab, chatMessageContainer);
            var text = msgObj.GetComponentInChildren<TextMeshProUGUI>();

            if (text != null)
            {
                text.text = $"<color=#88CCFF>[{timestamp:HH:mm}] {sender}:</color> {message}";
            }

            _chatMessages.Add(new ChatMessageEntry
            {
                Sender = sender,
                Message = message,
                Timestamp = timestamp
            });

            // Limit messages
            while (_chatMessages.Count > 100)
            {
                _chatMessages.RemoveAt(0);
                if (chatMessageContainer.childCount > 0)
                {
                    Destroy(chatMessageContainer.GetChild(0).gameObject);
                }
            }

            ScrollToLatestMessage();
        }

        private void ScrollToLatestMessage()
        {
            if (chatScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        #endregion
    }

    /// <summary>
    /// Alliance panel tabs
    /// </summary>
    public enum AlliancePanelTab
    {
        Overview,
        Members,
        WarRoom,
        Chat
    }

    /// <summary>
    /// Member list entry data
    /// </summary>
    public class AllianceMemberEntry
    {
        public string PlayerId;
        public string PlayerName;
        public string Role;
        public bool IsOnline;
        public int TerritoryCount;
    }

    /// <summary>
    /// Chat message entry
    /// </summary>
    public class ChatMessageEntry
    {
        public string Sender;
        public string Message;
        public DateTime Timestamp;
    }
}
