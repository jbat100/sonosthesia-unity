Shader "Playground/SonosthesiaTexture" {
	Properties {
		_MainTint ("Diffuse Tint", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_ScrollXSpeed ("X Scroll Speed", Range(0,10)) = 2
		_ScrollYSpeed ("Y Scroll Speed", Range(0,10)) = 2
		_Inverter ("Inverter", Range(0,1)) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert
		
		fixed4 _MainTint;
		fixed _ScrollXSpeed;
		fixed _ScrollYSpeed;
		fixed _Inverter;
		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			
			fixed2 scrolledUV = IN.uv_MainTex;
			
			fixed xScrollValue = _ScrollXSpeed * _Time;
			fixed yScrollValue = _ScrollYSpeed * _Time;
			
			scrolledUV += float2(xScrollValue, yScrollValue);
		
			half4 col = ( tex2D (_MainTex, scrolledUV) + _MainTint ) / 2.0;
			half4 inv = half4(1.0 - col.r, 1.0 - col.g, 1.0 - col.b, col.a);

			half4 c = (col * (1.0 - _Inverter)) + (inv * _Inverter);

			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
