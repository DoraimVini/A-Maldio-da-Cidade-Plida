// Shader de oclusão por dither para sprites (Built-in Render Pipeline).
// Usado nas paredes/casas altas: quando o Damião passa ATRÁS de uma parede
// (que o Y-sort desenha por cima dele), o componente OcclusaoDitherFade sobe o
// _DitherAmount e este shader recorta pixels da parede num padrão Bayer 4x4.
// Como o boneco foi desenhado antes (atrás), ele aparece pelos buracos = silhueta.
//
// _DitherAmount: 0 = opaco (sem oclusão), 0.5 = ~xadrez, 1 = quase todo furado.
// Ajustado por-renderer via MaterialPropertyBlock (uniform simples, sem instanciar material).
Shader "FavelaAmarela/SpriteDitherOcclusion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _DitherAmount ("Dither Amount", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex    : SV_POSITION;
                fixed4 color     : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _DitherAmount;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                OUT.screenPos = ComputeScreenPos(OUT.vertex);
                return OUT;
            }

            // Matriz Bayer 4x4 normalizada em [0,1) — limiar de dithering ordenado.
            static const float BAYER_4X4[16] =
            {
                 0.0 / 16.0,  8.0 / 16.0,  2.0 / 16.0, 10.0 / 16.0,
                12.0 / 16.0,  4.0 / 16.0, 14.0 / 16.0,  6.0 / 16.0,
                 3.0 / 16.0, 11.0 / 16.0,  1.0 / 16.0,  9.0 / 16.0,
                15.0 / 16.0,  7.0 / 16.0, 13.0 / 16.0,  5.0 / 16.0
            };

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                // Coordenada de pixel na tela → índice na matriz Bayer.
                float2 screenPixels = (IN.screenPos.xy / IN.screenPos.w) * _ScreenParams.xy;
                int x = (int)fmod(screenPixels.x, 4.0);
                int y = (int)fmod(screenPixels.y, 4.0);
                float limiar = BAYER_4X4[y * 4 + x];

                // Onde a "quantidade de dither" supera o limiar, abre um buraco.
                clip(limiar - _DitherAmount);

                c.rgb *= c.a; // premultiplied alpha (casa com o Blend acima)
                return c;
            }
        ENDCG
        }
    }

    Fallback "Sprites/Default"
}
