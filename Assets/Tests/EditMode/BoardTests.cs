using System.Collections.Generic;
using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    public class BoardTests
    {
        // 색 배열을 짧게 쓰기 위한 별칭
        private const PaintColor E = PaintColor.Empty;
        private const PaintColor R = PaintColor.Red;
        private const PaintColor Y = PaintColor.Yellow;
        private const PaintColor B = PaintColor.Blue;
        private const PaintColor P = PaintColor.Purple;
        private const PaintColor G = PaintColor.Green;
        private const PaintColor K = PaintColor.Black;

        // r = 0 한 줄에 q = 0부터 놓은 칸들. 목표는 모두 검정이라 막힘과 상관없다
        private static Board MakeRow(params PaintColor[] start)
        {
            var cells = new List<StageCell>();
            for (int q = 0; q < start.Length; q++) cells.Add(new StageCell(new HexCoord(q, 0), start[q], K));
            return new Board(new Stage("row", cells));
        }

        // 줄의 pick번째 칸을 골라 dir로 그은 뒤의 색
        private static PaintColor[] BrushRow(PaintColor[] start, HexDirection dir, int pick = 0)
        {
            Board board = MakeRow(start);
            PaintColor[] state = board.CreateStartState();
            board.Brush(state, pick, dir);
            return state;
        }

        private static bool Changes(HexDirection dir, params PaintColor[] start)
        {
            Board board = MakeRow(start);
            return board.Brush(board.CreateStartState(), 0, dir);
        }

        // 빈 붓은 빈칸을 칠하지 않고, 처음 만난 색을 묻힌 뒤로는 지나는 칸을 모두 칠한다
        [Test]
        public void EmptyBrush_PaintsOnlyAfterFirstColor()
        {
            CollectionAssert.AreEqual(new[] { E, E, R, R }, BrushRow(new[] { E, E, R, E }, HexDirection.Clock3));
        }

        // 붓은 고른 방향의 반대편 끝에서 출발한다: 3시는 왼쪽 끝부터, 9시는 오른쪽 끝부터
        [Test]
        public void Brush_StartsFromOppositeEnd()
        {
            CollectionAssert.AreEqual(new[] { E, R, R, R }, BrushRow(new[] { E, R, E, E }, HexDirection.Clock3));
            CollectionAssert.AreEqual(new[] { R, R, E, E }, BrushRow(new[] { E, R, E, E }, HexDirection.Clock9));
        }

        // 다른 색 칸을 지나면 칸과 붓이 함께 섞인 색이 된다
        [Test]
        public void Brush_MixesCellAndBrush()
        {
            CollectionAssert.AreEqual(new[] { R, R, P, P }, BrushRow(new[] { R, E, B, E }, HexDirection.Clock3));
        }

        // 색이 하나도 바뀌지 않으면 false — 같은 색만 지날 때 · 빈 줄 · 1칸짜리 줄
        [Test]
        public void Brush_WithoutColorChange_ReturnsFalse()
        {
            Assert.IsFalse(Changes(HexDirection.Clock3, R, R));
            Assert.IsFalse(Changes(HexDirection.Clock3, E, E));
            Assert.IsFalse(Changes(HexDirection.Clock3, R));
            Assert.IsTrue(Changes(HexDirection.Clock3, R, E));
        }

        // 같은 줄 · 같은 방향이면 어느 칸을 골라도 결과가 같다
        [Test]
        public void Brush_AnyCellOfLineGivesSameResult()
        {
            PaintColor[] start = { Y, E, B, E, R };
            CollectionAssert.AreEqual(new[] { Y, Y, G, G, K }, BrushRow(start, HexDirection.Clock3));

            foreach (HexDirection dir in new[] { HexDirection.Clock3, HexDirection.Clock9 })
            {
                PaintColor[] expected = BrushRow(start, dir);
                for (int pick = 1; pick < start.Length; pick++)
                    CollectionAssert.AreEqual(expected, BrushRow(start, dir, pick), $"{dir}, {pick}번째 칸");
            }
        }

        // 줄 중간의 빈자리는 건너간다 — 같은 직선 위면 한 줄 (CLAUDE.md §3)
        [Test]
        public void Brush_CrossesGapsInLine()
        {
            var board = new Board(new Stage("gap", new[]
            {
                new StageCell(new HexCoord(0, 0), R, R),
                new StageCell(new HexCoord(2, 0), E, R),
                new StageCell(new HexCoord(5, 0), E, R),
            }));
            PaintColor[] state = board.CreateStartState();

            Assert.IsTrue(board.Brush(state, 0, HexDirection.Clock3));
            CollectionAssert.AreEqual(new[] { R, R, R }, state);
        }

        // 6방향 모두 방향 표대로 긋는다: 원점(빨강)을 지나는 3칸 줄에서 dir 쪽 칸만 칠해지고, 반대쪽 칸과 줄 밖의 칸은 그대로다
        [TestCase(HexDirection.Clock1)]
        [TestCase(HexDirection.Clock3)]
        [TestCase(HexDirection.Clock5)]
        [TestCase(HexDirection.Clock7)]
        [TestCase(HexDirection.Clock9)]
        [TestCase(HexDirection.Clock11)]
        public void Brush_FollowsDirectionTable(HexDirection dir)
        {
            HexCoord d = dir.Delta();
            HexCoord side = ((HexDirection)((dir.Axis() + 1) % HexDirectionExtensions.AxisCount)).Delta(); // 다른 축 쪽으로 한 칸
            var board = new Board(new Stage("dir", new[]
            {
                new StageCell(new HexCoord(-d.Q, -d.R), E, R), // 반대쪽
                new StageCell(new HexCoord(0, 0), R, R),        // 원점
                new StageCell(d, E, R),                          // dir 쪽
                new StageCell(side, E, R),                       // 줄 밖
            }));
            PaintColor[] state = board.CreateStartState();

            board.Brush(state, 1, dir);

            CollectionAssert.AreEqual(new[] { E, R, R, E }, state);
        }

        // 막힌 칸은 목표 색에 없는 기본색이 들어간 칸뿐이다. 아직 칠하지 않은 칸은 목표와 달라도 막힘이 아니다 (CLAUDE.md §8)
        [TestCase(PaintColor.Empty, PaintColor.Purple, false)]
        [TestCase(PaintColor.Red, PaintColor.Purple, false)]
        [TestCase(PaintColor.Purple, PaintColor.Purple, false)]
        [TestCase(PaintColor.Yellow, PaintColor.Purple, true)]
        [TestCase(PaintColor.Black, PaintColor.Purple, true)]
        [TestCase(PaintColor.Red, PaintColor.Empty, true)]
        [TestCase(PaintColor.Black, PaintColor.Black, false)]
        public void IsDeadCell_OnlyWhenColorHasPrimaryMissingFromTarget(PaintColor color, PaintColor target, bool dead)
        {
            var board = new Board(new Stage("cell", new[] { new StageCell(new HexCoord(0, 0), color, target) }));
            PaintColor[] state = board.CreateStartState();

            Assert.AreEqual(dead, board.IsDeadCell(state, 0));
            Assert.AreEqual(dead, board.IsDead(state));
        }

        // 성공은 모든 칸이 목표 색과 같을 때만
        [Test]
        public void IsSolved_OnlyWhenEveryCellMatchesTarget()
        {
            var board = new Board(new Stage("solve", new[]
            {
                new StageCell(new HexCoord(0, 0), R, R),
                new StageCell(new HexCoord(1, 0), E, R),
            }));
            PaintColor[] state = board.CreateStartState();
            Assert.IsFalse(board.IsSolved(state));
            Assert.IsFalse(board.IsDead(state));

            board.Brush(state, 0, HexDirection.Clock3);

            Assert.IsTrue(board.IsSolved(state));
        }

        [Test]
        public void IndexOf_ReturnsMinusOneForMissingCoord()
        {
            Board board = MakeRow(E, R);

            Assert.AreEqual(1, board.IndexOf(new HexCoord(1, 0)));
            Assert.AreEqual(-1, board.IndexOf(new HexCoord(2, 0)));
        }
    }
}
