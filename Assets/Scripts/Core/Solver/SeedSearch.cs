using System;
using System.Collections.Generic;

namespace ColoringBoot.Core
{
    // 그림 맵 시작 칸 찾기 조건 (GDD §5 · §8, 2026-10-07)
    public sealed class SeedSearchOptions
    {
        public int Seeds = 3;                 // 시작 색 칸 수
        public int MinMoves = 4;              // 최소 수 범위
        public int MaxMoves = 6;
        public bool MixedSeeds;               // 시작 색에 섞인 색도 쓴다(그 칸 목표 색에 들어 있는 색만 — 예: 검정 줄무늬에 보라)
        public double MaxOrderRatio = 1.0;    // 순서 민감도(성공 비율) 상한 — 1이면 조건 없음
    }

    // 그림 맵: 목표 그림(칸 · 목표 색)을 먼저 정하고, 시작 색 칸 배치를 무작위로 하나 골라 조건에 맞게 풀리는지 본다.
    // 소재 맵 시험 스크립트(DevelopLog 2026-10-06)를 옮겼다. 에디터가 시간 안에서 되풀이해 부른다(동기 실행)
    public static class SeedSearch
    {
        private const int SolveLimit = 120000;   // 생성기와 같은 탐색 상한
        private static readonly PaintColor[] _primaries = { PaintColor.Red, PaintColor.Yellow, PaintColor.Blue };

        // 무작위 배치 하나를 시험한다. 조건에 맞으면 그 스테이지(시작 색 · minMoves 채움), 아니면 null. 그림의 목표 색은 바꾸지 않는다
        public static GeneratedStage TryOnce(Stage picture, SeedSearchOptions options, SeededRandom random)
        {
            var paintable = new List<int>();
            PaintColor needed = PaintColor.Empty;
            for (int i = 0; i < picture.Cells.Count; i++)
            {
                if (picture.Cells[i].Target == PaintColor.Empty) continue;
                paintable.Add(i);
                needed |= picture.Cells[i].Target;
            }
            if (paintable.Count < options.Seeds) throw new ArgumentException($"칠할 칸({paintable.Count})이 시작 색 칸 수({options.Seeds})보다 적습니다", nameof(options));

            var start = new PaintColor[picture.Cells.Count];
            PaintColor supplied = PaintColor.Empty;
            for (int k = 0; k < options.Seeds; k++)
            {
                int pick;
                do pick = paintable[random.Next(paintable.Count)]; while (start[pick] != PaintColor.Empty);
                PaintColor[] choices = SeedColors(picture.Cells[pick].Target, options.MixedSeeds);
                start[pick] = choices[random.Next(choices.Length)];
                supplied |= start[pick];
            }
            // 색은 더해지기만 한다 — 그림에 쓰인 기본색을 시작 색이 모두 갖고 있어야 풀릴 수 있다
            if ((needed & ~supplied) != PaintColor.Empty) return null;

            var cells = new StageCell[picture.Cells.Count];
            for (int i = 0; i < cells.Length; i++) cells[i] = new StageCell(picture.Cells[i].Coord, start[i], picture.Cells[i].Target);
            var board = new Board(new Stage(picture.Name, cells, picture.Palette));
            SolveResult result = Solver.Solve(board, board.CreateStartState(), SolveLimit);
            if (!result.Solved || result.Path.Count < options.MinMoves || result.Path.Count > options.MaxMoves) return null;
            OrderSensitivity? order = Solver.MeasureOrder(board, board.CreateStartState(), result.Path);
            if (options.MaxOrderRatio < 1.0 && order.HasValue && (double)order.Value.Succeeded / order.Value.Total > options.MaxOrderRatio) return null;
            return new GeneratedStage(new Stage(picture.Name, cells, picture.Palette, result.Path.Count), order, BoardShape.Drawn);
        }

        // 그 칸 목표 색에 들어 있는 시작 색 후보 — 기본색만, 또는 섞인 색까지(목표 색의 부분 집합)
        private static PaintColor[] SeedColors(PaintColor target, bool mixed)
        {
            var colors = new List<PaintColor>();
            for (int c = 1; c <= (int)PaintColor.Black; c++)
            {
                var color = (PaintColor)c;
                if ((color & ~target) != PaintColor.Empty) continue;
                if (!mixed && Array.IndexOf(_primaries, color) < 0) continue;
                colors.Add(color);
            }
            return colors.ToArray();
        }
    }
}
