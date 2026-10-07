using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ColoringBoot.Core.Tests
{
    // 무한 모드 (GDD §5, 2026-10-07) — 진행 기록 · 기록 합계 · 퍼즐 묶음(Assets/Data/Endless/*.txt — 한 줄에 스테이지 코드 하나)
    public class EndlessTests
    {
        [Test]
        public void Progress_AdvancesAndWraps()
        {
            var progress = new EndlessProgress();
            Assert.AreEqual(0, progress.Current("Easy"));

            progress.Solve("Easy", 3);
            progress.Skip("Easy", 3);
            Assert.AreEqual(2, progress.Current("Easy"));
            progress.Solve("Easy", 3);

            Assert.AreEqual(0, progress.Current("Easy"), "끝까지 가면 처음으로");
            Assert.AreEqual(2, progress.Solved("Easy"));
            Assert.AreEqual(1, progress.Skipped("Easy"));
            Assert.AreEqual(0, progress.Current("Hard"), "난이도마다 따로");
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.Solve("Hard", 0));
        }

        [Test]
        public void Progress_RoundTrip_AndBrokenText()
        {
            var progress = new EndlessProgress();
            progress.Solve("Normal", 300);
            progress.Skip("Normal", 300);
            progress.Solve("Hard", 300);

            EndlessProgress read = EndlessProgress.Deserialize(progress.Serialize());
            Assert.AreEqual(2, read.Current("Normal"));
            Assert.AreEqual(1, read.Solved("Normal"));
            Assert.AreEqual(1, read.Skipped("Normal"));
            Assert.AreEqual(1, read.Solved("Hard"));

            Assert.AreEqual(0, EndlessProgress.Deserialize("garbage").Current("Normal"));
            Assert.AreEqual(5, EndlessProgress.Deserialize("endless 1\nEasy\t5\t4\t1\nbroken line\nHard\t-1\t0\t0\n").Current("Easy"));
        }

        [Test]
        public void PlayStats_SumByPrefix()
        {
            var stats = new PlayStats();
            stats.Opened("EndlessEasy0000");
            stats.Stroked("EndlessEasy0000");
            stats.Hinted("EndlessEasy0001");
            stats.AddTime("EndlessEasy0001", 2.5);
            stats.Stroked("EndlessHard0000");
            stats.Stroked("Grape");

            StageStats easy = stats.Sum("EndlessEasy");
            Assert.AreEqual(1, easy.Opens);
            Assert.AreEqual(1, easy.Strokes);
            Assert.AreEqual(1, easy.Hints);
            Assert.AreEqual(2.5, easy.Seconds, 1e-9);
            Assert.AreEqual(1, stats.Sum("EndlessHard").Strokes);
            Assert.IsNull(easy.ClearMoves);
        }

        // 세 묶음 전체에서 거의 같은 퍼즐 없음 — 칸 모양이 같고 목표 색이 80% 이상 같으면 풀이가 거의 같다(2026-10-07 보통 3번 · 어려움 1번)
        [Test]
        public void Pools_HaveNoNearDuplicates()
        {
            var byShape = new Dictionary<string, List<(string name, Dictionary<HexCoord, PaintColor> targets)>>();
            var found = new List<string>();
            foreach (string tier in new[] { "Easy", "Normal", "Hard" })
            {
                string[] lines = File.ReadAllLines(Path.Combine(Application.dataPath, "Data/Endless", tier + ".txt")).Where(l => l.Length > 0).ToArray();
                for (int i = 0; i < lines.Length; i++)
                {
                    Stage stage = Stage.Parse(lines[i]);
                    string shape = string.Join(";", stage.Cells.Select(c => $"{c.Coord.Q},{c.Coord.R}").OrderBy(s => s, StringComparer.Ordinal));
                    Dictionary<HexCoord, PaintColor> targets = stage.Cells.ToDictionary(c => c.Coord, c => c.Target);
                    if (!byShape.TryGetValue(shape, out var list)) byShape[shape] = list = new List<(string, Dictionary<HexCoord, PaintColor>)>();
                    foreach (var other in list)
                    {
                        int same = targets.Count(cell => other.targets[cell.Key] == cell.Value);
                        if ((double)same / targets.Count >= 0.8) found.Add($"{tier} {i + 1}번 ≈ {other.name}");
                    }
                    list.Add(($"{tier} {i + 1}번", targets));
                }
            }
            CollectionAssert.IsEmpty(found, "거의 같은 퍼즐");
        }

        // 묶음: 모든 줄이 읽히고 · 풀리고 · minMoves = 솔버 최소 수 · 난이도 범위 안 · 같은 퍼즐 없음
        [TestCase("Easy", 3, 4)]
        [TestCase("Normal", 5, 6)]
        [TestCase("Hard", 6, 8)]
        public void Pool_PuzzlesSolveInRange(string tier, int minMoves, int maxMoves)
        {
            string[] lines = File.ReadAllLines(Path.Combine(Application.dataPath, "Data/Endless", tier + ".txt")).Where(l => l.Length > 0).ToArray();
            Assert.Greater(lines.Length, 0, "퍼즐이 있음");
            Assert.AreEqual(lines.Length, lines.Distinct().Count(), "같은 퍼즐 없음");
            foreach (string line in lines)
            {
                Stage stage = Stage.Parse(line);
                var board = new Board(stage);
                SolveResult result = Solver.Solve(board, board.CreateStartState());
                Assert.IsTrue(result.Solved, line);
                Assert.AreEqual(stage.MinMoves, result.Path.Count, line);
                Assert.That(result.Path.Count, Is.InRange(minMoves, maxMoves), line);
            }
        }
    }
}
