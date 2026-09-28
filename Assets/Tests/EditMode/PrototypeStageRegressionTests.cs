using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ColoringBoot.Core.Tests
{
    // 솔버 회귀 — 프로토타입 스테이지 9개의 최소 수 · 순서 민감도 · 탐색 상태 수가 프로토타입 엔진 값과 같다
    // (Prototype.html 스크립트를 Node로 실행한 기준값 — DevelopLog 2026-09-28). 탐색 수까지 같으면 탐색 순서가 같다는 뜻
    public class PrototypeStageRegressionTests
    {
        [TestCase("Grape", 10, 5, 8, 120, 67)]
        [TestCase("TwoColors", 15, 3, 1, 6, 18)]
        [TestCase("BrushChanges", 16, 3, 1, 6, 6)]
        [TestCase("Honeycomb", 19, 4, 2, 24, 22)]
        [TestCase("Crossing", 19, 5, 8, 120, 250)]
        [TestCase("Stain", 14, 5, 3, 120, 31)]
        [TestCase("MakeBlack", 19, 5, 2, 120, 34)]
        [TestCase("Hive", 37, 6, 20, 720, 430)]
        [TestCase("LastStroke", 37, 7, 100, 5040, 182)]
        public void MatchesPrototypeEngine(string file, int cells, int minMoves, int orderSucceeded, int orderTotal, int explored)
        {
            Stage stage = Stage.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Data/Stages", file + ".json")));
            var board = new Board(stage);
            PaintColor[] start = board.CreateStartState();

            SolveResult result = Solver.Solve(board, start);

            Assert.AreEqual(cells, board.CellCount, "칸 수");
            Assert.IsTrue(result.Solved, "풀림");
            Assert.AreEqual(minMoves, result.Path.Count, "최소 수");
            Assert.AreEqual(minMoves, stage.MinMoves, "JSON minMoves");
            Assert.AreEqual(explored, result.Explored, "탐색 상태 수");
            OrderSensitivity order = Solver.MeasureOrder(board, start, result.Path).Value;
            Assert.AreEqual(orderSucceeded, order.Succeeded, "순서 성공");
            Assert.AreEqual(orderTotal, order.Total, "순서 전체");
        }
    }
}
