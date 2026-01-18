Shader "ApexCitadels/FantasyWater"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor ("Shallow Color", Color) = (0.2, 0.6, 0.8, 0.8)
        _DeepColor ("Deep Color", Color) = (0.05, 0.2, 0.4, 0.9)
        _FoamColor ("Foam Color", Color) = (0.9, 0.95, 1, 0.8)
        
        [Header(Wave Settings)]
        _WaveSpeed ("Wave Speed", Range(0.1, 5)) = 1
        _WaveScale ("Wave Scale", Range(0.1, 10)) = 2
        _WaveHeight ("Wave Height", Range(0, 0.5)) = 0.1
        _WaveDirection ("Wave Direction", Vector) = (1, 0.5, 0, 0)
        
        [Header(Foam)]
        _FoamThreshold ("Foam Threshold", Range(0, 1)) = 0.5
        _FoamScale ("Foam Scale", Range(1, 20)) = 5
        _FoamSpeed ("Foam Speed", Range(0.1, 2)) = 0.5
        
        [Header(Reflection)]
        _Reflectivity ("Reflectivity", Range(0, 1)) = 0.3
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 3
        
        [Header(Transparency)]
        _Transparency ("Transparency", Range(0, 1)) = 0.7
        _DepthFade ("Depth Fade", Range(0.1, 10)) = 2
        
        [Header(Distortion)]
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        
        [Header(Sparkle)]
        _SparkleIntensity ("Sparkle Intensity", Range(0, 2)) = 0.5
        _SparkleScale ("Sparkle Scale", Range(10, 100)) = 50
        _SparkleSpeed ("Sparkle Speed", Range(0.5, 5)) = 2
        
        [Header(Textures)]
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _FoamTex ("Foam Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        
        Pass
        {
            Name "FantasyWater"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
                float fogCoord : TEXCOORD5;
            };
            
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_FoamTex);
            SAMPLER(sampler_FoamTex);
            
            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                
                half _WaveSpeed;
                half _WaveScale;
                half _WaveHeight;
                float4 _WaveDirection;
                
                half _FoamThreshold;
                half _FoamScale;
                half _FoamSpeed;
                
                half _Reflectivity;
                half _FresnelPower;
                
                half _Transparency;
                half _DepthFade;
                
                half _DistortionStrength;
                
                half _SparkleIntensity;
                half _SparkleScale;
                half _SparkleSpeed;
                
                float4 _NormalMap_ST;
                float4 _FoamTex_ST;
            CBUFFER_END
            
            // Simple noise function
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Gerstner wave function
            float3 GerstnerWave(float2 pos, float2 dir, float steepness, float wavelength, float time)
            {
                float k = 2.0 * PI / wavelength;
                float c = sqrt(9.8 / k);
                float2 d = normalize(dir);
                float f = k * (dot(d, pos) - c * time);
                float a = steepness / k;
                
                return float3(
                    d.x * a * cos(f),
                    a * sin(f),
                    d.y * a * cos(f)
                );
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                float3 posOS = IN.positionOS.xyz;
                float2 worldXZ = TransformObjectToWorld(posOS).xz;
                
                // Apply Gerstner waves
                float time = _Time.y * _WaveSpeed;
                float3 wave1 = GerstnerWave(worldXZ, _WaveDirection.xy, 0.25, 20, time);
                float3 wave2 = GerstnerWave(worldXZ, float2(0.7, 0.3), 0.15, 10, time * 1.2);
                float3 wave3 = GerstnerWave(worldXZ, float2(-0.5, 0.8), 0.1, 5, time * 0.8);
                
                float3 waveOffset = (wave1 + wave2 + wave3) * _WaveHeight;
                posOS += waveOffset;
                
                OUT.positionHCS = TransformObjectToHClip(posOS);
                OUT.uv = IN.uv;
                OUT.positionWS = TransformObjectToWorld(posOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                OUT.viewDirWS = GetWorldSpaceViewDir(OUT.positionWS);
                OUT.fogCoord = ComputeFogFactor(OUT.positionHCS.z);
                
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                // Screen UVs for depth sampling
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                
                // Sample depth for water depth calculation
                float sceneDepth = LinearEyeDepth(
                    SampleSceneDepth(screenUV), 
                    _ZBufferParams
                );
                float waterDepth = sceneDepth - IN.screenPos.w;
                float depthFactor = saturate(waterDepth / _DepthFade);
                
                // Animated UVs
                float time = _Time.y;
                float2 uv1 = IN.uv * _WaveScale + _WaveDirection.xy * time * 0.1;
                float2 uv2 = IN.uv * _WaveScale * 0.7 - float2(0.3, 0.5) * time * 0.08;
                
                // Sample normal maps
                float3 normal1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1));
                float3 normal2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2));
                float3 normalTS = normalize(normal1 + normal2);
                
                // Transform normal to world space (simplified)
                float3 normalWS = normalize(IN.normalWS + normalTS.x * float3(1,0,0) + normalTS.y * float3(0,0,1));
                
                // View direction
                float3 viewDir = normalize(IN.viewDirWS);
                
                // Fresnel
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), _FresnelPower);
                fresnel = lerp(0.02, 1.0, fresnel) * _Reflectivity;
                
                // Base water color (shallow to deep based on depth)
                half4 waterColor = lerp(_ShallowColor, _DeepColor, depthFactor);
                
                // Foam
                float2 foamUV = IN.uv * _FoamScale + float2(0.2, 0.3) * time * _FoamSpeed;
                float foamNoise = noise(foamUV);
                float foamNoise2 = noise(foamUV * 2.3 + time * 0.1);
                float foam = saturate((foamNoise + foamNoise2 * 0.5) - _FoamThreshold);
                foam *= (1.0 - depthFactor); // More foam in shallow areas
                
                // Sample foam texture if available
                half4 foamTex = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, foamUV);
                foam *= foamTex.r;
                
                // Mix foam with water
                waterColor = lerp(waterColor, _FoamColor, foam);
                
                // Sparkles (sun reflections)
                float2 sparkleUV = IN.positionWS.xz * _SparkleScale;
                float sparkle1 = noise(sparkleUV + time * _SparkleSpeed);
                float sparkle2 = noise(sparkleUV * 1.5 - time * _SparkleSpeed * 0.7);
                float sparkle = pow(sparkle1 * sparkle2, 10) * _SparkleIntensity;
                
                // Only show sparkles where light hits
                Light mainLight = GetMainLight();
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float specular = pow(saturate(dot(normalWS, halfDir)), 64);
                sparkle *= specular * mainLight.color.r;
                
                waterColor.rgb += sparkle;
                
                // Add fresnel reflection (simplified sky reflection)
                float3 reflectionColor = lerp(waterColor.rgb, float3(0.6, 0.8, 1.0), fresnel);
                waterColor.rgb = reflectionColor;
                
                // Lighting
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 lighting = mainLight.color * NdotL * 0.5 + 0.5; // Soft lighting
                waterColor.rgb *= lighting;
                
                // Final alpha
                waterColor.a = lerp(_Transparency * 0.5, _Transparency, depthFactor);
                waterColor.a = saturate(waterColor.a + foam * 0.5);
                
                // Apply fog
                waterColor.rgb = MixFog(waterColor.rgb, IN.fogCoord);
                
                return waterColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
