using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    // 난이도 지표 — 소재 맵 시험(DevelopLog 2026-10-06)의 Python 계산과 같은 값 (2026-10-07)
    public class StageMetricsTests
    {
        // 시험 목록의 나비(파스텔) · 벌 — 게임 데이터가 바뀌어도 기준이 흔들리지 않게 사본으로 둔다
        private const string Butterfly = @"{""name"":""나비"",""cells"":[[0,0,0,1],[1,0,0,1],[4,0,0,1],[5,0,1,1],[0,1,0,1],[1,1,0,1],[2,1,0,1],[3,1,0,1],[4,1,0,1],[0,2,0,5],[1,2,4,5],[2,2,0,5],[3,2,0,5],[-1,3,0,0],[0,3,0,1],[1,3,0,0],[2,3,1,1],[3,3,0,0],[-2,4,0,1],[-1,4,0,1],[2,4,0,1],[3,4,1,1]]}";
        private const string Bee = @"{""name"":""벌"",""cells"":[[0,0,2,2],[1,0,0,7],[2,0,0,2],[3,0,0,7],[4,0,0,2],[0,1,0,2],[1,1,0,7],[2,1,0,2],[3,1,5,7],[4,1,0,2],[0,2,0,2],[1,2,5,7],[2,2,2,2],[3,2,0,7],[4,2,0,2]]}";

        [TestCase(TestStages.Grape, 10, 10, 10, 10, 74, 33, 620, 288)]
        [TestCase(Butterfly, 22, 18, 19, 4, 1770, 645, 37326, 26296)]
        [TestCase(Bee, 15, 9, 15, 6, 914, 144, 15088, 10111)]
        public void Measure_MatchesReference(string code, int cells, int trap, int painted, int mixed, int states, int silent, int strokes, int dead)
        {
            StageMetrics m = StageMetrics.Measure(new Board(Stage.Parse(code)));

            Assert.IsTrue(m.Complete, "상한 안에서 다 셈");
            Assert.AreEqual(cells, m.Cells, "칸");
            Assert.AreEqual(trap, m.TrapCells, "막힐 수 있는 칸");
            Assert.AreEqual(painted, m.PaintedCells, "칠해지는 칸");
            Assert.AreEqual(mixed, m.MixedCells, "섞인 색 칸");
            Assert.AreEqual(states, m.States, "닿는 상태");
            Assert.AreEqual(silent, m.SilentStates, "조용한 막다른 길");
            Assert.AreEqual(strokes, m.Strokes, "획");
            Assert.AreEqual(dead, m.DeadStrokes, "막히는 획");
        }

        [Test]
        public void Measure_OverLimit_LeavesStateMetricsUnknown()
        {
            StageMetrics m = StageMetrics.Measure(new Board(Stage.Parse(Bee)), 100);

            Assert.IsFalse(m.Complete);
            Assert.IsNull(m.SilentRatio);
            Assert.IsNull(m.DeadStrokeRatio);
            Assert.AreEqual(9, m.TrapCells, "칸 지표는 상한과 상관없음");
        }

        // 섞인 색 하나로 다 칠하는 목표(단색 채우기)는 막힐 수 있는 칸이 없다
        [Test]
        public void SingleMixedFill_HasNoTrapCells()
        {
            var start = new[] { PaintColor.Red, PaintColor.Empty, PaintColor.Yellow, PaintColor.Empty };
            var target = new[] { PaintColor.Orange, PaintColor.Orange, PaintColor.Orange, PaintColor.Orange };

            Assert.AreEqual(0, StageMetrics.CountTrapCells(start, target));
            Assert.AreEqual(4, StageMetrics.CountMixedCells(target, out int painted));
            Assert.AreEqual(4, painted);
        }
    }
}
