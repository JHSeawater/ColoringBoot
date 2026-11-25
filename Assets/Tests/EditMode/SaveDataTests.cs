using System.Collections.Generic;
using System.Text.RegularExpressions;
using ColoringBoot.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ColoringBoot.Core.Tests
{
    // 읽지 못한 저장 값을 SaveData가 다루는 법 (2026-11-19, Docs/plans/2026-11-18-save-read-failure.md) —
    // 경고 로그 1회 · 덮일 원본은 백업 키에(최근 원본으로) · 새 버전 값은 그 실행 동안 쓰지 않음 · ?reset은 읽기 전에 백업까지 지운다
    public class SaveDataTests
    {
        private const string ProgressKey = "ColoringBoot.Progress";
        private const string StatsKey = "ColoringBoot.PlayStats";
        private const string EndlessKey = "ColoringBoot.Endless";
        private const string HintsKey = "ColoringBoot.Hints";
        private const string Backup = ".Unread";
        private const long Now = 1_800_000_000;   // 힌트 테스트 기준 시각(유닉스 초)
        private static readonly string[] RecordKeys = { ProgressKey, StatsKey, EndlessKey, HintsKey };

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
            else if (key == EndlessKey) data.SaveEndless();
            else data.SaveHints();
        }

        private static string Serialized(SaveData data, string key) =>
            key == ProgressKey ? data.Progress.Serialize() : key == StatsKey ? data.Stats.Serialize()
            : key == EndlessKey ? data.Endless.Serialize() : data.Hints.Serialize();

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
        [TestCase(HintsKey, "garbage")]
        [TestCase(HintsKey, "hints 1\n9\t100\n")]
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

        // 힌트 개수 (2026-11-25, Docs/plans/2026-11-25-hint-stock.md) — 저장 값이 없으면 3개, 쓴 뒤 저장하면 다시 만든 SaveData가 같은 개수 · 같은 충전 시각
        [Test]
        public void Hints_SavedCountSurvivesReload()
        {
            var store = new MemoryStore();
            var data = new SaveData(store);
            Assert.AreEqual(HintStock.Max, data.Hints.Count(Now), "처음 실행은 3개");
            Assert.IsNull(store.Load(HintsKey), "읽기만으로 키가 생기지 않음");

            Assert.IsTrue(data.Hints.TrySpend(Now));
            Assert.IsTrue(data.Hints.TrySpend(Now + 60));
            data.SaveHints();
            var again = new SaveData(store);

            Assert.AreEqual(1, again.Hints.Count(Now + 60));
            Assert.AreEqual(HintStock.ChargeSeconds - 60, again.Hints.SecondsToNext(Now + 60), "충전 시계는 첫 힌트를 쓴 시각부터");
            Assert.AreEqual(2, again.Hints.Count(Now + HintStock.ChargeSeconds), "꺼져 있던 시간도 친다");
            LogAssert.NoUnexpectedReceived();
        }

        // 깨진 힌트 값은 원본을 백업하고 3개로 시작한다(손해 방향이 플레이어 쪽이라 받아들임)
        [Test]
        public void Hints_BrokenValue_BacksUpAndStartsFull()
        {
            const string broken = "hints 1\n-1\t100\n";
            var store = new MemoryStore();
            store.Values[HintsKey] = broken;
            ExpectWarning(HintsKey);

            var data = new SaveData(store);

            Assert.AreEqual(HintStock.Max, data.Hints.Count(Now));
            Assert.AreEqual(broken, store.Load(HintsKey + Backup));
            LogAssert.NoUnexpectedReceived();
        }

        // 일부 줄만 버려 백업이 생긴 뒤, 다음에 값 전체가 깨지면 그 원본(진행 대부분)이 백업에 남아야 한다 (2026-11-19 코드 리뷰)
        [Test]
        public void DroppedValue_ReplacesBackupWithLatestOriginal()
        {
            const string earlier = "progress 1\nGrape\t5\nHive\tx\n";
            var store = new MemoryStore();
            store.Values[ProgressKey] = earlier;
            ExpectWarning(ProgressKey);
            var data = new SaveData(store);
            data.SaveProgress();                       // 첫 실행: 백업 = earlier, 키는 읽은 만큼으로 덮임
            Assert.AreEqual(earlier, store.Load(ProgressKey + Backup));
            LogAssert.NoUnexpectedReceived();

            store.Values[ProgressKey] = "garbage";     // 다음 실행: 값 전체가 깨짐
            ExpectWarning(ProgressKey);

            new SaveData(store);

            Assert.AreEqual("garbage", store.Load(ProgressKey + Backup), "백업은 이번에 버린 원본으로");
            LogAssert.NoUnexpectedReceived();
        }

        [TestCase(ProgressKey, "progress 2\nGrape\t5\n")]
        [TestCase(StatsKey, "stats 3\nGrape\t1\t1\t0\t0\t0\t1.0\t-\t9\n")]
        [TestCase(EndlessKey, "endless 2\nEasy\t5\t4\t1\n")]
        [TestCase(HintsKey, "hints 2\n1\t100\n")]
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
            data.Hints.TrySpend(Now);
            foreach (string k in RecordKeys) Save(data, k);

            Assert.AreEqual(text, store.Load(key), "옛 빌드가 새 형식 값을 덮지 않음");
            LogAssert.NoUnexpectedReceived();   // 막힌 저장마다 로그를 더 남기지 않음
            foreach (string other in RecordKeys)
            {
                if (other != key) Assert.AreEqual(Serialized(data, other), store.Load(other), $"{other}는 정상 저장");
            }
        }

        // 저장이 없어 지난 실행의 깨진 값이 그대로 남으면, 다음 실행부터는 경고를 되풀이하지 않는다(일반 로그) (2026-11-19 코드 리뷰)
        [Test]
        public void DroppedValue_SameOriginalNextRun_LogsWithoutWarning()
        {
            var store = new MemoryStore();
            store.Values[EndlessKey] = "endless 1\nEasy\t5\t4\t1\nbroken line\n";
            ExpectWarning(EndlessKey);
            new SaveData(store);                       // 첫 실행: 백업 · 경고. 무한 모드를 하지 않아 저장 없음
            LogAssert.NoUnexpectedReceived();

            LogAssert.Expect(LogType.Log, new Regex(@"^\[저장\] " + Regex.Escape(EndlessKey) + ":"));

            new SaveData(store);

            Assert.AreEqual(store.Load(EndlessKey), store.Load(EndlessKey + Backup));
            LogAssert.NoUnexpectedReceived();          // 두 번째 실행에 경고 없음
        }

        // ?reset은 SaveData를 만들기 전에 지운다 — 곧 지울 값에 경고 · 백업이 생기지 않고, 새 버전 값이었던 키도 그 실행에서 정상 저장
        [Test]
        public void ClearRecords_BeforeLoad_DeletesRecordsAndBackupsWithoutWarning()
        {
            var store = new MemoryStore();
            store.Values[ProgressKey] = "progress 2\nGrape\t5\n";
            store.Values[StatsKey] = "garbage";
            store.Values[EndlessKey + Backup] = "endless x";
            store.Values[HintsKey] = "hints 1\n0\t1800000000\n";
            store.Values[HintsKey + Backup] = "hints x";
            store.Values["ColoringBoot.Tutorial"] = "1";
            store.Values["ColoringBoot.Sound"] = "0";

            SaveData.ClearRecords(store);
            var data = new SaveData(store);

            LogAssert.NoUnexpectedReceived();
            foreach (string key in RecordKeys)
            {
                Assert.IsNull(store.Load(key), $"{key} 지움");
                Assert.IsNull(store.Load(key + Backup), $"{key}{Backup} 지움");
            }
            Assert.AreEqual(HintStock.Max, data.Hints.Count(Now), "힌트도 처음 상태(3개)로");
            Assert.IsFalse(data.TutorialSeen, "따라 하기 기록도 지움");
            Assert.IsFalse(data.Sound, "설정은 남김");
            data.Progress.RecordClear("Grape", 7);
            data.SaveProgress();
            Assert.AreEqual(data.Progress.Serialize(), store.Load(ProgressKey), "?reset 뒤에는 그 실행에서도 정상 저장");
        }
    }
}
