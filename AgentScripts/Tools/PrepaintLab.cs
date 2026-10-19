using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ColoringBoot.Core;

// 배경 + 미리 칠한 소재 칸 후보(2026-10-19 — GDD §5) — 게임 데이터는 쓰지 않는다. 고른 후보를 챕터 Stages/ 파일에 옮긴다
// run_script(file=AgentScripts/Tools/PrepaintLab.cs, entry=PrepaintLab.Generate, args=["sky", 1] 또는 ["sky", 4, 3])
//   — 배경(sky · hill) · 시드 · (미리 칠할 칸 수 고정). 후보는 Builds/PrepaintLab/<배경>_<시드>[_n<칸 수>].json(git이 무시)에 쓰고 요약을 돌려준다
// 지금 챕터 판의 목표 그림을 그대로 두고, 그림 속 소재 자리(앵커) 근처 칸 몇 개를 소재 색으로 미리 칠한 뒤(시작 = 목표)
// 나머지 칸에서 시작 색 배치를 무작위로 찾는다. 앵커는 챕터 1 그림(reference.png)의 소재 위치를 맵 좌표(x = q + r/2, y = r)로 옮긴 값
public static class PrepaintLab
{
    private const int TimeLimitMs = 45000;   // run_script 응답 한도(60초) 안에서
    private const int SolveLimit = 120000;
    private const int MetricsLimit = 300000;
    private const int MinMoves = 6, MaxMoves = 7;   // 챕터 1 최대 수(7) 안에서
    private const double MaxOrderRatio = 0.34;
    private const double MaxSolveChance = 0.5;      // 지금 언덕 59% · 하늘 53%보다 어렵게
    private const double ZoneRadius = 1.6;          // 앵커에서 칸 너비 몇 개까지 놓을 수 있나
    private const string OutFolder = "Builds/PrepaintLab";
    private static readonly PaintColor[] _primaries = { PaintColor.Red, PaintColor.Yellow, PaintColor.Blue };

    private sealed class Background
    {
        public string File;
        public PaintColor Color;              // 미리 칠할 소재 색
        public int MinCount, MaxCount;        // 미리 칠할 칸 수
        public (double x, double y)[] Anchors;
        public bool Compact;                  // 한 덩어리(바구니) / 따로 떨어짐(벌 여러 마리)
    }

    private static readonly Dictionary<string, Background> Backgrounds = new Dictionary<string, Background>
    {
        // 하늘 + 노랑 벌: 그림 가운데 아래 세 마리
        ["sky"] = new Background
        {
            File = "Assets/Data/Chapters/Chapter1/Stages/Sky.json", Color = PaintColor.Yellow, MinCount = 2, MaxCount = 3,
            Anchors = new[] { (1.89, 2.42), (2.80, 2.12), (2.82, 2.87) }, Compact = false,
        },
        // 언덕(풀밭) + 주황 바구니: 그림 가운데 아래 큰 바구니 하나
        ["hill"] = new Background
        {
            File = "Assets/Data/Chapters/Chapter1/Stages/Field.json", Color = PaintColor.Orange, MinCount = 2, MaxCount = 3,
            Anchors = new[] { (2.48, 2.3) }, Compact = true,
        },
    };

    // count = 미리 칠할 칸 수를 하나로 고정(0이면 배경의 범위대로)
    public static string Generate(string key, int seed, int count = 0)
    {
        Background bg = Backgrounds[key];
        Stage baseStage = Stage.Parse(File.ReadAllText(bg.File));
        int n = baseStage.Cells.Count;
        var zone = Enumerable.Range(0, n).Where(i => bg.Anchors.Any(a => Distance(baseStage.Cells[i].Coord, a) <= ZoneRadius)).ToList();

        var random = new SeededRandom((uint)seed);
        var seen = new HashSet<string>();
        var found = new List<string>();
        var clock = Stopwatch.StartNew();
        int tries = 0, shape = 0, unsolved = 0, ordered = 0, easy = 0;
        while (clock.ElapsedMilliseconds < TimeLimitMs)
        {
            tries++;
            int marks = count > 0 ? count : bg.MinCount + random.Next(bg.MaxCount - bg.MinCount + 1);
            var marked = new List<int>();
            while (marked.Count < marks)
            {
                int pick = zone[random.Next(zone.Count)];
                if (!marked.Contains(pick)) marked.Add(pick);
            }
            if (!Arranged(baseStage, marked, bg.Compact)) { shape++; continue; }

            // 목표 = 지금 판의 그림 + 미리 칠한 칸, 시작 = 미리 칠한 칸 + 무작위 시작 색
            var target = baseStage.Cells.Select(c => c.Target).ToArray();
            var start = new PaintColor[n];
            foreach (int i in marked) target[i] = start[i] = bg.Color;
            var paintable = Enumerable.Range(0, n).Where(i => target[i] != PaintColor.Empty && !marked.Contains(i)).ToList();
            int seeds = 2 + random.Next(4);
            PaintColor supplied = bg.Color, needed = PaintColor.Empty;
            foreach (PaintColor t in target) needed |= t;
            for (int k = 0; k < seeds; k++)
            {
                int pick;
                do pick = paintable[random.Next(paintable.Count)]; while (start[pick] != PaintColor.Empty);
                PaintColor[] choices = _primaries.Where(p => (p & ~target[pick]) == PaintColor.Empty).ToArray();
                start[pick] = choices[random.Next(choices.Length)];
                supplied |= start[pick];
            }
            if ((needed & ~supplied) != PaintColor.Empty) { unsolved++; continue; }
            string id = string.Join(",", start.Select(s => (int)s)) + "|" + string.Join(",", marked.OrderBy(i => i));
            if (!seen.Add(id)) continue;

            Stage stage = Make(baseStage, start, target, null);
            var board = new Board(stage);
            SolveResult result = Solver.Solve(board, board.CreateStartState(), SolveLimit);
            if (!result.Solved || result.Path.Count < MinMoves || result.Path.Count > MaxMoves) { unsolved++; continue; }
            OrderSensitivity? order = Solver.MeasureOrder(board, board.CreateStartState(), result.Path);
            if (order.HasValue && (double)order.Value.Succeeded / order.Value.Total > MaxOrderRatio) { ordered++; continue; }
            stage = Make(baseStage, start, target, result.Path.Count);
            StageMetrics m = StageMetrics.Measure(new Board(stage), MetricsLimit);
            if (!m.SolveChance.HasValue || m.SolveChance.Value > MaxSolveChance) { easy++; continue; }

            double fit = Fit(baseStage, marked, bg);
            string cells = string.Join(",", marked.Select(i => $"[{baseStage.Cells[i].Coord.Q},{baseStage.Cells[i].Coord.R}]"));
            found.Add($"{{\"moves\":{result.Path.Count},\"order\":\"{(order.HasValue ? $"{order.Value.Succeeded}/{order.Value.Total}" : "-")}\""
                + $",\"trap\":{Num(m.TrapRatio)},\"silent\":{Num(m.SilentRatio)},\"chance\":{Num(m.SolveChance)},\"fit\":{Num(fit)}"
                + $",\"seeds\":{seeds},\"marked\":[{cells}],\"stage\":{StageWriter.ToJson(stage)}}}");
        }
        Directory.CreateDirectory(OutFolder);
        string path = count > 0 ? $"{OutFolder}/{key}_{seed}_n{count}.json" : $"{OutFolder}/{key}_{seed}.json";
        File.WriteAllText(path, "[\n" + string.Join(",\n", found) + "\n]\n", new UTF8Encoding(false));
        return $"{key} 시드 {seed}: 후보 {found.Count}개 / 시도 {tries}번(배치 탈락 {shape} · 풀이 탈락 {unsolved} · 순서 쉬움 {ordered} · 대충 풀림 높음 {easy}) · 놓을 수 있는 칸 {zone.Count}개 → {path}";
    }

    // 벌은 서로 붙지 않게, 바구니는 한 덩어리로
    private static bool Arranged(Stage stage, List<int> marked, bool compact)
    {
        bool Adjacent(int a, int b)
        {
            int dq = stage.Cells[a].Coord.Q - stage.Cells[b].Coord.Q, dr = stage.Cells[a].Coord.R - stage.Cells[b].Coord.R;
            return Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr) == 2;
        }
        if (!compact) return marked.All(a => marked.All(b => a == b || !Adjacent(a, b)));
        return marked.All(a => marked.Any(b => a != b && Adjacent(a, b)));
    }

    // 그림 위치 일치도 — 칸과 앵커의 평균 거리(칸 너비). 따로 떨어진 소재는 칸마다 다른 앵커에 짝짓고, 덩어리는 무게중심끼리
    private static double Fit(Stage stage, List<int> marked, Background bg)
    {
        if (bg.Compact)
        {
            double cx = marked.Average(i => X(stage.Cells[i].Coord)), cy = marked.Average(i => (double)stage.Cells[i].Coord.R);
            return Distance(cx, cy, bg.Anchors[0].x, bg.Anchors[0].y);
        }
        double best = double.MaxValue;
        foreach (int[] perm in Permutations(Enumerable.Range(0, bg.Anchors.Length).ToArray(), marked.Count))
        {
            double sum = 0;
            for (int k = 0; k < marked.Count; k++) sum += Distance(stage.Cells[marked[k]].Coord, bg.Anchors[perm[k]]);
            best = Math.Min(best, sum / marked.Count);
        }
        return best;
    }

    private static IEnumerable<int[]> Permutations(int[] items, int length)
    {
        if (length == 0) { yield return new int[0]; yield break; }
        foreach (int item in items)
        {
            foreach (int[] rest in Permutations(items.Where(i => i != item).ToArray(), length - 1))
                yield return new[] { item }.Concat(rest).ToArray();
        }
    }

    private static double X(HexCoord c) => c.Q + c.R / 2.0;
    private static double Distance(HexCoord c, (double x, double y) a) => Distance(X(c), c.R, a.x, a.y);
    // 화면 배치 비율(가로 √3 · 세로 1.5)로 잰 거리를 칸 너비 단위로
    private static double Distance(double x1, double y1, double x2, double y2) =>
        Math.Sqrt(Math.Pow((x1 - x2) * Math.Sqrt(3), 2) + Math.Pow((y1 - y2) * 1.5, 2)) / Math.Sqrt(3);

    private static Stage Make(Stage baseStage, PaintColor[] start, PaintColor[] target, int? minMoves) =>
        new Stage(baseStage.Name, baseStage.Cells.Select((c, i) => new StageCell(c.Coord, start[i], target[i])).ToList(), baseStage.Palette, minMoves);

    private static string Num(double? v) => v.HasValue ? v.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "null";
}
