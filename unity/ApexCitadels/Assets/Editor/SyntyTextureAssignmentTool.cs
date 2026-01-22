// ============================================================================
// APEX CITADELS - SYNTY TEXTURE ASSIGNMENT TOOL
// Properly assigns Synty texture atlases to upgraded URP materials
// ============================================================================
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Assigns the correct texture atlases to Synty materials that lost them during URP conversion
    /// </summary>
    public class SyntyTextureAssignmentTool : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _fixedCount = 0;
        private bool _scanned = false;
        private List<MaterialTextureMapping> _mappings = new List<MaterialTextureMapping>();
        
        private class MaterialTextureMapping
        {
            public Material material;
            public string materialPath;
            public Texture2D foundTexture;
            public string texturePath;
            public bool hasExistingTexture;
        }
        
        // Mapping of material name patterns to texture file patterns
        // Synty convention: Mat_XX_Y maps to texture PackName_XX_Y.png
        private static readonly Dictionary<string, string[]> SyntyTexturePatterns = new Dictionary<string, string[]>
        {
            // PolygonFantasyKingdom
            { "PolygonFantasyKingdom_Mat_01_A", new[] { "PolygonFantasyKingdom_01_A", "PolygonFantasyKingdom_Texture_01_A" } },
            { "PolygonFantasyKingdom_Mat_01_B", new[] { "PolygonFantasyKingdom_01_B", "PolygonFantasyKingdom_Texture_01_B" } },
            { "PolygonFantasyKingdom_Mat_01_C", new[] { "PolygonFantasyKingdom_01_C", "PolygonFantasyKingdom_Texture_01_C" } },
            { "PolygonFantasyKingdom_Mat_02_A", new[] { "PolygonFantasyKingdom_02_A", "PolygonFantasyKingdom_Texture_02_A" } },
            { "PolygonFantasyKingdom_Mat_02_B", new[] { "PolygonFantasyKingdom_02_B", "PolygonFantasyKingdom_Texture_02_B" } },
            { "PolygonFantasyKingdom_Mat_02_C", new[] { "PolygonFantasyKingdom_02_C", "PolygonFantasyKingdom_Texture_02_C" } },
            { "PolygonFantasyKingdom_Mat_03_A", new[] { "PolygonFantasyKingdom_03_A", "PolygonFantasyKingdom_Texture_03_A" } },
            { "PolygonFantasyKingdom_Mat_03_B", new[] { "PolygonFantasyKingdom_03_B", "PolygonFantasyKingdom_Texture_03_B" } },
            { "PolygonFantasyKingdom_Mat_03_C", new[] { "PolygonFantasyKingdom_03_C", "PolygonFantasyKingdom_Texture_03_C" } },
            { "PolygonFantasyKingdom_Mat_04_A", new[] { "PolygonFantasyKingdom_04_A", "PolygonFantasyKingdom_Texture_04_A" } },
            { "PolygonFantasyKingdom_Mat_04_B", new[] { "PolygonFantasyKingdom_04_B", "PolygonFantasyKingdom_Texture_04_B" } },
            { "PolygonFantasyKingdom_Mat_04_C", new[] { "PolygonFantasyKingdom_04_C", "PolygonFantasyKingdom_Texture_04_C" } },
            
            // PolygonTown
            { "PolygonTown_Material_01_A", new[] { "PolygonTown_Texture_01_A", "PolygonTown_01_A" } },
            { "PolygonTown_Material_Pavement_01", new[] { "PolygonTown_Texture_Pavement_01" } },
            
            // Generic patterns for other packs
            { "PolygonAdventure_Mat_01_A", new[] { "PolygonAdventure_01_A", "Polygon_Adventure_Texture_01_A" } },
            { "PolygonDungeon_Mat_01_A", new[] { "PolygonDungeon_01_A", "PolygonDungeon_Texture_01_A" } },
            { "PolygonFarm_Mat_01_A", new[] { "PolygonFarm_01_A", "PolygonFarm_Texture_01_A" } },
            { "PolygonKnights_Mat_01_A", new[] { "PolygonKnights_01_A", "PolygonKnights_Texture_01_A" } },
            { "PolygonNature_Mat_01_A", new[] { "PolygonNature_01_A", "PolygonNature_Texture_01_A" } },
            { "PolygonPirates_Mat_01_A", new[] { "PolygonPirates_01_A", "PolygonPirates_Texture_01_A" } },
            { "PolygonSamurai_Mat_01_A", new[] { "PolygonSamurai_01_A", "PolygonSamurai_Texture_01_A" } },
            { "PolygonVikings_Mat_01_A", new[] { "PolygonVikings_01_A", "PolygonVikings_Texture_01_A" } },
        };
        
        [MenuItem("Apex Citadels/Tools/Synty Texture Assignment Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<SyntyTextureAssignmentTool>("Synty Texture Fixer");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Synty Texture Assignment Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool finds Synty materials that are missing textures after URP conversion\n" +
                "and assigns the correct texture atlases based on Synty's naming conventions.\n\n" +
                "Material: PolygonFantasyKingdom_Mat_01_A → Texture: PolygonFantasyKingdom_01_A.png",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("🔍 Scan for Materials Missing Textures", GUILayout.Height(30)))
            {
                ScanForMaterialsNeedingTextures();
            }
            
            EditorGUILayout.Space(10);
            
            if (_scanned)
            {
                int needsTexture = _mappings.Count(m => m.foundTexture != null && !m.hasExistingTexture);
                int alreadyHas = _mappings.Count(m => m.hasExistingTexture);
                int cantFind = _mappings.Count(m => m.foundTexture == null && !m.hasExistingTexture);
                
                EditorGUILayout.LabelField($"Materials needing texture: {needsTexture}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Already have texture: {alreadyHas}");
                EditorGUILayout.LabelField($"Cannot find texture: {cantFind}");
                
                if (needsTexture > 0)
                {
                    EditorGUILayout.Space(5);
                    
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(250));
                    foreach (var mapping in _mappings.Where(m => m.foundTexture != null && !m.hasExistingTexture).Take(50))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(mapping.material.name, GUILayout.Width(250));
                        EditorGUILayout.LabelField("→", GUILayout.Width(20));
                        EditorGUILayout.LabelField(mapping.foundTexture.name, GUILayout.Width(250));
                        EditorGUILayout.EndHorizontal();
                    }
                    if (needsTexture > 50)
                    {
                        EditorGUILayout.LabelField($"... and {needsTexture - 50} more");
                    }
                    EditorGUILayout.EndScrollView();
                    
                    EditorGUILayout.Space(10);
                    
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button($"✅ ASSIGN TEXTURES TO {needsTexture} MATERIALS", GUILayout.Height(50)))
                    {
                        AssignTextures();
                    }
                    GUI.backgroundColor = Color.white;
                }
                
                if (cantFind > 0)
                {
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("Materials with no matching texture found:", EditorStyles.boldLabel);
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(100));
                    foreach (var mapping in _mappings.Where(m => m.foundTexture == null && !m.hasExistingTexture).Take(20))
                    {
                        EditorGUILayout.LabelField($"  {mapping.material.name}");
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            
            if (_fixedCount > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox($"✅ Assigned textures to {_fixedCount} materials!\n\nPress Play to test.", MessageType.Info);
            }
            
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("📊 List All Synty Textures Found"))
            {
                ListAllSyntyTextures();
            }
        }
        
        private void ScanForMaterialsNeedingTextures()
        {
            _mappings.Clear();
            _scanned = true;
            _fixedCount = 0;
            
            // Build texture lookup dictionary
            var texturesByName = new Dictionary<string, Texture2D>();
            var allTextures = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Synty", "Assets/PolygonTown", "Assets/PolygonFantasyKingdom" });
            
            foreach (var guid in allTextures)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    string baseName = Path.GetFileNameWithoutExtension(path);
                    if (!texturesByName.ContainsKey(baseName))
                    {
                        texturesByName[baseName] = tex;
                    }
                }
            }
            
            Debug.Log($"[SyntyTextureTool] Found {texturesByName.Count} textures in Synty folders");
            
            // Find all materials in Synty folders
            string[] searchPaths = new[] {
                "Assets/Synty",
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
                "Assets/PolygonVikings"
            };
            
            var validPaths = searchPaths.Where(p => Directory.Exists(p)).ToArray();
            var allMats = AssetDatabase.FindAssets("t:Material", validPaths);
            
            foreach (var guid in allMats)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;
                
                // Check if it's URP Lit
                if (mat.shader == null || !mat.shader.name.Contains("Universal Render Pipeline/Lit"))
                    continue;
                
                // Check if it already has a texture
                Texture existingTex = null;
                if (mat.HasProperty("_BaseMap"))
                    existingTex = mat.GetTexture("_BaseMap");
                
                var mapping = new MaterialTextureMapping
                {
                    material = mat,
                    materialPath = path,
                    hasExistingTexture = existingTex != null
                };
                
                // Try to find matching texture
                if (existingTex == null)
                {
                    mapping.foundTexture = FindTextureForMaterial(mat.name, texturesByName);
                    if (mapping.foundTexture != null)
                    {
                        mapping.texturePath = AssetDatabase.GetAssetPath(mapping.foundTexture);
                    }
                }
                
                _mappings.Add(mapping);
            }
            
            Debug.Log($"[SyntyTextureTool] Scanned {_mappings.Count} URP materials");
        }
        
        private Texture2D FindTextureForMaterial(string materialName, Dictionary<string, Texture2D> textures)
        {
            // First try exact mapping
            if (SyntyTexturePatterns.TryGetValue(materialName, out var patterns))
            {
                foreach (var pattern in patterns)
                {
                    if (textures.TryGetValue(pattern, out var tex))
                        return tex;
                }
            }
            
            // Try to derive texture name from material name
            // Pattern: PackName_Mat_XX_Y -> PackName_XX_Y
            string derivedName = materialName
                .Replace("_Mat_", "_")
                .Replace("_Material_", "_");
            
            if (textures.TryGetValue(derivedName, out var derivedTex))
                return derivedTex;
            
            // Try with "Texture" in name
            string texturedName = materialName.Replace("_Mat_", "_Texture_");
            if (textures.TryGetValue(texturedName, out var texturedTex))
                return texturedTex;
            
            // Try fuzzy match - find texture with similar base name
            string baseName = materialName.Split('_')[0]; // e.g., "PolygonFantasyKingdom"
            foreach (var kvp in textures)
            {
                if (kvp.Key.StartsWith(baseName) && !kvp.Key.Contains("Normal") && !kvp.Key.Contains("Emission"))
                {
                    // Check if the pattern matches (e.g., _01_A)
                    string matSuffix = ExtractSuffix(materialName);
                    string texSuffix = ExtractSuffix(kvp.Key);
                    if (matSuffix == texSuffix)
                        return kvp.Value;
                }
            }
            
            return null;
        }
        
        private string ExtractSuffix(string name)
        {
            // Extract pattern like _01_A, _02_B etc
            var parts = name.Split('_');
            if (parts.Length >= 2)
            {
                string last = parts[parts.Length - 1];
                string secondLast = parts[parts.Length - 2];
                
                // Check if it's a number_letter pattern
                if (int.TryParse(secondLast, out _) && last.Length == 1)
                {
                    return $"_{secondLast}_{last}";
                }
            }
            return "";
        }
        
        private void AssignTextures()
        {
            _fixedCount = 0;
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                foreach (var mapping in _mappings)
                {
                    if (mapping.hasExistingTexture || mapping.foundTexture == null)
                        continue;
                    
                    var mat = mapping.material;
                    
                    // Set the texture
                    mat.SetTexture("_BaseMap", mapping.foundTexture);
                    
                    // Set reasonable defaults if not set
                    if (mat.HasProperty("_BaseColor"))
                    {
                        var color = mat.GetColor("_BaseColor");
                        if (color == Color.black)
                        {
                            mat.SetColor("_BaseColor", Color.white);
                        }
                    }
                    
                    EditorUtility.SetDirty(mat);
                    _fixedCount++;
                    
                    Debug.Log($"[SyntyTextureTool] Assigned {mapping.foundTexture.name} to {mat.name}");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            
            Debug.Log($"[SyntyTextureTool] ✅ Assigned textures to {_fixedCount} materials!");
            
            // Rescan
            ScanForMaterialsNeedingTextures();
        }
        
        private void ListAllSyntyTextures()
        {
            Debug.Log("[SyntyTextureTool] ========================================");
            Debug.Log("[SyntyTextureTool] ALL SYNTY TEXTURES FOUND");
            Debug.Log("[SyntyTextureTool] ========================================");
            
            var allTextures = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Synty" });
            var byFolder = new Dictionary<string, List<string>>();
            
            foreach (var guid in allTextures)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var folder = Path.GetDirectoryName(path);
                var name = Path.GetFileNameWithoutExtension(path);
                
                if (!byFolder.ContainsKey(folder))
                    byFolder[folder] = new List<string>();
                byFolder[folder].Add(name);
            }
            
            foreach (var kvp in byFolder.OrderBy(k => k.Key))
            {
                Debug.Log($"\n[SyntyTextureTool] {kvp.Key}:");
                foreach (var name in kvp.Value.OrderBy(n => n).Take(20))
                {
                    Debug.Log($"  - {name}");
                }
                if (kvp.Value.Count > 20)
                {
                    Debug.Log($"  ... and {kvp.Value.Count - 20} more");
                }
            }
            
            Debug.Log("[SyntyTextureTool] ========================================");
        }
    }
}
