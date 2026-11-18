using System.Collections.Generic;
using System.Text.RegularExpressions;
using ColoringBoot.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ColoringBoot.Core.Tests
{
    // 읽지 못한 저장 값을 SaveData가 다루는 법 (2026-11-19, Docs/plans/2026-11-18-save-read-failure.md) —
    // 경고 로그 1회 · 덮일 원본은 백업 키에 · 새 버전 값은 그 실행 동안 쓰지 않음 · ?reset은 백업도 지우고 쓰기 막기를 푼다
    public class SaveDataTests
    {
        private const string ProgressKey = "ColoringBoot.Progress";
        private const string StatsKey = "ColoringBoot.PlayStats";
        private const string EndlessKey = "ColoringBoot.Endless";
        private const string Backup = ".Unread";
        private static readonly string[] RecordKeys = { ProgressKey, StatsKey, EndlessKey };

        private sealed class MemoryStore : IKeyValueStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public string Load(string key) => Values.TryGetValue(key, out string value) ? value : null;
            public void Save(string key, string value) => Values[key] = value;
            public void Delete(string key) => Values.Remove(key);
        }

        // 그 키의 경고 1회를 기대한다 — 더 나오거나 다른 로그가 섞이면 NoUnexpectedReceived가 실패시킨다
        private static void ExpectWarning(string key) =>
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[저장\] " + Regex.Escape(key) + ":"));

        private static void Save(SaveData data, string key)
        {
            if (key == ProgressKey) data.SaveProgress();
            else if (key == StatsKey) data.SaveStats();
            else data.SaveEndless();
        }

        private static string Serialized(SaveData data, string key) =>
            key == ProgressKey ? data.Progress.Serialize() : key == StatsKey ? data.Stats.Serialize() : data.Endless.Serialize();

        [Test]
        public void NormalOrMissingValues_NoWarningNoBackup()
        {
            var progress = new Progress();
            progress.RecordClear("Grape", 5);
            var stats = new PlayStats();
            stats.Opened("Grape");
            var store = new MemoryStore();
            store.Values[ProgressKey] = progress.Serialize();
            store.Values[StatsKey] = stats.Serialize();   // 무한 모드 값은 없음 — 첫 실행과 같다
            var before = new Dictionary<string, string>(store.Values);

            var data = new SaveData(store);

            LogAssert.NoUnexpectedReceived();
            CollectionAssert.AreEquivalent(before, store.Values, "읽기만으로 키가 생기거나 바뀌지 않음");
            Assert.AreEqual(5, data.Progress.BestMoves("Grape"));
            Assert.AreEqual(1, data.Stats.Get("Grape").Opens);
        }

        [TestCase(ProgressKey, "garbage")]
        [TestCase(ProgressKey, "progress 1\nGrape\t5\nHive\tx\n")]
        [TestCase(StatsKey, "stats x\nGrape\t1\t2\t0\t0\t0\t3.0\t-\n")]
        [TestCase(StatsKey, "stats 2\nGrape\t1\t2\t0\t0\t0\t3.0\t-\nbroken\n")]
        [TestCase(EndlessKey, "progress 1\nGrape\t5\n")]
        [TestCase(EndlessKey, "endless 1\nEasy\t5\t4\t1\nbroken line\n")]
        public void DroppedValue_WarnsOnceBacksUpOriginalAndKeepsSaving(string key, string text)
        {
            var store = new MemoryStore();
            store.Values[key] = text;
            ExpectWarning(key);

            var data = new SaveData(store);

            Assert.AreEqual(text, store.Load(key + Backup), "원본을 백업 키에");
            Assert.AreEqual(text, store.Load(key), "읽기만으로 원래 키는 그대로");

            Save(data, key);

            Assert.AreEqual(Serialized(data, key), store.Load(key), "백업 뒤에는 지금처럼 저장");
            Assert.AreEqual(text, store.Load(key + Backup), "저장해도 백업은 그대로");
            LogAssert.NoUnexpectedReceived();   // 저장할 때 로그를 더 남기지 않음
        }

        [Test]
        public void DroppedValue_KeepsFirstBackup()
        {
            var store = new MemoryStore();
            store.Values[ProgressKey] = "garbage 2";
            store.Values[ProgressKey + Backup] = "garbage 1";
            ExpectWarning(ProgressKey);

            new SaveData(store);

            Assert.AreEqual("garbage 1", store.Load(ProgressKey + Backup), "처음 버린 원본을 지킨다");
            LogAssert.NoUnexpectedReceived();
        }

        [TestCase(ProgressKey, "progress 2\nGrape\t5\n")]
        [TestCase(StatsKey, "stats 3\nGrape\t1\t1\t0\t0\t0\t1.0\t-\t9\n")]
        [TestCase(EndlessKey, "endless 2\nEasy\t5\t4\t1\n")]
        public void NewerVersion_WarnsOnceAndDoesNotWriteThatKey(string key, string text)
        {
            var store = new MemoryStore();
            store.Values[key] = text;
            ExpectWarning(key);

            var data = new SaveData(store);

            Assert.IsNull(store.Load(key + Backup), "새 버전은 백업하지 않음");

            data.Progress.RecordClear("Grape", 7);
            data.Stats.Opened("Grape");
            data.Endless.Solve("Easy", 3);
            foreach (string k in RecordKeys) Save(data, k);

            Assert.AreEqual(text, store.Load(key), "옛 빌드가 새 형식 값을 덮지 않음");
            LogAssert.NoUnexpectedReceived();   // 막힌 저장마다 로그를 더 남기지 않음
            foreach (string other in RecordKeys)
            {
                if (other != key) Assert.AreEqual(Serialized(data, other), store.Load(other), $"{other}는 정상 저장");
            }
        }

        [Test]
        public void ClearRecords_DeletesBackupsAndUnblocksNewerVersionKey()
        {
            var store = new MemoryStore();
            store.Values[ProgressKey] = "progress 2\nGrape\t5\n";
            store.Values[StatsKey] = "garbage";
            store.Values[EndlessKey + Backup] = "endless x";
            ExpectWarning(ProgressKey);
            ExpectWarning(StatsKey);
            var data = new SaveData(store);

            data.ClearRecords();

            foreach (string key in RecordKeys) Assert.IsNull(store.Load(key + Backup), $"{key}{Backup} 지움");
            data.Progress.RecordClear("Grape", 7);
            data.SaveProgress();
            Assert.AreEqual(data.Progress.Serialize(), store.Load(ProgressKey), "?reset 뒤에는 그 실행에서도 정상 저장");
        }
    }
}
