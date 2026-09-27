using System;

namespace ColoringBoot.Core
{
    // 육각 축 좌표 (q, r) — 꼭짓점이 위를 향하는 배치 (CLAUDE.md §3)
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int Q { get; }
        public int R { get; }

        // 같은 줄 판정 키: dir의 축에서 이 값이 같은 칸들이 한 줄이다. 줄 중간의 빈자리와 상관없이 같은 직선 위면 같은 줄 (프로토타입 lineKey)
        public int LineKey(HexDirection dir)
        {
            switch (dir.Axis())
            {
                case 0: return Q + R; // 1·7시
                case 1: return R;     // 3·9시
                default: return Q;    // 5·11시
            }
        }

        // dir 쪽으로 나아간 정도. 같은 줄의 칸을 이 값의 오름차순으로 놓으면 dir의 반대편 끝 → dir 끝 순서다
        public int Along(HexDirection dir)
        {
            HexCoord d = dir.Delta();
            return Q * d.Q + R * d.R;
        }

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
        public override bool Equals(object obj) => obj is HexCoord other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Q, R);
        public static bool operator ==(HexCoord a, HexCoord b) => a.Equals(b);
        public static bool operator !=(HexCoord a, HexCoord b) => !a.Equals(b);
        public override string ToString() => $"({Q}, {R})";
    }
}
