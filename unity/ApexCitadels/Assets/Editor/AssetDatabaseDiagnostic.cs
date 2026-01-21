using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class AssetDatabaseDiagnostic : EditorWindow
{
    private Vector2 scrollPos;
    private string diagnosticOutput = "";

    [MenuItem("Tools/Asset Database Diagnostic")]
    public static void ShowWindow()
    {
        GetWindow<AssetDatabaseDiagnostic>("Asset Diagnostic");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Run Full Diagnostic", GUILayout.Height(40)))
        {
            RunDiagnostic();
        }

        if (GUILayout.Button("Force Reimport All Synty Packs", GUILayout.Height(30)))
        {
            ForceReimportSyntyPacks();
        }

        if (GUILayout.Button("Refresh Asset Database", GUILayout.Height(30)))
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log("Asset Database Refreshed with ForceUpdate");
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Diagnostic Results:", EditorStyles.boldLabel);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.TextArea(diagnosticOutput, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    void RunDiagnostic()
    {
        diagnosticOutput = "=== ASSET DATABASE DIAGNOSTIC ===\n\n";

        // Count all assets
        string[] allAssets = AssetDatabase.GetAllAssetPaths();
        diagnosticOutput += $"Total Assets in Database: {allAssets.Length}\n\n";

        // Count by folder
        diagnosticOutput += "=== POLYGON PACK COUNTS ===\n";
        string[] polygonFolders = new string[]
        {
            "Assets/PolygonAdventure",
            "Assets/PolygonDungeon",
            "Assets/PolygonFantasyCharacters",
            "Assets/PolygonFantasyHeroCharacters",
            "Assets/PolygonFantasyRivals",
            "Assets/PolygonFarm",
            "Assets/PolygonIcons",
            "Assets/PolygonKnights",
            "Assets/PolygonNature",
            "Assets/PolygonParticleFX",
            "Assets/PolygonPirates",
            "Assets/PolygonSamurai",
            "Assets/PolygonTown",
            "Assets/PolygonVikings",
            "Assets/Synty/PolygonFantasyKingdom",
            "Assets/Synty/PolygonGeneric"
        };

        int totalPolygon = 0;
        foreach (string folder in polygonFolders)
        {
            string[] guids = AssetDatabase.FindAssets("", new[] { folder });
            int count = guids.Length;
            totalPolygon += count;
            
            // Check if folder exists
            bool exists = AssetDatabase.IsValidFolder(folder);
            string status = exists ? "✓" : "✗ MISSING";
            
            diagnosticOutput += $"{status} {folder}: {count} assets\n";
        }
        diagnosticOutput += $"\nTotal Polygon Assets: {totalPolygon}\n\n";

        // Count by type
        diagnosticOutput += "=== ASSET TYPE COUNTS ===\n";
        diagnosticOutput += $"Prefabs: {AssetDatabase.FindAssets("t:Prefab").Length}\n";
        diagnosticOutput += $"Materials: {AssetDatabase.FindAssets("t:Material").Length}\n";
        diagnosticOutput += $"Textures: {AssetDatabase.FindAssets("t:Texture").Length}\n";
        diagnosticOutput += $"Models (Mesh): {AssetDatabase.FindAssets("t:Mesh").Length}\n";
        diagnosticOutput += $"Scripts: {AssetDatabase.FindAssets("t:Script").Length}\n";
        diagnosticOutput += $"Scenes: {AssetDatabase.FindAssets("t:Scene").Length}\n";
        diagnosticOutput += $"AudioClips: {AssetDatabase.FindAssets("t:AudioClip").Length}\n\n";

        // Check for import errors
        diagnosticOutput += "=== CHECKING FOR ISSUES ===\n";
        
        // Check if meta files match
        int missingMeta = 0;
        string assetsPath = Application.dataPath;
        string[] allFiles = Directory.GetFiles(assetsPath, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".meta") && !f.Contains("/.") && !f.Contains("\\."))
            .ToArray();

        foreach (string file in allFiles.Take(100)) // Check first 100
        {
            if (!File.Exists(file + ".meta"))
            {
                missingMeta++;
                if (missingMeta <= 10)
                    diagnosticOutput += $"Missing meta: {file}\n";
            }
        }
        if (missingMeta > 10)
            diagnosticOutput += $"... and {missingMeta - 10} more missing meta files\n";

        diagnosticOutput += $"\nFiles checked: {allFiles.Length}\n";
        diagnosticOutput += $"Missing .meta files found: {missingMeta}\n";

        Debug.Log(diagnosticOutput);
    }

    void ForceReimportSyntyPacks()
    {
        string[] folders = new string[]
        {
            "Assets/PolygonAdventure",
            "Assets/PolygonDungeon",
            "Assets/PolygonFantasyCharacters",
            "Assets/PolygonFantasyHeroCharacters",
            "Assets/PolygonFantasyRivals",
            "Assets/PolygonFarm",
            "Assets/PolygonIcons",
            "Assets/PolygonKnights",
            "Assets/PolygonNature",
            "Assets/PolygonParticleFX",
            "Assets/PolygonPirates",
            "Assets/PolygonSamurai",
            "Assets/PolygonTown",
            "Assets/PolygonVikings",
            "Assets/Synty"
        };

        foreach (string folder in folders)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                Debug.Log($"Reimporting: {folder}");
                AssetDatabase.ImportAsset(folder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Force reimport complete!");
    }
}
