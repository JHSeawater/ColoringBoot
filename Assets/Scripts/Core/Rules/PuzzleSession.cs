using System;
using System.Collections.Generic;

namespace ColoringBoot.Core
{
    // 한 판의 진행 — 현재 색 · 획 단위 되돌리기 · 처음부터 (GDD §2.5, CLAUDE.md §3 상태 · 되돌리기)
    public sealed class PuzzleSession
    {
        // 획마다 긋기 전 상태를 쌓는다. 보드가 작아서(최대 40칸 정도) 통째로 보관한다
        private readonly Stack<PaintColor[]> _history = new Stack<PaintColor[]>();
        private PaintColor[] _state;

        public PuzzleSession(Board board)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            _state = board.CreateStartState();
        }

        public Board Board { get; }
        // 둔 수 — 되돌리면 줄고, 처음부터 하면 0
        public int MoveCount => _history.Count;
        public bool IsSolved => Board.IsSolved(_state);
        public bool IsDead => Board.IsDead(_state);

        public PaintColor ColorAt(int cell) => _state[cell];
        public bool IsDeadCell(int cell) => Board.IsDeadCell(_state, cell);

        // 지금 상태에서 붓 경로 (미리보기용, 상태 불변 — Board.Trace)
        public int Trace(int cell, HexDirection dir, int[] cells, PaintColor[] brushes) => Board.Trace(_state, cell, dir, cells, brushes);

        // 붓질. 색이 하나도 바뀌지 않는 획은 무시한다 — 수로 세지 않고 기록도 남기지 않는다 (2026-09-28 사용자 결정)
        public bool Brush(int cell, HexDirection dir)
        {
            var next = (PaintColor[])_state.Clone();
            if (!Board.Brush(next, cell, dir)) return false;
            _history.Push(_state);
            _state = next;
            return true;
        }

        // 한 획 되돌리기. 되돌릴 획이 없으면 false
        public bool Undo()
        {
            if (_history.Count == 0) return false;
            _state = _history.Pop();
            return true;
        }

        // 힌트 (GDD §6, 2026-10-07): 지금 상태에서 최소 풀이의 다음 한 수. 지금 상태로는 풀 수 없으면(막힘 · 막힘 표시 없는 막다른 길)
        // 가장 최근 획부터 거슬러 몇 수 되돌려야 다시 풀 수 있는지. limit = 상태 하나당 탐색 상한 — 넘으면 Unknown (런타임은 작게, CLAUDE.md §4)
        public Hint FindHint(int limit)
        {
            SolveResult now = Solver.Solve(Board, _state, limit);
            if (now.Solved) return now.Path.Count == 0 ? Hint.Solved : Hint.Next(now.Path[0], now.Path.Count);
            if (now.Limited) return Hint.Unknown;

            int undos = 0;
            foreach (PaintColor[] before in _history) // Stack은 가장 최근 것부터 나온다
            {
                undos++;
                SolveResult back = Solver.Solve(Board, before, limit);
                if (back.Solved) return Hint.Undo(undos);
                if (back.Limited) return Hint.Unknown;
            }
            return Hint.Unknown; // 시작 상태부터 풀 수 없다(스테이지 오류)
        }

        // 처음부터: 시작 상태로 돌아가고 기록을 지운다 — 되돌릴 수 없다 (프로토타입과 같음)
        public void Restart()
        {
            _state = Board.CreateStartState();
            _history.Clear();
        }
    }
}
