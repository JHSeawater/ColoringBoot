using System;
using System.Collections.Generic;

namespace ColoringBoot.Core.Tests
{
    // 참조 탐색 (2026-11-19) — SolverSearch가 지켜야 할 탐색 순서의 정의다: 프로토타입과 같은 너비 우선, 상태는 발견한 순서 · 획은 board.Moves 순서,
    // 검사 순서는 이미 봄 → 막힘 → 받아들임 → 풀림 → 상한. 2026-11-19에 옛 Solve(da46e1c 이전)의 결과 2,009줄과 대조해 차이 0줄.
    // 성능 작업으로는 이 파일을 고치지 않는다(SolverSearch와 같이 고치면 둘이 함께 틀린다) — 그래서 일부러 단순하고 느리다: 획마다 Clone,
    // 키는 Core의 StateKey가 아니라 상태 숫자열 문자열(키 형식이 바뀌어도 기준이 따라 바뀌지 않는다). 붓질 · 판정 규칙은 Board를 부른다
    internal static class ReferenceSolver
    {
        public static SolveResult Solve(Board board, PaintColor[] start, int limit)
        {
            if (board.IsSolved(start)) return new SolveResult(Array.Empty<BoardMove>(), 1, false, false);
            if (board.IsDead(start)) return new SolveResult(null, 1, false, true);

            IReadOnlyList<BoardMove> moves = board.Moves;
            string startKey = Key(start);
            var seen = new HashSet<string> { startKey };
            var previous = new Dictionary<string, (string from, int move)>();
            var queue = new Queue<PaintColor[]>();
            queue.Enqueue(start);
            int explored = 1;
            while (queue.Count > 0)
            {
                PaintColor[] state = queue.Dequeue();
                string key = Key(state);
                for (int m = 0; m < moves.Count; m++)
                {
                    var candidate = (PaintColor[])state.Clone();
                    if (!board.Brush(candidate, moves[m].Cell, moves[m].Direction)) continue;
                    string candidateKey = Key(candidate);
                    if (seen.Contains(candidateKey) || board.IsDead(candidate)) continue;
                    seen.Add(candidateKey);
                    previous[candidateKey] = (key, m);
                    explored++;
                    if (board.IsSolved(candidate))
                    {
                        var path = new List<BoardMove>();
                        for (string k = candidateKey; k != startKey; k = previous[k].from) path.Add(moves[previous[k].move]);
                        path.Reverse();
                        return new SolveResult(path, explored, false, false);
                    }
                    if (explored > limit) return new SolveResult(null, explored, true, false);
                    queue.Enqueue(candidate);
                }
            }
            return new SolveResult(null, explored, false, false);
        }

        private static string Key(PaintColor[] state)
        {
            var digits = new char[state.Length];
            for (int i = 0; i < state.Length; i++) digits[i] = (char)('0' + (int)state[i]);
            return new string(digits);
        }
    }
}
