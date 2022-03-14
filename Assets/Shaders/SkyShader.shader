Shader "Unlit/SkyShader"
{
    Properties
    {
        _MainCubeMap ("Main cubemap", Cube) = "" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 viewDir : TEXCOORD3;
            };

            samplerCUBE _MainCubeMap;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.viewDir = -WorldSpaceViewDir(v.vertex);
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return texCUBE(_MainCubeMap, normalize(i.viewDir));
            }
            ENDCG
        }
    }
}
