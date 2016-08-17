Shader "Playground/NormalMapper" {
	Properties {
		_MainTint ("Diffuse Tint", Color) = (1,1,1,1)
		_NormalTex ("Normal Map", 2D) = "bump" {}
		_NormalIntensity ("Normal Intensity", Range(-3,3)) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert

		float4 _MainTint;
		sampler2D _NormalTex;
		float _NormalIntensity;

		struct Input {
			float2 uv_NormalTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			float3 normalMap = UnpackNormal(tex2D(_NormalTex, IN.uv_NormalTex));
			normalMap = float3(normalMap.x * _NormalIntensity, normalMap.y * _NormalIntensity, normalMap.z);
			o.Normal = normalMap.rgb;
			o.Albedo = _MainTint.rgb;
			o.Alpha = 1.0;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
