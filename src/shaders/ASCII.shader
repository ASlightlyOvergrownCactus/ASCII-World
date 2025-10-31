Shader"Futile/ASCII"
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
               "_BehindHUD"
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

                uniform sampler2D _ASCIIWorldTex;
                float4 _MainTex_ST;
                sampler2D _BehindHUD;
                uniform float4 _ASCIIWorldTex_TexelSize;
                uniform float4 _ASCIIWorldSize; // x and y are character size, z and w are color output size

                float get_lum(float4 pixcol, float contrast, float offset)
                {
                    pixcol.rgb *= 0.999;
                    float lum = ((pixcol.r * 0.2126) + (pixcol.g * 0.7152) + (pixcol.b * 0.0722));
                    lum = (lum - 0.5 + offset) * contrast + 0.5;
                    lum = clamp(lum, 0.0, 1.0);
                    return lum * 0.999;
                }

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
                    float2 sampTexSize = float2(_ASCIIWorldTex_TexelSize.z / _ASCIIWorldSize.x, _ASCIIWorldTex_TexelSize.w / _ASCIIWorldSize.y);
    
                    // Downscale and calculate proper offset
                    float2 screenCoords = i.grabScrPos.xy * _ScreenParams.xy / i.grabScrPos.w;
                    float2 offset = float2(screenCoords.x % _ASCIIWorldSize.x, screenCoords.y % _ASCIIWorldSize.y); // this line is evil
                    float2 colOffset = float2(screenCoords.x % _ASCIIWorldSize.z, screenCoords.y % _ASCIIWorldSize.w); // this line is evil
                    float2 sampleCoords = float2(screenCoords.x - colOffset.x + (_ASCIIWorldSize.z / 2.0), screenCoords.y - colOffset.y + (_ASCIIWorldSize.w / 2.0));
                    sampleCoords = sampleCoords.xy / _ScreenParams.xy * i.grabScrPos.w;
                    float2 sampleLumCoords = float2(screenCoords.x - offset.x + (_ASCIIWorldSize.x / 2.0), screenCoords.y - offset.y + (_ASCIIWorldSize.y / 2.0));
                    sampleLumCoords = sampleLumCoords.xy / _ScreenParams.xy * i.grabScrPos.w;
    
                    // sample the texture and calculate luminance
                    fixed4 col = tex2D(_BehindHUD, sampleCoords);
                    fixed4 colLum = tex2D(_BehindHUD, sampleLumCoords);
                    float lum = get_lum(colLum, i.clr.r, i.clr.g);
    
                    // Use luminance to choose ascii character, quantize to set range of luminance values first (length of sampTexSize.x)
                    int charNum = int(lum * (sampTexSize.x * sampTexSize.y)); // charNum out of total number of characters
                    int charXOffset = charNum % sampTexSize.x; // x char offset in texture 
                    int charYOffset = (charNum - charXOffset) / sampTexSize.y; // y char offset in texture
                    float2 asciiCoords = float2((charXOffset * _ASCIIWorldSize.x + offset.x) / _ASCIIWorldTex_TexelSize.z, 
                    (charYOffset * _ASCIIWorldSize.y + offset.y) / _ASCIIWorldTex_TexelSize.w);
                    
                    float4 finalCol = tex2D(_ASCIIWorldTex, asciiCoords);
                
                    finalCol.rgb *= col.rgb;
                
                    return finalCol;
                }
                ENDCG
            }
        }
    }
}
