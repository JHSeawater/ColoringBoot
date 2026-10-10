using System;
using System.Collections.Generic;

namespace ColoringBoot.Core
{
    // 난이도 지표 (GDD §5 · §8, 2026-10-07) — 소재 맵 시험(DevelopLog 2026-10-06)과 같은 정의.
    // 칸 지표(막힐 수 있는 칸 · 섞인 색)는 바로 세고, 상태 지표(막히는 획 · 조용한 막다른 길)는 시작에서 닿는 막히지 않은 상태를 모두 센다(상한 안에서만)
    public sealed class StageMetrics
    {
        private StageMetrics(int cells, int trapCells, int paintedCells, int mixedCells, bool complete, int states, int silentStates, int strokes, int deadStrokes)
        {
            Cells = cells;
            TrapCells = trapCells;
            PaintedCells = paintedCells;
            MixedCells = mixedCells;
            Complete = complete;
            States = states;
            SilentStates = silentStates;
            Strokes = strokes;
            DeadStrokes = deadStrokes;
        }

        public int Cells { get; }
        public int TrapCells { get; }      // 막힐 수 있는 칸: 시작 색에 쓰인 기본색 중 하나라도 목표에 없는 칸
        public int PaintedCells { get; }   // 목표가 빈칸이 아닌 칸
        public int MixedCells { get; }     // 목표가 섞인 색(주황 · 보라 · 초록 · 검정)인 칸
        public bool Complete { get; }      // 닿는 상태를 상한 안에서 다 셌는가 — 아니면 아래 넷은 0이고 비율은 null
        public int States { get; }         // 시작에서 닿는 막히지 않은 상태(시작 포함)
        public int SilentStates { get; }   // 그중 목표에 닿을 수 없는 상태 — 막힘 표시 없이 풀 수 없게 된 "조용한 막다른 길"
        public int Strokes { get; }        // 그 상태들에서 색이 바뀌는 획
        public int DeadStrokes { get; }    // 그중 막히는 획

        public double TrapRatio => Cells == 0 ? 0 : (double)TrapCells / Cells;
        public double MixedRatio => PaintedCells == 0 ? 0 : (double)MixedCells / PaintedCells;
        public double? DeadStrokeRatio => Complete && Strokes > 0 ? (double)DeadStrokes / Strokes : (double?)null;
        public double? SilentRatio => Complete && States > 0 ? (double)SilentStates / States : (double?)null;

        // 막힐 수 있는 칸 수 — 시작 색들의 기본색 합에서 목표에 없는 색이 있는 칸 (생성기 조건도 쓴다)
        public static int CountTrapCells(PaintColor[] start, PaintColor[] target)
        {
            PaintColor used = PaintColor.Empty;
            foreach (PaintColor c in start) used |= c;
            int count = 0;
            foreach (PaintColor t in target)
            {
                if ((used & ~t) != PaintColor.Empty) count++;
            }
            return count;
        }

        // 섞인 색 목표 칸 수와 칠해지는(빈칸이 아닌) 목표 칸 수
        public static int CountMixedCells(PaintColor[] target, out int painted)
        {
            int mixed = 0;
            painted = 0;
            foreach (PaintColor t in target)
            {
                if (t == PaintColor.Empty) continue;
                painted++;
                if (t != PaintColor.Red && t != PaintColor.Yellow && t != PaintColor.Blue) mixed++;
            }
            return mixed;
        }

        public static StageMetrics Measure(Board board, int limit = Solver.DefaultLimit)
        {
            if (board.CellCount > Solver.MaxCells) throw new ArgumentException($"칸이 {Solver.MaxCells}개를 넘습니다: {board.CellCount}", nameof(board));
            PaintColor[] start = board.CreateStartState();
            // 칸 지표는 일반 칸만 센다 — 벽 · 물 · 코팅 칸은 색이 바뀌지 않는다(기믹, 2026-10-10). 코팅 칸의 색은 공급 색이라 막힐 칸 계산의 시작 색에는 넣는다
            var paintStart = new List<PaintColor>();
            var paintTarget = new List<PaintColor>();
            for (int i = 0; i < board.CellCount; i++)
            {
                if (board.KindOf(i) == CellKind.Coated) paintStart.Add(start[i]);
                if (board.KindOf(i) != CellKind.Paint) continue;
                paintStart.Add(start[i]);
                paintTarget.Add(board.TargetOf(i));
            }
            PaintColor[] target = paintTarget.ToArray();
            int trap = CountTrapCells(paintStart.ToArray(), target);
            int mixed = CountMixedCells(target, out int painted);
            if (board.IsDead(start)) return new StageMetrics(target.Length, trap, painted, mixed, false, 0, 0, 0, 0);

            // 닿는 상태를 모두 펼친다 — 상태마다 번호, 번호마다 그 상태로 오는 앞 상태들(목표에서 거꾸로 따라가려고)
            var index = new Dictionary<Solver.StateKey, int> { [Solver.StateKey.Of(start)] = 0 };
            var states = new List<PaintColor[]> { start };
            var predecessors = new List<List<int>> { new List<int>() };
            IReadOnlyList<BoardMove> moves = board.Moves;
            int strokes = 0;
            int deadStrokes = 0;
            int goal = board.IsSolved(start) ? 0 : -1;
            for (int s = 0; s < states.Count; s++)
            {
                for (int m = 0; m < moves.Count; m++)
                {
                    var next = (PaintColor[])states[s].Clone();
                    if (!board.Brush(next, moves[m].Cell, moves[m].Direction)) continue;
                    strokes++;
                    if (board.IsDead(next))
                    {
                        deadStrokes++;
                        continue;
                    }
                    Solver.StateKey key = Solver.StateKey.Of(next);
                    if (!index.TryGetValue(key, out int n))
                    {
                        if (states.Count >= limit) return new StageMetrics(target.Length, trap, painted, mixed, false, 0, 0, 0, 0);
                        n = states.Count;
                        index[key] = n;
                        states.Add(next);
                        predecessors.Add(new List<int>());
                        if (goal < 0 && board.IsSolved(next)) goal = n;
                    }
                    predecessors[n].Add(s);
                }
            }

            // 목표에 닿을 수 있는 상태 = 목표에서 거꾸로 닿는 상태
            var canSolve = new bool[states.Count];
            int solvable = 0;
            if (goal >= 0)
            {
                var queue = new Queue<int>();
                canSolve[goal] = true;
                queue.Enqueue(goal);
                while (queue.Count > 0)
                {
                    solvable++;
                    foreach (int p in predecessors[queue.Dequeue()])
                    {
                        if (canSolve[p]) continue;
                        canSolve[p] = true;
                        queue.Enqueue(p);
                    }
                }
            }
            return new StageMetrics(target.Length, trap, painted, mixed, true, states.Count, states.Count - solvable, strokes, deadStrokes);
        }
    }
}
