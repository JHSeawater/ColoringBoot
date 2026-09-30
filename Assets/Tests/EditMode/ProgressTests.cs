using System;
using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    public class ProgressTests
    {
        private static readonly string[] Order = { "TwoColors", "BrushChanges", "Grape" };

        [Test]
        public void RecordClear_KeepsFewestMoves()
        {
            var progress = new Progress();

            Assert.IsTrue(progress.RecordClear("Grape", 7), "첫 클리어");
            Assert.IsFalse(progress.RecordClear("Grape", 9), "더 많은 수는 기록 안 함");
            Assert.IsFalse(progress.RecordClear("Grape", 7), "같은 수도 새 기록 아님");
            Assert.IsTrue(progress.RecordClear("Grape", 5), "더 적은 수");

            Assert.IsTrue(progress.IsCleared("Grape"));
            Assert.AreEqual(5, progress.BestMoves("Grape"));
            Assert.IsFalse(progress.IsCleared("Hive"));
            Assert.IsNull(progress.BestMoves("Hive"));
        }

        [Test]
        public void IsPerfect_OnlyWhenBestReachesMinMoves()
        {
            var progress = new Progress();
            Assert.IsFalse(progress.IsPerfect("Grape", 5), "클리어 전");

            progress.RecordClear("Grape", 6);
            Assert.IsFalse(progress.IsPerfect("Grape", 5));
            Assert.IsFalse(progress.IsPerfect("Grape", null), "최소 수가 없는 스테이지");

            progress.RecordClear("Grape", 5);
            Assert.IsTrue(progress.IsPerfect("Grape", 5));
        }

        [Test]
        public void IsUnlocked_FirstStageOrPreviousCleared()
        {
            var progress = new Progress();
            Assert.IsTrue(progress.IsUnlocked(Order, 0));
            Assert.IsFalse(progress.IsUnlocked(Order, 1));
            Assert.IsFalse(progress.IsUnlocked(Order, 2));

            progress.RecordClear("TwoColors", 3);
            Assert.IsTrue(progress.IsUnlocked(Order, 1));
            Assert.IsFalse(progress.IsUnlocked(Order, 2));

            // 바로 앞만 본다 — 목록 중간에 새 스테이지가 끼어도 뒤 스테이지가 통째로 잠기지 않게
            progress.RecordClear("BrushChanges", 3);
            Assert.IsTrue(progress.IsUnlocked(new[] { "TwoColors", "NewStage", "BrushChanges", "Grape" }, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.IsUnlocked(Order, 3));
        }

        [Test]
        public void NextStage_FirstUnlockedNotCleared()
        {
            var progress = new Progress();
            Assert.AreEqual(0, progress.NextStage(Order));

            progress.RecordClear("TwoColors", 3);
            Assert.AreEqual(1, progress.NextStage(Order));

            progress.RecordClear("BrushChanges", 3);
            progress.RecordClear("Grape", 5);
            Assert.AreEqual(-1, progress.NextStage(Order), "모두 클리어");

            // 뒤에 새 스테이지가 붙으면 그것이 다음
            Assert.AreEqual(3, progress.NextStage(new[] { "TwoColors", "BrushChanges", "Grape", "Hive" }));
        }

        [Test]
        public void Serialize_RoundTrip()
        {
            var progress = new Progress();
            progress.RecordClear("Grape", 5);
            progress.RecordClear("Hive", 9);

            Progress read = Progress.Deserialize(progress.Serialize());

            Assert.AreEqual(5, read.BestMoves("Grape"));
            Assert.AreEqual(9, read.BestMoves("Hive"));
            Assert.IsFalse(read.IsCleared("Stain"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("garbage")]
        [TestCase("progress 2\nGrape\t5\n")]
        public void Deserialize_UnreadableText_GivesEmptyProgress(string text)
        {
            Assert.IsFalse(Progress.Deserialize(text).IsCleared("Grape"));
        }

        [Test]
        public void Deserialize_SkipsBrokenLines()
        {
            Progress read = Progress.Deserialize("progress 1\nGrape\t5\nHive\tx\nStain\t0\n\tfour\nTwoColors\t3\r\n");

            Assert.AreEqual(5, read.BestMoves("Grape"));
            Assert.AreEqual(3, read.BestMoves("TwoColors"), "CRLF 줄 끝");
            Assert.IsFalse(read.IsCleared("Hive"));
            Assert.IsFalse(read.IsCleared("Stain"));
        }

        [Test]
        public void RecordClear_RejectsBadInput()
        {
            var progress = new Progress();
            Assert.Throws<ArgumentException>(() => progress.RecordClear("", 3));
            Assert.Throws<ArgumentException>(() => progress.RecordClear("two words", 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.RecordClear("Grape", 0));
        }
    }
}
