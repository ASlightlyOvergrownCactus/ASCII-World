Shader "Futile/ASCIIBloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    Category
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		ZWrite Off
		//Alphatest Greater 0
		Blend SrcAlpha OneMinusSrcAlpha 
		Fog { Color(0,0,0,0) }
		Lighting Off
		Cull Off //we can turn backface culling off because we know nothing will be facing backwards

        BindChannels 
		{
			Bind "Color", color 
		}

        SubShader
        {
            Tags { "RenderType"="Opaque" }

            GrabPass
            {
               "_BehindBloomHUD1"
            }

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
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float4 scrPos : TEXCOORD1;
                    float4 clr : COLOR; // r is contrast, g is offset
                    float4 grabScrPos : TEXCOORD2;
                };

                sampler2D _MainTex;
                uniform float4 _spriteRect;
                uniform float2 _screenSize;
                uniform int _ASCIIKernelSize;

                float4 _MainTex_ST;
                sampler2D _BehindBloomHUD1;
                float4 _BehindBloomHUD1_TexelSize;

                v2f vert (appdata_full v)
                {
                    v2f o;
                    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.pos = UnityObjectToClipPos(v.vertex);
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    o.scrPos = ComputeScreenPos(o.pos);
                    o.grabScrPos = ComputeGrabScreenPos(o.pos);
                    o.clr = v.color;
                    return o;
                }

                fixed4 frag (v2f i) : SV_Target
                {
                    float4 color = tex2D(_BehindBloomHUD1, i.grabScrPos);
                    fixed3 sum = fixed3(0.0, 0.0, 0.0);
                    float lum_thresh = 0.2;

                    int upper = ((_ASCIIKernelSize - 1) / 2);
                    int lower = -upper;

                    for (int x = lower; x <= upper; ++x)
                    {
                        float3 grabColor = tex2D(_BehindBloomHUD1, i.grabScrPos + fixed2(_BehindBloomHUD1_TexelSize.x * x, 0.0));
                        float lum = Luminance(grabColor);

                        if (lum > lum_thresh) sum += grabColor * ((lum - (lum_thresh)) / (1.0 - lum_thresh)); // If you want dark pixels to stay perfectly crisp
                    }
                    sum /= _ASCIIKernelSize;
                    
                    return fixed4((sum * i.clr.r * 2.0) + color, 1.0);
                }
                ENDCG
            }

            GrabPass
            {
               "_BehindBloomHUD2"
            }

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
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float4 scrPos : TEXCOORD1;
                    float4 clr : COLOR; // r is contrast, g is offset
                    float4 grabScrPos : TEXCOORD2;
                };

                sampler2D _MainTex;
                uniform float4 _spriteRect;
                uniform float2 _screenSize;
                uniform int _ASCIIKernelSize;

                float4 _MainTex_ST;
                sampler2D _BehindBloomHUD2;
                float4 _BehindBloomHUD2_TexelSize;

                v2f vert (appdata_full v)
                {
                    v2f o;
                    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.pos = UnityObjectToClipPos(v.vertex);
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    o.scrPos = ComputeScreenPos(o.pos);
                    o.grabScrPos = ComputeGrabScreenPos(o.pos);
                    o.clr = v.color;
                    return o;
                }

                fixed4 frag (v2f i) : SV_Target
                {
                    float4 color = tex2D(_BehindBloomHUD2, i.grabScrPos);
                    fixed3 sum = fixed3(0.0, 0.0, 0.0);
                    float lum_thresh = 0.2;

                    int upper = ((_ASCIIKernelSize - 1) / 2);
                    int lower = -upper;

                    for (int y = lower; y <= upper; ++y)
                    {
                        float3 grabColor = tex2D(_BehindBloomHUD2, i.grabScrPos + fixed2(0.0, _BehindBloomHUD2_TexelSize.y * y));
                        float lum = Luminance(grabColor);

                        if (lum > lum_thresh) sum += grabColor * ((lum - (lum_thresh)) / (1.0 - lum_thresh)); // If you want dark pixels to stay perfectly crisp
                    }
                    sum /= _ASCIIKernelSize;
                    
                    return fixed4((sum * i.clr.r * 2.0) + color, 1.0);
                }
                ENDCG
            }
        }
    }
}
