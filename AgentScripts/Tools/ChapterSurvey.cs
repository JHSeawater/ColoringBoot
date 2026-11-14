using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ColoringBoot.Core;
using ColoringBoot.Game;
using UnityEditor;
using UnityEngine;

// 챕터 표 (2026-11-15, 챕터 2~5 계획 3단계) — 읽기 전용. 챕터 목록 순서대로 판마다 난이도 지표를 한 표로 보여 준다
// run_script(file=AgentScripts/Tools/ChapterSurvey.cs, entry=ChapterSurvey.Table, args=[챕터 번호])
//   — 결과는 응답 텍스트와 Builds/ChapterSurvey/ChapterN.md(git이 무시). 게임 데이터는 쓰지 않는다
// 지표는 Core(Solver · StageMetrics) 그대로이고 새 지표를 만들지 않는다. 챕터끼리 비교하는 주 지표는 최소 수 · 대충 풀림,
// 순서 성공률은 7수 이하 판끼리 보는 보조 지표다(Solver.MaxOrderMoves). 값이 없는 칸은 "—"와 이유를 적고 평균에서 뺀다(뺀 판 수 표시).
// 대충 풀림이 상태 초과로 비면 그 판만 상한을 키워 다시 잰다(Core 상한은 바꾸지 않는다)
public static class ChapterSurvey
{
    private const int RetryLimit = 2000000;
    private const string OutFolder = "Builds/ChapterSurvey";

    public static string Table(int chapter)
    {
        string folder = $"Assets/Data/Chapters/Chapter{chapter}";
        var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>($"{folder}/StageCatalog.asset");
        if (catalog == null) return $"목록 없음: {folder}/StageCatalog.asset";
        if (catalog.Stages.Count == 0) return $"챕터 {chapter} 목록이 비어 있음";

        var sb = new StringBuilder();
        sb.AppendLine($"# 챕터 {chapter} 표 ({catalog.Stages.Count}판)");
        sb.AppendLine();
        sb.AppendLine("| # | 파일 | 이름 | 칸 | 팔레트 | 최소 수 | 순서 성공률 | 막힐 칸 | 조용한 막다른 길 | 대충 풀림 | 닿는 상태 |");
        sb.AppendLine("|---|---|---|---:|---|---:|---:|---:|---:|---:|---:|");
        var moves = new List<double>();
        var orders = new List<double>();
        var traps = new List<double>();
        var silents = new List<double>();
        var chances = new List<double>();
        var cells = new List<double>();
        var notes = new List<string>();
        int n = 0;
        foreach (TextAsset asset in catalog.Stages)
        {
            n++;
            if (asset == null)
            {
                sb.AppendLine($"| {n} | (빈 칸) | | | | | | | | | |");
                continue;
            }
            Stage stage = Stage.Parse(asset.text);
            var board = new Board(stage);
            SolveResult solve = Solver.Solve(board, board.CreateStartState());
            string minText = solve.Solved ? solve.Path.Count.ToString() : solve.Limited ? "— 상한" : "— 풀이 없음";
            if (solve.Solved) moves.Add(solve.Path.Count);
            if (solve.Solved && stage.MinMoves.HasValue && stage.MinMoves.Value != solve.Path.Count)
                notes.Add($"{asset.name}: 파일 minMoves {stage.MinMoves.Value} ≠ 솔버 {solve.Path.Count}");
            if (solve.Solved && !stage.MinMoves.HasValue) notes.Add($"{asset.name}: 파일에 minMoves 없음");

            string orderText = "— 풀이 없음";
            if (solve.Solved)
            {
                OrderSensitivity? order = Solver.MeasureOrder(board, board.CreateStartState(), solve.Path);
                if (order.HasValue)
                {
                    double ratio = (double)order.Value.Succeeded / order.Value.Total;
                    orders.Add(ratio);
                    orderText = $"{order.Value.Succeeded}/{order.Value.Total} ({Percent(ratio)})";
                }
                else orderText = solve.Path.Count > Solver.MaxOrderMoves ? $"— {solve.Path.Count}수" : "— 1수 이하";
            }

            StageMetrics metrics = StageMetrics.Measure(board);
            bool retried = false;
            if (!metrics.Complete)
            {
                metrics = StageMetrics.Measure(board, RetryLimit);
                retried = true;
            }
            traps.Add(metrics.TrapRatio);
            cells.Add(metrics.Cells);
            string silentText = "— 상태 초과";
            string chanceText = "— 상태 초과";
            string statesText = $"> {RetryLimit}";
            if (metrics.Complete)
            {
                statesText = metrics.States + (retried ? " (상한 키움)" : "");
                if (metrics.SilentRatio.HasValue)
                {
                    silents.Add(metrics.SilentRatio.Value);
                    silentText = Percent(metrics.SilentRatio.Value);
                }
                if (metrics.SolveChance.HasValue)
                {
                    chances.Add(metrics.SolveChance.Value);
                    chanceText = Percent(metrics.SolveChance.Value);
                }
            }
            sb.AppendLine($"| {n} | {asset.name} | {stage.Name} | {metrics.Cells} | {stage.Palette ?? "기본"} | {minText} | {orderText} | {Percent(metrics.TrapRatio)} | {silentText} | {chanceText} | {statesText} |");
        }
        int total = catalog.Stages.Count;
        sb.AppendLine($"| | **평균** | | {Mean(cells, total, "0.0")} | | {Mean(moves, total, "0.00")} | {MeanPercent(orders, total)} | {MeanPercent(traps, total)} | {MeanPercent(silents, total)} | {MeanPercent(chances, total)} | |");
        sb.AppendLine();
        sb.AppendLine("평균의 \"(n판 뺌)\"은 값이 없어 평균에서 뺀 판 수다. 순서 성공률은 8수 이상이면 재지 않는다. 대충 풀림 = 막히지 않는 획만 무작위로 그어 풀릴 확률.");
        foreach (string note in notes) sb.AppendLine("- 확인: " + note);

        Directory.CreateDirectory(OutFolder);
        File.WriteAllText($"{OutFolder}/Chapter{chapter}.md", sb.ToString(), new UTF8Encoding(false));
        return sb.ToString();
    }

    private static string Percent(double ratio) => (ratio * 100).ToString("0") + "%";

    private static string Mean(List<double> values, int total, string format) =>
        values.Count == 0 ? "—" : values.Average().ToString(format) + (values.Count < total ? $" ({total - values.Count}판 뺌)" : "");

    private static string MeanPercent(List<double> values, int total) =>
        values.Count == 0 ? "—" : Percent(values.Average()) + (values.Count < total ? $" ({total - values.Count}판 뺌)" : "");
}
