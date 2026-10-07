using System;
using System.Collections.Generic;

namespace ColoringBoot.Core
{
    // 프로토타입 rng(mulberry32)와 같은 난수 — 같은 시드면 프로토타입 생성기와 같은 스테이지가 나온다
    public sealed class SeededRandom
    {
        private uint _state;

        public SeededRandom(uint seed)
        {
            _state = seed;
        }

        // [0, 1)
        public double NextDouble()
        {
            _state += 0x6D2B79F5;
            uint t = (_state ^ (_state >> 15)) * (1 | _state);
            t = (t + ((t ^ (t >> 7)) * (61 | t))) ^ t;
            return (t ^ (t >> 14)) / 4294967296.0;
        }

        // [0, max)
        public int Next(int max) => (int)Math.Floor(NextDouble() * max);
    }

    public enum BoardShape
    {
        SmallHex,  // 반지름 2 육각형 (19칸)
        LargeHex,  // 반지름 3 육각형 (37칸)
        Diamond,   // 마름모 (16칸)
        Triangle,  // 삼각형 (15칸)
        Blob,      // 불규칙 (14~21칸)
        Random,    // 앞의 넷 중 하나(큰 육각형 제외 — 프로토타입과 같음)
        Drawn,     // 직접 그린 모양 — GeneratorOptions.Cells (2026-10-07)
    }

    // 생성 조건 — 기본값은 프로토타입 generate와 같다
    public sealed class GeneratorOptions
    {
        public BoardShape Shape = BoardShape.SmallHex;
        public int Seeds = 3;                    // 시작 색 칸 수
        public int Moves = 4;                    // 목표를 만들 때 긋는 획 수
        public PaintColor[] Palette = { PaintColor.Red, PaintColor.Yellow, PaintColor.Blue }; // 시작 색 후보
        public bool AllowBlack = true;           // 목표에 검정 허용
        public int MinSolve = 3;                 // 최소 수가 이보다 작으면 버림
        public double MaxOrderRatio = 0.34;      // 순서 민감도(성공 비율)가 이보다 크면 버림 — 1이면 조건 없음
        public double Fill = 0.5;                // 목표에서 칠해진 칸 비율 하한
        // 아래는 2026-10-07 생성 조건(GDD §8) — 기본값은 모두 꺼짐(프로토타입과 같은 결과)
        public IReadOnlyList<HexCoord> Cells;    // 직접 그린 모양 — 있으면 Shape 대신 이 칸들
        public PaintColor[] TargetColors;        // 목표에 쓸 색(빈칸 말고) — null이면 제한 없음
        public double MinMixedRatio;             // 칠해진 목표 칸 중 섞인 색 비율 하한
        public double MinTrapRatio;              // 막힐 수 있는 칸 비율 하한 — 단색 채우기 거르기(StageMetrics)
    }

    public sealed class GeneratedStage
    {
        public GeneratedStage(Stage stage, OrderSensitivity? order, BoardShape shape)
        {
            Stage = stage;
            Order = order;
            Shape = shape;
        }

        public Stage Stage { get; }             // MinMoves가 채워져 있다
        public OrderSensitivity? Order { get; }
        public BoardShape Shape { get; }
    }

    // 랜덤 생성기 (GDD §8) — 무작위 시작 색 · 무작위 획으로 목표를 만든 뒤 최소 수 · 순서 민감도로 거른다.
    // 프로토타입 generate · shapeCells를 난수 사용 순서까지 그대로 옮겼다. 에디터 도구용(동기 실행)
    public static class Generator
    {
        private const int Attempts = 400;
        private const int StrokeTriesPerMove = 6;
        private const int SolveLimit = 120000;
        private const int BlobRadius = 3;
        private const int BlobMin = 14;
        private const int BlobExtra = 8;

        private static readonly BoardShape[] _randomShapes = { BoardShape.SmallHex, BoardShape.Diamond, BoardShape.Triangle, BoardShape.Blob };

        // 조건에 맞는 스테이지 하나. 400번 시도해도 없으면 null (프로토타입 화면은 시간 제한 안에서 다시 부른다)
        public static GeneratedStage Generate(GeneratorOptions options, SeededRandom random, string name)
        {
            for (int attempt = 0; attempt < Attempts; attempt++)
            {
                BoardShape shape = options.Cells != null ? BoardShape.Drawn
                    : options.Shape == BoardShape.Random ? _randomShapes[random.Next(_randomShapes.Length)] : options.Shape;
                List<HexCoord> coords = options.Cells != null ? new List<HexCoord>(options.Cells) : ShapeCells(shape, random);
                int count = coords.Count;

                var start = new PaintColor[count];
                var picks = new List<int>();
                while (picks.Count < Math.Min(options.Seeds, count - 1))
                {
                    int pick = random.Next(count);
                    if (!picks.Contains(pick)) picks.Add(pick);
                }
                foreach (int pick in picks) start[pick] = options.Palette[random.Next(options.Palette.Length)];

                var board = new Board(new Stage(name, Cells(coords, start, new PaintColor[count])));
                var state = (PaintColor[])start.Clone();
                int used = 0;
                for (int t = 0; t < options.Moves * StrokeTriesPerMove && used < options.Moves; t++)
                {
                    BoardMove move = board.Moves[random.Next(board.Moves.Count)];
                    if (board.Brush(state, move.Cell, move.Direction)) used++;
                }
                if (used < options.Moves) continue;
                if (!options.AllowBlack && Array.IndexOf(state, PaintColor.Black) >= 0) continue;
                int painted = 0;
                foreach (PaintColor c in state)
                {
                    if (c != PaintColor.Empty) painted++;
                }
                if ((double)painted / count < options.Fill) continue;
                if (!MeetsConditions(options, start, state)) continue;

                var stage = new Stage(name, Cells(coords, start, state));
                var solvedBoard = new Board(stage);
                SolveResult result = Solver.Solve(solvedBoard, solvedBoard.CreateStartState(), SolveLimit);
                if (!result.Solved || result.Path.Count < options.MinSolve) continue;
                OrderSensitivity? order = Solver.MeasureOrder(solvedBoard, solvedBoard.CreateStartState(), result.Path);
                if (order.HasValue && (double)order.Value.Succeeded / order.Value.Total > options.MaxOrderRatio) continue;

                return new GeneratedStage(new Stage(name, stage.Cells, null, result.Path.Count), order, shape);
            }
            return null;
        }

        // 목표에 쓸 색 · 섞인 색 비율 · 막힐 수 있는 칸 비율 (2026-10-07 — 꺼져 있으면 늘 통과)
        private static bool MeetsConditions(GeneratorOptions options, PaintColor[] start, PaintColor[] target)
        {
            if (options.TargetColors != null)
            {
                foreach (PaintColor t in target)
                {
                    if (t != PaintColor.Empty && Array.IndexOf(options.TargetColors, t) < 0) return false;
                }
            }
            int mixed = StageMetrics.CountMixedCells(target, out int painted);
            if (options.MinMixedRatio > 0 && (painted == 0 || (double)mixed / painted < options.MinMixedRatio)) return false;
            return options.MinTrapRatio <= 0 || (double)StageMetrics.CountTrapCells(start, target) / target.Length >= options.MinTrapRatio;
        }

        // 보드 모양의 칸 좌표 — 프로토타입 shapeCells와 같은 순서
        public static List<HexCoord> ShapeCells(BoardShape shape, SeededRandom random)
        {
            var cells = new List<HexCoord>();
            switch (shape)
            {
                case BoardShape.SmallHex:
                case BoardShape.LargeHex:
                    int radius = shape == BoardShape.SmallHex ? 2 : 3;
                    for (int q = -radius; q <= radius; q++)
                        for (int r = -radius; r <= radius; r++)
                            if (Math.Abs(q + r) <= radius) cells.Add(new HexCoord(q, r));
                    break;
                case BoardShape.Diamond:
                    for (int q = 0; q < 4; q++)
                        for (int r = 0; r < 4; r++)
                            cells.Add(new HexCoord(q, r));
                    break;
                case BoardShape.Triangle:
                    for (int q = 0; q < 5; q++)
                        for (int r = 0; r < 5 - q; r++)
                            cells.Add(new HexCoord(q, r));
                    break;
                default: // 불규칙: 가운데에서 무작위로 이웃을 붙여 나간다
                    cells.Add(new HexCoord(0, 0));
                    int target = BlobMin + random.Next(BlobExtra);
                    while (cells.Count < target)
                    {
                        HexCoord from = cells[random.Next(cells.Count)];
                        HexCoord d = ((HexDirection)random.Next(6)).Delta();
                        var next = new HexCoord(from.Q + d.Q, from.R + d.R);
                        if (Math.Abs(next.Q) <= BlobRadius && Math.Abs(next.R) <= BlobRadius && Math.Abs(next.Q + next.R) <= BlobRadius && !cells.Contains(next))
                            cells.Add(next);
                    }
                    break;
            }
            return cells;
        }

        private static StageCell[] Cells(List<HexCoord> coords, PaintColor[] start, PaintColor[] target)
        {
            var cells = new StageCell[coords.Count];
            for (int i = 0; i < cells.Length; i++) cells[i] = new StageCell(coords[i], start[i], target[i]);
            return cells;
        }
    }
}
