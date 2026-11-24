using System.Collections.Generic;
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

        // 2~7수만 잰다(7! = 5040가지 — 8수부터는 40320가지라 재지 않음). 경계 양쪽(2026-11-25 — 지적 M11: 7수 초과가 없었음)
        [TestCase(1, false)]
        [TestCase(2, true)]
        [TestCase(7, true)]
        [TestCase(8, false)]
        public void MeasureOrder_NullOutsideTwoToSevenMoves(int moves, bool measured)
        {
            var board = new Board(Stage.Parse(TestStages.Grape));
            var path = new BoardMove[moves];
            for (int i = 0; i < moves; i++) path[i] = board.Moves[i];
            Assert.AreEqual(measured, Solver.MeasureOrder(board, board.CreateStartState(), path).HasValue);
        }

        // 탐색 결과의 기준은 참조 탐색(ReferenceSolver — SolverSearch와 따로 쓴 단순한 너비 우선, 2026-11-19)이다.
        // 끝까지 한 번에 도는 Solver.Solve도, 나눠 도는 SolverSearch(2026-11-17 힌트 프레임 나누기 — 한 번에 펼치는 상태 수 = 예산)도
        // 그것과 같은 결과를 낸다: 경로의 모든 획 · Explored · Limited · DeadAtStart. 탐색 순서가 바뀌면 여기서 실패한다
        // (Solve도 SolverSearch를 쓰므로 둘끼리만 비교해서는 순서가 바뀐 것을 잡지 못한다. Solve는 예산 int.MaxValue로 도는 것이다).
        // 탐색은 start 배열을 바꾸지 않는다(2026-11-19) — 힌트는 세션이 화면에 쓰는 배열을 그대로 넘긴다(PuzzleSession.StartHint)
        private static readonly int[] Budgets = { 1, 997 };

        // 나눠 도는 탐색의 Step 호출 상한(2026-11-25 — 지적 M10): 한 번에 상태를 budget개까지 펼치고, 한 탐색이 펼치는 상태는 limit + 1개를 넘지 않는다.
        // 이보다 많이 불러야 하면 끝나지 않는 회귀다 — 에디터가 멈추는 대신 실패시킨다(HintTests도 씀)
        internal static long StepCap(int limit, int budget) => ((long)limit + 1) / budget + 2;

        private static void AssertSearchMatchesSolve(Board board, PaintColor[] start, int limit)
        {
            var before = (PaintColor[])start.Clone();
            SolveResult expected = ReferenceSolver.Solve(board, start, limit);
            AssertSameResult(expected, Solver.Solve(board, start, limit), "Solve");
            CollectionAssert.AreEqual(before, start, "Solve 뒤 start 배열");
            foreach (int budget in Budgets)
            {
                var search = new SolverSearch(board, start, limit);
                bool done = search.Step(budget);
                if (!done) Assert.IsNull(search.Result, "끝나기 전에는 결과 없음");
                for (long steps = 1; !done; steps++)
                {
                    if (steps >= StepCap(limit, budget)) Assert.Fail($"예산 {budget}: Step을 {steps}번 불러도 끝나지 않음(상한 {StepCap(limit, budget)})");
                    done = search.Step(budget);
                }
                CollectionAssert.AreEqual(before, start, $"예산 {budget}: 탐색 뒤 start 배열");
                AssertSameResult(expected, search.Result, $"예산 {budget}");
            }
        }

        private static void AssertSameResult(SolveResult expected, SolveResult actual, string label)
        {
            Assert.AreEqual(expected.Solved, actual.Solved, $"{label}: 풀림");
            Assert.AreEqual(expected.Explored, actual.Explored, $"{label}: 탐색 상태 수");
            Assert.AreEqual(expected.Limited, actual.Limited, $"{label}: 상한");
            Assert.AreEqual(expected.DeadAtStart, actual.DeadAtStart, $"{label}: 시작부터 막힘");
            if (!expected.Solved) return;
            Assert.AreEqual(expected.Path.Count, actual.Path.Count, $"{label}: 수");
            for (int i = 0; i < expected.Path.Count; i++)
            {
                Assert.AreEqual(expected.Path[i].Cell, actual.Path[i].Cell, $"{label}: {i}번째 획의 칸");
                Assert.AreEqual(expected.Path[i].Direction, actual.Path[i].Direction, $"{label}: {i}번째 획의 방향");
            }
        }

        [TestCaseSource(typeof(TestStages), nameof(TestStages.GameStageFiles))]
        public void Search_AnyBudget_MatchesSolve_GameStage(string file)
        {
            var board = new Board(TestStages.ReadGameStage(file));
            AssertSearchMatchesSolve(board, board.CreateStartState(), Solver.DefaultLimit);
        }

        // 게임 판의 풀이를 1~(n−1)수 따라간 상태 — 힌트가 탐색을 시작하는 자리다
        [TestCaseSource(typeof(TestStages), nameof(TestStages.GameStageFiles))]
        public void Search_AnyBudget_MatchesSolve_GameStageAlongSolution(string file)
        {
            var board = new Board(TestStages.ReadGameStage(file));
            PaintColor[] state = board.CreateStartState();
            SolveResult solved = Solver.Solve(board, state);
            Assert.IsTrue(solved.Solved, "풀림");
            IReadOnlyList<BoardMove> path = solved.Path;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Assert.IsTrue(board.Brush(state, path[i].Cell, path[i].Direction), $"{i + 1}번째 획");
                AssertSearchMatchesSolve(board, state, Solver.DefaultLimit);
            }
        }

        // 무한 모드 묶음의 시작 상태(Assets/Data/Endless/*.txt, 한 줄에 스테이지 코드 하나) — 난이도마다 세 줄에 하나(300판 중 100판).
        // 전부 돌리면 EditMode 전체가 20초 넘게 늘어 줄였다(사용자 2026-11-19 — 900판 전부도 그날 통과했다)
        [TestCase("Easy")]
        [TestCase("Normal")]
        [TestCase("Hard")]
        public void Search_AnyBudget_MatchesSolve_EndlessPool(string tier)
        {
            string[] lines = TestStages.ReadEndlessPool(tier);
            Assert.Greater(lines.Length, 0, "퍼즐이 있음");
            for (int i = 0; i < lines.Length; i += 3)
            {
                var board = new Board(Stage.Parse(lines[i]));
                AssertSearchMatchesSolve(board, board.CreateStartState(), Solver.DefaultLimit);
            }
        }

        // 호박(챕터 4 사본)의 시작 상태와 막다른 길 다섯 상태(HintTests.PumpkinDeadEnd_SaysUndoFive의 다섯 획) — 힌트 탐색이 가장 무거운 자리.
        // 게임 화면의 상한(HintSearch.RuntimeLimit)과 탐색 도중에 걸리는 상한 셋으로
        [Test]
        public void Search_AnyBudget_MatchesSolve_PumpkinDeadEnd()
        {
            var board = new Board(Stage.Parse(TestStages.Pumpkin));
            PaintColor[] state = board.CreateStartState();
            var strokes = new[]
            {
                (new HexCoord(-1, 3), HexDirection.Clock3),
                (new HexCoord(-1, 2), HexDirection.Clock11),
                (new HexCoord(1, 4), HexDirection.Clock1),
                (new HexCoord(1, 4), HexDirection.Clock7),
                (new HexCoord(-1, 3), HexDirection.Clock1),
            };
            for (int done = 0; ; done++)
            {
                foreach (int limit in new[] { 100, 1000, 10000, HintSearch.RuntimeLimit }) AssertSearchMatchesSolve(board, state, limit);
                if (done == strokes.Length) break;
                Assert.IsTrue(board.Brush(state, board.IndexOf(strokes[done].Item1), strokes[done].Item2), $"{done + 1}번째 획");
            }
            SolveResult deadEnd = Solver.Solve(board, state, HintSearch.RuntimeLimit);
            Assert.IsFalse(deadEnd.Solved, "막다른 길");
            Assert.IsFalse(deadEnd.Limited, "끝까지 탐색함(상한에 걸린 것이 아님)");
        }

        [TestCase("Grape")]
        [TestCase("TwoColors")]
        [TestCase("BrushChanges")]
        [TestCase("Honeycomb")]
        [TestCase("Crossing")]
        [TestCase("Stain")]
        [TestCase("MakeBlack")]
        [TestCase("Hive")]
        [TestCase("LastStroke")]
        public void Search_AnyBudget_MatchesSolve_PrototypeStage(string file)
        {
            var board = new Board(Stage.Parse(TestStages.ReadPrototype(file)));
            AssertSearchMatchesSolve(board, board.CreateStartState(), Solver.DefaultLimit);
        }

        // 상한에 걸리는 경우 — 상한을 넘는 순간도 같다
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(66)]
        [TestCase(67)]
        public void Search_AnyBudget_MatchesSolve_Limited(int limit)
        {
            var board = new Board(Stage.Parse(TestStages.Grape));
            AssertSearchMatchesSolve(board, board.CreateStartState(), limit);
        }

        // 풀 수 없는 상태: 포도의 조용한 막다른 길(보라 칸에서 9시) · 거기서 더 그어 막힌 상태 · 이미 풀린 상태
        [Test]
        public void Search_AnyBudget_MatchesSolve_UnsolvableDeadAndSolved()
        {
            var board = new Board(Stage.Parse(TestStages.Grape));
            PaintColor[] state = board.CreateStartState();
            Assert.IsTrue(board.Brush(state, board.IndexOf(new HexCoord(3, 3)), HexDirection.Clock9));
            SolveResult quiet = Solver.Solve(board, state);
            Assert.IsFalse(quiet.Solved, "조용한 막다른 길");
            Assert.IsFalse(quiet.Limited, "끝까지 탐색함(상한에 걸린 것이 아님)");
            AssertSearchMatchesSolve(board, state, Solver.DefaultLimit);

            Assert.IsTrue(board.Brush(state, board.IndexOf(new HexCoord(1, 3)), HexDirection.Clock1));
            Assert.IsTrue(Solver.Solve(board, state).DeadAtStart, "막힌 상태");
            AssertSearchMatchesSolve(board, state, Solver.DefaultLimit);

            PaintColor[] solved = board.CreateStartState();
            foreach (BoardMove move in Solver.Solve(board, solved).Path) board.Brush(solved, move.Cell, move.Direction);
            AssertSearchMatchesSolve(board, solved, Solver.DefaultLimit);
        }
    }
}
