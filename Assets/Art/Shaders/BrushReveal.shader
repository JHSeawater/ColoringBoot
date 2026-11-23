// 챕터 그림 조각을 붓질처럼 드러낸다(2026-11-24 사용자 — "진짜 붓으로 칠하는거 마냥"). ChapterView가 칠하는 동안에만 조각에 건다.
// 조각(스프라이트 한 장 = UV 0~1)을 위에서부터 _Rows줄로 나눠 왼→오, 한 줄 내려 오→왼으로 번갈아 칠한다. 줄 경계와 붓 끝은 붓털처럼 울퉁불퉁.
// _Progress 0 = 안 보임, 1 = 다 칠함. 나머지는 UI/Default와 같다(RectMask2D 잘라내기 · 스텐실)
Shader "ColoringBoot/UI/BrushReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Progress ("Progress", Range(0, 1)) = 1
        _Rows ("Rows", Float) = 3
        _Tip ("Bristle Tip Length", Float) = 0.12

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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

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
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _Progress;
            float _Rows;
            float _Tip;

            // 0~1 값 잡음(1차원) — 정수 자리마다 무작위 값, 그 사이는 부드럽게 잇는다
            float Hash(float n) { return frac(sin(n * 127.1) * 43758.5453); }
            float Noise(float x)
            {
                float i = floor(x);
                float f = frac(x);
                return lerp(Hash(i), Hash(i + 1.0), f * f * (3.0 - 2.0 * f));
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // 몇 번째 줄인가 — 위에서부터. 줄 경계는 가로로 출렁인다(붓 폭이 고르지 않게)
                float down = (1.0 - IN.texcoord.y) * _Rows + (Noise(IN.texcoord.x * 7.0) - 0.5) * 0.35;
                float row = clamp(floor(down), 0.0, _Rows - 1.0);
                // 이 줄에서 붓이 간 거리(0~1) · 이 자리의 붓 진행 방향 위치 — 짝수 줄은 왼→오, 홀수 줄은 오→왼
                float reach = saturate(_Progress * _Rows - row);
                float along = fmod(row, 2.0) < 0.5 ? IN.texcoord.x : 1.0 - IN.texcoord.x;
                // 붓털: 줄 안의 세로 위치마다 끝이 다르게 나온다(가는 털 + 굵은 털)
                float bristle = Noise(down * 23.0) * 0.6 + Noise(down * 61.0 + 17.0) * 0.4;
                float front = reach * (1.02 + _Tip) - _Tip * bristle;   // 다 간 줄은 털 끝이 오른쪽(왼쪽) 끝을 넘어 빈틈이 없게
                color.a *= smoothstep(front + 0.006, front - 0.006, along);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                color.rgb *= color.a;
                return color;
            }
        ENDCG
        }
    }
}
