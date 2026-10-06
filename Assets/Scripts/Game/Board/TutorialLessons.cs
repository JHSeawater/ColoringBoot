using System.Collections.Generic;
using ColoringBoot.Core;

namespace ColoringBoot.Game
{
    // 따라 하기(튜토리얼, GDD §6 — 2026-10-07): 레슨 i = 따라 하기 목록(TutorialCatalog)의 i번째 스테이지.
    // 레슨마다 그을 획(칸 · 방향 · 한 줄 설명)을 차례로 정한다. 일부러 막혀 보는 획(trap)이 있으면 그것을 먼저 긋고, 되돌린 뒤 본 풀이로 간다.
    // 문구는 이 파일의 문자열이라 폰트 빌더가 모은다 — 바꾸면 FontBuilder → BuildPrefabs → BuildScene
    public static class TutorialLessons
    {
        public readonly struct Guide
        {
            public Guide(int q, int r, HexDirection direction, string caption)
            {
                Cell = new HexCoord(q, r);
                Direction = direction;
                Caption = caption;
            }

            public HexCoord Cell { get; }
            public HexDirection Direction { get; }
            public string Caption { get; }
        }

        private sealed class Lesson
        {
            public Guide[] Path;   // 본 풀이 — 수마다 하나
            public Guide? Trap;    // 일부러 막혀 보는 획(없으면 null)
            public string Done;    // 클리어 띠 문구
        }

        private static readonly Lesson[] _lessons =
        {
            // 1. 붓질: 빈 붓은 칠하지 않고, 처음 만난 색을 묻혀 그 뒤를 칠한다
            new Lesson
            {
                Path = new[] { new Guide(0, 0, HexDirection.Clock3, "칸을 누른 채 오른쪽으로 끌어 보세요") },
                Done = "처음 만난 색으로 칠해요",
            },
            // 2. 섞기: 다른 색을 지나면 섞인다
            new Lesson
            {
                Path = new[] { new Guide(0, 0, HexDirection.Clock3, "빨강 칸에서 오른쪽으로 끌어 보세요") },
                Done = "빨강 + 노랑 = 주황",
            },
            // 3. 되돌리기: 목표에 없는 색이 섞이면 막힌다 → 되돌리기
            new Lesson
            {
                Trap = new Guide(0, 0, HexDirection.Clock3, "이번에도 오른쪽으로 끌어 보세요"),
                Path = new[] { new Guide(1, -1, HexDirection.Clock5, "위 빨강 칸에서 오른쪽 아래로 끌어요") },
                Done = "막히면 되돌리면 돼요",
            },
        };

        public static int Count => _lessons.Length;

        // 지금 그을 획 — 둔 수와 일부러 막혀 보기를 마쳤는지로 정한다. 그을 획이 없으면(다 풀었거나 막혀서 되돌려야 함) false
        public static bool TryGetGuide(int lesson, int moveCount, bool trapDone, out Guide guide)
        {
            Lesson l = _lessons[lesson];
            if (l.Trap.HasValue && !trapDone)
            {
                guide = l.Trap.Value;
                return moveCount == 0;
            }
            guide = moveCount < l.Path.Length ? l.Path[moveCount] : default;
            return moveCount < l.Path.Length;
        }

        public static string Done(int lesson) => _lessons[lesson].Done;

        // 레슨의 획들(일부러 막혀 보는 획 빼고) — 씬 점검용
        public static IReadOnlyList<Guide> Path(int lesson) => _lessons[lesson].Path;

        public static Guide? Trap(int lesson) => _lessons[lesson].Trap;

        // 화면에 나오는 모든 문구 — 씬 점검(폰트 글자)용
        public static IEnumerable<string> Texts()
        {
            foreach (Lesson l in _lessons)
            {
                if (l.Trap.HasValue) yield return l.Trap.Value.Caption;
                foreach (Guide g in l.Path) yield return g.Caption;
                yield return l.Done;
            }
        }
    }
}
