using UnityEngine;

namespace ColoringBoot.Game
{
    // 화면 디자인 기준 (Phase 7.1 — 시안 A "종이와 갈색 선", 2026-10-03 사용자 결정): 챕터 그림의 진한 갈색 선을 화면의 선 · 글자로 쓴다.
    // AgentScripts/Build/BoardSceneBuilder가 이 값으로 씬 · 프리팹을 만든다 — 값을 바꾸면 BuildPrefabs → BuildScene을 다시 실행
    // 퍼즐 칸 색은 여기 없다(규칙 색 = 팔레트, GDD §2.3)
    [CreateAssetMenu(menuName = "ColoringBoot/UI Theme", fileName = "UiTheme")]
    public sealed class UiTheme : ScriptableObject
    {
        [SerializeField] private Color _background = new Color32(0xF1, 0xEB, 0xDD, 0xFF);   // 화면 바탕
        [SerializeField] private Color _ink = new Color32(0x38, 0x20, 0x08, 0xFF);          // 글자 · 선(그림 선화 색)
        [SerializeField] private Color _muted = new Color32(0x7B, 0x66, 0x50, 0xFF);        // 보조 글자
        [SerializeField] private Color _surface = new Color32(0xFF, 0xFD, 0xF6, 0xFF);      // 버튼 바탕(외곽선 버튼)
        [SerializeField] private Color _surfaceInk = new Color32(0xFF, 0xF8, 0xEA, 0xFF);   // 채운 버튼 · 띠 위 글자
        [SerializeField] private Color _emptyCell = new Color32(0xFF, 0xFD, 0xF7, 0xFF);    // 빈칸
        [SerializeField] private Color _locked = new Color32(0xE3, 0xDA, 0xC6, 0xFF);       // 잠긴 스테이지
        [SerializeField] private Color _next = new Color32(0x8C, 0x58, 0xA0, 0xFF);         // 다음에 풀 스테이지 테두리(그림의 포도색)
        [SerializeField] private Color _warn = new Color32(0xE0, 0x24, 0x6A, 0xFF);         // 막힘
        [SerializeField] private Color _star = new Color32(0xF1, 0xB9, 0x28, 0xFF);         // 완벽 별

        public Color Background => _background;
        public Color Ink => _ink;
        public Color Muted => _muted;
        public Color Surface => _surface;
        public Color SurfaceInk => _surfaceInk;
        public Color EmptyCell => _emptyCell;
        public Color Locked => _locked;
        public Color Next => _next;
        public Color Warn => _warn;
        public Color Star => _star;
    }
}
