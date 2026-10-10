using System;
using System.Collections.Generic;

namespace ColoringBoot.Core
{
    // 스테이지 한 칸 — 스테이지 코드의 [q, r, 시작 색, 목표 색]. 기믹 칸(벽 · 물 · 코팅)은 종류로 구분한다
    public readonly struct StageCell
    {
        public StageCell(HexCoord coord, PaintColor start, PaintColor target, CellKind kind = CellKind.Paint)
        {
            Coord = coord;
            Start = start;
            Target = target;
            Kind = kind;
        }

        public HexCoord Coord { get; }
        public PaintColor Start { get; }
        public PaintColor Target { get; }
        public CellKind Kind { get; }
    }

    // 스테이지 데이터 (GDD §3 스테이지 코드). 규칙 판정은 Board가 한다
    public sealed class Stage
    {
        public Stage(string name, IReadOnlyList<StageCell> cells, string palette = null, int? minMoves = null)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (cells.Count == 0) throw new ArgumentException("칸이 하나도 없습니다", nameof(cells));
            if (minMoves < 1) throw new ArgumentException($"최소 수는 1 이상이어야 합니다: {minMoves}", nameof(minMoves));

            var coords = new HashSet<HexCoord>();
            foreach (StageCell cell in cells)
            {
                if (cell.Start > PaintColor.Black || cell.Target > PaintColor.Black)
                    throw new ArgumentException($"색 값은 0~7이어야 합니다: {cell.Coord}", nameof(cells));
                if ((cell.Kind == CellKind.Wall || cell.Kind == CellKind.Water) && (cell.Start != PaintColor.Empty || cell.Target != PaintColor.Empty))
                    throw new ArgumentException($"벽 · 물 칸은 색이 없어야 합니다: {cell.Coord}", nameof(cells));
                if (cell.Kind == CellKind.Coated && (cell.Start == PaintColor.Empty || cell.Start != cell.Target))
                    throw new ArgumentException($"코팅 칸은 시작 색 = 목표 색(빈칸 아님)이어야 합니다: {cell.Coord}", nameof(cells));
                if (!coords.Add(cell.Coord))
                    throw new ArgumentException($"좌표가 중복됩니다: {cell.Coord}", nameof(cells));
            }

            Name = name;
            Cells = new List<StageCell>(cells);
            Palette = palette;
            MinMoves = minMoves;
        }

        public string Name { get; }
        public IReadOnlyList<StageCell> Cells { get; }
        // 팔레트 이름. null이면 기본 팔레트 — 팔레트 에셋은 표현 계층이 고른다 (CLAUDE.md §3)
        public string Palette { get; }
        // 최소 수. null이면 수 카운터에 표시하지 않는다 — 값은 솔버가 채운다 (Phase 3)
        public int? MinMoves { get; }

        // 스테이지 코드(JSON)를 읽는다. 형식이 틀리면 FormatException
        public static Stage Parse(string json) => StageParser.Parse(json);
    }
}
