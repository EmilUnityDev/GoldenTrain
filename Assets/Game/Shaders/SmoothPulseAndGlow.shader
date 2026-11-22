Shader "UI/SmoothPulseAndGlow_Fixed"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Pulse Settings)]
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 0.2)) = 0.03
        
        [Header(Glow Settings)]
        _GlowSpeed ("Glow Speed", Float) = 1.5
        _GlowWidth ("Glow Width", Range(0.01, 0.5)) = 0.1
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.5

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [ZTest]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma shader_feature_local _USEUIALPHACLIP_ON

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            sampler2D _MainTex;

            float _PulseSpeed;
            float _PulseAmount;
            float _GlowSpeed;
            float _GlowWidth;
            fixed4 _GlowColor;
            float _GlowIntensity;

            v2f vert(appdata_t v)
            {
                v2f o;
                // Больше не трогаем вершины, чтобы геометрия UI не дергалась
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1. ПУЛЬСАЦИЯ ЧЕРЕЗ UV (Эффект лупы из центра)
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                // Инвертируем скейл: чтобы картинка увеличилась, UV-координаты нужно сжать к центру
                float scale = 1.0 / (1.0 + (pulse * _PulseAmount));
                
                // Сдвигаем UV в центр (0.5, 0.5), масштабируем и возвращаем обратно
                float2 pulsedUV = (i.texcoord - 0.5) * scale + 0.5;

                // Проверяем, не вышли ли мы за границы текстуры при зуме (чтобы края не растягивались)
                if (pulsedUV.x < 0.0 || pulsedUV.x > 1.0 || pulsedUV.y < 0.0 || pulsedUV.y > 1.0)
                {
                    return fixed4(0,0,0,0);
                }

                // Выборка текстуры по измененным UV
                half4 color = (tex2D(_MainTex, pulsedUV) + _TextureSampleAdd) * i.color;

                // 2. БЛИК ОТ ЦЕНТРА (Используем оригинальные UV, чтобы блик не дергался вместе с пульсацией)
                float2 centerDist = i.texcoord - float2(0.5, 0.5);
                float radius = length(centerDist);

                float wavePos = fmod(_Time.y * _GlowSpeed, 1.3) - 0.2; 
                float glowFactor = smoothstep(wavePos - _GlowWidth, wavePos, radius) * 
                                   smoothstep(wavePos + _GlowWidth, wavePos, radius);

                // Мягкое затухание блика к краям
                glowFactor *= smoothstep(0.5, 0.3, radius);

                // Накладываем блик
                fixed4 finalGlow = _GlowColor * glowFactor * _GlowIntensity * color.a;
                color.rgb += finalGlow.rgb;

                #ifdef _USEUIALPHACLIP_ON
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}