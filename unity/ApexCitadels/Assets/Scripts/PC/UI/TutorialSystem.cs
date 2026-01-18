using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Tutorial System - Guides new players through game mechanics.
    /// Supports step-by-step tutorials, tooltips, and highlighting.
    /// </summary>
    public class TutorialSystem : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color highlightColor = new Color(0.3f, 0.7f, 1f, 0.8f);
        [SerializeField] private Color tooltipBgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        
        // UI Elements
        private Canvas _canvas;
        private GameObject _overlayPanel;
        private GameObject _highlightBox;
        private GameObject _tooltipBox;
        private GameObject _arrowIndicator;
        private GameObject _progressBar;
        
        // Tutorial state
        private TutorialSequence _currentSequence;
        private int _currentStepIndex = 0;
        private bool _isRunning = false;
        private bool _waitingForAction = false;
        
        // Registered tutorials
        private Dictionary<string, TutorialSequence> _tutorials = new Dictionary<string, TutorialSequence>();
        
        public static TutorialSystem Instance { get; private set; }
        
        public event Action<string> OnTutorialStarted;
        public event Action<string> OnTutorialCompleted;
        public event Action<int, int> OnStepChanged;

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
            _canvas = FindFirstObjectByType<Canvas>();
            CreateTutorialUI();
            RegisterDefaultTutorials();
            HideAll();
            
            // Check if new player
            if (PlayerPrefs.GetInt("TutorialComplete_basics", 0) == 0)
            {
                // Auto-start basic tutorial after a delay
                StartCoroutine(AutoStartTutorial());
            }
        }

        private IEnumerator AutoStartTutorial()
        {
            yield return new WaitForSeconds(2f);
            StartTutorial("basics");
        }

        private void CreateTutorialUI()
        {
            if (_canvas == null) return;
            
            // Create overlay parent
            _overlayPanel = new GameObject("TutorialOverlay");
            _overlayPanel.transform.SetParent(_canvas.transform, false);
            
            RectTransform overlayRect = _overlayPanel.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            
            // Semi-transparent background
            Image overlayBg = _overlayPanel.AddComponent<Image>();
            overlayBg.color = new Color(0, 0, 0, 0.6f);
            overlayBg.raycastTarget = true;
            
            // Highlight box (cutout effect)
            CreateHighlightBox();
            
            // Tooltip box
            CreateTooltipBox();
            
            // Arrow indicator
            CreateArrowIndicator();
            
            // Progress bar
            CreateProgressBar();
        }

        private void CreateHighlightBox()
        {
            _highlightBox = new GameObject("HighlightBox");
            _highlightBox.transform.SetParent(_overlayPanel.transform, false);
            
            RectTransform rect = _highlightBox.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 100);
            
            // Glowing border effect
            Image img = _highlightBox.AddComponent<Image>();
            img.color = Color.clear;
            
            Outline outline = _highlightBox.AddComponent<Outline>();
            outline.effectColor = highlightColor;
            outline.effectDistance = new Vector2(3, 3);
            
            // Pulsing animation
            _highlightBox.AddComponent<HighlightPulse>();
        }

        private void CreateTooltipBox()
        {
            _tooltipBox = new GameObject("TooltipBox");
            _tooltipBox.transform.SetParent(_overlayPanel.transform, false);
            
            RectTransform rect = _tooltipBox.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 200);
            
            Image bg = _tooltipBox.AddComponent<Image>();
            bg.color = tooltipBgColor;
            
            // Border
            Outline border = _tooltipBox.AddComponent<Outline>();
            border.effectColor = highlightColor;
            border.effectDistance = new Vector2(2, 2);
            
            VerticalLayoutGroup vlayout = _tooltipBox.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(20, 20, 15, 15);
            
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_tooltipBox.transform, false);
            
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 30;
            
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Tutorial";
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = highlightColor;
            
            // Message
            GameObject msgObj = new GameObject("Message");
            msgObj.transform.SetParent(_tooltipBox.transform, false);
            
            LayoutElement msgLE = msgObj.AddComponent<LayoutElement>();
            msgLE.flexibleHeight = 1;
            
            TextMeshProUGUI msgText = msgObj.AddComponent<TextMeshProUGUI>();
            msgText.text = "Message goes here...";
            msgText.fontSize = 14;
            msgText.alignment = TextAlignmentOptions.Center;
            msgText.color = new Color(0.9f, 0.9f, 0.9f);
            msgText.enableWordWrapping = true;
            
            // Buttons
            GameObject buttons = new GameObject("Buttons");
            buttons.transform.SetParent(_tooltipBox.transform, false);
            
            LayoutElement btnLE = buttons.AddComponent<LayoutElement>();
            btnLE.preferredHeight = 45;
            
            HorizontalLayoutGroup btnHL = buttons.AddComponent<HorizontalLayoutGroup>();
            btnHL.childAlignment = TextAnchor.MiddleCenter;
            btnHL.spacing = 20;
            btnHL.childForceExpandWidth = false;
            
            CreateTutorialButton(buttons.transform, "Skip Tutorial", SkipTutorial, new Color(0.4f, 0.4f, 0.5f));
            CreateTutorialButton(buttons.transform, "Next", NextStep, highlightColor);
        }

        private void CreateTutorialButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btn = new GameObject($"Btn_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 120;
            le.preferredHeight = 38;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateArrowIndicator()
        {
            _arrowIndicator = new GameObject("ArrowIndicator");
            _arrowIndicator.transform.SetParent(_overlayPanel.transform, false);
            
            RectTransform rect = _arrowIndicator.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(50, 50);
            
            TextMeshProUGUI arrow = _arrowIndicator.AddComponent<TextMeshProUGUI>();
            arrow.text = "👆";
            arrow.fontSize = 40;
            arrow.alignment = TextAlignmentOptions.Center;
            
            // Bouncing animation
            _arrowIndicator.AddComponent<ArrowBounce>();
        }

        private void CreateProgressBar()
        {
            _progressBar = new GameObject("ProgressBar");
            _progressBar.transform.SetParent(_overlayPanel.transform, false);
            
            RectTransform rect = _progressBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.3f, 0.05f);
            rect.anchorMax = new Vector2(0.7f, 0.07f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _progressBar.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
            
            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(_progressBar.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = highlightColor;
            
            // Text
            GameObject txt = new GameObject("Text");
            txt.transform.SetParent(_progressBar.transform, false);
            
            RectTransform txtRect = txt.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            
            TextMeshProUGUI text = txt.AddComponent<TextMeshProUGUI>();
            text.text = "Step 1 of 5";
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void RegisterDefaultTutorials()
        {
            // Basic tutorial
            RegisterTutorial("basics", new TutorialSequence
            {
                Id = "basics",
                Name = "Getting Started",
                Steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        Title = "Welcome to Apex Citadels!",
                        Message = "This tutorial will teach you the basics of building your empire. Let's start with the world map!",
                        Position = TutorialPosition.Center
                    },
                    new TutorialStep
                    {
                        Title = "Camera Controls",
                        Message = "Use WASD or edge panning to move the camera.\nScroll wheel to zoom.\nQ/E to rotate.\nTry moving around!",
                        Position = TutorialPosition.Center,
                        WaitForAction = "camera_move"
                    },
                    new TutorialStep
                    {
                        Title = "Your Resources",
                        Message = "These are your resources: Gold, Stone, Wood, Iron, Crystal, and Apex Coins.\nYou'll need these to build and train troops.",
                        HighlightElement = "TopBarHUD",
                        Position = TutorialPosition.Below
                    },
                    new TutorialStep
                    {
                        Title = "Territories",
                        Message = "Click on any territory to view its details.\nTerritories produce resources and can be captured in battle!",
                        Position = TutorialPosition.Center,
                        WaitForAction = "territory_click"
                    },
                    new TutorialStep
                    {
                        Title = "Quick Actions",
                        Message = "Use keyboard shortcuts for quick access:\nL - Leaderboard\nB - Barracks\nQ - Quests\nESC - Settings",
                        Position = TutorialPosition.Center
                    },
                    new TutorialStep
                    {
                        Title = "Ready to Conquer!",
                        Message = "You're all set! Build your army, capture territories, and rise to become the Apex Commander!\n\nGood luck, Commander!",
                        Position = TutorialPosition.Center
                    }
                }
            });
            
            // Combat tutorial
            RegisterTutorial("combat", new TutorialSequence
            {
                Id = "combat",
                Name = "Combat Basics",
                Steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        Title = "Combat System",
                        Message = "Learn how to attack enemy territories and defend your own!",
                        Position = TutorialPosition.Center
                    },
                    new TutorialStep
                    {
                        Title = "Training Troops",
                        Message = "First, you need troops! Open the Barracks (B) to train different unit types.",
                        Position = TutorialPosition.Center,
                        WaitForAction = "open_barracks"
                    },
                    new TutorialStep
                    {
                        Title = "Unit Types",
                        Message = "Infantry - Basic troops, balanced stats\nArcher - Ranged attacks, weak defense\nCavalry - Fast and strong\nSiege - Destroys buildings\nElite - Powerful but expensive",
                        Position = TutorialPosition.Right
                    },
                    new TutorialStep
                    {
                        Title = "Attacking",
                        Message = "To attack, select an enemy territory and click Attack. Choose your troops wisely!",
                        Position = TutorialPosition.Center
                    },
                    new TutorialStep
                    {
                        Title = "Battle Results",
                        Message = "Battles are simulated instantly. Victory means capturing the territory and claiming its resources!",
                        Position = TutorialPosition.Center
                    }
                }
            });
            
            // Alliance tutorial
            RegisterTutorial("alliance", new TutorialSequence
            {
                Id = "alliance",
                Name = "Joining an Alliance",
                Steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        Title = "Alliances",
                        Message = "Joining an alliance gives you powerful allies and access to alliance features!",
                        Position = TutorialPosition.Center
                    },
                    new TutorialStep
                    {
                        Title = "Benefits",
                        Message = "Alliance members can:\n• Help each other in battle\n• Share resources\n• Participate in alliance wars\n• Earn exclusive rewards",
                        Position = TutorialPosition.Center
                    },
                    new TutorialStep
                    {
                        Title = "Find an Alliance",
                        Message = "Browse available alliances or create your own! Look for active players with similar play styles.",
                        Position = TutorialPosition.Center
                    }
                }
            });
        }

        #region Tutorial Control

        public void RegisterTutorial(string id, TutorialSequence sequence)
        {
            _tutorials[id] = sequence;
        }

        public void StartTutorial(string tutorialId)
        {
            if (!_tutorials.ContainsKey(tutorialId))
            {
                Debug.LogWarning($"[Tutorial] Tutorial not found: {tutorialId}");
                return;
            }
            
            _currentSequence = _tutorials[tutorialId];
            _currentStepIndex = 0;
            _isRunning = true;
            
            ShowOverlay();
            ShowCurrentStep();
            
            OnTutorialStarted?.Invoke(tutorialId);
            Debug.Log($"[Tutorial] Started: {tutorialId}");
        }

        public void NextStep()
        {
            if (!_isRunning) return;
            
            _currentStepIndex++;
            
            if (_currentStepIndex >= _currentSequence.Steps.Count)
            {
                CompleteTutorial();
            }
            else
            {
                ShowCurrentStep();
            }
        }

        public void PreviousStep()
        {
            if (!_isRunning || _currentStepIndex <= 0) return;
            
            _currentStepIndex--;
            ShowCurrentStep();
        }

        public void SkipTutorial()
        {
            CompleteTutorial();
        }

        private void CompleteTutorial()
        {
            string tutorialId = _currentSequence.Id;
            
            _isRunning = false;
            HideAll();
            
            // Mark as complete
            PlayerPrefs.SetInt($"TutorialComplete_{tutorialId}", 1);
            PlayerPrefs.Save();
            
            OnTutorialCompleted?.Invoke(tutorialId);
            Debug.Log($"[Tutorial] Completed: {tutorialId}");
            
            // Show notification
            if (UI.NotificationSystem.Instance != null)
            {
                UI.NotificationSystem.Instance.ShowSuccess($"Tutorial Complete: {_currentSequence.Name}");
            }
        }

        private void ShowCurrentStep()
        {
            if (_currentSequence == null || _currentStepIndex >= _currentSequence.Steps.Count) return;
            
            TutorialStep step = _currentSequence.Steps[_currentStepIndex];
            
            // Update tooltip content
            UpdateTooltipContent(step);
            
            // Position tooltip
            PositionTooltip(step.Position, step.HighlightElement);
            
            // Show/hide highlight
            if (!string.IsNullOrEmpty(step.HighlightElement))
            {
                HighlightElement(step.HighlightElement);
            }
            else
            {
                _highlightBox.SetActive(false);
                _arrowIndicator.SetActive(false);
            }
            
            // Update progress
            UpdateProgress();
            
            // Handle wait for action
            _waitingForAction = !string.IsNullOrEmpty(step.WaitForAction);
            
            OnStepChanged?.Invoke(_currentStepIndex + 1, _currentSequence.Steps.Count);
        }

        private void UpdateTooltipContent(TutorialStep step)
        {
            Transform titleTrans = _tooltipBox.transform.Find("Title");
            Transform msgTrans = _tooltipBox.transform.Find("Message");
            
            if (titleTrans != null)
            {
                TextMeshProUGUI title = titleTrans.GetComponent<TextMeshProUGUI>();
                title.text = step.Title;
            }
            
            if (msgTrans != null)
            {
                TextMeshProUGUI msg = msgTrans.GetComponent<TextMeshProUGUI>();
                msg.text = step.Message;
            }
            
            // Show/hide next button based on wait for action
            Transform buttons = _tooltipBox.transform.Find("Buttons");
            if (buttons != null)
            {
                Transform nextBtn = buttons.Find("Btn_Next");
                if (nextBtn != null)
                {
                    nextBtn.gameObject.SetActive(string.IsNullOrEmpty(step.WaitForAction));
                }
            }
        }

        private void PositionTooltip(TutorialPosition position, string highlightElement)
        {
            RectTransform tooltipRect = _tooltipBox.GetComponent<RectTransform>();
            
            switch (position)
            {
                case TutorialPosition.Center:
                    tooltipRect.anchorMin = new Vector2(0.3f, 0.35f);
                    tooltipRect.anchorMax = new Vector2(0.7f, 0.65f);
                    break;
                case TutorialPosition.TopLeft:
                    tooltipRect.anchorMin = new Vector2(0.05f, 0.6f);
                    tooltipRect.anchorMax = new Vector2(0.4f, 0.9f);
                    break;
                case TutorialPosition.TopRight:
                    tooltipRect.anchorMin = new Vector2(0.6f, 0.6f);
                    tooltipRect.anchorMax = new Vector2(0.95f, 0.9f);
                    break;
                case TutorialPosition.BottomLeft:
                    tooltipRect.anchorMin = new Vector2(0.05f, 0.1f);
                    tooltipRect.anchorMax = new Vector2(0.4f, 0.4f);
                    break;
                case TutorialPosition.BottomRight:
                    tooltipRect.anchorMin = new Vector2(0.6f, 0.1f);
                    tooltipRect.anchorMax = new Vector2(0.95f, 0.4f);
                    break;
                case TutorialPosition.Above:
                case TutorialPosition.Below:
                case TutorialPosition.Left:
                case TutorialPosition.Right:
                    // Would position relative to highlight element
                    tooltipRect.anchorMin = new Vector2(0.3f, 0.15f);
                    tooltipRect.anchorMax = new Vector2(0.7f, 0.45f);
                    break;
            }
            
            tooltipRect.offsetMin = Vector2.zero;
            tooltipRect.offsetMax = Vector2.zero;
        }

        private void HighlightElement(string elementName)
        {
            // Find element by name
            GameObject element = GameObject.Find(elementName);
            if (element == null)
            {
                _highlightBox.SetActive(false);
                _arrowIndicator.SetActive(false);
                return;
            }
            
            RectTransform targetRect = element.GetComponent<RectTransform>();
            if (targetRect == null)
            {
                _highlightBox.SetActive(false);
                _arrowIndicator.SetActive(false);
                return;
            }
            
            // Position highlight box over element
            _highlightBox.SetActive(true);
            RectTransform highlightRect = _highlightBox.GetComponent<RectTransform>();
            
            // Copy position and size
            highlightRect.position = targetRect.position;
            highlightRect.sizeDelta = targetRect.sizeDelta + new Vector2(20, 20);
            
            // Position arrow
            _arrowIndicator.SetActive(true);
            RectTransform arrowRect = _arrowIndicator.GetComponent<RectTransform>();
            arrowRect.position = targetRect.position + new Vector3(0, targetRect.sizeDelta.y / 2 + 40, 0);
        }

        private void UpdateProgress()
        {
            if (_progressBar == null || _currentSequence == null) return;
            
            float progress = (float)(_currentStepIndex + 1) / _currentSequence.Steps.Count;
            
            Transform fill = _progressBar.transform.Find("Fill");
            if (fill != null)
            {
                RectTransform fillRect = fill.GetComponent<RectTransform>();
                fillRect.anchorMax = new Vector2(progress, 1f);
            }
            
            Transform txt = _progressBar.transform.Find("Text");
            if (txt != null)
            {
                TextMeshProUGUI text = txt.GetComponent<TextMeshProUGUI>();
                text.text = $"Step {_currentStepIndex + 1} of {_currentSequence.Steps.Count}";
            }
        }

        #endregion

        #region Action Triggers

        /// <summary>
        /// Call this when a tutorial action is completed
        /// </summary>
        public void TriggerAction(string actionId)
        {
            if (!_isRunning || !_waitingForAction) return;
            
            TutorialStep currentStep = _currentSequence.Steps[_currentStepIndex];
            
            if (currentStep.WaitForAction == actionId)
            {
                _waitingForAction = false;
                NextStep();
            }
        }

        #endregion

        #region Visibility

        private void ShowOverlay()
        {
            _overlayPanel.SetActive(true);
        }

        private void HideAll()
        {
            _overlayPanel.SetActive(false);
        }

        public bool IsRunning => _isRunning;

        #endregion

        #region Utility

        public bool IsTutorialComplete(string tutorialId)
        {
            return PlayerPrefs.GetInt($"TutorialComplete_{tutorialId}", 0) == 1;
        }

        public void ResetTutorial(string tutorialId)
        {
            PlayerPrefs.DeleteKey($"TutorialComplete_{tutorialId}");
        }

        public void ResetAllTutorials()
        {
            foreach (var tutorial in _tutorials.Keys)
            {
                PlayerPrefs.DeleteKey($"TutorialComplete_{tutorial}");
            }
            PlayerPrefs.Save();
        }

        #endregion
    }

    #region Data Classes

    public class TutorialSequence
    {
        public string Id;
        public string Name;
        public List<TutorialStep> Steps = new List<TutorialStep>();
    }

    public class TutorialStep
    {
        public string Title;
        public string Message;
        public string HighlightElement;
        public TutorialPosition Position = TutorialPosition.Center;
        public string WaitForAction; // If set, waits for action before allowing next
    }

    public enum TutorialPosition
    {
        Center,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Above,
        Below,
        Left,
        Right
    }

    #endregion

    #region Helper Components

    public class HighlightPulse : MonoBehaviour
    {
        private Outline _outline;
        private float _time;
        
        void Start()
        {
            _outline = GetComponent<Outline>();
        }
        
        void Update()
        {
            if (_outline == null) return;
            
            _time += Time.deltaTime * 2f;
            float alpha = 0.5f + Mathf.Sin(_time) * 0.3f;
            Color c = _outline.effectColor;
            c.a = alpha;
            _outline.effectColor = c;
        }
    }

    public class ArrowBounce : MonoBehaviour
    {
        private Vector3 _startPos;
        private float _time;
        
        void Start()
        {
            _startPos = transform.localPosition;
        }
        
        void Update()
        {
            _time += Time.deltaTime * 3f;
            float offset = Mathf.Sin(_time) * 15f;
            transform.localPosition = _startPos + new Vector3(0, offset, 0);
        }
    }

    #endregion
}
