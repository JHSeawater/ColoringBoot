using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ColoringBoot.Core;
using UnityEditor;

// 무한 모드 퍼즐 묶음 (GDD §5 · §8, 2026-10-07) — 생성기 조건으로 만들고 난이도별 기준으로 거른다(2026-11-27 확정 — 임시 기준 그대로, 난이도마다 1,000개).
// 결과: Assets/Data/Endless/{Easy,Normal,Hard}.txt — 한 줄에 스테이지 코드(JSON) 하나, 이미 섞인 순서(게임은 앞에서부터 차례로 낸다).
// run_script(file=AgentScripts/Build/EndlessPoolBuilder.cs, entry=EndlessPoolBuilder.Build, args=["Easy", 1000, 1]) — 같은 시드면 같은 묶음. 다시 만들면 덮어쓴다.
// 지금 묶음(2026-11-27, 넓힌 거르기)은 보통 · 어려움 파일을 비운 뒤 쉬움 → 보통 → 어려움 순서로 만들었다(다른 난이도 파일과 비교해 거르므로 순서 · 남은 파일에 따라 결과가 달라진다).
// 출시 뒤에는 다시 만들지 않는다 — 파일 끝에 덧붙이기만(Docs/TDD.md §4)
// 난이도마다 다른 시드를 쓴다(지금 쉬움 1 · 보통 2 · 어려움 3) — 같은 시드면 무작위 순서가 같아 난이도끼리 비슷한 퍼즐이 나온다(2026-10-07)
public static class EndlessPoolBuilder
{
    private const string Folder = "Assets/Data/Endless";
    private const double MinMixedRatio = 0.3;     // 모든 난이도 — 섞기가 있는 퍼즐
    private const int MaxSeedsPerPuzzle = 5000;   // 한 퍼즐을 찾는 생성기 호출 상한(조건이 너무 좁으면 멈춘다)

    // 난이도별 기준 (2026-10-07 임시 → 2026-11-27 그대로 확정, 사용자): 이름 · 최소 수 범위 · 막힐 칸 비율 하한 · 순서 성공 비율 상한 · 시작 색 칸 · 검정 허용
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
    // 칸 모양이 같고 목표 색이 SimilarRatio 이상 같으면 같은 퍼즐로 본다. 다른 난이도 묶음(파일이 있으면)과 이미 고른 퍼즐 모두와 비교한다.
    // 2026-11-27(사용자 — 돌리거나 뒤집은 꼴 · 색만 바꾼 꼴이 난이도 안팎에서 다시 나옴): 판을 돌리거나 뒤집어서(육각 격자 대칭 12가지),
    // 기본색을 서로 바꿔서(6가지) 겹쳤을 때도 같은 기준으로 거른다. EndlessTests.Pools_HaveNoNearDuplicates가 같은 기준으로 본다
    private const double SimilarRatio = 0.8;

    private sealed class Seen
    {
        private const int Symmetries = 12;   // 60°씩 돌리기 6 × 뒤집기 2
        private static readonly int[][] _colorSwaps = ColorSwaps();
        // 기준 모양(대칭 12가지로 놓아 본 것 중 키가 가장 앞서는 것) → 그 모양으로 놓은 목표 색 배열들
        private readonly Dictionary<string, List<int[]>> _byShape = new Dictionary<string, List<int[]>>();

        public bool AddIfNew(Stage stage)
        {
            var keys = new string[Symmetries];
            var targets = new int[Symmetries][];
            string shape = null;
            for (int s = 0; s < Symmetries; s++)
            {
                keys[s] = Place(stage, s, out targets[s]);
                if (shape == null || string.CompareOrdinal(keys[s], shape) < 0) shape = keys[s];
            }
            if (!_byShape.TryGetValue(shape, out List<int[]> list))
            {
                list = new List<int[]>();
                _byShape.Add(shape, list);
            }
            // 기준 모양으로 놓이는 방법은 판 자체의 대칭 수만큼 있다(삼각형 6 · 마름모 4 · 작은 육각 12) — 그 전부를 이미 고른 퍼즐과 겹쳐 본다
            int first = -1;
            for (int s = 0; s < Symmetries; s++)
            {
                if (keys[s] != shape) continue;
                if (first < 0) first = s;
                foreach (int[] other in list)
                {
                    foreach (int[] swap in _colorSwaps)
                    {
                        int same = 0;
                        for (int c = 0; c < other.Length; c++)
                        {
                            if (swap[targets[s][c]] == other[c]) same++;
                        }
                        if ((double)same / other.Length >= SimilarRatio) return false;
                    }
                }
            }
            list.Add(targets[first]);
            return true;
        }

        // 대칭 symmetry번으로 놓은 판: 칸 좌표 키(맨 앞 칸을 원점으로 옮겨 좌표순으로)와 그 순서의 목표 색
        private static string Place(Stage stage, int symmetry, out int[] targets)
        {
            int count = stage.Cells.Count;
            var placed = new HexCoord[count];
            var order = new int[count];
            for (int i = 0; i < count; i++)
            {
                int q = stage.Cells[i].Coord.Q, r = stage.Cells[i].Coord.R;
                if (symmetry >= 6) (q, r) = (r, q);                              // 뒤집기
                for (int turn = 0; turn < symmetry % 6; turn++) (q, r) = (-r, q + r); // 60° 돌리기
                placed[i] = new HexCoord(q, r);
                order[i] = i;
            }
            Array.Sort(order, (a, b) => placed[a].Q != placed[b].Q ? placed[a].Q.CompareTo(placed[b].Q) : placed[a].R.CompareTo(placed[b].R));
            HexCoord origin = placed[order[0]];
            var key = new StringBuilder();
            targets = new int[count];
            for (int i = 0; i < count; i++)
            {
                key.Append(placed[order[i]].Q - origin.Q).Append(',').Append(placed[order[i]].R - origin.R).Append(';');
                targets[i] = (int)stage.Cells[order[i]].Target;
            }
            return key.ToString();
        }

        // 기본색 세 가지를 서로 바꾸는 6가지 — [바꾸는 방법][색 값] = 바꾼 색 값(섞인 색은 비트마다 옮긴다)
        private static int[][] ColorSwaps()
        {
            int[][] orders = { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 }, new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };
            var swaps = new int[orders.Length][];
            for (int o = 0; o < orders.Length; o++)
            {
                swaps[o] = new int[(int)PaintColor.Black + 1];
                for (int color = 0; color < swaps[o].Length; color++)
                {
                    for (int bit = 0; bit < 3; bit++)
                    {
                        if ((color & (1 << bit)) != 0) swaps[o][color] |= 1 << orders[o][bit];
                    }
                }
            }
            return swaps;
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
