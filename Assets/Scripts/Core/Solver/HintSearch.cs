using System;

namespace ColoringBoot.Core
{
    // 힌트 절차를 이어서 돌린다 (2026-11-17, PuzzleSession.StartHint): 지금 상태를 풀어 보고, 풀 수 없으면
    // 가장 최근 획부터 거슬러 올라가며 풀릴 때까지 다시 탐색한다(상한은 상태 하나당 — 넘으면 Unknown).
    // 시작할 때 받은 상태 배열을 그대로 쥔다 — 세션은 획마다 새 배열을 만들므로 탐색 중에 내용이 바뀌지 않는다
    public sealed class HintSearch
    {
        // 게임 화면이 쓰는 상한(상태 하나당) — 값의 근거는 PuzzleController.HintLimit 주석. 테스트도 이 값으로 돌린다
        public const int RuntimeLimit = 200000;

        private readonly Board _board;
        private readonly PaintColor[][] _history;   // 긋기 전 상태들 — 가장 최근 것부터
        private readonly int _limit;
        private SolverSearch _search;
        private int _undos;     // 지금 탐색하는 상태가 몇 수 되돌린 것인가(0 = 지금 상태)
        private Hint? _result;

        public HintSearch(Board board, PaintColor[] state, PaintColor[][] history, int limit)
        {
            _board = board;
            _history = history;
            _limit = limit;
            _search = new SolverSearch(board, state, limit);
        }

        // 힌트 — 끝난 뒤에만 읽는다
        public Hint Result => _result ?? throw new InvalidOperationException("힌트 탐색이 끝나지 않았습니다");

        // 상태를 budget개까지 펼친다(SolverSearch.Step). 한 상태의 탐색이 끝나면 다음 상태는 다음 호출부터 탐색한다. 힌트가 정해졌으면 true
        public bool Step(int budget)
        {
            if (_result.HasValue) return true;
            if (!_search.Step(budget)) return false;

            SolveResult solve = _search.Result;
            if (solve.Solved)
            {
                if (_undos > 0) _result = Hint.Undo(_undos);
                else _result = solve.Path.Count == 0 ? Hint.Solved : Hint.Next(solve.Path[0], solve.Path.Count);
            }
            else if (solve.Limited || _undos == _history.Length)
            {
                _result = Hint.Unknown;   // 상한에 걸렸거나, 시작 상태부터 풀 수 없다(스테이지 오류)
            }
            else
            {
                _search = new SolverSearch(_board, _history[_undos], _limit);
                _undos++;
            }
            return _result.HasValue;
        }
    }
}
