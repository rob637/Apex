using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Procedurally generates "Magitech Glass" UI elements.
    /// This proves we can achieve a AAA Sci-Fantasy look without asset packs.
    /// </summary>
    public class GlassUI : MonoBehaviour
    {
        [Header("Magitech Theme Settings")]
        [SerializeField] private Color glassColor = new Color(0.1f, 0.2f, 0.3f, 0.85f);
        [SerializeField] private Color glowColor = new Color(0f, 0.8f, 1f, 1f);
        [SerializeField] private Color borderColor = new Color(0.4f, 0.9f, 1f, 0.6f);
        [SerializeField] private float cornerRadius = 15f;
        [SerializeField] private float borderThickness = 2f;
        
        [Header("Content")]
        [SerializeField] private bool showHUDOnStart = true;

        private GameObject canvasObj;
        private GameObject travelMenuPanel;
        private GameObject mainHUDPanel;
        
        private void Start()
        {
            InitializeCanvas();
            
            if (showHUDOnStart)
            {
                CreateMainHUD();
                CreateTravelMenu(); // Create but hide initially
            }
        }
        
        private void InitializeCanvas()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvasObj = new GameObject("GlassUICanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvasObj = canvas.gameObject;
            }
        }

        private void CreateMainHUD()
        {
            mainHUDPanel = new GameObject("MainHUD");
            mainHUDPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rect = mainHUDPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // 1. Top Left - Player Status
            CreatePlayerStatus(mainHUDPanel.transform);
            
            // 2. Top Right - Resources
            CreateResourceDisplay(mainHUDPanel.transform);
            
            // 3. Bottom Center - Action Bar
            CreateActionBar(mainHUDPanel.transform);
        }

        private void CreatePlayerStatus(Transform parent)
        {
            GameObject container = CreateGlassPanel("PlayerStatus", new Vector2(300, 80));
            container.transform.SetParent(parent, false);
            
            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -20);
            
            // Name
            GameObject nameTxt = CreateUnlitText("CITADEL LORD", 20, glowColor);
            nameTxt.transform.SetParent(container.transform, false);
            RectTransform nameRect = nameTxt.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(20, 0);
            nameRect.offsetMax = new Vector2(-20, 0);
            
            // Level
            GameObject lvlTxt = CreateUnlitText("LVL 12 | SANCTUM KEEPER", 14, Color.white);
            lvlTxt.transform.SetParent(container.transform, false);
            RectTransform lvlRect = lvlTxt.GetComponent<RectTransform>();
            lvlRect.anchorMin = new Vector2(0, 0);
            lvlRect.anchorMax = new Vector2(1, 0.5f);
            lvlRect.offsetMin = new Vector2(20, 0);
            lvlRect.offsetMax = new Vector2(-20, 0);
        }

        private void CreateResourceDisplay(Transform parent)
        {
            GameObject container = CreateGlassPanel("Resources", new Vector2(400, 60));
            container.transform.SetParent(parent, false);
            
            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-20, -20);
            
            // Simple Horizontal Layout for resources
            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.padding = new RectOffset(20, 20, 5, 5);
            layout.spacing = 20;
            
            // Gold
            CreateResourceItem(container.transform, "GOLD", "1,250", new Color(1f, 0.8f, 0.2f));
            // Aether
            CreateResourceItem(container.transform, "AETHER", "450", new Color(0.2f, 0.8f, 1f));
        }
        
        private void CreateResourceItem(Transform parent, string label, string value, Color color)
        {
            GameObject item = new GameObject($"Res_{label}");
            item.transform.SetParent(parent, false);
            VerticalLayoutGroup vLayout = item.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            
            CreateUnlitText(value, 22, color).transform.SetParent(item.transform, false);
            CreateUnlitText(label, 10, new Color(1,1,1,0.5f)).transform.SetParent(item.transform, false);
        }

        private void CreateActionBar(Transform parent)
        {
            GameObject container = CreateGlassPanel("ActionBar", new Vector2(600, 100));
            container.transform.SetParent(parent, false);
            
            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 20);
            
            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false; // Buttons have fixed size
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 15;
            
            // Buttons
            CreateActionButton(container.transform, "BUILD", () => Debug.Log("Build clicked"));
            CreateActionButton(container.transform, "TRAVEL", ToggleTravelMenu);
            CreateActionButton(container.transform, "ALLIANCE", () => Debug.Log("Alliance clicked"));
            CreateActionButton(container.transform, "MENU", () => Debug.Log("Menu clicked"));
        }
        
        private void CreateActionButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = CreateGlassButton(label, onClick);
            btnObj.transform.SetParent(parent, false);
            
            // Adjust size for action bar
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(130, 60);
        }

        private void CreateTravelMenu()
        {
            // 2. Create a "Travel Menu" Panel in the center
            travelMenuPanel = CreateGlassPanel("TravelPanel", new Vector2(500, 400));
            travelMenuPanel.transform.SetParent(canvasObj.transform, false);
            // Center it
            RectTransform rect = travelMenuPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f); 
            rect.anchoredPosition = Vector2.zero;
            
            // 3. Add Title
            GameObject title = CreateUnlitText("TRAVEL NETWORK", 32, glowColor);
            title.transform.SetParent(travelMenuPanel.transform, false);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 160);
            
            // 4. Add some buttons
            string[] destinations = { "Home Citadel", "Market District", "Arena of Champions", "Wilderness" };
            for(int i=0; i<destinations.Length; i++)
            {
                int index = i; // capture for lambda
                GameObject btn = CreateGlassButton(destinations[i], () => Debug.Log($"Traveling to {destinations[index]}..."));
                btn.transform.SetParent(travelMenuPanel.transform, false);
                btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100 - (i * 70));
            }
            
            // Close button
             GameObject closeBtn = CreateGlassButton("CLOSE", ToggleTravelMenu);
             closeBtn.transform.SetParent(travelMenuPanel.transform, false);
             closeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -170);
             closeBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 40);

            // Hide initially
            travelMenuPanel.SetActive(false);
        }

        public void ToggleTravelMenu()
        {
            if (travelMenuPanel != null)
            {
                bool isActive = !travelMenuPanel.activeSelf;
                travelMenuPanel.SetActive(isActive);
            }
        }
        
        private GameObject CreateGlassPanel(string name, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            
            Image img = obj.AddComponent<Image>();
            img.color = glassColor;
            
            // Add the "Magitech" border using a child object since we can't do shaders nicely here
            GameObject border = new GameObject("Border");
            border.transform.SetParent(obj.transform, false);
            RectTransform borderRect = border.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            
            // Using Outline component to simulate a glowy border
            Image borderImg = border.AddComponent<Image>();
            borderImg.color = Color.clear; // Transparent center
            
            Outline outline = border.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(borderThickness, borderThickness);
            
            // Add a subtle "Pulse" animation functionality here in a real shader
            // For now, we simulate the "Glass" look with simple transparency
            
            return obj;
        }
        
        private GameObject CreateGlassButton(string text, UnityEngine.Events.UnityAction onClick)
        {
            GameObject obj = new GameObject($"Btn_{text}");
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 50);
            
            Image img = obj.AddComponent<Image>();
            img.color = new Color(glassColor.r, glassColor.g, glassColor.b, 0.4f); // Lighter glass for buttons
            
            Button btn = obj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            
            // Add Button Text
            GameObject textObj = CreateUnlitText(text, 18, Color.white);
            textObj.transform.SetParent(obj.transform, false);
            
            // Add Hover effect script
            var colors = btn.colors;
            colors.highlightedColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.6f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.8f);
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            btn.colors = colors;
            
            // Add outline
            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(borderColor.r, borderColor.g, borderColor.b, 0.3f);
            
            return obj;
        }
        
        private GameObject CreateUnlitText(string content, int fontSize, Color color)
        {
            GameObject obj = new GameObject("Text");
            Text txt = obj.AddComponent<Text>();
            txt.text = content;
            txt.font = UnityEngine.Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
            
            // Add a "Glow" effect to text
            Outline glow = obj.AddComponent<Outline>();
            glow.effectColor = new Color(color.r, color.g, color.b, 0.3f);
            glow.effectDistance = new Vector2(1, -1);
            
            return obj;
        }
    }
}
