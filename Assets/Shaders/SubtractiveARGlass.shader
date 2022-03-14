Shader "AR Glass/Subtractive"
{
    Properties
    {
        _MainTex ("Mono/left texture", 2D) = "white" {}
        _RightTex ("Right texture", 2D) = "white" {}
        _BgMultWhenWhite ("Background multiplier when white", Range(0.0, 1.0)) = 0.9
        _BgMultWhenBlack ("Background multiplier when black", Range(0.0, 1.0)) = 0.1
        [Toggle] _Stereo ("Stereo", Float) = 0.0
        [Toggle] _OverrideDepth ("Override depth", Float) = 0.0
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
            Blend DstColor SrcAlpha
        
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma shader_feature _STEREO_ON

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
            float _BgMultWhenWhite;
            float _BgMultWhenBlack;
            
            #if _STEREO_ON
                sampler2D _RightTex;
                float4 _RightTex_ST;
            #endif

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
                #if _STEREO_ON
                    float4 col;
                    
                    if(unity_StereoEyeIndex == 0) {
                        col = tex2D(_MainTex, i.uv);
                    } else {
                        col = tex2D(_RightTex, i.uv);
                    }
                #else
                    float4 col = tex2D(_MainTex, i.uv);
                #endif
                
                col = float4(col.rgb * (_BgMultWhenWhite - _BgMultWhenBlack), _BgMultWhenBlack);
                
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
            #pragma shader_feature _STEREO_ON
            #pragma shader_feature _OVERRIDEDEPTH_ON
            
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
            
            #if _STEREO_ON
                sampler2D _RightTex;
            #endif
            
            v2f vert(appdata v)
            {
                v2f o;
                o.uv = v.uv;
                
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            
            float4 frag(v2f i) : COLOR
            {
                #if _OVERRIDEDEPTH_ON
                    discard;
                #endif
            
                #if _STEREO_ON
                    float4 col;
                    
                    if(unity_StereoEyeIndex == 0) {
                        col = tex2D(_MainTex, i.uv);
                    } else {
                        col = tex2D(_RightTex, i.uv);
                    }
                #else
                    float4 col = tex2D(_MainTex, i.uv);
                #endif
                
                if(col.a <= 0.1)
                    discard;
            
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
