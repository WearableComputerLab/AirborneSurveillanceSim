Shader "Custom/WaterShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Tiling ("Tiling", Vector) = (1.0,1.0,0.0,0.0)
        _RotSpeed ("Rotation speed", Float) = 0.05
        _RandomRotScale ("Random rot scale", Float) = 0.1
        _BorderWaveScale ("Border wave scale", Float) = 1.0
        _BorderWaveStrength ("Border wave strength", Range(0, 1)) = 0.3
        _NormalStrength ("Normal map strength", Range(0,1)) = 1.0
        [Normal] _NormalMap ("Normal map", 2D) = "purple" {}
        [Toggle] _RandomRot ("Rotate randomly", Float) = 0.0
        [Toggle] _RecomputeNormals ("Recompute normals", Float) = 0.0
        [Toggle] _DebugOrigin ("Debug origin", Float) = 0.0
        _Simplex3D ("Noise texture", 2D) = "black" {}
    }
    SubShader
    {
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma shader_feature _RANDOMROT_ON
        #pragma shader_feature _RECOMPUTENORMALS_ON
        #pragma shader_feature _DEBUGORIGIN_ON

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _NormalMap;
        sampler2D _Simplex3D;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float4 _Tiling;
        float _RotSpeed;
        float _RandomRotScale;
        float _BorderWaveScale;
        float _BorderWaveStrength;
        float _NormalStrength;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)
        
        #if 1
        float rand(float2 n)
        { 
            return frac(sin(dot(n, float2(12.9898, 4.1414))) * 43758.5453);
        }
        #else
        float rand(float2 n)
        {
            return tex2D(_Noise, (floor(n * 255.0) + 0.5) / 256.0).r;
        }
        #endif
        
        float rand1d(float n)
        {
            return rand(float2(n / 179.0, fmod(n, 179.0)));
        }
                        
        float noise1d(float p)
        {
            float ip = floor(p);
            float u = frac(p);
            
            u = u * u * (3.0 - 2.0 * u);
            return lerp(rand1d(ip), rand1d(ip + 1.0), u);
        }
        
        float noise2d(float2 p)
        {
            float2 ip = floor(p);
            float2 u = frac(p);
            
            u = u * u * (3.0 - 2.0 * u);

            return lerp(
                lerp(rand(ip                   ), rand(ip + float2(1.0, 0.0)), u.x),
                lerp(rand(ip + float2(0.0, 1.0)), rand(ip + float2(1.0, 1.0)), u.x),
                u.y);
        }
        
        float3 sampleNormal(float2 pos, float2 coords)
        {
            float rot = rand(pos + float2(506.62, 768.06)) * 2.0 * 3.14159;
            float2x2 rotMatrix = {
                cos(rot), -sin(rot),
                sin(rot),  cos(rot)
            };
            
            float2 rotCoords = mul(rotMatrix, coords);
            return UnpackNormal(tex2D(_NormalMap, rotCoords));
        }
        
        float3 rotNoise(float2 scaledCoords, float2 coords)
        {
            float2 u = frac(scaledCoords);
            float2 ip = floor(scaledCoords);
            
            //u = u * u * (3.0 - 2.0 * u);
            
            return lerp(
                lerp(sampleNormal(ip,                    coords), sampleNormal(ip + float2(1.0, 0.0), coords), u.x),
                lerp(sampleNormal(ip + float2(0.0, 1.0), coords), sampleNormal(ip + float2(1.0, 1.0), coords), u.x),
                u.y);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float testAngle = _Time.y * _RotSpeed;
            
            float2x2 rotMatrix = {
                cos(testAngle), -sin(testAngle),
                sin(testAngle),  cos(testAngle)
            };
            
            float2 coords = mul(rotMatrix, IN.uv_MainTex - 0.5) * _Tiling.xy;
            
            fixed4 c = _Color;
            
            #if _RANDOMROT_ON
                float border = float2(noise1d(coords.y * _BorderWaveScale + 876.21), noise1d(coords.x * _BorderWaveScale + 173.2));
                float alpha = 0.05;
                
                float2 scaledCoords = coords * _RandomRotScale + (border * 2.0 - 1.0) * _BorderWaveStrength;
                    
                //This is costful but it's the only way I have found to reduce aliasing on the borders
                float3 nrm1 = rotNoise(scaledCoords + float2(alpha, 0.0), coords);
                float3 nrm2 = rotNoise(scaledCoords - float2(alpha, 0.0), coords);
                float3 nrm3 = rotNoise(scaledCoords + float2(0.0, alpha), coords);
                float3 nrm4 = rotNoise(scaledCoords - float2(0.0, alpha), coords);
                
                float3 nrm = (nrm1 + nrm2 + nrm3 + nrm4) * 0.25;
            #else
                float3 nrm = UnpackNormal(tex2D(_NormalMap, coords));
            #endif
            
            float nrmStrength = lerp(0.6, 1.0, noise2d(coords * 2.0 + float2(649.6, 901.21)));
            nrm = normalize(lerp(float3(0.0, 0.0, 1.0), nrm, nrmStrength * _NormalStrength));
        
            #if _DEBUGORIGIN_ON
                float testX = 1.0 - smoothstep(0.0, 1.0, abs(coords.x));
                float testY = 1.0 - smoothstep(0.0, 1.0, abs(coords.y));
                float testCombined = 1.0 - saturate(testX + testY);
                o.Albedo = float4(c.rgb * testCombined, 1.0);
            #else
                o.Albedo = c.rgb;
            #endif
            
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            o.Normal = nrm;
        }
        
        float4 getNoiseData(float2 xz)
        {
            xz = floor(xz / 0.0078125);
            xz = xz / 64.0;
            xz = xz * 0.5 + 0.5;
            
            return tex2Dlod(_Simplex3D, float4(xz.x, xz.y, 0.0, 0.0));
        }
        
        void vert(inout appdata_full v)
        {
            float4 noiseData = getNoiseData(v.vertex.xz);   
            v.vertex = v.vertex + float4(0.0, noiseData.r * 0.005, 0.0, 0.0);
            
            #if _RECOMPUTENORMALS_ON
                float3 nrm = noiseData.gba;
                
                if(dot(nrm, nrm) > 0.1)
                    v.normal = noiseData.gba;
            #endif
            
            UNITY_TRANSFER_DEPTH(v);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
