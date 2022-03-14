Shader "Unlit/Bordered solid"
{
    Properties
    {
        _Color ("Background color", Color) = (1.0,1.0,1.0,1.0)
        _BorderColor ("Border color", Color) = (1.0,1.0,1.0,1.0)
        _BorderSize ("Border size", float) = 0.1
        _BorderTransition ("Border transition", float) = 0.01
    }
    SubShader
    {
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            fixed4 _BorderColor;
            float _BorderSize;
            float _BorderTransition;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float borderL = 1.0 - smoothstep(_BorderSize - _BorderTransition, _BorderSize + _BorderTransition, i.uv.x);
                float borderR = smoothstep(1.0 - _BorderSize - _BorderTransition, 1.0 - _BorderSize + _BorderTransition, i.uv.x);
                float borderT = 1.0 - smoothstep(_BorderSize - _BorderTransition, _BorderSize + _BorderTransition, i.uv.y);
                float borderB = smoothstep(1.0 - _BorderSize - _BorderTransition, 1.0 - _BorderSize + _BorderTransition, i.uv.y);
                float border = max(max(max(borderL, borderR), borderT), borderB);
                
                fixed4 col = lerp(_Color, _BorderColor, border);
                
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
            
            struct v2f
            { 
                V2F_SHADOW_CASTER;
            };
            
            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            
            float4 frag(v2f i) : COLOR
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            
            ENDCG
        }
    }
}
