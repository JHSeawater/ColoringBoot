using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ColoringBoot.Core;

// 소재 맵 후보 생성(2026-10-11 챕터 1 덩굴 · 언덕 · 하늘) — 게임 데이터는 쓰지 않는다. 고른 후보를 챕터 Stages/ 파일에 옮긴다
// run_script(file=AgentScripts/Tools/SubjectLab.cs, entry=SubjectLab.Generate, args=["vine", 8, 1])
//   — 소재(vine · hill · sky) · 후보 수 · 시드. 후보는 Builds/SubjectLab/<소재>.json(git이 무시)에 쓰고 요약을 돌려준다
// 소재 단서 = 맵 모양 + 목표 색 구성(GDD §5 2026-10-06). 시작 색 구성을 정해 두고 무작위 자리 · 무작위 획으로 목표를 만든 뒤
// 목표 색 구성 · 막힐 수 있는 칸 비율(단색 채우기 거르기) · 최소 수 · 순서 민감도로 거른다
public static class SubjectLab
{
    private const int TimeLimitMs = 45000;   // run_script 응답 한도(60초) 안에서
    private const int SolveLimit = 120000;
    private const int MetricsLimit = 200000;
    private const double MinTrapRatio = 0.3;
    private const double MaxOrderRatio = 0.34;   // 생성기 기본값과 같음
    private const string OutFolder = "Builds/SubjectLab";

    private sealed class Subject
    {
        public string Name;
        public string Palette;
        public (int r, int q0, int q1)[] Rows;   // 줄마다 q 범위 — 빈자리 없는 모양
        public PaintColor[] Seeds;               // 시작 색 칸(이 구성 그대로, 자리는 무작위)
        public PaintColor[] Targets;             // 목표에 쓸 색
        public double Fill;                      // 칠해진 칸 비율 하한
        public int MinSolve, MaxSolve;
        public Func<PaintColor[], bool> Looks;   // 목표 색 구성이 소재답나
    }

    private static readonly Dictionary<string, Subject> Subjects = new Dictionary<string, Subject>
    {
        // 덩굴: 오른쪽 위 → 왼쪽 아래로 뻗은 가지 띠, 초록 잎 + 주황 줄기(노랑이 두 색에 다 든다) — 노랑 두 칸 · 빈칸 조금까지.
        // 초록 · 주황만으로는 무작위 획 125만 번에 거의 안 나오고, 줄기 · 잎을 먼저 정한 그림 맵은 5~7수 풀이가 없었다(2026-10-11)
        ["vine"] = new Subject
        {
            Name = "덩굴", Rows = new[] { (0, 5, 7), (1, 3, 7), (2, 2, 5), (3, 0, 3), (4, -1, 1) },
            Seeds = new[] { PaintColor.Red, PaintColor.Yellow, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Green, PaintColor.Orange, PaintColor.Yellow }, Fill = 0.6, MinSolve = 5, MaxSolve = 5,
            Looks = t => Share(t, PaintColor.Green) >= 0.5 && Count(t, PaintColor.Orange) >= 2 && Count(t, PaintColor.Yellow) <= 2,
        },
        // 언덕: 둥근 언덕 모양, 초록 + 햇빛 받은 노랑
        ["hill"] = new Subject
        {
            Name = "언덕", Rows = new[] { (0, 2, 4), (1, 0, 4), (2, -1, 4), (3, -2, 4) },
            Seeds = new[] { PaintColor.Yellow, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Green, PaintColor.Yellow }, Fill = 0.8, MinSolve = 6, MaxSolve = 6,
            Looks = t => Share(t, PaintColor.Green) >= 0.5 && Count(t, PaintColor.Yellow) >= 3,
        },
        // 하늘: 넓은 판, 파랑 + 구름(빈칸) + 보라 노을 조금 — 파스텔
        ["sky"] = new Subject
        {
            Name = "하늘", Palette = "pastel", Rows = new[] { (0, 0, 5), (1, -1, 4), (2, -1, 4), (3, -2, 3) },
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Red },
            Targets = new[] { PaintColor.Blue, PaintColor.Purple }, Fill = 0.7, MinSolve = 7, MaxSolve = 7,
            Looks = t => Share(t, PaintColor.Blue) >= 0.6 && Count(t, PaintColor.Empty) >= 2,
        },
    };

    public static string Generate(string key, int count, int seed)
    {
        Subject subject = Subjects[key];
        var coords = new List<HexCoord>();
        foreach ((int r, int q0, int q1) in subject.Rows)
        {
            for (int q = q0; q <= q1; q++) coords.Add(new HexCoord(q, r));
        }
        var random = new SeededRandom((uint)seed);
        var seen = new HashSet<string>();
        var found = new List<string>();
        var report = new StringBuilder();
        var clock = Stopwatch.StartNew();
        int tries = 0, unlike = 0, unsolved = 0, ordered = 0;
        while (clock.ElapsedMilliseconds < TimeLimitMs && found.Count < count)
        {
            tries++;
            var start = new PaintColor[coords.Count];
            foreach (PaintColor color in subject.Seeds)
            {
                int pick;
                do pick = random.Next(coords.Count); while (start[pick] != PaintColor.Empty);
                start[pick] = color;
            }
            var board = new Board(Make(subject, coords, start, start, null));
            PaintColor[] state = board.CreateStartState();
            int strokes = subject.MinSolve + random.Next(3), used = 0;
            for (int t = 0; t < strokes * 6 && used < strokes; t++)
            {
                BoardMove move = board.Moves[random.Next(board.Moves.Count)];
                if (board.Brush(state, move.Cell, move.Direction)) used++;
            }
            if (used < strokes) continue;
            // 보드의 칸 순서는 좌표 순서와 같다(빈자리 · 기믹 없음)
            if (state.Any(c => c != PaintColor.Empty && Array.IndexOf(subject.Targets, c) < 0)
                || (double)state.Count(c => c != PaintColor.Empty) / state.Length < subject.Fill
                || !subject.Looks(state)
                || (double)StageMetrics.CountTrapCells(start, state) / state.Length < MinTrapRatio) { unlike++; continue; }
            string id = string.Join(",", start.Zip(state, (s, t) => $"{(int)s}{(int)t}"));
            if (!seen.Add(id)) continue;

            Stage stage = Make(subject, coords, start, state, null);
            var solvedBoard = new Board(stage);
            SolveResult result = Solver.Solve(solvedBoard, solvedBoard.CreateStartState(), SolveLimit);
            if (!result.Solved || result.Path.Count < subject.MinSolve || result.Path.Count > subject.MaxSolve) { unsolved++; continue; }
            OrderSensitivity? order = Solver.MeasureOrder(solvedBoard, solvedBoard.CreateStartState(), result.Path);
            if (order.HasValue && (double)order.Value.Succeeded / order.Value.Total > MaxOrderRatio) { ordered++; continue; }

            stage = Make(subject, coords, start, state, result.Path.Count);
            StageMetrics m = StageMetrics.Measure(new Board(stage), MetricsLimit);
            string line = $"{result.Path.Count}수 · 순서 {(order.HasValue ? $"{order.Value.Succeeded}/{order.Value.Total}" : "-")}"
                + $" · 막힐 칸 {Percent(m.TrapRatio)} · 섞인 색 {Percent(m.MixedRatio)} · 막히는 획 {Percent(m.DeadStrokeRatio)} · 조용한 막다른 길 {Percent(m.SilentRatio)}"
                + $" · 대충 풀림 {Percent(m.SolveChance)}";
            found.Add($"{{\"metrics\":\"{line}\",\"stage\":{StageWriter.ToJson(stage)}}}");
            report.AppendLine($"#{found.Count} {line}");
        }
        Directory.CreateDirectory(OutFolder);
        string path = $"{OutFolder}/{key}.json";
        File.WriteAllText(path, "[\n" + string.Join(",\n", found) + "\n]\n", new UTF8Encoding(false));
        return $"{subject.Name}({coords.Count}칸) 후보 {found.Count}개 / 시도 {tries}번(색 구성 탈락 {unlike} · 수 범위 밖 {unsolved} · 순서 쉬움 {ordered}) · {clock.ElapsedMilliseconds / 1000}초 → {path}\n{report}";
    }

    private static Stage Make(Subject subject, List<HexCoord> coords, PaintColor[] start, PaintColor[] target, int? minMoves) =>
        new Stage(subject.Name, coords.Select((c, i) => new StageCell(c, start[i], target[i])).ToList(), subject.Palette, minMoves);

    private static int Count(PaintColor[] target, PaintColor color) => target.Count(t => t == color);

    // 칠해진 칸 중 그 색의 비율
    private static double Share(PaintColor[] target, PaintColor color)
    {
        int painted = target.Count(t => t != PaintColor.Empty);
        return painted == 0 ? 0 : (double)Count(target, color) / painted;
    }

    private static string Percent(double? ratio) => ratio.HasValue ? $"{ratio.Value * 100:0}%" : "모름";
}
