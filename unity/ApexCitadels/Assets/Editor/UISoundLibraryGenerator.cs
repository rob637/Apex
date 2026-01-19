// ============================================================================
// APEX CITADELS - UI SOUND LIBRARY AUTO-GENERATOR
// Editor tool to auto-populate UISoundLibrary from Assets/Audio/SFX/SFX-UI* files
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using ApexCitadels.PC.Audio;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Auto-generates UISoundLibrary from UI audio files
    /// </summary>
    public class UISoundLibraryGenerator : EditorWindow
    {
        private UISoundLibrary library;

        [MenuItem("Apex Citadels/Assets/Generate UI Sound Library", false, 44)]
        public static void ShowWindow()
        {
            var window = GetWindow<UISoundLibraryGenerator>("UI Sound Generator");
            window.minSize = new Vector2(400, 350);
            window.Show();
        }

        [MenuItem("Apex Citadels/Assets/Quick Generate UI Sounds", false, 45)]
        public static void QuickGenerate()
        {
            var lib = LoadOrCreateLibrary();
            if (lib == null) return;

            GenerateLibrary(lib);
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            
            Debug.Log("[UI SFX] Quick generation complete! UISoundLibrary updated.");
        }

        private void OnEnable()
        {
            library = LoadOrCreateLibrary();
        }

        private void OnGUI()
        {
            GUILayout.Label("UI Sound Library Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            library = (UISoundLibrary)EditorGUILayout.ObjectField("Library", library, typeof(UISoundLibrary), false);

            if (library == null)
            {
                EditorGUILayout.HelpBox("No UISoundLibrary found. Click 'Create Library' to create one.", MessageType.Warning);
                if (GUILayout.Button("Create Library", GUILayout.Height(30)))
                {
                    library = CreateLibrary();
                }
                return;
            }

            GUILayout.Space(10);
            
            string sfxPath = "Assets/Audio/SFX";
            int uiFileCount = 0;
            if (Directory.Exists(sfxPath))
            {
                uiFileCount = Directory.GetFiles(sfxPath, "SFX-UI*.mp3").Length;
            }
            EditorGUILayout.LabelField($"UI Sound Files Found: {uiFileCount}");

            GUILayout.Space(15);

            if (GUILayout.Button("GENERATE UI SOUND LIBRARY", GUILayout.Height(40)))
            {
                GenerateLibrary(library);
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
                Debug.Log("[UI SFX] Library generated and saved!");
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "UI Sound Mapping:\n" +
                "• UI01-06: Button sounds\n" +
                "• UI07-10: Toggle/Slider\n" +
                "• UI11-18: Panel navigation\n" +
                "• UI19-26: Notifications\n" +
                "• UI27-37: Rewards & Currency\n" +
                "• UI38-48: Menu & System",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Save Library"))
            {
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
                Debug.Log("[UI SFX] Library saved!");
            }
        }

        private static UISoundLibrary LoadOrCreateLibrary()
        {
            string[] guids = AssetDatabase.FindAssets("t:UISoundLibrary");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<UISoundLibrary>(path);
            }
            return null;
        }

        private static UISoundLibrary CreateLibrary()
        {
            string dir = "Assets/Audio";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var lib = ScriptableObject.CreateInstance<UISoundLibrary>();
            string path = $"{dir}/UISoundLibrary.asset";
            AssetDatabase.CreateAsset(lib, path);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[UI SFX] Created UISoundLibrary at {path}");
            return lib;
        }

        private static void GenerateLibrary(UISoundLibrary library)
        {
            Debug.Log("[UI SFX] Generating library from audio files...");

            string sfxPath = "Assets/Audio/SFX";
            if (!Directory.Exists(sfxPath))
            {
                Debug.LogWarning($"[UI SFX] SFX folder not found: {sfxPath}");
                return;
            }

            // Load UI audio clips
            var uiClips = new Dictionary<string, AudioClip>();
            var uiFiles = Directory.GetFiles(sfxPath, "SFX-UI*.mp3");
            
            foreach (var file in uiFiles)
            {
                string assetPath = file.Replace("\\", "/");
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip != null)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    uiClips[fileName] = clip;
                }
            }

            Debug.Log($"[UI SFX] Loaded {uiClips.Count} UI audio clips");

            // Map files to UISoundType
            var mappings = new Dictionary<string, UISoundType>
            {
                // Buttons
                { "SFX-UI01_button_click_standard", UISoundType.ButtonClick },
                { "SFX-UI02_button_click_confirm", UISoundType.Confirm },
                { "SFX-UI03_button_click_cancel", UISoundType.Cancel },
                { "SFX-UI04_button_click_special", UISoundType.ButtonSpecial },
                { "SFX-UI05_button_hover", UISoundType.ButtonHover },
                { "SFX-UI06_button_disabled", UISoundType.Error },
                
                // Toggles & Sliders
                { "SFX-UI07_toggle_on", UISoundType.ToggleOn },
                { "SFX-UI08_toggle_off", UISoundType.ToggleOff },
                { "SFX-UI09_slider_tick", UISoundType.SliderTick },
                { "SFX-UI10_slider_end", UISoundType.SliderEnd },
                
                // Panels
                { "SFX-UI11_panel_open", UISoundType.PanelOpen },
                { "SFX-UI12_panel_close", UISoundType.PanelClose },
                { "SFX-UI13_popup_appear", UISoundType.PopupAppear },
                { "SFX-UI14_popup_dismiss", UISoundType.PopupDismiss },
                { "SFX-UI15_tab_switch", UISoundType.TabSwitch },
                { "SFX-UI16_scroll_tick", UISoundType.ScrollTick },
                { "SFX-UI17_drawer_open", UISoundType.DrawerOpen },
                { "SFX-UI18_drawer_close", UISoundType.DrawerClose },
                
                // Notifications
                { "SFX-UI19_notification_info", UISoundType.NotificationInfo },
                { "SFX-UI20_notification_success", UISoundType.Success },
                { "SFX-UI21_notification_warning", UISoundType.Warning },
                { "SFX-UI22_notification_error", UISoundType.NotificationError },
                { "SFX-UI23_notification_message", UISoundType.Message },
                { "SFX-UI24_notification_friend", UISoundType.FriendNotification },
                { "SFX-UI25_notification_achievement", UISoundType.Achievement },
                { "SFX-UI26_notification_quest", UISoundType.QuestComplete },
                
                // Currency & Rewards
                { "SFX-UI27_coin_single", UISoundType.CoinSingle },
                { "SFX-UI28_coin_multiple", UISoundType.CoinMultiple },
                { "SFX-UI29_coin_large", UISoundType.CoinLarge },
                { "SFX-UI30_gem_collect", UISoundType.GemCollect },
                { "SFX-UI31_resource_collect", UISoundType.ResourceCollect },
                { "SFX-UI32_xp_gain", UISoundType.XPGain },
                { "SFX-UI33_level_up", UISoundType.LevelUp },
                { "SFX-UI34_reward_chest_open", UISoundType.ChestOpen },
                { "SFX-UI35_reward_item_reveal", UISoundType.ItemReveal },
                { "SFX-UI36_reward_rare", UISoundType.RareReward },
                { "SFX-UI37_reward_legendary", UISoundType.LegendaryReward },
                
                // Menu Navigation
                { "SFX-UI38_menu_navigate", UISoundType.MenuNavigate },
                { "SFX-UI39_menu_select", UISoundType.MenuSelect },
                { "SFX-UI40_menu_back", UISoundType.MenuBack },
                
                // System
                { "SFX-UI41_loading_start", UISoundType.LoadingStart },
                { "SFX-UI42_loading_complete", UISoundType.LoadingComplete },
                { "SFX-UI43_screenshot", UISoundType.Screenshot },
                { "SFX-UI44_countdown_tick", UISoundType.CountdownTick },
                { "SFX-UI45_countdown_final", UISoundType.CountdownFinal },
                { "SFX-UI46_typing_key", UISoundType.TypingKey },
                { "SFX-UI47_error_buzz", UISoundType.ErrorBuzz },
                { "SFX-UI48_confirm_chime", UISoundType.ConfirmChime },
            };

            int mappedCount = 0;
            foreach (var mapping in mappings)
            {
                if (uiClips.TryGetValue(mapping.Key, out AudioClip clip))
                {
                    library.SetSound(mapping.Value, clip);
                    mappedCount++;
                }
            }

            Debug.Log($"[UI SFX] Mapped {mappedCount} UI sounds to library");
        }
    }
}
#endif
