Shader "MyShader/billboards123" {
Properties {
_MainTex ("Texture Image", 2D) = "white" {}
_CutOff ("cutout size",Range(0,1))=0.1
}
SubShader {
AlphaTest Greater [_CutOff]

Pass
{
Tags { "LightMode" = "ShadowCaster" }
SetTexture [_MainTex]
}

Pass {
Tags { "Queue" = "AlphaTest" "RenderType"="Transparentcutout" }

cull off
CGPROGRAM

#pragma vertex vert
#pragma fragment frag

// User-specified uniforms
uniform sampler2D _MainTex;
uniform float _CutOff;

struct vertexInput {
float4 vertex : POSITION;
float4 tex : TEXCOORD0;
};
struct vertexOutput {
float4 pos : SV_POSITION;
float4 tex : TEXCOORD0;
};

vertexOutput vert(vertexInput input)
{
vertexOutput output;

output.pos = mul(UNITY_MATRIX_P,
mul(UNITY_MATRIX_MV, float4(0.0, 0.0, 0.0, 1.0))
- float4(input.vertex.x, input.vertex.z, 0.0, 0.0));

output.tex = -input.tex;

return output;
}

float4 frag(vertexOutput input) : COLOR
{

float4 color = tex2D(_MainTex, float2(input.tex.xy));
clip(color.a - _CutOff) ;
return color;
}

ENDCG
}
//    UsePass "VertexLit/SHADOWCOLLECTOR"
//    UsePass "VertexLit/SHADOWCASTER"
}

Fallback  "Transparent/Cutout/Diffuse"
}
