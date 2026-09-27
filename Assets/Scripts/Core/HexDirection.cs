namespace ColoringBoot.Core
{
    // 붓질 방향 6개 — 꼭짓점이 위를 향하는 배치의 시계 방향 (CLAUDE.md §3 방향 표)
    // 값 순서는 프로토타입 DIRS와 같다: 0~2(1·3·5시)가 각 축의 정방향이고, 3~5는 같은 순서의 반대 방향이다
    public enum HexDirection
    {
        Clock1,
        Clock3,
        Clock5,
        Clock7,
        Clock9,
        Clock11,
    }

    public static class HexDirectionExtensions
    {
        // 줄의 축 수 — 반대 방향끼리 한 축: 1·7시 / 3·9시 / 5·11시
        public const int AxisCount = 3;

        private static readonly HexCoord[] _deltas =
        {
            new HexCoord(1, -1),
            new HexCoord(1, 0),
            new HexCoord(0, 1),
            new HexCoord(-1, 1),
            new HexCoord(-1, 0),
            new HexCoord(0, -1),
        };

        // 한 칸 나아갈 때의 좌표 변화 (dq, dr)
        public static HexCoord Delta(this HexDirection dir) => _deltas[(int)dir];

        // 줄의 축: 0 = 1·7시, 1 = 3·9시, 2 = 5·11시
        public static int Axis(this HexDirection dir) => (int)dir % AxisCount;
    }
}
