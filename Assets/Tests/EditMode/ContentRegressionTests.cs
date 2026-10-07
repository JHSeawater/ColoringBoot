using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ColoringBoot.Core.Tests
{
    // 콘텐츠 회귀 — 게임에 들어가는 모든 스테이지(Assets/Data 아래 JSON: 챕터 · 따라 하기 · 시험 목록)가
    // 읽히고 · 풀리고 · minMoves가 맞고 · 미리보기 경로(Trace)가 실제 붓질(Brush)과 같고 · 파일 이름(진행 기록의 키)이 겹치지 않는다 (2026-10-07)
    public class ContentRegressionTests
    {
        private static string DataPath => Path.Combine(Application.dataPath, "Data");

        // 테스트 이름에 보이도록 Data 기준 상대 경로
        private static IEnumerable<string> StageFiles() =>
            Directory.GetFiles(DataPath, "*.json", SearchOption.AllDirectories)
                .Select(path => path.Substring(DataPath.Length + 1).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal);

        private static Stage Read(string file) => Stage.Parse(File.ReadAllText(Path.Combine(DataPath, file)));

        [TestCaseSource(nameof(StageFiles))]
        public void Stage_SolvesInMinMoves(string file)
        {
            Stage stage = Read(file);
            var board = new Board(stage);

            SolveResult result = Solver.Solve(board, board.CreateStartState());

            Assert.IsTrue(result.Solved, "풀림");
            Assert.IsTrue(stage.MinMoves.HasValue, "JSON에 minMoves가 있음");
            Assert.AreEqual(stage.MinMoves.Value, result.Path.Count, "minMoves = 솔버 최소 수");
        }

        // 시작 상태와 풀이의 매 수 뒤 상태에서, 모든 칸 · 모든 방향의 미리보기 결과가 실제 붓질 결과와 같다
        [TestCaseSource(nameof(StageFiles))]
        public void Trace_MatchesBrush(string file)
        {
            var board = new Board(Read(file));
            var cells = new int[board.CellCount];
            var brushes = new PaintColor[board.CellCount];
            PaintColor[] state = board.CreateStartState();
            SolveResult result = Solver.Solve(board, state);
            Assert.IsTrue(result.Solved, "풀림");

            for (int step = 0; step <= result.Path.Count; step++)
            {
                for (int cell = 0; cell < board.CellCount; cell++)
                {
                    for (int d = 0; d < 6; d++)
                    {
                        int count = board.Trace(state, cell, (HexDirection)d, cells, brushes);
                        var predicted = (PaintColor[])state.Clone();
                        for (int k = 0; k < count; k++)
                        {
                            if (brushes[k] != PaintColor.Empty) predicted[cells[k]] = brushes[k];
                        }
                        var brushed = (PaintColor[])state.Clone();
                        board.Brush(brushed, cell, (HexDirection)d);
                        CollectionAssert.AreEqual(predicted, brushed, $"{step}수 뒤 칸 {cell}, {(HexDirection)d}");
                    }
                }
                if (step < result.Path.Count) board.Brush(state, result.Path[step].Cell, result.Path[step].Direction);
            }
        }

        // 진행 · 플레이 기록은 파일 이름으로 스테이지를 구분한다 — 목록(챕터 · 따라 하기 · 시험)이 달라도 겹치면 기록이 섞인다
        [Test]
        public void FileNames_AreUnique()
        {
            string[] names = StageFiles().Select(Path.GetFileNameWithoutExtension).ToArray();
            string[] duplicates = names.GroupBy(n => n, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();

            Assert.Greater(names.Length, 0, "스테이지 파일이 있음");
            CollectionAssert.IsEmpty(duplicates, "겹치는 파일 이름");
        }
    }
}
