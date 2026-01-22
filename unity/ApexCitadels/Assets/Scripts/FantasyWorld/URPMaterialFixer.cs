// ============================================================================
// APEX CITADELS - URP MATERIAL FIXER
// Automatically fixes pink/magenta materials at runtime by upgrading
// Standard shaders to URP equivalents
// ============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Fixes materials that appear pink/magenta due to shader incompatibility.
    /// Automatically converts Standard shader materials to URP Lit.
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
        private static Shader _urpUnlitShader;
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
            if (_urpUnlitShader == null)
            {
                _urpUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
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
            
            if (verboseLogging || _fixedCount > 0)
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
                
                if (NeedsFixing(mat))
                {
                    materials[i] = GetFixedMaterial(mat);
                    anyChanged = true;
                    _fixedCount++;
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
        /// Check if a material needs to be fixed (uses incompatible shader)
        /// </summary>
        private bool NeedsFixing(Material mat)
        {
            if (mat == null || mat.shader == null) return true;
            
            string shaderName = mat.shader.name;
            
            // Already URP shader
            if (shaderName.Contains("Universal Render Pipeline") ||
                shaderName.Contains("URP") ||
                shaderName.Contains("Sprites/Default") ||
                shaderName.Contains("UI/") ||
                shaderName.Contains("TextMeshPro/"))
            {
                return false;
            }
            
            // Check if shader is broken (pink material indicator)
            // Hidden/InternalErrorShader is what Unity uses for broken shaders
            if (shaderName.Contains("Hidden/InternalErrorShader") ||
                shaderName.Contains("Error"))
            {
                return true;
            }
            
            // Standard shader or other built-in shaders need fixing
            if (shaderName == "Standard" ||
                shaderName == "Standard (Specular setup)" ||
                shaderName.StartsWith("Legacy Shaders/") ||
                shaderName.StartsWith("Mobile/") ||
                shaderName.StartsWith("Nature/") ||
                shaderName.StartsWith("Particles/"))
            {
                return true;
            }
            
            // Synty often uses these
            if (shaderName.Contains("Synty") ||
                shaderName.Contains("POLYGON"))
            {
                return true;
            }
            
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
            
            // Copy properties from original
            CopyMaterialProperties(original, newMat);
            
            // Cache it
            if (cacheMaterials)
            {
                _fixedMaterialCache[original] = newMat;
            }
            
            if (verboseLogging)
            {
                Debug.Log($"[URPMaterialFixer] Fixed: {original.name} ({original.shader.name} -> URP/Lit)");
            }
            
            return newMat;
        }
        
        /// <summary>
        /// Copy relevant properties from original material to URP material
        /// </summary>
        private void CopyMaterialProperties(Material source, Material dest)
        {
            // Main texture (albedo)
            if (source.HasProperty("_MainTex"))
            {
                Texture mainTex = source.GetTexture("_MainTex");
                if (mainTex != null)
                {
                    dest.SetTexture("_BaseMap", mainTex);
                    dest.SetTexture("_MainTex", mainTex); // URP also uses this
                }
            }
            
            // Base color
            if (source.HasProperty("_Color"))
            {
                Color color = source.GetColor("_Color");
                dest.SetColor("_BaseColor", color);
                dest.SetColor("_Color", color);
            }
            else
            {
                // Default to white if no color
                dest.SetColor("_BaseColor", Color.white);
            }
            
            // Normal map
            if (source.HasProperty("_BumpMap"))
            {
                Texture bumpMap = source.GetTexture("_BumpMap");
                if (bumpMap != null)
                {
                    dest.SetTexture("_BumpMap", bumpMap);
                    dest.EnableKeyword("_NORMALMAP");
                }
            }
            
            // Metallic
            if (source.HasProperty("_Metallic"))
            {
                float metallic = source.GetFloat("_Metallic");
                dest.SetFloat("_Metallic", metallic);
            }
            else
            {
                dest.SetFloat("_Metallic", 0f);
            }
            
            // Smoothness/Glossiness
            if (source.HasProperty("_Glossiness"))
            {
                float smoothness = source.GetFloat("_Glossiness");
                dest.SetFloat("_Smoothness", smoothness);
            }
            else if (source.HasProperty("_Smoothness"))
            {
                dest.SetFloat("_Smoothness", source.GetFloat("_Smoothness"));
            }
            else
            {
                dest.SetFloat("_Smoothness", 0.5f);
            }
            
            // Emission
            if (source.HasProperty("_EmissionColor"))
            {
                Color emission = source.GetColor("_EmissionColor");
                if (emission != Color.black)
                {
                    dest.SetColor("_EmissionColor", emission);
                    dest.EnableKeyword("_EMISSION");
                    dest.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
            }
            
            // Emission map
            if (source.HasProperty("_EmissionMap"))
            {
                Texture emissionMap = source.GetTexture("_EmissionMap");
                if (emissionMap != null)
                {
                    dest.SetTexture("_EmissionMap", emissionMap);
                    dest.EnableKeyword("_EMISSION");
                }
            }
            
            // Occlusion map
            if (source.HasProperty("_OcclusionMap"))
            {
                Texture occlusionMap = source.GetTexture("_OcclusionMap");
                if (occlusionMap != null)
                {
                    dest.SetTexture("_OcclusionMap", occlusionMap);
                }
            }
            
            // Copy render queue
            dest.renderQueue = source.renderQueue;
            
            // Handle transparency
            if (source.HasProperty("_Mode"))
            {
                int mode = (int)source.GetFloat("_Mode");
                SetupSurfaceType(dest, mode);
            }
            else if (source.renderQueue >= 3000) // Transparent queue
            {
                SetupSurfaceType(dest, 3); // Transparent
            }
        }
        
        /// <summary>
        /// Set up URP surface type (opaque, cutout, transparent)
        /// </summary>
        private void SetupSurfaceType(Material mat, int mode)
        {
            // 0 = Opaque, 1 = Cutout, 2 = Fade, 3 = Transparent
            switch (mode)
            {
                case 1: // Cutout
                    mat.SetFloat("_Surface", 0); // Opaque
                    mat.SetFloat("_AlphaClip", 1);
                    mat.EnableKeyword("_ALPHATEST_ON");
                    break;
                case 2: // Fade
                case 3: // Transparent
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetFloat("_Blend", 0); // Alpha
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                    mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    break;
                default: // Opaque
                    mat.SetFloat("_Surface", 0);
                    mat.SetOverrideTag("RenderType", "Opaque");
                    mat.renderQueue = (int)RenderQueue.Geometry;
                    mat.SetInt("_SrcBlend", (int)BlendMode.One);
                    mat.SetInt("_DstBlend", (int)BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    break;
            }
        }
        
        /// <summary>
        /// Create a default material if original is null
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
            
            var fixer = go.GetComponent<URPMaterialFixer>();
            if (fixer == null)
            {
                fixer = go.AddComponent<URPMaterialFixer>();
                fixer.autoFixOnStart = false;
                fixer.verboseLogging = verbose;
            }
            
            fixer.FixAllMaterialsInChildren();
            
            // Remove fixer after use if we added it
            if (!fixer.autoFixOnStart)
            {
                Destroy(fixer);
            }
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
