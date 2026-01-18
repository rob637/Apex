using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Chat panel supporting World, Alliance, and Private messaging.
    /// Critical for social engagement and player retention.
    /// </summary>
    public class ChatPanel : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int maxMessages = 100;
        [SerializeField] private float messageTimeout = 300f; // 5 minutes
        
        [Header("Colors")]
        [SerializeField] private Color worldColor = new Color(1f, 1f, 1f);
        [SerializeField] private Color allianceColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color privateColor = new Color(0.8f, 0.5f, 0.9f);
        [SerializeField] private Color systemColor = new Color(1f, 0.8f, 0.2f);
        
        // State
        private ChatChannel _currentChannel = ChatChannel.World;
        private Dictionary<ChatChannel, List<ChatMessage>> _messages = new Dictionary<ChatChannel, List<ChatMessage>>();
        private string _currentPlayerId = "You";
        private string _allianceName = "Steel Legion";
        
        // UI
        private GameObject _panel;
        private Transform _messagesContainer;
        private TMP_InputField _inputField;
        private List<GameObject> _channelTabs = new List<GameObject>();
        private ScrollRect _scrollRect;
        private bool _isExpanded = false;
        
        public static ChatPanel Instance { get; private set; }
        
        public event Action<ChatMessage> OnMessageSent;
        public event Action<string, string> OnPrivateMessage; // playerId, message

        private void Awake()
        {
            Instance = this;
            
            // Initialize message lists
            foreach (ChatChannel channel in Enum.GetValues(typeof(ChatChannel)))
            {
                _messages[channel] = new List<ChatMessage>();
            }
        }

        private void Start()
        {
            CreateChatUI();
            GenerateDemoMessages();
            RefreshDisplay();
        }

        private void Update()
        {
            // Press Enter to send
            if (_inputField != null && _inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
            {
                SendCurrentMessage();
            }
            
            // Press T to focus chat
            if (Input.GetKeyDown(KeyCode.T) && (_inputField == null || !_inputField.isFocused))
            {
                if (_inputField != null)
                {
                    _inputField.Select();
                    _inputField.ActivateInputField();
                }
            }
        }

        /// <summary>
        /// Send a message to the current channel
        /// </summary>
        public void SendMessage(string text, ChatChannel channel = ChatChannel.World)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            
            var message = new ChatMessage
            {
                SenderId = _currentPlayerId,
                SenderName = _currentPlayerId,
                Text = text,
                Channel = channel,
                Timestamp = DateTime.Now
            };
            
            _messages[channel].Add(message);
            OnMessageSent?.Invoke(message);
            
            // Trim if too many
            while (_messages[channel].Count > maxMessages)
            {
                _messages[channel].RemoveAt(0);
            }
            
            RefreshDisplay();
            ScrollToBottom();
        }

        /// <summary>
        /// Receive a message (from server/other players)
        /// </summary>
        public void ReceiveMessage(ChatMessage message)
        {
            _messages[message.Channel].Add(message);
            
            while (_messages[message.Channel].Count > maxMessages)
            {
                _messages[message.Channel].RemoveAt(0);
            }
            
            if (_currentChannel == message.Channel)
            {
                RefreshDisplay();
            }
        }

        /// <summary>
        /// Add a system message
        /// </summary>
        public void AddSystemMessage(string text, ChatChannel channel = ChatChannel.World)
        {
            var message = new ChatMessage
            {
                SenderId = "SYSTEM",
                SenderName = "SYSTEM",
                Text = text,
                Channel = channel,
                Timestamp = DateTime.Now,
                IsSystem = true
            };
            
            _messages[channel].Add(message);
            
            if (_currentChannel == channel)
            {
                RefreshDisplay();
            }
        }

        public void SetChannel(ChatChannel channel)
        {
            _currentChannel = channel;
            UpdateTabVisuals();
            RefreshDisplay();
            ScrollToBottom();
        }

        public void ToggleExpanded()
        {
            _isExpanded = !_isExpanded;
            UpdatePanelSize();
        }

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void CreateChatUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel (bottom left, expandable)
            _panel = new GameObject("ChatPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0.3f, 0.35f);
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(0, 0);
            
            // Semi-transparent background
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.6f);
            
            // Channel tabs
            CreateChannelTabs();
            
            // Messages area
            CreateMessagesArea();
            
            // Input area
            CreateInputArea();
            
            // Expand button
            CreateExpandButton();
        }

        private void CreateChannelTabs()
        {
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(_panel.transform, false);
            
            RectTransform rect = tabBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.88f);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(5, 0);
            rect.offsetMax = new Vector2(-30, -2);
            
            HorizontalLayoutGroup layout = tabBar.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 2, 2);
            
            // Create tabs
            CreateChannelTab(tabBar.transform, "🌍 World", ChatChannel.World, worldColor);
            CreateChannelTab(tabBar.transform, "⚔️ Alliance", ChatChannel.Alliance, allianceColor);
            CreateChannelTab(tabBar.transform, "📨 Private", ChatChannel.Private, privateColor);
        }

        private void CreateChannelTab(Transform parent, string label, ChatChannel channel, Color color)
        {
            GameObject tab = new GameObject($"Tab_{channel}");
            tab.transform.SetParent(parent, false);
            
            LayoutElement le = tab.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            le.preferredHeight = 25;
            
            Image bg = tab.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
            
            Button btn = tab.AddComponent<Button>();
            ChatChannel c = channel;
            btn.onClick.AddListener(() => SetChannel(c));
            
            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tab.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 11;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            
            _channelTabs.Add(tab);
        }

        private void CreateMessagesArea()
        {
            // Scroll view
            GameObject scrollView = new GameObject("MessagesScroll");
            scrollView.transform.SetParent(_panel.transform, false);
            
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0.15f);
            scrollRect.anchorMax = new Vector2(1, 0.86f);
            scrollRect.offsetMin = new Vector2(5, 0);
            scrollRect.offsetMax = new Vector2(-5, 0);
            
            _scrollRect = scrollView.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 20f;
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            Image viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = Color.white;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            _scrollRect.viewport = viewportRect;
            
            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 0);
            contentRect.pivot = new Vector2(0.5f, 0);
            contentRect.sizeDelta = new Vector2(0, 0);
            
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 2;
            layout.padding = new RectOffset(5, 5, 5, 5);
            
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            _scrollRect.content = contentRect;
            _messagesContainer = content.transform;
        }

        private void CreateInputArea()
        {
            GameObject inputArea = new GameObject("InputArea");
            inputArea.transform.SetParent(_panel.transform, false);
            
            RectTransform rect = inputArea.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0.14f);
            rect.offsetMin = new Vector2(5, 5);
            rect.offsetMax = new Vector2(-5, 0);
            
            HorizontalLayoutGroup layout = inputArea.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.spacing = 5;
            
            // Input field
            GameObject inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(inputArea.transform, false);
            
            LayoutElement inputLE = inputObj.AddComponent<LayoutElement>();
            inputLE.flexibleWidth = 1;
            inputLE.preferredHeight = 25;
            
            Image inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(0.15f, 0.15f, 0.2f);
            
            _inputField = inputObj.AddComponent<TMP_InputField>();
            _inputField.textViewport = inputObj.GetComponent<RectTransform>();
            
            // Text area for input
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 0);
            textAreaRect.offsetMax = new Vector2(-10, 0);
            
            // Text component
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 12;
            inputText.color = Color.white;
            inputText.alignment = TextAlignmentOptions.Left;
            
            _inputField.textComponent = inputText;
            _inputField.textViewport = textAreaRect;
            
            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            
            RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = "Press T to chat...";
            placeholder.fontSize = 12;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.color = new Color(0.5f, 0.5f, 0.5f);
            placeholder.alignment = TextAlignmentOptions.Left;
            
            _inputField.placeholder = placeholder;
            
            // Send button
            GameObject sendBtn = new GameObject("SendButton");
            sendBtn.transform.SetParent(inputArea.transform, false);
            
            LayoutElement sendLE = sendBtn.AddComponent<LayoutElement>();
            sendLE.preferredWidth = 50;
            sendLE.preferredHeight = 25;
            
            Image sendBg = sendBtn.AddComponent<Image>();
            sendBg.color = new Color(0.2f, 0.5f, 0.8f);
            
            Button btn = sendBtn.AddComponent<Button>();
            btn.onClick.AddListener(SendCurrentMessage);
            
            GameObject sendTextObj = new GameObject("Text");
            sendTextObj.transform.SetParent(sendBtn.transform, false);
            
            RectTransform sendTextRect = sendTextObj.AddComponent<RectTransform>();
            sendTextRect.anchorMin = Vector2.zero;
            sendTextRect.anchorMax = Vector2.one;
            sendTextRect.offsetMin = Vector2.zero;
            sendTextRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI sendText = sendTextObj.AddComponent<TextMeshProUGUI>();
            sendText.text = "Send";
            sendText.fontSize = 12;
            sendText.alignment = TextAlignmentOptions.Center;
            sendText.color = Color.white;
        }

        private void CreateExpandButton()
        {
            GameObject expandBtn = new GameObject("ExpandButton");
            expandBtn.transform.SetParent(_panel.transform, false);
            
            RectTransform rect = expandBtn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-2, -2);
            rect.sizeDelta = new Vector2(25, 25);
            
            Image bg = expandBtn.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.4f, 0.8f);
            
            Button btn = expandBtn.AddComponent<Button>();
            btn.onClick.AddListener(ToggleExpanded);
            
            GameObject textObj = new GameObject("Icon");
            textObj.transform.SetParent(expandBtn.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "⬆";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        private void UpdatePanelSize()
        {
            if (_panel == null) return;
            
            RectTransform rect = _panel.GetComponent<RectTransform>();
            if (_isExpanded)
            {
                rect.anchorMax = new Vector2(0.4f, 0.6f);
            }
            else
            {
                rect.anchorMax = new Vector2(0.3f, 0.35f);
            }
        }

        private void UpdateTabVisuals()
        {
            Color[] tabColors = { worldColor, allianceColor, privateColor };
            
            for (int i = 0; i < _channelTabs.Count && i < 3; i++)
            {
                Image bg = _channelTabs[i].GetComponent<Image>();
                if (bg != null)
                {
                    bool isSelected = (int)_currentChannel == i;
                    bg.color = isSelected ?
                        new Color(tabColors[i].r * 0.3f, tabColors[i].g * 0.3f, tabColors[i].b * 0.3f, 0.9f) :
                        new Color(0.2f, 0.2f, 0.3f, 0.8f);
                }
            }
        }

        private void RefreshDisplay()
        {
            if (_messagesContainer == null) return;
            
            // Clear existing
            foreach (Transform child in _messagesContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Add messages for current channel
            var messages = _messages[_currentChannel];
            foreach (var msg in messages)
            {
                CreateMessageUI(msg);
            }
        }

        private void CreateMessageUI(ChatMessage msg)
        {
            GameObject msgObj = new GameObject("Message");
            msgObj.transform.SetParent(_messagesContainer, false);
            
            LayoutElement le = msgObj.AddComponent<LayoutElement>();
            
            TextMeshProUGUI text = msgObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 11;
            text.alignment = TextAlignmentOptions.Left;
            
            Color nameColor = msg.IsSystem ? systemColor : GetChannelColor(msg.Channel);
            string timeStr = msg.Timestamp.ToString("HH:mm");
            
            if (msg.IsSystem)
            {
                text.text = $"<color=#{ColorUtility.ToHtmlStringRGB(systemColor)}>[{timeStr}] ★ {msg.Text}</color>";
            }
            else
            {
                text.text = $"<color=#888888>[{timeStr}]</color> <color=#{ColorUtility.ToHtmlStringRGB(nameColor)}><b>{msg.SenderName}:</b></color> {msg.Text}";
            }
            
            text.color = Color.white;
        }

        private Color GetChannelColor(ChatChannel channel)
        {
            return channel switch
            {
                ChatChannel.World => worldColor,
                ChatChannel.Alliance => allianceColor,
                ChatChannel.Private => privateColor,
                _ => worldColor
            };
        }

        private void SendCurrentMessage()
        {
            if (_inputField == null) return;
            
            string text = _inputField.text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                SendMessage(text, _currentChannel);
                _inputField.text = "";
                _inputField.ActivateInputField();
            }
        }

        private void ScrollToBottom()
        {
            if (_scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void GenerateDemoMessages()
        {
            // World chat
            AddDemoMessage("DragonLord99", "Anyone want to alliance?", ChatChannel.World);
            AddDemoMessage("ShadowKnight", "Just conquered Crystal Peak! 💪", ChatChannel.World);
            AddDemoMessage("CrystalMage", "Need help defending my territory", ChatChannel.World);
            AddDemoMessage("IronFist", "Trading resources - have extra gold", ChatChannel.World);
            
            // Alliance chat
            AddDemoMessage("Commander", $"Welcome to {_allianceName}!", ChatChannel.Alliance);
            AddDemoMessage("Warlord", "Attack on sector 7 in 5 minutes", ChatChannel.Alliance);
            AddDemoMessage("Scout", "Enemy forces spotted near our border", ChatChannel.Alliance);
            
            // System messages
            AddSystemMessage("🎉 Season 1: Rise of Citadels has begun!");
            AddSystemMessage("⚔️ War between Steel Legion and Shadow Council declared!", ChatChannel.Alliance);
        }

        private void AddDemoMessage(string sender, string text, ChatChannel channel)
        {
            _messages[channel].Add(new ChatMessage
            {
                SenderId = sender,
                SenderName = sender,
                Text = text,
                Channel = channel,
                Timestamp = DateTime.Now.AddMinutes(-UnityEngine.Random.Range(1, 30))
            });
        }
    }

    public enum ChatChannel
    {
        World,
        Alliance,
        Private
    }

    public class ChatMessage
    {
        public string SenderId;
        public string SenderName;
        public string Text;
        public ChatChannel Channel;
        public DateTime Timestamp;
        public bool IsSystem;
    }
}
