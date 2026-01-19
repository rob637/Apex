// ============================================================================
// APEX CITADELS - ASSET DATABASE REFRESH TOOL
// Editor tool to scan folders and populate the GameAssetDatabase
// Run this whenever you add new assets to automatically integrate them
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ApexCitadels.Core.Assets;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Editor tool that scans asset folders and populates the GameAssetDatabase.
    /// Menu: Tools > Apex Citadels > Refresh Asset Database
    /// </summary>
    public class AssetDatabaseRefreshTool : EditorWindow
    {
        private GameAssetDatabase database;
        private Vector2 scrollPos;
        private bool showModels = true;
        private bool showSFX = true;
        private bool showAnimations = true;
        private bool showSkyboxes = true;

        // Scan results
        private int modelsFound;
        private int sfxFound;
        private int animationsFound;
        private int skyboxesFound;

        [MenuItem("Apex Citadels/Assets/Refresh Asset Database", false, 40)]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetDatabaseRefreshTool>("Asset Database");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        [MenuItem("Apex Citadels/Assets/Quick Refresh All Assets", false, 41)]
        public static void QuickRefreshAll()
        {
            var db = LoadOrCreateDatabase();
            if (db == null) return;

            RefreshAllAssets(db);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            
            Debug.Log("[AssetDB] Quick refresh complete! All assets updated.");
        }

        private void OnEnable()
        {
            database = LoadOrCreateDatabase();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("Asset Database Manager", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Database reference
            EditorGUI.BeginChangeCheck();
            database = (GameAssetDatabase)EditorGUILayout.ObjectField("Database", database, typeof(GameAssetDatabase), false);
            if (EditorGUI.EndChangeCheck() && database != null)
            {
                EditorUtility.SetDirty(database);
            }

            if (database == null)
            {
                EditorGUILayout.HelpBox("No GameAssetDatabase found. Click 'Create Database' to create one.", MessageType.Warning);
                if (GUILayout.Button("Create Database", GUILayout.Height(30)))
                {
                    database = CreateDatabase();
                }
                EditorGUILayout.EndScrollView();
                return;
            }

            GUILayout.Space(10);

            // Quick stats
            EditorGUILayout.LabelField("Current Database Contents:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  Buildings: {database.BuildingModels.Count}");
            EditorGUILayout.LabelField($"  Towers: {database.TowerModels.Count}");
            EditorGUILayout.LabelField($"  Walls: {database.WallModels.Count}");
            EditorGUILayout.LabelField($"  SFX: {database.SFX.AllClips.Count}");
            EditorGUILayout.LabelField($"  Animations: {database.Animations.AllClips.Count}");
            EditorGUILayout.LabelField($"  Skyboxes: {database.Skyboxes.Count}");

            GUILayout.Space(15);

            // Refresh buttons
            EditorGUILayout.LabelField("Refresh Options:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("REFRESH ALL ASSETS", GUILayout.Height(40)))
            {
                RefreshAllAssets(database);
                EditorUtility.SetDirty(database);
            }

            GUILayout.Space(10);

            // Individual refresh buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Models"))
            {
                RefreshModels(database);
                EditorUtility.SetDirty(database);
            }
            if (GUILayout.Button("Refresh SFX"))
            {
                RefreshSFX(database);
                EditorUtility.SetDirty(database);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Animations"))
            {
                RefreshAnimations(database);
                EditorUtility.SetDirty(database);
            }
            if (GUILayout.Button("Refresh Skyboxes"))
            {
                RefreshSkyboxes(database);
                EditorUtility.SetDirty(database);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            // Prefab generation
            EditorGUILayout.LabelField("Prefab Generation:", EditorStyles.boldLabel);
            if (GUILayout.Button("Generate Prefabs from Models", GUILayout.Height(30)))
            {
                GeneratePrefabs(database);
                EditorUtility.SetDirty(database);
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Asset Folder Structure:\n" +
                "• Models: Assets/Art/Models/Buildings, Towers, Walls, Foundations, Roofs\n" +
                "• SFX: Assets/Audio/SFX (files named SFX-XXX##_name.mp3)\n" +
                "• Animations: Assets/Animations/Mixamo (files named ###_Name.fbx)\n" +
                "• Skyboxes: Assets/Art/Skyboxes (SKY##.png/jpg)\n\n" +
                "When you add new assets, just click 'Refresh All Assets' to integrate them!",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Save Database"))
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                Debug.Log("[AssetDB] Database saved!");
            }

            EditorGUILayout.EndScrollView();
        }

        #region Database Creation

        private static GameAssetDatabase LoadOrCreateDatabase()
        {
            // Try to load from Resources
            var db = UnityEngine.Resources.Load<GameAssetDatabase>("GameAssetDatabase");
            if (db != null) return db;

            // Try to find in project
            string[] guids = AssetDatabase.FindAssets("t:GameAssetDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<GameAssetDatabase>(path);
            }

            return null;
        }

        private static GameAssetDatabase CreateDatabase()
        {
            // Create Resources folder if needed
            string resourcesPath = "Assets/Resources";
            if (!Directory.Exists(resourcesPath))
                Directory.CreateDirectory(resourcesPath);

            // Create the database
            var db = ScriptableObject.CreateInstance<GameAssetDatabase>();
            string dbPath = "Assets/Resources/GameAssetDatabase.asset";
            AssetDatabase.CreateAsset(db, dbPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[AssetDB] Created GameAssetDatabase at {dbPath}");
            return db;
        }

        #endregion

        #region Refresh Methods

        private static void RefreshAllAssets(GameAssetDatabase db)
        {
            Debug.Log("[AssetDB] Starting full asset refresh...");
            
            RefreshModels(db);
            RefreshSFX(db);
            RefreshAnimations(db);
            RefreshSkyboxes(db);
            
            Debug.Log("[AssetDB] Full refresh complete!");
        }

        private static void RefreshModels(GameAssetDatabase db)
        {
            Debug.Log("[AssetDB] Scanning 3D models...");

            // Buildings
            db.BuildingModels.Clear();
            var buildingFiles = FindModelFiles("Assets/Art/Models/Buildings");
            foreach (var file in buildingFiles)
            {
                var entry = CreateBuildingEntry(file);
                if (entry != null) db.BuildingModels.Add(entry);
            }
            Debug.Log($"[AssetDB] Found {db.BuildingModels.Count} building models");

            // Towers
            db.TowerModels.Clear();
            var towerFiles = FindModelFiles("Assets/Art/Models/Towers");
            foreach (var file in towerFiles)
            {
                var entry = CreateTowerEntry(file);
                if (entry != null) db.TowerModels.Add(entry);
            }
            Debug.Log($"[AssetDB] Found {db.TowerModels.Count} tower models");

            // Walls
            db.WallModels.Clear();
            var wallFiles = FindModelFiles("Assets/Art/Models/Walls");
            foreach (var file in wallFiles)
            {
                var entry = CreateWallEntry(file);
                if (entry != null) db.WallModels.Add(entry);
            }
            Debug.Log($"[AssetDB] Found {db.WallModels.Count} wall models");

            // Foundations
            db.FoundationModels.Clear();
            var foundationFiles = FindModelFiles("Assets/Art/Models/Foundations");
            foreach (var file in foundationFiles)
            {
                var entry = CreateModelEntry(file);
                if (entry != null) db.FoundationModels.Add(entry);
            }

            // Roofs
            db.RoofModels.Clear();
            var roofFiles = FindModelFiles("Assets/Art/Models/Roofs");
            foreach (var file in roofFiles)
            {
                var entry = CreateModelEntry(file);
                if (entry != null) db.RoofModels.Add(entry);
            }
        }

        private static void RefreshSFX(GameAssetDatabase db)
        {
            Debug.Log("[AssetDB] Scanning sound effects...");
            
            db.SFX.AllClips.Clear();
            db.SFX.ClearCache();

            string sfxPath = "Assets/Audio/SFX";
            if (!Directory.Exists(sfxPath))
            {
                Debug.LogWarning($"[AssetDB] SFX folder not found: {sfxPath}");
                return;
            }

            string[] audioFiles = Directory.GetFiles(sfxPath, "*.mp3", SearchOption.AllDirectories);
            
            foreach (var file in audioFiles)
            {
                string assetPath = file.Replace("\\", "/");
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip == null) continue;

                string fileName = Path.GetFileNameWithoutExtension(file);
                var entry = ParseSFXEntry(fileName, clip);
                if (entry != null)
                {
                    db.SFX.AllClips.Add(entry);
                }
            }

            // Also check for .wav and .ogg files
            string[] wavFiles = Directory.GetFiles(sfxPath, "*.wav", SearchOption.AllDirectories);
            string[] oggFiles = Directory.GetFiles(sfxPath, "*.ogg", SearchOption.AllDirectories);
            
            foreach (var file in wavFiles.Concat(oggFiles))
            {
                string assetPath = file.Replace("\\", "/");
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip == null) continue;

                string fileName = Path.GetFileNameWithoutExtension(file);
                var entry = ParseSFXEntry(fileName, clip);
                if (entry != null && !db.SFX.AllClips.Exists(e => e.Id == entry.Id))
                {
                    db.SFX.AllClips.Add(entry);
                }
            }

            Debug.Log($"[AssetDB] Found {db.SFX.AllClips.Count} sound effects");
        }

        private static void RefreshAnimations(GameAssetDatabase db)
        {
            Debug.Log("[AssetDB] Scanning animations...");
            
            db.Animations.AllClips.Clear();
            db.Animations.ClearCache();

            string animPath = "Assets/Animations/Mixamo";
            if (!Directory.Exists(animPath))
            {
                Debug.LogWarning($"[AssetDB] Animations folder not found: {animPath}");
                return;
            }

            string[] fbxFiles = Directory.GetFiles(animPath, "*.fbx", SearchOption.AllDirectories);
            
            foreach (var file in fbxFiles)
            {
                string assetPath = file.Replace("\\", "/");
                
                // Load all clips from the FBX
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var asset in allAssets)
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        var entry = ParseAnimationEntry(fileName, clip);
                        if (entry != null)
                        {
                            db.Animations.AllClips.Add(entry);
                        }
                        break; // Usually one main clip per FBX
                    }
                }
            }

            Debug.Log($"[AssetDB] Found {db.Animations.AllClips.Count} animations");
        }

        private static void RefreshSkyboxes(GameAssetDatabase db)
        {
            Debug.Log("[AssetDB] Scanning skyboxes...");
            
            db.Skyboxes.Clear();

            string skyboxPath = "Assets/Art/Skyboxes";
            string resourcesSkyboxPath = "Assets/Resources/PC/Skyboxes";
            
            var allPaths = new List<string>();
            if (Directory.Exists(skyboxPath))
                allPaths.AddRange(Directory.GetFiles(skyboxPath, "*.*", SearchOption.TopDirectoryOnly));
            if (Directory.Exists(resourcesSkyboxPath))
                allPaths.AddRange(Directory.GetFiles(resourcesSkyboxPath, "*.*", SearchOption.TopDirectoryOnly));

            foreach (var file in allPaths)
            {
                if (!file.EndsWith(".png") && !file.EndsWith(".jpg") && !file.EndsWith(".jpeg"))
                    continue;
                if (file.EndsWith(".meta")) continue;

                string assetPath = file.Replace("\\", "/");
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (tex == null) continue;

                string fileName = Path.GetFileNameWithoutExtension(file);
                
                // Create or find material
                Material mat = CreateSkyboxMaterial(tex, fileName);

                var entry = new SkyboxEntry
                {
                    Name = fileName,
                    Texture = tex,
                    Material = mat,
                    TimeOfDay = GuessSkyboxTime(fileName)
                };
                
                // Avoid duplicates
                if (!db.Skyboxes.Exists(s => s.Name == fileName))
                {
                    db.Skyboxes.Add(entry);
                }
            }

            Debug.Log($"[AssetDB] Found {db.Skyboxes.Count} skyboxes");
        }

        #endregion

        #region Model Entry Creators

        private static List<string> FindModelFiles(string folder)
        {
            var files = new List<string>();
            if (!Directory.Exists(folder))
            {
                Debug.LogWarning($"[AssetDB] Folder does not exist: {folder}");
                return files;
            }

            files.AddRange(Directory.GetFiles(folder, "*.glb", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(folder, "*.fbx", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(folder, "*.obj", SearchOption.AllDirectories));

            Debug.Log($"[AssetDB] Found {files.Count} model files in {folder}");
            return files;
        }

        private static ModelEntry CreateModelEntry(string filePath)
        {
            string assetPath = filePath.Replace("\\", "/");
            
            // Force import if not yet imported
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null)
            {
                Debug.LogWarning($"[AssetDB] Could not load model: {assetPath}");
                return null;
            }

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            
            return new ModelEntry
            {
                Id = ExtractId(fileName),
                Name = ExtractName(fileName),
                Model = model
            };
        }

        private static BuildingModelEntry CreateBuildingEntry(string filePath)
        {
            string assetPath = filePath.Replace("\\", "/");
            
            // Force import if not yet imported
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null)
            {
                Debug.LogWarning($"[AssetDB] Could not load building model: {assetPath}");
                return null;
            }

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            
            var entry = new BuildingModelEntry
            {
                Id = ExtractId(fileName),
                Name = ExtractName(fileName),
                Model = model,
                Category = GuessBuildingCategory(fileName),
                IsVariant = fileName.Contains("Variant"),
                VariantType = ExtractVariantType(fileName)
            };

            return entry;
        }

        private static TowerModelEntry CreateTowerEntry(string filePath)
        {
            string assetPath = filePath.Replace("\\", "/");
            
            // Force import if not yet imported
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null)
            {
                Debug.LogWarning($"[AssetDB] Could not load tower model: {assetPath}");
                return null;
            }

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            
            return new TowerModelEntry
            {
                Id = ExtractId(fileName),
                Name = ExtractName(fileName),
                Model = model,
                Type = GuessTowerType(fileName),
                IsVariant = fileName.Contains("Variant"),
                VariantType = ExtractVariantType(fileName)
            };
        }

        private static WallModelEntry CreateWallEntry(string filePath)
        {
            string assetPath = filePath.Replace("\\", "/");
            
            // Force import if not yet imported
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null)
            {
                Debug.LogWarning($"[AssetDB] Could not load wall model: {assetPath}");
                return null;
            }

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            
            return new WallModelEntry
            {
                Id = ExtractId(fileName),
                Name = ExtractName(fileName),
                Model = model,
                Type = GuessWallType(fileName),
                WallMaterial = GuessWallMaterial(fileName),
                IsCorner = fileName.ToLower().Contains("corner"),
                IsGate = fileName.ToLower().Contains("gate"),
                IsVariant = fileName.Contains("Variant"),
                VariantType = ExtractVariantType(fileName)
            };
        }

        #endregion

        #region Parse Helpers

        private static string ExtractId(string fileName)
        {
            // Extract ID like "B01", "T05", "W12"
            if (fileName.Length >= 3 && char.IsLetter(fileName[0]) && char.IsDigit(fileName[1]))
            {
                int endIdx = 1;
                while (endIdx < fileName.Length && char.IsDigit(fileName[endIdx])) endIdx++;
                return fileName.Substring(0, endIdx);
            }
            return fileName;
        }

        private static string ExtractName(string fileName)
        {
            // Remove ID prefix and underscores
            string name = fileName;
            int underscoreIdx = fileName.IndexOf('_');
            if (underscoreIdx > 0)
            {
                name = fileName.Substring(underscoreIdx + 1);
            }
            return name.Replace("_", " ");
        }

        private static string ExtractVariantType(string fileName)
        {
            if (!fileName.Contains("Variant")) return null;
            
            int variantIdx = fileName.IndexOf("Variant_");
            if (variantIdx >= 0)
            {
                return fileName.Substring(variantIdx + 8).Replace("_", " ");
            }
            return "Variant";
        }

        private static BuildingCategory GuessBuildingCategory(string fileName)
        {
            string lower = fileName.ToLower();
            if (lower.Contains("gold") || lower.Contains("lumber") || lower.Contains("quarry") || 
                lower.Contains("crystal") || lower.Contains("farm") || lower.Contains("mine"))
                return BuildingCategory.Resource;
            if (lower.Contains("barrack") || lower.Contains("armory") || lower.Contains("siege") || 
                lower.Contains("stable") || lower.Contains("blacksmith"))
                return BuildingCategory.Military;
            if (lower.Contains("market") || lower.Contains("treasury") || lower.Contains("bank") || 
                lower.Contains("warehouse"))
                return BuildingCategory.Economic;
            if (lower.Contains("magic") || lower.Contains("alchemist") || lower.Contains("portal") ||
                lower.Contains("academy"))
                return BuildingCategory.Magic;
            if (lower.Contains("tavern") || lower.Contains("hospital") || lower.Contains("prison") ||
                lower.Contains("library"))
                return BuildingCategory.Support;
            return BuildingCategory.Support;
        }

        private static TowerType GuessTowerType(string fileName)
        {
            string lower = fileName.ToLower();
            if (lower.Contains("guard")) return TowerType.Guard;
            if (lower.Contains("archer")) return TowerType.Archer;
            if (lower.Contains("mage") || lower.Contains("magic")) return TowerType.Mage;
            if (lower.Contains("siege")) return TowerType.Siege;
            if (lower.Contains("bell") || lower.Contains("clock") || lower.Contains("light") ||
                lower.Contains("windmill") || lower.Contains("signal") || lower.Contains("watch"))
                return TowerType.Utility;
            return TowerType.Guard;
        }

        private static WallType GuessWallType(string fileName)
        {
            string lower = fileName.ToLower();
            if (lower.Contains("corner")) return WallType.Corner;
            if (lower.Contains("t-junction") || lower.Contains("tjunction")) return WallType.TJunction;
            if (lower.Contains("end_cap") || lower.Contains("endcap")) return WallType.EndCap;
            if (lower.Contains("gate")) return WallType.Gate;
            if (lower.Contains("window")) return WallType.Window;
            if (lower.Contains("half")) return WallType.Half;
            if (lower.Contains("fence")) return WallType.Fence;
            if (lower.Contains("barricade")) return WallType.Barricade;
            if (lower.Contains("trench")) return WallType.Trench;
            if (lower.Contains("magic") || lower.Contains("barrier")) return WallType.Magic;
            return WallType.Straight;
        }

        private static WallMaterial GuessWallMaterial(string fileName)
        {
            string lower = fileName.ToLower();
            if (lower.Contains("stone")) return WallMaterial.Stone;
            if (lower.Contains("wood") || lower.Contains("palisade") || lower.Contains("plank"))
                return WallMaterial.Wood;
            if (lower.Contains("iron")) return WallMaterial.Iron;
            if (lower.Contains("magic") || lower.Contains("barrier")) return WallMaterial.Magic;
            if (lower.Contains("ice")) return WallMaterial.Ice;
            if (lower.Contains("bone")) return WallMaterial.Bone;
            return WallMaterial.Stone;
        }

        private static SFXEntry ParseSFXEntry(string fileName, AudioClip clip)
        {
            // Expected format: SFX-XXX##_name
            // e.g., SFX-CMB01_sword_swing
            
            if (!fileName.StartsWith("SFX-")) return null;

            string withoutPrefix = fileName.Substring(4); // Remove "SFX-"
            
            // Get category code (3 chars)
            if (withoutPrefix.Length < 5) return null;
            string categoryCode = withoutPrefix.Substring(0, 3);
            
            // Get ID (category + number)
            int underscoreIdx = withoutPrefix.IndexOf('_');
            string id = underscoreIdx > 0 ? withoutPrefix.Substring(0, underscoreIdx) : withoutPrefix;
            
            // Get name
            string name = underscoreIdx > 0 ? withoutPrefix.Substring(underscoreIdx + 1) : "";

            SFXCategory category = categoryCode switch
            {
                "BLD" => SFXCategory.Building,
                "CHR" => SFXCategory.Character,
                "CMB" => SFXCategory.Combat,
                "ENV" => SFXCategory.Environment,
                "FX0" or "FX1" or "FX2" => SFXCategory.Effects,
                "UI0" or "UI1" or "UI2" or "UI3" or "UI4" => SFXCategory.UI,
                _ => SFXCategory.Effects
            };

            // Fix category code for FX and UI
            if (categoryCode.StartsWith("FX")) category = SFXCategory.Effects;
            if (categoryCode.StartsWith("UI")) category = SFXCategory.UI;

            return new SFXEntry
            {
                Id = id,
                Name = name,
                Category = category,
                Clip = clip,
                DefaultVolume = 1f,
                PitchVariation = 0.05f
            };
        }

        private static AnimationEntry ParseAnimationEntry(string fileName, AnimationClip clip)
        {
            // Expected format: ###_Name.fbx
            // e.g., 001_Walking.fbx

            string id = "";
            string name = fileName;
            
            int underscoreIdx = fileName.IndexOf('_');
            if (underscoreIdx > 0)
            {
                id = fileName.Substring(0, underscoreIdx);
                name = fileName.Substring(underscoreIdx + 1);
            }

            AnimationType type = GuessAnimationType(name);
            bool isLooping = type == AnimationType.Walk || type == AnimationType.Run || 
                            type == AnimationType.Idle || name.ToLower().Contains("idle") ||
                            name.ToLower().Contains("walk") || name.ToLower().Contains("run");

            return new AnimationEntry
            {
                Id = id,
                Name = name.Replace("_", " "),
                Type = type,
                Clip = clip,
                IsLooping = isLooping
            };
        }

        private static AnimationType GuessAnimationType(string name)
        {
            string lower = name.ToLower().Replace("_", "").Replace(" ", "");
            
            // Locomotion
            if (lower.Contains("walkback")) return AnimationType.WalkBackward;
            if (lower.Contains("leftstrafe") && lower.Contains("walk")) return AnimationType.WalkLeft;
            if (lower.Contains("rightstrafe") && lower.Contains("walk")) return AnimationType.WalkRight;
            if (lower.Contains("injured") && lower.Contains("walk")) return AnimationType.WalkInjured;
            if (lower.Contains("sneak")) return AnimationType.Sneak;
            if (lower.Contains("walk")) return AnimationType.Walk;
            
            if (lower.Contains("runback")) return AnimationType.RunBackward;
            if (lower.Contains("fastrun")) return AnimationType.RunFast;
            if (lower.Contains("combatrun")) return AnimationType.RunCombat;
            if (lower.Contains("injuredrun")) return AnimationType.RunCombat;
            if (lower.Contains("run")) return AnimationType.Run;
            
            if (lower.Contains("runningjump")) return AnimationType.Jump;
            if (lower.Contains("jump")) return AnimationType.Jump;
            if (lower.Contains("fallingidle") || lower.Contains("falling")) return AnimationType.Fall;
            if (lower.Contains("hardlanding")) return AnimationType.HardLand;
            if (lower.Contains("landing")) return AnimationType.Land;
            
            if (lower.Contains("dodgeleft")) return AnimationType.DodgeLeft;
            if (lower.Contains("dodgeright")) return AnimationType.DodgeRight;
            if (lower.Contains("dodgeback")) return AnimationType.DodgeBack;
            if (lower.Contains("combatroll") || lower.Contains("roll")) return AnimationType.Roll;
            if (lower.Contains("dodge")) return AnimationType.Dodge;
            
            if (lower.Contains("leftturn")) return AnimationType.TurnLeft;
            if (lower.Contains("rightturn")) return AnimationType.TurnRight;
            if (lower.Contains("turn")) return AnimationType.Turn;
            
            // Idles
            if (lower.Contains("alertidle")) return AnimationType.IdleAlert;
            if (lower.Contains("tiredidle")) return AnimationType.IdleTired;
            if (lower.Contains("happyidle")) return AnimationType.IdleHappy;
            if (lower.Contains("sadidle")) return AnimationType.IdleSad;
            if (lower.Contains("breathingidle")) return AnimationType.IdleBreathing;
            if (lower.Contains("swordidle") && !lower.Contains("two")) return AnimationType.IdleSword;
            if (lower.Contains("twohandswordidle") || lower.Contains("2handswordidle")) return AnimationType.IdleTwoHandSword;
            if (lower.Contains("bowidle")) return AnimationType.IdleBow;
            if (lower.Contains("shieldidle")) return AnimationType.IdleShield;
            if (lower.Contains("spearidle")) return AnimationType.IdleSpear;
            if (lower.Contains("lookaround")) return AnimationType.LookAround;
            if (lower.Contains("scratchhead")) return AnimationType.ScratchHead;
            if (lower.Contains("stretch")) return AnimationType.Stretch;
            if (lower.Contains("yawn")) return AnimationType.Yawn;
            if (lower.Contains("checkwatch")) return AnimationType.CheckWatch;
            if (lower.Contains("idle")) return AnimationType.Idle;
            
            // Combat
            if (lower.Contains("swordandshieldslash")) return AnimationType.SwordShieldSlash;
            if (lower.Contains("swordandshieldattack")) return AnimationType.SwordShieldAttack;
            if (lower.Contains("greatswordslash")) return AnimationType.GreatSwordSlash;
            if (lower.Contains("greatswordattack")) return AnimationType.GreatSwordAttack;
            if (lower.Contains("greatswordoverhead")) return AnimationType.GreatSwordOverhead;
            if (lower.Contains("swordslash")) return AnimationType.SwordSlash;
            if (lower.Contains("swordblock")) return AnimationType.SwordBlock;
            if (lower.Contains("swordparry")) return AnimationType.SwordParry;
            if (lower.Contains("drawsword")) return AnimationType.SwordDraw;
            if (lower.Contains("sheathesword")) return AnimationType.SwordSheathe;
            if (lower.Contains("twohandswordattack") || lower.Contains("twohandattack")) return AnimationType.TwoHandAttack;
            if (lower.Contains("twohandblock")) return AnimationType.TwoHandBlock;
            if (lower.Contains("axeslash")) return AnimationType.AxeSlash;
            if (lower.Contains("axeattack")) return AnimationType.AxeAttack;
            if (lower.Contains("spearthrust")) return AnimationType.SpearThrust;
            if (lower.Contains("spearswing")) return AnimationType.SpearSwing;
            if (lower.Contains("spearblock")) return AnimationType.SpearBlock;
            if (lower.Contains("shieldblockhigh")) return AnimationType.ShieldBlockHigh;
            if (lower.Contains("shieldbash")) return AnimationType.ShieldBash;
            if (lower.Contains("shieldblock")) return AnimationType.ShieldBlock;
            if (lower.Contains("throw")) return AnimationType.Throw;
            
            // Ranged
            if (lower.Contains("drawarrow")) return AnimationType.DrawArrow;
            if (lower.Contains("aimidle")) return AnimationType.AimIdle;
            if (lower.Contains("runningbowfire")) return AnimationType.BowFireRunning;
            if (lower.Contains("bowfire")) return AnimationType.BowFire;
            if (lower.Contains("crossbowreload")) return AnimationType.CrossbowReload;
            if (lower.Contains("crossbowfire")) return AnimationType.CrossbowFire;
            
            // Magic
            if (lower.Contains("castingspell") || lower.Contains("castspell")) return AnimationType.CastSpell;
            if (lower.Contains("magicattack")) return AnimationType.MagicAttack;
            if (lower.Contains("channel")) return AnimationType.Channel;
            if (lower.Contains("blessing") || lower.Contains("bless")) return AnimationType.Bless;
            if (lower.Contains("magicaoe") || lower.Contains("aoe")) return AnimationType.MagicAOE;
            if (lower.Contains("summon")) return AnimationType.Summon;
            
            // Hit reactions
            if (lower.Contains("hitfromback")) return AnimationType.HitFromBack;
            if (lower.Contains("hitreactionleft") || lower.Contains("hitleft")) return AnimationType.HitLeft;
            if (lower.Contains("hitreactionright") || lower.Contains("hitright")) return AnimationType.HitRight;
            if (lower.Contains("heavyhit")) return AnimationType.HeavyHit;
            if (lower.Contains("hitreaction") || lower.Contains("hit")) return AnimationType.HitReaction;
            if (lower.Contains("knockback")) return AnimationType.Knockback;
            if (lower.Contains("knockeddown") || lower.Contains("knockdown")) return AnimationType.KnockedDown;
            if (lower.Contains("gettingup") || lower.Contains("getup")) return AnimationType.GetUp;
            
            // Deaths
            if (lower.Contains("deathforward")) return AnimationType.DeathForward;
            if (lower.Contains("deathbackward")) return AnimationType.DeathBackward;
            if (lower.Contains("deathleft")) return AnimationType.DeathLeft;
            if (lower.Contains("deathright")) return AnimationType.DeathRight;
            if (lower.Contains("dramaticdeath")) return AnimationType.DeathDramatic;
            if (lower.Contains("risingfromground") || lower.Contains("rising")) return AnimationType.RiseFromGround;
            
            // Interactions
            if (lower.Contains("pickingup") || lower.Contains("pickup")) return AnimationType.PickUp;
            if (lower.Contains("reachingup") || lower.Contains("reachup")) return AnimationType.ReachUp;
            if (lower.Contains("putdown")) return AnimationType.PutDown;
            if (lower.Contains("pushing") || lower.Contains("push")) return AnimationType.Push;
            if (lower.Contains("pulling") || lower.Contains("pull") && !lower.Contains("lever")) return AnimationType.Pull;
            if (lower.Contains("openingchest") || lower.Contains("openchest")) return AnimationType.OpenChest;
            if (lower.Contains("opendoor")) return AnimationType.OpenDoor;
            if (lower.Contains("leverpull") || lower.Contains("lever")) return AnimationType.PullLever;
            
            // Social
            if (lower.Contains("waving") || lower.Contains("wave")) return AnimationType.Wave;
            if (lower.Contains("bowing") || lower.Contains("bow") && !lower.Contains("cross")) return AnimationType.Bow;
            if (lower.Contains("salute")) return AnimationType.Salute;
            if (lower.Contains("clapping") || lower.Contains("clap")) return AnimationType.Clap;
            if (lower.Contains("victorycheer") || lower.Contains("cheer")) return AnimationType.Cheer;
            if (lower.Contains("headshake")) return AnimationType.HeadShake;
            if (lower.Contains("headnod") || lower.Contains("nod")) return AnimationType.HeadNod;
            if (lower.Contains("pointing") || lower.Contains("point")) return AnimationType.Point;
            
            // Work
            if (lower.Contains("hammer")) return AnimationType.Hammer;
            
            return AnimationType.Idle;
        }

        private static SkyboxTime GuessSkyboxTime(string fileName)
        {
            string lower = fileName.ToLower();
            if (lower.Contains("night") || lower.Contains("dark")) return SkyboxTime.Night;
            if (lower.Contains("sunset") || lower.Contains("dusk")) return SkyboxTime.Sunset;
            if (lower.Contains("dawn") || lower.Contains("sunrise")) return SkyboxTime.Dawn;
            if (lower.Contains("storm")) return SkyboxTime.Stormy;
            if (lower.Contains("day") || lower.Contains("bright")) return SkyboxTime.Day;
            return SkyboxTime.Any;
        }

        private static Material CreateSkyboxMaterial(Texture2D tex, string name)
        {
            // Check if material already exists
            string matDir = "Assets/Materials/Skyboxes";
            if (!Directory.Exists(matDir))
                Directory.CreateDirectory(matDir);

            string matPath = $"{matDir}/{name}_Skybox.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null) return existing;

            // Create new material
            Shader shader = Shader.Find("Skybox/Panoramic");
            if (shader == null)
            {
                shader = Shader.Find("Skybox/Cubemap");
            }
            if (shader == null) return null;

            Material mat = new Material(shader);
            mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_Exposure", 1.2f);

            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        #endregion

        #region Prefab Generation

        private static void GeneratePrefabs(GameAssetDatabase db)
        {
            Debug.Log("[AssetDB] Generating prefabs from models...");

            string prefabDir = "Assets/Prefabs/Generated";
            if (!Directory.Exists(prefabDir))
                Directory.CreateDirectory(prefabDir);

            int count = 0;

            // Buildings
            foreach (var entry in db.BuildingModels)
            {
                if (entry.Model == null) continue;
                entry.Prefab = CreateBuildingPrefab(entry, prefabDir);
                if (entry.Prefab != null) count++;
            }

            // Towers
            foreach (var entry in db.TowerModels)
            {
                if (entry.Model == null) continue;
                entry.Prefab = CreateTowerPrefab(entry, prefabDir);
                if (entry.Prefab != null) count++;
            }

            // Walls
            foreach (var entry in db.WallModels)
            {
                if (entry.Model == null) continue;
                entry.Prefab = CreateWallPrefab(entry, prefabDir);
                if (entry.Prefab != null) count++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[AssetDB] Generated {count} prefabs");
        }

        private static GameObject CreateBuildingPrefab(BuildingModelEntry entry, string prefabDir)
        {
            string categoryDir = $"{prefabDir}/Buildings";
            if (!Directory.Exists(categoryDir))
                Directory.CreateDirectory(categoryDir);

            string prefabPath = $"{categoryDir}/{entry.Id}_{entry.Name.Replace(" ", "_")}.prefab";
            
            // Check if prefab already exists
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            // Instantiate model and add components
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.Model);
            
            // Add box collider if none exists
            if (instance.GetComponent<Collider>() == null)
            {
                var meshFilter = instance.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null)
                {
                    var collider = instance.AddComponent<BoxCollider>();
                    // Size will be auto-calculated
                }
            }

            // Save as prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            return prefab;
        }

        private static GameObject CreateTowerPrefab(TowerModelEntry entry, string prefabDir)
        {
            string categoryDir = $"{prefabDir}/Towers";
            if (!Directory.Exists(categoryDir))
                Directory.CreateDirectory(categoryDir);

            string prefabPath = $"{categoryDir}/{entry.Id}_{entry.Name.Replace(" ", "_")}.prefab";
            
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.Model);
            
            if (instance.GetComponent<Collider>() == null)
            {
                instance.AddComponent<BoxCollider>();
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            return prefab;
        }

        private static GameObject CreateWallPrefab(WallModelEntry entry, string prefabDir)
        {
            string categoryDir = $"{prefabDir}/Walls";
            if (!Directory.Exists(categoryDir))
                Directory.CreateDirectory(categoryDir);

            string prefabPath = $"{categoryDir}/{entry.Id}_{entry.Name.Replace(" ", "_")}.prefab";
            
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.Model);
            
            if (instance.GetComponent<Collider>() == null)
            {
                instance.AddComponent<BoxCollider>();
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            return prefab;
        }

        #endregion
    }
}
#endif
