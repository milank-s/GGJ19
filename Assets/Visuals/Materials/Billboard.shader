Shader "MyShader/billboards123" {
Properties {
    _MainTex ("Texture Image", 2D) = "white" {}
    _CutOff ("cutout size",Range(0,1))=0.1
    _ScaleX ("scale X",float) = 1
    _ScaleY ("scale Y",float) = 1
}
SubShader {
    AlphaTest Greater [_CutOff]

    Pass
    {
        Tags { "LightMode" = "ShadowCaster" }
        SetTexture [_MainTex]
    }

    Pass {
        Tags { "Queue" = "Transparent" "RenderType"="Transparentcutout" "DisableBatching" = "True"}

        cull off
        CGPROGRAM

        #pragma vertex vert
        #pragma fragment frag

        // User-specified uniforms
        uniform sampler2D _MainTex;
        uniform float _CutOff;
        float _ScaleX;
        float _ScaleY;

        struct vertexInput {
            float4 vertex : POSITION;
            float2 tex : TEXCOORD0;
        };
        struct vertexOutput {
            float4 pos : SV_POSITION;
            float2 tex : TEXCOORD0;
        };

        vertexOutput vert(vertexInput input)
        {
            vertexOutput output;

            output.pos = mul(UNITY_MATRIX_P,
                        mul(UNITY_MATRIX_MV, float4(0.0, 0.0, 0.0, 1.0))
                        - float4(-1*_ScaleX*input.vertex.x, -1*_ScaleY*input.vertex.y, 0.0, 0.0));
            output.tex = input.tex;

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
