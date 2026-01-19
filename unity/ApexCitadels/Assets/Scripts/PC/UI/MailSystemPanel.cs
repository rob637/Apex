using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Mail System Panel - In-game messaging system with attachments.
    /// Send messages, resources, and items to other players.
    /// 
    /// Features:
    /// - Inbox/Sent/Drafts folders
    /// - Message composition
    /// - Item/resource attachments
    /// - System notifications
    /// - Mass mail (alliance/guild)
    /// - Message templates
    /// - Attachment claiming
    /// </summary>
    public class MailSystemPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.4f, 0.6f, 0.9f);
        [SerializeField] private Color panelColor = new Color(0.12f, 0.12f, 0.18f, 0.95f);
        [SerializeField] private Color unreadColor = new Color(0.3f, 0.5f, 0.8f);
        [SerializeField] private Color readColor = new Color(0.2f, 0.2f, 0.25f);
        [SerializeField] private Color systemColor = new Color(0.9f, 0.7f, 0.2f);
        [SerializeField] private Color attachmentColor = new Color(0.3f, 0.7f, 0.4f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _mailListContainer;
        private GameObject _messageViewPanel;
        private GameObject _composePanel;
        
        // Folder buttons
        private Button _inboxButton;
        private Button _sentButton;
        private Button _systemButton;
        private Button _trashButton;
        
        // Message view
        private TextMeshProUGUI _messageSubject;
        private TextMeshProUGUI _messageSender;
        private TextMeshProUGUI _messageDate;
        private TextMeshProUGUI _messageBody;
        private GameObject _attachmentContainer;
        
        // Compose panel
        private TMP_InputField _recipientInput;
        private TMP_InputField _subjectInput;
        private TMP_InputField _bodyInput;
        private Button _sendButton;
        private Button _attachButton;
        private GameObject _composeAttachments;
        
        // Counter
        private TextMeshProUGUI _unreadCounter;
        
        // State
        private List<MailMessage> _inbox = new List<MailMessage>();
        private List<MailMessage> _sent = new List<MailMessage>();
        private List<MailMessage> _system = new List<MailMessage>();
        private List<MailMessage> _trash = new List<MailMessage>();
        private List<MailMessage> _currentFolder;
        private MailMessage _selectedMessage;
        private MailFolder _currentFolderType = MailFolder.Inbox;
        private List<MailAttachment> _pendingAttachments = new List<MailAttachment>();
        
        public static MailSystemPanel Instance { get; private set; }
        
        // Events
        public event Action<MailMessage> OnMailReceived;
        public event Action<MailMessage> OnMailSent;
        public event Action<MailAttachment> OnAttachmentClaimed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            CreateUI();
            GenerateSampleMail();
            ShowFolder(MailFolder.Inbox);
            HideComposePanel();
            Hide();
        }

        private void CreateUI()
        {
            // Main panel
            _panel = new GameObject("MailSystemPanel");
            _panel.transform.SetParent(transform);
            
            RectTransform panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.15f, 0.1f);
            panelRect.anchorMax = new Vector2(0.85f, 0.9f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            Image panelBg = _panel.AddComponent<Image>();
            panelBg.color = panelColor;
            
            UnityEngine.UI.Outline outline = _panel.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(2, 2);
            
            // Header
            CreateHeader();
            
            // Main layout: sidebar (folders) + content (mail list + message view)
            CreateMainLayout();
            
            // Compose panel (overlay)
            CreateComposePanel();
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_panel.transform, false);
            
            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 55);
            
            Image headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.15f, 0.15f, 0.22f);
            
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(0.3f, 1);
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "📬 MAIL";
            title.fontSize = 24;
            title.fontStyle = FontStyles.Bold;
            title.color = accentColor;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Compose button
            CreateComposeButton(header.transform);
            
            // Close button
            CreateCloseButton(header.transform);
        }

        private void CreateComposeButton(Transform parent)
        {
            GameObject composeObj = new GameObject("ComposeButton");
            composeObj.transform.SetParent(parent, false);
            
            RectTransform composeRect = composeObj.AddComponent<RectTransform>();
            composeRect.anchorMin = new Vector2(0.4f, 0.15f);
            composeRect.anchorMax = new Vector2(0.6f, 0.85f);
            composeRect.offsetMin = Vector2.zero;
            composeRect.offsetMax = Vector2.zero;
            
            Image composeBg = composeObj.AddComponent<Image>();
            composeBg.color = accentColor;
            
            Button composeBtn = composeObj.AddComponent<Button>();
            composeBtn.onClick.AddListener(ShowComposePanel);
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(composeObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "[E] Compose";
            text.fontSize = 16;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(parent, false);
            
            RectTransform closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 0.5f);
            closeRect.anchorMax = new Vector2(1, 0.5f);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.anchoredPosition = new Vector2(-10, 0);
            closeRect.sizeDelta = new Vector2(40, 40);
            
            Image closeBg = closeObj.AddComponent<Image>();
            closeBg.color = new Color(0.6f, 0.2f, 0.2f);
            
            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
            
            GameObject closeText = new GameObject("X");
            closeText.transform.SetParent(closeObj.transform, false);
            RectTransform xtRect = closeText.AddComponent<RectTransform>();
            xtRect.anchorMin = Vector2.zero;
            xtRect.anchorMax = Vector2.one;
            xtRect.offsetMin = Vector2.zero;
            xtRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI x = closeText.AddComponent<TextMeshProUGUI>();
            x.text = "[X]";
            x.fontSize = 24;
            x.color = Color.white;
            x.alignment = TextAlignmentOptions.Center;
        }

        private void CreateMainLayout()
        {
            GameObject mainContent = new GameObject("MainContent");
            mainContent.transform.SetParent(_panel.transform, false);
            
            RectTransform contentRect = mainContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(0, 0);
            contentRect.offsetMax = new Vector2(0, -55);
            
            // Sidebar (folders)
            CreateSidebar(mainContent.transform);
            
            // Mail list
            CreateMailList(mainContent.transform);
            
            // Message view
            CreateMessageView(mainContent.transform);
        }

        private void CreateSidebar(Transform parent)
        {
            GameObject sidebar = new GameObject("Sidebar");
            sidebar.transform.SetParent(parent, false);
            
            RectTransform sidebarRect = sidebar.AddComponent<RectTransform>();
            sidebarRect.anchorMin = new Vector2(0, 0);
            sidebarRect.anchorMax = new Vector2(0.18f, 1);
            sidebarRect.offsetMin = Vector2.zero;
            sidebarRect.offsetMax = Vector2.zero;
            
            Image sidebarBg = sidebar.AddComponent<Image>();
            sidebarBg.color = new Color(0.08f, 0.08f, 0.12f);
            
            VerticalLayoutGroup vlayout = sidebar.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(8, 8, 10, 10);
            
            _inboxButton = CreateFolderButton(sidebar.transform, "📥 Inbox", () => ShowFolder(MailFolder.Inbox), true);
            _sentButton = CreateFolderButton(sidebar.transform, "[E] Sent", () => ShowFolder(MailFolder.Sent), false);
            _systemButton = CreateFolderButton(sidebar.transform, "[P] System", () => ShowFolder(MailFolder.System), false);
            _trashButton = CreateFolderButton(sidebar.transform, "[D] Trash", () => ShowFolder(MailFolder.Trash), false);
            
            // Add spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(sidebar.transform, false);
            LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
            spacerLE.flexibleHeight = 1;
            
            // Unread counter
            GameObject counterObj = new GameObject("UnreadCounter");
            counterObj.transform.SetParent(sidebar.transform, false);
            LayoutElement counterLE = counterObj.AddComponent<LayoutElement>();
            counterLE.preferredHeight = 30;
            
            _unreadCounter = counterObj.AddComponent<TextMeshProUGUI>();
            _unreadCounter.fontSize = 12;
            _unreadCounter.color = new Color(0.5f, 0.5f, 0.5f);
            _unreadCounter.alignment = TextAlignmentOptions.Center;
            UpdateUnreadCounter();
        }

        private Button CreateFolderButton(Transform parent, string label, Action onClick, bool selected)
        {
            GameObject btnObj = new GameObject(label);
            btnObj.transform.SetParent(parent, false);
            
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = selected ? accentColor : new Color(0.15f, 0.15f, 0.2f);
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            
            return btn;
        }

        private void CreateMailList(Transform parent)
        {
            GameObject mailListPanel = new GameObject("MailListPanel");
            mailListPanel.transform.SetParent(parent, false);
            
            RectTransform listRect = mailListPanel.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.18f, 0);
            listRect.anchorMax = new Vector2(0.48f, 1);
            listRect.offsetMin = new Vector2(5, 5);
            listRect.offsetMax = new Vector2(0, -5);
            
            Image listBg = mailListPanel.AddComponent<Image>();
            listBg.color = new Color(0.1f, 0.1f, 0.14f);
            
            // Scroll view
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(mailListPanel.transform, false);
            
            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(5, 5);
            scrollRect.offsetMax = new Vector2(-5, -5);
            
            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30;
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform viewRect = viewport.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = Vector2.zero;
            viewRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = viewRect;
            
            // Content
            _mailListContainer = new GameObject("Content");
            _mailListContainer.transform.SetParent(viewport.transform, false);
            RectTransform containerRect = _mailListContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = Vector2.zero;
            
            VerticalLayoutGroup vlayout = _mailListContainer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 3;
            vlayout.padding = new RectOffset(3, 3, 3, 3);
            
            ContentSizeFitter fitter = _mailListContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = containerRect;
        }

        private void CreateMessageView(Transform parent)
        {
            _messageViewPanel = new GameObject("MessageViewPanel");
            _messageViewPanel.transform.SetParent(parent, false);
            
            RectTransform viewRect = _messageViewPanel.AddComponent<RectTransform>();
            viewRect.anchorMin = new Vector2(0.48f, 0);
            viewRect.anchorMax = new Vector2(1, 1);
            viewRect.offsetMin = new Vector2(5, 5);
            viewRect.offsetMax = new Vector2(-5, -5);
            
            Image viewBg = _messageViewPanel.AddComponent<Image>();
            viewBg.color = new Color(0.08f, 0.08f, 0.12f);
            
            VerticalLayoutGroup vlayout = _messageViewPanel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperLeft;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Subject
            GameObject subjectObj = new GameObject("Subject");
            subjectObj.transform.SetParent(_messageViewPanel.transform, false);
            LayoutElement subjectLE = subjectObj.AddComponent<LayoutElement>();
            subjectLE.preferredHeight = 30;
            _messageSubject = subjectObj.AddComponent<TextMeshProUGUI>();
            _messageSubject.text = "Select a message";
            _messageSubject.fontSize = 20;
            _messageSubject.fontStyle = FontStyles.Bold;
            _messageSubject.color = Color.white;
            
            // Sender & Date row
            GameObject infoRow = new GameObject("InfoRow");
            infoRow.transform.SetParent(_messageViewPanel.transform, false);
            LayoutElement infoLE = infoRow.AddComponent<LayoutElement>();
            infoLE.preferredHeight = 25;
            
            HorizontalLayoutGroup hlayout = infoRow.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 20;
            
            GameObject senderObj = new GameObject("Sender");
            senderObj.transform.SetParent(infoRow.transform, false);
            _messageSender = senderObj.AddComponent<TextMeshProUGUI>();
            _messageSender.text = "";
            _messageSender.fontSize = 13;
            _messageSender.color = accentColor;
            
            GameObject dateObj = new GameObject("Date");
            dateObj.transform.SetParent(infoRow.transform, false);
            _messageDate = dateObj.AddComponent<TextMeshProUGUI>();
            _messageDate.text = "";
            _messageDate.fontSize = 12;
            _messageDate.color = new Color(0.5f, 0.5f, 0.5f);
            
            // Divider
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(_messageViewPanel.transform, false);
            LayoutElement divLE = divider.AddComponent<LayoutElement>();
            divLE.preferredHeight = 2;
            Image divImg = divider.AddComponent<Image>();
            divImg.color = new Color(0.2f, 0.2f, 0.25f);
            
            // Body
            GameObject bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(_messageViewPanel.transform, false);
            LayoutElement bodyLE = bodyObj.AddComponent<LayoutElement>();
            bodyLE.flexibleHeight = 1;
            bodyLE.preferredHeight = 200;
            _messageBody = bodyObj.AddComponent<TextMeshProUGUI>();
            _messageBody.text = "Select a message from the list to view its contents.";
            _messageBody.fontSize = 14;
            _messageBody.color = new Color(0.8f, 0.8f, 0.8f);
            
            // Attachments
            CreateAttachmentSection(_messageViewPanel.transform);
            
            // Action buttons
            CreateMessageActions(_messageViewPanel.transform);
        }

        private void CreateAttachmentSection(Transform parent)
        {
            _attachmentContainer = new GameObject("Attachments");
            _attachmentContainer.transform.SetParent(parent, false);
            
            LayoutElement le = _attachmentContainer.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Image bg = _attachmentContainer.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.15f);
            
            VerticalLayoutGroup vlayout = _attachmentContainer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperLeft;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(10, 10, 8, 8);
            
            // Header
            GameObject headerObj = new GameObject("AttachHeader");
            headerObj.transform.SetParent(_attachmentContainer.transform, false);
            LayoutElement headerLE = headerObj.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 20;
            TextMeshProUGUI header = headerObj.AddComponent<TextMeshProUGUI>();
            header.text = "[A] Attachments";
            header.fontSize = 12;
            header.fontStyle = FontStyles.Bold;
            header.color = attachmentColor;
            
            _attachmentContainer.SetActive(false);
        }

        private void CreateMessageActions(Transform parent)
        {
            GameObject actionsRow = new GameObject("Actions");
            actionsRow.transform.SetParent(parent, false);
            
            LayoutElement le = actionsRow.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            
            HorizontalLayoutGroup hlayout = actionsRow.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.childForceExpandWidth = true;
            
            CreateActionButton(actionsRow.transform, "[U] Reply", OnReplyClicked, accentColor);
            CreateActionButton(actionsRow.transform, "[D] Delete", OnDeleteClicked, new Color(0.6f, 0.3f, 0.3f));
            CreateActionButton(actionsRow.transform, "[A] Claim All", OnClaimAllClicked, attachmentColor);
        }

        private Button CreateActionButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btnObj = new GameObject(label);
            btnObj.transform.SetParent(parent, false);
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = color;
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            return btn;
        }

        private void CreateComposePanel()
        {
            _composePanel = new GameObject("ComposePanel");
            _composePanel.transform.SetParent(_panel.transform, false);
            
            RectTransform composeRect = _composePanel.AddComponent<RectTransform>();
            composeRect.anchorMin = new Vector2(0.2f, 0.15f);
            composeRect.anchorMax = new Vector2(0.8f, 0.85f);
            composeRect.offsetMin = Vector2.zero;
            composeRect.offsetMax = Vector2.zero;
            
            Image composeBg = _composePanel.AddComponent<Image>();
            composeBg.color = new Color(0.1f, 0.1f, 0.15f);
            
            UnityEngine.UI.Outline outline = _composePanel.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(2, 2);
            
            VerticalLayoutGroup vlayout = _composePanel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(20, 20, 15, 15);
            
            // Header
            GameObject headerObj = new GameObject("ComposeHeader");
            headerObj.transform.SetParent(_composePanel.transform, false);
            LayoutElement headerLE = headerObj.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 35;
            
            HorizontalLayoutGroup headerHL = headerObj.AddComponent<HorizontalLayoutGroup>();
            headerHL.childAlignment = TextAnchor.MiddleLeft;
            
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "[E] New Message";
            title.fontSize = 20;
            title.fontStyle = FontStyles.Bold;
            title.color = accentColor;
            
            // Close button for compose
            GameObject closeObj = new GameObject("Close");
            closeObj.transform.SetParent(headerObj.transform, false);
            LayoutElement closeLE = closeObj.AddComponent<LayoutElement>();
            closeLE.preferredWidth = 35;
            Image closeBg = closeObj.AddComponent<Image>();
            closeBg.color = new Color(0.5f, 0.2f, 0.2f);
            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(HideComposePanel);
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(closeObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI closeX = textObj.AddComponent<TextMeshProUGUI>();
            closeX.text = "[X]";
            closeX.fontSize = 18;
            closeX.color = Color.white;
            closeX.alignment = TextAlignmentOptions.Center;
            
            // Recipient
            CreateComposeField(_composePanel.transform, "To:", out _recipientInput, "Enter player name...");
            
            // Subject
            CreateComposeField(_composePanel.transform, "Subject:", out _subjectInput, "Enter subject...");
            
            // Body
            GameObject bodyRow = new GameObject("BodyRow");
            bodyRow.transform.SetParent(_composePanel.transform, false);
            LayoutElement bodyLE = bodyRow.AddComponent<LayoutElement>();
            bodyLE.preferredHeight = 200;
            bodyLE.flexibleHeight = 1;
            
            Image bodyBg = bodyRow.AddComponent<Image>();
            bodyBg.color = new Color(0.08f, 0.08f, 0.1f);
            
            GameObject inputObj = new GameObject("BodyInput");
            inputObj.transform.SetParent(bodyRow.transform, false);
            RectTransform inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = new Vector2(10, 10);
            inputRect.offsetMax = new Vector2(-10, -10);
            
            _bodyInput = inputObj.AddComponent<TMP_InputField>();
            _bodyInput.lineType = TMP_InputField.LineType.MultiLineNewline;
            
            // Text area
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = Vector2.zero;
            textAreaRect.offsetMax = Vector2.zero;
            
            GameObject inputTextObj = new GameObject("Text");
            inputTextObj.transform.SetParent(textArea.transform, false);
            RectTransform inputTextRect = inputTextObj.AddComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;
            TextMeshProUGUI inputText = inputTextObj.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 14;
            inputText.color = Color.white;
            
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = "Write your message here...";
            placeholder.fontSize = 14;
            placeholder.color = new Color(0.4f, 0.4f, 0.4f);
            placeholder.fontStyle = FontStyles.Italic;
            
            _bodyInput.textViewport = textAreaRect;
            _bodyInput.textComponent = inputText;
            _bodyInput.placeholder = placeholder;
            
            // Attachments row
            _composeAttachments = new GameObject("ComposeAttachments");
            _composeAttachments.transform.SetParent(_composePanel.transform, false);
            LayoutElement attachLE = _composeAttachments.AddComponent<LayoutElement>();
            attachLE.preferredHeight = 50;
            
            HorizontalLayoutGroup attachHL = _composeAttachments.AddComponent<HorizontalLayoutGroup>();
            attachHL.childAlignment = TextAnchor.MiddleLeft;
            attachHL.spacing = 10;
            attachHL.padding = new RectOffset(10, 10, 5, 5);
            
            _attachButton = CreateComposeActionButton(_composeAttachments.transform, "[A] Add Attachment", OnAddAttachment, new Color(0.3f, 0.3f, 0.4f));
            
            // Send row
            GameObject sendRow = new GameObject("SendRow");
            sendRow.transform.SetParent(_composePanel.transform, false);
            LayoutElement sendLE = sendRow.AddComponent<LayoutElement>();
            sendLE.preferredHeight = 50;
            
            HorizontalLayoutGroup sendHL = sendRow.AddComponent<HorizontalLayoutGroup>();
            sendHL.childAlignment = TextAnchor.MiddleCenter;
            sendHL.spacing = 15;
            sendHL.childForceExpandWidth = true;
            
            CreateComposeActionButton(sendRow.transform, "[X] Cancel", HideComposePanel, new Color(0.4f, 0.25f, 0.25f));
            _sendButton = CreateComposeActionButton(sendRow.transform, "[E] Send", OnSendClicked, accentColor);
        }

        private void CreateComposeField(Transform parent, string label, out TMP_InputField inputField, string placeholder)
        {
            GameObject row = new GameObject($"{label}Row");
            row.transform.SetParent(parent, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 35;
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 10;
            hlayout.childForceExpandHeight = true;
            
            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 70;
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.color = new Color(0.6f, 0.6f, 0.6f);
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Input container
            GameObject inputContainer = new GameObject("InputContainer");
            inputContainer.transform.SetParent(row.transform, false);
            LayoutElement inputLE = inputContainer.AddComponent<LayoutElement>();
            inputLE.flexibleWidth = 1;
            
            Image inputBg = inputContainer.AddComponent<Image>();
            inputBg.color = new Color(0.08f, 0.08f, 0.1f);
            
            GameObject inputObj = new GameObject("Input");
            inputObj.transform.SetParent(inputContainer.transform, false);
            RectTransform inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = new Vector2(10, 2);
            inputRect.offsetMax = new Vector2(-10, -2);
            
            inputField = inputObj.AddComponent<TMP_InputField>();
            
            // Text area
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = Vector2.zero;
            textAreaRect.offsetMax = Vector2.zero;
            
            GameObject inputTextObj = new GameObject("Text");
            inputTextObj.transform.SetParent(textArea.transform, false);
            RectTransform inputTextRect = inputTextObj.AddComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;
            TextMeshProUGUI inputText = inputTextObj.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 14;
            inputText.color = Color.white;
            
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            TextMeshProUGUI ph = placeholderObj.AddComponent<TextMeshProUGUI>();
            ph.text = placeholder;
            ph.fontSize = 14;
            ph.color = new Color(0.4f, 0.4f, 0.4f);
            ph.fontStyle = FontStyles.Italic;
            
            inputField.textViewport = textAreaRect;
            inputField.textComponent = inputText;
            inputField.placeholder = ph;
        }

        private Button CreateComposeActionButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btnObj = new GameObject(label);
            btnObj.transform.SetParent(parent, false);
            
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = 150;
            le.preferredHeight = 40;
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = color;
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            return btn;
        }

        #region Data & Logic

        private void GenerateSampleMail()
        {
            // Inbox
            _inbox.Add(new MailMessage
            {
                Id = Guid.NewGuid().ToString(),
                Sender = "Alliance Leader",
                Subject = "Important: War Declaration",
                Body = "Attention all alliance members!\n\nWe have declared war on the Shadow Legion alliance. All active members are expected to participate in the upcoming battle.\n\nPrepare your troops and report to the war room by 8 PM server time.\n\nFor glory!",
                ReceivedAt = DateTime.Now.AddHours(-2),
                IsRead = false,
                Attachments = new List<MailAttachment>
                {
                    new MailAttachment { Type = AttachmentType.Gold, Amount = 1000, Description = "War Supplies" }
                }
            });
            
            _inbox.Add(new MailMessage
            {
                Id = Guid.NewGuid().ToString(),
                Sender = "Friend123",
                Subject = "Thanks for the help!",
                Body = "Hey! Thanks for helping me defend my territory yesterday. Couldn't have done it without you!\n\nHere's a small gift to show my appreciation.",
                ReceivedAt = DateTime.Now.AddHours(-5),
                IsRead = true,
                Attachments = new List<MailAttachment>
                {
                    new MailAttachment { Type = AttachmentType.Item, ItemId = "health_potion", Amount = 5, Description = "Health Potions x5" }
                }
            });
            
            _inbox.Add(new MailMessage
            {
                Id = Guid.NewGuid().ToString(),
                Sender = "NewPlayer42",
                Subject = "How do I upgrade buildings?",
                Body = "Hi there! I'm new to the game and saw you're a high-level player. Could you explain how the building upgrade system works?",
                ReceivedAt = DateTime.Now.AddDays(-1),
                IsRead = false
            });
            
            // System messages
            _system.Add(new MailMessage
            {
                Id = Guid.NewGuid().ToString(),
                Sender = "SYSTEM",
                Subject = "[!] Daily Login Rewards",
                Body = "Thank you for logging in today! Here are your daily rewards.\n\nLogin streak: 7 days\nBonus multiplier: 1.5x",
                ReceivedAt = DateTime.Now.AddHours(-1),
                IsRead = false,
                IsSystem = true,
                Attachments = new List<MailAttachment>
                {
                    new MailAttachment { Type = AttachmentType.Gold, Amount = 500, Description = "Daily Gold" },
                    new MailAttachment { Type = AttachmentType.Resource, ItemId = "wood", Amount = 100, Description = "Wood x100" }
                }
            });
            
            _system.Add(new MailMessage
            {
                Id = Guid.NewGuid().ToString(),
                Sender = "SYSTEM",
                Subject = "[!] Battle Report: Victory!",
                Body = "Your defense was successful!\n\nAttacker: EnemyPlayer99\nYour losses: 50 Infantry, 20 Archers\nEnemy losses: 200 Infantry, 80 Archers, 30 Cavalry\n\nResources captured: 2,500 Gold",
                ReceivedAt = DateTime.Now.AddDays(-1),
                IsRead = true,
                IsSystem = true
            });
            
            // Sent
            _sent.Add(new MailMessage
            {
                Id = Guid.NewGuid().ToString(),
                Sender = "You",
                Recipient = "Friend123",
                Subject = "Re: Alliance invitation",
                Body = "I'd love to join! Send me an invite whenever you're ready.",
                ReceivedAt = DateTime.Now.AddDays(-2),
                IsRead = true
            });
        }

        private void ShowFolder(MailFolder folder)
        {
            _currentFolderType = folder;
            
            _currentFolder = folder switch
            {
                MailFolder.Inbox => _inbox,
                MailFolder.Sent => _sent,
                MailFolder.System => _system,
                MailFolder.Trash => _trash,
                _ => _inbox
            };
            
            UpdateFolderHighlights();
            RefreshMailList();
            ClearMessageView();
        }

        private void UpdateFolderHighlights()
        {
            SetFolderHighlight(_inboxButton, _currentFolderType == MailFolder.Inbox);
            SetFolderHighlight(_sentButton, _currentFolderType == MailFolder.Sent);
            SetFolderHighlight(_systemButton, _currentFolderType == MailFolder.System);
            SetFolderHighlight(_trashButton, _currentFolderType == MailFolder.Trash);
        }

        private void SetFolderHighlight(Button btn, bool active)
        {
            if (btn == null) return;
            var image = btn.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? accentColor : new Color(0.15f, 0.15f, 0.2f);
            }
        }

        private void RefreshMailList()
        {
            foreach (Transform child in _mailListContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            foreach (var mail in _currentFolder.OrderByDescending(m => m.ReceivedAt))
            {
                CreateMailRow(mail);
            }
            
            UpdateUnreadCounter();
        }

        private void CreateMailRow(MailMessage mail)
        {
            GameObject row = new GameObject($"Mail_{mail.Id}");
            row.transform.SetParent(_mailListContainer.transform, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = row.AddComponent<Image>();
            bg.color = mail.IsRead ? readColor : unreadColor;
            
            Button btn = row.AddComponent<Button>();
            var mail_copy = mail;
            btn.onClick.AddListener(() => SelectMail(mail_copy));
            
            VerticalLayoutGroup vlayout = row.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperLeft;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 2;
            vlayout.padding = new RectOffset(10, 10, 5, 5);
            
            // Top row: sender + time
            GameObject topRow = new GameObject("TopRow");
            topRow.transform.SetParent(row.transform, false);
            LayoutElement topLE = topRow.AddComponent<LayoutElement>();
            topLE.preferredHeight = 18;
            
            HorizontalLayoutGroup topHL = topRow.AddComponent<HorizontalLayoutGroup>();
            topHL.childAlignment = TextAnchor.MiddleLeft;
            
            // Unread indicator
            if (!mail.IsRead)
            {
                GameObject indicator = new GameObject("Unread");
                indicator.transform.SetParent(topRow.transform, false);
                LayoutElement indLE = indicator.AddComponent<LayoutElement>();
                indLE.preferredWidth = 15;
                TextMeshProUGUI ind = indicator.AddComponent<TextMeshProUGUI>();
                ind.text = "o";
                ind.fontSize = 10;
                ind.color = mail.IsSystem ? systemColor : accentColor;
            }
            
            // Sender
            GameObject senderObj = new GameObject("Sender");
            senderObj.transform.SetParent(topRow.transform, false);
            LayoutElement senderLE = senderObj.AddComponent<LayoutElement>();
            senderLE.flexibleWidth = 1;
            TextMeshProUGUI sender = senderObj.AddComponent<TextMeshProUGUI>();
            sender.text = mail.Sender;
            sender.fontSize = 12;
            sender.fontStyle = mail.IsRead ? FontStyles.Normal : FontStyles.Bold;
            sender.color = mail.IsSystem ? systemColor : Color.white;
            
            // Time
            GameObject timeObj = new GameObject("Time");
            timeObj.transform.SetParent(topRow.transform, false);
            LayoutElement timeLE = timeObj.AddComponent<LayoutElement>();
            timeLE.preferredWidth = 60;
            TextMeshProUGUI time = timeObj.AddComponent<TextMeshProUGUI>();
            time.text = FormatTime(mail.ReceivedAt);
            time.fontSize = 10;
            time.color = new Color(0.5f, 0.5f, 0.5f);
            time.alignment = TextAlignmentOptions.MidlineRight;
            
            // Subject row
            GameObject subjectObj = new GameObject("Subject");
            subjectObj.transform.SetParent(row.transform, false);
            LayoutElement subjectLE = subjectObj.AddComponent<LayoutElement>();
            subjectLE.preferredHeight = 20;
            TextMeshProUGUI subject = subjectObj.AddComponent<TextMeshProUGUI>();
            string icon = mail.Attachments?.Count > 0 ? "[A] " : "";
            subject.text = $"{icon}{mail.Subject}";
            subject.fontSize = 13;
            subject.fontStyle = mail.IsRead ? FontStyles.Normal : FontStyles.Bold;
            subject.color = new Color(0.9f, 0.9f, 0.9f);
            
            // Preview
            GameObject previewObj = new GameObject("Preview");
            previewObj.transform.SetParent(row.transform, false);
            LayoutElement previewLE = previewObj.AddComponent<LayoutElement>();
            previewLE.preferredHeight = 15;
            TextMeshProUGUI preview = previewObj.AddComponent<TextMeshProUGUI>();
            string previewText = mail.Body.Length > 50 ? mail.Body.Substring(0, 50) + "..." : mail.Body;
            preview.text = previewText.Replace("\n", " ");
            preview.fontSize = 11;
            preview.color = new Color(0.5f, 0.5f, 0.55f);
        }

        private void SelectMail(MailMessage mail)
        {
            _selectedMessage = mail;
            mail.IsRead = true;
            
            _messageSubject.text = mail.Subject;
            _messageSender.text = $"From: {mail.Sender}";
            _messageDate.text = mail.ReceivedAt.ToString("MMM dd, yyyy HH:mm");
            _messageBody.text = mail.Body;
            
            // Show attachments
            if (mail.Attachments != null && mail.Attachments.Count > 0)
            {
                _attachmentContainer.SetActive(true);
                RefreshAttachmentDisplay(mail.Attachments);
            }
            else
            {
                _attachmentContainer.SetActive(false);
            }
            
            RefreshMailList();
        }

        private void RefreshAttachmentDisplay(List<MailAttachment> attachments)
        {
            // Clear existing (except header)
            for (int i = _attachmentContainer.transform.childCount - 1; i > 0; i--)
            {
                Destroy(_attachmentContainer.transform.GetChild(i).gameObject);
            }
            
            foreach (var attach in attachments)
            {
                CreateAttachmentItem(attach);
            }
        }

        private void CreateAttachmentItem(MailAttachment attachment)
        {
            GameObject itemObj = new GameObject("Attachment");
            itemObj.transform.SetParent(_attachmentContainer.transform, false);
            
            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            HorizontalLayoutGroup hlayout = itemObj.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 10;
            
            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(itemObj.transform, false);
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 25;
            TextMeshProUGUI icon = iconObj.AddComponent<TextMeshProUGUI>();
            icon.text = GetAttachmentIcon(attachment.Type);
            icon.fontSize = 18;
            icon.alignment = TextAlignmentOptions.Center;
            
            // Description
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(itemObj.transform, false);
            LayoutElement descLE = descObj.AddComponent<LayoutElement>();
            descLE.flexibleWidth = 1;
            TextMeshProUGUI desc = descObj.AddComponent<TextMeshProUGUI>();
            desc.text = attachment.Description;
            desc.fontSize = 12;
            desc.color = Color.white;
            
            // Claim button
            if (!attachment.Claimed)
            {
                GameObject claimObj = new GameObject("Claim");
                claimObj.transform.SetParent(itemObj.transform, false);
                LayoutElement claimLE = claimObj.AddComponent<LayoutElement>();
                claimLE.preferredWidth = 60;
                Image claimBg = claimObj.AddComponent<Image>();
                claimBg.color = attachmentColor;
                Button claimBtn = claimObj.AddComponent<Button>();
                var attach_copy = attachment;
                claimBtn.onClick.AddListener(() => ClaimAttachment(attach_copy));
                TextMeshProUGUI claimText = claimObj.AddComponent<TextMeshProUGUI>();
                claimText.text = "Claim";
                claimText.fontSize = 11;
                claimText.color = Color.white;
                claimText.alignment = TextAlignmentOptions.Center;
            }
        }

        private void ClaimAttachment(MailAttachment attachment)
        {
            if (attachment.Claimed) return;
            
            attachment.Claimed = true;
            NotificationSystem.Instance?.ShowSuccess($"Claimed: {attachment.Description}");
            OnAttachmentClaimed?.Invoke(attachment);
            
            if (_selectedMessage != null)
            {
                RefreshAttachmentDisplay(_selectedMessage.Attachments);
            }
        }

        private void ClearMessageView()
        {
            _selectedMessage = null;
            _messageSubject.text = "Select a message";
            _messageSender.text = "";
            _messageDate.text = "";
            _messageBody.text = "Select a message from the list to view its contents.";
            _attachmentContainer.SetActive(false);
        }

        private void ShowComposePanel()
        {
            _composePanel.SetActive(true);
            _recipientInput.text = "";
            _subjectInput.text = "";
            _bodyInput.text = "";
            _pendingAttachments.Clear();
        }

        private void HideComposePanel()
        {
            _composePanel.SetActive(false);
        }

        private void OnReplyClicked()
        {
            if (_selectedMessage == null) return;
            
            ShowComposePanel();
            _recipientInput.text = _selectedMessage.Sender;
            _subjectInput.text = $"Re: {_selectedMessage.Subject}";
            _bodyInput.text = $"\n\n--- Original Message ---\n{_selectedMessage.Body}";
        }

        private void OnDeleteClicked()
        {
            if (_selectedMessage == null) return;
            
            _currentFolder.Remove(_selectedMessage);
            if (_currentFolderType != MailFolder.Trash)
            {
                _trash.Add(_selectedMessage);
            }
            
            NotificationSystem.Instance?.ShowInfo("Message deleted");
            RefreshMailList();
            ClearMessageView();
        }

        private void OnClaimAllClicked()
        {
            if (_selectedMessage?.Attachments == null) return;
            
            int claimed = 0;
            foreach (var attach in _selectedMessage.Attachments.Where(a => !a.Claimed))
            {
                attach.Claimed = true;
                OnAttachmentClaimed?.Invoke(attach);
                claimed++;
            }
            
            if (claimed > 0)
            {
                NotificationSystem.Instance?.ShowSuccess($"Claimed {claimed} attachment(s)!");
                RefreshAttachmentDisplay(_selectedMessage.Attachments);
            }
        }

        private void OnAddAttachment()
        {
            // In real implementation, would open inventory/resource selector
            ApexLogger.Log("[MailSystem] Add attachment clicked - would open selector", ApexLogger.LogCategory.UI);
        }

        private void OnSendClicked()
        {
            if (string.IsNullOrWhiteSpace(_recipientInput.text))
            {
                NotificationSystem.Instance?.ShowError("Please enter a recipient");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(_subjectInput.text))
            {
                NotificationSystem.Instance?.ShowError("Please enter a subject");
                return;
            }
            
            var newMail = new MailMessage
            {
                Id = Guid.NewGuid().ToString(),
                Sender = "You",
                Recipient = _recipientInput.text,
                Subject = _subjectInput.text,
                Body = _bodyInput.text,
                ReceivedAt = DateTime.Now,
                IsRead = true,
                Attachments = new List<MailAttachment>(_pendingAttachments)
            };
            
            _sent.Add(newMail);
            OnMailSent?.Invoke(newMail);
            
            NotificationSystem.Instance?.ShowSuccess($"Message sent to {_recipientInput.text}");
            HideComposePanel();
        }

        private void UpdateUnreadCounter()
        {
            int unreadInbox = _inbox.Count(m => !m.IsRead);
            int unreadSystem = _system.Count(m => !m.IsRead);
            int total = unreadInbox + unreadSystem;
            
            _unreadCounter.text = total > 0 ? $"[M] {total} unread" : "📭 No unread mail";
        }

        #endregion

        #region Helpers

        private string FormatTime(DateTime time)
        {
            var diff = DateTime.Now - time;
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return time.ToString("MMM dd");
        }

        private string GetAttachmentIcon(AttachmentType type)
        {
            return type switch
            {
                AttachmentType.Gold => "[$]",
                AttachmentType.Resource => "[B]",
                AttachmentType.Item => "[?]",
                AttachmentType.Troop => "[!]",
                _ => "[A]"
            };
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel?.SetActive(true);
            ShowFolder(MailFolder.Inbox);
        }

        public void Hide()
        {
            _panel?.SetActive(false);
        }

        public void Toggle()
        {
            if (_panel != null)
            {
                if (_panel.activeSelf) Hide();
                else Show();
            }
        }

        public void ReceiveMail(MailMessage mail)
        {
            if (mail.IsSystem)
                _system.Add(mail);
            else
                _inbox.Add(mail);
            
            OnMailReceived?.Invoke(mail);
            UpdateUnreadCounter();
        }

        public int GetUnreadCount()
        {
            return _inbox.Count(m => !m.IsRead) + _system.Count(m => !m.IsRead);
        }

        #endregion
    }

    #region Data Classes

    public enum MailFolder
    {
        Inbox,
        Sent,
        System,
        Trash
    }

    public class MailMessage
    {
        public string Id;
        public string Sender;
        public string Recipient;
        public string Subject;
        public string Body;
        public DateTime ReceivedAt;
        public bool IsRead;
        public bool IsSystem;
        public List<MailAttachment> Attachments;
    }

    public class MailAttachment
    {
        public AttachmentType Type;
        public string ItemId;
        public int Amount;
        public string Description;
        public bool Claimed;
    }

    public enum AttachmentType
    {
        Gold,
        Resource,
        Item,
        Troop
    }

    #endregion
}
