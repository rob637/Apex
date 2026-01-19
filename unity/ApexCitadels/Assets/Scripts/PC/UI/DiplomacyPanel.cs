using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Diplomacy Panel - Manage relationships with other players and alliances.
    /// Treaties, trade agreements, war declarations, and peace negotiations.
    /// </summary>
    public class DiplomacyPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color allyColor = new Color(0.3f, 0.7f, 0.3f);
        [SerializeField] private Color neutralColor = new Color(0.7f, 0.7f, 0.5f);
        [SerializeField] private Color hostileColor = new Color(0.8f, 0.3f, 0.3f);
        [SerializeField] private Color warColor = new Color(0.9f, 0.2f, 0.2f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _contentContainer;
        private DiplomacyTab _selectedTab = DiplomacyTab.Relations;
        private Dictionary<DiplomacyTab, GameObject> _tabs = new Dictionary<DiplomacyTab, GameObject>();
        
        // Diplomacy data
        private Dictionary<string, DiplomaticRelation> _relations = new Dictionary<string, DiplomaticRelation>();
        private List<Treaty> _activeTreaties = new List<Treaty>();
        private List<Treaty> _pendingTreaties = new List<Treaty>();
        private List<DiplomaticMessage> _messages = new List<DiplomaticMessage>();
        
        public static DiplomacyPanel Instance { get; private set; }
        
        public event Action<string, RelationStatus> OnRelationChanged;
        public event Action<Treaty> OnTreatySigned;
        public event Action<Treaty> OnTreatyBroken;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeDiplomacy();
        }

        private void Start()
        {
            CreatePanel();
            Hide();
        }

        private void InitializeDiplomacy()
        {
            // Sample relations
            _relations["Player_DragonSlayer"] = new DiplomaticRelation
            {
                PlayerId = "Player_DragonSlayer",
                PlayerName = "DragonSlayer",
                AllianceName = "Knights of Valor",
                Status = RelationStatus.Ally,
                Reputation = 85,
                PowerLevel = 12500,
                LastInteraction = DateTime.Now.AddHours(-2),
                TotalTradesCompleted = 15,
                BattlesWon = 2,
                BattlesLost = 1
            };
            
            _relations["Player_ShadowKing"] = new DiplomaticRelation
            {
                PlayerId = "Player_ShadowKing",
                PlayerName = "ShadowKing",
                AllianceName = "Dark Empire",
                Status = RelationStatus.AtWar,
                Reputation = -50,
                PowerLevel = 18200,
                LastInteraction = DateTime.Now.AddHours(-6),
                BattlesWon = 0,
                BattlesLost = 3,
                WarStartTime = DateTime.Now.AddDays(-2)
            };
            
            _relations["Player_MerchantPrince"] = new DiplomaticRelation
            {
                PlayerId = "Player_MerchantPrince",
                PlayerName = "MerchantPrince",
                Status = RelationStatus.Friendly,
                Reputation = 45,
                PowerLevel = 8700,
                LastInteraction = DateTime.Now.AddDays(-1),
                TotalTradesCompleted = 8
            };
            
            _relations["Player_IronFist"] = new DiplomaticRelation
            {
                PlayerId = "Player_IronFist",
                PlayerName = "IronFist",
                AllianceName = "Iron Legion",
                Status = RelationStatus.Hostile,
                Reputation = -25,
                PowerLevel = 15400,
                LastInteraction = DateTime.Now.AddHours(-12),
                BattlesWon = 1,
                BattlesLost = 2
            };
            
            _relations["Player_PeaceMaker"] = new DiplomaticRelation
            {
                PlayerId = "Player_PeaceMaker",
                PlayerName = "PeaceMaker",
                AllianceName = "United Nations",
                Status = RelationStatus.Neutral,
                Reputation = 0,
                PowerLevel = 9200,
                LastInteraction = DateTime.Now.AddDays(-3)
            };
            
            // Active treaties
            _activeTreaties.Add(new Treaty
            {
                TreatyId = "TREATY_001",
                Type = TreatyType.MutualDefense,
                PartnerPlayerId = "Player_DragonSlayer",
                PartnerName = "DragonSlayer",
                StartTime = DateTime.Now.AddDays(-7),
                Duration = TimeSpan.FromDays(30),
                Terms = "Both parties agree to defend each other when attacked.",
                Benefits = "+20% defense when ally is attacked"
            });
            
            _activeTreaties.Add(new Treaty
            {
                TreatyId = "TREATY_002",
                Type = TreatyType.TradeAgreement,
                PartnerPlayerId = "Player_MerchantPrince",
                PartnerName = "MerchantPrince",
                StartTime = DateTime.Now.AddDays(-3),
                Duration = TimeSpan.FromDays(14),
                Terms = "Reduced trading fees between parties.",
                Benefits = "-50% trade fees"
            });
            
            // Pending treaties
            _pendingTreaties.Add(new Treaty
            {
                TreatyId = "TREATY_003",
                Type = TreatyType.NonAggression,
                PartnerPlayerId = "Player_IronFist",
                PartnerName = "IronFist",
                Duration = TimeSpan.FromDays(7),
                Terms = "Neither party will initiate attacks for the duration.",
                IsIncoming = true
            });
            
            // Messages
            _messages.Add(new DiplomaticMessage
            {
                MessageId = "MSG_001",
                SenderId = "Player_ShadowKing",
                SenderName = "ShadowKing",
                Subject = "Surrender Now!",
                Content = "Your territories will be ours. Surrender and pay tribute, or face total annihilation!",
                Timestamp = DateTime.Now.AddHours(-3),
                IsRead = false,
                Type = MessageType.Threat
            });
            
            _messages.Add(new DiplomaticMessage
            {
                MessageId = "MSG_002",
                SenderId = "Player_DragonSlayer",
                SenderName = "DragonSlayer",
                Subject = "Alliance War Strategy",
                Content = "Let's coordinate our attack on the eastern front tomorrow at 8 PM. I'll bring cavalry, you bring siege?",
                Timestamp = DateTime.Now.AddHours(-5),
                IsRead = true,
                Type = MessageType.Coordination
            });
            
            _messages.Add(new DiplomaticMessage
            {
                MessageId = "MSG_003",
                SenderId = "Player_PeaceMaker",
                SenderName = "PeaceMaker",
                Subject = "Trade Proposal",
                Content = "I have excess wood and need iron. Would you be interested in a trade? 5000 wood for 3000 iron.",
                Timestamp = DateTime.Now.AddDays(-1),
                IsRead = true,
                Type = MessageType.TradeOffer
            });
        }

        private void CreatePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("DiplomacyPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.08f);
            rect.anchorMax = new Vector2(0.9f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.07f, 0.1f, 0.98f);
            
            VerticalLayoutGroup vlayout = _panel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Header
            CreateHeader();
            
            // Stats overview
            CreateStatsOverview();
            
            // Tabs
            CreateTabs();
            
            // Content
            CreateContentArea();
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            
            // Title
            GameObject title = new GameObject("Title");
            title.transform.SetParent(header.transform, false);
            
            TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "🏛️ DIPLOMACY";
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = accentColor;
            
            // Close button
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(header.transform, false);
            
            LayoutElement closeLe = closeBtn.AddComponent<LayoutElement>();
            closeLe.preferredWidth = 40;
            closeLe.preferredHeight = 40;
            
            Image closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = new Color(0.6f, 0.2f, 0.2f, 0.8f);
            
            Button btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(Hide);
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(closeBtn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI x = textObj.AddComponent<TextMeshProUGUI>();
            x.text = "✕";
            x.fontSize = 24;
            x.alignment = TextAlignmentOptions.Center;
        }

        private void CreateStatsOverview()
        {
            GameObject stats = new GameObject("Stats");
            stats.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = stats.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            Image bg = stats.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f);
            
            HorizontalLayoutGroup hlayout = stats.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 30;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            // Count by status
            int allies = 0, friendly = 0, neutral = 0, hostile = 0, atWar = 0;
            foreach (var kvp in _relations)
            {
                switch (kvp.Value.Status)
                {
                    case RelationStatus.Ally: allies++; break;
                    case RelationStatus.Friendly: friendly++; break;
                    case RelationStatus.Neutral: neutral++; break;
                    case RelationStatus.Hostile: hostile++; break;
                    case RelationStatus.AtWar: atWar++; break;
                }
            }
            
            CreateStatItem(stats.transform, "🤝", "Allies", allies.ToString(), allyColor);
            CreateStatItem(stats.transform, "😊", "Friendly", friendly.ToString(), new Color(0.5f, 0.8f, 0.5f));
            CreateStatItem(stats.transform, "😐", "Neutral", neutral.ToString(), neutralColor);
            CreateStatItem(stats.transform, "😠", "Hostile", hostile.ToString(), hostileColor);
            CreateStatItem(stats.transform, "⚔️", "At War", atWar.ToString(), warColor);
            CreateStatItem(stats.transform, "📜", "Treaties", _activeTreaties.Count.ToString(), accentColor);
            CreateStatItem(stats.transform, "📨", "Pending", _pendingTreaties.Count.ToString(), new Color(0.9f, 0.7f, 0.2f));
        }

        private void CreateStatItem(Transform parent, string icon, string label, string value, Color color)
        {
            GameObject item = new GameObject($"Stat_{label}");
            item.transform.SetParent(parent, false);
            
            VerticalLayoutGroup vlayout = item.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(item.transform, $"{icon} {value}", 18, TextAlignmentOptions.Center, color);
            CreateText(item.transform, label, 10, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateTabs()
        {
            GameObject tabs = new GameObject("Tabs");
            tabs.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = tabs.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = tabs.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 5;
            
            int unreadCount = _messages.FindAll(m => !m.IsRead).Count;
            
            CreateTab(tabs.transform, DiplomacyTab.Relations, "👥 Relations");
            CreateTab(tabs.transform, DiplomacyTab.Treaties, $"📜 Treaties ({_activeTreaties.Count})");
            CreateTab(tabs.transform, DiplomacyTab.Pending, $"⏳ Pending ({_pendingTreaties.Count})");
            CreateTab(tabs.transform, DiplomacyTab.Messages, unreadCount > 0 ? $"📨 Messages ({unreadCount})" : "📨 Messages");
            CreateTab(tabs.transform, DiplomacyTab.History, "📖 History");
        }

        private void CreateTab(Transform parent, DiplomacyTab tab, string label)
        {
            GameObject tabObj = new GameObject($"Tab_{tab}");
            tabObj.transform.SetParent(parent, false);
            
            LayoutElement le = tabObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 35;
            
            Color bgColor = tab == _selectedTab ? accentColor : new Color(0.15f, 0.15f, 0.2f);
            
            Image bg = tabObj.AddComponent<Image>();
            bg.color = bgColor;
            
            Button btn = tabObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectTab(tab));
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tabObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 13;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            _tabs[tab] = tabObj;
        }

        private void CreateContentArea()
        {
            _contentContainer = new GameObject("Content");
            _contentContainer.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _contentContainer.AddComponent<LayoutElement>();
            le.flexibleHeight = 1;
            
            Image bg = _contentContainer.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.5f);
            
            VerticalLayoutGroup vlayout = _contentContainer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            RefreshContent();
        }

        private void RefreshContent()
        {
            foreach (Transform child in _contentContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            switch (_selectedTab)
            {
                case DiplomacyTab.Relations:
                    CreateRelationsContent();
                    break;
                case DiplomacyTab.Treaties:
                    CreateTreatiesContent();
                    break;
                case DiplomacyTab.Pending:
                    CreatePendingContent();
                    break;
                case DiplomacyTab.Messages:
                    CreateMessagesContent();
                    break;
                case DiplomacyTab.History:
                    CreateHistoryContent();
                    break;
            }
        }

        private void CreateRelationsContent()
        {
            // Sort by status (allies first, then by power)
            List<DiplomaticRelation> sorted = new List<DiplomaticRelation>(_relations.Values);
            sorted.Sort((a, b) =>
            {
                int statusCompare = GetStatusPriority(a.Status).CompareTo(GetStatusPriority(b.Status));
                if (statusCompare != 0) return statusCompare;
                return b.PowerLevel.CompareTo(a.PowerLevel);
            });
            
            foreach (var relation in sorted)
            {
                CreateRelationCard(relation);
            }
        }

        private int GetStatusPriority(RelationStatus status)
        {
            return status switch
            {
                RelationStatus.AtWar => 0,
                RelationStatus.Ally => 1,
                RelationStatus.Hostile => 2,
                RelationStatus.Friendly => 3,
                RelationStatus.Neutral => 4,
                _ => 5
            };
        }

        private void CreateRelationCard(DiplomaticRelation relation)
        {
            GameObject card = new GameObject($"Relation_{relation.PlayerId}");
            card.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Color statusColor = GetStatusColor(relation.Status);
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(statusColor.r * 0.2f, statusColor.g * 0.2f, statusColor.b * 0.2f);
            
            UnityEngine.UI.Outline outline = card.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = statusColor;
            outline.effectDistance = new Vector2(1, 1);
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Status indicator
            CreateText(card.transform, GetStatusEmoji(relation.Status), 32, TextAlignmentOptions.Center);
            
            // Player info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 3;
            
            CreateText(info.transform, $"<b>{relation.PlayerName}</b>", 16, TextAlignmentOptions.Left, Color.white);
            
            if (!string.IsNullOrEmpty(relation.AllianceName))
            {
                CreateText(info.transform, $"⚔️ {relation.AllianceName}", 11, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.8f));
            }
            
            CreateText(info.transform, $"Power: {relation.PowerLevel:N0}", 11, TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.8f));
            
            // Reputation bar
            CreateReputationBar(info.transform, relation.Reputation);
            
            // Stats
            GameObject stats = new GameObject("Stats");
            stats.transform.SetParent(card.transform, false);
            
            VerticalLayoutGroup statsVL = stats.AddComponent<VerticalLayoutGroup>();
            statsVL.childAlignment = TextAnchor.MiddleRight;
            
            CreateText(stats.transform, relation.Status.ToString(), 12, TextAlignmentOptions.Right, statusColor);
            
            if (relation.TotalTradesCompleted > 0)
            {
                CreateText(stats.transform, $"📦 {relation.TotalTradesCompleted} trades", 10, TextAlignmentOptions.Right, new Color(0.6f, 0.6f, 0.6f));
            }
            
            if (relation.BattlesWon > 0 || relation.BattlesLost > 0)
            {
                CreateText(stats.transform, $"⚔️ {relation.BattlesWon}W / {relation.BattlesLost}L", 10, TextAlignmentOptions.Right, new Color(0.6f, 0.6f, 0.6f));
            }
            
            // Actions
            CreateRelationActions(card.transform, relation);
        }

        private void CreateReputationBar(Transform parent, int reputation)
        {
            GameObject bar = new GameObject("RepBar");
            bar.transform.SetParent(parent, false);
            
            LayoutElement le = bar.AddComponent<LayoutElement>();
            le.preferredHeight = 10;
            le.preferredWidth = 120;
            
            HorizontalLayoutGroup hlayout = bar.AddComponent<HorizontalLayoutGroup>();
            hlayout.spacing = 5;
            
            // Bar background
            GameObject barBg = new GameObject("Bg");
            barBg.transform.SetParent(bar.transform, false);
            
            LayoutElement bgLE = barBg.AddComponent<LayoutElement>();
            bgLE.preferredWidth = 80;
            bgLE.preferredHeight = 8;
            
            Image bgImg = barBg.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f);
            
            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(barBg.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            float normalizedRep = (reputation + 100f) / 200f; // -100 to 100 -> 0 to 1
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(normalizedRep, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = reputation >= 0 ? allyColor : hostileColor;
            
            // Text
            CreateText(bar.transform, reputation >= 0 ? $"+{reputation}" : reputation.ToString(), 10, TextAlignmentOptions.Left, 
                      reputation >= 50 ? allyColor : reputation <= -50 ? hostileColor : neutralColor);
        }

        private void CreateRelationActions(Transform parent, DiplomaticRelation relation)
        {
            GameObject actions = new GameObject("Actions");
            actions.transform.SetParent(parent, false);
            
            VerticalLayoutGroup vlayout = actions.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            
            switch (relation.Status)
            {
                case RelationStatus.AtWar:
                    CreateSmallButton(actions.transform, "☮️ Peace", () => ProposePeace(relation), new Color(0.3f, 0.6f, 0.3f));
                    break;
                case RelationStatus.Ally:
                    CreateSmallButton(actions.transform, "📨 Message", () => SendMessage(relation), accentColor);
                    break;
                case RelationStatus.Hostile:
                    CreateSmallButton(actions.transform, "⚔️ War", () => DeclareWar(relation), warColor);
                    CreateSmallButton(actions.transform, "📜 Treaty", () => ProposeTreaty(relation), accentColor);
                    break;
                case RelationStatus.Friendly:
                case RelationStatus.Neutral:
                    CreateSmallButton(actions.transform, "📜 Treaty", () => ProposeTreaty(relation), accentColor);
                    CreateSmallButton(actions.transform, "📦 Trade", () => ProposeTrade(relation), new Color(0.8f, 0.6f, 0.2f));
                    break;
            }
        }

        private void CreateTreatiesContent()
        {
            if (_activeTreaties.Count == 0)
            {
                CreateText(_contentContainer.transform, "No active treaties. Negotiate with other players to form agreements!", 14, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
                return;
            }
            
            foreach (var treaty in _activeTreaties)
            {
                CreateTreatyCard(treaty, false);
            }
        }

        private void CreatePendingContent()
        {
            if (_pendingTreaties.Count == 0)
            {
                CreateText(_contentContainer.transform, "No pending treaties. Send proposals to negotiate with others!", 14, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
                return;
            }
            
            foreach (var treaty in _pendingTreaties)
            {
                CreateTreatyCard(treaty, true);
            }
        }

        private void CreateTreatyCard(Treaty treaty, bool isPending)
        {
            GameObject card = new GameObject($"Treaty_{treaty.TreatyId}");
            card.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 90;
            
            Color typeColor = GetTreatyColor(treaty.Type);
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(typeColor.r * 0.15f, typeColor.g * 0.15f, typeColor.b * 0.15f);
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Icon
            CreateText(card.transform, GetTreatyIcon(treaty.Type), 32, TextAlignmentOptions.Center);
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 3;
            
            string directionStr = isPending ? (treaty.IsIncoming ? "From: " : "To: ") : "With: ";
            CreateText(info.transform, $"<b>{treaty.Type}</b>", 15, TextAlignmentOptions.Left, typeColor);
            CreateText(info.transform, $"{directionStr}{treaty.PartnerName}", 12, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, treaty.Terms, 10, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
            CreateText(info.transform, $"✨ {treaty.Benefits}", 10, TextAlignmentOptions.Left, new Color(0.5f, 0.8f, 0.5f));
            
            // Duration/Status
            GameObject status = new GameObject("Status");
            status.transform.SetParent(card.transform, false);
            
            VerticalLayoutGroup statusVL = status.AddComponent<VerticalLayoutGroup>();
            statusVL.childAlignment = TextAnchor.MiddleRight;
            
            if (isPending)
            {
                CreateText(status.transform, "PENDING", 12, TextAlignmentOptions.Right, new Color(0.9f, 0.7f, 0.2f));
                CreateText(status.transform, $"Duration: {treaty.Duration.Days}d", 10, TextAlignmentOptions.Right, new Color(0.6f, 0.6f, 0.6f));
            }
            else
            {
                TimeSpan remaining = (treaty.StartTime + treaty.Duration) - DateTime.Now;
                string timeStr = remaining.TotalDays >= 1 ? $"{(int)remaining.TotalDays}d left" : $"{(int)remaining.TotalHours}h left";
                CreateText(status.transform, "ACTIVE", 12, TextAlignmentOptions.Right, allyColor);
                CreateText(status.transform, timeStr, 10, TextAlignmentOptions.Right, new Color(0.6f, 0.6f, 0.6f));
            }
            
            // Actions
            if (isPending)
            {
                if (treaty.IsIncoming)
                {
                    CreateSmallButton(card.transform, "✓ Accept", () => AcceptTreaty(treaty), allyColor);
                    CreateSmallButton(card.transform, "✗ Decline", () => DeclineTreaty(treaty), hostileColor);
                }
                else
                {
                    CreateSmallButton(card.transform, "Cancel", () => CancelTreaty(treaty), new Color(0.5f, 0.5f, 0.5f));
                }
            }
            else
            {
                CreateSmallButton(card.transform, "Break", () => BreakTreaty(treaty), hostileColor);
            }
        }

        private void CreateMessagesContent()
        {
            if (_messages.Count == 0)
            {
                CreateText(_contentContainer.transform, "No diplomatic messages. Start a conversation!", 14, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
                return;
            }
            
            // Sort by unread first, then by date
            List<DiplomaticMessage> sorted = new List<DiplomaticMessage>(_messages);
            sorted.Sort((a, b) =>
            {
                if (a.IsRead != b.IsRead) return a.IsRead ? 1 : -1;
                return b.Timestamp.CompareTo(a.Timestamp);
            });
            
            foreach (var msg in sorted)
            {
                CreateMessageCard(msg);
            }
        }

        private void CreateMessageCard(DiplomaticMessage msg)
        {
            GameObject card = new GameObject($"Message_{msg.MessageId}");
            card.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 70;
            
            Color typeColor = GetMessageColor(msg.Type);
            
            Image bg = card.AddComponent<Image>();
            bg.color = msg.IsRead ? new Color(0.08f, 0.08f, 0.1f) : new Color(typeColor.r * 0.15f, typeColor.g * 0.15f, typeColor.b * 0.15f);
            
            if (!msg.IsRead)
            {
                UnityEngine.UI.Outline outline = card.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = typeColor;
                outline.effectDistance = new Vector2(1, 1);
            }
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Icon
            CreateText(card.transform, GetMessageIcon(msg.Type), 24, TextAlignmentOptions.Center);
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 2;
            
            string readIndicator = msg.IsRead ? "" : "● ";
            CreateText(info.transform, $"{readIndicator}<b>{msg.Subject}</b>", 13, TextAlignmentOptions.Left, msg.IsRead ? Color.white : typeColor);
            CreateText(info.transform, $"From: {msg.SenderName}", 11, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
            
            string preview = msg.Content.Length > 60 ? msg.Content.Substring(0, 60) + "..." : msg.Content;
            CreateText(info.transform, preview, 10, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            
            // Time
            TimeSpan ago = DateTime.Now - msg.Timestamp;
            string timeStr = ago.TotalDays >= 1 ? $"{(int)ago.TotalDays}d ago" : ago.TotalHours >= 1 ? $"{(int)ago.TotalHours}h ago" : $"{(int)ago.TotalMinutes}m ago";
            CreateText(card.transform, timeStr, 10, TextAlignmentOptions.Right, new Color(0.4f, 0.4f, 0.4f));
            
            // Actions
            CreateSmallButton(card.transform, "📖 Read", () => ReadMessage(msg), accentColor);
            CreateSmallButton(card.transform, "↩️ Reply", () => ReplyToMessage(msg), new Color(0.4f, 0.4f, 0.5f));
        }

        private void CreateHistoryContent()
        {
            CreateText(_contentContainer.transform, "📜 Diplomatic History", 16, TextAlignmentOptions.Center, accentColor);
            
            // Recent events
            CreateHistoryItem("⚔️ War declared by ShadowKing", DateTime.Now.AddDays(-2), warColor);
            CreateHistoryItem("📜 Trade Agreement signed with MerchantPrince", DateTime.Now.AddDays(-3), allyColor);
            CreateHistoryItem("🤝 Alliance formed with DragonSlayer", DateTime.Now.AddDays(-7), accentColor);
            CreateHistoryItem("☮️ Peace treaty ended with IronFist", DateTime.Now.AddDays(-10), neutralColor);
        }

        private void CreateHistoryItem(string text, DateTime date, Color color)
        {
            GameObject item = new GameObject("HistoryItem");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            CreateText(item.transform, date.ToString("MMM dd"), 11, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            
            GameObject textObj = CreateText(item.transform, text, 12, TextAlignmentOptions.Left, color);
            textObj.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        #region UI Helpers

        private GameObject CreateText(Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Color? color = null)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color ?? Color.white;
            tmp.enableWordWrapping = false;
            
            return obj;
        }

        private void CreateSmallButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btn = new GameObject($"Btn_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 70;
            le.preferredHeight = 25;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 10;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private Color GetStatusColor(RelationStatus status)
        {
            return status switch
            {
                RelationStatus.Ally => allyColor,
                RelationStatus.Friendly => new Color(0.5f, 0.8f, 0.5f),
                RelationStatus.Neutral => neutralColor,
                RelationStatus.Hostile => hostileColor,
                RelationStatus.AtWar => warColor,
                _ => Color.white
            };
        }

        private string GetStatusEmoji(RelationStatus status)
        {
            return status switch
            {
                RelationStatus.Ally => "🤝",
                RelationStatus.Friendly => "😊",
                RelationStatus.Neutral => "😐",
                RelationStatus.Hostile => "😠",
                RelationStatus.AtWar => "⚔️",
                _ => "❓"
            };
        }

        private Color GetTreatyColor(TreatyType type)
        {
            return type switch
            {
                TreatyType.MutualDefense => allyColor,
                TreatyType.TradeAgreement => new Color(0.8f, 0.6f, 0.2f),
                TreatyType.NonAggression => neutralColor,
                TreatyType.Alliance => accentColor,
                TreatyType.Vassalage => new Color(0.6f, 0.3f, 0.6f),
                _ => Color.white
            };
        }

        private string GetTreatyIcon(TreatyType type)
        {
            return type switch
            {
                TreatyType.MutualDefense => "🛡️",
                TreatyType.TradeAgreement => "📦",
                TreatyType.NonAggression => "☮️",
                TreatyType.Alliance => "🤝",
                TreatyType.Vassalage => "👑",
                _ => "📜"
            };
        }

        private Color GetMessageColor(MessageType type)
        {
            return type switch
            {
                MessageType.Threat => warColor,
                MessageType.TradeOffer => new Color(0.8f, 0.6f, 0.2f),
                MessageType.Coordination => accentColor,
                MessageType.PeaceOffer => allyColor,
                _ => Color.white
            };
        }

        private string GetMessageIcon(MessageType type)
        {
            return type switch
            {
                MessageType.Threat => "⚠️",
                MessageType.TradeOffer => "📦",
                MessageType.Coordination => "🎯",
                MessageType.PeaceOffer => "☮️",
                _ => "📨"
            };
        }

        #endregion

        #region Diplomatic Actions

        private void DeclareWar(DiplomaticRelation relation)
        {
            relation.Status = RelationStatus.AtWar;
            relation.WarStartTime = DateTime.Now;
            relation.Reputation -= 30;
            
            OnRelationChanged?.Invoke(relation.PlayerId, RelationStatus.AtWar);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowWarning($"War declared on {relation.PlayerName}!");
            }
            
            RefreshContent();
            ApexLogger.Log($"[Diplomacy] War declared on {relation.PlayerName}", ApexLogger.LogCategory.UI);
        }

        private void ProposePeace(DiplomaticRelation relation)
        {
            ApexLogger.Log($"[Diplomacy] Proposing peace to {relation.PlayerName}", ApexLogger.LogCategory.UI);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Peace proposal sent to {relation.PlayerName}");
            }
        }

        private void ProposeTreaty(DiplomaticRelation relation)
        {
            ApexLogger.Log($"[Diplomacy] Opening treaty dialog for {relation.PlayerName}", ApexLogger.LogCategory.UI);
            // TODO: Open treaty creation dialog
        }

        private void ProposeTrade(DiplomaticRelation relation)
        {
            ApexLogger.Log($"[Diplomacy] Opening trade dialog for {relation.PlayerName}", ApexLogger.LogCategory.UI);
            // TODO: Open trade panel
        }

        private void SendMessage(DiplomaticRelation relation)
        {
            ApexLogger.Log($"[Diplomacy] Opening message composer for {relation.PlayerName}", ApexLogger.LogCategory.UI);
            // TODO: Open message composer
        }

        private void AcceptTreaty(Treaty treaty)
        {
            _pendingTreaties.Remove(treaty);
            treaty.StartTime = DateTime.Now;
            _activeTreaties.Add(treaty);
            
            OnTreatySigned?.Invoke(treaty);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"Treaty with {treaty.PartnerName} accepted!");
            }
            
            RefreshContent();
            ApexLogger.Log($"[Diplomacy] Treaty accepted: {treaty.Type} with {treaty.PartnerName}", ApexLogger.LogCategory.UI);
        }

        private void DeclineTreaty(Treaty treaty)
        {
            _pendingTreaties.Remove(treaty);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Treaty proposal declined");
            }
            
            RefreshContent();
            ApexLogger.Log($"[Diplomacy] Treaty declined: {treaty.Type} with {treaty.PartnerName}", ApexLogger.LogCategory.UI);
        }

        private void CancelTreaty(Treaty treaty)
        {
            _pendingTreaties.Remove(treaty);
            RefreshContent();
            ApexLogger.Log($"[Diplomacy] Treaty proposal cancelled", ApexLogger.LogCategory.UI);
        }

        private void BreakTreaty(Treaty treaty)
        {
            _activeTreaties.Remove(treaty);
            
            // Reputation penalty
            if (_relations.TryGetValue(treaty.PartnerPlayerId, out DiplomaticRelation relation))
            {
                relation.Reputation -= 25;
                relation.Status = RelationStatus.Hostile;
            }
            
            OnTreatyBroken?.Invoke(treaty);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowWarning($"Treaty broken! Reputation decreased.");
            }
            
            RefreshContent();
            ApexLogger.Log($"[Diplomacy] Treaty broken with {treaty.PartnerName}", ApexLogger.LogCategory.UI);
        }

        private void ReadMessage(DiplomaticMessage msg)
        {
            msg.IsRead = true;
            RefreshContent();
            ApexLogger.Log($"[Diplomacy] Reading message: {msg.Subject}", ApexLogger.LogCategory.UI);
            // TODO: Open full message view
        }

        private void ReplyToMessage(DiplomaticMessage msg)
        {
            ApexLogger.Log($"[Diplomacy] Replying to {msg.SenderName}", ApexLogger.LogCategory.UI);
            // TODO: Open message composer
        }

        #endregion

        private void SelectTab(DiplomacyTab tab)
        {
            _selectedTab = tab;
            
            foreach (var kvp in _tabs)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == tab ? accentColor : new Color(0.15f, 0.15f, 0.2f);
            }
            
            RefreshContent();
        }

        #region Public API

        public void Show()
        {
            _panel.SetActive(true);
            RefreshContent();
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        public void Toggle()
        {
            if (_panel.activeSelf)
                Hide();
            else
                Show();
        }

        public RelationStatus GetRelationStatus(string playerId)
        {
            return _relations.TryGetValue(playerId, out DiplomaticRelation rel) ? rel.Status : RelationStatus.Neutral;
        }

        public int GetUnreadMessageCount()
        {
            return _messages.FindAll(m => !m.IsRead).Count;
        }

        #endregion
    }

    #region Data Classes

    public enum DiplomacyTab
    {
        Relations,
        Treaties,
        Pending,
        Messages,
        History
    }

    public enum RelationStatus
    {
        Ally,
        Friendly,
        Neutral,
        Hostile,
        AtWar
    }

    public enum TreatyType
    {
        NonAggression,
        TradeAgreement,
        MutualDefense,
        Alliance,
        Vassalage
    }

    public enum MessageType
    {
        General,
        Threat,
        TradeOffer,
        Coordination,
        PeaceOffer
    }

    public class DiplomaticRelation
    {
        public string PlayerId;
        public string PlayerName;
        public string AllianceName;
        public RelationStatus Status;
        public int Reputation;
        public int PowerLevel;
        public DateTime LastInteraction;
        public int TotalTradesCompleted;
        public int BattlesWon;
        public int BattlesLost;
        public DateTime? WarStartTime;
    }

    public class Treaty
    {
        public string TreatyId;
        public TreatyType Type;
        public string PartnerPlayerId;
        public string PartnerName;
        public DateTime StartTime;
        public TimeSpan Duration;
        public string Terms;
        public string Benefits;
        public bool IsIncoming;
    }

    public class DiplomaticMessage
    {
        public string MessageId;
        public string SenderId;
        public string SenderName;
        public string Subject;
        public string Content;
        public DateTime Timestamp;
        public bool IsRead;
        public MessageType Type;
    }

    #endregion
}
