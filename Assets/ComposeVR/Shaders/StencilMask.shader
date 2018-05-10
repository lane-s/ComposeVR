Shader "Unlit/StencilMask"
{
	Properties
	{
		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil Value", Float) = 0
		_StencilOp("Stencil Operation", Float) = 2
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255
	}
		SubShader
	{
		Tags {
		"Queue" = "Transparent"
		"RenderType" = "Transparent"
		}

	}
}
