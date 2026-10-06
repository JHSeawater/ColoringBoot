using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    public class PlayStatsTests
    {
        [Test]
        public void Counts_UntilFirstClear()
        {
            var stats = new PlayStats();
            stats.Opened("Grape");
            stats.Stroked("Grape");
            stats.Stroked("Grape");
            stats.Undid("Grape");
            stats.Restarted("Grape");
            stats.Hinted("Grape");
            stats.Hinted("Grape");
            stats.AddTime("Grape", 12.5);
            stats.Cleared("Grape", 5);

            // 클리어 뒤로는 세지 않는다
            stats.Opened("Grape");
            stats.Stroked("Grape");
            stats.Hinted("Grape");
            stats.AddTime("Grape", 30);
            stats.Cleared("Grape", 4);

            StageStats grape = stats.Get("Grape");
            Assert.AreEqual(1, grape.Opens);
            Assert.AreEqual(2, grape.Strokes);
            Assert.AreEqual(1, grape.Undos);
            Assert.AreEqual(1, grape.Restarts);
            Assert.AreEqual(2, grape.Hints);
            Assert.AreEqual(12.5, grape.Seconds, 1e-9);
            Assert.AreEqual(5, grape.ClearMoves);
        }

        [Test]
        public void Get_UnknownStage_IsEmpty()
        {
            StageStats none = new PlayStats().Get("Hive");
            Assert.AreEqual(0, none.Opens);
            Assert.IsNull(none.ClearMoves);
        }

        [Test]
        public void Serialize_RoundTrip()
        {
            var stats = new PlayStats();
            stats.Opened("Grape");
            stats.Stroked("Grape");
            stats.AddTime("Grape", 61.24);
            stats.Cleared("Grape", 5);
            stats.Opened("Hive");
            stats.Undid("Hive");
            stats.Hinted("Hive");

            PlayStats read = PlayStats.Deserialize(stats.Serialize());

            StageStats grape = read.Get("Grape");
            Assert.AreEqual(1, grape.Opens);
            Assert.AreEqual(1, grape.Strokes);
            Assert.AreEqual(61.2, grape.Seconds, 1e-9, "소수 첫째 자리로 저장");
            Assert.AreEqual(5, grape.ClearMoves);
            StageStats hive = read.Get("Hive");
            Assert.AreEqual(1, hive.Undos);
            Assert.AreEqual(1, hive.Hints);
            Assert.IsNull(hive.ClearMoves);
        }

        [Test]
        public void Deserialize_BrokenText_SkipsOrEmpties()
        {
            Assert.AreEqual(0, PlayStats.Deserialize(null).Get("Grape").Opens);
            Assert.AreEqual(0, PlayStats.Deserialize("progress 1\nGrape\t5\n").Get("Grape").Opens, "다른 머리줄");

            PlayStats read = PlayStats.Deserialize("stats 1\nGrape\t2\t9\t1\t0\t30.0\t5\nHive\t1\t-3\t0\t0\t1.0\t-\nStain\t1\t2\n");
            Assert.AreEqual(2, read.Get("Grape").Opens, "옛 형식(stats 1)도 읽음");
            Assert.AreEqual(0, read.Get("Grape").Hints, "옛 형식은 힌트 0");
            Assert.AreEqual(5, read.Get("Grape").ClearMoves);
            Assert.AreEqual(0, read.Get("Hive").Opens, "음수");
            Assert.AreEqual(0, read.Get("Stain").Opens, "칸 수 부족");
        }
    }
}
