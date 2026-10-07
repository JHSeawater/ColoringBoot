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

        // 생성 조건 (2026-10-07): 직접 그린 모양 · 목표에 쓸 색 · 섞인 색 비율 · 막힐 수 있는 칸 비율
        [Test]
        public void Conditions_AreMet()
        {
            HexCoord[] drawn = Enumerable.Range(0, 4).SelectMany(q => Enumerable.Range(0, 3).Select(r => new HexCoord(q, r))).ToArray(); // 12칸 평행사변형
            PaintColor[] allowed = { PaintColor.Red, PaintColor.Yellow, PaintColor.Orange };
            var options = new GeneratorOptions
            {
                Cells = drawn,
                Seeds = 3,
                Moves = 3,
                MinSolve = 3,
                Palette = new[] { PaintColor.Red, PaintColor.Yellow },
                MaxOrderRatio = 1.0,
                TargetColors = allowed,
                MinMixedRatio = 0.3,
                MinTrapRatio = 0.3,
            };

            GeneratedStage generated = null;
            var random = new SeededRandom(7);
            for (int i = 0; i < 50 && generated == null; i++) generated = Generator.Generate(options, random, "조건");

            Assert.IsNotNull(generated, "조건에 맞는 스테이지를 찾음");
            Assert.AreEqual(BoardShape.Drawn, generated.Shape);
            CollectionAssert.AreEquivalent(drawn, generated.Stage.Cells.Select(c => c.Coord), "그린 칸 그대로");
            Assert.IsTrue(generated.Stage.Cells.All(c => c.Target == PaintColor.Empty || allowed.Contains(c.Target)), "목표 색");
            StageMetrics metrics = StageMetrics.Measure(new Board(generated.Stage));
            Assert.GreaterOrEqual(metrics.MixedRatio, 0.3, "섞인 색 비율");
            Assert.GreaterOrEqual(metrics.TrapRatio, 0.3, "막힐 수 있는 칸 비율");
        }

        private static string CellsText(Stage stage) =>
            string.Join(",", stage.Cells.Select(c => $"[{c.Coord.Q},{c.Coord.R},{(int)c.Start},{(int)c.Target}]"));
    }
}
