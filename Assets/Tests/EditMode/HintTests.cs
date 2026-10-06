using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    // 힌트 (GDD §6, 2026-10-07) — PuzzleSession.FindHint
    public class HintTests
    {
        private static PuzzleSession Grape() => new PuzzleSession(new Board(Stage.Parse(TestStages.Grape)));

        private static void Brush(PuzzleSession session, int q, int r, HexDirection dir) =>
            Assert.IsTrue(session.Brush(session.Board.IndexOf(new HexCoord(q, r)), dir), $"({q},{r}) {dir} 붓질");

        [Test]
        public void FollowingHints_SolvesInMinimumMoves()
        {
            PuzzleSession session = Grape();
            for (int remaining = 5; remaining > 0; remaining--)
            {
                Hint hint = session.FindHint(Solver.DefaultLimit);
                Assert.AreEqual(HintKind.Next, hint.Kind);
                Assert.AreEqual(remaining, hint.Count, "남은 최소 수");
                Assert.IsTrue(session.Brush(hint.Move.Cell, hint.Move.Direction));
            }
            Assert.IsTrue(session.IsSolved);
            Assert.AreEqual(HintKind.Solved, session.FindHint(Solver.DefaultLimit).Kind);
        }

        [Test]
        public void SilentDeadEnd_SaysUndoOne()
        {
            // 보라 칸에서 9시 — 막힘 표시는 없지만 초록 줄 앞이 보라가 되어 더는 풀 수 없다
            PuzzleSession session = Grape();
            Brush(session, 3, 3, HexDirection.Clock9);
            Assert.IsFalse(session.IsDead);

            Hint hint = session.FindHint(Solver.DefaultLimit);
            Assert.AreEqual(HintKind.Undo, hint.Kind);
            Assert.AreEqual(1, hint.Count);
        }

        [Test]
        public void DeadAfterDeadEnd_SaysUndoTwo()
        {
            // 위 막다른 길에서 초록 줄을 1시로 그으면 검정이 생겨 막힌다 — 두 수 되돌려야 다시 풀 수 있다
            PuzzleSession session = Grape();
            Brush(session, 3, 3, HexDirection.Clock9);
            Brush(session, 1, 3, HexDirection.Clock1);
            Assert.IsTrue(session.IsDead);

            Hint hint = session.FindHint(Solver.DefaultLimit);
            Assert.AreEqual(HintKind.Undo, hint.Kind);
            Assert.AreEqual(2, hint.Count);
        }

        [Test]
        public void TinyLimit_IsUnknown()
        {
            Assert.AreEqual(HintKind.Unknown, Grape().FindHint(1).Kind);
        }
    }
}
