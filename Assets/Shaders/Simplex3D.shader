Shader "Hidden/Simplex3D"
{
    Properties
    {
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            /* 3D Simplex Noise - MIT License - Copyright © 2013 Nikita Miropolskiy - Stolen from https://www.shadertoy.com/view/XsX3zB */
            float3 random3(float3 c) {
                float j = 4096.0*sin(dot(c,float3(17.0, 59.4, 15.0)));
                float3 r;
                r.z = frac(512.0*j);
                j *= .125;
                r.x = frac(512.0*j);
                j *= .125;
                r.y = frac(512.0*j);
                return r-0.5;
            }
            
            float simplex3d(float3 p) {
                /* 1. find current tetrahedron T and it's four vertices */
                /* s, s+i1, s+i2, s+1.0 - absolute skewed (integer) coordinates of T vertices */
                /* x, x1, x2, x3 - unskewed coordinates of p relative to each of T vertices*/
                
                float F3 = 0.3333333;
                float G3 = 0.1666667;
                
                /* calculate s and x */
                float issou = dot(p, float3(F3, F3, F3));
                float3 s = floor(p + issou);
                float3 x = p - s + dot(s, float3(G3, G3, G3));
                
                /* calculate i1 and i2 */
                float3 e = step(float3(0.0, 0.0, 0.0), x - x.yzx);
                float3 i1 = e*(1.0 - e.zxy);
                float3 i2 = 1.0 - e.zxy*(1.0 - e);
                
                /* x1, x2, x3 */
                float3 x1 = x - i1 + G3;
                float3 x2 = x - i2 + 2.0*G3;
                float3 x3 = x - 1.0 + 3.0*G3;
                
                /* 2. find four surflets and store them in d */
                float4 w, d;
                
                /* calculate surflet weights */
                w.x = dot(x, x);
                w.y = dot(x1, x1);
                w.z = dot(x2, x2);
                w.w = dot(x3, x3);
                
                /* w fades from 0.6 at the center of the surflet to 0.0 at the margin */
                w = max(0.6 - w, 0.0);
                
                /* calculate surflet components */
                d.x = dot(random3(s), x);
                d.y = dot(random3(s + i1), x1);
                d.z = dot(random3(s + i2), x2);
                d.w = dot(random3(s + 1.0), x3);
                
                /* multiply d by w^4 */
                w *= w;
                w *= w;
                d *= w;
                
                /* 3. return the sum of the four surflets */
                return dot(d, float4(52.0, 52.0, 52.0, 52.0));
            }
            
            /*--------------------------------------------------------------------------------------------------------------------------*/
            
            float getHeightAt(float2 xz)
            {
                return simplex3d(float3(xz * 10.0, _Time.y * 0.2));
            }

            float4 frag (v2f i) : SV_Target
            {
                //Noise
                float2 xz = (i.uv * 2.0 - 1.0) * (64 * 0.0078125f);
                float dist = length(xz);
                float attn = sqrt(1.0 - smoothstep(0.4, 0.5, dist));
                float noise = getHeightAt(xz) * attn;
                
                //Normal
                float diff = 0.0078125; //Size of a subdivision in the mesh
                float dx = getHeightAt(xz - float2(diff, 0.0)) - getHeightAt(xz + float2(diff, 0.0));
                float dz = getHeightAt(xz - float2(0.0, diff)) - getHeightAt(xz + float2(0.0, diff));
                float3 nrm = normalize(lerp(float3(0.0, 1.0, 0.0), float3(dx * 0.005, 2.0 * diff, dz * 0.005), attn));
                
                //Combined
                return float4(noise, nrm.x, nrm.y, nrm.z);
            }
            ENDCG
        }
    }
}
