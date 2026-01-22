// ============================================================================
// APEX CITADELS - SYNTY MATERIAL BATCH UPGRADER
// Forcefully upgrades ALL Synty materials to URP by modifying the shader reference
// ============================================================================
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Batch upgrades all Synty POLYGON materials to URP Lit shader.
    /// This is a brute-force approach that works when the automatic converter fails.
    /// </summary>
    public class SyntyMaterialBatchUpgrader : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<string> _materialPaths = new List<string>();
        private bool _scanned = false;
        private int _upgradedCount = 0;
        
        [MenuItem("Apex Citadels/Tools/Synty Material Batch Upgrader")]
        public static void ShowWindow()
        {
            var window = GetWindow<SyntyMaterialBatchUpgrader>("Synty Batch Upgrader");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Synty Material Batch Upgrader", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool finds ALL Synty POLYGON materials and upgrades them to URP Lit shader.\n\n" +
                "This is a brute-force fix that works when Unity's Render Pipeline Converter fails.\n\n" +
                "⚠️ BACKUP YOUR PROJECT FIRST - This modifies material assets directly!",
                MessageType.Warning);
            
            EditorGUILayout.Space(10);
            
            // Scan button
            if (GUILayout.Button("Scan for Synty Materials", GUILayout.Height(30)))
            {
                ScanForMaterials();
            }
            
            EditorGUILayout.Space(10);
            
            if (_scanned)
            {
                EditorGUILayout.LabelField($"Found {_materialPaths.Count} Synty materials", EditorStyles.boldLabel);
                
                if (_materialPaths.Count > 0)
                {
                    // Show first 20 materials
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
                    int shown = 0;
                    foreach (var path in _materialPaths)
                    {
                        EditorGUILayout.LabelField(Path.GetFileName(path));
                        shown++;
                        if (shown >= 50)
                        {
                            EditorGUILayout.LabelField($"... and {_materialPaths.Count - 50} more");
                            break;
                        }
                    }
                    EditorGUILayout.EndScrollView();
                    
                    EditorGUILayout.Space(10);
                    
                    // Upgrade button
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("🔧 UPGRADE ALL TO URP LIT", GUILayout.Height(50)))
                    {
                        if (EditorUtility.DisplayDialog("Confirm Upgrade",
                            $"This will modify {_materialPaths.Count} material files.\n\n" +
                            "Make sure you have a backup!\n\nContinue?",
                            "Yes, Upgrade", "Cancel"))
                        {
                            UpgradeAllMaterials();
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    EditorGUILayout.HelpBox("No Synty materials found. They may already be upgraded!", MessageType.Info);
                }
            }
            
            if (_upgradedCount > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox($"✅ Successfully upgraded {_upgradedCount} materials!\n\nPress Play to test.", MessageType.Info);
            }
        }
        
        private void ScanForMaterials()
        {
            _materialPaths.Clear();
            _scanned = true;
            _upgradedCount = 0;
            
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
                    // Check if it uses Standard shader (fileID: 46)
                    string content = File.ReadAllText(matFile);
                    if (content.Contains("m_Shader: {fileID: 46") || 
                        content.Contains("m_Shader: {fileID: 4800000") ||
                        content.Contains("guid: 0000000000000000f000000000000000"))
                    {
                        _materialPaths.Add(matFile);
                    }
                }
            }
            
            Debug.Log($"[SyntyBatchUpgrader] Found {_materialPaths.Count} materials using Standard shader");
        }
        
        private void UpgradeAllMaterials()
        {
            _upgradedCount = 0;
            
            // Get URP Lit shader GUID
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find URP Lit shader!", "OK");
                return;
            }
            
            string urpLitPath = AssetDatabase.GetAssetPath(urpLit);
            string urpLitGuid = AssetDatabase.AssetPathToGUID(urpLitPath);
            
            // If built-in shader, use known GUID
            if (string.IsNullOrEmpty(urpLitGuid))
            {
                // URP Lit shader GUID (this is the standard one in URP package)
                urpLitGuid = "933532a4fcc9baf4fa0491de14d08ed7";
            }
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                for (int i = 0; i < _materialPaths.Count; i++)
                {
                    string matPath = _materialPaths[i];
                    EditorUtility.DisplayProgressBar("Upgrading Materials", 
                        $"Processing {Path.GetFileName(matPath)}...", 
                        (float)i / _materialPaths.Count);
                    
                    try
                    {
                        // Load material
                        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if (mat == null) continue;
                        
                        // Store original properties
                        Texture mainTex = null;
                        Color color = Color.white;
                        Texture normalMap = null;
                        Texture metallicMap = null;
                        float metallic = 0f;
                        float smoothness = 0.5f;
                        
                        if (mat.HasProperty("_MainTex")) mainTex = mat.GetTexture("_MainTex");
                        if (mat.HasProperty("_Color")) color = mat.GetColor("_Color");
                        if (mat.HasProperty("_BumpMap")) normalMap = mat.GetTexture("_BumpMap");
                        if (mat.HasProperty("_MetallicGlossMap")) metallicMap = mat.GetTexture("_MetallicGlossMap");
                        if (mat.HasProperty("_Metallic")) metallic = mat.GetFloat("_Metallic");
                        if (mat.HasProperty("_Glossiness")) smoothness = mat.GetFloat("_Glossiness");
                        
                        // Get texture scale/offset
                        Vector2 scale = Vector2.one;
                        Vector2 offset = Vector2.zero;
                        if (mat.HasProperty("_MainTex"))
                        {
                            scale = mat.GetTextureScale("_MainTex");
                            offset = mat.GetTextureOffset("_MainTex");
                        }
                        
                        // Change shader to URP Lit
                        mat.shader = urpLit;
                        
                        // Apply properties to URP material
                        if (mainTex != null)
                        {
                            mat.SetTexture("_BaseMap", mainTex);
                            mat.SetTextureScale("_BaseMap", scale);
                            mat.SetTextureOffset("_BaseMap", offset);
                        }
                        mat.SetColor("_BaseColor", color);
                        mat.SetFloat("_Metallic", metallic);
                        mat.SetFloat("_Smoothness", smoothness);
                        
                        if (normalMap != null)
                        {
                            mat.SetTexture("_BumpMap", normalMap);
                            mat.EnableKeyword("_NORMALMAP");
                        }
                        
                        if (metallicMap != null)
                        {
                            mat.SetTexture("_MetallicGlossMap", metallicMap);
                            mat.EnableKeyword("_METALLICGLOSSMAP");
                        }
                        
                        EditorUtility.SetDirty(mat);
                        _upgradedCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[SyntyBatchUpgrader] Failed to upgrade {matPath}: {e.Message}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
            }
            
            Debug.Log($"[SyntyBatchUpgrader] ✅ Upgraded {_upgradedCount} materials to URP Lit!");
            
            // Rescan to show remaining
            ScanForMaterials();
        }
    }
}
