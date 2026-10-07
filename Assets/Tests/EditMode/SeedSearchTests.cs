using System.Linq;
using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    // 그림 맵 시작 칸 찾기 (2026-10-07) — 목표 그림은 그대로, 시작 색은 그 칸 목표 색 안의 색, 찾은 배치는 조건 수에 풀린다
    public class SeedSearchTests
    {
        // 위 줄 초록 · 파랑 셋, 아래 줄 노랑 넷 (따라 하기 복습의 목표 그림) — 시작 색은 비워 둔다
        private const string Picture = @"{""name"":""그림"",""cells"":[[0,0,0,6],[1,0,0,4],[2,0,0,4],[3,0,0,4],[0,1,0,2],[1,1,0,2],[2,1,0,2],[3,1,0,2]]}";

        [Test]
        public void FindsSeedsThatSolveInRange()
        {
            Stage picture = Stage.Parse(Picture);
            var options = new SeedSearchOptions { Seeds = 3, MinMoves = 3, MaxMoves = 3 };
            var random = new SeededRandom(11);

            GeneratedStage found = null;
            for (int i = 0; i < 2000 && found == null; i++) found = SeedSearch.TryOnce(picture, options, random);

            Assert.IsNotNull(found, "배치를 찾음");
            Stage stage = found.Stage;
            CollectionAssert.AreEqual(picture.Cells.Select(c => (c.Coord, c.Target)), stage.Cells.Select(c => (c.Coord, c.Target)), "목표 그림 그대로");
            Assert.AreEqual(3, stage.Cells.Count(c => c.Start != PaintColor.Empty), "시작 색 칸 3개");
            Assert.IsTrue(stage.Cells.All(c => (c.Start & ~c.Target) == PaintColor.Empty), "시작 색은 그 칸 목표 색 안의 색");
            Assert.IsTrue(stage.Cells.All(c => c.Start == PaintColor.Empty || c.Start == PaintColor.Red || c.Start == PaintColor.Yellow || c.Start == PaintColor.Blue), "기본색만(섞인 색 허용 안 함)");
            var board = new Board(stage);
            Assert.AreEqual(3, Solver.Solve(board, board.CreateStartState()).Path.Count, "최소 3수");
            Assert.AreEqual(3, stage.MinMoves);
        }

        [Test]
        public void TooFewPaintableCells_Throws()
        {
            Stage picture = Stage.Parse(@"{""name"":""작음"",""cells"":[[0,0,0,1],[1,0,0,0]]}");
            Assert.Throws<System.ArgumentException>(() => SeedSearch.TryOnce(picture, new SeedSearchOptions { Seeds = 2 }, new SeededRandom(1)));
        }
    }
}
