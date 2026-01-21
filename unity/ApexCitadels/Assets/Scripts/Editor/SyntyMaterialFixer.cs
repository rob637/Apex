// ============================================================================
// APEX CITADELS - SYNTY MATERIAL FIXER
// Helps fix pink/magenta materials from Synty packs by upgrading to URP
// ============================================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Collections.Generic;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Editor utility to fix Synty pack materials for URP
    /// Pink/magenta materials occur when shaders are built-in but project uses URP
    /// </summary>
    public class SyntyMaterialFixer : EditorWindow
    {
        private string searchPath = "Assets/Synty";
        private bool includeSubfolders = true;
        private int materialsFound = 0;
        private int materialsFixed = 0;
        private Vector2 scrollPos;
        private List<string> log = new List<string>();
        
        [MenuItem("Tools/Apex Citadels/Fix Synty Materials (URP)")]
        public static void ShowWindow()
        {
            GetWindow<SyntyMaterialFixer>("Synty Material Fixer");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Synty Material Fixer for URP", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool fixes pink/magenta materials in Synty packs by:\n" +
                "1. Finding materials using legacy shaders\n" +
                "2. Upgrading them to Universal Render Pipeline/Lit\n\n" +
                "RECOMMENDED: First import the Synty pack's own URP support package if available.",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            searchPath = EditorGUILayout.TextField("Search Path", searchPath);
            includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Scan Materials", GUILayout.Height(30)))
            {
                ScanMaterials();
            }
            
            EditorGUI.BeginDisabledGroup(materialsFound == 0);
            if (GUILayout.Button("Fix All Materials", GUILayout.Height(30)))
            {
                FixAllMaterials();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField($"Materials Found: {materialsFound}");
            EditorGUILayout.LabelField($"Materials Fixed: {materialsFixed}");
            
            EditorGUILayout.Space();
            
            // Log output
            GUILayout.Label("Log:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            foreach (var line in log)
            {
                EditorGUILayout.LabelField(line);
            }
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            
            // Additional tips
            EditorGUILayout.HelpBox(
                "TIP: If Synty packs have a 'URP' or 'UniversalRenderPipeline' folder, " +
                "you can import those packages manually via Assets > Import Package for best results.",
                MessageType.Warning);
        }
        
        private void ScanMaterials()
        {
            log.Clear();
            materialsFound = 0;
            materialsFixed = 0;
            
            if (!Directory.Exists(searchPath))
            {
                log.Add($"ERROR: Path does not exist: {searchPath}");
                return;
            }
            
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { searchPath });
            log.Add($"Found {matGuids.Length} total materials in {searchPath}");
            
            int legacyCount = 0;
            
            foreach (var guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (mat != null && mat.shader != null)
                {
                    string shaderName = mat.shader.name;
                    
                    // Check for legacy/broken shaders
                    if (shaderName.Contains("Hidden/InternalErrorShader") ||
                        shaderName == "Standard" ||
                        shaderName.StartsWith("Legacy Shaders/") ||
                        shaderName == "Standard (Specular setup)" ||
                        shaderName.Contains("Hidden/") && !shaderName.Contains("URP"))
                    {
                        legacyCount++;
                        if (legacyCount <= 20)
                        {
                            log.Add($"  Legacy: {mat.name} ({shaderName})");
                        }
                    }
                }
            }
            
            if (legacyCount > 20)
            {
                log.Add($"  ... and {legacyCount - 20} more");
            }
            
            materialsFound = legacyCount;
            log.Add($"Found {legacyCount} materials needing upgrade");
        }
        
        private void FixAllMaterials()
        {
            if (!Directory.Exists(searchPath))
            {
                log.Add($"ERROR: Path does not exist: {searchPath}");
                return;
            }
            
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader urpSimpleLit = Shader.Find("Universal Render Pipeline/Simple Lit");
            
            if (urpLit == null)
            {
                log.Add("ERROR: URP Lit shader not found. Is URP installed?");
                return;
            }
            
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { searchPath });
            materialsFixed = 0;
            
            foreach (var guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (mat != null && mat.shader != null)
                {
                    string shaderName = mat.shader.name;
                    
                    // Check for legacy/broken shaders
                    if (shaderName.Contains("Hidden/InternalErrorShader") ||
                        shaderName == "Standard" ||
                        shaderName.StartsWith("Legacy Shaders/") ||
                        shaderName == "Standard (Specular setup)" ||
                        shaderName.Contains("Hidden/") && !shaderName.Contains("URP"))
                    {
                        // Store texture references before changing shader
                        Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                        Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                        Texture normalMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                        float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                        float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
                        
                        // Change to URP shader
                        mat.shader = urpLit;
                        
                        // Restore properties with URP property names
                        if (mainTex != null)
                            mat.SetTexture("_BaseMap", mainTex);
                        mat.SetColor("_BaseColor", color);
                        if (normalMap != null)
                            mat.SetTexture("_BumpMap", normalMap);
                        mat.SetFloat("_Metallic", metallic);
                        mat.SetFloat("_Smoothness", smoothness);
                        
                        EditorUtility.SetDirty(mat);
                        materialsFixed++;
                    }
                }
            }
            
            AssetDatabase.SaveAssets();
            log.Add($"Fixed {materialsFixed} materials to use URP shaders");
            log.Add("You may need to re-import textures if colors look wrong.");
        }
    }
}
#endif
