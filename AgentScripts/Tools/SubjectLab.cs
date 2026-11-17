using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ColoringBoot.Core;

// 소재 맵 후보 생성(2026-10-11 챕터 1 덩굴 · 언덕 · 하늘) — 게임 데이터는 쓰지 않는다. 고른 후보를 챕터 Stages/ 파일에 옮긴다
// run_script(file=AgentScripts/Tools/SubjectLab.cs, entry=SubjectLab.Generate, args=["vine", 8, 1])
//   — 소재(vine · hill · sky · 챕터 2: lawn · stone · fence · bush · lavender · ladybug · can · sunflower
//     · 챕터 3: beachsky · sea · sand · palm · gull · fish · umbrella · parasol · crab · shell · castle
//     · 챕터 4: duskhill · maple · meadow · trail · mushroom · pumpkin · squirrel · acorn · leaves · aster
//     · 챕터 5: snowhill · smoke · roof · house · window · fir · snowman · sled) · 후보 수 · 시드. 후보는 Builds/SubjectLab/<소재>.json(git이 무시)에 쓰고 요약을 돌려준다
// 소재 단서 = 맵 모양 + 목표 색 구성(GDD §5 2026-10-06). 시작 색 구성을 정해 두고 무작위 자리 · 무작위 획으로 목표를 만든 뒤
// 목표 색 구성 · 막힐 수 있는 칸 비율(단색 채우기 거르기) · 최소 수 · 순서 민감도로 거른다
public static class SubjectLab
{
    private const int TimeLimitMs = 45000;   // run_script 응답 한도(60초) 안에서
    private const int SolveLimit = 120000;
    private const int MetricsLimit = 200000;
    private const int PictureSolveLimit = 30000;   // 그림 먼저 찾기: 풀이 없는 배치를 빨리 버린다(챕터 판은 상태 수천 개 안쪽)
    private const double MinTrapRatio = 0.3;
    private const double MaxOrderRatio = 0.34;   // 생성기 기본값과 같음
    private const string OutFolder = "Builds/SubjectLab";

    private sealed class Subject
    {
        public string Name;
        public string Palette;
        public (int r, int q0, int q1)[] Rows;   // 줄마다 q 범위 — 빈자리 없는 모양
        public PaintColor[] Seeds;               // 시작 색 칸(이 구성 그대로, 자리는 무작위)
        public PaintColor[] Targets;             // 목표에 쓸 색
        public double Fill;                      // 칠해진 칸 비율 하한
        public int MinSolve, MaxSolve;
        public Func<PaintColor[], bool> Looks;   // 목표 색 구성이 소재답나
    }

    private static readonly Dictionary<string, Subject> Subjects = new Dictionary<string, Subject>
    {
        // 덩굴: 오른쪽 위 → 왼쪽 아래로 뻗은 가지 띠, 초록 잎 + 주황 줄기(노랑이 두 색에 다 든다) — 노랑 두 칸 · 빈칸 조금까지.
        // 초록 · 주황만으로는 무작위 획 125만 번에 거의 안 나오고, 줄기 · 잎을 먼저 정한 그림 맵은 5~7수 풀이가 없었다(2026-10-11)
        ["vine"] = new Subject
        {
            Name = "덩굴", Rows = new[] { (0, 5, 7), (1, 3, 7), (2, 2, 5), (3, 0, 3), (4, -1, 1) },
            Seeds = new[] { PaintColor.Red, PaintColor.Yellow, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Green, PaintColor.Orange, PaintColor.Yellow }, Fill = 0.6, MinSolve = 5, MaxSolve = 5,
            Looks = t => Share(t, PaintColor.Green) >= 0.5 && Count(t, PaintColor.Orange) >= 2 && Count(t, PaintColor.Yellow) <= 2,
        },
        // 언덕: 둥근 언덕 모양, 초록 + 햇빛 받은 노랑
        ["hill"] = new Subject
        {
            Name = "언덕", Rows = new[] { (0, 2, 4), (1, 0, 4), (2, -1, 4), (3, -2, 4) },
            Seeds = new[] { PaintColor.Yellow, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Green, PaintColor.Yellow }, Fill = 0.8, MinSolve = 6, MaxSolve = 6,
            Looks = t => Share(t, PaintColor.Green) >= 0.5 && Count(t, PaintColor.Yellow) >= 3,
        },
        // 하늘: 넓은 판, 파랑 + 구름(빈칸) + 보라 노을 조금 — 파스텔
        ["sky"] = new Subject
        {
            Name = "하늘", Palette = "pastel", Rows = new[] { (0, 0, 5), (1, -1, 4), (2, -1, 4), (3, -2, 3) },
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Red },
            Targets = new[] { PaintColor.Blue, PaintColor.Purple }, Fill = 0.7, MinSolve = 7, MaxSolve = 7,
            Looks = t => Share(t, PaintColor.Blue) >= 0.6 && Count(t, PaintColor.Empty) >= 2,
        },

        // ── 챕터 2 봄 정원 (2026-11-15) — 색 구성은 판마다 다르게: 포함(노랑 ⊂ 연두 등) · 겹침(보라 · 초록이 파랑을 나눠 씀) · 3단(노랑 ⊂ 초록 ⊂ 진갈색)
        // 봄볕 spring: 1 분홍 · 2 밝은 노랑 · 3 나무색 · 4 하늘색 · 5 연보라 · 6 연두 · 7 회갈색 / 정원 garden: 1 빨강 · 2 샛노랑 · 3 귤색 · 4 파랑 · 5 보라 · 6 초록 · 7 진갈색
        // 잔디밭: 넓은 판, 연두 + 먼 들판의 밝은 노랑
        ["lawn"] = new Subject
        {
            Name = "잔디밭", Palette = "spring", Rows = Shape("xxxxx", "xxxxx", "xxxxx", "xxxxx"),
            Seeds = new[] { PaintColor.Yellow, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Green, PaintColor.Yellow }, Fill = 0.9, MinSolve = 5, MaxSolve = 7,
            Looks = t => Share(t, PaintColor.Green) >= 0.6 && Count(t, PaintColor.Yellow) >= 3,
        },
        // 디딤돌: 지그재그로 놓인 길, 회갈색 돌 + 사이의 연두 잔디
        ["stone"] = new Subject
        {
            Name = "디딤돌", Palette = "spring", Rows = Shape("xxx..", ".xxx.", "..xxx", ".xxx.", "xxx.."),
            Seeds = new[] { PaintColor.Red, PaintColor.Yellow, PaintColor.Blue, PaintColor.Yellow },
            Targets = new[] { PaintColor.Black, PaintColor.Green }, Fill = 0.85, MinSolve = 5, MaxSolve = 7,
            Looks = t => Count(t, PaintColor.Black) >= 4 && Share(t, PaintColor.Green) >= 0.35,
        },
        // 울타리: 낮고 넓은 판, 나무색 + 사이로 보이는 연두 덤불 · 노랑 조금
        ["fence"] = new Subject
        {
            Name = "울타리", Palette = "spring", Rows = Shape("xxxxxx", "xxxxxx", "xxxxxx"),
            Seeds = new[] { PaintColor.Red, PaintColor.Yellow, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Orange, PaintColor.Green, PaintColor.Yellow }, Fill = 0.85, MinSolve = 5, MaxSolve = 7,
            Looks = t => Share(t, PaintColor.Orange) >= 0.5 && Count(t, PaintColor.Green) >= 3 && Count(t, PaintColor.Yellow) <= 2,
        },
        // 덤불 · 잎: 둥근 덩어리, 초록 + 곁의 보라 꽃
        ["bush"] = new Subject
        {
            Name = "덤불", Palette = "garden", Rows = Shape(".xxxx.", "xxxxx.", "xxxxxx", "xxxxx."),
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Yellow, PaintColor.Red },
            Targets = new[] { PaintColor.Green, PaintColor.Purple }, Fill = 0.8, MinSolve = 6, MaxSolve = 7,
            Looks = t => Share(t, PaintColor.Green) >= 0.55 && Count(t, PaintColor.Purple) >= 2,
        },
        // 라벤더: 세로로 긴 판, 보라 꽃대 + 뒤의 파랑 하늘
        ["lavender"] = new Subject
        {
            Name = "라벤더", Palette = "garden", Rows = Shape("xxxx", "xxxx", "xxxx", "xxxx", "xxxx"),
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Red },
            Targets = new[] { PaintColor.Purple, PaintColor.Blue }, Fill = 0.9, MinSolve = 5, MaxSolve = 7,
            Looks = t => Share(t, PaintColor.Purple) >= 0.5 && Count(t, PaintColor.Blue) >= 4,
        },
        // 무당벌레: 둥근 몸, 빨강 + 진갈색 점 · 머리
        ["ladybug"] = new Subject
        {
            Name = "무당벌레", Palette = "garden", Rows = Shape(".xxx.", "xxxx.", "xxxxx", "xxxx.", ".xxx."),
            Seeds = new[] { PaintColor.Red, PaintColor.Red, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Red, PaintColor.Black }, Fill = 0.85, MinSolve = 6, MaxSolve = 8,
            Looks = t => Share(t, PaintColor.Red) >= 0.5 && Count(t, PaintColor.Black) >= 2,
        },
        // 물뿌리개: 몸통 + 왼쪽으로 뻗은 주둥이, 하늘색 + 아래 연두 잔디
        ["can"] = new Subject
        {
            Name = "물뿌리개", Palette = "spring", Rows = Shape("..xxx", "xxxxx", ".xxxx", ".xxxx", "xxxxx"),
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Yellow },
            Targets = new[] { PaintColor.Blue, PaintColor.Green }, Fill = 0.9, MinSolve = 6, MaxSolve = 8,
            Looks = t => Share(t, PaintColor.Blue) >= 0.5 && Count(t, PaintColor.Green) >= 4,
        },
        // 해바라기: 둥근 꽃 + 줄기, 샛노랑 꽃잎 ⊂ 초록 줄기 · 잎 ⊂ 진갈색 가운데
        ["sunflower"] = new Subject
        {
            Name = "해바라기", Palette = "garden", Rows = Shape(".xxx.", "xxxx.", "xxxxx", "xxxx.", ".xxx.", ".xx..", "..x.."),
            Seeds = new[] { PaintColor.Yellow, PaintColor.Yellow, PaintColor.Blue, PaintColor.Red },
            Targets = new[] { PaintColor.Yellow, PaintColor.Green, PaintColor.Black }, Fill = 0.9, MinSolve = 6, MaxSolve = 8,
            Looks = t => Share(t, PaintColor.Yellow) >= 0.4 && Count(t, PaintColor.Black) >= 3 && Count(t, PaintColor.Green) >= 2,
        },

        // ── 챕터 3 여름 바닷가 (2026-11-16) — 바닷가 beach: 1 빨강(게) · 2 노랑(해) · 3 주황(불가사리) · 4 청록(바다) · 5 보라(파라솔) · 6 초록(야자 잎) · 7 갈색(줄기)
        // 물가 shore: 1 분홍(조개) · 2 모래 · 3 모래성 · 4 하늘 · 5 연보라 · 6 민트(얕은 물) · 7 회색(갈매기)
        // 하늘: 낮고 넓은 판, 하늘색 + 회색 갈매기
        ["beachsky"] = new Subject
        {
            Name = "하늘", Palette = "shore", Rows = Shape("xxxxxx", "xxxxxx", "xxxxxx"),
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Red, PaintColor.Yellow },
            Targets = new[] { PaintColor.Blue, PaintColor.Black }, Fill = 0.9, MinSolve = 5, MaxSolve = 7,
            Looks = t => Share(t, PaintColor.Blue) >= 0.6 && Count(t, PaintColor.Black) >= 2 && Count(t, PaintColor.Black) <= 5,
        },
        // 바다: 물결 모양 판, 하늘색 깊은 물 + 민트 얕은 물
        ["sea"] = new Subject
        {
            Name = "바다", Palette = "shore", Rows = Shape(".xxxxx", "xxxxxx", "xxxxxx", "xxxxx."),
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Yellow },
            Targets = new[] { PaintColor.Blue, PaintColor.Green }, Fill = 0.9, MinSolve = 5, MaxSolve = 7,
            Looks = t => Share(t, PaintColor.Blue) >= 0.5 && Count(t, PaintColor.Green) >= 4,
        },
        // 모래사장: 낮은 언덕 모양, 모래 + 물가의 민트
        ["sand"] = new Subject
        {
            Name = "모래사장", Palette = "shore", Rows = Shape(".xxxx.", "xxxxxx", "xxxxxx"),
            Seeds = new[] { PaintColor.Yellow, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Yellow, PaintColor.Green }, Fill = 0.9, MinSolve = 4, MaxSolve = 6,
            Looks = t => Share(t, PaintColor.Yellow) >= 0.55 && Count(t, PaintColor.Green) >= 3,
        },
        // 야자수: 넓은 잎 아래 가는 줄기, 초록 잎 + 갈색 줄기 · 열매
        ["palm"] = new Subject
        {
            Name = "야자수", Palette = "beach", Rows = Shape("xxxxx", "xxxxx", ".xxx.", "..x..", "..x..", "..x.."),
            Seeds = new[] { PaintColor.Yellow, PaintColor.Blue, PaintColor.Red, PaintColor.Yellow },
            Targets = new[] { PaintColor.Green, PaintColor.Black }, Fill = 0.85, MinSolve = 6, MaxSolve = 8,
            // 줄기(아래 세 칸 — 칸 번호 13~15)는 갈색이어야 나무로 보인다
            Looks = t => Share(t, PaintColor.Green) >= 0.5 && t[13] == PaintColor.Black && t[14] == PaintColor.Black && t[15] == PaintColor.Black,
        },
        // 갈매기: 날개를 편 모양, 회색 몸 + 뒤의 하늘색
        ["gull"] = new Subject
        {
            Name = "갈매기", Palette = "shore", Rows = Shape("xxxxxxx", ".xxxxx.", "..xxx.."),
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Red, PaintColor.Yellow },
            Targets = new[] { PaintColor.Black, PaintColor.Blue }, Fill = 0.9, MinSolve = 5, MaxSolve = 7,
            Looks = t => Share(t, PaintColor.Black) >= 0.5 && Count(t, PaintColor.Blue) >= 3,
        },
        // 물고기: 둥근 몸, 노랑 몸 + 주황 지느러미
        ["fish"] = new Subject
        {
            Name = "물고기", Palette = "beach", Rows = Shape(".xxxx.", "xxxxxx", "xxxxxx", ".xxxx."),
            Seeds = new[] { PaintColor.Yellow, PaintColor.Yellow, PaintColor.Red },
            Targets = new[] { PaintColor.Yellow, PaintColor.Orange }, Fill = 0.9, MinSolve = 5, MaxSolve = 7,
            Looks = t => Share(t, PaintColor.Yellow) >= 0.5 && Count(t, PaintColor.Orange) >= 4,
        },
        // 파라솔: 둥근 지붕 모양, 보라 + 뒤의 청록 바다
        ["umbrella"] = new Subject
        {
            Name = "파라솔", Palette = "beach", Rows = Shape("..xxx..", ".xxxxx.", "xxxxxxx"),
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Red },
            Targets = new[] { PaintColor.Purple, PaintColor.Blue }, Fill = 0.9, MinSolve = 6, MaxSolve = 8,
            Looks = t => Share(t, PaintColor.Purple) >= 0.55 && Count(t, PaintColor.Blue) >= 3,
        },
        // 파라솔(2026-11-18 — 그림의 파라솔은 보라 + 노랑 줄무늬다. 위 umbrella는 노랑을 빼고 뒤의 바다 청록을 넣었던 색 구성 — 기록으로 둔다)
        ["parasol"] = new Subject
        {
            Name = "파라솔", Palette = "beach", Rows = Shape("..xxx..", ".xxxxx.", "xxxxxxx"),
            Seeds = new[] { PaintColor.Red, PaintColor.Blue, PaintColor.Yellow, PaintColor.Yellow },
            Targets = new[] { PaintColor.Purple, PaintColor.Yellow }, Fill = 0.8, MinSolve = 6, MaxSolve = 8,
            Looks = t => Count(t, PaintColor.Purple) >= 5 && Count(t, PaintColor.Yellow) >= 4,
        },
        // 게: 옆으로 넓은 몸, 빨강 + 갈색 다리 · 눈
        ["crab"] = new Subject
        {
            Name = "게", Palette = "beach", Rows = Shape(".xxxxx.", "xxxxxxx", ".xxxxx."),
            Seeds = new[] { PaintColor.Red, PaintColor.Red, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Red, PaintColor.Black }, Fill = 0.85, MinSolve = 6, MaxSolve = 8,
            Looks = t => Share(t, PaintColor.Red) >= 0.5 && Count(t, PaintColor.Black) >= 2,
        },
        // 조개 · 불가사리: 부채 모양, 분홍 조개 + 주황 불가사리
        ["shell"] = new Subject
        {
            Name = "조개", Palette = "shore", Rows = Shape("xxxxx", "xxxx.", ".xxx.", ".xx.."),
            Seeds = new[] { PaintColor.Red, PaintColor.Red, PaintColor.Yellow },
            Targets = new[] { PaintColor.Red, PaintColor.Orange }, Fill = 0.9, MinSolve = 5, MaxSolve = 7,
            Looks = t => Share(t, PaintColor.Red) >= 0.5 && Count(t, PaintColor.Orange) >= 3,
        },
        // 모래성: 위로 좁아지는 탑, 모래성 색 + 모래
        ["castle"] = new Subject
        {
            Name = "모래성", Palette = "shore", Rows = Shape("..xx..", ".xxxx.", "xxxxxx", "xxxxxx"),
            Seeds = new[] { PaintColor.Yellow, PaintColor.Yellow, PaintColor.Red },
            Targets = new[] { PaintColor.Orange, PaintColor.Yellow }, Fill = 0.9, MinSolve = 6, MaxSolve = 8,
            Looks = t => Share(t, PaintColor.Orange) >= 0.55 && Count(t, PaintColor.Yellow) >= 3,
        },

        // ── 챕터 4 가을 숲길 (2026-11-16) — 단풍 autumn: 1 빨강(단풍 · 버섯 갓) · 2 노랑(노란 단풍) · 3 주황(호박) · 4 파랑(재료) · 5 자주(꽃) · 6 초록(덤불 · 줄기) · 7 갈색(나무 줄기)
        // 노을 dusk: 1 분홍(노을) · 2 살구(오솔길) · 3 다람쥐 주황 · 4 푸른빛(재료) · 5 보라(먼 산) · 6 연두(풀밭) · 7 밤색(도토리 갓)
        // 색 규칙(DECISIONS 2026-11-16): 목표 색은 그 소재의 색 + 그림에서 맞닿은 이미 칠한 것의 색. 아직 칠하지 않은 소재 자리는 빈칸
        // 먼 산: 낮고 넓은 판, 아랫줄은 보라 능선 + 위로 이미 칠한 분홍 노을
        ["duskhill"] = new Subject
        {
            Name = "먼 산", Palette = "dusk", Rows = Shape("xxxxxxx", "xxxxxx", "xxxxxxx"),
            Seeds = new[] { PaintColor.Red, PaintColor.Red, PaintColor.Blue },
            Targets = new[] { PaintColor.Purple, PaintColor.Red }, Fill = 0.9, MinSolve = 5, MaxSolve = 7,
            // 아랫줄(칸 13~19)은 모두 산, 노을은 그 위에만
            Looks = t => Enumerable.Range(13, 7).All(i => t[i] == PaintColor.Purple) && Count(t, PaintColor.Red) >= 4,
        },
        // 단풍나무: 넓은 잎 덩어리 + 굵은 줄기, 빨강 · 주황 단풍 + 갈색 줄기
        ["maple"] = new Subject
        {
            Name = "단풍나무", Palette = "autumn", Rows = Shape(".xxxx.", "xxxxxx", "xxxxxx", "..xx..", "..xx.."),
            Seeds = new[] { PaintColor.Red, PaintColor.Red, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Red, PaintColor.Orange, PaintColor.Black }, Fill = 0.9, MinSolve = 6, MaxSolve = 8,
            // 줄기 아래 두 칸(칸 18 · 19)은 갈색, 갈색은 모두 일곱 칸까지(네 칸 모두 갈색을 요구하면 45초에 후보 2개뿐)
            Looks = t => t[18] == PaintColor.Black && t[19] == PaintColor.Black && Count(t, PaintColor.Black) <= 7
                && Count(t, PaintColor.Red) >= 4 && Count(t, PaintColor.Orange) >= 4,
        },
        // 풀밭: 넓은 판, 연두 + 너머의 이미 칠한 보라 산(둘이 푸른빛을 나눠 쓴다). 빈칸 = 아직 칠하지 않은 소재 자리
        // 산 자리를 조건으로 걸면(윗줄 · 위 두 줄) 무작위 획 40만 번에 후보가 없다 — 색 구성만 거르고 산이 위쪽에 있는 후보를 눈으로 고른다
        ["meadow"] = new Subject
        {
            Name = "풀밭", Palette = "dusk", Rows = Shape(".xxxxx", "xxxxxx", "xxxxxx", "xxxxx."),
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Yellow, PaintColor.Red },
            Targets = new[] { PaintColor.Green, PaintColor.Purple }, Fill = 0.8, MinSolve = 6, MaxSolve = 8,
            Looks = t => Share(t, PaintColor.Green) >= 0.6 && Count(t, PaintColor.Purple) >= 2,
        },
        // 오솔길: 구불구불한 띠, 살구 길 + 양옆의 이미 칠한 연두 풀밭
        ["trail"] = new Subject
        {
            Name = "오솔길", Palette = "dusk", Rows = Shape("..xxxx", ".xxxx.", "xxxx..", ".xxxx."),
            Seeds = new[] { PaintColor.Yellow, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Yellow, PaintColor.Green }, Fill = 0.9, MinSolve = 5, MaxSolve = 6,
            Looks = t => Count(t, PaintColor.Yellow) >= 6 && Count(t, PaintColor.Green) >= 4,
        },
        // 버섯: 둥근 갓 + 대, 빨강 갓 + 갈색 대 · 흙더미
        ["mushroom"] = new Subject
        {
            Name = "버섯", Palette = "autumn", Rows = Shape(".xxx.", "xxxx.", "xxxxx", ".xx..", ".xx.."),
            Seeds = new[] { PaintColor.Red, PaintColor.Red, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Red, PaintColor.Black }, Fill = 0.85, MinSolve = 6, MaxSolve = 8,
            // 맨 아래 두 칸(칸 14 · 15)은 갈색
            Looks = t => Share(t, PaintColor.Red) >= 0.5 && t[14] == PaintColor.Black && t[15] == PaintColor.Black && Count(t, PaintColor.Black) >= 3,
        },
        // 호박: 넓은 타원 + 꼭지, 주황 + 초록(꼭지 · 뒤의 이미 칠한 덤불 — 둘이 노랑을 나눠 쓴다)
        // 꼭지 칸이 초록이기를 조건으로 걸면 후보가 없다 — 색 구성만 거르고 초록이 위쪽에 있는 후보를 눈으로 고른다
        ["pumpkin"] = new Subject
        {
            Name = "호박", Palette = "autumn", Rows = Shape("..xx..", ".xxxx.", "xxxxxx", "xxxxx.", ".xxxx."),
            Seeds = new[] { PaintColor.Yellow, PaintColor.Yellow, PaintColor.Red, PaintColor.Blue },
            Targets = new[] { PaintColor.Orange, PaintColor.Green }, Fill = 0.8, MinSolve = 6, MaxSolve = 8,
            Looks = t => Share(t, PaintColor.Orange) >= 0.6 && Count(t, PaintColor.Green) >= 2,
        },
        // 다람쥐: 둥근 몸, 주황 + 살구 배
        ["squirrel"] = new Subject
        {
            Name = "다람쥐", Palette = "dusk", Rows = Shape(".xx..", "xxxx.", "xxxxx", "xxxx.", ".xxx."),
            Seeds = new[] { PaintColor.Yellow, PaintColor.Yellow, PaintColor.Red },
            Targets = new[] { PaintColor.Orange, PaintColor.Yellow }, Fill = 0.9, MinSolve = 5, MaxSolve = 7,
            Looks = t => Share(t, PaintColor.Orange) >= 0.6 && Count(t, PaintColor.Yellow) >= 2 && Count(t, PaintColor.Yellow) <= 5,
        },
        // 도토리: 넓은 갓 + 아래로 좁아지는 몸, 갈색 갓 + 주황 몸
        // 갓 · 몸 자리를 조건으로 걸면 후보가 없다 — 색 구성만 거르고 갈색이 위쪽에 있는 후보를 눈으로 고른다
        ["acorn"] = new Subject
        {
            Name = "도토리", Palette = "autumn", Rows = Shape(".xxxx.", "xxxxx.", ".xxxx.", ".xxx..", "..xx.."),
            Seeds = new[] { PaintColor.Red, PaintColor.Yellow, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Orange, PaintColor.Black }, Fill = 0.85, MinSolve = 6, MaxSolve = 8,
            Looks = t => Count(t, PaintColor.Orange) >= 6 && Count(t, PaintColor.Black) >= 4,
        },
        // 낙엽: 흩날리는 덩어리, 빨강 · 주황 · 노랑 잎
        ["leaves"] = new Subject
        {
            Name = "낙엽", Palette = "autumn", Rows = Shape(".xxxx.", "xxxxxx", "xxxxx.", ".xxxx."),
            Seeds = new[] { PaintColor.Red, PaintColor.Red, PaintColor.Yellow, PaintColor.Yellow },
            Targets = new[] { PaintColor.Red, PaintColor.Orange, PaintColor.Yellow }, Fill = 0.9, MinSolve = 6, MaxSolve = 8,
            Looks = t => Count(t, PaintColor.Red) >= 4 && Count(t, PaintColor.Yellow) >= 4 && Count(t, PaintColor.Orange) >= 4,
        },
        // 꽃: 자주 꽃잎 + 초록 잎(둘이 파랑을 나눠 쓴다) · 노랑 가운데는 있으면 좋고(0~3칸)
        ["aster"] = new Subject
        {
            Name = "꽃", Palette = "autumn", Rows = Shape(".xxxx.", "xxxxx.", "xxxxxx", "xxxxx."),
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Red, PaintColor.Yellow },
            Targets = new[] { PaintColor.Purple, PaintColor.Green, PaintColor.Yellow }, Fill = 0.8, MinSolve = 6, MaxSolve = 8,
            Looks = t => Count(t, PaintColor.Purple) >= 5 && Count(t, PaintColor.Green) >= 4 && Count(t, PaintColor.Yellow) <= 3,
        },

        // ── 챕터 5 눈 내린 밤 (2026-11-16) — 밤 night: 1 빨강(눈사람 모자 · 목도리) · 2 노랑(달 · 별) · 3 주황(당근 코 · 썰매) · 4 남색(밤하늘) · 5 자주(재료) · 6 초록(전나무) · 7 갈색(나뭇가지)
        // 눈마을 village: 1 살구(집 벽) · 2 달빛 노랑 · 3 주황(창문 불빛) · 4 남색(밤하늘) · 5 연보라(먼 산 · 연기) · 6 청록(지붕) · 7 벽돌(굴뚝). 흰 눈은 종이 — 판에서는 빈칸
        // 색 규칙(DECISIONS 2026-11-16)대로 다른 소재 색은 이미 칠한 맞닿은 것만. 소재 자리 조건은 걸지 않고(챕터 4 기록) 색 구성만 거른 뒤 눈으로 고른다
        // 먼 산: 삼각형 판, 연보라 산 + 뒤의 이미 칠한 남색 밤하늘
        ["snowhill"] = new Subject
        {
            Name = "먼 산", Palette = "village", Rows = Shape("..xxx..", ".xxxxx.", "xxxxxxx"),
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Red },
            Targets = new[] { PaintColor.Purple, PaintColor.Blue }, Fill = 0.9, MinSolve = 6, MaxSolve = 8,
            Looks = t => Share(t, PaintColor.Purple) >= 0.55 && Count(t, PaintColor.Blue) >= 3,
        },
        // 굴뚝 연기: 세로로 긴 판, 연보라 연기 + 뒤의 남색 밤하늘
        ["smoke"] = new Subject
        {
            Name = "연기", Palette = "village", Rows = Shape("xxxx", "xxxx", "xxxx", "xxxx", "xxxx"),
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Red },
            Targets = new[] { PaintColor.Purple, PaintColor.Blue }, Fill = 0.9, MinSolve = 6, MaxSolve = 8,
            Looks = t => Share(t, PaintColor.Purple) >= 0.4 && Count(t, PaintColor.Blue) >= 5,
        },
        // 지붕: 삼각 지붕, 청록 + 뒤의 연보라 산(둘이 남색을 나눠 쓴다). 빈칸 = 지붕 위 눈
        ["roof"] = new Subject
        {
            Name = "지붕", Palette = "village", Rows = Shape("..xxx..", ".xxxxx.", "xxxxxxx"),
            Seeds = new[] { PaintColor.Blue, PaintColor.Blue, PaintColor.Yellow, PaintColor.Red },
            Targets = new[] { PaintColor.Green, PaintColor.Purple }, Fill = 0.8, MinSolve = 6, MaxSolve = 8,
            Looks = t => Share(t, PaintColor.Green) >= 0.55 && Count(t, PaintColor.Purple) >= 2,
        },
        // 집 · 굴뚝: 네모난 벽 + 위의 굴뚝, 살구 벽 + 벽돌 굴뚝
        ["house"] = new Subject
        {
            Name = "집", Palette = "village", Rows = Shape("..xx..", "xxxxx.", "xxxxxx", "xxxxx."),
            Seeds = new[] { PaintColor.Red, PaintColor.Red, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Red, PaintColor.Black }, Fill = 0.85, MinSolve = 7, MaxSolve = 8,
            Looks = t => Share(t, PaintColor.Red) >= 0.5 && Count(t, PaintColor.Black) >= 3,
        },
        // 창문 불빛: 네모난 판, 주황 불빛 + 둘레의 이미 칠한 살구 벽
        ["window"] = new Subject
        {
            Name = "창문", Palette = "village", Rows = Shape("xxxx.", "xxxxx", "xxxx.", "xxxxx"),
            Seeds = new[] { PaintColor.Red, PaintColor.Red, PaintColor.Yellow },
            Targets = new[] { PaintColor.Red, PaintColor.Orange }, Fill = 0.9, MinSolve = 5, MaxSolve = 6,
            Looks = t => Count(t, PaintColor.Orange) >= 5 && Count(t, PaintColor.Red) >= 5,
        },
        // 전나무: 층층이 넓어지는 세모 + 줄기, 초록 + 갈색 줄기. 빈칸 = 가지 위 눈
        ["fir"] = new Subject
        {
            Name = "전나무", Palette = "night", Rows = Shape("..x..", ".xx..", ".xxx.", "xxxx.", "xxxxx", ".xx.."),
            Seeds = new[] { PaintColor.Yellow, PaintColor.Blue, PaintColor.Red, PaintColor.Yellow },
            Targets = new[] { PaintColor.Green, PaintColor.Black }, Fill = 0.8, MinSolve = 7, MaxSolve = 8,
            // 줄기(아래 두 칸 — 칸 15 · 16)는 갈색
            Looks = t => Share(t, PaintColor.Green) >= 0.5 && t[15] == PaintColor.Black && t[16] == PaintColor.Black,
        },
        // 눈사람: 둥근 덩어리 둘, 빨강 모자 · 목도리 + 갈색 나뭇가지 팔 · 눈(주황 코는 두 칸까지). 빈칸 = 눈으로 된 몸
        ["snowman"] = new Subject
        {
            Name = "눈사람", Palette = "night", Rows = Shape(".xxx.", "xxxx.", ".xxx.", "xxxx.", "xxxxx", "xxxx."),
            Seeds = new[] { PaintColor.Red, PaintColor.Red, PaintColor.Yellow, PaintColor.Blue },
            Targets = new[] { PaintColor.Red, PaintColor.Orange, PaintColor.Black }, Fill = 0.6, MinSolve = 7, MaxSolve = 8,
            Looks = t => Count(t, PaintColor.Red) >= 6 && Count(t, PaintColor.Black) >= 3 && Count(t, PaintColor.Orange) <= 2 && Count(t, PaintColor.Empty) >= 4,
        },
        // 썰매: 낮고 넓은 판, 주황 썰매 + 갈색 다리(곁의 이미 칠한 눈사람 빨강은 세 칸까지)
        ["sled"] = new Subject
        {
            Name = "썰매", Palette = "night", Rows = Shape("xxxxxx.", "xxxxxxx", ".xxxxx."),
            Seeds = new[] { PaintColor.Yellow, PaintColor.Yellow, PaintColor.Red, PaintColor.Blue },
            Targets = new[] { PaintColor.Orange, PaintColor.Black, PaintColor.Red }, Fill = 0.85, MinSolve = 7, MaxSolve = 8,
            Looks = t => Share(t, PaintColor.Orange) >= 0.5 && Count(t, PaintColor.Black) >= 3 && Count(t, PaintColor.Red) <= 3,
        },
    };

    // 글자 그림 → 줄마다 q 범위. 'x' = 칸, 홀수 줄은 반 칸 오른쪽(q = 열 - 줄/2 내림) — 줄마다 칸이 이어져 있어야 한다
    private static (int r, int q0, int q1)[] Shape(params string[] rows) =>
        rows.Select((row, r) => (r, row.IndexOf('x') - (r - (r & 1)) / 2, row.LastIndexOf('x') - (r - (r & 1)) / 2)).ToArray();

    public static string Generate(string key, int count, int seed)
    {
        Subject subject = Subjects[key];
        var coords = new List<HexCoord>();
        foreach ((int r, int q0, int q1) in subject.Rows)
        {
            for (int q = q0; q <= q1; q++) coords.Add(new HexCoord(q, r));
        }
        var random = new SeededRandom((uint)seed);
        var seen = new HashSet<string>();
        var found = new List<string>();
        var report = new StringBuilder();
        var clock = Stopwatch.StartNew();
        int tries = 0, unlike = 0, unsolved = 0, ordered = 0;
        while (clock.ElapsedMilliseconds < TimeLimitMs && found.Count < count)
        {
            tries++;
            var start = new PaintColor[coords.Count];
            foreach (PaintColor color in subject.Seeds)
            {
                int pick;
                do pick = random.Next(coords.Count); while (start[pick] != PaintColor.Empty);
                start[pick] = color;
            }
            var board = new Board(Make(subject, coords, start, start, null));
            PaintColor[] state = board.CreateStartState();
            int strokes = subject.MinSolve + random.Next(3), used = 0;
            for (int t = 0; t < strokes * 6 && used < strokes; t++)
            {
                BoardMove move = board.Moves[random.Next(board.Moves.Count)];
                if (board.Brush(state, move.Cell, move.Direction)) used++;
            }
            if (used < strokes) continue;
            // 보드의 칸 순서는 좌표 순서와 같다(빈자리 · 기믹 없음)
            if (state.Any(c => c != PaintColor.Empty && Array.IndexOf(subject.Targets, c) < 0)
                || (double)state.Count(c => c != PaintColor.Empty) / state.Length < subject.Fill
                || !subject.Looks(state)
                || (double)StageMetrics.CountTrapCells(start, state) / state.Length < MinTrapRatio) { unlike++; continue; }
            string id = string.Join(",", start.Zip(state, (s, t) => $"{(int)s}{(int)t}"));
            if (!seen.Add(id)) continue;

            Stage stage = Make(subject, coords, start, state, null);
            var solvedBoard = new Board(stage);
            SolveResult result = Solver.Solve(solvedBoard, solvedBoard.CreateStartState(), SolveLimit);
            if (!result.Solved || result.Path.Count < subject.MinSolve || result.Path.Count > subject.MaxSolve) { unsolved++; continue; }
            OrderSensitivity? order = Solver.MeasureOrder(solvedBoard, solvedBoard.CreateStartState(), result.Path);
            if (order.HasValue && (double)order.Value.Succeeded / order.Value.Total > MaxOrderRatio) { ordered++; continue; }

            stage = Make(subject, coords, start, state, result.Path.Count);
            StageMetrics m = StageMetrics.Measure(new Board(stage), MetricsLimit);
            string line = $"{result.Path.Count}수 · 순서 {(order.HasValue ? $"{order.Value.Succeeded}/{order.Value.Total}" : "-")}"
                + $" · 막힐 칸 {Percent(m.TrapRatio)} · 섞인 색 {Percent(m.MixedRatio)} · 막히는 획 {Percent(m.DeadStrokeRatio)} · 조용한 막다른 길 {Percent(m.SilentRatio)}"
                + $" · 대충 풀림 {Percent(m.SolveChance)}";
            found.Add($"{{\"metrics\":\"{line}\",\"stage\":{StageWriter.ToJson(stage)}}}");
            report.AppendLine($"#{found.Count} {line}");
        }
        Directory.CreateDirectory(OutFolder);
        string path = $"{OutFolder}/{key}.json";
        File.WriteAllText(path, "[\n" + string.Join(",\n", found) + "\n]\n", new UTF8Encoding(false));
        return $"{subject.Name}({coords.Count}칸) 후보 {found.Count}개 / 시도 {tries}번(색 구성 탈락 {unlike} · 수 범위 밖 {unsolved} · 순서 쉬움 {ordered}) · {clock.ElapsedMilliseconds / 1000}초 → {path}\n{report}";
    }

    // 그림 먼저(2026-11-15, 챕터 2~) — 목표 그림을 글자로 정해 두고 시작 색 칸 배치만 무작위로 찾는다(그림 맵 · 배경 + 미리 칠한 소재 칸 공용).
    // run_script(file=AgentScripts/Tools/SubjectLab.cs, entry=SubjectLab.Picture,
    //   args=["파일 키", "화면 이름", "팔레트(default면 기본)", "rows", 시작 칸 최소, 최대, 최소 수 하한, 상한, 대충 풀림 하한 %, 상한 %, 후보 수, 시드])
    // rows: 줄을 '/'로 나눈다. 글자 하나 = 칸 하나, 홀수 줄은 반 칸 오른쪽(q = 열 - 줄/2 내림). '.' 칸 없음 · 0~7 목표 색 · A~G 미리 칠한 칸(시작 = 목표 = 1~7).
    // 시작 색은 그 칸 목표에 든 기본색만. 후보는 Builds/SubjectLab/<파일 키>.json(한 줄에 하나)에 쓴다 — 고른 것은 Phase8Chapters.AddStage로 챕터에 넣는다.
    // 챕터 2에 넣은 판(2026-11-15 — 같은 인자로 다시 돌리면 같은 후보가 같은 번호로 나온다. 넣기는 Phase8Chapters.AddStages):
    //   화분    Picture("GardenPot5", "화분", "garden", "..1../.11../33333/3333.", 3, 4, 4, 5, 90, 100, 8, 2) #6
    //   하늘    Picture("GardenSky", "하늘", "spring", "44444/44444/44444/2222C", 2, 4, 4, 8, 0, 100, 16, 1) #10 — 2026-11-16 울타리 한 칸(C)을 비워 둘 칸(0)으로 손으로 바꿈
    //   튤립    Picture("TulipI", "튤립", "spring", ".111./1111./.111./.666./..6..", 5, 8, 5, 6, 20, 100, 12, 7) #6 (2026-11-16 — 그 전은 잎 여섯 칸을 미리 칠한 GardenTulip #2)
    //   덤불    Picture("BushG", "덤불", "garden", ".666./66066/66666.", 3, 5, 5, 6, 0, 100, 12, 5) #1 (2026-11-16 — 그 전은 초록 + 보라 Generate("bush", 12, 2) #2)
    //   해바라기 Picture("GardenSunflower", "해바라기", "garden", ".222./2GG2./2GGG2/2GG2./.222./.66../..6..", 2, 4, 5, 8, 0, 100, 16, 1) #3
    //   잔디밭 Generate("lawn", 14, 1) #8 · 라벤더 Generate("lavender", 14, 1) #3 · 울타리 Generate("fence", 14, 1) #9
    //   물뿌리개 Generate("can", 30, 2) #1 · 디딤돌 Generate("stone", 14, 1) #3 · 무당벌레 Generate("ladybug", 12, 2) #9
    // 챕터 3에 넣은 판(2026-11-16):
    //   해      Picture("BeachSun", "해", "beach", ".C2C./2222./C222C/2222./.C2C.", 2, 3, 3, 6, 0, 100, 16, 1) #4
    //   모래사장 Picture("SandB", "모래사장", "shore", ".2222/222220/222200", 2, 4, 4, 5, 0, 100, 12, 1) #4 (그 전은 모래 + 민트 Generate("sand", 24, 1) #5)
    //   하늘    Picture("SkyB", "하늘", "shore", "44044/44444/40044/44444", 2, 4, 5, 7, 0, 100, 14, 1) #5 (그 전은 하늘색 + 회색 Generate("beachsky", 24, 1) #7)
    //   게      Picture("CrabK5", "게", "beach", "11311/1331./11311", 3, 5, 6, 8, 5, 40, 10, 2) #3 (그 전은 빨강 + 갈색 띠 Generate("crab", 24, 1) #1)
    //   물고기 Generate("fish", 24, 1) #3 · 갈매기 Generate("gull", 24, 1) #8
    //   바다 Generate("sea", 30, 2) #5 · 모래성 Generate("castle", 24, 1) #23 · 조개 Generate("shell", 24, 1) #10 · 파라솔 Generate("parasol", 60, 3) #43 (2026-11-18 보라 + 노랑 — 그 전은 보라 + 청록 Generate("umbrella", 30, 2) #9)
    //   야자수 Generate("palm", 24, 1) #10
    // 챕터 4에 넣은 판(2026-11-16 — 그림 단계 순서 그대로):
    //   노을    Picture("DuskSkyG", "노을", "dusk", "111111/33333./.3BB3.", 1, 3, 4, 5, 0, 100, 12, 1) #4 (해 두 칸 B는 주황을 만드는 노랑 재료 — 큰 그림 19칸은 7~10수)
    //   도토리  Picture("AcornB", "도토리", "autumn", ".777./7777./.333./.33../..3..", 3, 5, 5, 8, 0, 100, 12, 1) #2 (18칸 그림은 풀이 탐색 상한에 걸려 후보 없음)
    //   먼 산 Generate("duskhill", 24, 1) #1 · 단풍나무 Generate("maple", 24, 3) #8 · 풀밭 Generate("meadow", 24, 3) #2 · 오솔길 Generate("trail", 40, 3) #32
    //   버섯 Generate("mushroom", 24, 1) #2 · 호박 Generate("pumpkin", 24, 1) #1 · 다람쥐 Generate("squirrel", 24, 1) #13 · 낙엽 Generate("leaves", 24, 1) #5 · 꽃 Generate("aster", 24, 1) #1
    //   — 단풍나무 · 풀밭 · 호박 · 다람쥐 · 꽃 · 도토리는 후보 수를 채우기 전에 시간 제한(45초)으로 끝난다: 후보 수는 PC 속도에 따라 다르지만 번호는 앞에서부터 같다
    //   — 소재 자리(산은 위쪽 · 꼭지 · 줄기)를 조건으로 걸면 후보가 없어 색 구성만 거르고 눈으로 골랐다(풀밭은 시드 1 · 3 · 4의 후보 10개 중 산이 윗줄인 것 5개)
    // 챕터 5에 넣은 판(2026-11-16 — 달을 먼저 칠하고 밤하늘은 그 달을 피해서):
    //   달      Picture("MoonA", "달", "night", ".222./222../22.../222../.222.", 1, 2, 4, 5, 0, 100, 12, 1) #12 (초승달 모양 · 노랑 한 색)
    //   밤하늘  Picture("NightSkyB", "밤하늘", "night", "BB444/B4404/44444/40444", 2, 4, 5, 7, 0, 100, 12, 1) #11 (B = 이미 칠한 달 · 0 = 아직 칠하지 않은 별 자리)
    //   별      Picture("StarA", "별", "night", ".2...2./222.222/.2...2./...2.../..222../...2...", 2, 3, 4, 6, 0, 100, 12, 1) #12 (떨어진 세 덩어리 — 붓이 빈자리를 건너간다)
    //   먼 산 Generate("snowhill", 24, 1) #6 · 연기 Generate("smoke", 24, 1) #13 · 지붕 Generate("roof", 24, 2) #19 · 집 Generate("house", 24, 1) #12
    //   창문 Generate("window", 40, 3) #10 · 전나무 Generate("fir", 24, 1) #15 · 눈사람 Generate("snowman", 24, 1) #21 · 썰매 Generate("sled", 24, 2) #17
    //   — 전나무 · 썰매는 시간 제한(45초)으로 끝난다(번호는 앞에서부터 같다). 별 · 달을 남색 하늘 안에 두는 그림은 못 만든다(노랑 붓이 하늘 칸에 닿으면 막힘 — 가장자리 칸만 칠할 수 있다)
    //   — 눈사람을 "몸은 칸 없음(종이)" 그림으로 하면(모자 · 목도리 · 팔만 칸) 6~7수 · 대충 풀림 100%뿐이라 소재 맵 후보를 썼다
    // 색 규칙(DECISIONS 2026-11-16): 목표 색은 그 소재의 색과 그림에서 맞닿아 있고 이미 칠한(앞 판의) 것의 색만. 아직 칠하지 않은 소재 자리는 0(비워 둘 칸)
    //   — 소재 표의 bush · sand · beachsky · crab · umbrella는 이 규칙 전의 색 구성이다(챕터 판에는 더 쓰지 않음, 기록으로 둔다)
    // 쉬어 가기 판은 대충 풀림만 보지 않는다 — 순서 성공률이 높고(대략 9% 이상) 수가 적은 후보를 고른다(챕터 2 해바라기: 대충 풀림 100%인데 가장 많이 헤맴, DevelopLog 2026-11-16)
    // 그림 먼저가 맞는 것: 색 하나짜리 띠 · 포함 관계(빨강 ⊂ 귤색) · 미리 칠한 칸으로 막아 둔 그림 — 5~7수 · 대충 풀림 100%가 나온다.
    // 안 맞는 것: 틈이 있는 그림(줄이 끊겨 9~12수) · 기본색을 나눠 쓰지 않는 두 색 그림(분홍 꽃 + 연두 잎 — 8수 이상) → 소재 맵(Generate)으로
    // 2026-11-16에 더 알게 된 것:
    //   한 색 + 비워 둘 칸 — 0이 한 칸이거나 가장자리면 대충 풀림 100%(BushG · SandB), 가운데에 2~3칸이면 조용한 막다른 길이 생겨 15~40%도 나온다(SkyB). 0 두 칸이 한 줄 가운데에 붙으면 1%까지 떨어진다
    //   분홍 꽃 + 연두 잎 — 잎을 네 칸으로 줄이고 시작 칸을 7~8개 주면 5~6수가 나온다(TulipI). 잎 여섯 칸은 시작 칸 10개에서도 7수
    //   빨강 ⊂ 주황 — 주황이 빨강 옆에 붙으면 100%, 빨강 덩어리 사이에 끼면 17~40%(CrabK5 · 주황 칸의 노랑이 빨강 칸으로 새면 막힌다)
    public static string Picture(string key, string name, string palette, string rows, int minSeeds, int maxSeeds, int minMoves, int maxMoves,
        int minChance, int maxChance, int count, int seed)
    {
        var coords = new List<HexCoord>();
        var target = new List<PaintColor>();
        var fixedStart = new List<PaintColor>();
        string[] lines = rows.Split('/');
        for (int r = 0; r < lines.Length; r++)
        {
            for (int col = 0; col < lines[r].Length; col++)
            {
                char c = lines[r][col];
                if (c == '.') continue;
                bool pre = c >= 'A' && c <= 'G';
                if (!pre && (c < '0' || c > '7')) return $"줄 {r} 열 {col}: 모르는 글자 '{c}'";
                var color = (PaintColor)(pre ? c - 'A' + 1 : c - '0');
                coords.Add(new HexCoord(col - (r - (r & 1)) / 2, r));
                target.Add(color);
                fixedStart.Add(pre ? color : PaintColor.Empty);
            }
        }
        int n = coords.Count;
        string paletteId = palette == "default" ? null : palette;
        var paintable = Enumerable.Range(0, n).Where(i => target[i] != PaintColor.Empty && fixedStart[i] == PaintColor.Empty).ToList();
        // 시작 칸을 놓을 자리가 모자라면 아래 자리 고르기가 끝나지 않는다(에디터가 멈춤)
        if (minSeeds < 0 || minSeeds > maxSeeds || maxSeeds > paintable.Count) return $"시작 칸 수 {minSeeds}~{maxSeeds}: 0 이상 · 최소 ≤ 최대 ≤ 칠할 칸 {paintable.Count}개여야 함";
        PaintColor needed = PaintColor.Empty, given = PaintColor.Empty;
        foreach (PaintColor t in target) needed |= t;
        foreach (PaintColor f in fixedStart) given |= f;
        PaintColor[] primaries = { PaintColor.Red, PaintColor.Yellow, PaintColor.Blue };

        var random = new SeededRandom((uint)seed);
        var seen = new HashSet<string>();
        var found = new List<string>();
        var report = new StringBuilder();
        var clock = Stopwatch.StartNew();
        int tries = 0, uncovered = 0, unsolved = 0, range = 0, chanceOut = 0;
        var outOfRange = new SortedDictionary<int, int>();   // 범위 밖이었던 최소 수 → 개수(범위를 고칠 때 본다)
        while (clock.ElapsedMilliseconds < TimeLimitMs && found.Count < count)
        {
            tries++;
            PaintColor[] start = fixedStart.ToArray();
            PaintColor supplied = given;
            int seeds = minSeeds + random.Next(maxSeeds - minSeeds + 1);
            for (int k = 0; k < seeds; k++)
            {
                int pick;
                do pick = paintable[random.Next(paintable.Count)]; while (start[pick] != PaintColor.Empty);
                PaintColor[] choices = primaries.Where(p => (p & ~target[pick]) == PaintColor.Empty).ToArray();
                start[pick] = choices[random.Next(choices.Length)];
                supplied |= start[pick];
            }
            if ((needed & ~supplied) != PaintColor.Empty) { uncovered++; continue; }
            if (!seen.Add(string.Join(",", start.Select(c => (int)c)))) continue;

            Stage Build(int? moves) => new Stage(name, coords.Select((c, i) => new StageCell(c, start[i], target[i])).ToList(), paletteId, moves);
            var board = new Board(Build(null));
            SolveResult result = Solver.Solve(board, board.CreateStartState(), PictureSolveLimit);
            if (!result.Solved) { unsolved++; continue; }
            if (result.Path.Count < minMoves || result.Path.Count > maxMoves)
            {
                range++;
                outOfRange[result.Path.Count] = outOfRange.TryGetValue(result.Path.Count, out int had) ? had + 1 : 1;
                continue;
            }
            OrderSensitivity? order = Solver.MeasureOrder(board, board.CreateStartState(), result.Path);
            Stage stage = Build(result.Path.Count);
            StageMetrics m = StageMetrics.Measure(new Board(stage), 2000000);
            if (!m.SolveChance.HasValue || m.SolveChance.Value * 100 < minChance || m.SolveChance.Value * 100 > maxChance) { chanceOut++; continue; }
            string line = $"{result.Path.Count}수 · 시작 칸 {seeds} · 순서 {(order.HasValue ? $"{order.Value.Succeeded}/{order.Value.Total}" : "-")}"
                + $" · 막힐 칸 {Percent(m.TrapRatio)} · 막히는 획 {Percent(m.DeadStrokeRatio)} · 조용한 막다른 길 {Percent(m.SilentRatio)} · 대충 풀림 {Percent(m.SolveChance)} · 상태 {m.States}";
            found.Add($"{{\"metrics\":\"{line}\",\"stage\":{StageWriter.ToJson(stage)}}}");
            report.AppendLine($"#{found.Count} {line}");
        }
        Directory.CreateDirectory(OutFolder);
        string path = $"{OutFolder}/{key}.json";
        File.WriteAllText(path, "[\n" + string.Join(",\n", found) + "\n]\n", new UTF8Encoding(false));
        return $"{name}({n}칸 · 칠할 칸 {paintable.Count}) 후보 {found.Count}개 / 시도 {tries}번(색 모자람 {uncovered} · 풀이 없음 {unsolved} · 수 범위 밖 {range}[{string.Join(" ", outOfRange.Select(p => $"{p.Key}수×{p.Value}"))}] · 대충 풀림 범위 밖 {chanceOut}) · {clock.ElapsedMilliseconds / 1000}초 → {path}\n{report}";
    }

    private static Stage Make(Subject subject, List<HexCoord> coords, PaintColor[] start, PaintColor[] target, int? minMoves) =>
        new Stage(subject.Name, coords.Select((c, i) => new StageCell(c, start[i], target[i])).ToList(), subject.Palette, minMoves);

    private static int Count(PaintColor[] target, PaintColor color) => target.Count(t => t == color);

    // 칠해진 칸 중 그 색의 비율
    private static double Share(PaintColor[] target, PaintColor color)
    {
        int painted = target.Count(t => t != PaintColor.Empty);
        return painted == 0 ? 0 : (double)Count(target, color) / painted;
    }

    private static string Percent(double? ratio) => ratio.HasValue ? $"{ratio.Value * 100:0}%" : "모름";
}
