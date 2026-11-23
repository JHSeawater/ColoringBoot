// 챕터 그림 책장 넘기기(2026-11-24 사용자 — "실제 컬러링북처럼 책을 넘기는 페이지"). BookPage가 넘기는 동안만 그 장의 그림 레이어 전부에 건다.
// 장을 왼쪽 제본(_Spine)을 축으로 화면 앞으로 들어 _Angle(0~180°)만큼 돌린다(2026-11-24 비교 페이지 B안 "들어 올려 넘기기") — 너비는 cos로 줄고(90°를 넘으면 제본 왼쪽으로),
// 들린 쪽은 장 가운데(_Center)에서 멀어지듯 커진다(원근, 거리 _Distance). 90°를 넘으면 뒷면이라 그림 대신 종이 뒷면 색(_BackColor — 어두운 꼭짓점 색인 테두리는 진갈색 그대로).
// 원근은 꼭짓점의 w로 주므로 GPU가 텍스처를 원근에 맞게 펴 준다(사각형 두 삼각형이어도 대각선이 꺾이지 않음). 90°에 가까울수록 _Shade만큼 어둡고, _Fade로 사라진다.
// 좌표는 모두 캔버스 공간(UI 꼭짓점과 같은 공간 — BookPage가 넘겨줌). 나머지는 UI/Default와 같다
Shader "ColoringBoot/UI/PageTurn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Angle ("Angle (radian)", Float) = 0
        _Spine ("Spine X", Float) = 0
        _Center ("Center", Vector) = (0,0,0,0)
        _Distance ("Perspective Distance", Float) = 1000
        _Shade ("Shade", Range(0, 1)) = 0.28
        _Fade ("Fade", Range(0, 1)) = 1
        _BackColor ("Back Color", Color) = (0.929, 0.902, 0.839, 1)

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
            float _Angle;
            float _Spine;
            float4 _Center;
            float _Distance;
            float _Shade;
            float _Fade;
            fixed4 _BackColor;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;

                float s = sin(_Angle);
                float dx = v.vertex.x - _Spine;
                float4 turned = float4(_Spine + dx * cos(_Angle), v.vertex.y, v.vertex.z, 1.0);
                float lift = dx * s;                               // 제본에서 멀수록 화면 앞으로 들린다
                float scale = _Distance / (_Distance - lift);      // 원근: 다가온 만큼 가운데에서 멀어지며 커진다
                float4 p = UnityObjectToClipPos(turned);
                float4 c = UnityObjectToClipPos(float4(_Center.xy, v.vertex.z, 1.0));
                float2 np = p.xy / p.w;
                float2 nc = c.xy / c.w;
                float w = 1.0 / scale;                             // 화면 위치는 그대로, w만 키워 원근에 맞는 텍스처 보간
                OUT.vertex = float4((nc + (np - nc) * scale) * w, p.z / p.w * w, w);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                OUT.color.rgb *= 1.0 - _Shade * s;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                // 뒷면: 바탕 · 조각 · 선화는 한 가지 종이색(꼭짓점 색 차이로 그림이 비치지 않게), 어두운 색(진갈색 테두리)은 그대로
                if (_Angle > UNITY_HALF_PI && dot(IN.color.rgb, half3(0.299, 0.587, 0.114)) > 0.4)
                    color.rgb = _BackColor.rgb * (1.0 - _Shade * sin(_Angle));
                color.a *= _Fade;

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
