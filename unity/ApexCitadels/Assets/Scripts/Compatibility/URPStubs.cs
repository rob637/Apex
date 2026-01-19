// ============================================================================
// URPStubs.cs - Compatibility stubs for when URP package is not installed
// Remove this file once URP is properly installed in Unity
// ============================================================================

#if !UNITY_RENDER_PIPELINES_UNIVERSAL
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Stub Volume component
    /// </summary>
    public class Volume : MonoBehaviour
    {
        public VolumeProfile profile;
        public bool isGlobal = true;
        public float weight = 1f;
        public float priority = 0f;
    }
    
    /// <summary>
    /// Stub VolumeProfile
    /// </summary>
    public class VolumeProfile : ScriptableObject
    {
        public bool TryGet<T>(out T component) where T : VolumeComponent
        {
            component = default;
            return false;
        }
        
        public T Add<T>() where T : VolumeComponent, new()
        {
            return new T();
        }
    }
    
    /// <summary>
    /// Base class for volume components
    /// </summary>
    public abstract class VolumeComponent : ScriptableObject
    {
        public bool active = true;
    }
    
    /// <summary>
    /// Stub Bloom effect
    /// </summary>
    public class Bloom : VolumeComponent
    {
        public ClampedFloatParameter threshold = new ClampedFloatParameter(0.9f, 0f, 10f);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 10f);
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f);
        public ColorParameter tint = new ColorParameter(Color.white);
    }
    
    /// <summary>
    /// Stub Vignette effect
    /// </summary>
    public class Vignette : VolumeComponent
    {
        public ColorParameter color = new ColorParameter(Color.black);
        public ClampedFloatParameter center = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter smoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f);
        public BoolParameter rounded = new BoolParameter(false);
    }
    
    /// <summary>
    /// Stub ChromaticAberration effect
    /// </summary>
    public class ChromaticAberration : VolumeComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
    }
    
    /// <summary>
    /// Stub ColorAdjustments effect
    /// </summary>
    public class ColorAdjustments : VolumeComponent
    {
        public FloatParameter postExposure = new FloatParameter(0f);
        public ClampedFloatParameter contrast = new ClampedFloatParameter(0f, -100f, 100f);
        public ColorParameter colorFilter = new ColorParameter(Color.white);
        public ClampedFloatParameter hueShift = new ClampedFloatParameter(0f, -180f, 180f);
        public ClampedFloatParameter saturation = new ClampedFloatParameter(0f, -100f, 100f);
    }
    
    /// <summary>
    /// Stub FilmGrain effect
    /// </summary>
    public class FilmGrain : VolumeComponent
    {
        public FilmGrainLookupParameter type = new FilmGrainLookupParameter(FilmGrainLookup.Thin1);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter response = new ClampedFloatParameter(0.8f, 0f, 1f);
    }
    
    /// <summary>
    /// Stub LensDistortion effect
    /// </summary>
    public class LensDistortion : VolumeComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, -1f, 1f);
        public ClampedFloatParameter xMultiplier = new ClampedFloatParameter(1f, 0f, 1f);
        public ClampedFloatParameter yMultiplier = new ClampedFloatParameter(1f, 0f, 1f);
        public Vector2Parameter center = new Vector2Parameter(Vector2.one * 0.5f);
        public ClampedFloatParameter scale = new ClampedFloatParameter(1f, 0.01f, 5f);
    }
    
    /// <summary>
    /// Stub DepthOfField effect
    /// </summary>
    public class DepthOfField : VolumeComponent
    {
        public DepthOfFieldModeParameter mode = new DepthOfFieldModeParameter(DepthOfFieldMode.Off);
        public MinFloatParameter gaussianStart = new MinFloatParameter(10f, 0f);
        public MinFloatParameter gaussianEnd = new MinFloatParameter(30f, 0f);
        public ClampedFloatParameter gaussianMaxRadius = new ClampedFloatParameter(1f, 0.5f, 1.5f);
        public MinFloatParameter focusDistance = new MinFloatParameter(10f, 0.1f);
        public ClampedFloatParameter aperture = new ClampedFloatParameter(5.6f, 1f, 32f);
        public ClampedFloatParameter focalLength = new ClampedFloatParameter(50f, 1f, 300f);
        public ClampedIntParameter bladeCount = new ClampedIntParameter(5, 3, 9);
        public ClampedFloatParameter bladeCurvature = new ClampedFloatParameter(1f, 0f, 1f);
        public ClampedFloatParameter bladeRotation = new ClampedFloatParameter(0f, -180f, 180f);
    }
    
    public enum DepthOfFieldMode { Off, Gaussian, Bokeh }
    public enum FilmGrainLookup { Thin1, Thin2, Medium1, Medium2, Medium3, Medium4, Medium5, Medium6, Large01, Large02, Custom }
    
    // Parameter types
    public class VolumeParameter<T>
    {
        public T value;
        public bool overrideState = true;
        public VolumeParameter(T defaultValue) { value = defaultValue; }
        public static implicit operator T(VolumeParameter<T> p) => p.value;
    }
    
    public class FloatParameter : VolumeParameter<float>
    {
        public FloatParameter(float value) : base(value) { }
    }
    
    public class MinFloatParameter : VolumeParameter<float>
    {
        public float min;
        public MinFloatParameter(float value, float min) : base(value) { this.min = min; }
    }
    
    public class ClampedFloatParameter : VolumeParameter<float>
    {
        public float min, max;
        public ClampedFloatParameter(float value, float min, float max) : base(value) { this.min = min; this.max = max; }
    }
    
    public class ClampedIntParameter : VolumeParameter<int>
    {
        public int min, max;
        public ClampedIntParameter(int value, int min, int max) : base(value) { this.min = min; this.max = max; }
    }
    
    public class ColorParameter : VolumeParameter<Color>
    {
        public ColorParameter(Color value) : base(value) { }
    }
    
    public class BoolParameter : VolumeParameter<bool>
    {
        public BoolParameter(bool value) : base(value) { }
    }
    
    public class Vector2Parameter : VolumeParameter<Vector2>
    {
        public Vector2Parameter(Vector2 value) : base(value) { }
    }
    
    public class DepthOfFieldModeParameter : VolumeParameter<DepthOfFieldMode>
    {
        public DepthOfFieldModeParameter(DepthOfFieldMode value) : base(value) { }
    }
    
    public class FilmGrainLookupParameter : VolumeParameter<FilmGrainLookup>
    {
        public FilmGrainLookupParameter(FilmGrainLookup value) : base(value) { }
    }
}
#endif
