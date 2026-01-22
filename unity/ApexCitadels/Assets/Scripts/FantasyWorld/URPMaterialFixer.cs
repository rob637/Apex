// ============================================================================
// APEX CITADELS - URP MATERIAL FIXER
// Automatically fixes pink/magenta materials at runtime by upgrading
// Standard shaders to URP equivalents while PRESERVING TEXTURES
// ============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Fixes materials that appear pink/magenta due to shader incompatibility.
    /// Automatically converts Standard shader materials to URP Lit.
    /// IMPORTANT: Only fixes truly broken shaders, preserves working materials.
    /// </summary>
    public class URPMaterialFixer : MonoBehaviour
    {
        [Header("=== AUTO-FIX SETTINGS ===")]
        [Tooltip("Automatically fix materials on all children when enabled")]
        [SerializeField] private bool autoFixOnStart = true;
        
        [Tooltip("Log details about fixed materials")]
        [SerializeField] private bool verboseLogging = false;
        
        [Tooltip("Cache fixed materials to avoid recreating")]
        [SerializeField] private bool cacheMaterials = true;
        
        // Cache of already-fixed materials
        private static Dictionary<Material, Material> _fixedMaterialCache = new Dictionary<Material, Material>();
        
        // URP shader reference
        private static Shader _urpLitShader;
        private static Shader _urpSimpleLitShader;
        
        private int _fixedCount = 0;
        private int _skippedCount = 0;
        
        private void Start()
        {
            if (autoFixOnStart)
            {
                FixAllMaterialsInChildren();
            }
        }
        
        /// <summary>
        /// Initialize URP shaders (lazy loading)
        /// </summary>
        private static void InitializeShaders()
        {
            if (_urpLitShader == null)
            {
                _urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (_urpSimpleLitShader == null)
            {
                _urpSimpleLitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            }
            
            // Fallback to simple lit if lit isn't available
            if (_urpLitShader == null)
            {
                _urpLitShader = _urpSimpleLitShader;
            }
        }
        
        /// <summary>
        /// Fix all materials on all renderers in this object and its children
        /// </summary>
        public void FixAllMaterialsInChildren()
        {
            InitializeShaders();
            
            if (_urpLitShader == null)
            {
                Debug.LogWarning("[URPMaterialFixer] Could not find URP Lit shader. Materials cannot be fixed.");
                return;
            }
            
            _fixedCount = 0;
            _skippedCount = 0;
            
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                FixRendererMaterials(renderer);
            }
            
            if (_fixedCount > 0 || verboseLogging)
            {
                Debug.Log($"[URPMaterialFixer] Fixed {_fixedCount} materials, skipped {_skippedCount} (already OK)");
            }
        }
        
        /// <summary>
        /// Fix materials on a specific renderer
        /// </summary>
        private void FixRendererMaterials(Renderer renderer)
        {
            if (renderer == null) return;
            
            var materials = renderer.sharedMaterials;
            bool anyChanged = false;
            
            for (int i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                if (mat == null) continue;
                
                if (IsBrokenShader(mat))
                {
                    materials[i] = GetFixedMaterial(mat);
                    anyChanged = true;
                    _fixedCount++;
                    
                    if (verboseLogging)
                    {
                        Debug.Log($"[URPMaterialFixer] Fixed broken shader on: {mat.name}");
                    }
                }
                else
                {
                    _skippedCount++;
                }
            }
            
            if (anyChanged)
            {
                renderer.sharedMaterials = materials;
            }
        }
        
        /// <summary>
        /// Check if a material has a BROKEN shader (pink/magenta or renders white)
        /// </summary>
        private bool IsBrokenShader(Material mat)
        {
            if (mat == null) return true;
            if (mat.shader == null) return true;
            
            string shaderName = mat.shader.name;
            
            // Hidden/InternalErrorShader is what Unity uses for truly broken shaders (pink)
            if (shaderName.Contains("Hidden/InternalErrorShader") ||
                shaderName.Contains("Error") ||
                shaderName == "Hidden/InternalErrorShader")
            {
                return true;
            }
            
            // Check if the shader failed to compile (another pink indicator)
            if (!mat.shader.isSupported)
            {
                return true;
            }
            
            // Standard shader in URP will show pink - needs fixing
            if (shaderName == "Standard" || shaderName == "Standard (Specular setup)")
            {
                return true;
            }
            
            // Legacy shaders are definitely broken in URP
            if (shaderName.StartsWith("Legacy Shaders/"))
            {
                return true;
            }
            
            // Mobile shaders don't work in URP
            if (shaderName.StartsWith("Mobile/"))
            {
                return true;
            }
            
            // Nature shaders (trees, terrain) don't work in URP
            if (shaderName.StartsWith("Nature/"))
            {
                return true;
            }
            
            // Particles shaders need URP equivalents
            if (shaderName.StartsWith("Particles/") && !shaderName.Contains("Universal"))
            {
                return true;
            }
            
            // Everything else is probably fine
            return false;
        }
        
        /// <summary>
        /// Get or create a fixed URP version of a material
        /// </summary>
        private Material GetFixedMaterial(Material original)
        {
            if (original == null) return CreateDefaultMaterial();
            
            // Check cache first
            if (cacheMaterials && _fixedMaterialCache.TryGetValue(original, out Material cached))
            {
                return cached;
            }
            
            // Create new URP material
            Material newMat = new Material(_urpLitShader);
            newMat.name = original.name + "_URP";
            
            // Copy ALL properties from original - be comprehensive!
            CopyMaterialProperties(original, newMat);
            
            // Cache it
            if (cacheMaterials)
            {
                _fixedMaterialCache[original] = newMat;
            }
            
            return newMat;
        }
        
        /// <summary>
        /// Copy relevant properties from original material to URP material
        /// Handles both Standard shader AND URP shader property names
        /// </summary>
        private void CopyMaterialProperties(Material source, Material dest)
        {
            // ===== MAIN TEXTURE / ALBEDO =====
            // Try multiple property names that different shaders use
            Texture mainTex = null;
            string[] mainTexNames = { "_BaseMap", "_MainTex", "_Albedo", "_AlbedoMap", "_Diffuse", "_DiffuseMap" };
            foreach (var propName in mainTexNames)
            {
                if (source.HasProperty(propName))
                {
                    mainTex = source.GetTexture(propName);
                    if (mainTex != null) break;
                }
            }
            
            if (mainTex != null)
            {
                dest.SetTexture("_BaseMap", mainTex);
                dest.SetTexture("_MainTex", mainTex);
                
                if (verboseLogging)
                {
                    Debug.Log($"[URPMaterialFixer] Copied texture: {mainTex.name}");
                }
            }
            
            // ===== BASE COLOR =====
            Color baseColor = Color.white;
            string[] colorNames = { "_BaseColor", "_Color", "_Albedo", "_MainColor" };
            foreach (var propName in colorNames)
            {
                if (source.HasProperty(propName))
                {
                    baseColor = source.GetColor(propName);
                    break;
                }
            }
            dest.SetColor("_BaseColor", baseColor);
            dest.SetColor("_Color", baseColor);
            
            // ===== NORMAL MAP =====
            Texture bumpMap = null;
            string[] bumpNames = { "_BumpMap", "_NormalMap", "_Normal" };
            foreach (var propName in bumpNames)
            {
                if (source.HasProperty(propName))
                {
                    bumpMap = source.GetTexture(propName);
                    if (bumpMap != null) break;
                }
            }
            if (bumpMap != null)
            {
                dest.SetTexture("_BumpMap", bumpMap);
                dest.EnableKeyword("_NORMALMAP");
                
                // Copy bump scale if available
                if (source.HasProperty("_BumpScale"))
                {
                    dest.SetFloat("_BumpScale", source.GetFloat("_BumpScale"));
                }
            }
            
            // ===== METALLIC =====
            float metallic = 0f;
            if (source.HasProperty("_Metallic"))
            {
                metallic = source.GetFloat("_Metallic");
            }
            dest.SetFloat("_Metallic", metallic);
            
            // Metallic map
            if (source.HasProperty("_MetallicGlossMap"))
            {
                Texture metallicMap = source.GetTexture("_MetallicGlossMap");
                if (metallicMap != null)
                {
                    dest.SetTexture("_MetallicGlossMap", metallicMap);
                    dest.EnableKeyword("_METALLICGLOSSMAP");
                }
            }
            
            // ===== SMOOTHNESS / GLOSSINESS =====
            float smoothness = 0.5f;
            string[] smoothNames = { "_Smoothness", "_Glossiness", "_Gloss" };
            foreach (var propName in smoothNames)
            {
                if (source.HasProperty(propName))
                {
                    smoothness = source.GetFloat(propName);
                    break;
                }
            }
            dest.SetFloat("_Smoothness", smoothness);
            
            // ===== EMISSION =====
            Color emission = Color.black;
            if (source.HasProperty("_EmissionColor"))
            {
                emission = source.GetColor("_EmissionColor");
            }
            
            Texture emissionMap = null;
            if (source.HasProperty("_EmissionMap"))
            {
                emissionMap = source.GetTexture("_EmissionMap");
            }
            
            if (emission != Color.black || emissionMap != null)
            {
                dest.SetColor("_EmissionColor", emission);
                if (emissionMap != null)
                {
                    dest.SetTexture("_EmissionMap", emissionMap);
                }
                dest.EnableKeyword("_EMISSION");
                dest.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            
            // ===== OCCLUSION =====
            if (source.HasProperty("_OcclusionMap"))
            {
                Texture occlusionMap = source.GetTexture("_OcclusionMap");
                if (occlusionMap != null)
                {
                    dest.SetTexture("_OcclusionMap", occlusionMap);
                    
                    if (source.HasProperty("_OcclusionStrength"))
                    {
                        dest.SetFloat("_OcclusionStrength", source.GetFloat("_OcclusionStrength"));
                    }
                }
            }
            
            // ===== TILING AND OFFSET =====
            if (source.HasProperty("_MainTex"))
            {
                dest.SetTextureScale("_BaseMap", source.GetTextureScale("_MainTex"));
                dest.SetTextureOffset("_BaseMap", source.GetTextureOffset("_MainTex"));
            }
            else if (source.HasProperty("_BaseMap"))
            {
                dest.SetTextureScale("_BaseMap", source.GetTextureScale("_BaseMap"));
                dest.SetTextureOffset("_BaseMap", source.GetTextureOffset("_BaseMap"));
            }
            
            // ===== RENDER QUEUE =====
            dest.renderQueue = source.renderQueue;
            
            // ===== TRANSPARENCY =====
            SetupSurfaceType(source, dest);
        }
        
        /// <summary>
        /// Set up URP surface type (opaque, cutout, transparent)
        /// </summary>
        private void SetupSurfaceType(Material source, Material dest)
        {
            bool isTransparent = false;
            bool isCutout = false;
            
            // Check Standard shader mode
            if (source.HasProperty("_Mode"))
            {
                int mode = (int)source.GetFloat("_Mode");
                isCutout = mode == 1;
                isTransparent = mode == 2 || mode == 3;
            }
            
            // Check by render queue
            if (source.renderQueue >= 3000)
            {
                isTransparent = true;
            }
            else if (source.renderQueue >= 2450 && source.renderQueue < 3000)
            {
                isCutout = true;
            }
            
            // Check for cutout keywords
            if (source.IsKeywordEnabled("_ALPHATEST_ON"))
            {
                isCutout = true;
            }
            
            if (isCutout)
            {
                dest.SetFloat("_Surface", 0); // Opaque
                dest.SetFloat("_AlphaClip", 1);
                dest.EnableKeyword("_ALPHATEST_ON");
                dest.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else if (isTransparent)
            {
                dest.SetFloat("_Surface", 1); // Transparent
                dest.SetFloat("_Blend", 0); // Alpha
                dest.SetOverrideTag("RenderType", "Transparent");
                dest.renderQueue = (int)RenderQueue.Transparent;
                dest.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                dest.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                dest.SetInt("_ZWrite", 0);
                dest.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                // Opaque
                dest.SetFloat("_Surface", 0);
                dest.SetOverrideTag("RenderType", "Opaque");
                dest.SetInt("_SrcBlend", (int)BlendMode.One);
                dest.SetInt("_DstBlend", (int)BlendMode.Zero);
                dest.SetInt("_ZWrite", 1);
            }
        }
        
        /// <summary>
        /// Create a default gray material if original is null
        /// </summary>
        private Material CreateDefaultMaterial()
        {
            InitializeShaders();
            
            var mat = new Material(_urpLitShader);
            mat.name = "Default_URP";
            mat.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.7f, 1f));
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.5f);
            
            return mat;
        }
        
        /// <summary>
        /// Static method to fix a single GameObject and its children
        /// </summary>
        public static void FixGameObject(GameObject go, bool verbose = false)
        {
            if (go == null) return;
            
            InitializeShaders();
            
            if (_urpLitShader == null)
            {
                Debug.LogWarning("[URPMaterialFixer] Could not find URP Lit shader.");
                return;
            }
            
            // Check if ANY materials need fixing before adding the component
            bool needsFix = false;
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null && mat.shader != null)
                    {
                        string shaderName = mat.shader.name;
                        if (shaderName.Contains("InternalErrorShader") ||
                            shaderName == "Standard" ||
                            shaderName == "Standard (Specular setup)" ||
                            shaderName.StartsWith("Legacy Shaders/") ||
                            !mat.shader.isSupported)
                        {
                            needsFix = true;
                            break;
                        }
                    }
                }
                if (needsFix) break;
            }
            
            // Only add fixer if actually needed
            if (!needsFix)
            {
                return;
            }
            
            var fixer = go.AddComponent<URPMaterialFixer>();
            fixer.autoFixOnStart = false;
            fixer.verboseLogging = verbose;
            fixer.FixAllMaterialsInChildren();
            
            // Remove fixer after use
            Destroy(fixer);
        }
        
        /// <summary>
        /// Clear the material cache (useful when reloading scenes)
        /// </summary>
        public static void ClearCache()
        {
            _fixedMaterialCache.Clear();
        }
    }
}
