Shader "Custom/VertexColors" {

	// Vertex Colors Surface Shader for Unity
	//
	//   Copyright 2018 Gregary Pergrossi
	//
	//   Licensed under the Apache License, Version 2.0 (the "License");
	//   you may not use this file except in compliance with the License.
	//   You may obtain a copy of the License at
	//
	//       http://www.apache.org/licenses/LICENSE-2.0
	//
	//   Unless required by applicable law or agreed to in writing, software
	//   distributed under the License is distributed on an "AS IS" BASIS,
	//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	//   See the License for the specific language governing permissions and
	//   limitations under the License.
	//
	// This surface shader is intended for use with the PolyOBJImporter to display
	// vertex color information. You may modify the code as you see fit, but know
	// that any changes to the surface shader's name must be reflected by the
	// PolyOBJImporter script's "Shader.Find()" statement.

	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float4 color : COLOR;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color * IN.color;
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
