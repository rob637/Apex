using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Activity Feed showing recent game events like attacks, conquests, and achievements.
    /// Creates social proof and FOMO.
    /// </summary>
    public class ActivityFeedPanel : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int maxEntries = 20;
        [SerializeField] private float entryLifetime = 60f; // Seconds before fading
        [SerializeField] private float fadeOutDuration = 2f;
        
        [Header("UI")]
        [SerializeField] private Transform entriesContainer;
        [SerializeField] private ScrollRect scrollRect;
        
        [Header("Colors")]
        [SerializeField] private Color attackColor = new Color(1f, 0.5f, 0.3f);
        [SerializeField] private Color defenseColor = new Color(0.3f, 0.7f, 1f);
        [SerializeField] private Color conquestColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color allianceColor = new Color(0.5f, 0.8f, 0.5f);
        [SerializeField] private Color achievementColor = new Color(0.9f, 0.6f, 0.9f);
        
        // State
        private List<ActivityEntry> _entries = new List<ActivityEntry>();
        private Queue<ActivityEntry> _pendingEntries = new Queue<ActivityEntry>();
        private float _lastEntryTime;

        public static ActivityFeedPanel Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (entriesContainer == null)
            {
                CreateActivityFeedUI();
            }
            
            // Generate some fake activity for demo
            GenerateDemoActivity();
        }

        private void Update()
        {
            // Process pending entries with slight delay for dramatic effect
            if (_pendingEntries.Count > 0 && Time.time - _lastEntryTime > 0.5f)
            {
                var entry = _pendingEntries.Dequeue();
                CreateEntryUI(entry);
                _lastEntryTime = Time.time;
            }
            
            // Clean up old entries
            CleanupOldEntries();
        }

        /// <summary>
        /// Add an activity entry to the feed
        /// </summary>
        public void AddActivity(ActivityType type, string actor, string target, string details = "")
        {
            var entry = new ActivityEntry
            {
                Type = type,
                Actor = actor,
                Target = target,
                Details = details,
                Timestamp = DateTime.Now
            };
            
            _pendingEntries.Enqueue(entry);
        }

        /// <summary>
        /// Add attack activity
        /// </summary>
        public void AddAttack(string attacker, string territory, bool success)
        {
            if (success)
            {
                AddActivity(ActivityType.Conquest, attacker, territory, "conquered");
            }
            else
            {
                AddActivity(ActivityType.Attack, attacker, territory, "attacked");
            }
        }

        /// <summary>
        /// Add defense activity
        /// </summary>
        public void AddDefense(string defender, string territory, bool success)
        {
            AddActivity(ActivityType.Defense, defender, territory, success ? "defended" : "lost");
        }

        /// <summary>
        /// Add alliance activity
        /// </summary>
        public void AddAllianceEvent(string alliance, string action, string target = "")
        {
            AddActivity(ActivityType.Alliance, alliance, target, action);
        }

        /// <summary>
        /// Add achievement activity
        /// </summary>
        public void AddAchievement(string player, string achievement)
        {
            AddActivity(ActivityType.Achievement, player, achievement, "earned");
        }

        private void CreateEntryUI(ActivityEntry entry)
        {
            if (entriesContainer == null) return;
            
            // Create entry object
            GameObject entryObj = new GameObject($"Entry_{_entries.Count}");
            entryObj.transform.SetParent(entriesContainer, false);
            entryObj.transform.SetAsFirstSibling(); // New entries at top
            
            RectTransform rect = entryObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 30);
            
            // Layout
            HorizontalLayoutGroup layout = entryObj.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 2, 2);
            
            // Background
            Image bg = entryObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.4f);
            
            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(entryObj.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(24, 24);
            
            TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
            iconText.text = GetActivityIcon(entry.Type);
            iconText.fontSize = 16;
            iconText.alignment = TextAlignmentOptions.Center;
            iconText.color = GetActivityColor(entry.Type);
            
            // Message
            GameObject msgObj = new GameObject("Message");
            msgObj.transform.SetParent(entryObj.transform, false);
            
            TextMeshProUGUI msgText = msgObj.AddComponent<TextMeshProUGUI>();
            msgText.text = FormatActivityMessage(entry);
            msgText.fontSize = 14;
            msgText.alignment = TextAlignmentOptions.Left;
            msgText.color = Color.white;
            
            LayoutElement msgLayout = msgObj.AddComponent<LayoutElement>();
            msgLayout.flexibleWidth = 1;
            
            // Time
            GameObject timeObj = new GameObject("Time");
            timeObj.transform.SetParent(entryObj.transform, false);
            
            TextMeshProUGUI timeText = timeObj.AddComponent<TextMeshProUGUI>();
            timeText.text = "now";
            timeText.fontSize = 12;
            timeText.alignment = TextAlignmentOptions.Right;
            timeText.color = new Color(0.6f, 0.6f, 0.6f);
            
            LayoutElement timeLayout = timeObj.AddComponent<LayoutElement>();
            timeLayout.preferredWidth = 50;
            
            // Store reference
            entry.UIObject = entryObj;
            entry.TimeText = timeText;
            _entries.Add(entry);
            
            // Trim if too many
            while (_entries.Count > maxEntries)
            {
                var oldest = _entries[0];
                _entries.RemoveAt(0);
                if (oldest.UIObject != null)
                {
                    Destroy(oldest.UIObject);
                }
            }
            
            // Scroll to top
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private string GetActivityIcon(ActivityType type)
        {
            return type switch
            {
                ActivityType.Attack => "[!]",
                ActivityType.Defense => "[D]",
                ActivityType.Conquest => "[T]",
                ActivityType.Alliance => "🤝",
                ActivityType.Achievement => "[*]",
                ActivityType.LevelUp => "[+]",
                ActivityType.Building => "🏗️",
                _ => "📢"
            };
        }

        private Color GetActivityColor(ActivityType type)
        {
            return type switch
            {
                ActivityType.Attack => attackColor,
                ActivityType.Defense => defenseColor,
                ActivityType.Conquest => conquestColor,
                ActivityType.Alliance => allianceColor,
                ActivityType.Achievement => achievementColor,
                _ => Color.white
            };
        }

        private string FormatActivityMessage(ActivityEntry entry)
        {
            return entry.Type switch
            {
                ActivityType.Attack => $"<b>{entry.Actor}</b> attacked <b>{entry.Target}</b>",
                ActivityType.Defense => $"<b>{entry.Actor}</b> defended <b>{entry.Target}</b>",
                ActivityType.Conquest => $"<b>{entry.Actor}</b> conquered <b>{entry.Target}</b>!",
                ActivityType.Alliance => $"<b>{entry.Actor}</b> {entry.Details}",
                ActivityType.Achievement => $"<b>{entry.Actor}</b> earned <b>{entry.Target}</b>",
                ActivityType.LevelUp => $"<b>{entry.Actor}</b> reached level <b>{entry.Target}</b>!",
                ActivityType.Building => $"<b>{entry.Actor}</b> built <b>{entry.Target}</b>",
                _ => $"{entry.Actor} {entry.Details} {entry.Target}"
            };
        }

        private void CleanupOldEntries()
        {
            float now = Time.time;
            
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                float age = (float)(DateTime.Now - entry.Timestamp).TotalSeconds;
                
                // Update time text
                if (entry.TimeText != null)
                {
                    entry.TimeText.text = FormatTimeAgo(entry.Timestamp);
                }
                
                // Fade out old entries
                if (age > entryLifetime && entry.UIObject != null)
                {
                    var cg = entry.UIObject.GetComponent<CanvasGroup>();
                    if (cg == null)
                    {
                        cg = entry.UIObject.AddComponent<CanvasGroup>();
                    }
                    
                    float fadeProgress = (age - entryLifetime) / fadeOutDuration;
                    cg.alpha = 1f - Mathf.Clamp01(fadeProgress);
                    
                    if (fadeProgress >= 1f)
                    {
                        Destroy(entry.UIObject);
                        _entries.RemoveAt(i);
                    }
                }
            }
        }

        private string FormatTimeAgo(DateTime time)
        {
            var span = DateTime.Now - time;
            
            if (span.TotalSeconds < 60) return "now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h";
            return $"{(int)span.TotalDays}d";
        }

        private void CreateActivityFeedUI()
        {
            // Find canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Create panel on the left side
            GameObject panel = new GameObject("ActivityFeed");
            panel.transform.SetParent(canvas.transform, false);
            
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0.3f);
            panelRect.anchorMax = new Vector2(0.25f, 0.8f);
            panelRect.offsetMin = new Vector2(10, 0);
            panelRect.offsetMax = new Vector2(0, 0);
            
            // Background (semi-transparent)
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);
            
            // Title
            GameObject title = new GameObject("Title");
            title.transform.SetParent(panel.transform, false);
            RectTransform titleRect = title.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.92f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "📢 Activity Feed";
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            
            // Scroll view
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(panel.transform, false);
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 0.9f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            
            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            this.scrollRect = scroll;
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            Image viewportMask = viewport.AddComponent<Image>();
            viewportMask.color = Color.white;
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
            
            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 2;
            
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = contentRect;
            entriesContainer = content.transform;
        }

        private void GenerateDemoActivity()
        {
            // Generate some fake activity to show the system working
            string[] players = { "DragonLord", "ShadowKnight", "CrystalMage", "IronFist", "StarWarden" };
            string[] territories = { "Crystal Peak", "Iron Fortress", "Shadow Valley", "Dragon's Lair", "Star Harbor" };
            string[] achievements = { "First Blood", "Empire Builder", "Defender", "Conqueror", "Strategist" };
            
            // Add a few demo entries
            AddActivity(ActivityType.Conquest, players[0], territories[2]);
            AddActivity(ActivityType.Defense, players[1], territories[1]);
            AddActivity(ActivityType.Attack, players[2], territories[0]);
            AddActivity(ActivityType.Achievement, players[3], achievements[1]);
            AddActivity(ActivityType.Alliance, "Steel Legion", "declared war on", "Shadow Council");
        }
    }

    public enum ActivityType
    {
        Attack,
        Defense,
        Conquest,
        Alliance,
        Achievement,
        LevelUp,
        Building,
        Market,
        Chat
    }

    public class ActivityEntry
    {
        public ActivityType Type;
        public string Actor;
        public string Target;
        public string Details;
        public DateTime Timestamp;
        public GameObject UIObject;
        public TextMeshProUGUI TimeText;
    }
}
