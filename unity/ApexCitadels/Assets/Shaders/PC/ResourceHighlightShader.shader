Shader "ApexCitadels/PC/ResourceHighlightShader"
{
    // Shader for highlighting resources, collectibles, and interactable objects
    // Features pulsing glow, outline, and particle-like effects
    
    Properties
    {
        [Header(Base)]
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        
        [Header(Resource Types)]
        _GoldColor ("Gold Color", Color) = (1, 0.85, 0.3, 1)
        _WoodColor ("Wood Color", Color) = (0.6, 0.4, 0.2, 1)
        _StoneColor ("Stone Color", Color) = (0.5, 0.5, 0.55, 1)
        _FoodColor ("Food Color", Color) = (0.3, 0.8, 0.3, 1)
        _ManaColor ("Mana Color", Color) = (0.5, 0.3, 1, 1)
        _RareColor ("Rare Color", Color) = (1, 0.5, 1, 1)
        
        [Header(Highlight Effect)]
        _ResourceType ("Resource Type", Range(0, 5)) = 0
        _HighlightIntensity ("Highlight Intensity", Range(0, 5)) = 1.5
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.3
        
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
        _OutlineGlow ("Outline Glow", Range(0, 3)) = 1
        
        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(0.5, 5)) = 2
        _FresnelIntensity ("Fresnel Intensity", Range(0, 3)) = 1
        
        [Header(Sparkle)]
        _SparkleIntensity ("Sparkle Intensity", Range(0, 2)) = 0.5
        _SparkleSpeed ("Sparkle Speed", Range(0, 10)) = 3
        _SparkleScale ("Sparkle Scale", Range(0.1, 10)) = 2
        
        [Header(Discovery)]
        _DiscoveryProgress ("Discovery Progress", Range(0, 1)) = 1
        _DiscoveryGlow ("Discovery Glow Intensity", Range(0, 5)) = 3
        
        [Header(Interaction)]
        _IsHovered ("Is Hovered", Range(0, 1)) = 0
        _IsSelected ("Is Selected", Range(0, 1)) = 0
        _HoverBoost ("Hover Boost", Range(1, 3)) = 1.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "Queue" = "Geometry+50"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        // Main pass with highlight
        Pass
        {
            Name "ResourceHighlight"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _GoldColor;
                float4 _WoodColor;
                float4 _StoneColor;
                float4 _FoodColor;
                float4 _ManaColor;
                float4 _RareColor;
                float _ResourceType;
                float _HighlightIntensity;
                float _PulseSpeed;
                float _PulseAmount;
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineGlow;
                float _FresnelPower;
                float _FresnelIntensity;
                float _SparkleIntensity;
                float _SparkleSpeed;
                float _SparkleScale;
                float _DiscoveryProgress;
                float _DiscoveryGlow;
                float _IsHovered;
                float _IsSelected;
                float _HoverBoost;
            CBUFFER_END
            
            // Hash function for sparkles
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            
            float sparkle(float2 uv, float time)
            {
                float2 grid = floor(uv * _SparkleScale);
                float2 gridUV = frac(uv * _SparkleScale);
                
                float h = hash(grid);
                float sparkleTime = frac(time * _SparkleSpeed + h * 10);
                
                // Sparkle only appears briefly
                float sparkleWindow = smoothstep(0, 0.1, sparkleTime) * smoothstep(0.3, 0.2, sparkleTime);
                
                // Distance from center of grid cell
                float2 center = float2(0.5, 0.5);
                float dist = length(gridUV - center);
                float sparkleShape = smoothstep(0.3, 0, dist);
                
                return sparkleWindow * sparkleShape * h;
            }
            
            float4 GetResourceColor(float resourceType)
            {
                // 0: Gold, 1: Wood, 2: Stone, 3: Food, 4: Mana, 5: Rare
                if (resourceType < 0.5) return _GoldColor;
                if (resourceType < 1.5) return _WoodColor;
                if (resourceType < 2.5) return _StoneColor;
                if (resourceType < 3.5) return _FoodColor;
                if (resourceType < 4.5) return _ManaColor;
                return _RareColor;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample base texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 resourceColor = GetResourceColor(_ResourceType);
                
                // Base color with resource tint
                float3 baseColor = texColor.rgb * _BaseColor.rgb;
                baseColor = lerp(baseColor, baseColor * resourceColor.rgb, 0.5);
                
                // Lighting
                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 diffuse = mainLight.color * NdotL;
                
                float3 ambient = SampleSH(normalWS);
                float3 litColor = baseColor * (diffuse + ambient);
                
                // Pulse effect
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                
                // Fresnel rim light
                float fresnel = pow(1.0 - saturate(dot(input.viewDirWS, normalWS)), _FresnelPower);
                float3 fresnelColor = resourceColor.rgb * fresnel * _FresnelIntensity;
                
                // Sparkle effect
                float sparkleVal = sparkle(input.uv, _Time.y);
                sparkleVal += sparkle(input.uv + 0.3, _Time.y + 0.5) * 0.5;
                float3 sparkleColor = float3(1, 1, 1) * sparkleVal * _SparkleIntensity;
                
                // Discovery reveal effect
                float discoveryGlow = 0;
                if (_DiscoveryProgress < 1.0)
                {
                    float revealEdge = abs(input.positionWS.y - _DiscoveryProgress * 5.0);
                    discoveryGlow = smoothstep(0.5, 0, revealEdge) * _DiscoveryGlow;
                }
                
                // Hover and selection effects
                float interactionBoost = 1.0;
                if (_IsSelected > 0.5)
                {
                    interactionBoost = _HoverBoost * 1.2;
                    fresnelColor *= 2.0;
                }
                else if (_IsHovered > 0.5)
                {
                    interactionBoost = _HoverBoost;
                }
                
                // Combine all effects
                float3 finalColor = litColor;
                finalColor += fresnelColor * pulse;
                finalColor += sparkleColor;
                finalColor += resourceColor.rgb * discoveryGlow;
                finalColor *= interactionBoost;
                finalColor *= _HighlightIntensity;
                
                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, texColor.a);
            }
            ENDHLSL
        }
        
        // Outline pass
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            
            Cull Front
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineGlow;
                float _ResourceType;
                float4 _GoldColor;
                float4 _WoodColor;
                float4 _StoneColor;
                float4 _FoodColor;
                float4 _ManaColor;
                float4 _RareColor;
                float _PulseSpeed;
                float _PulseAmount;
                float _IsHovered;
                float _IsSelected;
            CBUFFER_END
            
            float4 GetResourceColor(float resourceType)
            {
                if (resourceType < 0.5) return _GoldColor;
                if (resourceType < 1.5) return _WoodColor;
                if (resourceType < 2.5) return _StoneColor;
                if (resourceType < 3.5) return _FoodColor;
                if (resourceType < 4.5) return _ManaColor;
                return _RareColor;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Expand along normal for outline
                float outlineWidth = _OutlineWidth;
                if (_IsSelected > 0.5) outlineWidth *= 1.5;
                else if (_IsHovered > 0.5) outlineWidth *= 1.2;
                
                float3 posOS = input.positionOS.xyz + input.normalOS * outlineWidth;
                
                output.positionWS = TransformObjectToWorld(posOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float4 resourceColor = GetResourceColor(_ResourceType);
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                
                float3 outlineColor = lerp(_OutlineColor.rgb, resourceColor.rgb, 0.5);
                outlineColor *= _OutlineGlow * pulse;
                
                if (_IsSelected > 0.5)
                {
                    outlineColor *= 1.5;
                }
                else if (_IsHovered > 0.5)
                {
                    outlineColor *= 1.2;
                }
                
                return half4(outlineColor, 1);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
