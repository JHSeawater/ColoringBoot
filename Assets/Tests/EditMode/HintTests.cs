using System;
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

        // 나눠 도는 힌트 탐색(HintSearch, 2026-11-17) — 상태를 budget개씩 펼쳐도 한 번에 도는 FindHint와 같은 힌트다.
        // Step 호출 상한(2026-11-25 — 지적 M10): 지금 상태 + 되돌린 상태마다 탐색 하나씩 → SolverTests.StepCap × (수 + 1). 넘으면 끝나지 않는 회귀
        private static Hint FindInSteps(PuzzleSession session, int limit, int budget)
        {
            HintSearch search = session.StartHint(limit);
            long cap = SolverTests.StepCap(limit, budget) * (session.MoveCount + 1);
            for (long steps = 1; !search.Step(budget); steps++)
            {
                if (steps >= cap) Assert.Fail($"예산 {budget}: 힌트 Step을 {steps}번 불러도 끝나지 않음(상한 {cap})");
            }
            return search.Result;
        }

        private static void AssertStepsMatchFindHint(PuzzleSession session, int limit, HintKind kind, int count)
        {
            Hint expected = session.FindHint(limit);
            Assert.AreEqual(kind, expected.Kind);
            Assert.AreEqual(count, expected.Count);
            foreach (int budget in new[] { 1, 997 })
            {
                Hint actual = FindInSteps(session, limit, budget);
                Assert.AreEqual(expected.Kind, actual.Kind, $"예산 {budget}: 종류");
                Assert.AreEqual(expected.Count, actual.Count, $"예산 {budget}: 수");
                Assert.AreEqual(expected.Move.Cell, actual.Move.Cell, $"예산 {budget}: 획의 칸");
                Assert.AreEqual(expected.Move.Direction, actual.Move.Direction, $"예산 {budget}: 획의 방향");
            }
        }

        [Test]
        public void Steps_MatchFindHint_NextMove()
        {
            AssertStepsMatchFindHint(Grape(), Solver.DefaultLimit, HintKind.Next, 5);
        }

        [Test]
        public void Steps_MatchFindHint_UndoOne()
        {
            PuzzleSession session = Grape();
            Brush(session, 3, 3, HexDirection.Clock9);
            AssertStepsMatchFindHint(session, Solver.DefaultLimit, HintKind.Undo, 1);
        }

        [Test]
        public void Steps_MatchFindHint_UndoTwo()
        {
            PuzzleSession session = Grape();
            Brush(session, 3, 3, HexDirection.Clock9);
            Brush(session, 1, 3, HexDirection.Clock1);
            AssertStepsMatchFindHint(session, Solver.DefaultLimit, HintKind.Undo, 2);
        }

        [Test]
        public void Steps_MatchFindHint_TinyLimit()
        {
            AssertStepsMatchFindHint(Grape(), 1, HintKind.Unknown, 0);
        }

        [Test]
        public void Result_BeforeDone_Throws()
        {
            HintSearch search = Grape().StartHint(Solver.DefaultLimit);
            Assert.IsFalse(search.Step(1));
            Assert.Throws<InvalidOperationException>(() => { Hint unused = search.Result; });
        }

        // 챕터 4 호박의 막다른 길(2026-11-17 리뷰 때 에디터 · PC 브라우저에서 "5수 되돌려 보세요") — 게임 화면과 같은 상한(HintSearch.RuntimeLimit)으로.
        // 판은 고정 사본(TestStages.Pumpkin) — 게임 파일이 바뀌어도 이 테스트는 그대로다
        [Test]
        public void PumpkinDeadEnd_SaysUndoFive()
        {
            var session = new PuzzleSession(new Board(Stage.Parse(TestStages.Pumpkin)));
            Brush(session, -1, 3, HexDirection.Clock3);
            Brush(session, -1, 2, HexDirection.Clock11);
            Brush(session, 1, 4, HexDirection.Clock1);
            Brush(session, 1, 4, HexDirection.Clock7);
            Brush(session, -1, 3, HexDirection.Clock1);
            Assert.IsFalse(session.IsDead, "막힘 표시는 없다");

            Hint whole = session.FindHint(HintSearch.RuntimeLimit);
            Assert.AreEqual(HintKind.Undo, whole.Kind);
            Assert.AreEqual(5, whole.Count);
            Hint stepped = FindInSteps(session, HintSearch.RuntimeLimit, 997);
            Assert.AreEqual(HintKind.Undo, stepped.Kind);
            Assert.AreEqual(5, stepped.Count);
        }
    }
}
