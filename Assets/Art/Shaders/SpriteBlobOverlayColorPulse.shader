//Only use for sprites
Shader "Game/Sprite/Blob Overlay Color Pulse"
{
	Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
		
		_OverlayScroll("Overlay Scroll", 2D) = "white" {}
		_OverlayScrollColor("Overlay Scroll Color", Color) = (1,1,1,1)
		_OverlayScrollParams("Overlay Scroll Params speed=(x,y) wave amp=(z,w)", Vector) = (1,1,0,0)
		_OverlayScrollScale("Overlay Scroll Scale", Float) = 1
		
		_OverlayScroll2("Overlay Scroll 2", 2D) = "white" {}
		_OverlayScroll2Color("Overlay Scroll 2 Color", Color) = (1,1,1,1)
		_OverlayScroll2Params("Overlay Scroll 2 Params speed=(x,y) wave amp=(z,w)", Vector) = (1,1,0,0)
		_OverlayScroll2Scale("Overlay Scroll 2 Scale", Float) = 1
		
		_OverlayColorPulseMin("Overlay Color Pulse Min", Color) = (1,1,1,0)
		_OverlayColorPulseMax("Overlay Color Pulse Max", Color) = (1,1,1,1)
		_OverlayColorPulseScale("Overlay Color Pulse Scale", Float) = 1
    }
	
	SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert_Blob
            #pragma fragment SpriteFrag_Blob
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
			
			struct v2fExt
			{
				float4 vertex   : SV_POSITION;
				fixed4 color : COLOR;
				fixed4 colorOverlay : COLOR1;
				float2 texcoord : TEXCOORD0;
				float2 texcoord2 : TEXCOORD1;
				float2 texcoord3 : TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			sampler2D _OverlayScroll;
			fixed4 _OverlayScrollColor;
			float4 _OverlayScrollParams;
			float _OverlayScrollScale;
			
			sampler2D _OverlayScroll2;
			fixed4 _OverlayScroll2Color;
			float4 _OverlayScroll2Params;
			float _OverlayScroll2Scale;
			
			fixed4 _OverlayColorPulseMin;
			fixed4 _OverlayColorPulseMax;
			float _OverlayColorPulseScale;
			
			v2fExt SpriteVert_Blob(appdata_t IN)
			{
				v2fExt OUT;

				UNITY_SETUP_INSTANCE_ID (IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				OUT.vertex = UnityFlipSprite(IN.vertex, _Flip);
				OUT.vertex = UnityObjectToClipPos(OUT.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color * _RendererColor;

				#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap (OUT.vertex);
				#endif
				
				//coord for overlay to accomodate texture scale/offset and scroll
				OUT.texcoord2 = IN.vertex.xy * _OverlayScrollScale + _OverlayScrollParams.xy * _Time.y + half2(_OverlayScrollParams.z * _CosTime.w, _OverlayScrollParams.w * _SinTime.w);
				OUT.texcoord3 = IN.vertex.xy * _OverlayScroll2Scale + _OverlayScroll2Params.xy * _Time.y + half2(_OverlayScroll2Params.z * _CosTime.w, _OverlayScroll2Params.w * _SinTime.w);
				
				float sinT = sin(_Time.y * _OverlayColorPulseScale);
				float t = sinT * sinT;

				OUT.colorOverlay = lerp(_OverlayColorPulseMin, _OverlayColorPulseMax, t);

				return OUT;
			}
			
			fixed4 SpriteFrag_Blob(v2fExt IN) : SV_Target
			{
				fixed4 clrTxt = SampleSpriteTexture (IN.texcoord);
				
				fixed4 clrBase = clrTxt.rrra;
				fixed4 clrOverlayScroll = tex2D(_OverlayScroll, IN.texcoord2) * _OverlayScrollColor;
				fixed4 clrOverlayScroll2 = tex2D(_OverlayScroll2, IN.texcoord3) * _OverlayScroll2Color;
				
				clrBase.rgb = lerp(clrBase.rgb, clrOverlayScroll.rgb, clrOverlayScroll.a * clrTxt.g);
				
				clrBase.rgb = lerp(clrBase.rgb, clrOverlayScroll2.rgb, clrOverlayScroll2.a * clrTxt.g);
				
				fixed4 c = clrBase * IN.color;
				
				c.rgb = lerp(c.rgb, IN.colorOverlay.rgb, IN.colorOverlay.a * clrTxt.b);
				
				c.rgb *= c.a;
				return c;
			}
        ENDCG
        }
    }
}