// ============================================================================
// APEX CITADELS - MATERIAL DEBUG ANALYZER
// Runtime debugging tool to analyze why materials appear white
// ============================================================================
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Debug analyzer that logs detailed information about materials in the scene
    /// Add this to any GameObject or use the static method to analyze
    /// </summary>
    public class MaterialDebugAnalyzer : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool analyzeOnStart = true;
        [SerializeField] private bool analyzeChildren = true;
        [SerializeField] private bool logAllMaterials = false; // If false, only log suspicious ones
        
        private void Start()
        {
            if (analyzeOnStart)
            {
                AnalyzeGameObject(gameObject, analyzeChildren);
            }
        }
        
        /// <summary>
        /// Analyze a GameObject and all its children for material issues
        /// </summary>
        public static void AnalyzeGameObject(GameObject go, bool includeChildren = true)
        {
            if (go == null) return;
            
            var renderers = includeChildren 
                ? go.GetComponentsInChildren<Renderer>(true)
                : go.GetComponents<Renderer>();
                
            Debug.Log($"[MaterialDebug] ========================================");
            Debug.Log($"[MaterialDebug] Analyzing: {go.name}");
            Debug.Log($"[MaterialDebug] Found {renderers.Length} renderers");
            Debug.Log($"[MaterialDebug] ========================================");
            
            int totalMaterials = 0;
            int suspiciousMaterials = 0;
            var materialStats = new Dictionary<string, int>();
            var problemMaterials = new List<string>();
            
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    totalMaterials++;
                    
                    if (mat == null)
                    {
                        problemMaterials.Add($"  NULL material on {renderer.gameObject.name}");
                        suspiciousMaterials++;
                        continue;
                    }
                    
                    string shaderName = mat.shader != null ? mat.shader.name : "NULL SHADER";
                    
                    // Count shader usage
                    if (!materialStats.ContainsKey(shaderName))
                        materialStats[shaderName] = 0;
                    materialStats[shaderName]++;
                    
                    // Check for problems
                    bool hasProblem = false;
                    StringBuilder problem = new StringBuilder();
                    problem.Append($"  [{renderer.gameObject.name}] {mat.name}");
                    problem.Append($" | Shader: {shaderName}");
                    
                    // Check shader
                    if (mat.shader == null)
                    {
                        problem.Append(" | ⚠️ NULL SHADER");
                        hasProblem = true;
                    }
                    else if (!mat.shader.isSupported)
                    {
                        problem.Append(" | ⚠️ SHADER NOT SUPPORTED");
                        hasProblem = true;
                    }
                    else if (shaderName.Contains("Error") || shaderName.Contains("InternalError"))
                    {
                        problem.Append(" | ⚠️ ERROR SHADER");
                        hasProblem = true;
                    }
                    
                    // Check for texture
                    Texture mainTex = null;
                    if (mat.HasProperty("_BaseMap"))
                        mainTex = mat.GetTexture("_BaseMap");
                    else if (mat.HasProperty("_MainTex"))
                        mainTex = mat.GetTexture("_MainTex");
                    
                    if (mainTex == null)
                    {
                        problem.Append(" | ⚠️ NO TEXTURE");
                        hasProblem = true;
                    }
                    else
                    {
                        problem.Append($" | Tex: {mainTex.name}");
                    }
                    
                    // Check color
                    Color baseColor = Color.white;
                    if (mat.HasProperty("_BaseColor"))
                        baseColor = mat.GetColor("_BaseColor");
                    else if (mat.HasProperty("_Color"))
                        baseColor = mat.GetColor("_Color");
                    
                    if (baseColor == Color.white && mainTex == null)
                    {
                        problem.Append(" | ⚠️ WHITE + NO TEXTURE = WILL BE WHITE");
                        hasProblem = true;
                    }
                    
                    if (hasProblem)
                    {
                        problemMaterials.Add(problem.ToString());
                        suspiciousMaterials++;
                    }
                }
            }
            
            // Log summary
            Debug.Log($"[MaterialDebug] --- SHADER USAGE SUMMARY ---");
            foreach (var kvp in materialStats)
            {
                Debug.Log($"[MaterialDebug]   {kvp.Key}: {kvp.Value} materials");
            }
            
            Debug.Log($"[MaterialDebug] --- STATISTICS ---");
            Debug.Log($"[MaterialDebug] Total materials: {totalMaterials}");
            Debug.Log($"[MaterialDebug] Suspicious materials: {suspiciousMaterials}");
            
            if (problemMaterials.Count > 0)
            {
                Debug.LogWarning($"[MaterialDebug] --- PROBLEM MATERIALS ({problemMaterials.Count}) ---");
                int shown = 0;
                foreach (var prob in problemMaterials)
                {
                    Debug.LogWarning($"[MaterialDebug] {prob}");
                    shown++;
                    if (shown >= 30)
                    {
                        Debug.LogWarning($"[MaterialDebug] ... and {problemMaterials.Count - 30} more");
                        break;
                    }
                }
            }
            
            Debug.Log($"[MaterialDebug] ========================================");
        }
        
        /// <summary>
        /// Analyze all root objects in the scene
        /// </summary>
        public static void AnalyzeEntireScene()
        {
            Debug.Log("[MaterialDebug] =====================================");
            Debug.Log("[MaterialDebug] ANALYZING ENTIRE SCENE");
            Debug.Log("[MaterialDebug] =====================================");
            
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            
            foreach (var root in rootObjects)
            {
                AnalyzeGameObject(root, true);
            }
        }
        
        /// <summary>
        /// Deep analysis of a single material
        /// </summary>
        public static void DeepAnalyzeMaterial(Material mat, string context = "")
        {
            if (mat == null)
            {
                Debug.LogError($"[MaterialDebug] {context} - Material is NULL");
                return;
            }
            
            Debug.Log($"[MaterialDebug] ========================================");
            Debug.Log($"[MaterialDebug] DEEP ANALYSIS: {mat.name} ({context})");
            Debug.Log($"[MaterialDebug] ========================================");
            
            // Shader info
            if (mat.shader == null)
            {
                Debug.LogError($"[MaterialDebug] Shader: NULL");
            }
            else
            {
                Debug.Log($"[MaterialDebug] Shader: {mat.shader.name}");
                Debug.Log($"[MaterialDebug] Shader Supported: {mat.shader.isSupported}");
                Debug.Log($"[MaterialDebug] Shader Property Count: {mat.shader.GetPropertyCount()}");
            }
            
            // Render info
            Debug.Log($"[MaterialDebug] Render Queue: {mat.renderQueue}");
            Debug.Log($"[MaterialDebug] Pass Count: {mat.passCount}");
            
            // List all properties
            Debug.Log($"[MaterialDebug] --- TEXTURES ---");
            string[] texProps = { "_MainTex", "_BaseMap", "_BumpMap", "_NormalMap", 
                "_MetallicGlossMap", "_OcclusionMap", "_EmissionMap", "_DetailMask" };
            foreach (var prop in texProps)
            {
                if (mat.HasProperty(prop))
                {
                    var tex = mat.GetTexture(prop);
                    Debug.Log($"[MaterialDebug]   {prop}: {(tex != null ? tex.name : "NULL")}");
                }
            }
            
            Debug.Log($"[MaterialDebug] --- COLORS ---");
            string[] colorProps = { "_Color", "_BaseColor", "_EmissionColor", "_SpecColor" };
            foreach (var prop in colorProps)
            {
                if (mat.HasProperty(prop))
                {
                    var col = mat.GetColor(prop);
                    Debug.Log($"[MaterialDebug]   {prop}: R={col.r:F2} G={col.g:F2} B={col.b:F2} A={col.a:F2}");
                }
            }
            
            Debug.Log($"[MaterialDebug] --- FLOATS ---");
            string[] floatProps = { "_Metallic", "_Smoothness", "_Glossiness", "_BumpScale", 
                "_OcclusionStrength", "_Surface", "_Blend", "_AlphaClip" };
            foreach (var prop in floatProps)
            {
                if (mat.HasProperty(prop))
                {
                    Debug.Log($"[MaterialDebug]   {prop}: {mat.GetFloat(prop):F2}");
                }
            }
            
            // Keywords
            Debug.Log($"[MaterialDebug] --- KEYWORDS ---");
            var keywords = mat.shaderKeywords;
            if (keywords.Length == 0)
            {
                Debug.Log($"[MaterialDebug]   (no keywords)");
            }
            else
            {
                foreach (var kw in keywords)
                {
                    Debug.Log($"[MaterialDebug]   {kw}");
                }
            }
            
            Debug.Log($"[MaterialDebug] ========================================");
        }
    }
}
