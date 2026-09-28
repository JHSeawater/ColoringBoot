using System;
using System.Linq;
using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    // 랜덤 생성기 — 같은 시드 · 조건이면 프로토타입 generate와 같은 스테이지 (기준값: Prototype.html을 Node로 실행, 2026-09-29)
    public class GeneratorTests
    {
        [TestCase(12345u, BoardShape.SmallHex, 3, 4, 3, "1,2,4", true, 0.34, "SmallHex", 4, 8, 24,
            "[-2,0,0,0],[-2,1,0,2],[-2,2,0,0],[-1,-1,0,0],[-1,0,0,0],[-1,1,0,2],[-1,2,0,0],[0,-2,0,3],[0,-1,0,2],[0,0,0,2],[0,1,0,2],[0,2,0,2],[1,-2,0,1],[1,-1,0,0],[1,0,0,0],[1,1,0,2],[2,-2,1,1],[2,-1,2,2],[2,0,2,2]")]
        [TestCase(2024u, BoardShape.Blob, 3, 5, 5, "1,2,4", true, 0.2, "Blob", 5, 6, 120,
            "[0,0,2,2],[-1,1,0,0],[-2,2,0,4],[-1,2,0,4],[0,1,0,0],[-2,3,4,4],[0,2,0,4],[1,2,0,6],[2,1,0,0],[-1,3,0,0],[-2,1,0,4],[-2,0,0,4],[1,1,0,2],[3,0,0,2],[-3,3,0,0],[1,0,0,2],[1,-1,0,4],[0,3,0,0],[2,-1,0,4],[3,-1,4,4]")]
        [TestCase(777u, BoardShape.Random, 4, 5, 5, "1,2,4,3,5,6", false, 0.2, "SmallHex", 5, 6, 120,
            "[-2,0,4,4],[-2,1,4,4],[-2,2,0,0],[-1,-1,0,0],[-1,0,0,4],[-1,1,0,4],[-1,2,0,0],[0,-2,0,5],[0,-1,0,4],[0,0,0,0],[0,1,0,4],[0,2,3,3],[1,-2,0,5],[1,-1,0,5],[1,0,0,5],[1,1,0,5],[2,-2,1,1],[2,-1,0,4],[2,0,0,0]")]
        [TestCase(99u, BoardShape.Triangle, 2, 3, 3, "1,2", true, 1.0, "Triangle", 3, 3, 6,
            "[0,0,0,2],[0,1,0,2],[0,2,0,2],[0,3,2,2],[0,4,0,2],[1,0,0,2],[1,1,0,0],[1,2,0,0],[1,3,0,0],[2,0,0,2],[2,1,0,0],[2,2,0,0],[3,0,2,2],[3,1,0,0],[4,0,0,2]")]
        public void MatchesPrototypeGenerator(uint seed, BoardShape shape, int seeds, int moves, int minSolve, string palette, bool allowBlack, double maxOrderRatio,
            string expectedShape, int expectedMin, int expectedOrderSucceeded, int expectedOrderTotal, string expectedCells)
        {
            var options = new GeneratorOptions
            {
                Shape = shape,
                Seeds = seeds,
                Moves = moves,
                MinSolve = minSolve,
                Palette = palette.Split(',').Select(v => (PaintColor)int.Parse(v)).ToArray(),
                AllowBlack = allowBlack,
                MaxOrderRatio = maxOrderRatio,
            };

            GeneratedStage generated = Generator.Generate(options, new SeededRandom(seed), "랜덤");

            Assert.IsNotNull(generated);
            Assert.AreEqual(expectedShape, generated.Shape.ToString());
            Assert.AreEqual(expectedCells, CellsText(generated.Stage));
            Assert.AreEqual(expectedMin, generated.Stage.MinMoves);
            Assert.AreEqual(expectedOrderSucceeded, generated.Order.Value.Succeeded);
            Assert.AreEqual(expectedOrderTotal, generated.Order.Value.Total);

            // 조건: 최소 수 하한 · 순서 비율 상한 · 검정 금지 · 솔버로 다시 풀면 같은 최소 수
            Assert.GreaterOrEqual(generated.Stage.MinMoves.Value, minSolve);
            Assert.LessOrEqual((double)generated.Order.Value.Succeeded / generated.Order.Value.Total, maxOrderRatio);
            if (!allowBlack) Assert.IsFalse(generated.Stage.Cells.Any(c => c.Target == PaintColor.Black));
            var board = new Board(generated.Stage);
            Assert.AreEqual(expectedMin, Solver.Solve(board, board.CreateStartState()).Path.Count);
        }

        [Test]
        public void SameSeed_SameStage()
        {
            var options = new GeneratorOptions { Shape = BoardShape.Random };
            string first = CellsText(Generator.Generate(options, new SeededRandom(42), "a").Stage);
            string second = CellsText(Generator.Generate(options, new SeededRandom(42), "a").Stage);
            Assert.AreEqual(first, second);
        }

        private static string CellsText(Stage stage) =>
            string.Join(",", stage.Cells.Select(c => $"[{c.Coord.Q},{c.Coord.R},{(int)c.Start},{(int)c.Target}]"));
    }
}
