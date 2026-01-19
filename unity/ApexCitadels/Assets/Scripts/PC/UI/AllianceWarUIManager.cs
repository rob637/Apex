using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Alliance War UI System for war coordination and battle status
    /// Features:
    /// - War declaration interface
    /// - Battle map with live status
    /// - Alliance coordination tools
    /// - War results display
    /// - Resource contribution tracking
    /// </summary>
    public class AllianceWarUIManager : MonoBehaviour
    {
        [Header("Main Panels")]
        [SerializeField] private GameObject warOverviewPanel;
        [SerializeField] private GameObject warDeclarationPanel;
        [SerializeField] private GameObject battleMapPanel;
        [SerializeField] private GameObject warResultsPanel;
        [SerializeField] private CanvasGroup mainCanvasGroup;
        
        [Header("Overview Components")]
        [SerializeField] private TextMeshProUGUI warStatusText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private Image allianceALogo;
        [SerializeField] private Image allianceBLogo;
        [SerializeField] private TextMeshProUGUI allianceAName;
        [SerializeField] private TextMeshProUGUI allianceBName;
        [SerializeField] private TextMeshProUGUI allianceAScore;
        [SerializeField] private TextMeshProUGUI allianceBScore;
        [SerializeField] private Slider warProgressSlider;
        
        [Header("Battle Map")]
        [SerializeField] private RectTransform battleMapContainer;
        [SerializeField] private GameObject territoryMarkerPrefab;
        [SerializeField] private LineRenderer frontlineRenderer;
        
        [Header("Coordination")]
        [SerializeField] private Transform memberListContainer;
        [SerializeField] private GameObject memberItemPrefab;
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private ScrollRect chatScrollRect;
        [SerializeField] private Transform chatMessageContainer;
        [SerializeField] private GameObject chatMessagePrefab;
        
        [Header("Contribution")]
        [SerializeField] private TextMeshProUGUI personalContributionText;
        [SerializeField] private Slider contributionProgressSlider;
        [SerializeField] private Transform contributionLeaderboard;
        [SerializeField] private GameObject leaderboardEntryPrefab;
        
        [Header("Buttons")]
        [SerializeField] private Button declareWarButton;
        [SerializeField] private Button viewBattleMapButton;
        [SerializeField] private Button contributeButton;
        [SerializeField] private Button sendMessageButton;
        [SerializeField] private Button closeButton;
        
        // Singleton
        private static AllianceWarUIManager _instance;
        public static AllianceWarUIManager Instance => _instance;
        
        // State
        private AllianceWar _currentWar;
        private List<WarTerritoryMarker> _territoryMarkers = new List<WarTerritoryMarker>();
        private bool _isVisible;
        
        // Events
        public event Action<string> OnWarDeclared;
        public event Action<int> OnResourceContributed;
        public event Action<string> OnChatMessageSent;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeButtons();
        }
        
        private void InitializeButtons()
        {
            declareWarButton?.onClick.AddListener(OnDeclareWarClicked);
            viewBattleMapButton?.onClick.AddListener(ShowBattleMap);
            contributeButton?.onClick.AddListener(OnContributeClicked);
            sendMessageButton?.onClick.AddListener(OnSendMessageClicked);
            closeButton?.onClick.AddListener(Hide);
        }
        
        private void Start()
        {
            // Load demo war for testing
            LoadDemoWar();
        }
        
        private void LoadDemoWar()
        {
            _currentWar = new AllianceWar
            {
                id = "war_001",
                status = WarStatus.InProgress,
                startTime = DateTime.Now.AddHours(-2),
                endTime = DateTime.Now.AddHours(22),
                allianceA = new WarAlliance
                {
                    id = "alliance_1",
                    name = "Iron Legion",
                    score = 12500,
                    members = new List<WarMember>
                    {
                        new WarMember { name = "Commander", contribution = 3200, online = true },
                        new WarMember { name = "Warlord", contribution = 2800, online = true },
                        new WarMember { name = "Striker", contribution = 2100, online = false }
                    }
                },
                allianceB = new WarAlliance
                {
                    id = "alliance_2",
                    name = "Shadow Council",
                    score = 11200,
                    members = new List<WarMember>()
                },
                territories = new List<WarTerritory>
                {
                    new WarTerritory { id = "t1", name = "Northern Fortress", owner = "alliance_1", contested = true },
                    new WarTerritory { id = "t2", name = "Eastern Mines", owner = "alliance_2", contested = false },
                    new WarTerritory { id = "t3", name = "Central Keep", owner = "alliance_1", contested = true }
                }
            };
            
            UpdateWarDisplay();
        }
        
        #region Public API
        
        public void Show()
        {
            _isVisible = true;
            gameObject.SetActive(true);
            StartCoroutine(AnimateIn());
            StartCoroutine(UpdateCountdown());
        }
        
        public void Hide()
        {
            _isVisible = false;
            StartCoroutine(AnimateOut());
        }
        
        public void SetWar(AllianceWar war)
        {
            _currentWar = war;
            UpdateWarDisplay();
        }
        
        public void UpdateScore(string allianceId, int newScore)
        {
            if (_currentWar == null) return;
            
            if (_currentWar.allianceA.id == allianceId)
            {
                _currentWar.allianceA.score = newScore;
            }
            else if (_currentWar.allianceB.id == allianceId)
            {
                _currentWar.allianceB.score = newScore;
            }
            
            UpdateScoreDisplay();
            AnimateScoreChange();
        }
        
        public void AddChatMessage(string sender, string message, bool isSystem = false)
        {
            if (chatMessagePrefab == null || chatMessageContainer == null) return;
            
            var msgObj = Instantiate(chatMessagePrefab, chatMessageContainer);
            var text = msgObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (text != null)
            {
                if (isSystem)
                {
                    text.text = $"<color=#FFAA00>[SYSTEM] {message}</color>";
                }
                else
                {
                    text.text = $"<color=#88CCFF>{sender}:</color> {message}";
                }
            }
            
            // Auto-scroll
            Canvas.ForceUpdateCanvases();
            chatScrollRect?.verticalScrollbar?.SetValueWithoutNotify(0);
        }
        
        public void UpdateTerritoryStatus(string territoryId, string newOwner, bool contested)
        {
            if (_currentWar == null) return;
            
            var territory = _currentWar.territories.Find(t => t.id == territoryId);
            if (territory != null)
            {
                territory.owner = newOwner;
                territory.contested = contested;
                
                UpdateBattleMap();
                
                // Announce change
                string announcement = contested 
                    ? $"⚔️ {territory.name} is under attack!"
                    : $"🏴 {territory.name} captured!";
                AddChatMessage("War", announcement, true);
            }
        }
        
        #endregion
        
        #region Display
        
        private void UpdateWarDisplay()
        {
            if (_currentWar == null) return;
            
            // Status
            if (warStatusText != null)
            {
                warStatusText.text = GetStatusText(_currentWar.status);
            }
            
            // Alliance info
            if (allianceAName != null) allianceAName.text = _currentWar.allianceA.name;
            if (allianceBName != null) allianceBName.text = _currentWar.allianceB.name;
            
            UpdateScoreDisplay();
            UpdateMemberList();
            UpdateContributionDisplay();
            UpdateBattleMap();
        }
        
        private string GetStatusText(WarStatus status)
        {
            return status switch
            {
                WarStatus.Preparation => "⏳ PREPARATION PHASE",
                WarStatus.InProgress => "⚔️ WAR IN PROGRESS",
                WarStatus.Concluded => "🏆 WAR CONCLUDED",
                _ => "UNKNOWN"
            };
        }
        
        private void UpdateScoreDisplay()
        {
            if (_currentWar == null) return;
            
            int scoreA = _currentWar.allianceA.score;
            int scoreB = _currentWar.allianceB.score;
            int total = scoreA + scoreB;
            
            if (allianceAScore != null) allianceAScore.text = scoreA.ToString("N0");
            if (allianceBScore != null) allianceBScore.text = scoreB.ToString("N0");
            
            if (warProgressSlider != null && total > 0)
            {
                warProgressSlider.value = (float)scoreA / total;
            }
        }
        
        private void AnimateScoreChange()
        {
            // Flash effect on score change
            StartCoroutine(FlashScore());
        }
        
        private IEnumerator FlashScore()
        {
            if (allianceAScore != null)
            {
                Color originalColor = allianceAScore.color;
                allianceAScore.color = Color.yellow;
                yield return new WaitForSeconds(0.3f);
                allianceAScore.color = originalColor;
            }
        }
        
        private void UpdateMemberList()
        {
            if (memberListContainer == null || memberItemPrefab == null || _currentWar == null)
                return;
            
            // Clear existing
            foreach (Transform child in memberListContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Add members
            foreach (var member in _currentWar.allianceA.members)
            {
                var item = Instantiate(memberItemPrefab, memberListContainer);
                var memberUI = item.GetComponent<WarMemberItem>();
                memberUI?.Setup(member);
            }
        }
        
        private void UpdateContributionDisplay()
        {
            if (_currentWar == null) return;
            
            // Personal contribution
            int personal = 1500; // Would come from player data
            int goal = 5000;
            
            if (personalContributionText != null)
            {
                personalContributionText.text = $"{personal:N0} / {goal:N0}";
            }
            
            if (contributionProgressSlider != null)
            {
                contributionProgressSlider.value = (float)personal / goal;
            }
            
            // Leaderboard
            UpdateContributionLeaderboard();
        }
        
        private void UpdateContributionLeaderboard()
        {
            if (contributionLeaderboard == null || leaderboardEntryPrefab == null || _currentWar == null)
                return;
            
            // Clear existing
            foreach (Transform child in contributionLeaderboard)
            {
                Destroy(child.gameObject);
            }
            
            // Sort members by contribution
            var sortedMembers = new List<WarMember>(_currentWar.allianceA.members);
            sortedMembers.Sort((a, b) => b.contribution.CompareTo(a.contribution));
            
            // Add entries
            for (int i = 0; i < sortedMembers.Count; i++)
            {
                var entry = Instantiate(leaderboardEntryPrefab, contributionLeaderboard);
                var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
                
                if (texts.Length >= 3)
                {
                    texts[0].text = $"#{i + 1}";
                    texts[1].text = sortedMembers[i].name;
                    texts[2].text = sortedMembers[i].contribution.ToString("N0");
                }
            }
        }
        
        private void UpdateBattleMap()
        {
            if (_currentWar == null || battleMapContainer == null) return;
            
            // Clear existing markers
            foreach (var marker in _territoryMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker.gameObject);
                }
            }
            _territoryMarkers.Clear();
            
            // Create territory markers
            foreach (var territory in _currentWar.territories)
            {
                if (territoryMarkerPrefab != null)
                {
                    var markerObj = Instantiate(territoryMarkerPrefab, battleMapContainer);
                    var marker = markerObj.GetComponent<WarTerritoryMarker>();
                    marker?.Setup(territory, _currentWar.allianceA.id, _currentWar.allianceB.id);
                    _territoryMarkers.Add(marker);
                }
            }
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnDeclareWarClicked()
        {
            warDeclarationPanel?.SetActive(true);
            warOverviewPanel?.SetActive(false);
        }
        
        private void ShowBattleMap()
        {
            battleMapPanel?.SetActive(true);
            warOverviewPanel?.SetActive(false);
        }
        
        private void OnContributeClicked()
        {
            // Open contribution popup
            int contribution = 100;
            OnResourceContributed?.Invoke(contribution);
            
            AddChatMessage("You", $"Contributed {contribution} resources!", true);
        }
        
        private void OnSendMessageClicked()
        {
            if (chatInput == null || string.IsNullOrEmpty(chatInput.text)) return;
            
            string message = chatInput.text;
            chatInput.text = "";
            
            AddChatMessage("You", message);
            OnChatMessageSent?.Invoke(message);
        }
        
        #endregion
        
        #region Coroutines
        
        private IEnumerator AnimateIn()
        {
            if (mainCanvasGroup == null) yield break;
            
            mainCanvasGroup.alpha = 0;
            
            float elapsed = 0;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                mainCanvasGroup.alpha = elapsed / duration;
                yield return null;
            }
            
            mainCanvasGroup.alpha = 1;
        }
        
        private IEnumerator AnimateOut()
        {
            if (mainCanvasGroup == null) yield break;
            
            float elapsed = 0;
            float duration = 0.2f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                mainCanvasGroup.alpha = 1 - (elapsed / duration);
                yield return null;
            }
            
            gameObject.SetActive(false);
        }
        
        private IEnumerator UpdateCountdown()
        {
            while (_isVisible && _currentWar != null)
            {
                var timeLeft = _currentWar.endTime - DateTime.Now;
                
                if (countdownText != null)
                {
                    if (timeLeft.TotalSeconds <= 0)
                    {
                        countdownText.text = "WAR ENDED";
                    }
                    else if (timeLeft.TotalHours >= 1)
                    {
                        countdownText.text = $"⏱ {(int)timeLeft.TotalHours}:{timeLeft.Minutes:D2}:{timeLeft.Seconds:D2}";
                    }
                    else
                    {
                        countdownText.text = $"⏱ {timeLeft.Minutes:D2}:{timeLeft.Seconds:D2}";
                    }
                }
                
                yield return new WaitForSecondsRealtime(1f);
            }
        }
        
        #endregion
    }
    
    #region Data Classes
    
    [Serializable]
    public class WarUIData
    {
        public string id;
        public WarStatus status;
        public DateTime startTime;
        public DateTime endTime;
        public WarAlliance allianceA;
        public WarAlliance allianceB;
        public List<WarTerritory> territories;
    }
    
    [Serializable]
    public class WarAlliance
    {
        public string id;
        public string name;
        public int score;
        public Sprite logo;
        public List<WarMember> members;
    }
    
    [Serializable]
    public class WarMember
    {
        public string id;
        public string name;
        public int contribution;
        public bool online;
        public string role;
    }
    
    [Serializable]
    public class WarTerritory
    {
        public string id;
        public string name;
        public string owner;
        public bool contested;
        public Vector2 mapPosition;
        public int pointValue;
    }
    
    public enum WarStatus
    {
        Preparation,
        InProgress,
        Concluded
    }
    
    #endregion
    
    /// <summary>
    /// War member list item
    /// </summary>
    public class WarMemberItem : MonoBehaviour
    {
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI roleText;
        [SerializeField] private TextMeshProUGUI contributionText;
        [SerializeField] private Image onlineIndicator;
        
        public void Setup(WarMember member)
        {
            if (nameText != null) nameText.text = member.name;
            if (roleText != null) roleText.text = member.role ?? "Member";
            if (contributionText != null) contributionText.text = $"+{member.contribution:N0}";
            
            if (onlineIndicator != null)
            {
                onlineIndicator.color = member.online ? Color.green : Color.gray;
            }
        }
    }
    
    /// <summary>
    /// Territory marker on battle map
    /// </summary>
    public class WarTerritoryMarker : MonoBehaviour
    {
        [SerializeField] private Image territoryIcon;
        [SerializeField] private Image ownerIndicator;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private GameObject contestedIndicator;
        [SerializeField] private Button markerButton;
        
        [SerializeField] private Color allianceAColor = new Color(0.2f, 0.5f, 0.9f);
        [SerializeField] private Color allianceBColor = new Color(0.9f, 0.3f, 0.3f);
        [SerializeField] private Color neutralColor = Color.gray;
        
        private WarTerritory _territory;
        
        public event Action<WarTerritory> OnSelected;
        
        private void Awake()
        {
            markerButton?.onClick.AddListener(() => OnSelected?.Invoke(_territory));
        }
        
        public void Setup(WarTerritory territory, string allianceAId, string allianceBId)
        {
            _territory = territory;
            
            if (nameText != null) nameText.text = territory.name;
            
            // Position on map
            var rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = territory.mapPosition;
            }
            
            // Owner color
            if (ownerIndicator != null)
            {
                if (territory.owner == allianceAId)
                    ownerIndicator.color = allianceAColor;
                else if (territory.owner == allianceBId)
                    ownerIndicator.color = allianceBColor;
                else
                    ownerIndicator.color = neutralColor;
            }
            
            // Contested indicator
            contestedIndicator?.SetActive(territory.contested);
            
            if (territory.contested)
            {
                StartCoroutine(AnimateContested());
            }
        }
        
        private IEnumerator AnimateContested()
        {
            while (_territory != null && _territory.contested)
            {
                float scale = 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.1f;
                transform.localScale = Vector3.one * scale;
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
    
    /// <summary>
    /// War declaration popup
    /// </summary>
    public class WarDeclarationPanel : MonoBehaviour
    {
        [Header("Target Selection")]
        [SerializeField] private Transform allianceListContainer;
        [SerializeField] private GameObject allianceItemPrefab;
        [SerializeField] private TMP_InputField searchInput;
        
        [Header("Selected Target")]
        [SerializeField] private Image targetLogo;
        [SerializeField] private TextMeshProUGUI targetNameText;
        [SerializeField] private TextMeshProUGUI targetStrengthText;
        [SerializeField] private TextMeshProUGUI targetMemberCountText;
        
        [Header("War Settings")]
        [SerializeField] private TMP_Dropdown warDurationDropdown;
        [SerializeField] private Toggle immediateStartToggle;
        [SerializeField] private TextMeshProUGUI warCostText;
        
        [Header("Buttons")]
        [SerializeField] private Button declareButton;
        [SerializeField] private Button cancelButton;
        
        private string _selectedAllianceId;
        
        public event Action<string, int> OnDeclare;
        public event Action OnCancel;
        
        private void Awake()
        {
            declareButton?.onClick.AddListener(OnDeclareClicked);
            cancelButton?.onClick.AddListener(() =>
            {
                OnCancel?.Invoke();
                gameObject.SetActive(false);
            });
            
            searchInput?.onValueChanged.AddListener(FilterAlliances);
        }
        
        private void OnEnable()
        {
            LoadAvailableTargets();
        }
        
        private void LoadAvailableTargets()
        {
            // Would load from server
            var demoAlliances = new[]
            {
                ("Shadow Council", 15000, 45),
                ("Dragon's Fury", 12000, 38),
                ("Night Watch", 18000, 52)
            };
            
            if (allianceListContainer == null || allianceItemPrefab == null) return;
            
            foreach (Transform child in allianceListContainer)
            {
                Destroy(child.gameObject);
            }
            
            foreach (var (name, strength, members) in demoAlliances)
            {
                var item = Instantiate(allianceItemPrefab, allianceListContainer);
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                
                if (texts.Length >= 2)
                {
                    texts[0].text = name;
                    texts[1].text = $"Power: {strength:N0} | {members} members";
                }
                
                var button = item.GetComponent<Button>();
                string allianceName = name;
                button?.onClick.AddListener(() => SelectTarget(allianceName, strength, members));
            }
        }
        
        private void FilterAlliances(string search)
        {
            // Would filter the list
        }
        
        private void SelectTarget(string name, int strength, int members)
        {
            _selectedAllianceId = name;
            
            if (targetNameText != null) targetNameText.text = name;
            if (targetStrengthText != null) targetStrengthText.text = $"Power: {strength:N0}";
            if (targetMemberCountText != null) targetMemberCountText.text = $"{members} members";
            
            declareButton.interactable = true;
        }
        
        private void OnDeclareClicked()
        {
            if (string.IsNullOrEmpty(_selectedAllianceId)) return;
            
            int duration = warDurationDropdown?.value switch
            {
                0 => 24,
                1 => 48,
                2 => 72,
                _ => 24
            };
            
            OnDeclare?.Invoke(_selectedAllianceId, duration);
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// War results screen
    /// </summary>
    public class WarResultsPanel : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI winnerNameText;
        [SerializeField] private Image winnerLogo;
        
        [Header("Scores")]
        [SerializeField] private TextMeshProUGUI finalScoreAText;
        [SerializeField] private TextMeshProUGUI finalScoreBText;
        
        [Header("Rewards")]
        [SerializeField] private Transform rewardsContainer;
        [SerializeField] private GameObject rewardItemPrefab;
        
        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI territoriesCapturedText;
        [SerializeField] private TextMeshProUGUI battlesWonText;
        [SerializeField] private TextMeshProUGUI topContributorText;
        
        [Header("Buttons")]
        [SerializeField] private Button claimRewardsButton;
        [SerializeField] private Button viewDetailsButton;
        [SerializeField] private Button closeButton;
        
        public event Action OnRewardsClaimed;
        
        private void Awake()
        {
            claimRewardsButton?.onClick.AddListener(ClaimRewards);
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        }
        
        public void ShowResults(AllianceWar war, bool victory)
        {
            gameObject.SetActive(true);
            
            if (resultTitleText != null)
            {
                resultTitleText.text = victory ? "🏆 VICTORY!" : "DEFEAT";
                resultTitleText.color = victory ? new Color(1f, 0.84f, 0f) : new Color(0.6f, 0.2f, 0.2f);
            }
            
            var winner = war.allianceA.score > war.allianceB.score ? war.allianceA : war.allianceB;
            
            if (winnerNameText != null) winnerNameText.text = winner.name;
            if (finalScoreAText != null) finalScoreAText.text = war.allianceA.score.ToString("N0");
            if (finalScoreBText != null) finalScoreBText.text = war.allianceB.score.ToString("N0");
            
            // Stats
            int territoriesCaptured = war.territories.FindAll(t => t.owner == war.allianceA.id).Count;
            if (territoriesCapturedText != null) 
                territoriesCapturedText.text = territoriesCaptured.ToString();
            
            // Top contributor
            var topContributor = war.allianceA.members[0];
            foreach (var m in war.allianceA.members)
            {
                if (m.contribution > topContributor.contribution)
                    topContributor = m;
            }
            
            if (topContributorText != null)
                topContributorText.text = $"{topContributor.name} ({topContributor.contribution:N0})";
            
            // Animate in
            StartCoroutine(AnimateIn());
        }
        
        private IEnumerator AnimateIn()
        {
            if (canvasGroup == null) yield break;
            
            canvasGroup.alpha = 0;
            
            float elapsed = 0;
            while (elapsed < 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = elapsed / 0.5f;
                yield return null;
            }
            
            canvasGroup.alpha = 1;
        }
        
        private void ClaimRewards()
        {
            OnRewardsClaimed?.Invoke();
            claimRewardsButton.interactable = false;
            
            var text = claimRewardsButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = "CLAIMED!";
        }
    }
}
