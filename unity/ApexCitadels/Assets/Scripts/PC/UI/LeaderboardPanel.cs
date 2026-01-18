using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIOutline = UnityEngine.UI.Outline;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Leaderboard panel showing player and alliance rankings.
    /// Multiple categories: Power, Territories, Victories, Resources.
    /// </summary>
    public class LeaderboardPanel : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int displayCount = 10;
        [SerializeField] private float refreshInterval = 30f;
        
        [Header("Colors")]
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color silverColor = new Color(0.75f, 0.75f, 0.75f);
        [SerializeField] private Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        [SerializeField] private Color selfHighlight = new Color(0.2f, 0.5f, 0.8f, 0.3f);
        
        // UI Elements
        private GameObject _panel;
        private Transform _entriesContainer;
        private TextMeshProUGUI _titleText;
        private LeaderboardCategory _currentCategory = LeaderboardCategory.Power;
        private List<GameObject> _tabButtons = new List<GameObject>();
        private List<LeaderboardEntry> _currentEntries = new List<LeaderboardEntry>();
        
        // Demo data
        private string _currentPlayerId = "Player1";
        
        public static LeaderboardPanel Instance { get; private set; }
        
        public event Action<LeaderboardEntry> OnPlayerSelected;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            CreateLeaderboardUI();
            GenerateDemoData();
            RefreshDisplay();
        }

        /// <summary>
        /// Show the leaderboard panel
        /// </summary>
        public void Show()
        {
            if (_panel != null)
            {
                _panel.SetActive(true);
                RefreshDisplay();
            }
        }

        /// <summary>
        /// Hide the leaderboard panel
        /// </summary>
        public void Hide()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        /// <summary>
        /// Toggle leaderboard visibility
        /// </summary>
        public void Toggle()
        {
            if (_panel != null && _panel.activeSelf)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// Switch to a different category
        /// </summary>
        public void SetCategory(LeaderboardCategory category)
        {
            _currentCategory = category;
            UpdateTabVisuals();
            GenerateDemoData(); // In real implementation, fetch from server
            RefreshDisplay();
        }

        private void CreateLeaderboardUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel (right side of screen)
            _panel = new GameObject("LeaderboardPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.73f, 0.2f);
            rect.anchorMax = new Vector2(0.98f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Background
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            
            // Outline
            UIUnityEngine.UI.Outline outline = _panel.AddComponent<UIOutline>();
            outline.effectColor = new Color(0.3f, 0.5f, 0.8f, 0.8f);
            outline.effectDistance = new Vector2(2, 2);
            
            // Header
            CreateHeader();
            
            // Category Tabs
            CreateCategoryTabs();
            
            // Entries container
            CreateEntriesContainer();
            
            // Close button
            CreateCloseButton();
            
            // Initially hidden
            _panel.SetActive(false);
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_panel.transform, false);
            
            RectTransform rect = header.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.92f);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(10, 0);
            rect.offsetMax = new Vector2(-40, -5);
            
            _titleText = header.AddComponent<TextMeshProUGUI>();
            _titleText.text = "🏆 LEADERBOARD";
            _titleText.fontSize = 24;
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.color = goldColor;
        }

        private void CreateCategoryTabs()
        {
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(_panel.transform, false);
            
            RectTransform rect = tabBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.84f);
            rect.anchorMax = new Vector2(1, 0.92f);
            rect.offsetMin = new Vector2(5, 0);
            rect.offsetMax = new Vector2(-5, 0);
            
            HorizontalLayoutGroup layout = tabBar.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 2, 2);
            
            // Create tabs
            string[] categories = { "⚡Power", "🏰Territories", "⚔️Victories", "💎Resources" };
            LeaderboardCategory[] categoryValues = { 
                LeaderboardCategory.Power, 
                LeaderboardCategory.Territories, 
                LeaderboardCategory.Victories, 
                LeaderboardCategory.Resources 
            };
            
            for (int i = 0; i < categories.Length; i++)
            {
                int index = i; // Capture for closure
                CreateTabButton(tabBar.transform, categories[i], () => SetCategory(categoryValues[index]));
            }
        }

        private void CreateTabButton(Transform parent, string label, Action onClick)
        {
            GameObject tab = new GameObject($"Tab_{label}");
            tab.transform.SetParent(parent, false);
            
            RectTransform rect = tab.AddComponent<RectTransform>();
            
            Image bg = tab.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
            
            Button btn = tab.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.4f, 0.6f);
            colors.pressedColor = new Color(0.2f, 0.3f, 0.5f);
            btn.colors = colors;
            
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
            text.color = Color.white;
            
            _tabButtons.Add(tab);
        }

        private void CreateEntriesContainer()
        {
            // Scroll View
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(_panel.transform, false);
            
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0.05f);
            scrollRect.anchorMax = new Vector2(1, 0.82f);
            scrollRect.offsetMin = new Vector2(5, 0);
            scrollRect.offsetMax = new Vector2(-5, 0);
            
            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            
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
            
            scroll.viewport = viewportRect;
            
            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 2;
            layout.padding = new RectOffset(5, 5, 5, 5);
            
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = contentRect;
            _entriesContainer = content.transform;
        }

        private void CreateCloseButton()
        {
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(_panel.transform, false);
            
            RectTransform rect = closeBtn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-5, -5);
            rect.sizeDelta = new Vector2(30, 30);
            
            Image bg = closeBtn.AddComponent<Image>();
            bg.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            
            Button btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(Hide);
            
            GameObject textObj = new GameObject("X");
            textObj.transform.SetParent(closeBtn.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "✕";
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        private void UpdateTabVisuals()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                Image bg = _tabButtons[i].GetComponent<Image>();
                if (bg != null)
                {
                    bool isSelected = (int)_currentCategory == i;
                    bg.color = isSelected ? 
                        new Color(0.3f, 0.5f, 0.7f, 0.9f) : 
                        new Color(0.2f, 0.2f, 0.3f, 0.8f);
                }
            }
        }

        private void RefreshDisplay()
        {
            if (_entriesContainer == null) return;
            
            // Clear existing entries
            foreach (Transform child in _entriesContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create new entries
            for (int i = 0; i < _currentEntries.Count && i < displayCount; i++)
            {
                CreateEntryRow(_currentEntries[i], i);
            }
            
            UpdateTabVisuals();
        }

        private void CreateEntryRow(LeaderboardEntry entry, int index)
        {
            int rank = index + 1;
            
            GameObject row = new GameObject($"Entry_{rank}");
            row.transform.SetParent(_entriesContainer, false);
            
            RectTransform rect = row.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 40);
            
            // Background
            Image bg = row.AddComponent<Image>();
            bool isSelf = entry.PlayerId == _currentPlayerId;
            bg.color = isSelf ? selfHighlight : new Color(0.15f, 0.15f, 0.2f, 0.7f);
            
            // Make clickable
            Button btn = row.AddComponent<Button>();
            btn.onClick.AddListener(() => OnPlayerSelected?.Invoke(entry));
            
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 5, 5);
            
            // Rank
            CreateText(row.transform, GetRankDisplay(rank), 20, 40, GetRankColor(rank), true);
            
            // Player name
            CreateText(row.transform, entry.PlayerName, 14, 0, Color.white, false, true);
            
            // Score
            CreateText(row.transform, FormatScore(entry.Score), 14, 80, goldColor, false, false, TextAlignmentOptions.Right);
        }

        private void CreateText(Transform parent, string text, int fontSize, float width, Color color, bool bold = false, bool flexible = false, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            
            LayoutElement le = obj.AddComponent<LayoutElement>();
            if (width > 0) le.preferredWidth = width;
            if (flexible) le.flexibleWidth = 1;
        }

        private string GetRankDisplay(int rank)
        {
            return rank switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => $"#{rank}"
            };
        }

        private Color GetRankColor(int rank)
        {
            return rank switch
            {
                1 => goldColor,
                2 => silverColor,
                3 => bronzeColor,
                _ => Color.white
            };
        }

        private string FormatScore(long score)
        {
            if (score >= 1000000) return $"{score / 1000000f:F1}M";
            if (score >= 1000) return $"{score / 1000f:F1}K";
            return score.ToString();
        }

        private void GenerateDemoData()
        {
            _currentEntries.Clear();
            
            // Demo player names
            string[] names = { 
                "DragonLord99", "ShadowKnight", "CrystalMage", "IronFist", "StarWarden",
                "ThunderBolt", "MysticRaven", "FrostGiant", "BlazeMaster", "NightHawk",
                "Player1" // Current player
            };
            
            System.Random rng = new System.Random((int)_currentCategory);
            List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
            
            foreach (var name in names)
            {
                entries.Add(new LeaderboardEntry
                {
                    PlayerId = name,
                    PlayerName = name,
                    Score = rng.Next(10000, 1000000),
                    AllianceName = rng.Next(2) == 0 ? "Steel Legion" : "Shadow Council"
                });
            }
            
            // Sort by score
            entries.Sort((a, b) => b.Score.CompareTo(a.Score));
            _currentEntries = entries;
        }
    }

    public enum LeaderboardCategory
    {
        Power,
        Territories,
        Victories,
        Resources
    }

    public class LeaderboardEntry
    {
        public string PlayerId;
        public string PlayerName;
        public long Score;
        public string AllianceName;
        public int Rank;
    }
}
