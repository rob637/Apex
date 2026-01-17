using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Alliance;

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
            // TODO: Load from AllianceManager
            RefreshOverview();
        }

        private void RefreshOverview()
        {
            // Mock data for now
            if (allianceNameText != null) allianceNameText.text = "Shadow Legion";
            if (allianceLevelText != null) allianceLevelText.text = "Level 5";
            if (memberCountText != null) memberCountText.text = "23/50 Members";
            if (territoryCountText != null) territoryCountText.text = "47 Territories";
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

            // TODO: Load from AllianceManager
            // Mock data
            string[] mockMembers = { "Commander Alpha", "Lieutenant Beta", "Soldier Gamma", "Recruit Delta" };
            string[] mockRoles = { "Leader", "Officer", "Member", "Member" };

            for (int i = 0; i < mockMembers.Length; i++)
            {
                CreateMemberEntry(mockMembers[i], mockRoles[i], i == 0);
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

            if (warStatusText != null)
                warStatusText.text = "No active war operations";

            // TODO: Load war targets and strategic data
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
