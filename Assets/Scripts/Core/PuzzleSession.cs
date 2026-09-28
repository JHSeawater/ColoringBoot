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

        // 처음부터: 시작 상태로 돌아가고 기록을 지운다 — 되돌릴 수 없다 (프로토타입과 같음)
        public void Restart()
        {
            _state = Board.CreateStartState();
            _history.Clear();
        }
    }
}
