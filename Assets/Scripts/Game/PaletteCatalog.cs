using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColoringBoot.Game
{
    // 팔레트 목록 — 스테이지 JSON의 palette 이름으로 팔레트 에셋을 고른다 (GDD §2.3). 첫 팔레트가 기본 팔레트
    [CreateAssetMenu(menuName = "ColoringBoot/Palette Catalog", fileName = "PaletteCatalog")]
    public sealed class PaletteCatalog : ScriptableObject
    {
        [Tooltip("첫 팔레트 = 기본 팔레트 (스테이지에 palette가 없을 때)")]
        [SerializeField] private List<ColorPalette> _palettes = new List<ColorPalette>();

        public IReadOnlyList<ColorPalette> Palettes => _palettes;
        public ColorPalette Default => _palettes[0];

        // 이름으로 찾는다(대소문자 무시). 이름이 없으면(null · 빈 문자열) 기본 팔레트, 모르는 이름이면 -1
        public int IndexOf(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return 0;
            for (int i = 0; i < _palettes.Count; i++)
            {
                if (string.Equals(_palettes[i].Id, id.Trim(), StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }
    }
}
