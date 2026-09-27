using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    public class PuzzleSessionTests
    {
        // 색 배열을 짧게 쓰기 위한 별칭
        private const PaintColor E = PaintColor.Empty;
        private const PaintColor R = PaintColor.Red;
        private const PaintColor B = PaintColor.Blue;
        private const PaintColor P = PaintColor.Purple;

        // r = 0 한 줄 [빨강, 빈칸, 파랑] — 목표는 기본 모두 보라
        private static PuzzleSession MakeSession(PaintColor target = P)
        {
            return new PuzzleSession(new Board(new Stage("session", new[]
            {
                new StageCell(new HexCoord(0, 0), R, P),
                new StageCell(new HexCoord(1, 0), E, P),
                new StageCell(new HexCoord(2, 0), B, target),
            })));
        }

        private static PaintColor[] Colors(PuzzleSession session)
        {
            var colors = new PaintColor[session.Board.CellCount];
            for (int i = 0; i < colors.Length; i++) colors[i] = session.ColorAt(i);
            return colors;
        }

        [Test]
        public void Brush_CountsMove_UndoRestores()
        {
            PuzzleSession session = MakeSession();

            Assert.IsTrue(session.Brush(0, HexDirection.Clock3));
            Assert.AreEqual(1, session.MoveCount);
            CollectionAssert.AreEqual(new[] { R, R, P }, Colors(session));

            Assert.IsTrue(session.Undo());
            Assert.AreEqual(0, session.MoveCount);
            CollectionAssert.AreEqual(new[] { R, E, B }, Colors(session));
        }

        // 되돌리기는 한 획씩 거슬러 가고, 더 되돌릴 획이 없으면 false
        [Test]
        public void Undo_StepsBackOneStrokeAtATime()
        {
            PuzzleSession session = MakeSession();
            session.Brush(0, HexDirection.Clock3); // [R, R, P]
            session.Brush(0, HexDirection.Clock9); // [P, P, P]
            Assert.IsTrue(session.IsSolved);

            Assert.IsTrue(session.Undo());
            CollectionAssert.AreEqual(new[] { R, R, P }, Colors(session));
            Assert.IsFalse(session.IsSolved);

            Assert.IsTrue(session.Undo());
            CollectionAssert.AreEqual(new[] { R, E, B }, Colors(session));
            Assert.IsFalse(session.Undo());
        }

        // 색이 하나도 바뀌지 않는 획은 무시한다 — 수로 세지 않고 되돌리기 기록에도 없다 (2026-09-28 결정)
        [Test]
        public void NoChangeStroke_IsIgnored()
        {
            PuzzleSession session = MakeSession();
            session.Brush(0, HexDirection.Clock3); // [R, R, P]

            Assert.IsFalse(session.Brush(0, HexDirection.Clock3)); // 같은 획을 다시 그어도 그대로
            Assert.IsFalse(session.Brush(0, HexDirection.Clock1)); // 1칸짜리 줄
            Assert.AreEqual(1, session.MoveCount);

            session.Undo();
            CollectionAssert.AreEqual(new[] { R, E, B }, Colors(session));
        }

        [Test]
        public void Restart_RestoresStartAndClearsHistory()
        {
            PuzzleSession session = MakeSession();
            session.Brush(0, HexDirection.Clock3);
            session.Brush(0, HexDirection.Clock9);

            session.Restart();

            CollectionAssert.AreEqual(new[] { R, E, B }, Colors(session));
            Assert.AreEqual(0, session.MoveCount);
            Assert.IsFalse(session.Undo());
        }

        // 막히면 막힌 칸을 알 수 있고, 되돌리면 풀린다 (GDD §2.5)
        [Test]
        public void DeadStroke_ShowsDeadCell_UndoRecovers()
        {
            PuzzleSession session = MakeSession(target: B); // 파랑 칸의 목표는 파랑 그대로

            session.Brush(0, HexDirection.Clock3); // 파랑 칸에 빨강이 섞여 보라

            Assert.IsTrue(session.IsDead);
            Assert.IsTrue(session.IsDeadCell(2));
            Assert.IsFalse(session.IsDeadCell(1));

            session.Undo();
            Assert.IsFalse(session.IsDead);
        }
    }
}
