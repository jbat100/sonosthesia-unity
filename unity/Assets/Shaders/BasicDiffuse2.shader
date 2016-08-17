Shader "Playground/BasicDiffuse2" {

	Properties {
		_EmissiveColor ("Emissive Color", Color) = (1,1,1,1)
		_AmbientColor ("Ambient Color", Color) = (1,1,1,1)
		_MySliderValue ("Excitation", Range(0,3)) = 2.5
		_RampTex ("Ramp", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf BasicDiffuse4

		float4 _EmissiveColor;
        float4 _AmbientColor;
        float _MySliderValue;
        sampler2D _RampTex;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			//We can then use the properties values in our shader
             float4 c;
             c = pow((_EmissiveColor + _AmbientColor), _MySliderValue);
             o.Albedo = c.rgb;
             o.Alpha = c.a;
		}
		
		inline float4 LightingBasicDiffuse2 (SurfaceOutput s, fixed3 lightDir, fixed atten)
        {
         	float difLight = max(0, dot(s.Normal, lightDir));
         	float4 col;
         	col.rgb = s.Albedo * _LightColor0.rgb * (difLight * atten * 2);
         	col.a = s.Alpha;
         	return col;
		}
		
		inline float4 LightingBasicDiffuse3 (SurfaceOutput s, fixed3 lightDir, fixed atten)
        {
         	float difLight = max(0, dot(s.Normal, lightDir));
         	float halfLambert = difLight * 0.5 + 0.5;
         	float3 ramp = tex2D(_RampTex, float2(halfLambert)).rgb;
         	
         	float4 col;
         	//col.rgb = s.Albedo * _LightColor0.rgb * (halfLambert * atten * 2);
         	col.rgb = s.Albedo * _LightColor0.rgb * (ramp);
         	col.a = s.Alpha;
         	return col;
		}
		
		inline float4 LightingBasicDiffuse4 (SurfaceOutput s, fixed3 lightDir, fixed3 viewDir, fixed atten)
        {
         	float difLight = dot(s.Normal, lightDir);
         	float rimLight = dot(s.Normal, viewDir);
         	float halfLambert = difLight * 0.5 + 0.5;
         	float3 ramp = tex2D(_RampTex, float2(halfLambert, rimLight)).rgb;
         	
         	float4 col;
         	//col.rgb = s.Albedo * _LightColor0.rgb * (halfLambert * atten * 2);
         	col.rgb = s.Albedo * _LightColor0.rgb * (ramp);
         	col.a = s.Alpha;
         	return col;
		}
		
		ENDCG
	} 
	FallBack "Diffuse"
}