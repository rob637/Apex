Shader "ApexCitadels/PC/CombatEffectsShader"
{
    // Combat visual effects shader for attacks, abilities, and damage
    // Supports multiple effect types: projectiles, impacts, shields, damage indicators
    
    Properties
    {
        [Header(Effect Type)]
        [KeywordEnum(Projectile, Impact, Shield, Damage, Heal, Buff, Debuff)] _EffectType ("Effect Type", Float) = 0
        
        [Header(Colors)]
        _MainColor ("Main Color", Color) = (1, 0.5, 0.2, 1)
        _SecondaryColor ("Secondary Color", Color) = (1, 0.2, 0.1, 1)
        _CoreColor ("Core Color", Color) = (1, 1, 0.8, 1)
        
        [Header(Texture)]
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "gray" {}
        _DistortionTex ("Distortion Texture", 2D) = "bump" {}
        
        [Header(Animation)]
        _Speed ("Animation Speed", Range(0, 10)) = 2
        _ScrollSpeed ("Scroll Speed", Vector) = (0, 1, 0, 0)
        _DistortionStrength ("Distortion Strength", Range(0, 1)) = 0.2
        
        [Header(Shape)]
        _Radius ("Radius", Range(0, 2)) = 1
        _Thickness ("Thickness", Range(0, 1)) = 0.3
        _Falloff ("Edge Falloff", Range(0.1, 5)) = 2
        
        [Header(Intensity)]
        _Intensity ("Intensity", Range(0, 10)) = 2
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Glow ("Glow Amount", Range(0, 5)) = 1.5
        
        [Header(Lifetime)]
        _LifetimeProgress ("Lifetime Progress", Range(0, 1)) = 0
        _FadeInDuration ("Fade In Duration", Range(0, 0.5)) = 0.1
        _FadeOutStart ("Fade Out Start", Range(0.5, 1)) = 0.8
        
        [Header(Impact Specific)]
        _ImpactProgress ("Impact Progress", Range(0, 1)) = 0
        _RingCount ("Ring Count", Range(1, 5)) = 3
        _RingSpacing ("Ring Spacing", Range(0.1, 1)) = 0.3
        
        [Header(Shield Specific)]
        _HexScale ("Hex Scale", Range(1, 20)) = 8
        _HitPoint ("Hit Point", Vector) = (0, 0, 0, 0)
        _HitIntensity ("Hit Intensity", Range(0, 5)) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+200"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        
        Pass
        {
            Name "CombatEffect"
            
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _EFFECTTYPE_PROJECTILE _EFFECTTYPE_IMPACT _EFFECTTYPE_SHIELD _EFFECTTYPE_DAMAGE _EFFECTTYPE_HEAL _EFFECTTYPE_BUFF _EFFECTTYPE_DEBUFF
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float4 color : TEXCOORD4;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_DistortionTex);
            SAMPLER(sampler_DistortionTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainColor;
                float4 _SecondaryColor;
                float4 _CoreColor;
                float _Speed;
                float4 _ScrollSpeed;
                float _DistortionStrength;
                float _Radius;
                float _Thickness;
                float _Falloff;
                float _Intensity;
                float _Alpha;
                float _Glow;
                float _LifetimeProgress;
                float _FadeInDuration;
                float _FadeOutStart;
                float _ImpactProgress;
                float _RingCount;
                float _RingSpacing;
                float _HexScale;
                float4 _HitPoint;
                float _HitIntensity;
            CBUFFER_END
            
            // Hexagonal distance function for shield
            float hexDist(float2 p)
            {
                p = abs(p);
                return max(dot(p, float2(0.866025, 0.5)), p.y);
            }
            
            float hexGrid(float2 uv, float scale)
            {
                float2 p = uv * scale;
                float2 h = float2(1, sqrt(3.0));
                float2 a = fmod(p, h) - h * 0.5;
                float2 b = fmod(p + h * 0.5, h) - h * 0.5;
                
                float da = hexDist(a);
                float db = hexDist(b);
                
                return min(da, db);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionOS = input.positionOS.xyz;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Lifetime fade
                float fadeIn = smoothstep(0, _FadeInDuration, _LifetimeProgress);
                float fadeOut = 1.0 - smoothstep(_FadeOutStart, 1.0, _LifetimeProgress);
                float lifetimeFade = fadeIn * fadeOut;
                
                float2 uv = input.uv;
                float2 centeredUV = uv * 2 - 1;
                float dist = length(centeredUV);
                
                // Scrolling and distortion
                float2 scrolledUV = uv + _ScrollSpeed.xy * _Time.y * _Speed;
                float2 distortion = SAMPLE_TEXTURE2D(_DistortionTex, sampler_DistortionTex, scrolledUV).xy * 2 - 1;
                float2 distortedUV = uv + distortion * _DistortionStrength;
                
                float3 finalColor = float3(0, 0, 0);
                float finalAlpha = 0;
                
                #if defined(_EFFECTTYPE_PROJECTILE)
                {
                    // Projectile: elongated with trail
                    float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, distortedUV + _Time.y * _Speed).r;
                    
                    float core = smoothstep(_Radius, 0, dist);
                    float outer = smoothstep(_Radius + _Thickness, _Radius, dist);
                    
                    float trail = (1.0 - uv.y) * smoothstep(0.5, 0, abs(uv.x - 0.5));
                    trail *= noise;
                    
                    finalColor = lerp(_SecondaryColor.rgb, _CoreColor.rgb, core);
                    finalColor = lerp(_MainColor.rgb, finalColor, outer);
                    finalColor += trail * _MainColor.rgb * 0.5;
                    
                    finalAlpha = max(outer, trail * 0.5);
                }
                #elif defined(_EFFECTTYPE_IMPACT)
                {
                    // Impact: expanding rings
                    float progress = _ImpactProgress;
                    
                    for (int i = 0; i < _RingCount; i++)
                    {
                        float ringProgress = saturate(progress - i * _RingSpacing);
                        float ringRadius = ringProgress * _Radius * 2;
                        float ringThickness = _Thickness * (1.0 - ringProgress * 0.5);
                        
                        float ring = smoothstep(ringRadius + ringThickness, ringRadius, dist);
                        ring *= smoothstep(ringRadius - ringThickness, ringRadius, dist);
                        ring *= 1.0 - ringProgress; // Fade as expands
                        
                        finalColor += _MainColor.rgb * ring;
                        finalAlpha += ring;
                    }
                    
                    // Central flash
                    float flash = smoothstep(0.3, 0, dist) * (1.0 - progress * 2);
                    flash = max(flash, 0);
                    finalColor += _CoreColor.rgb * flash;
                    finalAlpha += flash;
                }
                #elif defined(_EFFECTTYPE_SHIELD)
                {
                    // Shield: hexagonal pattern with hit reaction
                    float hex = hexGrid(uv, _HexScale);
                    float hexLine = smoothstep(0.05, 0.02, hex);
                    
                    // Fresnel for edge glow
                    float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                    float fresnel = pow(1.0 - saturate(dot(viewDir, input.normalWS)), _Falloff);
                    
                    // Hit ripple
                    float hitDist = distance(input.positionOS.xyz, _HitPoint.xyz);
                    float hitRipple = sin(hitDist * 20 - _Time.y * 20) * 0.5 + 0.5;
                    hitRipple *= smoothstep(2, 0, hitDist) * _HitIntensity;
                    
                    finalColor = _MainColor.rgb * (hexLine * 0.5 + fresnel + hitRipple);
                    finalColor += _CoreColor.rgb * hitRipple;
                    
                    finalAlpha = hexLine * 0.3 + fresnel * 0.5 + hitRipple;
                }
                #elif defined(_EFFECTTYPE_DAMAGE)
                {
                    // Damage: angry red with slash marks
                    float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, distortedUV * 2).r;
                    
                    // Slash pattern
                    float slash = smoothstep(0.1, 0, abs(uv.x - uv.y - 0.5 + noise * 0.3));
                    slash += smoothstep(0.1, 0, abs(uv.x + uv.y - 1 + noise * 0.3)) * 0.7;
                    
                    float edge = smoothstep(_Radius, _Radius - _Thickness, dist);
                    
                    finalColor = _MainColor.rgb * slash;
                    finalColor += _SecondaryColor.rgb * edge * noise;
                    
                    finalAlpha = max(slash, edge * 0.5) * _Alpha;
                }
                #elif defined(_EFFECTTYPE_HEAL)
                {
                    // Heal: gentle green spiraling particles
                    float angle = atan2(centeredUV.y, centeredUV.x);
                    float spiral = frac(angle / 6.28318 + dist * 3 - _Time.y * _Speed);
                    
                    float particles = smoothstep(0.3, 0.5, spiral) * smoothstep(0.7, 0.5, spiral);
                    particles *= smoothstep(_Radius, 0, dist);
                    
                    float rising = sin(uv.y * 10 + _Time.y * _Speed * 2) * 0.5 + 0.5;
                    particles *= rising;
                    
                    float core = smoothstep(0.3, 0, dist);
                    
                    finalColor = _MainColor.rgb * particles + _CoreColor.rgb * core;
                    finalAlpha = particles + core * 0.5;
                }
                #elif defined(_EFFECTTYPE_BUFF)
                {
                    // Buff: upward flowing energy
                    float2 flowUV = uv;
                    flowUV.y = frac(flowUV.y - _Time.y * _Speed);
                    
                    float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, flowUV * 3).r;
                    float flow = smoothstep(0.3, 0.7, noise);
                    flow *= smoothstep(0.5, 0.3, abs(uv.x - 0.5));
                    flow *= uv.y; // Fade at bottom
                    
                    float sparkle = pow(noise, 4) * 2;
                    
                    finalColor = _MainColor.rgb * flow + _CoreColor.rgb * sparkle;
                    finalAlpha = flow + sparkle * 0.3;
                }
                #elif defined(_EFFECTTYPE_DEBUFF)
                {
                    // Debuff: dripping dark energy
                    float2 dripUV = uv;
                    dripUV.y = frac(dripUV.y + _Time.y * _Speed * 0.5);
                    
                    float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, dripUV * 2).r;
                    float drip = smoothstep(0.6, 0.4, noise);
                    drip *= smoothstep(0.4, 0.2, abs(uv.x - 0.5));
                    drip *= 1.0 - uv.y; // Stronger at top
                    
                    // Dark tendrils
                    float tendril = sin(uv.x * 20 + noise * 5) * 0.5 + 0.5;
                    tendril *= 1.0 - uv.y;
                    
                    finalColor = _MainColor.rgb * drip + _SecondaryColor.rgb * tendril * 0.5;
                    finalAlpha = drip + tendril * 0.2;
                }
                #endif
                
                // Apply intensity and glow
                finalColor *= _Intensity;
                finalColor += finalColor * _Glow * finalAlpha;
                
                // Apply lifetime fade
                finalAlpha *= lifetimeFade * _Alpha;
                
                // Vertex color tint
                finalColor *= input.color.rgb;
                finalAlpha *= input.color.a;
                
                return half4(finalColor, saturate(finalAlpha));
            }
            ENDHLSL
        }
    }
    
    FallBack "Sprites/Default"
}
