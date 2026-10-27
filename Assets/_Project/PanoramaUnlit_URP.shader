// Shader Unlit compatível com a Universal Render Pipeline (URP).
// Exibe uma textura panorâmica 360 no interior de uma esfera.
// Renderiza os dois lados da malha (Cull Off).
Shader "IniciacaoCientifica/PanoramaUnlit_URP"
{
    Properties
    {
        _MainTex("Textura Panorâmica (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        // Tag essencial que informa à Unity que este shader é para a URP.
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Opaque" }

        Pass
        {
            // A diretiva Cull Off permanece, garantindo a renderização da face interna.
            Cull Off

            // Início do bloco de código HLSL, padrão da URP.
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Inclusão das bibliotecas Core da URP. Essencial para funcionar.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Struct para os dados da textura.
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            // Definição da textura e seus dados de tiling/offset.
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END

            // Vertex Shader: Transforma os vértices do objeto para o espaço da tela.
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            // Fragment Shader: Colore cada pixel com base na textura.
            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return color;
            }
            
            // CORREÇÃO: Adicionada a tag ENDHLSL que estava faltando.
            ENDHLSL
        }
    }
}