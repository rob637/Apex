Shader "ApexCitadels/FantasyMapTile"
{
    Properties
    {
        _MainTex ("Map Texture", 2D) = "white" {}
        _ParchmentTex ("Parchment Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        
        [Header(Fantasy Style)]
        _ParchmentTint ("Parchment Tint", Color) = (0.95, 0.88, 0.72, 1)
        _FantasyIntensity ("Fantasy Intensity", Range(0, 1)) = 0.7
        
        [Header(Color Grading)]
        _WaterTint ("Water Tint", Color) = (0.2, 0.5, 0.8, 0.6)
        _ForestTint ("Forest Tint", Color) = (0.1, 0.4, 0.15, 0.4)
        _RoadTint ("Road Tint", Color) = (0.6, 0.55, 0.5, 0.3)
        
        [Header(Magical Effects)]
        _GlowColor ("Glow Color", Color) = (0.4, 0.6, 1, 0.3)
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 0.5
        _ShimmerSpeed ("Shimmer Speed", Range(0, 2)) = 0.5
        _ShimmerIntensity ("Shimmer Intensity", Range(0, 0.5)) = 0.1
        
        [Header(Vignette)]
        _VignetteIntensity ("Vignette Intensity", Range(0, 1)) = 0.4
        _VignetteRadius ("Vignette Radius", Range(0.1, 1)) = 0.7
        _VignetteSoftness ("Vignette Softness", Range(0.01, 1)) = 0.3
        
        [Header(Fog of War)]
        _FogOfWar ("Fog of War", Range(0, 1)) = 0
        _FogColor ("Fog Color", Color) = (0.3, 0.25, 0.2, 0.8)
        _ExploredCenter ("Explored Center", Vector) = (0, 0, 0, 0)
        _ExploredRadius ("Explored Radius", Float) = 100
        _FogSoftness ("Fog Softness", Float) = 50
        
        [Header(Territory)]
        _TerritoryOverlay ("Territory Overlay", 2D) = "black" {}
        _TerritoryColor ("Territory Color", Color) = (0.8, 0.4, 0.1, 0.5)
        _TerritoryPulse ("Territory Pulse", Range(0, 1)) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        
        Pass
        {
            Name "FantasyMapPass"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fogCoord : TEXCOORD2;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_ParchmentTex);
            SAMPLER(sampler_ParchmentTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_TerritoryOverlay);
            SAMPLER(sampler_TerritoryOverlay);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ParchmentTex_ST;
                float4 _NoiseTex_ST;
                
                half4 _ParchmentTint;
                half _FantasyIntensity;
                
                half4 _WaterTint;
                half4 _ForestTint;
                half4 _RoadTint;
                
                half4 _GlowColor;
                half _GlowIntensity;
                half _ShimmerSpeed;
                half _ShimmerIntensity;
                
                half _VignetteIntensity;
                half _VignetteRadius;
                half _VignetteSoftness;
                
                half _FogOfWar;
                half4 _FogColor;
                float4 _ExploredCenter;
                float _ExploredRadius;
                float _FogSoftness;
                
                half4 _TerritoryColor;
                half _TerritoryPulse;
            CBUFFER_END
            
            // Simplex noise for procedural effects
            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 permute(float3 x) { return mod289(((x*34.0)+1.0)*x); }
            
            float snoise(float2 v)
            {
                const float4 C = float4(0.211324865405187, 0.366025403784439,
                                        -0.577350269189626, 0.024390243902439);
                float2 i = floor(v + dot(v, C.yy));
                float2 x0 = v - i + dot(i, C.xx);
                float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
                float4 x12 = x0.xyxy + C.xxzz;
                x12.xy -= i1;
                i = mod289(i);
                float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0))
                                   + i.x + float3(0.0, i1.x, 1.0));
                float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy),
                               dot(x12.zw, x12.zw)), 0.0);
                m = m * m;
                m = m * m;
                float3 x = 2.0 * frac(p * C.www) - 1.0;
                float3 h = abs(x) - 0.5;
                float3 ox = floor(x + 0.5);
                float3 a0 = x - ox;
                m *= 1.79284291400159 - 0.85373472095314 * (a0*a0 + h*h);
                float3 g;
                g.x = a0.x * x0.x + h.x * x0.y;
                g.yz = a0.yz * x12.xz + h.yz * x12.yw;
                return 130.0 * dot(m, g);
            }
            
            // Detect water based on color (blue dominant)
            half IsWater(half3 color)
            {
                half blueDominance = color.b - max(color.r, color.g);
                return saturate(blueDominance * 3.0);
            }
            
            // Detect vegetation based on color (green dominant)
            half IsVegetation(half3 color)
            {
                half greenDominance = color.g - max(color.r, color.b);
                return saturate(greenDominance * 2.0);
            }
            
            // Detect roads/buildings based on color (gray/brown)
            half IsUrban(half3 color)
            {
                half grayness = 1.0 - (max(color.r, max(color.g, color.b)) - min(color.r, min(color.g, color.b)));
                return saturate(grayness * 0.5);
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.fogCoord = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                // Sample base map texture
                half4 mapColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                // Sample parchment texture for overlay
                float2 parchmentUV = IN.uv * _ParchmentTex_ST.xy + _ParchmentTex_ST.zw;
                half4 parchment = SAMPLE_TEXTURE2D(_ParchmentTex, sampler_ParchmentTex, parchmentUV);
                
                // Sample noise for effects
                float2 noiseUV = IN.uv * 5.0 + _Time.y * _ShimmerSpeed * 0.1;
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                
                // ===== Color Classification =====
                half waterMask = IsWater(mapColor.rgb);
                half vegMask = IsVegetation(mapColor.rgb);
                half urbanMask = IsUrban(mapColor.rgb);
                
                // ===== Apply Terrain-Specific Tinting =====
                half3 tintedColor = mapColor.rgb;
                
                // Apply parchment base
                tintedColor = lerp(tintedColor, tintedColor * _ParchmentTint.rgb, _FantasyIntensity * 0.5);
                
                // Tint water areas
                tintedColor = lerp(tintedColor, tintedColor * _WaterTint.rgb, waterMask * _WaterTint.a * _FantasyIntensity);
                
                // Tint vegetation areas
                tintedColor = lerp(tintedColor, tintedColor * _ForestTint.rgb, vegMask * _ForestTint.a * _FantasyIntensity);
                
                // Tint urban areas
                tintedColor = lerp(tintedColor, tintedColor * _RoadTint.rgb, urbanMask * _RoadTint.a * _FantasyIntensity);
                
                // ===== Parchment Overlay =====
                half parchmentMix = parchment.r * _FantasyIntensity * 0.3;
                tintedColor = lerp(tintedColor, tintedColor * parchment.rgb * 1.2, parchmentMix);
                
                // ===== Magical Shimmer =====
                float shimmerNoise = snoise(IN.uv * 20.0 + _Time.y * _ShimmerSpeed);
                half shimmer = shimmerNoise * _ShimmerIntensity * _FantasyIntensity;
                tintedColor += shimmer * _GlowColor.rgb;
                
                // ===== Water Wave Animation =====
                if (waterMask > 0.3)
                {
                    float waveNoise = snoise(IN.uv * 30.0 + _Time.y * 0.5);
                    half wave = waveNoise * 0.05 * waterMask;
                    tintedColor.rgb += wave * half3(0.1, 0.2, 0.3);
                }
                
                // ===== Vignette Effect =====
                float2 uvCentered = IN.uv - 0.5;
                float dist = length(uvCentered);
                half vignette = smoothstep(_VignetteRadius, _VignetteRadius - _VignetteSoftness, dist);
                vignette = lerp(1.0, vignette, _VignetteIntensity * _FantasyIntensity);
                tintedColor *= vignette;
                
                // ===== Fog of War =====
                if (_FogOfWar > 0.01)
                {
                    float distToExplored = distance(IN.positionWS.xz, _ExploredCenter.xz);
                    half fogFactor = smoothstep(_ExploredRadius, _ExploredRadius + _FogSoftness, distToExplored);
                    fogFactor *= _FogOfWar;
                    tintedColor = lerp(tintedColor, _FogColor.rgb, fogFactor * _FogColor.a);
                }
                
                // ===== Territory Overlay =====
                half4 territory = SAMPLE_TEXTURE2D(_TerritoryOverlay, sampler_TerritoryOverlay, IN.uv);
                if (territory.a > 0.01)
                {
                    half pulse = sin(_Time.y * 3.0) * 0.2 + 0.8;
                    half territoryGlow = territory.a * _TerritoryColor.a * pulse * _TerritoryPulse;
                    tintedColor = lerp(tintedColor, _TerritoryColor.rgb, territoryGlow);
                    
                    // Add edge glow
                    half edge = fwidth(territory.a) * 20.0;
                    tintedColor += _GlowColor.rgb * edge * _GlowIntensity;
                }
                
                // ===== Final Output =====
                half4 finalColor = half4(tintedColor, 1.0);
                finalColor.rgb = MixFog(finalColor.rgb, IN.fogCoord);
                
                return finalColor;
            }
            ENDHLSL
        }
        
        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };
            
            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }
            
            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
