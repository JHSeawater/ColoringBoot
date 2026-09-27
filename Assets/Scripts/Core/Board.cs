using System;
using System.Collections.Generic;

namespace ColoringBoot.Core
{
    // 스테이지의 칸 배치로 만든 줄 구성과 규칙 판정 (GDD §2). 칸은 스테이지 Cells 순서의 인덱스로 가리킨다.
    // 색 상태(칸별 PaintColor 배열)는 들고 있지 않고 인자로 받는다 — 플레이 세션과 솔버(Phase 3)가 같은 규칙을 쓴다
    public sealed class Board
    {
        private readonly HexCoord[] _coords;
        private readonly PaintColor[] _start;
        private readonly PaintColor[] _target;
        private readonly Dictionary<HexCoord, int> _indexOf;
        // 줄마다 칸 인덱스를 축의 정방향(1·3·5시) 쪽으로 정렬해 둔다
        private readonly List<int[]> _lines = new List<int[]>();
        // [칸, 축] → 그 칸이 속한 줄의 _lines 인덱스
        private readonly int[,] _lineOf;

        public Board(Stage stage)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));

            int count = stage.Cells.Count;
            _coords = new HexCoord[count];
            _start = new PaintColor[count];
            _target = new PaintColor[count];
            _indexOf = new Dictionary<HexCoord, int>(count);
            for (int i = 0; i < count; i++)
            {
                StageCell cell = stage.Cells[i];
                _coords[i] = cell.Coord;
                _start[i] = cell.Start;
                _target[i] = cell.Target;
                _indexOf.Add(cell.Coord, i);
            }

            // 줄 = 같은 직선 위의 모든 칸. 줄 중간의 빈자리는 건너간다 (CLAUDE.md §3 — GDD §2.6 미확정, 프로토타입 규칙)
            _lineOf = new int[count, HexDirectionExtensions.AxisCount];
            for (int axis = 0; axis < HexDirectionExtensions.AxisCount; axis++)
            {
                var forward = (HexDirection)axis;
                var byKey = new Dictionary<int, List<int>>();
                for (int i = 0; i < count; i++)
                {
                    int key = _coords[i].LineKey(forward);
                    if (!byKey.TryGetValue(key, out List<int> members))
                    {
                        members = new List<int>();
                        byKey.Add(key, members);
                    }
                    members.Add(i);
                }

                foreach (List<int> line in byKey.Values)
                {
                    line.Sort((a, b) => _coords[a].Along(forward).CompareTo(_coords[b].Along(forward)));
                    foreach (int i in line) _lineOf[i, axis] = _lines.Count;
                    _lines.Add(line.ToArray());
                }
            }
        }

        public int CellCount => _coords.Length;
        public HexCoord CoordOf(int cell) => _coords[cell];
        public PaintColor TargetOf(int cell) => _target[cell];

        // 좌표의 칸 인덱스. 보드에 없는 좌표면 -1
        public int IndexOf(HexCoord coord) => _indexOf.TryGetValue(coord, out int cell) ? cell : -1;

        // 시작 상태를 새 배열로 돌려준다
        public PaintColor[] CreateStartState() => (PaintColor[])_start.Clone();

        // 붓질 (GDD §2.4): 칸이 속한 줄 전체를 dir의 반대편 끝에서 dir 끝까지 쓸고 지나간다. 색이 바뀐 칸이 있으면 true
        public bool Brush(PaintColor[] state, int cell, HexDirection dir)
        {
            int[] line = _lines[_lineOf[cell, dir.Axis()]];
            // 줄은 1·3·5시 쪽으로 정렬되어 있다 — 1·3·5시는 앞에서부터, 반대 방향(7·9·11시)은 뒤에서부터 쓴다
            bool forward = (int)dir < HexDirectionExtensions.AxisCount;
            PaintColor brush = PaintColor.Empty;
            bool changed = false;
            for (int k = 0; k < line.Length; k++)
            {
                int i = line[forward ? k : line.Length - 1 - k];
                if (state[i] != PaintColor.Empty) brush |= state[i];
                if (brush != PaintColor.Empty && state[i] != brush)
                {
                    state[i] = brush;
                    changed = true;
                }
            }
            return changed;
        }

        // 막힌 칸 (GDD §2.5): 목표 색에 없는 기본색이 들어갔다. 색은 빠지지 않으므로 이 칸은 다시 목표 색이 될 수 없다
        public bool IsDeadCell(PaintColor[] state, int cell) => (state[cell] & ~_target[cell]) != PaintColor.Empty;

        // 막힘: 막힌 칸이 하나라도 있다
        public bool IsDead(PaintColor[] state)
        {
            for (int i = 0; i < _target.Length; i++)
            {
                if (IsDeadCell(state, i)) return true;
            }
            return false;
        }

        // 성공: 모든 칸이 목표 색과 같다
        public bool IsSolved(PaintColor[] state)
        {
            for (int i = 0; i < _target.Length; i++)
            {
                if (state[i] != _target[i]) return false;
            }
            return true;
        }
    }
}
