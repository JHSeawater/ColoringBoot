using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

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

            EndlessProgress read = EndlessProgress.Deserialize(progress.Serialize(), out _);
            Assert.AreEqual(2, read.Current("Normal"));
            Assert.AreEqual(1, read.Solved("Normal"));
            Assert.AreEqual(1, read.Skipped("Normal"));
            Assert.AreEqual(1, read.Solved("Hard"));

            Assert.AreEqual(0, EndlessProgress.Deserialize("garbage", out _).Current("Normal"));
            Assert.AreEqual(5, EndlessProgress.Deserialize("endless 1\nEasy\t5\t4\t1\nbroken line\nHard\t-1\t0\t0\n", out _).Current("Easy"));
        }

        // 읽은 상태 (2026-11-19) — ProgressTests의 같은 테스트 참고
        [TestCase(null)]
        [TestCase("")]
        [TestCase("endless 1\n")]
        [TestCase("endless 1\r\nEasy\t5\t4\t1\r\n\r\n  \nHard\t1\t1\t0\n")]
        public void Progress_Status_OkForEmptyAndNormalText(string text)
        {
            EndlessProgress.Deserialize(text, out RecordReadStatus status);
            Assert.AreEqual(RecordReadStatus.Ok, status);
        }

        [Test]
        public void Progress_Status_OkForSerializedText()
        {
            var progress = new EndlessProgress();
            progress.Solve("Normal", 300);
            progress.Skip("Hard", 300);

            EndlessProgress.Deserialize(progress.Serialize(), out RecordReadStatus status);

            Assert.AreEqual(RecordReadStatus.Ok, status, "끝의 빈 줄은 버림이 아님");
        }

        [Test]
        public void Progress_Status_LinesDropped_KeepsReadLines()
        {
            EndlessProgress read = EndlessProgress.Deserialize("endless 1\nEasy\t5\t4\t1\nbroken line\nHard\t-1\t0\t0\n", out RecordReadStatus status);

            Assert.AreEqual(RecordReadStatus.LinesDropped, status);
            Assert.AreEqual(5, read.Current("Easy"));
            Assert.AreEqual(0, read.Current("Hard"));
        }

        [TestCase("endless 2\nEasy\t5\t4\t1\n", RecordReadStatus.NewerVersion)]
        [TestCase("garbage", RecordReadStatus.Unreadable)]
        [TestCase("endless x\nEasy\t5\t4\t1\n", RecordReadStatus.Unreadable)]
        [TestCase("progress 1\nEasy\t5\n", RecordReadStatus.Unreadable)]
        public void Progress_Status_UnknownHeader_GivesEmptyProgress(string text, RecordReadStatus expected)
        {
            EndlessProgress read = EndlessProgress.Deserialize(text, out RecordReadStatus status);

            Assert.AreEqual(expected, status);
            Assert.AreEqual(0, read.Current("Easy"));
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
                string[] lines = TestStages.ReadEndlessPool(tier);
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

        // 묶음의 품질 조건(GDD §5 · §8, 2026-11-25 — 지적 M11): 섞인 색 비율(칠해지는 목표 칸 중) ≥ 0.3 · 막힐 수 있는 칸 비율(모든 칸 중) ≥ 난이도 하한.
        // 값과 계산은 AgentScripts/Build/EndlessPoolBuilder.cs → Generator.MeetsConditions와 같다 — 묶음을 다시 만들 때 조건이 빠지면 여기서 걸린다
        [TestCase("Easy", 0.2)]
        [TestCase("Normal", 0.4)]
        [TestCase("Hard", 0.5)]
        public void Pool_MeetsQualityConditions(string tier, double minTrapRatio)
        {
            const double MinMixedRatio = 0.3;
            foreach (string line in TestStages.ReadEndlessPool(tier))
            {
                Stage stage = Stage.Parse(line);
                var start = new PaintColor[stage.Cells.Count];
                var target = new PaintColor[stage.Cells.Count];
                for (int i = 0; i < start.Length; i++)
                {
                    start[i] = stage.Cells[i].Start;
                    target[i] = stage.Cells[i].Target;
                }
                int mixed = StageMetrics.CountMixedCells(target, out int painted);
                Assert.That(painted > 0 && (double)mixed / painted >= MinMixedRatio, $"섞인 색 {mixed} / {painted}: {line}");
                int trap = StageMetrics.CountTrapCells(start, target);
                Assert.That((double)trap / target.Length >= minTrapRatio, $"막힐 수 있는 칸 {trap} / {target.Length}: {line}");
            }
        }

        // 묶음: 모든 줄이 읽히고 · 풀리고 · minMoves = 솔버 최소 수 · 난이도 범위 안 · 같은 퍼즐 없음
        [TestCase("Easy", 3, 4)]
        [TestCase("Normal", 5, 6)]
        [TestCase("Hard", 6, 8)]
        public void Pool_PuzzlesSolveInRange(string tier, int minMoves, int maxMoves)
        {
            string[] lines = TestStages.ReadEndlessPool(tier);
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
