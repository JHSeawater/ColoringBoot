using System;
using System.Collections.Generic;

namespace ColoringBoot.Core
{
    // 난이도 지표 (GDD §5 · §8, 2026-10-07) — 소재 맵 시험(DevelopLog 2026-10-06)과 같은 정의.
    // 칸 지표(막힐 수 있는 칸 · 섞인 색)는 바로 세고, 상태 지표(막히는 획 · 조용한 막다른 길)는 시작에서 닿는 막히지 않은 상태를 모두 센다(상한 안에서만)
    public sealed class StageMetrics
    {
        private StageMetrics(int cells, int trapCells, int paintedCells, int mixedCells, bool complete, int states, int silentStates, int strokes, int deadStrokes, double? solveChance)
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
            SolveChance = solveChance;
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

        // 대충 풀림: 막히지 않는 획 중 하나를 매번 같은 확률로 골라 그을 때 결국 풀릴 확률(2026-10-11 — 챕터 1 순서를 이것으로 정함).
        // 사람이 실제로 깨는 비율이 아니라 판끼리 비교하는 값 — 100%면 막힘만 피하면 반드시 풀린다. 다 세지 못했으면 null
        public double? SolveChance { get; }

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
            var target = new PaintColor[board.CellCount];
            for (int i = 0; i < target.Length; i++) target[i] = board.TargetOf(i);
            int trap = CountTrapCells(start, target);
            int mixed = CountMixedCells(target, out int painted);
            if (board.IsDead(start)) return new StageMetrics(target.Length, trap, painted, mixed, false, 0, 0, 0, 0, null);

            // 닿는 상태를 모두 펼친다 — 상태마다 번호, 번호마다 그 상태로 오는 앞 상태들(목표에서 거꾸로 따라가려고)
            var index = new Dictionary<Solver.StateKey, int> { [Solver.StateKey.Of(start)] = 0 };
            var states = new List<PaintColor[]> { start };
            var predecessors = new List<List<int>> { new List<int>() };
            var choices = new List<int>();   // 상태마다 막히지 않는 획 수(대충 풀림)
            IReadOnlyList<BoardMove> moves = board.Moves;
            int strokes = 0;
            int deadStrokes = 0;
            int goal = board.IsSolved(start) ? 0 : -1;
            for (int s = 0; s < states.Count; s++)
            {
                int safe = 0;
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
                        if (states.Count >= limit) return new StageMetrics(target.Length, trap, painted, mixed, false, 0, 0, 0, 0, null);
                        n = states.Count;
                        index[key] = n;
                        states.Add(next);
                        predecessors.Add(new List<int>());
                        if (goal < 0 && board.IsSolved(next)) goal = n;
                    }
                    predecessors[n].Add(s);
                    safe++;
                }
                choices.Add(safe);
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
            double chance = goal >= 0 ? RandomSolveChance(states, predecessors, choices, goal) : 0;
            return new StageMetrics(target.Length, trap, painted, mixed, true, states.Count, states.Count - solvable, strokes, deadStrokes, chance);
        }

        // 색은 빠지지 않아 획마다 색 비트 합이 는다 → 비트 합이 큰 상태부터 확률을 정하고 앞 상태들에 나눠 더한다
        // (앞 상태 목록에는 획마다 한 번씩 들어 있어 같은 상태로 가는 획 여러 개도 그만큼 센다). 목표에 닿으면 판이 끝난다
        private static double RandomSolveChance(List<PaintColor[]> states, List<List<int>> predecessors, List<int> choices, int goal)
        {
            var order = new int[states.Count];
            var bits = new int[states.Count];   // 비트 합의 음수 — 오름차순 정렬이 큰 것부터
            for (int s = 0; s < states.Count; s++)
            {
                order[s] = s;
                foreach (PaintColor c in states[s]) bits[s] -= ((int)c & 1) + ((int)c >> 1 & 1) + ((int)c >> 2 & 1);
            }
            Array.Sort(bits, order);
            var chance = new double[states.Count];
            var sum = new double[states.Count];   // 다음 상태들의 확률 합
            foreach (int s in order)
            {
                chance[s] = s == goal ? 1 : choices[s] == 0 ? 0 : sum[s] / choices[s];
                foreach (int p in predecessors[s]) sum[p] += chance[s];
            }
            return chance[0];
        }
    }
}
