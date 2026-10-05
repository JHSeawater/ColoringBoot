using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColoringBoot.Game
{
    // 게임이 여는 스테이지 목록 — 스테이지 코드(JSON TextAsset)를 순서대로 (CLAUDE.md §3 저장 형식).
    // Phase 4 스테이지 선택 화면도 이 목록을 쓴다. 레벨 에디터가 저장하면 끝에 등록된다
    [CreateAssetMenu(menuName = "ColoringBoot/Stage Catalog", fileName = "StageCatalog")]
    public sealed class StageCatalog : ScriptableObject
    {
        [SerializeField] private List<TextAsset> _stages = new List<TextAsset>();

        public IReadOnlyList<TextAsset> Stages => _stages;

        // 파일 이름(확장자 없이, 대소문자 무시)으로 찾는다. 없으면 null
        public TextAsset Find(string name)
        {
            foreach (TextAsset stage in _stages)
            {
                if (stage != null && string.Equals(stage.name, name, StringComparison.OrdinalIgnoreCase)) return stage;
            }
            return null;
        }
    }
}
