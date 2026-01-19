using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Shows an always-visible controls help panel that can be toggled.
    /// Displays keyboard shortcuts and basic game controls.
    /// </summary>
    public class ControlsHelpPanel : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool showOnStart = true;
        [SerializeField] private float autoHideDelay = 30f;
        
        private GameObject _helpPanel;
        private GameObject _minimizedButton;
        private bool _isExpanded = true;
        private float _showTime;
        
        public static ControlsHelpPanel Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            CreateHelpPanel();
            
            if (showOnStart)
            {
                ShowPanel();
                _showTime = Time.time;
            }
            else
            {
                HidePanel();
            }
        }

        private void Update()
        {
            // Toggle with F1
            if (Input.GetKeyDown(KeyCode.F1))
            {
                TogglePanel();
            }
            
            // Auto-hide after delay (but keep minimized button)
            if (_isExpanded && autoHideDelay > 0 && Time.time - _showTime > autoHideDelay)
            {
                HidePanel();
            }
        }

        private void CreateHelpPanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel (top-right corner)
            _helpPanel = new GameObject("ControlsHelpPanel");
            _helpPanel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _helpPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-10, -60); // Below top bar
            rect.sizeDelta = new Vector2(220, 320);
            
            // Background
            Image bg = _helpPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
            
            // Vertical layout
            VerticalLayoutGroup layout = _helpPanel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4;
            layout.padding = new RectOffset(12, 12, 12, 12);
            
            // Title
            CreateTitle();
            
            // Divider
            CreateDivider();
            
            // Control sections
            CreateSection("CAMERA CONTROLS");
            CreateControlRow("WASD / Arrows", "Move camera");
            CreateControlRow("Mouse Scroll", "Zoom in/out");
            CreateControlRow("Q / E", "Rotate view");
            CreateControlRow("C", "Cycle camera modes");
            CreateControlRow("Space", "Toggle view");
            
            CreateSection("PANELS");
            CreateControlRow("B", "Build Menu");
            CreateControlRow("Tab", "Alliance Panel");
            CreateControlRow("I", "Inventory");
            CreateControlRow("M", "World Map");
            CreateControlRow("L", "Leaderboard");
            CreateControlRow("P", "Profile");
            CreateControlRow("Esc", "Settings Menu");
            
            CreateSection("QUICK ACTIONS");
            CreateControlRow("1-6", "Action bar buttons");
            CreateControlRow("F1", "Toggle this help");
            
            // Footer
            CreateFooter();
            
            // Create minimized button
            CreateMinimizedButton(canvas.transform);
        }

        private void CreateTitle()
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_helpPanel.transform, false);
            
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "CONTROLS [F1]";
            title.fontSize = 14;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.4f, 0.7f, 1f);
            
            LayoutElement le = titleObj.AddComponent<LayoutElement>();
            le.preferredHeight = 22;
        }

        private void CreateDivider()
        {
            GameObject div = new GameObject("Divider");
            div.transform.SetParent(_helpPanel.transform, false);
            
            Image divImg = div.AddComponent<Image>();
            divImg.color = new Color(0.3f, 0.3f, 0.4f);
            
            LayoutElement le = div.AddComponent<LayoutElement>();
            le.preferredHeight = 1;
        }

        private void CreateSection(string sectionName)
        {
            GameObject sectionObj = new GameObject($"Section_{sectionName}");
            sectionObj.transform.SetParent(_helpPanel.transform, false);
            
            TextMeshProUGUI text = sectionObj.AddComponent<TextMeshProUGUI>();
            text.text = sectionName;
            text.fontSize = 10;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Left;
            text.color = new Color(0.6f, 0.6f, 0.7f);
            
            LayoutElement le = sectionObj.AddComponent<LayoutElement>();
            le.preferredHeight = 18;
        }

        private void CreateControlRow(string key, string action)
        {
            GameObject row = new GameObject($"Control_{key}");
            row.transform.SetParent(_helpPanel.transform, false);
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.childForceExpandWidth = false;
            hlayout.spacing = 8;
            
            LayoutElement rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 16;
            
            // Key box
            GameObject keyObj = new GameObject("Key");
            keyObj.transform.SetParent(row.transform, false);
            
            Image keyBg = keyObj.AddComponent<Image>();
            keyBg.color = new Color(0.25f, 0.25f, 0.35f);
            
            LayoutElement keyLE = keyObj.AddComponent<LayoutElement>();
            keyLE.preferredWidth = 70;
            keyLE.preferredHeight = 16;
            
            // Key text
            GameObject keyTextObj = new GameObject("KeyText");
            keyTextObj.transform.SetParent(keyObj.transform, false);
            
            RectTransform keyTextRect = keyTextObj.AddComponent<RectTransform>();
            keyTextRect.anchorMin = Vector2.zero;
            keyTextRect.anchorMax = Vector2.one;
            keyTextRect.offsetMin = Vector2.zero;
            keyTextRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI keyText = keyTextObj.AddComponent<TextMeshProUGUI>();
            keyText.text = key;
            keyText.fontSize = 10;
            keyText.fontStyle = FontStyles.Bold;
            keyText.alignment = TextAlignmentOptions.Center;
            keyText.color = Color.white;
            
            // Action text
            GameObject actionObj = new GameObject("Action");
            actionObj.transform.SetParent(row.transform, false);
            
            TextMeshProUGUI actionText = actionObj.AddComponent<TextMeshProUGUI>();
            actionText.text = action;
            actionText.fontSize = 10;
            actionText.alignment = TextAlignmentOptions.Left;
            actionText.color = new Color(0.8f, 0.8f, 0.8f);
            
            LayoutElement actionLE = actionObj.AddComponent<LayoutElement>();
            actionLE.flexibleWidth = 1;
        }

        private void CreateFooter()
        {
            GameObject footer = new GameObject("Footer");
            footer.transform.SetParent(_helpPanel.transform, false);
            
            TextMeshProUGUI text = footer.AddComponent<TextMeshProUGUI>();
            text.text = "Press F1 to hide";
            text.fontSize = 9;
            text.fontStyle = FontStyles.Italic;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.5f, 0.5f, 0.5f);
            
            LayoutElement le = footer.AddComponent<LayoutElement>();
            le.preferredHeight = 18;
        }

        private void CreateMinimizedButton(Transform parent)
        {
            _minimizedButton = new GameObject("ControlsHelpMinimized");
            _minimizedButton.transform.SetParent(parent, false);
            
            RectTransform rect = _minimizedButton.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-10, -60);
            rect.sizeDelta = new Vector2(90, 30);
            
            Image bg = _minimizedButton.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            
            Button btn = _minimizedButton.AddComponent<Button>();
            btn.onClick.AddListener(ShowPanel);
            
            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(_minimizedButton.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "? Help [F1]";
            text.fontSize = 12;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.6f, 0.8f, 1f);
            
            _minimizedButton.SetActive(false);
        }

        public void ShowPanel()
        {
            if (_helpPanel != null) _helpPanel.SetActive(true);
            if (_minimizedButton != null) _minimizedButton.SetActive(false);
            _isExpanded = true;
            _showTime = Time.time;
            ApexLogger.Log("Controls help panel shown", ApexLogger.LogCategory.UI);
        }

        public void HidePanel()
        {
            if (_helpPanel != null) _helpPanel.SetActive(false);
            if (_minimizedButton != null) _minimizedButton.SetActive(true);
            _isExpanded = false;
            ApexLogger.Log("Controls help panel hidden", ApexLogger.LogCategory.UI);
        }

        public void TogglePanel()
        {
            if (_isExpanded)
                HidePanel();
            else
                ShowPanel();
        }
    }
}
