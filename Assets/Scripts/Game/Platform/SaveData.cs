using System.Collections.Generic;
using ColoringBoot.Core;
using UnityEngine;

namespace ColoringBoot.Game
{
    // 저장하는 것 전부 — 진행(Progress) · 플레이테스트 기록(PlayStats) · 무한 모드 진행(EndlessProgress) · 설정(기호 · 소리). 저장소는 인터페이스 뒤(CLAUDE.md §5 플랫폼 서비스)
    public sealed class SaveData
    {
        private const string ProgressKey = "ColoringBoot.Progress";
        private const string StatsKey = "ColoringBoot.PlayStats";
        private const string SymbolsKey = "ColoringBoot.Symbols";
        private const string SoundKey = "ColoringBoot.Sound";
        private const string TutorialKey = "ColoringBoot.Tutorial";   // 따라 하기를 봤는가 (2026-10-07)
        private const string EndlessKey = "ColoringBoot.Endless";     // 무한 모드 진행 (2026-10-07)
        private const string UnreadSuffix = ".Unread";                // 읽다 버린 원본 백업 (2026-11-19)
        private const string On = "1";
        private const string Off = "0";
        private static readonly string[] RecordKeys = { ProgressKey, StatsKey, EndlessKey };   // 읽기 상태 · 백업 · ?reset 대상

        private delegate T Parse<T>(string text, out RecordReadStatus status);

        private readonly IKeyValueStore _store;
        private readonly HashSet<string> _blocked = new HashSet<string>();   // 새 버전 값이라 이번 실행 동안 쓰지 않는 키
        private bool _symbols;
        private bool _sound;
        private bool _tutorialSeen;

        public SaveData(IKeyValueStore store)
        {
            _store = store;
            Progress = Read<Progress>(ProgressKey, Progress.Deserialize);
            Stats = Read<PlayStats>(StatsKey, PlayStats.Deserialize);
            Endless = Read<EndlessProgress>(EndlessKey, EndlessProgress.Deserialize);
            _symbols = store.Load(SymbolsKey) == On;   // 기본 끔
            _sound = store.Load(SoundKey) != Off;      // 기본 켬
            _tutorialSeen = store.Load(TutorialKey) == On;
        }

        public Progress Progress { get; }
        public PlayStats Stats { get; }
        public EndlessProgress Endless { get; }

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

        public void SaveProgress() => SaveRecord(ProgressKey, Progress.Serialize());
        public void SaveStats() => SaveRecord(StatsKey, Stats.Serialize());
        public void SaveEndless() => SaveRecord(EndlessKey, Endless.Serialize());

        private void SaveRecord(string key, string text)
        {
            if (!_blocked.Contains(key)) _store.Save(key, text);
        }

        // 읽다 버린 것이 있으면 로그 · 원본 보존 (2026-11-19, DECISIONS.md) — 게임은 읽은 만큼으로 시작한다.
        // 깨진 값 · 일부 줄 버림: 다음 저장이 원본을 덮으므로 <키>.Unread에 복사(이미 있으면 이번 원본으로 바꿈 — 진행이 가장 많이 남은 최근 원본을 지킨다) 뒤 지금처럼 저장.
        // 백업과 같은 원본이면(저장이 없어 지난 실행의 깨진 값이 그대로 남음) 쓰지 않고 경고 대신 일반 로그 — 실행마다 같은 경고가 쌓이지 않게.
        // 새 버전 값(옛 빌드가 새 빌드의 값을 읽음): 백업 없이 이번 실행 동안 그 키에 쓰지 않는다 — 원본이 그대로 남는다
        private T Read<T>(string key, Parse<T> parse)
        {
            string text = _store.Load(key);
            T record = parse(text, out RecordReadStatus status);
            if (status == RecordReadStatus.Ok) return record;
            if (status == RecordReadStatus.NewerVersion)
            {
                _blocked.Add(key);
                Debug.LogWarning($"[저장] {key}: 이 빌드보다 새 형식이라 읽지 않았습니다 — 이번 실행 동안 이 기록은 저장하지 않습니다");
                return record;
            }
            string backupKey = key + UnreadSuffix;
            string what = status == RecordReadStatus.LinesDropped ? "읽을 수 없는 줄을 건너뛰었습니다" : "읽을 수 없어 빈 기록으로 시작합니다";
            if (_store.Load(backupKey) == text)
            {
                Debug.Log($"[저장] {key}: {what} — 같은 원본이 이미 {backupKey}에 있습니다");
                return record;
            }
            _store.Save(backupKey, text);
            Debug.LogWarning($"[저장] {key}: {what} — 원본을 {backupKey}에 백업했습니다");
            return record;
        }

        // 주소 ?reset — 진행 · 플레이테스트 기록 · 무한 모드 진행과 읽다 버린 원본 백업을 지운다(설정은 남긴다). 휴대폰 하나로 여러 명이 테스트할 때 —
        // 다음 사람이 따라 하기부터 보게 그 기록도 지운다. SaveData를 만들기 전에 부른다 — 읽은 뒤면 곧 지울 값에 경고 · 백업이 생긴다
        public static void ClearRecords(IKeyValueStore store)
        {
            foreach (string key in RecordKeys)
            {
                store.Delete(key);
                store.Delete(key + UnreadSuffix);
            }
            store.Delete(TutorialKey);
        }
    }
}
