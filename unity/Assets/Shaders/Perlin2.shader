//
//	Code repository for GPU noise development blog
//	http://briansharpe.wordpress.com
//	https://github.com/BrianSharpe
//
//	I'm not one for copyrights.  Use the code however you wish.
//	All I ask is that credit be given back to the blog or myself when appropriate.
//	And also to let me know if you come up with any changes, improvements, thoughts or interesting uses for this stuff. :)
//	Thanks!
//
//	Brian Sharpe
//	brisharpe CIRCLE_A yahoo DOT com
//	http://briansharpe.wordpress.com
//	https://github.com/BrianSharpe
//
//===============================================================================
//  Scape Software License
//===============================================================================
//
//Copyright (c) 2007-2012, Giliam de Carpentier
//All rights reserved.
//
//Redistribution and use in source and binary forms, with or without
//modification, are permitted provided that the following conditions are met: 
//
//1. Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer. 
//2. Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
//
//THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNERS OR CONTRIBUTORS BE LIABLE 
//FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL 
//DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
//SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER 
//CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
//OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
//OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.;

Shader "Noise/Perlin2" 
{
	Properties 
	{
		_Octaves ("Octaves", Float) = 8.0
		_Frequency ("Frequency", Float) = 1.0
		_Amplitude ("Amplitude", Float) = 1.0
		_Lacunarity ("Lacunarity", Float) = 1.92
		_Persistence ("Persistence", Float) = 0.8
		_Offset ("Offset", Vector) = (0.0, 0.0, 0.0, 0.0)
		_RidgeOffset ("Ridge Offset", Float) = 1.0
		_Warp ("Warp", Float) = 0.25
		_Displacement("Displacement", Float) = 1.0		_AnimSpeed("Animation Speed", Float) = 1.0		_LowColor("Low Color", Vector) = (0.0, 0.0, 0.0, 1.0)
		_HighColor("High Color", Vector) = (1.0, 1.0, 1.0, 1.0)

	}

	CGINCLUDE
		//
		//	FAST32_hash
		//	A very fast hashing function.  Requires 32bit support.
		//	http://briansharpe.wordpress.com/2011/11/15/a-fast-and-simple-32bit-floating-point-hash-function/
		//
		//	The hash formula takes the form....
		//	hash = mod( coord.x * coord.x * coord.y * coord.y, SOMELARGEFLOAT ) / SOMELARGEFLOAT
		//	We truncate and offset the domain to the most interesting part of the noise.
		//	SOMELARGEFLOAT should be in the range of 400.0->1000.0 and needs to be hand picked.  Only some give good results.
		//	3D Noise is achieved by offsetting the SOMELARGEFLOAT value by the Z coordinate
		//
		   void FAST32_hash_3D( 	float3 gridcell,
								out float4 lowz_hash_0,
								out float4 lowz_hash_1,
								out float4 lowz_hash_2,
								out float4 highz_hash_0,
								out float4 highz_hash_1,
								out float4 highz_hash_2	)		//	generates 3 random numbers for each of the 8 cell corners
		{
			//    gridcell is assumed to be an integer coordinate
		
			//	TODO: 	these constants need tweaked to find the best possible noise.
			//			probably requires some kind of brute force computational searching or something....
			const float2 OFFSET = float2( 50.0, 161.0 );
			const float DOMAIN = 69.0;
			const float3 SOMELARGEFLOATS = float3( 635.298681, 682.357502, 668.926525 );
			const float3 ZINC = float3( 48.500388, 65.294118, 63.934599 );
		
			//	truncate the domain
			gridcell.xyz = gridcell.xyz - floor(gridcell.xyz * ( 1.0 / DOMAIN )) * DOMAIN;
			float3 gridcell_inc1 = step( gridcell, float3( DOMAIN - 1.5, DOMAIN - 1.5, DOMAIN - 1.5 ) ) * ( gridcell + 1.0 );
		
			//	calculate the noise
			float4 P = float4( gridcell.xy, gridcell_inc1.xy ) + OFFSET.xyxy;
			P *= P;
			P = P.xzxz * P.yyww;
			float3 lowz_mod = float3( 1.0 / ( SOMELARGEFLOATS.xyz + gridcell.zzz * ZINC.xyz ) );
			float3 highz_mod = float3( 1.0 / ( SOMELARGEFLOATS.xyz + gridcell_inc1.zzz * ZINC.xyz ) );
			lowz_hash_0 = frac( P * lowz_mod.xxxx );
			highz_hash_0 = frac( P * highz_mod.xxxx );
			lowz_hash_1 = frac( P * lowz_mod.yyyy );
			highz_hash_1 = frac( P * highz_mod.yyyy );
			lowz_hash_2 = frac( P * lowz_mod.zzzz );
			highz_hash_2 = frac( P * highz_mod.zzzz );
		}
		//
		//	PerlinSurflet3D_Deriv
		//	Perlin Surflet 3D noise with derivatives
		//	returns float4( value, xderiv, yderiv, zderiv )
		//
		float4 PerlinSurflet3D_Deriv( float3 P )
		{
			//	establish our grid cell and unit position
			float3 Pi = floor(P);
			float3 Pf = P - Pi;
			float3 Pf_min1 = Pf - 1.0;
		
			//	calculate the hash.
			//	( various hashing methods listed in order of speed )
			float4 hashx0, hashy0, hashz0, hashx1, hashy1, hashz1;
			FAST32_hash_3D( Pi, hashx0, hashy0, hashz0, hashx1, hashy1, hashz1 );
		
			//	calculate the gradients
			float4 grad_x0 = hashx0 - 0.49999;
			float4 grad_y0 = hashy0 - 0.49999;
			float4 grad_z0 = hashz0 - 0.49999;
			float4 norm_0 = rsqrt( grad_x0 * grad_x0 + grad_y0 * grad_y0 + grad_z0 * grad_z0 );
			grad_x0 *= norm_0;
			grad_y0 *= norm_0;
			grad_z0 *= norm_0;
			float4 grad_x1 = hashx1 - 0.49999;
			float4 grad_y1 = hashy1 - 0.49999;
			float4 grad_z1 = hashz1 - 0.49999;
			float4 norm_1 = rsqrt( grad_x1 * grad_x1 + grad_y1 * grad_y1 + grad_z1 * grad_z1 );
			grad_x1 *= norm_1;
			grad_y1 *= norm_1;
			grad_z1 *= norm_1;
			float4 grad_results_0 = float2( Pf.x, Pf_min1.x ).xyxy * grad_x0 + float2( Pf.y, Pf_min1.y ).xxyy * grad_y0 + Pf.zzzz * grad_z0;
			float4 grad_results_1 = float2( Pf.x, Pf_min1.x ).xyxy * grad_x1 + float2( Pf.y, Pf_min1.y ).xxyy * grad_y1 + Pf_min1.zzzz * grad_z1;
		
			//	get lengths in the x+y plane
			float3 Pf_sq = Pf*Pf;
			float3 Pf_min1_sq = Pf_min1*Pf_min1;
			float4 vecs_len_sq = float2( Pf_sq.x, Pf_min1_sq.x ).xyxy + float2( Pf_sq.y, Pf_min1_sq.y ).xxyy;
		
			//	evaluate the surflet
			float4 m_0 = vecs_len_sq + Pf_sq.zzzz;
			m_0 = max(1.0 - m_0, 0.0);
			float4 m2_0 = m_0*m_0;
			float4 m3_0 = m_0*m2_0;
		
			float4 m_1 = vecs_len_sq + Pf_min1_sq.zzzz;
			m_1 = max(1.0 - m_1, 0.0);
			float4 m2_1 = m_1*m_1;
			float4 m3_1 = m_1*m2_1;
		
			//	calc the deriv
			float4 temp_0 = -6.0 * m2_0 * grad_results_0;
			float xderiv_0 = dot( temp_0, float2( Pf.x, Pf_min1.x ).xyxy ) + dot( m3_0, grad_x0 );
			float yderiv_0 = dot( temp_0, float2( Pf.y, Pf_min1.y ).xxyy ) + dot( m3_0, grad_y0 );
			float zderiv_0 = dot( temp_0, Pf.zzzz ) + dot( m3_0, grad_z0 );
		
			float4 temp_1 = -6.0 * m2_1 * grad_results_1;
			float xderiv_1 = dot( temp_1, float2( Pf.x, Pf_min1.x ).xyxy ) + dot( m3_1, grad_x1 );
			float yderiv_1 = dot( temp_1, float2( Pf.y, Pf_min1.y ).xxyy ) + dot( m3_1, grad_y1 );
			float zderiv_1 = dot( temp_1, Pf_min1.zzzz ) + dot( m3_1, grad_z1 );
		
			const float FINAL_NORMALIZATION = 2.3703703703703703703703703703704;	//	scales the final result to a strict 1.0->-1.0 range
			return float4( dot( m3_0, grad_results_0 ) + dot( m3_1, grad_results_1 ) , float3(xderiv_0,yderiv_0,zderiv_0) + float3(xderiv_1,yderiv_1,zderiv_1) ) * FINAL_NORMALIZATION;
		}
		float PerlinDerivedSwiss(float3 p, int octaves, float3 offset, float frequency, float amplitude, float lacunarity, float persistence, float warp, float ridgeOffset)
		{
			float sum = 0.0;
			float3 dsum = float3(0.0, 0.0, 0.0);
			for (int i = 0; i < octaves; i++)
			{
				float4 n = 0.5 * (0 + (ridgeOffset - abs(PerlinSurflet3D_Deriv((p+offset+warp*dsum)*frequency))));
				sum += amplitude * n.x;
				dsum += amplitude * n.yzw * -n.x;
				frequency *= lacunarity;
				amplitude *= persistence * saturate(sum);
			}
			return sum;
		}
	ENDCG

	SubShader 
	{



		
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert
		#pragma glsl
		#pragma target 3.0
		
		fixed _Octaves;
		float _Frequency;
		float _Amplitude;
		float3 _Offset;
		float _Lacunarity;
		float _Persistence;
		float _RidgeOffset;
		float _Warp;
		float _Displacement;		float _AnimSpeed;		fixed4 _LowColor;
		fixed4 _HighColor;


		struct Input 
		{
			float3 pos;

		};

		void vert (inout appdata_full v, out Input OUT)
		{
			UNITY_INITIALIZE_OUTPUT(Input, OUT);
			OUT.pos = float3(v.texcoord.xy, _Time.y * _AnimSpeed);
			float h = PerlinDerivedSwiss(OUT.pos, _Octaves, _Offset, _Frequency, _Amplitude, _Lacunarity, _Persistence, _Warp, _RidgeOffset);
			v.vertex.xyz += v.normal * h * _Displacement;
		}

		void surf (Input IN, inout SurfaceOutput o) 
		{
			float h = PerlinDerivedSwiss(IN.pos, _Octaves, _Offset, _Frequency, _Amplitude, _Lacunarity, _Persistence, _Warp, _RidgeOffset);
			
			h = h * 0.5 + 0.5;
			
			float4 color = float4(0.0, 0.0, 0.0, 0.0);
			color = lerp(_LowColor, _HighColor, h);

			o.Albedo = color.rgb;
			o.Alpha = 1.0;
		}
		ENDCG
	}
	
	FallBack "Diffuse"
}