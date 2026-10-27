// Shader simples que renderiza as faces internas de um objeto (Cull Front).
// Perfeito para skyboxes ou salas internas.
Shader "Custom/InvertNormals"
{
    Properties
    {
        // Propriedade para a textura (a imagem da parede do cubo)
        _MainTex ("Texture", 2D) = "white" {}
        // Propriedade para uma cor de fundo, caso não haja textura
        _Color ("Color", Color) = (0.2, 0.2, 0.2, 1) // Um cinza escuro
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        // A linha mágica: diz ao Unity para "descartar" as faces frontais
        // e renderizar apenas as faces traseiras (internas).
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Combina a textura com a cor
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
}