using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using ApexCitadels.Core.Assets;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Auto-creates and populates the GameAssetDatabase from the Art/Models folder
    /// </summary>
    public static class GameAssetDatabaseSetup
    {
        [MenuItem("Apex Citadels/Setup/Create Game Asset Database (Auto)", false, 200)]
        public static void CreateAndPopulateDatabase()
        {
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Check if already exists
            GameAssetDatabase existing = UnityEngine.Resources.Load<GameAssetDatabase>("GameAssetDatabase");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("Database Exists",
                    "GameAssetDatabase already exists. Repopulate it with models?",
                    "Repopulate", "Cancel"))
                {
                    return;
                }
                PopulateDatabase(existing);
                return;
            }

            // Create new database
            GameAssetDatabase db = ScriptableObject.CreateInstance<GameAssetDatabase>();
            AssetDatabase.CreateAsset(db, "Assets/Resources/GameAssetDatabase.asset");
            
            // Populate with models
            PopulateDatabase(db);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("✅ Game Asset Database Created!",
                $"Created and populated GameAssetDatabase with:\n\n" +
                $"• {db.BuildingModels.Count} Buildings\n" +
                $"• {db.TowerModels.Count} Towers\n" +
                $"• {db.WallModels.Count} Walls\n" +
                $"• {db.FoundationModels.Count} Foundations\n" +
                $"• {db.RoofModels.Count} Roofs\n\n" +
                "The database is now in Assets/Resources/GameAssetDatabase.asset",
                "OK");

            Selection.activeObject = db;
        }

        private static void PopulateDatabase(GameAssetDatabase db)
        {
            db.BuildingModels.Clear();
            db.TowerModels.Clear();
            db.WallModels.Clear();
            db.FoundationModels.Clear();
            db.RoofModels.Clear();

            // Scan for building models
            ScanFolder(db, "Assets/Art/Models/Buildings", ModelType.Building);
            ScanFolder(db, "Assets/Art/Models/Towers", ModelType.Tower);
            ScanFolder(db, "Assets/Art/Models/Walls", ModelType.Wall);
            ScanFolder(db, "Assets/Art/Models/Foundations", ModelType.Foundation);
            ScanFolder(db, "Assets/Art/Models/Roofs", ModelType.Roof);

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();  // Force save immediately
            Debug.Log($"[AssetDB] Populated and SAVED database: {db.BuildingModels.Count} buildings, " +
                     $"{db.TowerModels.Count} towers, {db.WallModels.Count} walls");
        }

        private enum ModelType { Building, Tower, Wall, Foundation, Roof }

        private static void ScanFolder(GameAssetDatabase db, string folderPath, ModelType type)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogWarning($"[AssetDB] Folder not found: {folderPath}");
                return;
            }

            // Search for all model types: prefabs, fbx, glb, gltf
            string[] guids = AssetDatabase.FindAssets("t:GameObject t:Model", new[] { folderPath });
            
            // Also find GLB/GLTF files directly
            string[] allFiles = System.IO.Directory.GetFiles(folderPath, "*.*", System.IO.SearchOption.AllDirectories);
            var modelFiles = new List<string>();
            
            foreach (string file in allFiles)
            {
                string ext = System.IO.Path.GetExtension(file).ToLower();
                if (ext == ".glb" || ext == ".gltf" || ext == ".fbx" || ext == ".obj")
                {
                    string unityPath = file.Replace("\\", "/");
                    if (unityPath.StartsWith(Application.dataPath))
                    {
                        unityPath = "Assets" + unityPath.Substring(Application.dataPath.Length);
                    }
                    modelFiles.Add(unityPath);
                }
            }
            
            Debug.Log($"[AssetDB] Found {guids.Length} GameObjects and {modelFiles.Count} model files in {folderPath}");
            
            // Process GameObjects from GUIDs
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ProcessModelFile(db, path, type);
            }
            
            // Process model files directly
            foreach (string path in modelFiles)
            {
                // Try to load as GameObject (Unity imports GLB as prefab)
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model != null)
                {
                    ProcessModelAsset(db, model, path, type);
                }
                else
                {
                    Debug.LogWarning($"[AssetDB] Could not load model: {path}. Unity may need to import it first. Try reimporting the Assets/Art/Models folder.");
                }
            }
        }
        
        private static void ProcessModelFile(GameAssetDatabase db, string path, ModelType type)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) return;
            ProcessModelAsset(db, model, path, type);
        }
        
        private static void ProcessModelAsset(GameAssetDatabase db, GameObject model, string path, ModelType type)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            
            switch (type)
            {
                case ModelType.Building:
                    var buildingEntry = new BuildingModelEntry
                    {
                        Id = fileName,
                        Name = CleanName(fileName),
                        Model = model,
                        Category = GuessBuildingCategory(fileName)
                    };
                    db.BuildingModels.Add(buildingEntry);
                    Debug.Log($"[AssetDB] Added building: {buildingEntry.Name}");
                    break;

                case ModelType.Tower:
                    var towerEntry = new TowerModelEntry
                    {
                        Id = fileName,
                        Name = CleanName(fileName),
                        Model = model,
                        Type = GuessTowerType(fileName)
                    };
                    db.TowerModels.Add(towerEntry);
                    Debug.Log($"[AssetDB] Added tower: {towerEntry.Name}");
                    break;

                case ModelType.Wall:
                    var wallEntry = new WallModelEntry
                    {
                        Id = fileName,
                        Name = CleanName(fileName),
                        Model = model,
                        Type = GuessWallType(fileName),
                        WallMaterial = WallMaterial.Stone
                    };
                    db.WallModels.Add(wallEntry);
                    Debug.Log($"[AssetDB] Added wall: {wallEntry.Name}");
                    break;

                case ModelType.Foundation:
                    var foundationEntry = new ModelEntry
                    {
                        Id = fileName,
                        Name = CleanName(fileName),
                        Model = model
                    };
                    db.FoundationModels.Add(foundationEntry);
                    break;

                case ModelType.Roof:
                    var roofEntry = new ModelEntry
                    {
                        Id = fileName,
                        Name = CleanName(fileName),
                        Model = model
                    };
                    db.RoofModels.Add(roofEntry);
                    break;
            }
        }

        private static string CleanName(string fileName)
        {
            // Remove prefix like "B01_" and convert underscores to spaces
            string name = fileName;
            if (name.Length > 4 && name[3] == '_')
                name = name.Substring(4);
            return name.Replace("_", " ");
        }

        private static BuildingCategory GuessBuildingCategory(string name)
        {
            name = name.ToLower();
            if (name.Contains("mine") || name.Contains("quarry")) return BuildingCategory.Resource;
            if (name.Contains("farm") || name.Contains("lumber")) return BuildingCategory.Resource;
            if (name.Contains("barracks") || name.Contains("armory")) return BuildingCategory.Military;
            if (name.Contains("blacksmith") || name.Contains("siege")) return BuildingCategory.Military;
            if (name.Contains("stable")) return BuildingCategory.Military;
            if (name.Contains("market") || name.Contains("bank")) return BuildingCategory.Economic;
            if (name.Contains("treasury") || name.Contains("warehouse")) return BuildingCategory.Economic;
            if (name.Contains("magic") || name.Contains("library")) return BuildingCategory.Magic;
            if (name.Contains("alchemist") || name.Contains("portal")) return BuildingCategory.Magic;
            if (name.Contains("tavern") || name.Contains("hospital")) return BuildingCategory.Support;
            if (name.Contains("prison")) return BuildingCategory.Support;
            return BuildingCategory.Support;
        }

        private static TowerType GuessTowerType(string name)
        {
            name = name.ToLower();
            if (name.Contains("archer")) return TowerType.Archer;
            if (name.Contains("mage") || name.Contains("magic")) return TowerType.Mage;
            if (name.Contains("cannon") || name.Contains("siege")) return TowerType.Siege;
            if (name.Contains("watch") || name.Contains("guard")) return TowerType.Guard;
            return TowerType.Guard;
        }

        private static WallType GuessWallType(string name)
        {
            name = name.ToLower();
            if (name.Contains("corner")) return WallType.Corner;
            if (name.Contains("gate")) return WallType.Gate;
            if (name.Contains("window")) return WallType.Window;
            return WallType.Straight;
        }

        [MenuItem("Apex Citadels/Debug/Verify GameAssetDatabase", false, 300)]
        public static void VerifyDatabase()
        {
            // Try to load from Resources (like runtime does)
            var db = UnityEngine.Resources.Load<GameAssetDatabase>("GameAssetDatabase");
            
            if (db == null)
            {
                EditorUtility.DisplayDialog("❌ Database Not Found!",
                    "GameAssetDatabase.asset is NOT in Assets/Resources folder!\n\n" +
                    "Run 'Apex Citadels > Setup > Create Game Asset Database (Auto)' first.",
                    "OK");
                return;
            }

            string report = $"GameAssetDatabase loaded successfully!\n\n" +
                $"Buildings: {db.BuildingModels.Count}\n";
            
            for (int i = 0; i < Mathf.Min(5, db.BuildingModels.Count); i++)
            {
                var b = db.BuildingModels[i];
                report += $"  [{i}] {b.Id}: Model={(b.Model != null ? b.Model.name : "NULL")}\n";
            }
            
            report += $"\nTowers: {db.TowerModels.Count}\n";
            for (int i = 0; i < Mathf.Min(3, db.TowerModels.Count); i++)
            {
                var t = db.TowerModels[i];
                report += $"  [{i}] {t.Id}: Model={(t.Model != null ? t.Model.name : "NULL")}\n";
            }
            
            report += $"\nWalls: {db.WallModels.Count}\n";
            
            EditorUtility.DisplayDialog("✅ Database Verification", report, "OK");
            
            Debug.Log("[AssetDB] Full database contents:");
            foreach (var b in db.BuildingModels)
            {
                Debug.Log($"  Building: {b.Id} | {b.Name} | Model={b.Model?.name ?? "NULL"} | Prefab={b.Prefab?.name ?? "NULL"}");
            }
        }
    }
}
