// ============================================================================
// APEX CITADELS - URP MATERIAL UPGRADE TOOL
// Editor tool to batch convert Standard shader materials to URP
// ============================================================================
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Editor tool to upgrade materials from Standard to URP Lit shader.
    /// Fixes the pink/magenta shader issue permanently in prefabs.
    /// </summary>
    public class URPMaterialUpgradeTool : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<Material> _materialsToFix = new List<Material>();
        private bool _scanned = false;
        private bool _includePackages = false;
        private string _searchPath = "Assets";
        
        [MenuItem("Apex Citadels/Tools/URP Material Upgrade Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<URPMaterialUpgradeTool>("URP Material Upgrade");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("URP Material Upgrade Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool finds materials using Standard/Built-in shaders and upgrades them to URP Lit. " +
                "This permanently fixes the pink/magenta shader issue.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Search options
            EditorGUILayout.LabelField("Search Options", EditorStyles.boldLabel);
            _searchPath = EditorGUILayout.TextField("Search Path", _searchPath);
            _includePackages = EditorGUILayout.Toggle("Include Packages", _includePackages);
            
            EditorGUILayout.Space(10);
            
            // Scan button
            if (GUILayout.Button("Scan for Materials to Upgrade", GUILayout.Height(30)))
            {
                ScanForMaterials();
            }
            
            EditorGUILayout.Space(10);
            
            if (_scanned)
            {
                EditorGUILayout.LabelField($"Found {_materialsToFix.Count} materials to upgrade", EditorStyles.boldLabel);
                
                if (_materialsToFix.Count > 0)
                {
                    // Scroll view for materials
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(250));
                    foreach (var mat in _materialsToFix)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(mat, typeof(Material), false);
                        EditorGUILayout.LabelField(mat.shader.name, GUILayout.Width(200));
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                    
                    EditorGUILayout.Space(10);
                    
                    // Upgrade buttons
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Upgrade All Materials to URP Lit", GUILayout.Height(40)))
                    {
                        UpgradeAllMaterials();
                    }
                    GUI.backgroundColor = Color.white;
                    
                    EditorGUILayout.Space(5);
                    
                    if (GUILayout.Button("Upgrade Selected Materials Only"))
                    {
                        UpgradeSelectedMaterials();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No materials need upgrading. All materials are already URP compatible!", MessageType.Info);
                }
            }
            
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Alternative: Use Unity's Built-in Upgrader", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Unity provides a built-in render pipeline converter:\n" +
                "Window > Rendering > Render Pipeline Converter\n\n" +
                "Select 'Built-in to URP' and convert materials.",
                MessageType.Info);
            
            if (GUILayout.Button("Open Render Pipeline Converter"))
            {
                EditorApplication.ExecuteMenuItem("Window/Rendering/Render Pipeline Converter");
            }
        }
        
        private void ScanForMaterials()
        {
            _materialsToFix.Clear();
            _scanned = true;
            
            string[] searchFolders = _includePackages 
                ? new[] { _searchPath, "Packages" } 
                : new[] { _searchPath };
            
            string[] guids = AssetDatabase.FindAssets("t:Material", searchFolders);
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (mat != null && NeedsUpgrade(mat))
                {
                    _materialsToFix.Add(mat);
                }
            }
            
            Debug.Log($"[URPMaterialUpgrade] Found {_materialsToFix.Count} materials that need upgrading");
        }
        
        private bool NeedsUpgrade(Material mat)
        {
            if (mat == null || mat.shader == null) return false;
            
            string shaderName = mat.shader.name;
            
            // Already URP or other compatible shaders
            if (shaderName.Contains("Universal Render Pipeline") ||
                shaderName.Contains("URP") ||
                shaderName.Contains("Sprites/") ||
                shaderName.Contains("UI/") ||
                shaderName.Contains("TextMeshPro/") ||
                shaderName.Contains("Hidden/"))
            {
                return false;
            }
            
            // Shaders that need upgrading
            if (shaderName == "Standard" ||
                shaderName == "Standard (Specular setup)" ||
                shaderName.StartsWith("Legacy Shaders/") ||
                shaderName.StartsWith("Mobile/") ||
                shaderName.StartsWith("Nature/") ||
                shaderName.StartsWith("Particles/") ||
                shaderName.Contains("Synty") ||
                shaderName.Contains("POLYGON"))
            {
                return true;
            }
            
            // Check for broken shaders
            if (shaderName.Contains("Error") || shaderName.Contains("InternalErrorShader"))
            {
                return true;
            }
            
            return false;
        }
        
        private void UpgradeAllMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find URP Lit shader. Make sure URP is installed.", "OK");
                return;
            }
            
            int upgraded = 0;
            int failed = 0;
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                for (int i = 0; i < _materialsToFix.Count; i++)
                {
                    Material mat = _materialsToFix[i];
                    EditorUtility.DisplayProgressBar("Upgrading Materials", $"Processing {mat.name}...", (float)i / _materialsToFix.Count);
                    
                    try
                    {
                        UpgradeMaterial(mat, urpLit);
                        upgraded++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[URPMaterialUpgrade] Failed to upgrade {mat.name}: {e.Message}");
                        failed++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();
            }
            
            EditorUtility.DisplayDialog("Upgrade Complete", 
                $"Upgraded: {upgraded}\nFailed: {failed}\n\nPlease save your project.", "OK");
            
            // Rescan
            ScanForMaterials();
        }
        
        private void UpgradeSelectedMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find URP Lit shader.", "OK");
                return;
            }
            
            var selected = Selection.objects;
            int upgraded = 0;
            
            foreach (var obj in selected)
            {
                if (obj is Material mat && _materialsToFix.Contains(mat))
                {
                    UpgradeMaterial(mat, urpLit);
                    upgraded++;
                }
            }
            
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Upgrade Complete", $"Upgraded {upgraded} selected materials.", "OK");
            ScanForMaterials();
        }
        
        private void UpgradeMaterial(Material mat, Shader urpLit)
        {
            // Save original properties
            Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
            float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 
                              (mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.5f);
            Color emission = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
            Texture emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
            
            // Change shader
            mat.shader = urpLit;
            
            // Apply properties to URP material
            if (mainTex != null)
            {
                mat.SetTexture("_BaseMap", mainTex);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            
            if (bumpMap != null)
            {
                mat.SetTexture("_BumpMap", bumpMap);
                mat.EnableKeyword("_NORMALMAP");
            }
            
            if (emission != Color.black || emissionMap != null)
            {
                mat.SetColor("_EmissionColor", emission);
                if (emissionMap != null) mat.SetTexture("_EmissionMap", emissionMap);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            
            EditorUtility.SetDirty(mat);
            Debug.Log($"[URPMaterialUpgrade] Upgraded: {mat.name}");
        }
    }
}
