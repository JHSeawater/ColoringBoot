using System.Collections.Generic;
using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    // 회귀 기준 — 포도 스테이지 (GDD §3, CLAUDE.md §3): 최소 풀이 5수, 그 5수의 순서 120가지 중 8가지만 성공
    public class GrapeRegressionTests
    {
        // GDD §3 풀이를 (고른 칸, 방향) 그대로 옮긴 것
        private static readonly (HexCoord cell, HexDirection dir)[] _gddSolution =
        {
            (new HexCoord(3, 1), HexDirection.Clock1),  // 1. 초록 칸에서 1시 — 꼭대기 칸만 초록
            (new HexCoord(3, 3), HexDirection.Clock7),  // 2. 보라 칸에서 7시 — 아래쪽 두 칸이 보라
            (new HexCoord(2, 4), HexDirection.Clock11), // 3. 2에서 보라가 된 두 칸 중 위쪽 칸에서 11시 — 가운데 칸과 그 위 칸
            (new HexCoord(2, 3), HexDirection.Clock1),  // 4. 가운데 칸에서 1시 — 오른쪽 위 칸
            (new HexCoord(1, 5), HexDirection.Clock11), // 5. 맨 아래 칸에서 11시 — 남은 두 칸
        };

        [Test]
        public void GddSolution_ClearsInFiveMoves()
        {
            var session = new PuzzleSession(new Board(Stage.Parse(TestStages.Grape)));

            foreach ((HexCoord cell, HexDirection dir) in _gddSolution)
            {
                Assert.IsTrue(session.Brush(session.Board.IndexOf(cell), dir), $"{cell} {dir}: 색이 바뀌지 않음");
                Assert.IsFalse(session.IsDead, $"{cell} {dir}: 막힘");
            }

            Assert.IsTrue(session.IsSolved);
            Assert.AreEqual(5, session.MoveCount);
        }

        // 미리보기 경로: GDD §3 1수(초록 칸 1시) — 앞의 두 빈칸에서 붓은 비어 있고, 초록 칸부터 초록. 상태는 그대로
        [Test]
        public void Trace_FirstMove_ShowsEmptyBrushThenGreen()
        {
            var board = new Board(Stage.Parse(TestStages.Grape));
            PaintColor[] state = board.CreateStartState();
            var cells = new int[board.CellCount];
            var brushes = new PaintColor[board.CellCount];

            int count = board.Trace(state, board.IndexOf(new HexCoord(3, 1)), HexDirection.Clock1, cells, brushes);

            Assert.AreEqual(4, count);
            HexCoord[] order = { new HexCoord(1, 3), new HexCoord(2, 2), new HexCoord(3, 1), new HexCoord(4, 0) };
            PaintColor[] expected = { PaintColor.Empty, PaintColor.Empty, PaintColor.Green, PaintColor.Green };
            for (int k = 0; k < count; k++)
            {
                Assert.AreEqual(order[k], board.CoordOf(cells[k]), $"{k}번째 칸");
                Assert.AreEqual(expected[k], brushes[k], $"{k}번째 붓 색");
            }
            CollectionAssert.AreEqual(board.CreateStartState(), state);
        }

        // 경로의 마지막 붓 색으로 칠한 결과가 실제 붓질 결과와 같다 — 모든 칸 · 모든 방향
        [Test]
        public void Trace_MatchesBrushForEveryCellAndDirection()
        {
            var board = new Board(Stage.Parse(TestStages.Grape));
            var cells = new int[board.CellCount];
            var brushes = new PaintColor[board.CellCount];
            for (int cell = 0; cell < board.CellCount; cell++)
            {
                for (int d = 0; d < 6; d++)
                {
                    PaintColor[] state = board.CreateStartState();
                    int count = board.Trace(state, cell, (HexDirection)d, cells, brushes);
                    PaintColor[] predicted = board.CreateStartState();
                    for (int k = 0; k < count; k++)
                    {
                        if (brushes[k] != PaintColor.Empty) predicted[cells[k]] = brushes[k];
                    }
                    board.Brush(state, cell, (HexDirection)d);
                    CollectionAssert.AreEqual(state, predicted, $"칸 {cell}, {(HexDirection)d}");
                }
            }
        }

        [Test]
        public void GddSolution_Only8Of120OrdersSucceed()
        {
            var board = new Board(Stage.Parse(TestStages.Grape));
            int total = 0;
            int solved = 0;

            foreach (int[] order in Permutations(_gddSolution.Length))
            {
                PaintColor[] state = board.CreateStartState();
                foreach (int k in order) board.Brush(state, board.IndexOf(_gddSolution[k].cell), _gddSolution[k].dir);
                total++;
                if (board.IsSolved(state)) solved++;
            }

            Assert.AreEqual(120, total);
            Assert.AreEqual(8, solved);
        }

        // 0 ~ n-1의 모든 순열 — n-1을 (n-1)개짜리 순열의 모든 자리에 끼워 넣는다
        private static IEnumerable<int[]> Permutations(int n)
        {
            if (n == 0)
            {
                yield return new int[0];
                yield break;
            }

            foreach (int[] rest in Permutations(n - 1))
            {
                for (int pos = 0; pos <= rest.Length; pos++)
                {
                    var order = new int[n];
                    for (int k = 0, j = 0; k < n; k++) order[k] = k == pos ? n - 1 : rest[j++];
                    yield return order;
                }
            }
        }
    }
}
