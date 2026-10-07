using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ColoringBoot.Core;
using UnityEditor;

// 무한 모드 퍼즐 묶음 (GDD §5 · §8, 2026-10-07) — 생성기 조건으로 만들고 난이도별 임시 기준으로 거른다(플레이테스트 기록으로 고친다).
// 결과: Assets/Data/Endless/{Easy,Normal,Hard}.txt — 한 줄에 스테이지 코드(JSON) 하나, 이미 섞인 순서(게임은 앞에서부터 차례로 낸다).
// run_script(file=AgentScripts/Build/EndlessPoolBuilder.cs, entry=EndlessPoolBuilder.Build, args=["Easy", 300, 1]) — 같은 시드면 같은 묶음. 다시 만들면 덮어쓴다.
// 난이도마다 다른 시드를 쓴다(지금 쉬움 1 · 보통 2 · 어려움 3) — 같은 시드면 무작위 순서가 같아 난이도끼리 비슷한 퍼즐이 나온다(2026-10-07)
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

    // 거의 같은 퍼즐 거르기 (2026-10-07 — 보통 3번 · 어려움 1번처럼 모양이 같고 목표가 한두 칸만 달라 풀이가 거의 같았음):
    // 칸 모양이 같고 목표 색이 SimilarRatio 이상 같으면 같은 퍼즐로 본다. 다른 난이도 묶음(파일이 있으면)과 이미 고른 퍼즐 모두와 비교한다
    private const double SimilarRatio = 0.8;

    private sealed class Seen
    {
        private readonly Dictionary<string, List<Dictionary<HexCoord, PaintColor>>> _byShape = new Dictionary<string, List<Dictionary<HexCoord, PaintColor>>>();

        public bool AddIfNew(Stage stage)
        {
            string shape = ShapeKey(stage);
            var targets = new Dictionary<HexCoord, PaintColor>();
            foreach (StageCell c in stage.Cells) targets[c.Coord] = c.Target;
            if (!_byShape.TryGetValue(shape, out List<Dictionary<HexCoord, PaintColor>> list))
            {
                list = new List<Dictionary<HexCoord, PaintColor>>();
                _byShape.Add(shape, list);
            }
            foreach (Dictionary<HexCoord, PaintColor> other in list)
            {
                int same = 0;
                foreach (KeyValuePair<HexCoord, PaintColor> cell in targets)
                {
                    if (other[cell.Key] == cell.Value) same++;
                }
                if ((double)same / targets.Count >= SimilarRatio) return false;
            }
            list.Add(targets);
            return true;
        }

        // 칸 좌표 집합(순서 무관)
        private static string ShapeKey(Stage stage)
        {
            var coords = new List<string>();
            foreach (StageCell c in stage.Cells) coords.Add($"{c.Coord.Q},{c.Coord.R}");
            coords.Sort(StringComparer.Ordinal);
            return string.Join(";", coords);
        }
    }

    public static string Build(string tier, int count, int seed)
    {
        if (!_tiers.TryGetValue(tier, out Tier t)) return $"난이도 없음: {tier} (Easy · Normal · Hard)";
        var watch = Stopwatch.StartNew();
        var random = new SeededRandom((uint)seed);
        // 다른 난이도 묶음의 퍼즐을 먼저 넣어 두고 비교한다
        var seen = new Seen();
        foreach (string other in _tiers.Keys)
        {
            string otherPath = $"{Folder}/{other}.txt";
            if (other == tier || !File.Exists(otherPath)) continue;
            foreach (string line in File.ReadAllLines(otherPath)) if (line.Trim().Length > 0) seen.AddIfNew(Stage.Parse(line));
        }
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
            if (!seen.AddIfNew(generated.Stage))
            {
                duplicates++;
                continue;
            }
            lines.Add(StageWriter.ToJson(generated.Stage));
        }

        Directory.CreateDirectory(Folder);
        string path = $"{Folder}/{tier}.txt";
        File.WriteAllText(path, string.Join("\n", lines) + "\n", new UTF8Encoding(false));
        AssetDatabase.ImportAsset(path);
        var counts = new int[t.MaxMoves + 1];
        foreach (string line in lines) counts[Stage.Parse(line).MinMoves.Value]++;
        var histogram = new StringBuilder();
        for (int m = t.MinMoves; m <= t.MaxMoves; m++) histogram.Append($" {m}수 {counts[m]}");
        return $"{path}: {lines.Count}개({histogram.ToString().Trim()}) · 생성기 호출 {calls} · 거의 같아 거름 {duplicates} · 범위 밖 {outOfRange} · {watch.Elapsed.TotalSeconds:F0}초 · {new FileInfo(path).Length:N0}바이트";
    }
}
