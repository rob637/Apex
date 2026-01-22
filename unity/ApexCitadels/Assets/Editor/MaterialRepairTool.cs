// ============================================================================
// APEX CITADELS - MATERIAL REPAIR TOOL
// Finds materials that have URP Lit shader but missing textures and repairs them
// ============================================================================
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Validates and repairs materials that were upgraded to URP but lost textures
    /// </summary>
    public class MaterialRepairTool : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<MaterialIssue> _issues = new List<MaterialIssue>();
        private bool _scanned = false;
        private int _repairedCount = 0;
        
        private class MaterialIssue
        {
            public string path;
            public Material material;
            public string issue;
            public Texture suggestedTexture;
        }
        
        [MenuItem("Apex Citadels/Tools/Material Repair Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialRepairTool>("Material Repair");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Material Repair Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool finds materials that have URP shaders but are missing textures.\n" +
                "It attempts to repair them by finding textures in the same folder or by material name.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("🔍 Scan for Broken Materials", GUILayout.Height(30)))
            {
                ScanForBrokenMaterials();
            }
            
            EditorGUILayout.Space(10);
            
            if (_scanned)
            {
                EditorGUILayout.LabelField($"Found {_issues.Count} materials with issues", EditorStyles.boldLabel);
                
                if (_issues.Count > 0)
                {
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
                    foreach (var issue in _issues.Take(50))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(issue.path), GUILayout.Width(200));
                        EditorGUILayout.LabelField(issue.issue, GUILayout.Width(200));
                        if (issue.suggestedTexture != null)
                        {
                            EditorGUILayout.LabelField($"Found: {issue.suggestedTexture.name}", GUILayout.Width(150));
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    if (_issues.Count > 50)
                    {
                        EditorGUILayout.LabelField($"... and {_issues.Count - 50} more");
                    }
                    EditorGUILayout.EndScrollView();
                    
                    EditorGUILayout.Space(10);
                    
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("🔧 ATTEMPT AUTO-REPAIR", GUILayout.Height(50)))
                    {
                        AutoRepairMaterials();
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    EditorGUILayout.HelpBox("✅ No broken materials found!", MessageType.Info);
                }
            }
            
            if (_repairedCount > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox($"✅ Repaired {_repairedCount} materials!", MessageType.Info);
            }
            
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Debug Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("📊 Log All Synty Material States"))
            {
                LogAllSyntyMaterialStates();
            }
            
            if (GUILayout.Button("📊 Log Materials by Shader Type"))
            {
                LogMaterialsByShaderType();
            }
            
            if (GUILayout.Button("🔄 Force Reimport All Synty Materials"))
            {
                ForceReimportSyntyMaterials();
            }
        }
        
        private void ScanForBrokenMaterials()
        {
            _issues.Clear();
            _scanned = true;
            _repairedCount = 0;
            
            // Find all materials in Polygon folders
            string[] searchPatterns = new[] {
                "Assets/PolygonTown",
                "Assets/PolygonAdventure",
                "Assets/PolygonDungeon",
                "Assets/PolygonFantasyCharacters",
                "Assets/PolygonFantasyHeroCharacters",
                "Assets/PolygonFantasyRivals",
                "Assets/PolygonFarm",
                "Assets/PolygonKnights",
                "Assets/PolygonNature",
                "Assets/PolygonPirates",
                "Assets/PolygonSamurai",
                "Assets/PolygonVikings",
                "Assets/Synty"
            };
            
            foreach (var folder in searchPatterns)
            {
                if (!Directory.Exists(folder)) continue;
                
                var matFiles = Directory.GetFiles(folder, "*.mat", SearchOption.AllDirectories);
                foreach (var matFile in matFiles)
                {
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matFile);
                    if (mat == null) continue;
                    
                    // Check if it's URP Lit
                    if (mat.shader == null) continue;
                    if (!mat.shader.name.Contains("Universal Render Pipeline/Lit")) continue;
                    
                    // Check for texture
                    Texture baseMap = null;
                    if (mat.HasProperty("_BaseMap"))
                    {
                        baseMap = mat.GetTexture("_BaseMap");
                    }
                    
                    // Also check legacy property
                    Texture mainTex = null;
                    if (mat.HasProperty("_MainTex"))
                    {
                        mainTex = mat.GetTexture("_MainTex");
                    }
                    
                    // Check if material should have a texture but doesn't
                    if (baseMap == null)
                    {
                        var issue = new MaterialIssue
                        {
                            path = matFile,
                            material = mat,
                            issue = mainTex != null ? "Has _MainTex but no _BaseMap" : "No texture at all"
                        };
                        
                        // Try to find a texture for this material
                        issue.suggestedTexture = FindTextureForMaterial(mat, matFile);
                        
                        // If we found mainTex, use that
                        if (mainTex != null)
                        {
                            issue.suggestedTexture = mainTex;
                        }
                        
                        _issues.Add(issue);
                    }
                }
            }
            
            Debug.Log($"[MaterialRepair] Found {_issues.Count} materials needing repair");
        }
        
        private Texture FindTextureForMaterial(Material mat, string matPath)
        {
            string matDir = Path.GetDirectoryName(matPath);
            string matName = Path.GetFileNameWithoutExtension(matPath);
            
            // Look for textures in same folder and parent folders
            string[] searchDirs = new[]
            {
                matDir,
                Path.Combine(matDir, ".."),
                Path.Combine(matDir, "..", "Textures"),
                Path.Combine(matDir, "Textures"),
                Path.Combine(matDir, "..", "..", "Textures")
            };
            
            // Generate possible texture names
            List<string> possibleNames = new List<string>
            {
                matName,
                matName + "_Albedo",
                matName + "_BaseColor",
                matName + "_Diffuse",
                matName.Replace("_Material", ""),
                matName.Replace("_Mat", "")
            };
            
            // Handle Synty naming conventions
            if (matName.StartsWith("PolygonTown_"))
                possibleNames.Add("PolygonTown_Texture_01_A");
            if (matName.StartsWith("PolygonAdventure_"))
                possibleNames.Add("Polygon_Adventure_Texture_01_A");
            if (matName.StartsWith("PolygonDungeon_"))
                possibleNames.Add("PolygonDungeon_Texture_01_A");
            if (matName.StartsWith("PolygonFantasy"))
                possibleNames.Add("Polygon_Fantasy_Texture_01_A");
            
            foreach (var dir in searchDirs)
            {
                if (!Directory.Exists(dir)) continue;
                
                var textures = Directory.GetFiles(dir, "*.png", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.GetFiles(dir, "*.tga", SearchOption.TopDirectoryOnly))
                    .Concat(Directory.GetFiles(dir, "*.jpg", SearchOption.TopDirectoryOnly));
                
                foreach (var texPath in textures)
                {
                    string texName = Path.GetFileNameWithoutExtension(texPath);
                    
                    foreach (var possibleName in possibleNames)
                    {
                        if (texName.Contains(possibleName) || possibleName.Contains(texName))
                        {
                            return AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                        }
                    }
                }
            }
            
            return null;
        }
        
        private void AutoRepairMaterials()
        {
            _repairedCount = 0;
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                foreach (var issue in _issues)
                {
                    if (issue.suggestedTexture == null) continue;
                    
                    var mat = issue.material;
                    
                    // Set the texture
                    mat.SetTexture("_BaseMap", issue.suggestedTexture);
                    
                    // Copy scale/offset from _MainTex if available
                    if (mat.HasProperty("_MainTex"))
                    {
                        var scale = mat.GetTextureScale("_MainTex");
                        var offset = mat.GetTextureOffset("_MainTex");
                        mat.SetTextureScale("_BaseMap", scale);
                        mat.SetTextureOffset("_BaseMap", offset);
                    }
                    
                    EditorUtility.SetDirty(mat);
                    _repairedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            
            Debug.Log($"[MaterialRepair] ✅ Repaired {_repairedCount} materials!");
            
            // Rescan
            ScanForBrokenMaterials();
        }
        
        private void LogAllSyntyMaterialStates()
        {
            Debug.Log("[MaterialRepair] ========================================");
            Debug.Log("[MaterialRepair] SYNTY MATERIAL STATE REPORT");
            Debug.Log("[MaterialRepair] ========================================");
            
            string[] searchPatterns = new[] {
                "Assets/PolygonTown",
                "Assets/PolygonAdventure",
                "Assets/PolygonDungeon",
                "Assets/PolygonFantasyCharacters",
                "Assets/PolygonFarm",
                "Assets/PolygonKnights",
                "Assets/PolygonNature",
                "Assets/PolygonPirates",
                "Assets/PolygonSamurai",
                "Assets/PolygonVikings"
            };
            
            foreach (var folder in searchPatterns)
            {
                if (!Directory.Exists(folder)) continue;
                
                Debug.Log($"\n[MaterialRepair] --- {folder} ---");
                
                var matFiles = Directory.GetFiles(folder, "*.mat", SearchOption.AllDirectories);
                int urpWithTexture = 0;
                int urpNoTexture = 0;
                int standard = 0;
                int other = 0;
                
                foreach (var matFile in matFiles)
                {
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matFile);
                    if (mat == null) continue;
                    
                    string shaderName = mat.shader != null ? mat.shader.name : "NULL";
                    bool hasBaseMap = mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null;
                    bool hasMainTex = mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null;
                    
                    if (shaderName.Contains("Universal Render Pipeline/Lit"))
                    {
                        if (hasBaseMap)
                            urpWithTexture++;
                        else
                            urpNoTexture++;
                    }
                    else if (shaderName == "Standard" || shaderName.Contains("Standard"))
                    {
                        standard++;
                    }
                    else
                    {
                        other++;
                    }
                }
                
                Debug.Log($"  URP Lit (with texture): {urpWithTexture}");
                Debug.Log($"  URP Lit (NO texture): {urpNoTexture}");
                Debug.Log($"  Standard shader: {standard}");
                Debug.Log($"  Other shaders: {other}");
            }
            
            Debug.Log("\n[MaterialRepair] ========================================");
        }
        
        private void LogMaterialsByShaderType()
        {
            Debug.Log("[MaterialRepair] ========================================");
            Debug.Log("[MaterialRepair] MATERIALS BY SHADER TYPE");
            Debug.Log("[MaterialRepair] ========================================");
            
            var allMats = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            var shaderCounts = new Dictionary<string, int>();
            
            foreach (var guid in allMats)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null) continue;
                
                string shaderName = mat.shader.name;
                if (!shaderCounts.ContainsKey(shaderName))
                    shaderCounts[shaderName] = 0;
                shaderCounts[shaderName]++;
            }
            
            var sorted = shaderCounts.OrderByDescending(kvp => kvp.Value);
            foreach (var kvp in sorted)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }
            
            Debug.Log("[MaterialRepair] ========================================");
        }
        
        private void ForceReimportSyntyMaterials()
        {
            if (!EditorUtility.DisplayDialog("Confirm Reimport",
                "This will force reimport all Synty materials.\nThis may take a while.\n\nContinue?",
                "Yes", "Cancel"))
            {
                return;
            }
            
            string[] folders = new[] {
                "Assets/PolygonTown",
                "Assets/PolygonAdventure",
                "Assets/PolygonDungeon",
                "Assets/PolygonFantasyCharacters",
                "Assets/PolygonFarm",
                "Assets/PolygonKnights",
                "Assets/PolygonNature",
                "Assets/PolygonPirates",
                "Assets/PolygonSamurai",
                "Assets/PolygonVikings"
            };
            
            int count = 0;
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                
                var matFiles = Directory.GetFiles(folder, "*.mat", SearchOption.AllDirectories);
                foreach (var matFile in matFiles)
                {
                    AssetDatabase.ImportAsset(matFile, ImportAssetOptions.ForceUpdate);
                    count++;
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"[MaterialRepair] Reimported {count} materials");
        }
    }
}
