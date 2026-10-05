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
        private const string On = "1";
        private const string Off = "0";

        private readonly IKeyValueStore _store;
        private bool _symbols;
        private bool _sound;

        public SaveData(IKeyValueStore store)
        {
            _store = store;
            Progress = Progress.Deserialize(store.Load(ProgressKey));
            Stats = PlayStats.Deserialize(store.Load(StatsKey));
            _symbols = store.Load(SymbolsKey) == On;   // 기본 끔
            _sound = store.Load(SoundKey) != Off;      // 기본 켬
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

        public void SaveProgress() => _store.Save(ProgressKey, Progress.Serialize());
        public void SaveStats() => _store.Save(StatsKey, Stats.Serialize());

        // 주소 ?reset — 진행 · 플레이테스트 기록을 지운다(설정은 남긴다). 휴대폰 하나로 여러 명이 테스트할 때
        public void ClearRecords()
        {
            _store.Delete(ProgressKey);
            _store.Delete(StatsKey);
            Progress = new Progress();
            Stats = new PlayStats();
        }
    }
}
