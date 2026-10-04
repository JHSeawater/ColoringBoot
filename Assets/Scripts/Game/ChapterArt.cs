using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColoringBoot.Game
{
    // 챕터 그림 (Phase 5, ArtSpec.md) — 선화 + 색칠 단계 레이어. 단계 i는 스테이지 목록의 i번째 스테이지가 칠한다(GDD §5).
    // 레이어는 여백을 잘라 낸 조각이고, 캔버스(픽셀, 왼쪽 위 기준) 안의 자리를 함께 갖는다 — AgentScripts/Build/ChapterArtBuilder.cs가 채운다
    [CreateAssetMenu(menuName = "ColoringBoot/Chapter Art", fileName = "ChapterArt")]
    public sealed class ChapterArt : ScriptableObject
    {
        [Serializable]
        public struct Layer
        {
            [SerializeField] private Sprite _sprite;
            [SerializeField] private RectInt _rect;

            public Sprite Sprite => _sprite;
            public RectInt Rect => _rect;
        }

        [SerializeField] private Vector2Int _canvas = new Vector2Int(1080, 1350);
        [SerializeField] private Layer _line;
        [SerializeField] private List<Layer> _steps = new List<Layer>();

        public Vector2Int Canvas => _canvas;
        public Layer Line => _line;
        public IReadOnlyList<Layer> Steps => _steps;
    }
}
