using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Helper class for creating materials that work in both Editor and WebGL builds.
    /// Handles URP vs Built-in pipeline automatically.
    /// </summary>
    public static class MaterialHelper
    {
        private static Material _baseMaterial;
        private static bool _isURP = false;
        private static bool _initialized = false;

        /// <summary>
        /// Initialize the material helper. Call this early in your scene.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            
            // Detect if URP is being used
            _isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            
            // Get a base material from a primitive (guaranteed to work)
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (temp != null)
            {
                var renderer = temp.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    _baseMaterial = renderer.sharedMaterial;
                    ApexLogger.Log(ApexLogger.LogCategory.General, $"Base material shader: {_baseMaterial.shader.name}");
                }
                Object.DestroyImmediate(temp);
            }
            
            _initialized = true;
            ApexLogger.Log(ApexLogger.LogCategory.General, $"MaterialHelper Initialized. URP: {_isURP}");
        }

        /// <summary>
        /// Create a solid color material that works in URP and Built-in pipeline.
        /// </summary>
        public static Material CreateColorMaterial(Color color)
        {
            if (!_initialized) Initialize();
            
            Material mat;
            
            if (_baseMaterial != null && _baseMaterial.shader != null)
            {
                mat = new Material(_baseMaterial);
            }
            else
            {
                // Fallback: try to find a working shader
                mat = CreateFallbackMaterial();
            }
            
            if (mat == null)
            {
                ApexLogger.LogError(ApexLogger.LogCategory.General, "Failed to create material!");
                return null;
            }
            
            // Apply color to all known color properties
            ApplyColor(mat, color);
            
            return mat;
        }

        /// <summary>
        /// Create a material with transparency support.
        /// </summary>
        public static Material CreateTransparentMaterial(Color color)
        {
            Material mat = CreateColorMaterial(color);
            if (mat == null) return null;
            
            // Enable transparency
            SetupTransparency(mat);
            ApplyColor(mat, color);
            
            return mat;
        }

        /// <summary>
        /// Create a material for line renderers.
        /// </summary>
        public static Material CreateLineMaterial(Color color)
        {
            Material mat = null;
            
            // Try Internal-Colored first (best for lines)
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader != null)
            {
                mat = new Material(shader);
            }
            
            // Fallback to Sprites/Default
            if (mat == null)
            {
                shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    mat = new Material(shader);
                }
            }
            
            // Last resort: use base material
            if (mat == null)
            {
                mat = CreateColorMaterial(color);
            }
            
            if (mat != null)
            {
                ApplyColor(mat, color);
                
                // Setup for transparency
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
            }
            
            return mat;
        }

        /// <summary>
        /// Create a material for UI/unlit rendering (no lighting required).
        /// </summary>
        public static Material CreateUnlitMaterial(Color color)
        {
            Material mat = null;
            
            // Try URP Unlit
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                // Try standard unlit
                shader = Shader.Find("Unlit/Color");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            
            if (shader != null)
            {
                mat = new Material(shader);
                ApplyColor(mat, color);
            }
            else
            {
                // Fallback to lit material
                mat = CreateColorMaterial(color);
            }
            
            return mat;
        }

        private static Material CreateFallbackMaterial()
        {
            string[] shaderNames = new string[]
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Standard",
                "Diffuse",
                "Sprites/Default"
            };
            
            foreach (string name in shaderNames)
            {
                Shader shader = Shader.Find(name);
                if (shader != null)
                {
                    ApexLogger.Log(ApexLogger.LogCategory.General, $"Using fallback shader: {name}");
                    return new Material(shader);
                }
            }
            
            return null;
        }

        private static void ApplyColor(Material mat, Color color)
        {
            // Try all known color property names
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }
            
            // Legacy setter
            mat.color = color;
        }

        private static void SetupTransparency(Material mat)
        {
            // URP transparency setup
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1); // 1 = Transparent
            }
            if (mat.HasProperty("_Blend"))
            {
                mat.SetFloat("_Blend", 0); // 0 = Alpha
            }
            
            // Standard shader transparency
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        /// <summary>
        /// Ensure the scene has proper lighting for materials to be visible.
        /// Call this in Start() if colors aren't showing.
        /// </summary>
        public static void EnsureLighting()
        {
            // Check for directional light
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            bool hasDirectional = false;
            
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional && light.intensity > 0)
                {
                    hasDirectional = true;
                    break;
                }
            }
            
            if (!hasDirectional)
            {
                ApexLogger.LogWarning(ApexLogger.LogCategory.General, "No directional light found! Creating one...");
                CreateEmergencyLight();
            }
            
            // Ensure ambient light
            if (RenderSettings.ambientIntensity < 0.1f)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f);
                RenderSettings.ambientIntensity = 1f;
                ApexLogger.Log(ApexLogger.LogCategory.General, "Set ambient lighting");
            }
        }

        private static void CreateEmergencyLight()
        {
            GameObject sunObj = new GameObject("EmergencySun");
            Light sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.9f);
            sun.intensity = 1.0f;
            sun.shadows = LightShadows.None; // No shadows for performance
            sunObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            ApexLogger.Log(ApexLogger.LogCategory.General, "Created emergency directional light");
        }
    }
}
