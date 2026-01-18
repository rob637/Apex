// ============================================================================
// APEX CITADELS - UI HELPER
// Common UI creation utilities to avoid null reference errors
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Helper class for creating UI elements properly.
    /// Ensures Image and TextMeshProUGUI are not on the same GameObject.
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// Creates a button with proper structure (Image on parent, Text as child)
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string text, 
            Color bgColor, Action onClick = null, float fontSize = 14f)
        {
            // Button container with Image
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = bgColor;
            
            Button btn = btnObj.AddComponent<Button>();
            if (onClick != null)
            {
                btn.onClick.AddListener(() => onClick());
            }
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            return btn;
        }

        /// <summary>
        /// Creates a close button (X) in standard style
        /// </summary>
        public static Button CreateCloseButton(Transform parent, Action onClose)
        {
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(parent, false);
            
            RectTransform rect = closeBtn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-5, -5);
            rect.sizeDelta = new Vector2(30, 30);
            
            Image bg = closeBtn.AddComponent<Image>();
            bg.color = new Color(0.6f, 0.2f, 0.2f, 0.8f);
            
            Button btn = closeBtn.AddComponent<Button>();
            if (onClose != null)
            {
                btn.onClick.AddListener(() => onClose());
            }
            
            // Text child
            GameObject textObj = new GameObject("X");
            textObj.transform.SetParent(closeBtn.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "✕";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            return btn;
        }

        /// <summary>
        /// Creates a toggle with proper structure
        /// </summary>
        public static Toggle CreateToggle(Transform parent, string name, string label, 
            bool initialValue = false, Action<bool> onChanged = null)
        {
            GameObject toggleObj = new GameObject(name);
            toggleObj.transform.SetParent(parent, false);
            
            RectTransform rect = toggleObj.AddComponent<RectTransform>();
            
            HorizontalLayoutGroup hlg = toggleObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            
            // Checkbox background
            GameObject checkBg = new GameObject("Background");
            checkBg.transform.SetParent(toggleObj.transform, false);
            
            RectTransform checkRect = checkBg.AddComponent<RectTransform>();
            LayoutElement checkLE = checkBg.AddComponent<LayoutElement>();
            checkLE.preferredWidth = 20;
            checkLE.preferredHeight = 20;
            
            Image checkImg = checkBg.AddComponent<Image>();
            checkImg.color = new Color(0.2f, 0.2f, 0.25f);
            
            // Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(checkBg.transform, false);
            
            RectTransform cmRect = checkmark.AddComponent<RectTransform>();
            cmRect.anchorMin = new Vector2(0.1f, 0.1f);
            cmRect.anchorMax = new Vector2(0.9f, 0.9f);
            cmRect.offsetMin = Vector2.zero;
            cmRect.offsetMax = Vector2.zero;
            
            Image cmImg = checkmark.AddComponent<Image>();
            cmImg.color = new Color(0.3f, 0.8f, 0.3f);
            
            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);
            
            TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 12;
            tmp.color = Color.white;
            
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 150;
            
            // Toggle component
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = initialValue;
            toggle.graphic = cmImg;
            toggle.targetGraphic = checkImg;
            
            if (onChanged != null)
            {
                toggle.onValueChanged.AddListener((value) => onChanged(value));
            }
            
            return toggle;
        }

        /// <summary>
        /// Creates a simple text label
        /// </summary>
        public static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
            float fontSize = 14f, TextAlignmentOptions alignment = TextAlignmentOptions.Left,
            Color? color = null)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rect = obj.AddComponent<RectTransform>();
            
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color ?? Color.white;
            
            return tmp;
        }

        /// <summary>
        /// Creates a panel with background
        /// </summary>
        public static (GameObject, Image) CreatePanel(Transform parent, string name, Color bgColor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rect = obj.AddComponent<RectTransform>();
            
            Image bg = obj.AddComponent<Image>();
            bg.color = bgColor;
            
            return (obj, bg);
        }

        /// <summary>
        /// Creates a tab button for tab-based panels
        /// </summary>
        public static Button CreateTabButton(Transform parent, string name, string label,
            bool isActive, Action onClick)
        {
            Color activeColor = new Color(0.2f, 0.5f, 0.3f);
            Color inactiveColor = new Color(0.15f, 0.15f, 0.18f);
            
            return CreateButton(parent, name, label, 
                isActive ? activeColor : inactiveColor, 
                onClick, 11f);
        }
    }
}
