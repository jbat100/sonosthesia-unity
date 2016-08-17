// check out aricle on computing normals 
// - http://http.developer.nvidia.com/GPUGems/gpugems_ch42.html
// - https://www.youtube.com/watch?v=1G37-Yav2ZM

// possible asset store stuff
// - https://www.assetstore.unity3d.com/en/#!/content/8228

Shader "Custom/Vertex2" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_TintAmount ("Tint Amout", Range(0,1)) = 0.5
		_ColorA ("Color	A", Color) = (1,1,1,1)
		_ColorB ("Color	B", Color) = (1,1,1,1)
		_Speed ("Wave Speed", Range(0.1, 80)) = 5
		_Frequency ("Wave Frequency", Range(0, 500)) = 2
		_Amplitude ("Wave Amplitude", Range(-0.01,0.01)) = 0.01
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Lambert vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		float _TintAmount;
		float4 _ColorA;
		float4 _ColorB;
		float _Speed;
		float _Frequency;
		float _Amplitude;

		struct Input {
			float2 uv_MainTex;
			float3 vertColor;
		};

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			float time = _Time * _Speed;
			float waveValueA = sin(time + v.vertex.x * _Frequency) * _Amplitude;
			v.vertex.xyz = float3(v.vertex.x, v.vertex.y, v.vertex.z + waveValueA);
			v.normal = normalize(float3(v.normal.x + waveValueA, v.normal.y, v.normal.z));

			float colorScale = waveValueA * 1000.0;
			o.vertColor = float3(colorScale, colorScale, colorScale);
		}

		void surf (Input IN, inout SurfaceOutput o) {
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			float3 tintColor = lerp(_ColorA, _ColorB, IN.vertColor).rgb;
			o.Albedo = c.rgb * (tintColor * _TintAmount);
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
