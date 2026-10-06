using System.Collections.Generic;
using ColoringBoot.Core;

namespace ColoringBoot.Game
{
    // 따라 하기(튜토리얼, GDD §6 — 2026-10-07): 레슨 i = 따라 하기 목록(TutorialCatalog)의 i번째 스테이지.
    // 레슨마다 그을 획(칸 · 방향 · 한 줄 설명)을 차례로 정한다. 일부러 막혀 보는 획(trap)이 있으면 그것을 먼저 긋고, 되돌린 뒤 본 풀이로 간다.
    // 혼자 풀기 레슨(Note가 있음)은 획을 정하지 않는다 — 아무 획이나 받고 힌트도 쓸 수 있다(마지막 복습)
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
            public Guide[] Path = new Guide[0];   // 본 풀이 — 수마다 하나
            public Guide? Trap;    // 일부러 막혀 보는 획(없으면 null)
            public string Note;    // 혼자 풀기 레슨의 안내 한 줄(없으면 정해진 획만 받는 레슨)
            public bool Goal;      // 클리어 때 목표 그림(오른쪽 위)을 강조한다
            public string Done;    // 클리어 띠 문구
        }

        // 비공식 테스트(2026-10-07): 누른 칸부터 칠한다고 생각해 섞인 것을 버그로 여김 · 목표 그림을 몰랐음 → 2 · 3번 손가락은 줄 가운데 칸에서 시작한다
        private static readonly Lesson[] _lessons =
        {
            // 1. 붓질: 빈 붓은 칠하지 않고, 처음 만난 색을 묻혀 그 뒤를 칠한다 + 목표는 오른쪽 위 그림
            new Lesson
            {
                Path = new[] { new Guide(0, 0, HexDirection.Clock3, "칸을 누른 채 오른쪽으로 끌어 보세요") },
                Goal = true,
                Done = "오른쪽 위 그림처럼 칠하면 성공!",
            },
            // 2. 끝에서 끝까지: 누른 칸이 아니라 줄 끝에서 출발한다 — 누른 칸 뒤쪽도 칠해진다
            new Lesson
            {
                Path = new[] { new Guide(3, 0, HexDirection.Clock3, "이번엔 이 칸에서 오른쪽으로 끌어 보세요") },
                Done = "어느 칸에서 끌어도 줄 끝부터 칠해져요",
            },
            // 3. 섞기: 다른 색을 지나면 섞이고, 붓도 그 색이 되어 끝까지 칠한다
            new Lesson
            {
                Path = new[] { new Guide(2, 0, HexDirection.Clock3, "노랑 칸에서 오른쪽으로 끌어 보세요") },
                Done = "노랑을 만난 뒤로는 주황으로 칠해져요",
            },
            // 4. 되돌리기: 목표에 없는 색이 섞이면 막힌다 → 되돌리기
            new Lesson
            {
                Trap = new Guide(0, 0, HexDirection.Clock3, "이번에도 오른쪽으로 끌어 보세요"),
                Path = new[] { new Guide(1, -1, HexDirection.Clock5, "위 빨강 칸에서 오른쪽 아래로 끌어요") },
                Done = "막히면 되돌리면 돼요",
            },
            // 5. 복습: 혼자 풀기(3수 — 끝에서 끝까지 · 섞기 · 대각선 · 순서를 틀리면 막힘)
            new Lesson
            {
                Note = "배운 것으로 혼자 풀어 보세요 · 막히면 힌트",
                Done = "준비 끝! 이제 시작해요",
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

        public static bool IsFree(int lesson) => _lessons[lesson].Note != null;

        public static string Note(int lesson) => _lessons[lesson].Note;

        public static bool PointsAtGoal(int lesson) => _lessons[lesson].Goal;

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
                if (l.Note != null) yield return l.Note;
                yield return l.Done;
            }
        }
    }
}
