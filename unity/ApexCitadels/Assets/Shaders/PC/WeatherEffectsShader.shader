Shader "ApexCitadels/PC/WeatherEffectsShader"
{
    // Weather and environmental effects shader
    // Rain, snow, fog, dust storms, day/night transitions
    
    Properties
    {
        [Header(Weather Type)]
        [KeywordEnum(Rain, Snow, Fog, Dust, Wind, Storm)] _WeatherType ("Weather Type", Float) = 0
        
        [Header(Precipitation)]
        _ParticleColor ("Particle Color", Color) = (0.7, 0.8, 1, 0.5)
        _ParticleDensity ("Particle Density", Range(0, 100)) = 50
        _ParticleSize ("Particle Size", Range(0.001, 0.1)) = 0.02
        _ParticleSpeed ("Fall Speed", Range(0.1, 10)) = 3
        _ParticleStretch ("Stretch Amount", Range(1, 20)) = 5
        
        [Header(Wind)]
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0.3, 0)
        _WindStrength ("Wind Strength", Range(0, 5)) = 1
        _WindTurbulence ("Wind Turbulence", Range(0, 2)) = 0.5
        
        [Header(Fog)]
        _FogColor ("Fog Color", Color) = (0.7, 0.75, 0.8, 1)
        _FogDensity ("Fog Density", Range(0, 1)) = 0.3
        _FogHeight ("Fog Height", Range(0, 100)) = 20
        _FogFalloff ("Fog Falloff", Range(0.1, 5)) = 1
        
        [Header(Ground Effects)]
        _WetnessFactor ("Wetness Factor", Range(0, 1)) = 0
        _PuddleAmount ("Puddle Amount", Range(0, 1)) = 0
        _SnowAccumulation ("Snow Accumulation", Range(0, 1)) = 0
        
        [Header(Lighting)]
        _AmbientBoost ("Ambient Boost", Range(0, 2)) = 1
        _LightScattering ("Light Scattering", Range(0, 2)) = 0.5
        _CloudShadow ("Cloud Shadow", Range(0, 1)) = 0
        
        [Header(Storm Effects)]
        _LightningIntensity ("Lightning Intensity", Range(0, 10)) = 0
        _LightningColor ("Lightning Color", Color) = (0.8, 0.9, 1, 1)
        _ThunderRumble ("Thunder Rumble", Range(0, 1)) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+300"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "WeatherParticles"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _WEATHERTYPE_RAIN _WEATHERTYPE_SNOW _WEATHERTYPE_FOG _WEATHERTYPE_DUST _WEATHERTYPE_WIND _WEATHERTYPE_STORM
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _ParticleColor;
                float _ParticleDensity;
                float _ParticleSize;
                float _ParticleSpeed;
                float _ParticleStretch;
                float4 _WindDirection;
                float _WindStrength;
                float _WindTurbulence;
                float4 _FogColor;
                float _FogDensity;
                float _FogHeight;
                float _FogFalloff;
                float _WetnessFactor;
                float _PuddleAmount;
                float _SnowAccumulation;
                float _AmbientBoost;
                float _LightScattering;
                float _CloudShadow;
                float _LightningIntensity;
                float4 _LightningColor;
                float _ThunderRumble;
            CBUFFER_END
            
            // Hash functions
            float hash(float n) { return frac(sin(n) * 43758.5453); }
            float hash2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            
            float3 hash3(float2 p)
            {
                float3 q = float3(
                    dot(p, float2(127.1, 311.7)),
                    dot(p, float2(269.5, 183.3)),
                    dot(p, float2(419.2, 371.9))
                );
                return frac(sin(q) * 43758.5453);
            }
            
            // Noise function
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash2(i);
                float b = hash2(i + float2(1, 0));
                float c = hash2(i + float2(0, 1));
                float d = hash2(i + float2(1, 1));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Rain drop
            float rainDrop(float2 uv, float2 offset, float speed, float time)
            {
                float2 p = uv + offset;
                p.y = frac(p.y - time * speed);
                
                float drop = smoothstep(0.5 - _ParticleSize, 0.5, p.y);
                drop *= smoothstep(0.5 + _ParticleSize * _ParticleStretch, 0.5, p.y);
                drop *= smoothstep(_ParticleSize, 0, abs(p.x - 0.5));
                
                return drop;
            }
            
            // Snowflake
            float snowflake(float2 uv, float2 offset, float speed, float time, float size)
            {
                float2 p = uv + offset;
                
                // Slower fall with side-to-side movement
                p.y = frac(p.y - time * speed * 0.3);
                p.x += sin(p.y * 10 + time * 2 + offset.x * 50) * 0.1 * _WindStrength;
                
                float dist = length(p - 0.5);
                float flake = smoothstep(size, size * 0.5, dist);
                
                return flake;
            }
            
            // Dust particle
            float dustParticle(float2 uv, float2 offset, float time)
            {
                float2 p = uv + offset;
                
                // Horizontal movement
                p.x = frac(p.x - time * _ParticleSpeed * _WindStrength);
                p.y += sin(p.x * 5 + time) * 0.1 * _WindTurbulence;
                
                float dist = length(p - 0.5);
                float dust = smoothstep(_ParticleSize, _ParticleSize * 0.3, dist);
                
                return dust * 0.5;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                float3 finalColor = float3(0, 0, 0);
                float finalAlpha = 0;
                
                float time = _Time.y;
                
                #if defined(_WEATHERTYPE_RAIN) || defined(_WEATHERTYPE_STORM)
                {
                    // Multiple rain layers
                    float rain = 0;
                    
                    for (int layer = 0; layer < 3; layer++)
                    {
                        float layerScale = 1.0 + layer * 0.5;
                        float layerSpeed = _ParticleSpeed * (1.0 - layer * 0.2);
                        float layerAlpha = 1.0 - layer * 0.3;
                        
                        float2 layerUV = uv * _ParticleDensity * layerScale;
                        
                        // Wind offset
                        layerUV.x += time * _WindStrength * _WindDirection.x;
                        
                        // Grid of rain drops
                        float2 gridId = floor(layerUV);
                        float2 gridUV = frac(layerUV);
                        
                        float randomOffset = hash2(gridId) * 10;
                        rain += rainDrop(gridUV, float2(hash(gridId.x), 0), layerSpeed, time + randomOffset) * layerAlpha;
                    }
                    
                    finalColor = _ParticleColor.rgb;
                    finalAlpha = rain * _ParticleColor.a;
                    
                    #if defined(_WEATHERTYPE_STORM)
                    {
                        // Lightning flash
                        float lightning = smoothstep(0.99, 1, sin(time * 0.3) * sin(time * 0.7));
                        lightning *= _LightningIntensity;
                        
                        finalColor += _LightningColor.rgb * lightning;
                        finalAlpha = saturate(finalAlpha + lightning * 0.3);
                    }
                    #endif
                }
                #elif defined(_WEATHERTYPE_SNOW)
                {
                    float snow = 0;
                    
                    for (int layer = 0; layer < 4; layer++)
                    {
                        float layerScale = 0.5 + layer * 0.3;
                        float layerSize = _ParticleSize * (1.5 - layer * 0.3);
                        float layerAlpha = 1.0 - layer * 0.2;
                        
                        float2 layerUV = uv * _ParticleDensity * layerScale;
                        layerUV.x += time * _WindStrength * _WindDirection.x * (1 + layer * 0.2);
                        
                        float2 gridId = floor(layerUV);
                        float2 gridUV = frac(layerUV);
                        
                        float3 rnd = hash3(gridId);
                        snow += snowflake(gridUV, float2(rnd.x, 0), _ParticleSpeed, time + rnd.z * 10, layerSize) * layerAlpha;
                    }
                    
                    finalColor = float3(1, 1, 1);
                    finalAlpha = snow * _ParticleColor.a;
                }
                #elif defined(_WEATHERTYPE_FOG)
                {
                    // Volumetric fog effect
                    float heightFactor = 1.0 - saturate((input.positionWS.y - 0) / _FogHeight);
                    heightFactor = pow(heightFactor, _FogFalloff);
                    
                    // Animated fog wisps
                    float2 fogUV = input.positionWS.xz * 0.01;
                    fogUV += _WindDirection.xz * time * _WindStrength * 0.1;
                    
                    float fogNoise = noise(fogUV * 5);
                    fogNoise += noise(fogUV * 10 + time * 0.1) * 0.5;
                    fogNoise = fogNoise * 0.5 + 0.5;
                    
                    float fog = heightFactor * _FogDensity * fogNoise;
                    
                    finalColor = _FogColor.rgb;
                    finalAlpha = fog;
                    
                    // Light scattering
                    float scatter = _LightScattering * heightFactor;
                    finalColor += float3(1, 0.95, 0.9) * scatter * 0.2;
                }
                #elif defined(_WEATHERTYPE_DUST)
                {
                    float dust = 0;
                    
                    for (int layer = 0; layer < 3; layer++)
                    {
                        float layerScale = 1.0 + layer * 0.7;
                        
                        float2 layerUV = uv * _ParticleDensity * layerScale;
                        
                        float2 gridId = floor(layerUV);
                        float2 gridUV = frac(layerUV);
                        
                        float2 rnd = hash3(gridId).xy;
                        dust += dustParticle(gridUV, rnd, time) * (1.0 - layer * 0.25);
                    }
                    
                    // Overall dust haze
                    float haze = noise(input.positionWS.xz * 0.02 + time * _WindStrength * 0.1);
                    haze = haze * 0.3 * _FogDensity;
                    
                    finalColor = _ParticleColor.rgb;
                    finalAlpha = (dust + haze) * _ParticleColor.a;
                }
                #elif defined(_WEATHERTYPE_WIND)
                {
                    // Wind streaks/leaves
                    float wind = 0;
                    
                    for (int layer = 0; layer < 2; layer++)
                    {
                        float2 windUV = uv * _ParticleDensity * (1 + layer);
                        windUV.x = frac(windUV.x - time * _ParticleSpeed * _WindStrength);
                        windUV.y += sin(windUV.x * 10 + time * 5) * _WindTurbulence * 0.2;
                        
                        float2 gridId = floor(windUV);
                        float2 gridUV = frac(windUV);
                        
                        float streak = smoothstep(0.4, 0.5, gridUV.x) * smoothstep(0.6, 0.5, gridUV.x);
                        streak *= smoothstep(0.3, 0.5, gridUV.y) * smoothstep(0.7, 0.5, gridUV.y);
                        streak *= hash2(gridId) > 0.7 ? 1 : 0;
                        
                        wind += streak * (1.0 - layer * 0.4);
                    }
                    
                    finalColor = _ParticleColor.rgb;
                    finalAlpha = wind * _ParticleColor.a * 0.5;
                }
                #endif
                
                // Cloud shadows
                if (_CloudShadow > 0)
                {
                    float2 cloudUV = input.positionWS.xz * 0.005 + time * _WindStrength * 0.01;
                    float cloud = noise(cloudUV * 3) * noise(cloudUV * 7);
                    cloud = smoothstep(0.3, 0.7, cloud);
                    
                    finalColor *= 1.0 - cloud * _CloudShadow * 0.5;
                }
                
                // Ambient modification
                finalColor *= _AmbientBoost;
                
                return half4(finalColor, saturate(finalAlpha));
            }
            ENDHLSL
        }
    }
    
    FallBack "Sprites/Default"
}
