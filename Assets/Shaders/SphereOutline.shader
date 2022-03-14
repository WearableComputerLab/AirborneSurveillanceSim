Shader "Unlit/SphereOutline"
{
    Properties
    {
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _StepMin("Step min", Range(0.0, 1.0)) = 0.3
        _DiscardThreshold("Discard threshold", Range(0.0, 1.0)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderQueue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 crap : NORMAL;
            };

            float4 _Color;
            float _StepMin;
            float _DiscardThreshold;

            v2f vert (appdata v)
            {
                float3 camToV = normalize(WorldSpaceViewDir(v.vertex));
                float3 nrm = UnityObjectToWorldNormal(v.normal);
            
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.crap = abs(dot(nrm, camToV));
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float x = smoothstep(_StepMin, 1.0, 1.0 - i.crap.z);
                if(x < _DiscardThreshold)
                    discard;
                
                float4 col = _Color * float4(1.0, 1.0, 1.0, x);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
