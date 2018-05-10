// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "AlbedoOutline"
{
	Properties
	{
		_ASEOutlineColor("Outline Color", Color) = (1,0.9968559,0.5441177,0)
		_ASEOutlineWidth("Outline Width", Float) = 0.07
		_AlbedoTexture("AlbedoTexture", 2D) = "white" {}
		_AlbedoColor("AlbedoColor", Color) = (0.1838235,0.9662271,1,0)
		_Smoothness("Smoothness", Float) = 0
		[HideInInspector] _texcoord("", 2D) = "white" {}
		[HideInInspector] __dirty("", Int) = 1
	}

		SubShader
		{
			Tags{ }
			Cull Front
			CGPROGRAM
			#pragma target 3.0
			#pragma surface outlineSurf Outline nofog  keepalpha noshadow noambient novertexlights nolightmap nodynlightmap nodirlightmap nometa noforwardadd vertex:outlineVertexDataFunc 



			struct Input {
				fixed filler;
			};
			uniform fixed4 _ASEOutlineColor;
			uniform fixed _ASEOutlineWidth;
			void outlineVertexDataFunc(inout appdata_full v, out Input o)
			{
				UNITY_INITIALIZE_OUTPUT(Input, o);
				v.vertex.xyz *= (1 + _ASEOutlineWidth);
			}
			inline fixed4 LightingOutline(SurfaceOutput s, half3 lightDir, half atten) { return fixed4(0,0,0, s.Alpha); }
			void outlineSurf(Input i, inout SurfaceOutput o)
			{
				o.Emission = _ASEOutlineColor.rgb;
				o.Alpha = 1;
			}
			ENDCG


			Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
			Cull Back
			CGPROGRAM
			#pragma target 3.0
			#pragma surface surf Standard keepalpha addshadow fullforwardshadows exclude_path:deferred 
			struct Input
			{
				float2 uv_texcoord;
			};

			uniform float4 _AlbedoColor;
			uniform sampler2D _AlbedoTexture;
			uniform float4 _AlbedoTexture_ST;
			uniform float _Smoothness;

			void surf(Input i , inout SurfaceOutputStandard o)
			{
				float2 uv_AlbedoTexture = i.uv_texcoord * _AlbedoTexture_ST.xy + _AlbedoTexture_ST.zw;
				o.Albedo = (_AlbedoColor * tex2D(_AlbedoTexture, uv_AlbedoTexture)).rgb;
				o.Smoothness = _Smoothness;
				o.Alpha = 1;
			}

			ENDCG
		}
			Fallback "Diffuse"
				CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15301
7;29;1906;1004;734.876;540.6501;1;True;False
Node;AmplifyShaderEditor.TexturePropertyNode;6;-236.3521,-192.6527;Float;True;Property;_AlbedoTexture;AlbedoTexture;0;0;Create;True;0;0;False;0;None;None;False;white;Auto;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode;8;18.74785,-184.0529;Float;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;5;90.74783,-376.3528;Float;False;Property;_AlbedoColor;AlbedoColor;1;0;Create;True;0;0;False;0;0.1838235,0.9662271,1,0;0.6085294,1,0.5955881,1;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;359.0477,-198.4528;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;9;80.12402,23.34991;Float;False;Property;_Smoothness;Smoothness;2;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;739,-133;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;AlbedoOutline;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;0;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;ForwardOnly;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;-1;False;-1;-1;False;-1;0;True;0.07;1,0.9968559,0.5441177,0;VertexScale;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;0;0;False;0;0;0;False;-1;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;8;0;6;0
WireConnection;7;0;5;0
WireConnection;7;1;8;0
WireConnection;0;0;7;0
WireConnection;0;4;9;0
ASEEND*/
//CHKSM=EB34954D8186FEC82D0E2E3C402D994300C7E675