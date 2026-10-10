using System;
using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    // 기믹 시제품 규칙 (GDD §7 후보 — 2026-10-10 사용자 결정): 벽은 줄을 구간으로 끊는다 · 물 칸을 지나면 붓이 빈다 · 코팅 칸은 바뀌지 않고 그 색이 붓에 묻는다
    public class GimmickTests
    {
        private const PaintColor E = PaintColor.Empty;
        private const PaintColor R = PaintColor.Red;
        private const PaintColor Y = PaintColor.Yellow;
        private const PaintColor O = PaintColor.Orange;

        // r = 0 한 줄: 빨 · ? · 노 · — q = 2 자리가 벽 · 물 칸(extra), 일반 칸의 목표는 검정(막힘과 상관없게)
        private const string SplitRow = @"[0,0,1,7],[1,0,0,7],[3,0,0,7],[4,0,2,7],[5,0,0,7]";

        private static Board Row(string cells, string extra = "") =>
            new Board(Stage.Parse($"{{\"name\":\"row\",\"cells\":[{cells}]{extra}}}"));

        // q = pick 칸을 골라 dir로 그은 뒤의 색을 q 순서로(벽 · 물 자리는 빈칸)
        private static PaintColor[] BrushAt(Board board, int pick, HexDirection dir)
        {
            PaintColor[] state = board.CreateStartState();
            board.Brush(state, board.IndexOf(new HexCoord(pick, 0)), dir);
            var byQ = new PaintColor[board.CellCount];
            for (int i = 0; i < board.CellCount; i++) byQ[board.CoordOf(i).Q] = state[i];
            return byQ;
        }

        [Test]
        public void Wall_SplitsLine_StrokeSweepsOnlyThePickedSegment()
        {
            Board board = Row(SplitRow, @",""walls"":[[2,0]]");
            CollectionAssert.AreEqual(new[] { R, R, E, E, Y, E }, BrushAt(board, 1, HexDirection.Clock3));
            CollectionAssert.AreEqual(new[] { R, E, E, E, Y, Y }, BrushAt(board, 4, HexDirection.Clock3));
            CollectionAssert.AreEqual(new[] { R, E, E, E, Y, Y }, BrushAt(board, 3, HexDirection.Clock3)); // 같은 구간이면 어느 칸이든 같다
            CollectionAssert.AreEqual(new[] { R, E, E, Y, Y, E }, BrushAt(board, 4, HexDirection.Clock9));
        }

        [Test]
        public void Wall_HasNoLine_AndIsNeverAMove()
        {
            Board board = Row(SplitRow, @",""walls"":[[2,0]]");
            int wall = board.IndexOf(new HexCoord(2, 0));
            Assert.AreEqual(CellKind.Wall, board.KindOf(wall));
            Assert.AreEqual(0, board.LineLength(wall, HexDirection.Clock3));
            Assert.AreEqual(2, board.LineLength(board.IndexOf(new HexCoord(1, 0)), HexDirection.Clock3));
            Assert.AreEqual(3, board.LineLength(board.IndexOf(new HexCoord(4, 0)), HexDirection.Clock9));
            Assert.IsFalse(board.Brush(board.CreateStartState(), wall, HexDirection.Clock3));
            foreach (BoardMove move in board.Moves) Assert.AreNotEqual(wall, move.Cell);
        }

        // 왼쪽 구간 1수 + 오른쪽 구간 양쪽 2수 = 3수. 벽이 없으면 빨강이 노랑 구간까지 묻어 막힌다
        [Test]
        public void Wall_SolverPlaysSegments()
        {
            Board board = Row(@"[0,0,1,1],[1,0,0,1],[3,0,0,2],[4,0,2,2],[5,0,0,2]", @",""walls"":[[2,0]]");
            SolveResult result = Solver.Solve(board, board.CreateStartState());
            Assert.IsTrue(result.Solved);
            Assert.AreEqual(3, result.Path.Count);
        }

        [Test]
        public void Water_EmptiesBrush_StrokeGoesOn()
        {
            Board board = Row(SplitRow, @",""water"":[[2,0]]");
            CollectionAssert.AreEqual(new[] { R, R, E, E, Y, Y }, BrushAt(board, 0, HexDirection.Clock3));
            CollectionAssert.AreEqual(new[] { R, E, E, Y, Y, E }, BrushAt(board, 5, HexDirection.Clock9));
            Assert.AreEqual(6, board.LineLength(board.IndexOf(new HexCoord(2, 0)), HexDirection.Clock3));
        }

        [Test]
        public void Water_TraceShowsEmptyBrushAfterWater()
        {
            Board board = Row(SplitRow, @",""water"":[[2,0]]");
            var cells = new int[board.CellCount];
            var brushes = new PaintColor[board.CellCount];
            int count = board.Trace(board.CreateStartState(), 0, HexDirection.Clock3, cells, brushes);
            Assert.AreEqual(6, count);
            CollectionAssert.AreEqual(new[] { R, R, E, E, Y, Y }, brushes);
            Assert.AreEqual(CellKind.Water, board.KindOf(cells[2]));
        }

        [Test]
        public void Coated_GivesItsColor_ButNeverChanges()
        {
            Board board = Row(@"[0,0,1,7],[1,0,0,7],[2,0,2,2],[3,0,0,7],[4,0,0,7]", @",""coated"":[[2,0]]");
            CollectionAssert.AreEqual(new[] { R, R, Y, O, O }, BrushAt(board, 0, HexDirection.Clock3));
            CollectionAssert.AreEqual(new[] { O, Y, Y, E, E }, BrushAt(board, 0, HexDirection.Clock9));
        }

        [Test]
        public void Parse_GimmickFields_OrderIsCellsWallsWater()
        {
            Stage stage = Stage.Parse(@"{""name"":""t"",""cells"":[[0,0,1,1],[1,0,0,1]],""water"":[[3,0]],""walls"":[[2,0]],""coated"":[[0,0]]}");
            Assert.AreEqual(4, stage.Cells.Count);
            Assert.AreEqual(CellKind.Coated, stage.Cells[0].Kind);
            Assert.AreEqual(CellKind.Paint, stage.Cells[1].Kind);
            Assert.AreEqual(CellKind.Wall, stage.Cells[2].Kind);
            Assert.AreEqual(new HexCoord(2, 0), stage.Cells[2].Coord);
            Assert.AreEqual(CellKind.Water, stage.Cells[3].Kind);
            Assert.AreEqual(E, stage.Cells[3].Start);
        }

        [TestCase(@"{""name"":""t"",""cells"":[[0,0,1,1]],""coated"":[[5,5]]}")]          // 코팅 칸이 cells에 없음
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,1,3]],""coated"":[[0,0]]}")]          // 코팅 칸 시작 ≠ 목표
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,0,0]],""coated"":[[0,0]]}")]          // 코팅 칸이 빈칸
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,1,1]],""coated"":[[0,0],[0,0]]}")]    // 코팅 좌표 중복
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,1,1]],""walls"":[[0,0]]}")]           // 벽이 칸 자리
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,1,1]],""walls"":[[1,0]],""water"":[[1,0]]}")] // 벽 · 물 같은 자리
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,1,1]],""walls"":[[1,0,0]]}")]         // 좌표가 [q, r]이 아님
        public void Parse_InvalidGimmick_ThrowsFormatException(string json)
        {
            Assert.Throws<FormatException>(() => Stage.Parse(json));
        }

        [Test]
        public void Writer_RoundTrip_WithGimmicks()
        {
            const string json = @"{""name"":""t"",""cells"":[[0,0,1,1],[1,0,0,1]],""walls"":[[2,0]],""water"":[[3,0]],""coated"":[[0,0]],""minMoves"":2}";
            Assert.AreEqual(json, StageWriter.ToJson(Stage.Parse(json)));
            const string plain = @"{""name"":""t"",""cells"":[[0,0,1,1],[1,0,0,1]],""minMoves"":1}";
            Assert.AreEqual(plain, StageWriter.ToJson(Stage.Parse(plain)));
        }

        // 칸 지표는 일반 칸만: 벽 · 물 · 코팅 칸은 빼고, 코팅 칸 색(노랑)은 공급 색으로 센다 → 빨강 목표 3칸 모두 막힐 수 있음
        [Test]
        public void Metrics_CountOnlyPaintCells()
        {
            var board = new Board(Stage.Parse(@"{""name"":""t"",""cells"":[[0,0,1,1],[1,0,0,1],[2,0,2,2],[4,0,0,1]],""walls"":[[3,0]],""water"":[[5,0]],""coated"":[[2,0]]}"));
            StageMetrics metrics = StageMetrics.Measure(board);
            Assert.AreEqual(3, metrics.Cells);
            Assert.AreEqual(3, metrics.TrapCells);
            Assert.AreEqual(3, metrics.PaintedCells);
            Assert.AreEqual(0, metrics.MixedCells);
        }
    }
}
