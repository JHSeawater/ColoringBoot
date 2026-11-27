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

        // 저장된 번호가 int 끝이어도 다음 번호가 넘치지 않는다(2026-11-25 — 지적 M7: 전에는 음수가 돼 GameFlow의 pool[번호 % 길이]가 예외)
        [Test]
        public void Progress_AdvanceFromMaxValue_StaysInPool()
        {
            EndlessProgress read = EndlessProgress.Deserialize($"endless 1\nEasy\t{int.MaxValue}\t0\t0\n", out _);

            read.Solve("Easy", 300);
            Assert.AreEqual((int)(((long)int.MaxValue + 1) % 300), read.Current("Easy"), "번호 + 1을 묶음 길이로 나눈 나머지");
            read.Skip("Easy", 300);
            Assert.That(read.Current("Easy"), Is.InRange(0, 299));
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

        // 세 묶음 전체에서 거의 같은 퍼즐 없음 — 칸 모양이 같고 목표 색이 80% 이상 같으면 풀이가 거의 같다(2026-10-07 보통 3번 · 어려움 1번).
        // 2026-11-27(사용자): 판을 돌리거나 뒤집어서(육각 격자 대칭 12가지), 기본색을 서로 바꿔서(6가지) 겹쳐도 같은 것으로 본다 —
        // 뒤집은 꼴이 바로 다음 판에 나오고(쉬움 557 · 558번), 색만 바꾼 꼴이 난이도를 건너 다시 나왔다. 빌더(EndlessPoolBuilder.Seen)와 따로 짠 계산이다
        [Test]
        public void Pools_HaveNoNearDuplicates()
        {
            const double SimilarRatio = 0.8;
            // 앞서 본 퍼즐은 12가지로 놓은 것을 모두 넣어 두고, 새 퍼즐은 그대로 놓은 것 하나만 같은 자리의 것들과 견준다
            var byShape = new Dictionary<string, List<(string name, PaintColor[] targets)>>();
            var found = new List<string>();
            foreach (string tier in new[] { "Easy", "Normal", "Hard" })
            {
                string[] lines = TestStages.ReadEndlessPool(tier);
                for (int i = 0; i < lines.Length; i++)
                {
                    Stage stage = Stage.Parse(lines[i]);
                    string name = $"{tier} {i + 1}번";
                    string shape = Placed(stage, 0, false, out PaintColor[] targets);
                    if (byShape.TryGetValue(shape, out var others))
                    {
                        foreach (var other in others)
                        {
                            foreach (PaintColor[] swap in _colorSwaps)
                            {
                                int same = 0;
                                for (int c = 0; c < targets.Length; c++)
                                {
                                    if (swap[(int)other.targets[c]] == targets[c]) same++;
                                }
                                if ((double)same / targets.Length < SimilarRatio) continue;
                                found.Add($"{name} ≈ {other.name}");
                                break;
                            }
                        }
                    }
                    for (int turns = 0; turns < 6; turns++)
                    {
                        foreach (bool flip in new[] { false, true })
                        {
                            string key = Placed(stage, turns, flip, out PaintColor[] placed);
                            if (!byShape.TryGetValue(key, out var list)) byShape[key] = list = new List<(string, PaintColor[])>();
                            list.Add((name, placed));
                        }
                    }
                }
            }
            CollectionAssert.IsEmpty(found.Distinct().Take(30), $"거의 같은 퍼즐 {found.Distinct().Count()}쌍");
        }

        // 기본색 셋을 서로 바꾸는 6가지 — [색 값] = 바꾼 색
        private static readonly PaintColor[][] _colorSwaps = new[]
        {
            new[] { PaintColor.Red, PaintColor.Yellow, PaintColor.Blue }, new[] { PaintColor.Red, PaintColor.Blue, PaintColor.Yellow },
            new[] { PaintColor.Yellow, PaintColor.Red, PaintColor.Blue }, new[] { PaintColor.Yellow, PaintColor.Blue, PaintColor.Red },
            new[] { PaintColor.Blue, PaintColor.Red, PaintColor.Yellow }, new[] { PaintColor.Blue, PaintColor.Yellow, PaintColor.Red },
        }.Select(to => Enumerable.Range(0, (int)PaintColor.Black + 1).Select(value =>
        {
            var color = (PaintColor)value;
            return ((color & PaintColor.Red) != 0 ? to[0] : PaintColor.Empty)
                | ((color & PaintColor.Yellow) != 0 ? to[1] : PaintColor.Empty)
                | ((color & PaintColor.Blue) != 0 ? to[2] : PaintColor.Empty);
        }).ToArray()).ToArray();

        // 판을 60°씩 turns번 돌리고(flip이면 먼저 뒤집어) 놓은 것: 칸 자리 키(가장 왼쪽 · 위쪽 좌표를 0으로 당겨 좌표순으로)와 그 순서의 목표 색.
        // 돌리기는 큐브 좌표 (x, y, z) = (q, -q - r, r)에서 (x, y, z) → (-z, -x, -y), 뒤집기는 x와 z를 맞바꾼다
        private static string Placed(Stage stage, int turns, bool flip, out PaintColor[] targets)
        {
            var cells = new List<(int q, int r, PaintColor target)>();
            foreach (StageCell cell in stage.Cells)
            {
                int x = cell.Coord.Q, z = cell.Coord.R, y = -x - z;
                if (flip) (x, z) = (z, x);
                for (int t = 0; t < turns; t++) (x, y, z) = (-z, -x, -y);
                cells.Add((x, z, cell.Target));
            }
            int minQ = cells.Min(c => c.q), minR = cells.Min(c => c.r);
            var sorted = cells.Select(c => (q: c.q - minQ, r: c.r - minR, c.target)).OrderBy(c => c.q).ThenBy(c => c.r).ToList();
            targets = sorted.Select(c => c.target).ToArray();
            return string.Join(";", sorted.Select(c => $"{c.q},{c.r}"));
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

        // 팔레트 돌려 쓰기(GDD §5, 2026-11-27 사용자): 모든 난이도에서 팔레트 10가지를 5판마다 목록 순서대로 — 첫 5판은 기본(이름 없음).
        // 출시 뒤 덧붙이는 줄도 번호에 맞는 팔레트여야 하고, 이미 있는 줄의 팔레트는 바뀌지 않아야 한다
        [TestCase("Easy")]
        [TestCase("Normal")]
        [TestCase("Hard")]
        public void Pool_RotatesPalettes(string tier)
        {
            string[] order = { null, "pastel", "garden", "spring", "beach", "shore", "autumn", "dusk", "night", "village" };
            string[] lines = TestStages.ReadEndlessPool(tier);
            for (int i = 0; i < lines.Length; i++)
                Assert.AreEqual(order[i / 5 % order.Length], Stage.Parse(lines[i]).Palette, $"{tier} {i + 1}번");
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
