using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    public class SolverTests
    {
        private const PaintColor E = PaintColor.Empty;
        private const PaintColor R = PaintColor.Red;
        private const PaintColor Y = PaintColor.Yellow;

        private static Board MakeRow(PaintColor[] start, PaintColor[] target)
        {
            var cells = new StageCell[start.Length];
            for (int q = 0; q < start.Length; q++) cells[q] = new StageCell(new HexCoord(q, 0), start[q], target[q]);
            return new Board(new Stage("row", cells));
        }

        // 회귀 기준 (GDD §3): 포도 최소 5수, 솔버가 고른 풀이 = GDD 풀이(프로토타입과 같은 탐색 순서) → 120가지 중 8가지
        [Test]
        public void Grape_MinimumFiveMoves_EightOf120Orders()
        {
            var board = new Board(Stage.Parse(TestStages.Grape));
            PaintColor[] start = board.CreateStartState();

            SolveResult result = Solver.Solve(board, start);

            Assert.IsTrue(result.Solved);
            Assert.AreEqual(5, result.Path.Count);
            Assert.AreEqual(67, result.Explored); // 프로토타입 엔진 실측값 (DevelopLog 2026-09-28)
            OrderSensitivity? order = Solver.MeasureOrder(board, start, result.Path);
            Assert.AreEqual(8, order.Value.Succeeded);
            Assert.AreEqual(120, order.Value.Total);

            PaintColor[] state = board.CreateStartState();
            foreach (BoardMove move in result.Path) board.Brush(state, move.Cell, move.Direction);
            Assert.IsTrue(board.IsSolved(state));
        }

        [Test]
        public void AlreadySolved_EmptyPath()
        {
            Board board = MakeRow(new[] { R, R }, new[] { R, R });
            SolveResult result = Solver.Solve(board, board.CreateStartState());
            Assert.IsTrue(result.Solved);
            Assert.AreEqual(0, result.Path.Count);
        }

        [Test]
        public void DeadStart_ReportsDead()
        {
            Board board = MakeRow(new[] { Y, E }, new[] { R, R });
            SolveResult result = Solver.Solve(board, board.CreateStartState());
            Assert.IsFalse(result.Solved);
            Assert.IsTrue(result.DeadAtStart);
        }

        // 목표에 필요한 색이 어디에도 없으면 풀이 없음(상한에 걸린 것도 아님)
        [Test]
        public void Unsolvable_NotSolvedNotLimited()
        {
            Board board = MakeRow(new[] { R, E, E }, new[] { R, R, PaintColor.Orange });
            SolveResult result = Solver.Solve(board, board.CreateStartState());
            Assert.IsFalse(result.Solved);
            Assert.IsFalse(result.Limited);
            Assert.IsFalse(result.DeadAtStart);
        }

        [Test]
        public void Limit_StopsSearch()
        {
            var board = new Board(Stage.Parse(TestStages.Grape));
            SolveResult result = Solver.Solve(board, board.CreateStartState(), 10);
            Assert.IsFalse(result.Solved);
            Assert.IsTrue(result.Limited);
        }

        [Test]
        public void MeasureOrder_NullOutsideTwoToSevenMoves()
        {
            var board = new Board(Stage.Parse(TestStages.Grape));
            Assert.IsNull(Solver.MeasureOrder(board, board.CreateStartState(), new[] { board.Moves[0] }));
        }
    }
}
