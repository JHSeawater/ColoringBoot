using System;
using ColoringBoot.Core;
using UnityEngine;

namespace ColoringBoot.Game
{
    // 스테이지 팔레트 — 색 값 1~7을 화면에 보일 색으로 바꾼다 (GDD §2.3). 로직은 팔레트를 모른다
    [CreateAssetMenu(menuName = "ColoringBoot/Color Palette", fileName = "Palette")]
    public sealed class ColorPalette : ScriptableObject
    {
        private const int ColorCount = 7;

        [Tooltip("색 값 1~7 순서: 빨강 · 노랑 · 주황 · 파랑 · 보라 · 초록 · 검정(기본 팔레트 이름 기준)")]
        [SerializeField] private Color[] _colors = new Color[ColorCount];

        // 빈칸(0)의 색은 팔레트가 아니라 보드 테마(BoardView)가 정한다
        public Color Get(PaintColor color) => _colors[(int)color - 1];

        private void OnValidate()
        {
            if (_colors.Length != ColorCount) Array.Resize(ref _colors, ColorCount);
        }
    }
}
