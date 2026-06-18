Shader "Custom/ShaderLobbyColorido"
{
    Properties
    {
        _CorPiso("Cor do Piso (Interior)", Color) = (1,1,1,1)
        _CorTeto("Cor do Teto (Interior)", Color) = (0.8, 0.8, 0.8, 1)
        _CorParedes("Cor das Paredes (Interior)", Color) = (0.6, 0.6, 0.6, 1)
        _CorExterior("Cor Externa", Color) = (0.2, 0.2, 0.2, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _CorPiso;
                half4 _CorTeto;
                half4 _CorParedes;
                half4 _CorExterior;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN, float facing : VFACE) : SV_Target
            {
                if (facing < 0) 
                {
                    float3 normal = normalize(IN.normalWS);
                    float epsilon = 0.001;

                    if (normal.y > 1.0 - epsilon)
                    {
                        return _CorPiso;
                    }
                    else if (normal.y < -1.0 + epsilon)
                    {
                        return _CorTeto;
                    }
                    else
                    {
                        return _CorParedes;
                    }
                }
                else
                {
                    return _CorExterior;
                }
            }
            ENDHLSL
        }
    }
}