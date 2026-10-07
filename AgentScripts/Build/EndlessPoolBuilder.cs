using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ColoringBoot.Core;
using UnityEditor;

// 무한 모드 퍼즐 묶음 (GDD §5 · §8, 2026-10-07) — 생성기 조건으로 만들고 난이도별 임시 기준으로 거른다(플레이테스트 기록으로 고친다).
// 결과: Assets/Data/Endless/{Easy,Normal,Hard}.txt — 한 줄에 스테이지 코드(JSON) 하나, 이미 섞인 순서(게임은 앞에서부터 차례로 낸다).
// run_script(file=AgentScripts/Build/EndlessPoolBuilder.cs, entry=EndlessPoolBuilder.Build, args=["Easy", 300, 1]) — 같은 시드면 같은 묶음. 다시 만들면 덮어쓴다
public static class EndlessPoolBuilder
{
    private const string Folder = "Assets/Data/Endless";
    private const double MinMixedRatio = 0.3;     // 모든 난이도 — 섞기가 있는 퍼즐
    private const int MaxSeedsPerPuzzle = 5000;   // 한 퍼즐을 찾는 생성기 호출 상한(조건이 너무 좁으면 멈춘다)

    // 난이도별 임시 기준 (2026-10-07 계획 승인): 이름 · 최소 수 범위 · 막힐 칸 비율 하한 · 순서 성공 비율 상한 · 시작 색 칸 · 검정 허용
    private sealed class Tier
    {
        public string Name;
        public int MinMoves, MaxMoves;
        public double MinTrap, MaxOrder;
        public int MinSeeds, MaxSeeds;
        public bool AllowBlack;
    }

    private static readonly Dictionary<string, Tier> _tiers = new Dictionary<string, Tier>
    {
        ["Easy"] = new Tier { Name = "쉬움", MinMoves = 3, MaxMoves = 4, MinTrap = 0.2, MaxOrder = 1.0, MinSeeds = 3, MaxSeeds = 3, AllowBlack = false },
        ["Normal"] = new Tier { Name = "보통", MinMoves = 5, MaxMoves = 6, MinTrap = 0.4, MaxOrder = 0.34, MinSeeds = 3, MaxSeeds = 4, AllowBlack = true },
        ["Hard"] = new Tier { Name = "어려움", MinMoves = 6, MaxMoves = 8, MinTrap = 0.5, MaxOrder = 0.2, MinSeeds = 3, MaxSeeds = 4, AllowBlack = true },
    };

    public static string Build(string tier, int count, int seed)
    {
        if (!_tiers.TryGetValue(tier, out Tier t)) return $"난이도 없음: {tier} (Easy · Normal · Hard)";
        var watch = Stopwatch.StartNew();
        var random = new SeededRandom((uint)seed);
        var seen = new HashSet<string>();
        var lines = new List<string>();
        int calls = 0, duplicates = 0, outOfRange = 0;
        while (lines.Count < count)
        {
            var options = new GeneratorOptions
            {
                Shape = BoardShape.Random,
                Seeds = t.MinSeeds + random.Next(t.MaxSeeds - t.MinSeeds + 1),
                Moves = t.MinMoves + random.Next(t.MaxMoves - t.MinMoves + 1),
                MinSolve = t.MinMoves,
                AllowBlack = t.AllowBlack,
                MaxOrderRatio = t.MaxOrder,
                MinMixedRatio = MinMixedRatio,
                MinTrapRatio = t.MinTrap,
            };
            GeneratedStage generated = null;
            for (int i = 0; i < MaxSeedsPerPuzzle && generated == null; i++, calls++) generated = Generator.Generate(options, random, t.Name);
            if (generated == null) return $"{tier}: 조건에 맞는 퍼즐을 {MaxSeedsPerPuzzle}번 안에 못 찾음 — 기준을 넓혀야 함 ({lines.Count}개에서 멈춤)";
            if (generated.Stage.MinMoves > t.MaxMoves)
            {
                outOfRange++;
                continue;
            }
            string line = StageWriter.ToJson(generated.Stage);
            if (!seen.Add(CellsKey(generated.Stage)))
            {
                duplicates++;
                continue;
            }
            lines.Add(line);
        }

        Directory.CreateDirectory(Folder);
        string path = $"{Folder}/{tier}.txt";
        File.WriteAllText(path, string.Join("\n", lines) + "\n", new UTF8Encoding(false));
        AssetDatabase.ImportAsset(path);
        var counts = new int[t.MaxMoves + 1];
        foreach (string line in lines) counts[Stage.Parse(line).MinMoves.Value]++;
        var histogram = new StringBuilder();
        for (int m = t.MinMoves; m <= t.MaxMoves; m++) histogram.Append($" {m}수 {counts[m]}");
        return $"{path}: {lines.Count}개({histogram.ToString().Trim()}) · 생성기 호출 {calls} · 겹침 {duplicates} · 범위 밖 {outOfRange} · {watch.Elapsed.TotalSeconds:F0}초 · {new FileInfo(path).Length:N0}바이트";
    }

    // 같은 퍼즐 거르기 — 칸 · 시작 색 · 목표 색이 모두 같으면 같은 퍼즐
    private static string CellsKey(Stage stage)
    {
        var key = new StringBuilder();
        foreach (StageCell c in stage.Cells) key.Append(c.Coord.Q).Append(',').Append(c.Coord.R).Append(',').Append((int)c.Start).Append(',').Append((int)c.Target).Append(';');
        return key.ToString();
    }
}
