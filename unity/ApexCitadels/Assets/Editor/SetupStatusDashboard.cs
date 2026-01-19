// ============================================================================
// APEX CITADELS - SETUP STATUS DASHBOARD
// Shows the complete status of all game systems at a glance
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEngine.Rendering;
using ApexCitadels.Core.Assets;
using ApexCitadels.PC;

namespace ApexCitadels.Editor
{
    public class SetupStatusDashboard : EditorWindow
    {
        private Vector2 scrollPos;
        
        // Status cache
        private bool _needsRefresh = true;
        private float _lastRefreshTime;
        
        // Asset Database
        private bool _hasGameAssetDatabase;
        private int _buildingCount;
        private int _towerCount;
        private int _wallCount;
        private int _foundationCount;
        private int _roofCount;
        private int _sfxCount;
        private int _animationCount;
        private int _skyboxCount;
        
        // Libraries
        private bool _hasCombatSFXLibrary;
        private bool _hasUISoundLibrary;
        private bool _hasAnimatorController;
        
        // Scene Managers
        private bool _hasPCSceneBootstrapper;
        private bool _hasWorldMapRenderer;
        private bool _hasPCGameController;
        private bool _hasPCInputManager;
        private bool _hasPCUIManager;
        private bool _hasWorldEnvironment;
        private bool _hasBuildingModelProvider;
        
        // Render Pipeline
        private bool _hasURPAsset;
        private bool _hasURPLitShader;
        private bool _hasDirectionalLight;
        
        // Folders
        private bool _hasModelsFolder;
        private bool _hasSFXFolder;
        private bool _hasAnimationsFolder;
        private bool _hasSkyboxesFolder;

        [MenuItem("Apex Citadels/★ Quick Start/Setup Status Dashboard", false, 3)]
        public static void ShowWindow()
        {
            var window = GetWindow<SetupStatusDashboard>("Setup Status");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        private void OnEnable()
        {
            _needsRefresh = true;
        }

        private void OnFocus()
        {
            _needsRefresh = true;
        }

        private void OnGUI()
        {
            if (_needsRefresh || Time.realtimeSinceStartup - _lastRefreshTime > 2f)
            {
                RefreshStatus();
                _needsRefresh = false;
                _lastRefreshTime = Time.realtimeSinceStartup;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Header
            GUILayout.Space(5);
            EditorGUILayout.LabelField("APEX CITADELS SETUP STATUS", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Complete overview of all game systems", EditorStyles.miniLabel);
            GUILayout.Space(10);

            // Overall Progress
            DrawOverallProgress();
            GUILayout.Space(10);

            // Sections
            DrawAssetDatabaseSection();
            GUILayout.Space(5);
            DrawLibrariesSection();
            GUILayout.Space(5);
            DrawSceneManagersSection();
            GUILayout.Space(5);
            DrawRenderPipelineSection();
            GUILayout.Space(5);
            DrawFolderStructureSection();
            
            GUILayout.Space(15);
            
            // Action Buttons
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Status", GUILayout.Height(25)))
            {
                _needsRefresh = true;
            }
            if (GUILayout.Button("Fix All Issues", GUILayout.Height(25)))
            {
                FixAllIssues();
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Open One-Click Setup", GUILayout.Height(30)))
            {
                EditorApplication.ExecuteMenuItem("Apex Citadels/★ Quick Start/ONE-CLICK SETUP (Start Here!)");
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshStatus()
        {
            // Asset Database
            var db = FindAssetDatabase();
            _hasGameAssetDatabase = db != null;
            if (db != null)
            {
                _buildingCount = db.BuildingModels?.Count ?? 0;
                _towerCount = db.TowerModels?.Count ?? 0;
                _wallCount = db.WallModels?.Count ?? 0;
                _sfxCount = db.SFX?.AllClips?.Count ?? 0;
                _animationCount = db.Animations?.AllClips?.Count ?? 0;
                _skyboxCount = db.Skyboxes?.Count ?? 0;
            }
            
            // Count files in folders
            _foundationCount = CountFilesInFolder("Assets/Art/Models/Foundations", "*.glb");
            _roofCount = CountFilesInFolder("Assets/Art/Models/Roofs", "*.glb");
            
            // Libraries
            _hasCombatSFXLibrary = AssetDatabase.FindAssets("t:CombatSFXLibrary").Length > 0;
            _hasUISoundLibrary = AssetDatabase.FindAssets("t:UISoundLibrary").Length > 0;
            _hasAnimatorController = AssetDatabase.FindAssets("HumanoidAnimator t:AnimatorController").Length > 0;
            
            // Scene Managers (check in scene)
            _hasPCSceneBootstrapper = Object.FindFirstObjectByType<PCSceneBootstrapper>() != null;
            _hasWorldMapRenderer = FindObjectByTypeName("WorldMapRenderer");
            _hasPCGameController = FindObjectByTypeName("PCGameController");
            _hasPCInputManager = FindObjectByTypeName("PCInputManager");
            _hasPCUIManager = FindObjectByTypeName("PCUIManager");
            _hasWorldEnvironment = FindObjectByTypeName("WorldEnvironmentManager");
            _hasBuildingModelProvider = FindObjectByTypeName("BuildingModelProvider");
            
            // Render Pipeline
            _hasURPAsset = GraphicsSettings.currentRenderPipeline != null;
            _hasURPLitShader = Shader.Find("Universal Render Pipeline/Lit") != null;
            _hasDirectionalLight = Object.FindFirstObjectByType<Light>() != null;
            
            // Folders
            _hasModelsFolder = Directory.Exists("Assets/Art/Models");
            _hasSFXFolder = Directory.Exists("Assets/Audio/SFX");
            _hasAnimationsFolder = Directory.Exists("Assets/Animations/Mixamo");
            _hasSkyboxesFolder = Directory.Exists("Assets/Resources/PC/Skyboxes") || 
                                 Directory.Exists("Assets/Art/Skyboxes");
        }

        private void DrawOverallProgress()
        {
            int total = 20;
            int complete = 0;
            
            // Count completed items
            if (_hasGameAssetDatabase) complete++;
            if (_buildingCount > 0) complete++;
            if (_towerCount > 0) complete++;
            if (_wallCount > 0) complete++;
            if (_sfxCount > 0 || _hasCombatSFXLibrary) complete++;
            if (_animationCount > 0) complete++;
            if (_skyboxCount > 0) complete++;
            if (_hasCombatSFXLibrary) complete++;
            if (_hasUISoundLibrary) complete++;
            if (_hasAnimatorController) complete++;
            if (_hasWorldMapRenderer) complete++;
            if (_hasPCGameController) complete++;
            if (_hasPCInputManager) complete++;
            if (_hasPCUIManager) complete++;
            if (_hasWorldEnvironment) complete++;
            if (_hasURPAsset) complete++;
            if (_hasURPLitShader) complete++;
            if (_hasDirectionalLight) complete++;
            if (_hasModelsFolder) complete++;
            if (_hasSFXFolder) complete++;
            
            float progress = (float)complete / total;
            
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(25)), 
                progress, $"Overall Setup: {complete}/{total} ({(int)(progress * 100)}%)");
            
            if (progress >= 1f)
            {
                EditorGUILayout.HelpBox("✓ All systems configured! Your project is ready.", MessageType.Info);
            }
            else if (progress >= 0.7f)
            {
                EditorGUILayout.HelpBox("Almost there! A few items need attention.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Setup incomplete. Run One-Click Setup to configure.", MessageType.Error);
            }
        }

        private void DrawAssetDatabaseSection()
        {
            EditorGUILayout.LabelField("📦 ASSET DATABASE", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            DrawStatusRow("GameAssetDatabase", _hasGameAssetDatabase, 
                _hasGameAssetDatabase ? "Created" : "Not Found - Run Asset Setup");
            
            if (_hasGameAssetDatabase)
            {
                DrawStatusRow("  Buildings", _buildingCount > 0, $"{_buildingCount} loaded");
                DrawStatusRow("  Towers", _towerCount > 0, $"{_towerCount} loaded");
                DrawStatusRow("  Walls", _wallCount > 0, $"{_wallCount} loaded");
                DrawStatusRow("  SFX Clips", _sfxCount > 0, $"{_sfxCount} loaded");
                DrawStatusRow("  Animations", _animationCount > 0, $"{_animationCount} loaded");
                DrawStatusRow("  Skyboxes", _skyboxCount > 0, $"{_skyboxCount} loaded");
            }
            
            // Show files on disk even if not in database
            EditorGUILayout.LabelField("Files on Disk:", EditorStyles.miniLabel);
            int modelsOnDisk = CountFilesInFolder("Assets/Art/Models/Buildings", "*.glb") +
                              CountFilesInFolder("Assets/Art/Models/Towers", "*.glb") +
                              CountFilesInFolder("Assets/Art/Models/Walls", "*.glb");
            int sfxOnDisk = CountFilesInFolder("Assets/Audio/SFX", "*.mp3");
            int animsOnDisk = CountFilesInFolder("Assets/Animations/Mixamo", "*.fbx");
            
            EditorGUILayout.LabelField($"  Models: {modelsOnDisk} GLB files", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"  Audio: {sfxOnDisk} MP3 files", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"  Animations: {animsOnDisk} FBX files", EditorStyles.miniLabel);
            
            EditorGUI.indentLevel--;
        }

        private void DrawLibrariesSection()
        {
            EditorGUILayout.LabelField("📚 LIBRARIES", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            DrawStatusRow("CombatSFXLibrary", _hasCombatSFXLibrary,
                _hasCombatSFXLibrary ? "Ready" : "Generate from SFX Generator");
            DrawStatusRow("UISoundLibrary", _hasUISoundLibrary,
                _hasUISoundLibrary ? "Ready" : "Generate from UI Sound Generator");
            DrawStatusRow("AnimatorController", _hasAnimatorController,
                _hasAnimatorController ? "Ready" : "Generate from Animation Generator");
            
            EditorGUI.indentLevel--;
        }

        private void DrawSceneManagersSection()
        {
            EditorGUILayout.LabelField("🎮 SCENE MANAGERS", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            DrawStatusRow("PCSceneBootstrapper", _hasPCSceneBootstrapper,
                _hasPCSceneBootstrapper ? "Found" : "Not in scene - REQUIRED");
            DrawStatusRow("WorldMapRenderer", _hasWorldMapRenderer,
                _hasWorldMapRenderer ? "Found" : "Not in scene");
            DrawStatusRow("PCGameController", _hasPCGameController,
                _hasPCGameController ? "Found" : "Not in scene");
            DrawStatusRow("PCInputManager", _hasPCInputManager,
                _hasPCInputManager ? "Found" : "Not in scene");
            DrawStatusRow("PCUIManager", _hasPCUIManager,
                _hasPCUIManager ? "Found" : "Not in scene");
            DrawStatusRow("WorldEnvironmentManager", _hasWorldEnvironment,
                _hasWorldEnvironment ? "Found" : "Not in scene");
            DrawStatusRow("BuildingModelProvider", _hasBuildingModelProvider,
                _hasBuildingModelProvider ? "Found" : "Add for 3D models");
            
            EditorGUI.indentLevel--;
            
            // Quick fix for missing bootstrapper
            if (!_hasPCSceneBootstrapper)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                if (GUILayout.Button("Add PCSceneBootstrapper to Scene", GUILayout.Height(22)))
                {
                    AddPCSceneBootstrapper();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void AddPCSceneBootstrapper()
        {
            if (Object.FindFirstObjectByType<PCSceneBootstrapper>() == null)
            {
                GameObject bootObj = new GameObject("PCSceneBootstrapper");
                bootObj.AddComponent<PCSceneBootstrapper>();
                Undo.RegisterCreatedObjectUndo(bootObj, "Create PCSceneBootstrapper");
                Debug.Log("[Setup] Added PCSceneBootstrapper to scene");
                _needsRefresh = true;
            }
        }

        private void DrawRenderPipelineSection()
        {
            EditorGUILayout.LabelField("🎨 RENDER PIPELINE", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            DrawStatusRow("URP Pipeline Asset", _hasURPAsset,
                _hasURPAsset ? GraphicsSettings.currentRenderPipeline.name : "Not configured!");
            DrawStatusRow("URP/Lit Shader", _hasURPLitShader,
                _hasURPLitShader ? "Available" : "Missing - check URP package");
            DrawStatusRow("Directional Light", _hasDirectionalLight,
                _hasDirectionalLight ? "Found" : "Add for lighting");
            
            EditorGUI.indentLevel--;
        }

        private void DrawFolderStructureSection()
        {
            EditorGUILayout.LabelField("📁 FOLDER STRUCTURE", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            DrawStatusRow("Assets/Art/Models", _hasModelsFolder, 
                _hasModelsFolder ? "Exists" : "Create folder");
            DrawStatusRow("Assets/Audio/SFX", _hasSFXFolder,
                _hasSFXFolder ? "Exists" : "Create folder");
            DrawStatusRow("Assets/Animations/Mixamo", _hasAnimationsFolder,
                _hasAnimationsFolder ? "Exists" : "Create folder");
            DrawStatusRow("Skyboxes Folder", _hasSkyboxesFolder,
                _hasSkyboxesFolder ? "Exists" : "Create folder");
            
            EditorGUI.indentLevel--;
        }

        private void DrawStatusRow(string label, bool isOk, string detail)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Status icon
            GUIStyle iconStyle = new GUIStyle(EditorStyles.label);
            iconStyle.normal.textColor = isOk ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.3f, 0.3f);
            iconStyle.fontStyle = FontStyle.Bold;
            
            EditorGUILayout.LabelField(isOk ? "✓" : "✗", iconStyle, GUILayout.Width(20));
            EditorGUILayout.LabelField(label, GUILayout.Width(180));
            
            GUIStyle detailStyle = new GUIStyle(EditorStyles.miniLabel);
            detailStyle.normal.textColor = isOk ? Color.gray : new Color(0.9f, 0.5f, 0.3f);
            EditorGUILayout.LabelField(detail, detailStyle);
            
            EditorGUILayout.EndHorizontal();
        }

        private GameAssetDatabase FindAssetDatabase()
        {
            // Try Resources first
            var db = UnityEngine.Resources.Load<GameAssetDatabase>("GameAssetDatabase");
            if (db != null) return db;
            
            // Search project
            string[] guids = AssetDatabase.FindAssets("t:GameAssetDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<GameAssetDatabase>(path);
            }
            return null;
        }

        private int CountFilesInFolder(string folder, string pattern)
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

        private bool FindObjectByTypeName(string typeName)
        {
            var allObjects = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            return allObjects.Any(obj => obj.GetType().Name == typeName);
        }

        private void FixAllIssues()
        {
            bool fixedSomething = false;
            
            // Create folders if missing
            if (!_hasModelsFolder)
            {
                Directory.CreateDirectory("Assets/Art/Models/Buildings");
                Directory.CreateDirectory("Assets/Art/Models/Towers");
                Directory.CreateDirectory("Assets/Art/Models/Walls");
                Directory.CreateDirectory("Assets/Art/Models/Foundations");
                Directory.CreateDirectory("Assets/Art/Models/Roofs");
                fixedSomething = true;
            }
            
            if (!_hasSFXFolder)
            {
                Directory.CreateDirectory("Assets/Audio/SFX");
                fixedSomething = true;
            }
            
            if (!_hasAnimationsFolder)
            {
                Directory.CreateDirectory("Assets/Animations/Mixamo");
                fixedSomething = true;
            }
            
            if (fixedSomething)
            {
                AssetDatabase.Refresh();
                Debug.Log("[Setup] Created missing folders");
            }
            
            // Suggest running full setup
            if (!_hasGameAssetDatabase || !_hasCombatSFXLibrary)
            {
                if (EditorUtility.DisplayDialog("Run Full Setup?",
                    "Some components need to be created. Would you like to run the Full Asset Setup?",
                    "Yes, Run Setup", "Cancel"))
                {
                    EditorApplication.ExecuteMenuItem("Apex Citadels/★ Quick Start/Full Asset Setup Wizard");
                }
            }
            
            _needsRefresh = true;
        }
    }
}
#endif
