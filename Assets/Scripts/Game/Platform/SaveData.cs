using ColoringBoot.Core;

namespace ColoringBoot.Game
{
    // 저장하는 것 전부 — 진행(Progress) · 플레이테스트 기록(PlayStats) · 설정(기호 · 소리). 저장소는 인터페이스 뒤(CLAUDE.md §3)
    public sealed class SaveData
    {
        private const string ProgressKey = "ColoringBoot.Progress";
        private const string StatsKey = "ColoringBoot.PlayStats";
        private const string SymbolsKey = "ColoringBoot.Symbols";
        private const string SoundKey = "ColoringBoot.Sound";
        private const string TutorialKey = "ColoringBoot.Tutorial";   // 따라 하기를 봤는가 (2026-10-07)
        private const string On = "1";
        private const string Off = "0";

        private readonly IKeyValueStore _store;
        private bool _symbols;
        private bool _sound;
        private bool _tutorialSeen;

        public SaveData(IKeyValueStore store)
        {
            _store = store;
            Progress = Progress.Deserialize(store.Load(ProgressKey));
            Stats = PlayStats.Deserialize(store.Load(StatsKey));
            _symbols = store.Load(SymbolsKey) == On;   // 기본 끔
            _sound = store.Load(SoundKey) != Off;      // 기본 켬
            _tutorialSeen = store.Load(TutorialKey) == On;
        }

        public Progress Progress { get; private set; }
        public PlayStats Stats { get; private set; }

        public bool Symbols
        {
            get => _symbols;
            set
            {
                _symbols = value;
                _store.Save(SymbolsKey, value ? On : Off);
            }
        }

        public bool Sound
        {
            get => _sound;
            set
            {
                _sound = value;
                _store.Save(SoundKey, value ? On : Off);
            }
        }

        // 따라 하기(튜토리얼)를 봤는가 — 처음 시작할 때만 자동으로 연다
        public bool TutorialSeen
        {
            get => _tutorialSeen;
            set
            {
                _tutorialSeen = value;
                _store.Save(TutorialKey, value ? On : Off);
            }
        }

        public void SaveProgress() => _store.Save(ProgressKey, Progress.Serialize());
        public void SaveStats() => _store.Save(StatsKey, Stats.Serialize());

        // 주소 ?reset — 진행 · 플레이테스트 기록을 지운다(설정은 남긴다). 휴대폰 하나로 여러 명이 테스트할 때 — 다음 사람이 따라 하기부터 보게 그 기록도 지운다
        public void ClearRecords()
        {
            _store.Delete(ProgressKey);
            _store.Delete(StatsKey);
            _store.Delete(TutorialKey);
            Progress = new Progress();
            Stats = new PlayStats();
            _tutorialSeen = false;
        }
    }
}
