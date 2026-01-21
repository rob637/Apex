// ============================================================================
// APEX CITADELS - FANTASY KINGDOM UI SETUP
// Auto-creates the required UI for Fantasy Kingdom scene
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Creates the Fantasy Kingdom UI at runtime if not present
    /// Attach to the FantasyKingdomController GameObject
    /// </summary>
    public class FantasyKingdomUISetup : MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField] private FantasyKingdomController kingdomController;
        
        [Header("=== COLORS ===")]
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.08f, 0.15f, 0.95f);
        [SerializeField] private Color accentColor = new Color(0.85f, 0.65f, 0.2f);
        [SerializeField] private Color textColor = Color.white;
        
        private Canvas loadingCanvas;
        private Canvas gameplayCanvas;
        
        private void Awake()
        {
            if (kingdomController == null)
                kingdomController = GetComponent<FantasyKingdomController>();
            
            SetupUI();
        }
        
        private void SetupUI()
        {
            // Create Loading UI
            CreateLoadingUI();
            
            // Create Gameplay UI
            CreateGameplayUI();
            
            // Assign to controller via reflection (or public setter)
            AssignUIToController();
        }
        
        private void CreateLoadingUI()
        {
            // Create Canvas
            var canvasObj = new GameObject("LoadingCanvas");
            canvasObj.transform.SetParent(transform);
            loadingCanvas = canvasObj.AddComponent<Canvas>();
            loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            loadingCanvas.sortingOrder = 100;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.AddComponent<CanvasGroup>();
            
            // Background
            var bg = CreateUIElement("Background", canvasObj.transform);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = backgroundColor;
            SetFullStretch(bg.GetComponent<RectTransform>());
            
            // Title
            var title = CreateUIElement("Title", canvasObj.transform);
            var titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "⚔ FANTASY KINGDOM ⚔";
            titleText.fontSize = 72;
            titleText.color = accentColor;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.7f);
            titleRect.anchorMax = new Vector2(0.5f, 0.7f);
            titleRect.sizeDelta = new Vector2(800, 100);
            
            // Status Text
            var status = CreateUIElement("StatusText", canvasObj.transform);
            var statusText = status.AddComponent<TextMeshProUGUI>();
            statusText.text = "Preparing your kingdom...";
            statusText.fontSize = 28;
            statusText.color = textColor;
            statusText.alignment = TextAlignmentOptions.Center;
            var statusRect = status.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 0.45f);
            statusRect.anchorMax = new Vector2(0.5f, 0.45f);
            statusRect.sizeDelta = new Vector2(600, 50);
            
            // Progress Bar Background
            var progressBg = CreateUIElement("ProgressBackground", canvasObj.transform);
            var progressBgImage = progressBg.AddComponent<Image>();
            progressBgImage.color = new Color(0.2f, 0.2f, 0.2f);
            var progressBgRect = progressBg.GetComponent<RectTransform>();
            progressBgRect.anchorMin = new Vector2(0.5f, 0.35f);
            progressBgRect.anchorMax = new Vector2(0.5f, 0.35f);
            progressBgRect.sizeDelta = new Vector2(500, 20);
            
            // Progress Bar
            var slider = progressBg.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;
            slider.interactable = false;
            
            // Fill Area
            var fillArea = CreateUIElement("FillArea", progressBg.transform);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            // Fill
            var fill = CreateUIElement("Fill", fillArea.transform);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = accentColor;
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            slider.fillRect = fillRect;
            
            // Progress Percentage
            var percent = CreateUIElement("ProgressText", canvasObj.transform);
            var percentText = percent.AddComponent<TextMeshProUGUI>();
            percentText.text = "0%";
            percentText.fontSize = 24;
            percentText.color = textColor;
            percentText.alignment = TextAlignmentOptions.Center;
            var percentRect = percent.GetComponent<RectTransform>();
            percentRect.anchorMin = new Vector2(0.5f, 0.28f);
            percentRect.anchorMax = new Vector2(0.5f, 0.28f);
            percentRect.sizeDelta = new Vector2(100, 40);
            
            // Store references
            // We'll use SendMessage or find by name since we can't access private fields directly
        }
        
        private void CreateGameplayUI()
        {
            // Create Canvas
            var canvasObj = new GameObject("GameplayCanvas");
            canvasObj.transform.SetParent(transform);
            gameplayCanvas = canvasObj.AddComponent<Canvas>();
            gameplayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameplayCanvas.sortingOrder = 50;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Controls Help Panel
            var controlsPanel = CreateUIElement("ControlsHelp", canvasObj.transform);
            var panelImage = controlsPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);
            var panelRect = controlsPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.anchoredPosition = new Vector2(20, 20);
            panelRect.sizeDelta = new Vector2(300, 200);
            
            // Controls Text
            var controls = CreateUIElement("ControlsText", controlsPanel.transform);
            var controlsText = controls.AddComponent<TextMeshProUGUI>();
            controlsText.text = @"<b>CONTROLS</b>

WASD - Move
Mouse - Look
Shift - Run
Space - Jump
V - Toggle 1st/3rd Person
Esc - Toggle Cursor";
            controlsText.fontSize = 18;
            controlsText.color = textColor;
            controlsText.alignment = TextAlignmentOptions.TopLeft;
            var controlsRect = controls.GetComponent<RectTransform>();
            controlsRect.anchorMin = Vector2.zero;
            controlsRect.anchorMax = Vector2.one;
            controlsRect.offsetMin = new Vector2(15, 15);
            controlsRect.offsetMax = new Vector2(-15, -15);
            
            // Title overlay (top)
            var titleOverlay = CreateUIElement("TitleOverlay", canvasObj.transform);
            var titleImage = titleOverlay.AddComponent<Image>();
            titleImage.color = new Color(0, 0, 0, 0.5f);
            var titleORect = titleOverlay.GetComponent<RectTransform>();
            titleORect.anchorMin = new Vector2(0.5f, 1);
            titleORect.anchorMax = new Vector2(0.5f, 1);
            titleORect.pivot = new Vector2(0.5f, 1);
            titleORect.anchoredPosition = new Vector2(0, -10);
            titleORect.sizeDelta = new Vector2(400, 50);
            
            var titleText = CreateUIElement("TitleText", titleOverlay.transform);
            var titleTMP = titleText.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "FANTASY KINGDOM";
            titleTMP.fontSize = 28;
            titleTMP.color = accentColor;
            titleTMP.alignment = TextAlignmentOptions.Center;
            var ttRect = titleText.GetComponent<RectTransform>();
            SetFullStretch(ttRect);
            
            canvasObj.SetActive(false);
        }
        
        private GameObject CreateUIElement(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.AddComponent<RectTransform>();
            return obj;
        }
        
        private void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        
        private void AssignUIToController()
        {
            if (kingdomController == null) return;
            
            // Use reflection to set private serialized fields
            var type = kingdomController.GetType();
            
            // Loading Canvas
            var loadingField = type.GetField("loadingCanvas", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            loadingField?.SetValue(kingdomController, loadingCanvas);
            
            // Progress Slider
            var sliderField = type.GetField("progressSlider",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var slider = loadingCanvas.GetComponentInChildren<Slider>();
            sliderField?.SetValue(kingdomController, slider);
            
            // Progress Text
            var progressTextField = type.GetField("progressText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var progressText = loadingCanvas.transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
            progressTextField?.SetValue(kingdomController, progressText);
            
            // Status Text
            var statusField = type.GetField("statusText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var statusText = loadingCanvas.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
            statusField?.SetValue(kingdomController, statusText);
            
            // Gameplay Canvas
            var gameplayField = type.GetField("gameplayCanvas",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            gameplayField?.SetValue(kingdomController, gameplayCanvas);
            
            // Controls Help
            var controlsField = type.GetField("controlsHelp",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var controlsHelp = gameplayCanvas.transform.Find("ControlsHelp")?.gameObject;
            controlsField?.SetValue(kingdomController, controlsHelp);
        }
    }
}
