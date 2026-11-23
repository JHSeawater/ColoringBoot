using System;
using System.Collections.Generic;

namespace ColoringBoot.Core
{
    // 스테이지 한 칸 — 스테이지 코드의 [q, r, 시작 색, 목표 색]
    public readonly struct StageCell
    {
        public StageCell(HexCoord coord, PaintColor start, PaintColor target)
        {
            Coord = coord;
            Start = start;
            Target = target;
        }

        public HexCoord Coord { get; }
        public PaintColor Start { get; }
        public PaintColor Target { get; }
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
        // 팔레트 이름. null이면 기본 팔레트 — 팔레트 에셋은 표현 계층이 고른다 (CLAUDE.md §5)
        public string Palette { get; }
        // 최소 수. null이면 수 카운터에 표시하지 않는다 — 값은 솔버가 채운다 (Phase 3)
        public int? MinMoves { get; }

        // 대표 색 = 목표에서 칸이 가장 많은 색(빈칸 제외) — 선택 화면이 깬 판 버튼을 이 색으로 채운다(2026-11-23 사용자).
        // 칸 수가 같으면 색 값이 작은 쪽(기본색이 섞인 색 · 검정보다 먼저). 목표가 모두 빈칸이면 Empty
        public PaintColor MainTargetColor()
        {
            var counts = new int[(int)PaintColor.Black + 1];
            foreach (StageCell cell in Cells) counts[(int)cell.Target]++;
            var main = PaintColor.Empty;
            int best = 0;
            for (int c = (int)PaintColor.Black; c > 0; c--)   // 큰 값부터 — 같은 수면 뒤에 오는 작은 값이 이긴다
            {
                if (counts[c] == 0 || counts[c] < best) continue;
                main = (PaintColor)c;
                best = counts[c];
            }
            return main;
        }

        // 스테이지 코드(JSON)를 읽는다. 형식이 틀리면 FormatException
        public static Stage Parse(string json) => StageParser.Parse(json);
    }
}
