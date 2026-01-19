// ============================================================================
// APEX CITADELS - MASTER ASSET SETUP WIZARD
// One-click tool to set up ALL game assets from the asset libraries
// Run this after adding new assets from Meshy, Suno, Mixamo, etc.
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Master wizard that sets up all game assets in one click.
    /// Combines the functionality of:
    /// - AssetDatabaseRefreshTool (models, sounds, animations, skyboxes)
    /// - SFXLibraryGenerator (CombatSFXLibrary)
    /// - UISoundLibraryGenerator (UISoundLibrary) 
    /// - AnimationControllerGenerator (Animator Controllers)
    /// - Prefab generation
    /// </summary>
    public class MasterAssetSetupWizard : EditorWindow
    {
        private Vector2 scrollPos;
        private bool showStatus = true;
        
        // Status
        private int modelsCount;
        private int sfxCount;
        private int animationsCount;
        private int skyboxesCount;
        private bool hasGameAssetDatabase;
        private bool hasCombatSFXLibrary;
        private bool hasUISoundLibrary;
        private bool hasAnimatorController;

        [MenuItem("Tools/Apex Citadels/MASTER ASSET SETUP", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<MasterAssetSetupWizard>("Asset Setup Wizard");
            window.minSize = new Vector2(500, 650);
            window.Show();
        }

        [MenuItem("Tools/Apex Citadels/Run Full Asset Setup", false, 1)]
        public static void RunFullSetup()
        {
            Debug.Log("=".PadRight(60, '='));
            Debug.Log("[APEX CITADELS] Starting Full Asset Setup...");
            Debug.Log("=".PadRight(60, '='));
            
            // Step 1: Refresh Asset Database
            AssetDatabaseRefreshTool.QuickRefreshAll();
            
            // Step 2: Generate SFX Library
            SFXLibraryGenerator.QuickGenerate();
            
            // Step 3: Generate UI Sound Library
            UISoundLibraryGenerator.QuickGenerate();
            
            // Step 4: Generate Animation Controllers
            AnimationControllerGenerator.QuickGenerate();
            
            // Final save
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("=".PadRight(60, '='));
            Debug.Log("[APEX CITADELS] Full Asset Setup Complete!");
            Debug.Log("=".PadRight(60, '='));
            
            EditorUtility.DisplayDialog("Asset Setup Complete", 
                "All game assets have been configured!\n\n" +
                "- GameAssetDatabase populated\n" +
                "- CombatSFXLibrary generated\n" +
                "- UISoundLibrary generated\n" +
                "- Animation Controllers created\n\n" +
                "Your assets are ready to use.", "OK");
        }

        private void OnEnable()
        {
            RefreshStatus();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Header
            DrawHeader();
            
            GUILayout.Space(10);
            
            // Status section
            DrawStatusSection();
            
            GUILayout.Space(15);
            
            // Main action button
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("RUN FULL ASSET SETUP", GUILayout.Height(50)))
            {
                RunFullSetup();
                RefreshStatus();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(15);
            
            // Individual tools
            DrawIndividualTools();
            
            GUILayout.Space(15);
            
            // Help section
            DrawHelpSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            
            GUILayout.Label("APEX CITADELS", headerStyle);
            GUILayout.Label("Master Asset Setup Wizard", EditorStyles.centeredGreyMiniLabel);
            
            GUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "This wizard configures all game assets from your asset libraries.\n" +
                "Run this after adding new assets from Meshy, Suno, Mixamo, or Blockade.",
                MessageType.Info);
        }

        private void DrawStatusSection()
        {
            showStatus = EditorGUILayout.Foldout(showStatus, "Asset Status", true);
            
            if (!showStatus) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("Asset Files Found:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  3D Models:", $"{modelsCount} files");
            EditorGUILayout.LabelField("  Sound Effects:", $"{sfxCount} files");
            EditorGUILayout.LabelField("  Animations:", $"{animationsCount} files");
            EditorGUILayout.LabelField("  Skyboxes:", $"{skyboxesCount} files");
            
            GUILayout.Space(10);
            
            GUILayout.Label("Configuration Status:", EditorStyles.boldLabel);
            DrawStatusRow("GameAssetDatabase", hasGameAssetDatabase);
            DrawStatusRow("CombatSFXLibrary", hasCombatSFXLibrary);
            DrawStatusRow("UISoundLibrary", hasUISoundLibrary);
            DrawStatusRow("Animation Controller", hasAnimatorController);
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Refresh Status", GUILayout.Height(25)))
            {
                RefreshStatus();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawStatusRow(string label, bool isConfigured)
        {
            EditorGUILayout.BeginHorizontal();
            
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = isConfigured ? Color.green : Color.yellow;
            
            string icon = isConfigured ? "[OK]" : "[--]";
            EditorGUILayout.LabelField($"  {icon} {label}", style);
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawIndividualTools()
        {
            EditorGUILayout.LabelField("Individual Tools:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (GUILayout.Button("1. Refresh Asset Database", GUILayout.Height(28)))
            {
                AssetDatabaseRefreshTool.QuickRefreshAll();
                RefreshStatus();
            }
            
            if (GUILayout.Button("2. Generate Combat SFX Library", GUILayout.Height(28)))
            {
                SFXLibraryGenerator.QuickGenerate();
                RefreshStatus();
            }
            
            if (GUILayout.Button("3. Generate UI Sound Library", GUILayout.Height(28)))
            {
                UISoundLibraryGenerator.QuickGenerate();
                RefreshStatus();
            }
            
            if (GUILayout.Button("4. Generate Animation Controllers", GUILayout.Height(28)))
            {
                AnimationControllerGenerator.QuickGenerate();
                RefreshStatus();
            }
            
            GUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Asset DB Editor"))
            {
                AssetDatabaseRefreshTool.ShowWindow();
            }
            if (GUILayout.Button("Open Animation Editor"))
            {
                AnimationControllerGenerator.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawHelpSection()
        {
            EditorGUILayout.LabelField("Asset Pipeline Guide:", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "ASSET LOCATIONS:\n\n" +
                "3D Models: Assets/Art/Models/\n" +
                "  - Buildings/ (B##_Name.glb)\n" +
                "  - Towers/ (T##_Name.glb)\n" +
                "  - Walls/ (W##_Name.glb)\n\n" +
                "Sound Effects: Assets/Audio/SFX/\n" +
                "  - SFX-BLD##_name.mp3 (Building)\n" +
                "  - SFX-CMB##_name.mp3 (Combat)\n" +
                "  - SFX-UI##_name.mp3 (UI)\n\n" +
                "Animations: Assets/Animations/Mixamo/\n" +
                "  - ###_AnimationName.fbx\n\n" +
                "Skyboxes: Assets/Art/Skyboxes/\n" +
                "  - SKY##_description.png/jpg",
                MessageType.None);
            
            GUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "ADDING NEW ASSETS:\n" +
                "1. Download assets from Meshy, Suno, Mixamo, Blockade\n" +
                "2. Import into the appropriate folder\n" +
                "3. Run 'Full Asset Setup' or individual tools\n" +
                "4. Assets are automatically wired up!",
                MessageType.Info);
        }

        private void RefreshStatus()
        {
            // Count files
            modelsCount = CountFiles("Assets/Art/Models", "*.glb") + 
                         CountFiles("Assets/Art/Models", "*.fbx");
            sfxCount = CountFiles("Assets/Audio/SFX", "*.mp3");
            animationsCount = CountFiles("Assets/Animations/Mixamo", "*.fbx");
            skyboxesCount = CountFiles("Assets/Art/Skyboxes", "*.png") +
                           CountFiles("Assets/Art/Skyboxes", "*.jpg");
            
            // Check configurations
            hasGameAssetDatabase = AssetDatabase.FindAssets("t:GameAssetDatabase").Length > 0;
            hasCombatSFXLibrary = AssetDatabase.FindAssets("t:CombatSFXLibrary").Length > 0;
            hasUISoundLibrary = AssetDatabase.FindAssets("t:UISoundLibrary").Length > 0;
            hasAnimatorController = File.Exists("Assets/Animations/Controllers/HumanoidController.controller");
        }

        private int CountFiles(string folder, string pattern)
        {
            if (!Directory.Exists(folder)) return 0;
            
            try
            {
                return Directory.GetFiles(folder, pattern, SearchOption.AllDirectories).Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}
#endif
