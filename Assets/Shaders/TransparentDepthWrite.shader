Shader "Unlit/TransparentDepthWrite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HDRMultiplier ("HDR Multiplier", Float) = 1.0
        _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags {
            "RenderType"="Transparent"
            //The secret to make the ShadowCaster pass work is to set the queue to AlphaTest and not Transparent
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
        }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
        
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _HDRMultiplier;
            float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 col = tex2D(_MainTex, i.uv);
                col = float4(col.rgb * _HDRMultiplier, col.a * _Alpha);
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma fragmentoption ARB_precision_hint_fastest
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            { 
                float2 uv : TEXCOORD0;
                V2F_SHADOW_CASTER;
            };
            
            sampler2D _MainTex;
            float _Dither;
            float _DepthDiscardThreshold;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.uv = v.uv;
                
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            
            float4 frag(v2f i) : COLOR
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                if(color.a <= 0.1)
                    discard;
            
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
