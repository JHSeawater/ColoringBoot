using System;
using System.Collections.Generic;

namespace ColoringBoot.Core
{
    // 이어서 돌릴 수 있는 너비 우선 탐색 (2026-11-17) — Solver.Solve와 같은 순서로 탐색해 같은 결과를 낸다.
    // 런타임(힌트)은 Step을 프레임마다 조금씩 불러 화면이 멈추지 않게 한다(CLAUDE.md §6). start 배열은 바꾸지 않는다
    public sealed class SolverSearch
    {
        private readonly Board _board;
        private readonly int _limit;
        private readonly IReadOnlyList<BoardMove> _moves;
        private readonly Solver.StateKey _startKey;
        // 발견한 상태 → (부모, 획). 방문 여부도 이것 하나로 본다 — 시작 상태는 자기 자신을 부모로 넣어 둔다(PathTo는 시작 키에서 멈춰 읽지 않는다)
        private readonly Dictionary<Solver.StateKey, (Solver.StateKey from, int move)> _previous;
        private List<PaintColor[]> _frontier;   // 지금 펼치는 층
        private List<PaintColor[]> _next;       // 다음 층
        private PaintColor[] _scratch;          // Expand의 작업 배열 — 후보를 받아들이면 그 배열은 다음 층으로 가고 새로 만든다
        private int _index;                     // _frontier에서 다음에 펼칠 칸
        private int _explored = 1;

        public SolverSearch(Board board, PaintColor[] start, int limit = Solver.DefaultLimit)
        {
            if (board.CellCount > Solver.MaxCells) throw new ArgumentException($"칸이 {Solver.MaxCells}개를 넘습니다: {board.CellCount}", nameof(board));
            if (board.IsSolved(start))
            {
                Result = new SolveResult(Array.Empty<BoardMove>(), 1, false, false);
                return;
            }
            if (board.IsDead(start))
            {
                Result = new SolveResult(null, 1, false, true);
                return;
            }

            _board = board;
            _limit = limit;
            _moves = board.Moves;
            _startKey = Solver.StateKey.Of(start);
            _previous = new Dictionary<Solver.StateKey, (Solver.StateKey from, int move)> { [_startKey] = (_startKey, -1) };
            _frontier = new List<PaintColor[]> { start };
            _next = new List<PaintColor[]>();
            _scratch = new PaintColor[start.Length];
        }

        // 탐색 결과 — 끝나기 전에는 null
        public SolveResult Result { get; private set; }

        // 상태를 budget개까지 펼친다(상태 하나의 모든 획을 다 본 뒤에만 멈춘다 — 나눠 돌려도 순서가 같다). 탐색이 끝났으면 true
        public bool Step(int budget)
        {
            if (budget < 1) throw new ArgumentOutOfRangeException(nameof(budget));
            for (; Result == null && budget > 0; budget--)
            {
                if (_index == _frontier.Count)
                {
                    // 한 층을 다 펼쳤다 — 다음 층이 비었으면 풀이가 없다
                    _frontier = _next;
                    _next = new List<PaintColor[]>();
                    _index = 0;
                    if (_frontier.Count == 0)
                    {
                        Result = new SolveResult(null, _explored, false, false);
                        break;
                    }
                }
                Expand(_frontier[_index++]);
            }
            return Result != null;
        }

        // 획마다 배열을 새로 만들지 않고 작업 배열 하나에 칠한다(2026-11-19) — state(첫 층은 호출 쪽의 start)에는 칠하지 않는다.
        // 전제: Board.Brush가 false면 배열은 그대로다(Board.Sweep은 색이 바뀔 때만 칸에 쓴다) → 그 획 뒤에는 되돌릴 것이 없다
        private void Expand(PaintColor[] state)
        {
            Solver.StateKey key = Solver.StateKey.Of(state);
            PaintColor[] candidate = _scratch;
            Array.Copy(state, candidate, state.Length);
            for (int m = 0; m < _moves.Count; m++)
            {
                if (!_board.Brush(candidate, _moves[m].Cell, _moves[m].Direction)) continue;
                Solver.StateKey candidateKey = Solver.StateKey.Of(candidate);
                if (_previous.ContainsKey(candidateKey) || _board.IsDead(candidate))
                {
                    Array.Copy(state, candidate, state.Length);   // 버리는 후보 — 다음 획을 위해 되돌린다
                    continue;
                }
                _previous[candidateKey] = (key, m);
                _explored++;
                if (_board.IsSolved(candidate))
                {
                    Result = new SolveResult(Solver.PathTo(candidateKey, _startKey, _previous, _moves), _explored, false, false);
                    return;
                }
                if (_explored > _limit)
                {
                    Result = new SolveResult(null, _explored, true, false);
                    return;
                }
                _next.Add(candidate);   // 받아들인 상태만 새 배열이 된다
                candidate = _scratch = (PaintColor[])state.Clone();
            }
        }
    }
}
