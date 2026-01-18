Shader "ApexCitadels/PC/TerritoryBorderShader"
{
    // Territory border shader with animated effects
    // Creates visible boundaries between player territories
    
    Properties
    {
        [Header(Colors)]
        _OwnerColor ("Owner Color", Color) = (0.2, 0.5, 1, 1)
        _HostileColor ("Hostile Color", Color) = (1, 0.2, 0.2, 1)
        _NeutralColor ("Neutral Color", Color) = (0.7, 0.7, 0.7, 1)
        _ContestedColor ("Contested Color", Color) = (1, 0.8, 0.2, 1)
        
        [Header(Border Settings)]
        _BorderWidth ("Border Width", Range(0.1, 5)) = 1
        _BorderFalloff ("Border Falloff", Range(0.1, 3)) = 1
        _BorderHeight ("Border Height", Range(0, 10)) = 2
        
        [Header(Animation)]
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.3
        _FlowSpeed ("Flow Speed", Range(0, 5)) = 1
        _WaveAmplitude ("Wave Amplitude", Range(0, 2)) = 0.5
        _WaveFrequency ("Wave Frequency", Range(0.1, 10)) = 2
        
        [Header(Effects)]
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 2
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.3
        _Shimmer ("Shimmer Intensity", Range(0, 2)) = 0.5
        
        [Header(Visibility)]
        _FadeDistance ("Fade Distance", Range(10, 500)) = 100
        _MinAlpha ("Minimum Alpha", Range(0, 1)) = 0.1
        _MaxAlpha ("Maximum Alpha", Range(0, 1)) = 0.8
        
        [Header(Combat Mode)]
        _CombatMode ("Combat Mode", Range(0, 1)) = 0
        _CombatPulse ("Combat Pulse Speed", Range(1, 10)) = 3
        _CombatIntensity ("Combat Intensity", Range(1, 5)) = 2
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        
        Pass
        {
            Name "TerritoryBorder"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 color : COLOR; // Vertex color for territory ownership
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 vertexColor : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _OwnerColor;
                float4 _HostileColor;
                float4 _NeutralColor;
                float4 _ContestedColor;
                float _BorderWidth;
                float _BorderFalloff;
                float _BorderHeight;
                float _PulseSpeed;
                float _PulseAmount;
                float _FlowSpeed;
                float _WaveAmplitude;
                float _WaveFrequency;
                float _GlowIntensity;
                float _NoiseScale;
                float _NoiseStrength;
                float _Shimmer;
                float _FadeDistance;
                float _MinAlpha;
                float _MaxAlpha;
                float _CombatMode;
                float _CombatPulse;
                float _CombatIntensity;
            CBUFFER_END
            
            // Simplex noise functions
            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 permute(float3 x) { return mod289(((x*34.0)+1.0)*x); }
            
            float snoise(float2 v)
            {
                const float4 C = float4(0.211324865405187, 0.366025403784439,
                                       -0.577350269189626, 0.024390243902439);
                float2 i = floor(v + dot(v, C.yy));
                float2 x0 = v - i + dot(i, C.xx);
                
                float2 i1;
                i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
                float4 x12 = x0.xyxy + C.xxzz;
                x12.xy -= i1;
                
                i = mod289(i);
                float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0))
                    + i.x + float3(0.0, i1.x, 1.0));
                
                float3 m = max(0.5 - float3(dot(x0,x0), dot(x12.xy,x12.xy), dot(x12.zw,x12.zw)), 0.0);
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
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 posOS = input.positionOS.xyz;
                
                // Apply wave animation along border
                float wave = sin(_Time.y * _FlowSpeed + posOS.x * _WaveFrequency) * _WaveAmplitude;
                wave += sin(_Time.y * _FlowSpeed * 0.7 + posOS.z * _WaveFrequency * 1.3) * _WaveAmplitude * 0.5;
                posOS.y += wave * input.uv.y; // Only affect upper vertices
                
                // Add noise-based displacement
                float noise = snoise(posOS.xz * _NoiseScale + _Time.y * 0.5) * _NoiseStrength;
                posOS.xz += input.normalOS.xz * noise * input.uv.y;
                
                output.positionWS = TransformObjectToWorld(posOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.vertexColor = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Decode territory state from vertex color
                // R: Owner (1) vs Enemy (0)
                // G: Contested (1) vs Clear (0)
                // B: Visibility/strength
                // A: Border edge factor
                
                float isOwner = input.vertexColor.r;
                float isContested = input.vertexColor.g;
                float borderStrength = input.vertexColor.b;
                float edgeFactor = input.vertexColor.a;
                
                // Select base color
                float4 baseColor = lerp(
                    lerp(_NeutralColor, _HostileColor, 1 - isOwner),
                    _OwnerColor,
                    isOwner
                );
                
                // Apply contested overlay
                baseColor = lerp(baseColor, _ContestedColor, isContested * 0.5);
                
                // Combat mode intensifies everything
                float combatPulse = 1.0;
                if (_CombatMode > 0.5)
                {
                    combatPulse = 1.0 + sin(_Time.y * _CombatPulse) * 0.5 * _CombatIntensity;
                    baseColor.rgb *= combatPulse;
                }
                
                // Calculate distance fade
                float3 camPos = _WorldSpaceCameraPos;
                float dist = distance(input.positionWS, camPos);
                float distanceFade = 1.0 - saturate((dist - _FadeDistance * 0.5) / (_FadeDistance * 0.5));
                
                // Pulse animation
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                
                // Shimmer effect based on view angle
                float3 viewDir = normalize(camPos - input.positionWS);
                float fresnel = pow(1.0 - saturate(dot(viewDir, input.normalWS)), 2);
                float shimmer = snoise(input.positionWS.xz * 5 + _Time.y * 2) * _Shimmer * fresnel;
                
                // Edge glow
                float edgeGlow = pow(edgeFactor, _BorderFalloff) * _GlowIntensity;
                
                // Height-based fade (fade out at top)
                float heightFade = 1.0 - pow(input.uv.y, 2);
                
                // Flowing energy lines
                float2 flowUV = input.uv;
                flowUV.y += _Time.y * _FlowSpeed * 0.1;
                float energyLines = sin(flowUV.y * 50) * 0.5 + 0.5;
                energyLines = pow(energyLines, 4);
                
                // Combine all effects
                float3 finalColor = baseColor.rgb;
                finalColor += baseColor.rgb * edgeGlow;
                finalColor += shimmer;
                finalColor += energyLines * baseColor.rgb * 0.3;
                finalColor *= pulse;
                
                // Calculate final alpha
                float alpha = borderStrength;
                alpha *= heightFade;
                alpha *= distanceFade;
                alpha = lerp(_MinAlpha, _MaxAlpha, alpha);
                alpha *= pulse;
                
                // Combat mode boost
                if (_CombatMode > 0.5)
                {
                    alpha = saturate(alpha * _CombatIntensity);
                }
                
                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Sprites/Default"
}
