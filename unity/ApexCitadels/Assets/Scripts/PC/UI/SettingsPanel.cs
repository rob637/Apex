using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Settings Panel - Audio, Graphics, Controls, Account settings.
    /// Essential for any PC game.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color activeTabColor = new Color(0.2f, 0.4f, 0.6f);
        
        // UI Elements
        private GameObject _panel;
        private Dictionary<SettingsCategory, GameObject> _categoryTabs = new Dictionary<SettingsCategory, GameObject>();
        private GameObject _settingsContainer;
        private SettingsCategory _selectedCategory = SettingsCategory.Audio;
        
        // Settings values
        private GameSettings _settings;
        
        public static SettingsPanel Instance { get; private set; }
        
        // Events
        public event Action<GameSettings> OnSettingsChanged;
        public event Action OnSettingsSaved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadSettings();
        }

        private void Start()
        {
            CreateSettingsPanel();
            Hide();
        }

        private void LoadSettings()
        {
            // Load from PlayerPrefs or use defaults
            _settings = new GameSettings
            {
                MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f),
                MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f),
                SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f),
                UIVolume = PlayerPrefs.GetFloat("UIVolume", 0.8f),
                
                GraphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", 2),
                Fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1,
                VSync = PlayerPrefs.GetInt("VSync", 1) == 1,
                ShowFPS = PlayerPrefs.GetInt("ShowFPS", 0) == 1,
                ParticleQuality = PlayerPrefs.GetInt("ParticleQuality", 2),
                ShadowQuality = PlayerPrefs.GetInt("ShadowQuality", 2),
                
                CameraSensitivity = PlayerPrefs.GetFloat("CameraSensitivity", 1.0f),
                InvertY = PlayerPrefs.GetInt("InvertY", 0) == 1,
                EdgePanning = PlayerPrefs.GetInt("EdgePanning", 1) == 1,
                KeyboardScrollSpeed = PlayerPrefs.GetFloat("KeyboardScrollSpeed", 1.0f),
                
                ShowDamageNumbers = PlayerPrefs.GetInt("ShowDamageNumbers", 1) == 1,
                ShowMinimap = PlayerPrefs.GetInt("ShowMinimap", 1) == 1,
                ShowResourcePopups = PlayerPrefs.GetInt("ShowResourcePopups", 1) == 1,
                ChatFilter = PlayerPrefs.GetInt("ChatFilter", 1) == 1,
                ScreenShake = PlayerPrefs.GetInt("ScreenShake", 1) == 1
            };
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", _settings.MasterVolume);
            PlayerPrefs.SetFloat("MusicVolume", _settings.MusicVolume);
            PlayerPrefs.SetFloat("SFXVolume", _settings.SFXVolume);
            PlayerPrefs.SetFloat("UIVolume", _settings.UIVolume);
            
            PlayerPrefs.SetInt("GraphicsQuality", _settings.GraphicsQuality);
            PlayerPrefs.SetInt("Fullscreen", _settings.Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt("VSync", _settings.VSync ? 1 : 0);
            PlayerPrefs.SetInt("ShowFPS", _settings.ShowFPS ? 1 : 0);
            PlayerPrefs.SetInt("ParticleQuality", _settings.ParticleQuality);
            PlayerPrefs.SetInt("ShadowQuality", _settings.ShadowQuality);
            
            PlayerPrefs.SetFloat("CameraSensitivity", _settings.CameraSensitivity);
            PlayerPrefs.SetInt("InvertY", _settings.InvertY ? 1 : 0);
            PlayerPrefs.SetInt("EdgePanning", _settings.EdgePanning ? 1 : 0);
            PlayerPrefs.SetFloat("KeyboardScrollSpeed", _settings.KeyboardScrollSpeed);
            
            PlayerPrefs.SetInt("ShowDamageNumbers", _settings.ShowDamageNumbers ? 1 : 0);
            PlayerPrefs.SetInt("ShowMinimap", _settings.ShowMinimap ? 1 : 0);
            PlayerPrefs.SetInt("ShowResourcePopups", _settings.ShowResourcePopups ? 1 : 0);
            PlayerPrefs.SetInt("ChatFilter", _settings.ChatFilter ? 1 : 0);
            PlayerPrefs.SetInt("ScreenShake", _settings.ScreenShake ? 1 : 0);
            
            PlayerPrefs.Save();
            
            ApplySettings();
            OnSettingsSaved?.Invoke();
            ApexLogger.Log("[Settings] Settings saved!", ApexLogger.LogCategory.UI);
        }

        private void ApplySettings()
        {
            // Apply audio
            AudioListener.volume = _settings.MasterVolume;
            
            // Apply graphics
            QualitySettings.SetQualityLevel(_settings.GraphicsQuality);
            Screen.fullScreen = _settings.Fullscreen;
            QualitySettings.vSyncCount = _settings.VSync ? 1 : 0;
            
            // Apply to camera controller if exists
            if (PCCameraController.Instance != null)
            {
                PCCameraController.Instance.SetSensitivity(_settings.CameraSensitivity);
                PCCameraController.Instance.SetEdgePanning(_settings.EdgePanning);
            }
            
            OnSettingsChanged?.Invoke(_settings);
        }

        private void CreateSettingsPanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("SettingsPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.15f);
            rect.anchorMax = new Vector2(0.8f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);
            
            VerticalLayoutGroup vlayout = _panel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Header
            CreateHeader();
            
            // Category tabs
            CreateCategoryTabs();
            
            // Settings content
            CreateSettingsContent();
            
            // Action buttons
            CreateActionButtons();
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
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "⚙️ SETTINGS";
            title.fontSize = 28;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = accentColor;
            
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
            
            GameObject x = new GameObject("X");
            x.transform.SetParent(closeBtn.transform, false);
            
            TextMeshProUGUI xText = x.AddComponent<TextMeshProUGUI>();
            xText.text = "✕";
            xText.fontSize = 24;
            xText.alignment = TextAlignmentOptions.Center;
            
            RectTransform xRect = x.GetComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
        }

        private void CreateCategoryTabs()
        {
            GameObject tabs = new GameObject("Tabs");
            tabs.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = tabs.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = tabs.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(30, 30, 0, 0);
            
            CreateCategoryTab(tabs.transform, SettingsCategory.Audio, "🔊 Audio");
            CreateCategoryTab(tabs.transform, SettingsCategory.Graphics, "🖥️ Graphics");
            CreateCategoryTab(tabs.transform, SettingsCategory.Controls, "🎮 Controls");
            CreateCategoryTab(tabs.transform, SettingsCategory.Gameplay, "🎯 Gameplay");
            CreateCategoryTab(tabs.transform, SettingsCategory.Account, "👤 Account");
        }

        private void CreateCategoryTab(Transform parent, SettingsCategory category, string label)
        {
            GameObject tab = new GameObject($"Tab_{category}");
            tab.transform.SetParent(parent, false);
            
            LayoutElement le = tab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 35;
            
            Image bg = tab.AddComponent<Image>();
            bg.color = category == _selectedCategory ? activeTabColor : new Color(0.15f, 0.15f, 0.2f);
            
            Button btn = tab.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectCategory(category));
            
            ColorBlock colors = btn.colors;
            colors.highlightedColor = activeTabColor * 0.8f;
            colors.pressedColor = activeTabColor * 0.6f;
            btn.colors = colors;
            
            GameObject txt = new GameObject("Label");
            txt.transform.SetParent(tab.transform, false);
            
            TextMeshProUGUI text = txt.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            RectTransform txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            
            _categoryTabs[category] = tab;
        }

        private void CreateSettingsContent()
        {
            _settingsContainer = new GameObject("SettingsContent");
            _settingsContainer.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _settingsContainer.AddComponent<LayoutElement>();
            le.flexibleHeight = 1;
            
            Image bg = _settingsContainer.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.5f);
            
            VerticalLayoutGroup vlayout = _settingsContainer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 8;
            vlayout.padding = new RectOffset(20, 20, 15, 15);
            
            RefreshSettingsContent();
        }

        private void RefreshSettingsContent()
        {
            // Clear existing
            foreach (Transform child in _settingsContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            switch (_selectedCategory)
            {
                case SettingsCategory.Audio:
                    CreateAudioSettings();
                    break;
                case SettingsCategory.Graphics:
                    CreateGraphicsSettings();
                    break;
                case SettingsCategory.Controls:
                    CreateControlsSettings();
                    break;
                case SettingsCategory.Gameplay:
                    CreateGameplaySettings();
                    break;
                case SettingsCategory.Account:
                    CreateAccountSettings();
                    break;
            }
        }

        private void CreateAudioSettings()
        {
            CreateSliderSetting("Master Volume", _settings.MasterVolume, 0f, 1f, v => _settings.MasterVolume = v);
            CreateSliderSetting("Music Volume", _settings.MusicVolume, 0f, 1f, v => _settings.MusicVolume = v);
            CreateSliderSetting("Sound Effects", _settings.SFXVolume, 0f, 1f, v => _settings.SFXVolume = v);
            CreateSliderSetting("UI Sounds", _settings.UIVolume, 0f, 1f, v => _settings.UIVolume = v);
            
            CreateSpacer();
            
            CreateButtonSetting("🔊 Test Sound", () => ApexLogger.Log("[Settings] Playing test sound..."), ApexLogger.LogCategory.UI);
        }

        private void CreateGraphicsSettings()
        {
            string[] qualityOptions = { "Low", "Medium", "High", "Ultra" };
            CreateDropdownSetting("Graphics Quality", qualityOptions, _settings.GraphicsQuality, v => _settings.GraphicsQuality = v);
            
            CreateToggleSetting("Fullscreen", _settings.Fullscreen, v => _settings.Fullscreen = v);
            CreateToggleSetting("VSync", _settings.VSync, v => _settings.VSync = v);
            CreateToggleSetting("Show FPS", _settings.ShowFPS, v => _settings.ShowFPS = v);
            
            CreateSpacer();
            
            string[] particleOptions = { "Off", "Low", "Medium", "High" };
            CreateDropdownSetting("Particle Quality", particleOptions, _settings.ParticleQuality, v => _settings.ParticleQuality = v);
            
            string[] shadowOptions = { "Off", "Low", "Medium", "High" };
            CreateDropdownSetting("Shadow Quality", shadowOptions, _settings.ShadowQuality, v => _settings.ShadowQuality = v);
        }

        private void CreateControlsSettings()
        {
            CreateSliderSetting("Camera Sensitivity", _settings.CameraSensitivity, 0.1f, 3f, v => _settings.CameraSensitivity = v);
            CreateSliderSetting("Scroll Speed", _settings.KeyboardScrollSpeed, 0.1f, 3f, v => _settings.KeyboardScrollSpeed = v);
            
            CreateSpacer();
            
            CreateToggleSetting("Invert Y-Axis", _settings.InvertY, v => _settings.InvertY = v);
            CreateToggleSetting("Edge Panning", _settings.EdgePanning, v => _settings.EdgePanning = v);
            
            CreateSpacer();
            
            CreateLabel("Keyboard Shortcuts:");
            CreateLabel("  WASD - Camera Movement");
            CreateLabel("  Q/E - Rotate Camera");
            CreateLabel("  Mouse Wheel - Zoom");
            CreateLabel("  L - Leaderboard");
            CreateLabel("  P - Season Pass");
            CreateLabel("  R - Daily Rewards");
            CreateLabel("  T - Territory Detail");
            CreateLabel("  B - Barracks");
            CreateLabel("  Q - Quests");
            CreateLabel("  ESC - Settings/Menu");
        }

        private void CreateGameplaySettings()
        {
            CreateToggleSetting("Show Damage Numbers", _settings.ShowDamageNumbers, v => _settings.ShowDamageNumbers = v);
            CreateToggleSetting("Show Minimap", _settings.ShowMinimap, v => _settings.ShowMinimap = v);
            CreateToggleSetting("Resource Popups", _settings.ShowResourcePopups, v => _settings.ShowResourcePopups = v);
            CreateToggleSetting("Screen Shake", _settings.ScreenShake, v => _settings.ScreenShake = v);
            
            CreateSpacer();
            
            CreateToggleSetting("Chat Profanity Filter", _settings.ChatFilter, v => _settings.ChatFilter = v);
        }

        private void CreateAccountSettings()
        {
            CreateLabel("Player ID: DEMO_USER_001");
            CreateLabel("Account Type: Guest");
            
            CreateSpacer();
            
            CreateButtonSetting("🔗 Link Account", () => ApexLogger.Log("[Settings] Link account clicked"), ApexLogger.LogCategory.UI);
            CreateButtonSetting("🔄 Sync Data", () => ApexLogger.Log("[Settings] Syncing data..."), ApexLogger.LogCategory.UI);
            
            CreateSpacer();
            
            CreateLabel("Support:");
            CreateButtonSetting("📧 Contact Support", () => ApexLogger.Log("[Settings] Opening support..."), ApexLogger.LogCategory.UI);
            CreateButtonSetting("📋 Copy Player ID", () => {
                GUIUtility.systemCopyBuffer = "DEMO_USER_001";
                ApexLogger.Log("[Settings] Player ID copied!", ApexLogger.LogCategory.UI);
            });
            
            CreateSpacer();
            
            CreateLabel("<color=#888888>Version: 0.1.0 (Alpha)</color>");
            CreateLabel("<color=#888888>© 2026 Apex Studios</color>");
        }

        #region Setting UI Helpers

        private void CreateSliderSetting(string label, float currentValue, float min, float max, Action<float> onChanged)
        {
            GameObject setting = new GameObject($"Setting_{label}");
            setting.transform.SetParent(_settingsContainer.transform, false);
            
            LayoutElement le = setting.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = setting.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(setting.transform, false);
            
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.alignment = TextAlignmentOptions.Left;
            
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 150;
            
            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(setting.transform, false);
            
            LayoutElement sliderLE = sliderObj.AddComponent<LayoutElement>();
            sliderLE.flexibleWidth = 1;
            sliderLE.preferredHeight = 20;
            
            // Slider background
            Image sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = new Color(0.2f, 0.2f, 0.25f);
            
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = currentValue;
            
            // Fill area
            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = accentColor;
            
            slider.fillRect = fillRect;
            
            // Handle
            GameObject handleArea = new GameObject("HandleArea");
            handleArea.transform.SetParent(sliderObj.transform, false);
            
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);
            
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);
            
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            
            // Value text
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(setting.transform, false);
            
            TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = $"{currentValue * 100:F0}%";
            valueText.fontSize = 14;
            valueText.alignment = TextAlignmentOptions.Right;
            
            LayoutElement valueLE = valueObj.AddComponent<LayoutElement>();
            valueLE.preferredWidth = 50;
            
            slider.onValueChanged.AddListener(v =>
            {
                valueText.text = $"{v * 100:F0}%";
                onChanged?.Invoke(v);
            });
        }

        private void CreateToggleSetting(string label, bool currentValue, Action<bool> onChanged)
        {
            GameObject setting = new GameObject($"Setting_{label}");
            setting.transform.SetParent(_settingsContainer.transform, false);
            
            LayoutElement le = setting.AddComponent<LayoutElement>();
            le.preferredHeight = 35;
            
            HorizontalLayoutGroup hlayout = setting.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(setting.transform, false);
            
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.alignment = TextAlignmentOptions.Left;
            
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.flexibleWidth = 1;
            
            // Toggle
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(setting.transform, false);
            
            LayoutElement toggleLE = toggleObj.AddComponent<LayoutElement>();
            toggleLE.preferredWidth = 50;
            toggleLE.preferredHeight = 25;
            
            Image toggleBg = toggleObj.AddComponent<Image>();
            toggleBg.color = currentValue ? accentColor : new Color(0.3f, 0.3f, 0.35f);
            
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = currentValue;
            toggle.targetGraphic = toggleBg;
            
            // Checkmark
            GameObject checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(toggleObj.transform, false);
            
            RectTransform checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI checkText = checkObj.AddComponent<TextMeshProUGUI>();
            checkText.text = currentValue ? "ON" : "OFF";
            checkText.fontSize = 12;
            checkText.fontStyle = FontStyles.Bold;
            checkText.alignment = TextAlignmentOptions.Center;
            
            toggle.onValueChanged.AddListener(v =>
            {
                toggleBg.color = v ? accentColor : new Color(0.3f, 0.3f, 0.35f);
                checkText.text = v ? "ON" : "OFF";
                onChanged?.Invoke(v);
            });
        }

        private void CreateDropdownSetting(string label, string[] options, int currentValue, Action<int> onChanged)
        {
            GameObject setting = new GameObject($"Setting_{label}");
            setting.transform.SetParent(_settingsContainer.transform, false);
            
            LayoutElement le = setting.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = setting.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(setting.transform, false);
            
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.alignment = TextAlignmentOptions.Left;
            
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 150;
            
            // Simple button-based selector (simpler than full dropdown)
            GameObject selectorObj = new GameObject("Selector");
            selectorObj.transform.SetParent(setting.transform, false);
            
            LayoutElement selectorLE = selectorObj.AddComponent<LayoutElement>();
            selectorLE.flexibleWidth = 1;
            
            HorizontalLayoutGroup selectorHL = selectorObj.AddComponent<HorizontalLayoutGroup>();
            selectorHL.childAlignment = TextAnchor.MiddleCenter;
            selectorHL.spacing = 5;
            
            // Left arrow
            GameObject leftBtn = CreateArrowButton(selectorObj.transform, "<");
            
            // Value display
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(selectorObj.transform, false);
            
            LayoutElement valueLE = valueObj.AddComponent<LayoutElement>();
            valueLE.flexibleWidth = 1;
            
            Image valueBg = valueObj.AddComponent<Image>();
            valueBg.color = new Color(0.15f, 0.15f, 0.2f);
            
            TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = options[Mathf.Clamp(currentValue, 0, options.Length - 1)];
            valueText.fontSize = 14;
            valueText.alignment = TextAlignmentOptions.Center;
            
            // Right arrow
            GameObject rightBtn = CreateArrowButton(selectorObj.transform, ">");
            
            // Wire up buttons
            int index = currentValue;
            leftBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                index = Mathf.Max(0, index - 1);
                valueText.text = options[index];
                onChanged?.Invoke(index);
            });
            
            rightBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                index = Mathf.Min(options.Length - 1, index + 1);
                valueText.text = options[index];
                onChanged?.Invoke(index);
            });
        }

        private GameObject CreateArrowButton(Transform parent, string arrow)
        {
            GameObject btn = new GameObject($"Btn_{arrow}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 30;
            le.preferredHeight = 30;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.3f);
            
            Button button = btn.AddComponent<Button>();
            
            ColorBlock colors = button.colors;
            colors.highlightedColor = accentColor;
            colors.pressedColor = accentColor * 0.7f;
            button.colors = colors;
            
            GameObject txt = new GameObject("Text");
            txt.transform.SetParent(btn.transform, false);
            
            TextMeshProUGUI text = txt.AddComponent<TextMeshProUGUI>();
            text.text = arrow;
            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            RectTransform txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            
            return btn;
        }

        private void CreateButtonSetting(string label, Action onClick)
        {
            GameObject btn = new GameObject($"Btn_{label}");
            btn.transform.SetParent(_settingsContainer.transform, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.3f, 0.4f);
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            ColorBlock colors = button.colors;
            colors.highlightedColor = accentColor;
            colors.pressedColor = accentColor * 0.7f;
            button.colors = colors;
            
            GameObject txt = new GameObject("Label");
            txt.transform.SetParent(btn.transform, false);
            
            TextMeshProUGUI text = txt.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            
            RectTransform txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
        }

        private void CreateLabel(string text)
        {
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(_settingsContainer.transform, false);
            
            LayoutElement le = labelObj.AddComponent<LayoutElement>();
            le.preferredHeight = 22;
            
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = text;
            labelText.fontSize = 12;
            labelText.color = new Color(0.7f, 0.7f, 0.7f);
        }

        private void CreateSpacer()
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(_settingsContainer.transform, false);
            
            LayoutElement le = spacer.AddComponent<LayoutElement>();
            le.preferredHeight = 15;
        }

        #endregion

        private void CreateActionButtons()
        {
            GameObject actions = new GameObject("Actions");
            actions.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = actions.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            HorizontalLayoutGroup hlayout = actions.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(100, 100, 5, 5);
            
            // Reset button
            CreateActionButton(actions.transform, "🔄 Reset to Defaults", ResetToDefaults, new Color(0.5f, 0.3f, 0.2f));
            
            // Save button
            CreateActionButton(actions.transform, "💾 Save Settings", SaveSettings, new Color(0.2f, 0.5f, 0.3f));
        }

        private void CreateActionButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btn = new GameObject("ActionBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 40;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            ColorBlock colors = button.colors;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            button.colors = colors;
            
            GameObject txt = new GameObject("Label");
            txt.transform.SetParent(btn.transform, false);
            
            TextMeshProUGUI text = txt.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            RectTransform txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
        }

        private void SelectCategory(SettingsCategory category)
        {
            _selectedCategory = category;
            
            foreach (var kvp in _categoryTabs)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == category ? activeTabColor : new Color(0.15f, 0.15f, 0.2f);
            }
            
            RefreshSettingsContent();
        }

        private void ResetToDefaults()
        {
            _settings = new GameSettings();
            RefreshSettingsContent();
            ApexLogger.Log("[Settings] Reset to defaults", ApexLogger.LogCategory.UI);
        }

        #region Public API

        public void Show()
        {
            _panel.SetActive(true);
            RefreshSettingsContent();
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

        public GameSettings GetSettings()
        {
            return _settings;
        }

        #endregion
    }

    public enum SettingsCategory
    {
        Audio,
        Graphics,
        Controls,
        Gameplay,
        Account
    }

    public class GameSettings
    {
        // Audio
        public float MasterVolume = 0.8f;
        public float MusicVolume = 0.7f;
        public float SFXVolume = 1.0f;
        public float UIVolume = 0.8f;
        
        // Graphics
        public int GraphicsQuality = 2;
        public bool Fullscreen = true;
        public bool VSync = true;
        public bool ShowFPS = false;
        public int ParticleQuality = 2;
        public int ShadowQuality = 2;
        
        // Controls
        public float CameraSensitivity = 1.0f;
        public bool InvertY = false;
        public bool EdgePanning = true;
        public float KeyboardScrollSpeed = 1.0f;
        
        // Gameplay
        public bool ShowDamageNumbers = true;
        public bool ShowMinimap = true;
        public bool ShowResourcePopups = true;
        public bool ChatFilter = true;
        public bool ScreenShake = true;
    }
}
