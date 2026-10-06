namespace ColoringBoot.Core
{
    public enum HintKind
    {
        Solved,   // 이미 완성
        Next,     // 다음 한 수
        Undo,     // 지금 상태로는 풀 수 없음 — 몇 수 되돌려야 다시 풀 수 있는지
        Unknown,  // 탐색 상한에 걸려 모름
    }

    // 힌트 (GDD §6, 2026-10-07) — PuzzleSession.FindHint의 결과
    public readonly struct Hint
    {
        private Hint(HintKind kind, BoardMove move, int count)
        {
            Kind = kind;
            Move = move;
            Count = count;
        }

        public HintKind Kind { get; }
        public BoardMove Move { get; }   // Next: 다음 한 수
        public int Count { get; }        // Next: 남은 최소 수(이 수 포함) · Undo: 되돌릴 수

        public static Hint Solved => new Hint(HintKind.Solved, default, 0);
        public static Hint Unknown => new Hint(HintKind.Unknown, default, 0);
        public static Hint Next(BoardMove move, int remaining) => new Hint(HintKind.Next, move, remaining);
        public static Hint Undo(int undos) => new Hint(HintKind.Undo, default, undos);
    }
}
