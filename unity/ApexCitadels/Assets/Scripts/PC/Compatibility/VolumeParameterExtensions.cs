// ============================================================================
// APEX CITADELS - VOLUME PARAMETER EXTENSIONS
// Compatibility layer for Unity 6 URP Volume API changes
// ============================================================================
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ApexCitadels.PC.Compatibility
{
    /// <summary>
    /// Extension methods for VolumeParameter to support both old and new URP APIs.
    /// In Unity 6 URP, the .Override() method was removed. This provides a compatible API.
    /// </summary>
    public static class VolumeParameterExtensions
    {
        /// <summary>
        /// Sets the override state and value for a volume parameter.
        /// Replaces the deprecated .Override() method from older URP versions.
        /// </summary>
        public static void SetOverride<T>(this VolumeParameter<T> param, T value)
        {
            param.overrideState = true;
            param.value = value;
        }
    }
}
