using UnityEngine;

namespace ColoringBoot.Game
{
    // 챕터 (Phase 8 구조 정리, 2026-10-07) — 제목 · 부제(장소와 계절, GDD §5) · 스테이지 목록 · 그림. 스테이지 i ↔ 그림 단계 i.
    // 챕터마다 Assets/Data/Chapters/ChapterN/Chapter.asset 하나. 제목 · 부제 글자는 AgentScripts/Build/FontBuilder.cs가 모은다
    [CreateAssetMenu(menuName = "ColoringBoot/Chapter", fileName = "Chapter")]
    public sealed class Chapter : ScriptableObject
    {
        [SerializeField] private string _title = "";
        [SerializeField] private string _subtitle = "";
        [SerializeField] private StageCatalog _stages;
        [SerializeField] private ChapterArt _art;

        public string Title => _title;
        public string Subtitle => _subtitle;
        public StageCatalog Stages => _stages;
        public ChapterArt Art => _art;
    }
}
