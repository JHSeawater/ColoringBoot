using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColoringBoot.Core;

// 기믹 시험 스테이지 후보 생성(2026-10-10 시제품) — 파일은 쓰지 않는다. 고른 후보를 Assets/Data/Lab/Stages/에 옮긴다
// run_script(file=AgentScripts/Tools/GimmickLab.cs, entry=GimmickLab.Generate, args=["wall", 5, 1, 4, 5, 0.3, true])
//   — 종류(wall · water · coated) · 후보 수 · 시드 · 최소 수 범위 · 섞인 색 비율 하한 · strict(기믹을 빼면 아예 못 푸는 것만)
// 생성기 모양에 기믹 칸 · 시작 색을 무작위로 놓고 무작위 획으로 목표를 만든 뒤 거른다: 최소 수 범위 · 기믹을 빼면 그 풀이가 안 됨 · 풀이의 2획 이상이 기믹을 씀
public static class GimmickLab
{
    private const int Attempts = 3000;
    private const int SolveLimit = 120000;
    private const int MetricsLimit = 200000;
    private static readonly BoardShape[] Shapes = { BoardShape.SmallHex, BoardShape.Diamond, BoardShape.Blob, BoardShape.Triangle };
    private static readonly PaintColor[] Primaries = { PaintColor.Red, PaintColor.Yellow, PaintColor.Blue };
    private const string Letters = ".RYOBPGK";

    public static string Generate(string kind, int count, int seed, int minSolve, int maxSolve, double minMixed, bool strict)
    {
        CellKind gimmick = kind == "wall" ? CellKind.Wall : kind == "water" ? CellKind.Water : CellKind.Coated;
        var random = new SeededRandom((uint)seed);
        var report = new StringBuilder();
        var seen = new HashSet<string>();
        int found = 0, attempts = 0;
        for (; attempts < Attempts && found < count; attempts++)
        {
            Stage stage = Build(gimmick, random, minSolve);
            if (stage == null) continue;
            var board = new Board(stage);
            PaintColor[] start = board.CreateStartState();
            if (board.IsDead(start) || board.IsSolved(start)) continue;
            SolveResult solved = Solver.Solve(board, start, SolveLimit);
            if (!solved.Solved || solved.Path.Count < minSolve || solved.Path.Count > maxSolve) continue;

            Stage plain = Variant(stage);
            var plainBoard = new Board(plain);
            if (ReplaySolves(board, plainBoard, solved.Path)) continue;           // 기믹 없이도 같은 풀이가 된다
            int uses = Uses(board, plainBoard, start, solved.Path, gimmick);
            if (uses < 2) continue;
            string key = string.Join(",", stage.Cells.Select(c => $"{c.Coord.Q}:{c.Coord.R}:{(int)c.Target}:{(int)c.Kind}"));
            if (!seen.Add(key)) continue;

            StageMetrics metrics = StageMetrics.Measure(board, MetricsLimit);
            if (metrics.MixedRatio < minMixed) continue;
            SolveResult plainSolved = Solver.Solve(plainBoard, plainBoard.CreateStartState(), SolveLimit);
            if (strict && (plainSolved.Solved || plainSolved.Limited)) continue;
            OrderSensitivity? order = Solver.MeasureOrder(board, start, solved.Path);
            found++;
            var final = new Stage(stage.Name, stage.Cells, null, solved.Path.Count);
            report.AppendLine($"#{found} {solved.Path.Count}수 · 순서 {(order.HasValue ? $"{order.Value.Succeeded}/{order.Value.Total}" : "-")} · 기믹 쓰는 획 {uses}"
                + $" · 막힐 칸 {Percent(metrics.TrapRatio)} · 섞인 색 {Percent(metrics.MixedRatio)} · 조용한 막다른 길 {Percent(metrics.SilentRatio)}"
                + $" · 기믹 빼면 {(plainSolved.Solved ? $"{plainSolved.Path.Count}수" : plainSolved.Limited ? "모름" : "못 풂")}");
            report.AppendLine(StageWriter.ToJson(final));
            report.AppendLine(Picture(board, start));
            report.AppendLine(Picture(board, Targets(board)));
        }
        return $"{kind} {minSolve}~{maxSolve}수: 후보 {found}개 / 시도 {attempts}번\n{report}";
    }

    // 모양 · 기믹 칸 · 시작 색을 놓고 무작위 획으로 목표를 만든다. 칸 순서 = 일반 · 코팅 → 벽 → 물(Stage.Parse와 같게)
    private static Stage Build(CellKind gimmick, SeededRandom random, int minSolve)
    {
        List<HexCoord> coords = Generator.ShapeCells(Shapes[random.Next(Shapes.Length)], random);
        int specials = gimmick == CellKind.Wall ? 2 + random.Next(2) : 1 + random.Next(2);
        var kinds = new CellKind[coords.Count];
        var start = new PaintColor[coords.Count];
        for (int placed = 0; placed < specials;)
        {
            int pick = random.Next(coords.Count);
            if (kinds[pick] != CellKind.Paint) continue;
            kinds[pick] = gimmick;
            if (gimmick == CellKind.Coated) start[pick] = Primaries[random.Next(Primaries.Length)];
            placed++;
        }
        int seeds = gimmick == CellKind.Coated ? 1 + random.Next(2) : 2 + random.Next(2);
        for (int placed = 0; placed < seeds;)
        {
            int pick = random.Next(coords.Count);
            if (kinds[pick] != CellKind.Paint || start[pick] != PaintColor.Empty) continue;
            start[pick] = Primaries[random.Next(Primaries.Length)];
            placed++;
        }

        // 목표: 기믹이 있는 보드에서 무작위 획 — 일반 칸 절반 이상이 칠해져야 한다
        Stage blank = Make(coords, kinds, start, start);
        var board = new Board(blank);
        PaintColor[] state = board.CreateStartState();
        int strokes = minSolve + random.Next(3), used = 0;
        for (int t = 0; t < strokes * 6 && used < strokes; t++)
        {
            BoardMove move = board.Moves[random.Next(board.Moves.Count)];
            if (board.Brush(state, move.Cell, move.Direction)) used++;
        }
        if (used < strokes) return null;
        int paint = 0, painted = 0;
        for (int i = 0; i < board.CellCount; i++)
        {
            if (board.KindOf(i) != CellKind.Paint) continue;
            paint++;
            if (state[i] != PaintColor.Empty) painted++;
        }
        if (painted * 2 < paint) return null;
        // board의 칸 순서(Make가 정렬한 순서)대로 목표를 옮긴다
        var target = new PaintColor[coords.Count];
        for (int i = 0; i < coords.Count; i++) target[i] = state[board.IndexOf(coords[i])];
        return Make(coords, kinds, start, target);
    }

    private static Stage Make(List<HexCoord> coords, CellKind[] kinds, PaintColor[] start, PaintColor[] target)
    {
        var cells = new List<StageCell>();
        foreach (CellKind kind in new[] { CellKind.Paint, CellKind.Wall, CellKind.Water })
        {
            for (int i = 0; i < coords.Count; i++)
            {
                CellKind k = kinds[i] == CellKind.Coated ? CellKind.Paint : kinds[i];   // 코팅 칸은 일반 칸 자리에
                if (k != kind) continue;
                bool none = kinds[i] == CellKind.Wall || kinds[i] == CellKind.Water;
                cells.Add(new StageCell(coords[i], none ? PaintColor.Empty : start[i], none ? PaintColor.Empty : kinds[i] == CellKind.Coated ? start[i] : target[i], kinds[i]));
            }
        }
        return new Stage("기믹", cells);
    }

    // 기믹을 뺀 보드: 벽 · 물 칸은 빈자리로, 코팅 칸은 같은 색의 일반 칸으로
    private static Stage Variant(Stage stage) => new Stage(stage.Name, stage.Cells
        .Where(c => c.Kind != CellKind.Wall && c.Kind != CellKind.Water)
        .Select(c => new StageCell(c.Coord, c.Start, c.Target)).ToList());

    // 같은 칸 · 방향의 획을 기믹 없는 보드에 그대로 그어 풀리는가
    private static bool ReplaySolves(Board board, Board plain, IReadOnlyList<BoardMove> path)
    {
        PaintColor[] state = plain.CreateStartState();
        foreach (BoardMove move in path)
        {
            int cell = plain.IndexOf(board.CoordOf(move.Cell));
            if (cell < 0) return false;
            plain.Brush(state, cell, move.Direction);
            if (plain.IsDead(state)) return false;
        }
        return plain.IsSolved(state);
    }

    // 풀이에서 기믹이 결과를 바꾸는 획 수 — 벽: 구간이 줄보다 짧음 · 물: 색 묻은 붓이 씻긴 뒤에도 칸이 남음 · 코팅: 붓에 새 색을 묻힌 뒤에도 칸이 남음
    private static int Uses(Board board, Board plain, PaintColor[] start, IReadOnlyList<BoardMove> path, CellKind gimmick)
    {
        PaintColor[] state = (PaintColor[])start.Clone();
        var cells = new int[board.CellCount];
        var brushes = new PaintColor[board.CellCount];
        int uses = 0;
        foreach (BoardMove move in path)
        {
            int n = board.Trace(state, move.Cell, move.Direction, cells, brushes);
            bool use = false;
            if (gimmick == CellKind.Wall)
            {
                int cell = plain.IndexOf(board.CoordOf(move.Cell));
                use = cell >= 0 && board.LineLength(move.Cell, move.Direction) < plain.LineLength(cell, move.Direction);
            }
            for (int k = 0; k < n - 1 && !use; k++)
            {
                if (board.KindOf(cells[k]) != gimmick) continue;
                PaintColor before = k == 0 ? PaintColor.Empty : brushes[k - 1];
                if (gimmick == CellKind.Water) use = before != PaintColor.Empty;
                if (gimmick == CellKind.Coated) use = (state[cells[k]] & ~before) != PaintColor.Empty;
            }
            if (use) uses++;
            board.Brush(state, move.Cell, move.Direction);
        }
        return uses;
    }

    private static PaintColor[] Targets(Board board)
    {
        var target = new PaintColor[board.CellCount];
        for (int i = 0; i < target.Length; i++) target[i] = board.TargetOf(i);
        return target;
    }

    // 글자 그림: 칸마다 4글자(줄마다 반 칸씩 어긋남). 벽 ##, 물 ~~, 코팅은 색 글자 + *
    private static string Picture(Board board, PaintColor[] colors)
    {
        var rows = new SortedDictionary<int, List<int>>();
        int minX = int.MaxValue;
        for (int i = 0; i < board.CellCount; i++)
        {
            HexCoord c = board.CoordOf(i);
            if (!rows.TryGetValue(c.R, out List<int> row)) rows[c.R] = row = new List<int>();
            row.Add(i);
            minX = System.Math.Min(minX, 2 * c.Q + c.R);
        }
        var text = new StringBuilder();
        foreach (List<int> row in rows.Values)
        {
            var line = new StringBuilder();
            foreach (int i in row.OrderBy(i => board.CoordOf(i).Q))
            {
                int column = (2 * board.CoordOf(i).Q + board.CoordOf(i).R - minX) * 2;
                while (line.Length < column) line.Append(' ');
                CellKind kind = board.KindOf(i);
                line.Append(kind == CellKind.Wall ? "##" : kind == CellKind.Water ? "~~" : Letters[(int)colors[i]] + (kind == CellKind.Coated ? "*" : " "));
            }
            text.AppendLine(line.ToString().TrimEnd());
        }
        return text.ToString();
    }

    private static string Percent(double? ratio) => ratio.HasValue ? $"{ratio.Value * 100:0}%" : "모름";
}
