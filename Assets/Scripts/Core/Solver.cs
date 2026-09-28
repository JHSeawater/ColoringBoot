using System;
using System.Collections.Generic;

namespace ColoringBoot.Core
{
    // 솔버 결과. Path는 풀었을 때만(이미 목표면 빈 목록), Explored는 발견한 상태 수(시작 포함)
    public sealed class SolveResult
    {
        public SolveResult(IReadOnlyList<BoardMove> path, int explored, bool limited, bool deadAtStart)
        {
            Path = path;
            Explored = explored;
            Limited = limited;
            DeadAtStart = deadAtStart;
        }

        public IReadOnlyList<BoardMove> Path { get; }
        public int Explored { get; }
        public bool Limited { get; }      // 탐색 상한에 걸려 끝까지 못 봄
        public bool DeadAtStart { get; }  // 시작부터 막힘
        public bool Solved => Path != null;
    }

    // 순서 민감도: 최적 풀이의 모든 순서 중 성공한 수 (GDD §8)
    public readonly struct OrderSensitivity
    {
        public OrderSensitivity(int succeeded, int total)
        {
            Succeeded = succeeded;
            Total = total;
        }

        public int Succeeded { get; }
        public int Total { get; }
    }

    // 너비 우선 탐색 솔버 (GDD §8) — 프로토타입 solve · orderSensitivity와 같은 순서로 탐색해 같은 풀이를 찾는다.
    // 막힌 상태 · 색이 안 바뀌는 획은 제외한다. 동기 실행이라 에디터 도구용 — 런타임에 쓰려면 프레임을 나눠야 한다(CLAUDE.md §4)
    public static class Solver
    {
        public const int DefaultLimit = 400000;  // 프로토타입 플레이 화면과 같은 상한
        public const int MaxOrderMoves = 7;      // 순서 민감도는 7수(5040가지)까지만

        private const int BitsPerCell = 3;
        private const int CellsPerWord = 64 / BitsPerCell;  // 21
        public const int MaxCells = CellsPerWord * 2;       // 상태 키 두 워드 = 42칸

        public static SolveResult Solve(Board board, PaintColor[] start, int limit = DefaultLimit)
        {
            if (board.CellCount > MaxCells) throw new ArgumentException($"칸이 {MaxCells}개를 넘습니다: {board.CellCount}", nameof(board));
            if (board.IsSolved(start)) return new SolveResult(Array.Empty<BoardMove>(), 1, false, false);
            if (board.IsDead(start)) return new SolveResult(null, 1, false, true);

            StateKey startKey = StateKey.Of(start);
            var previous = new Dictionary<StateKey, (StateKey from, int move)>();
            var seen = new HashSet<StateKey> { startKey };
            var frontier = new List<PaintColor[]> { start };
            IReadOnlyList<BoardMove> moves = board.Moves;
            int explored = 1;

            while (frontier.Count > 0)
            {
                var next = new List<PaintColor[]>();
                foreach (PaintColor[] state in frontier)
                {
                    StateKey key = StateKey.Of(state);
                    for (int m = 0; m < moves.Count; m++)
                    {
                        var candidate = (PaintColor[])state.Clone();
                        if (!board.Brush(candidate, moves[m].Cell, moves[m].Direction)) continue;
                        StateKey candidateKey = StateKey.Of(candidate);
                        if (seen.Contains(candidateKey) || board.IsDead(candidate)) continue;
                        seen.Add(candidateKey);
                        previous[candidateKey] = (key, m);
                        explored++;
                        if (board.IsSolved(candidate)) return new SolveResult(PathTo(candidateKey, startKey, previous, moves), explored, false, false);
                        if (explored > limit) return new SolveResult(null, explored, true, false);
                        next.Add(candidate);
                    }
                }
                frontier = next;
            }
            return new SolveResult(null, explored, false, false);
        }

        // 풀이의 순서를 모두 바꿔 대입해 본다. 2수 미만 · 7수 초과면 null (프로토타입과 같음)
        public static OrderSensitivity? MeasureOrder(Board board, PaintColor[] start, IReadOnlyList<BoardMove> path)
        {
            if (path == null || path.Count < 2 || path.Count > MaxOrderMoves) return null;
            int succeeded = 0;
            int total = 0;
            var order = new int[path.Count];
            var used = new bool[path.Count];
            Permute(0);
            return new OrderSensitivity(succeeded, total);

            void Permute(int depth)
            {
                if (depth == order.Length)
                {
                    var state = (PaintColor[])start.Clone();
                    foreach (int k in order) board.Brush(state, path[k].Cell, path[k].Direction);
                    total++;
                    if (board.IsSolved(state)) succeeded++;
                    return;
                }
                for (int k = 0; k < order.Length; k++)
                {
                    if (used[k]) continue;
                    used[k] = true;
                    order[depth] = k;
                    Permute(depth + 1);
                    used[k] = false;
                }
            }
        }

        private static IReadOnlyList<BoardMove> PathTo(StateKey end, StateKey start, Dictionary<StateKey, (StateKey from, int move)> previous, IReadOnlyList<BoardMove> moves)
        {
            var path = new List<BoardMove>();
            for (StateKey key = end; !key.Equals(start); key = previous[key].from) path.Add(moves[previous[key].move]);
            path.Reverse();
            return path;
        }

        // 칸당 3비트로 압축한 상태 (최대 42칸)
        private readonly struct StateKey : IEquatable<StateKey>
        {
            private readonly ulong _low;
            private readonly ulong _high;

            private StateKey(ulong low, ulong high)
            {
                _low = low;
                _high = high;
            }

            public static StateKey Of(PaintColor[] state)
            {
                ulong low = 0, high = 0;
                for (int i = 0; i < state.Length; i++)
                {
                    ulong bits = (ulong)state[i] << (BitsPerCell * (i % CellsPerWord));
                    if (i < CellsPerWord) low |= bits;
                    else high |= bits;
                }
                return new StateKey(low, high);
            }

            public bool Equals(StateKey other) => _low == other._low && _high == other._high;
            public override bool Equals(object obj) => obj is StateKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(_low, _high);
        }
    }
}
