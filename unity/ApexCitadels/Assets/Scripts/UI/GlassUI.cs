using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ApexCitadels.UI
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
        [SerializeField] private bool createTestMenu = true;

        private GameObject canvasObj;
        
        private void Start()
        {
            if (createTestMenu)
            {
                CreateDemohud();
            }
        }
        
        private void CreateDemohud()
        {
            // 1. Create Canvas if not exists
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
            
            // 2. Create a "Travel Menu" Panel in the center
            GameObject panel = CreateGlassPanel("TravelPanel", new Vector2(500, 400));
            panel.transform.SetParent(canvasObj.transform, false);
            
            // 3. Add Title
            GameObject title = CreateUnlitText("TRAVEL NETWORK", 32, glowColor);
            title.transform.SetParent(panel.transform, false);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 160);
            
            // 4. Add some buttons
            string[] destinations = { "Home Citadel", "Market District", "Arena of Champions", "Wilderness" };
            for(int i=0; i<destinations.Length; i++)
            {
                int index = i; // capture for lambda
                GameObject btn = CreateGlassButton(destinations[i], () => Debug.Log($"Traveling to {destinations[index]}..."));
                btn.transform.SetParent(panel.transform, false);
                btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100 - (i * 70));
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
            GameObject textObj = CreateUnlitText(text, 20, Color.white);
            textObj.transform.SetParent(obj.transform, false);
            
            // Add Hover effect script
            var colors = btn.colors;
            colors.highlightedColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.6f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.8f);
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
            
            // Add a "Glow" effect to text
            Outline glow = obj.AddComponent<Outline>();
            glow.effectColor = new Color(color.r, color.g, color.b, 0.3f);
            glow.effectDistance = new Vector2(1, -1);
            
            return obj;
        }
    }
}
