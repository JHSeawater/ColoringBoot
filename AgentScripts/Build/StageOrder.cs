using System.Linq;
using ColoringBoot.Game;
using UnityEditor;
using UnityEngine;

// 챕터 스테이지 순서 (Phase 4.5, 사용자 결정 2026-10-02) — run_script(file=AgentScripts/Build/StageOrder.cs, entry=StageOrder.SetOrder, args=[챕터 번호])
// 시험 목록(?lab) 순서 — entry=StageOrder.SetLabOrder (2026-10-06) · 따라 하기 순서 — entry=StageOrder.SetTutorialOrder (2026-10-07)
// StageCatalog를 이 순서로 맞춘다(다시 실행해도 같은 결과). 목록에 있는데 여기 없는 스테이지가 있으면 바꾸지 않고 알린다
public static class StageOrder
{
    private const string LabCatalogPath = "Assets/Data/Lab/LabCatalog.asset";
    private const string LabFolder = "Assets/Data/Lab/Stages";

    // 챕터 i+1의 순서 — 데이터는 Assets/Data/Chapters/ChapterN/(StageCatalog · Stages/, 2026-10-07)
    private static readonly string[][] ChapterOrders =
    {
        // 챕터 1 (2026-10-11 다시 짝짓기 — 그림 단계 i의 소재 = i번째 스테이지): 톱니 모양(GDD §5 — 쉬운 판에서 시작, 어려운 판 뒤에 쉬어 가기)
        // 난이도 = 막히지 않는 획만 무작위로 그어 풀릴 확률(같은 날 사용자 "언덕 · 하늘이 너무 쉽다" — 끝에 있던 둘을 앞으로):
        // 바구니 100% · 잎 100 → 언덕 59 · 하늘 53 → 길 100(쉬어 가기) → 벌집 39 · 벌 33 → 해 100(쉬어 가기) → 나비 29 · 덩굴 16 · 포도 15
        // 덩굴 · 언덕 · 하늘은 소재 맵(AgentScripts/Tools/SubjectLab.cs 후보), 포도는 시험 포도(기본색에서 시작)
        // 2026-10-19 언덕 · 하늘을 "배경 + 미리 칠한 소재 칸"으로(AgentScripts/Tools/PrepaintLab.cs — 하늘 + 벌 3마리 38% · 언덕 + 바구니 28%) → 다시 톱니:
        // 바구니 100 · 잎 100 → 벌집 39 · 하늘 38 → 길 100(쉬어 가기) → 벌 33 · 언덕 28 → 해 100(쉬어 가기) → 나비 29 · 덩굴 16 · 포도 15
        // 2026-11-16 하늘의 벌 세 칸을 비워 둘 칸으로(벌은 6번 — 아직 칠하지 않은 소재 색은 미리 넣지 않는다, DECISIONS 2026-11-16). 6수 · 38% 그대로. 언덕의 바구니(1번)는 그대로
        new[]
        {
            "Basket", "Leaf", "Beehive", "Sky", "Path", "Bee",
            "Field", "Sun", "Butterfly", "Vine", "Grape",
        },
        // 챕터 2 봄 정원 (2026-11-15 — 그림 단계 i의 소재 = i번째 스테이지, 임시 챕터 2의 Temp01~11은 같은 날 지움): 톱니 모양
        // 대충 풀림(AgentScripts/Tools/ChapterSurvey.cs): 화분 100 · 하늘 100 → 잔디밭 36 · 라벤더 27 → 튤립 100(쉬어 가기) → 덤불 26 · 울타리 18
        // → 해바라기 100(쉬어 가기) → 물뿌리개 26 · 디딤돌 18 · 무당벌레 15. 평균 최소 수 6.18 · 대충 풀림 51%(챕터 1은 5.82 · 54%)
        // 화분 · 하늘 · 튤립 · 해바라기는 그림을 먼저 정한 판(SubjectLab.Picture — 해바라기 가운데 · 튤립 잎은 미리 칠한 칸), 나머지는 소재 맵(SubjectLab.Generate)
        // 2026-11-16 사용자 플레이 의견 뒤(DECISIONS 2026-11-16) 튤립(잎까지 직접 칠함) · 덤불(초록 한 색 + 무당벌레 자리 빈칸)을 그림 먼저 판으로 바꾸고 하늘의 울타리 칸을 비움 — 순서는 그대로, 역할이 바뀜:
        // 화분 100 · 하늘 100 → 잔디밭 36 · 라벤더 27 · 튤립 29 → 덤불 100(쉬어 가기 — 6수 · 순서 11%) → 울타리 18 → 해바라기 100(쉬어 가기) → 물뿌리개 26 · 디딤돌 18 · 무당벌레 15. 평균 최소 수 6.18 · 대충 풀림 52%
        new[]
        {
            "GardenPot", "GardenSky", "GardenLawn", "GardenLavender", "GardenTulip", "GardenBush",
            "GardenFence", "GardenSunflower", "GardenCan", "GardenStone", "GardenLadybug",
        },
        // 챕터 3 여름 바닷가 (2026-11-16): 톱니 모양 — 쉬어 가기는 대충 풀림 100%가 아니라 순서 성공률이 높은 판으로(챕터 2 해바라기 기록 — DevelopLog 2026-11-16)
        // 모래사장 · 해 → 하늘 · 물고기 → 갈매기(쉬어 가기) → 바다 · 모래성 → 조개(쉬어 가기) → 파라솔 · 야자수(8수) · 게. 수치는 ChapterSurvey.Table(3)
        // 해는 그림을 먼저 정한 판(SubjectLab.Picture — 햇살 여섯 칸은 미리 칠한 칸), 나머지는 소재 맵(SubjectLab.Generate)
        // 같은 날 색 규칙(DECISIONS 2026-11-16)에 맞춰 모래사장(모래 한 색 + 바다 자리 빈칸) · 하늘(하늘색 한 색 + 갈매기 자리 빈칸) · 게(빨강 + 이미 칠한 불가사리 주황)를 그림 먼저 판으로 바꿈
        // — 최소 수(5 · 6 · 7)와 톱니는 그대로, 평균 최소 수 6.27 · 대충 풀림 46%
        // 2026-11-18 파라솔을 보라 + 노랑(그림의 줄무늬 색)으로 바꿈 — 그 전은 보라 + 뒤의 바다 청록. 7수 · 대충 풀림 25%(전 22%), 평균은 그대로
        new[]
        {
            "BeachSand", "BeachSun", "BeachSky", "BeachFish", "BeachGull", "BeachSea",
            "BeachCastle", "BeachShell", "BeachUmbrella", "BeachPalm", "BeachCrab",
        },
        // 챕터 4 가을 숲길 (2026-11-16): 그림을 뒤에서 앞으로 칠하는 순서 그대로(그림 단계 순서 = 처음 나눈 순서) — 톱니 모양
        // 노을 100 → 먼 산 42 → 단풍나무 38 · 풀밭 25 → 오솔길 50(쉬어 가기 — 순서 14%) → 버섯 25 · 호박 33(8수) → 다람쥐 100(쉬어 가기 — 순서 12%)
        // → 도토리 29 · 낙엽 11(8수) · 꽃 14. 수치는 ChapterSurvey.Table(4). 색은 판마다 색 규칙(DECISIONS 2026-11-16 — 그 소재의 색 + 이미 칠한 맞닿은 것의 색):
        // 먼 산 = 보라 + 노을 분홍 · 풀밭 = 연두 + 산 보라 · 오솔길 = 살구 + 풀밭 연두, 나머지는 그 소재의 색만
        // 노을 · 도토리는 그림을 먼저 정한 판(SubjectLab.Picture — 노을의 해 두 칸은 주황을 만드는 노랑 재료), 나머지는 소재 맵(SubjectLab.Generate)
        new[]
        {
            "AutumnSky", "AutumnHill", "AutumnTree", "AutumnGrass", "AutumnPath", "AutumnMushroom",
            "AutumnPumpkin", "AutumnSquirrel", "AutumnAcorn", "AutumnLeaves", "AutumnFlower",
        },
        // 챕터 5 눈 내린 밤 (2026-11-16): 달을 먼저 칠하고 밤하늘은 그 달을 피해서 칠한다(배경 판은 앞의 소재를 칠한 뒤에 — DECISIONS 2026-11-16).
        // 그림 단계도 이 순서로 다시 나눴다(달 → 밤하늘 → 먼 산 → 별 → …). 톱니 모양 — 수치는 ChapterSurvey.Table(5):
        // 달 → 밤하늘 · 먼 산 → 별(쉬어 가기) → 연기(8수) · 지붕(8수) · 집 → 창문(쉬어 가기) → 전나무 · 눈사람(8수) · 썰매
        // 달 · 별은 노랑 한 색 그림(별은 떨어진 세 덩어리 — 붓이 빈자리를 건너간다), 밤하늘은 남색 + 미리 칠한 달 + 별 자리 빈칸(SubjectLab.Picture), 나머지는 소재 맵
        // 흰 눈은 그림에서 종이로 남는다 — 판에서는 빈칸(지붕 · 전나무 · 눈사람 · 집의 창문 자리)
        new[]
        {
            "WinterMoon", "WinterSky", "WinterMountain", "WinterStar", "WinterSmoke", "WinterRoof",
            "WinterHouse", "WinterWindow", "WinterTree", "WinterSnowman", "WinterSled",
        },
    };

    public static string SetOrder(int chapter)
    {
        if (chapter < 1 || chapter > ChapterOrders.Length) return $"챕터 {chapter}의 순서가 없음(1~{ChapterOrders.Length})";
        string[] order = ChapterOrders[chapter - 1];
        var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>($"Assets/Data/Chapters/Chapter{chapter}/StageCatalog.asset");
        var stages = order.Select(name => AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/Data/Chapters/Chapter{chapter}/Stages/{name}.json")).ToArray();
        string[] missing = order.Where((name, i) => stages[i] == null).ToArray();
        if (missing.Length > 0) return $"파일 없음: {string.Join(", ", missing)}";
        string[] extra = catalog.Stages.Where(s => s != null && !order.Contains(s.name)).Select(s => s.name).ToArray();
        if (extra.Length > 0) return $"순서에 없는 스테이지가 목록에 있음(바꾸지 않음): {string.Join(", ", extra)}";

        var so = new SerializedObject(catalog);
        SerializedProperty list = so.FindProperty("_stages");
        list.arraySize = stages.Length;
        for (int i = 0; i < stages.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return $"목록 {stages.Length}개: {string.Join(" → ", order)}";
    }

    // 시험 목록(주소 ?lab — 소재 맵 시험, 2026-10-06): 섞인 색 소재 → 무늬 → 테두리 · 색 하나 더 → 소재 묶기 → 검정 줄무늬 → 그림 맵
    private static readonly string[] LabOrder =
    {
        // 소재 맵 시험(2026-10-06) 중 챕터 1에 들어가지 않은 것
        "LabSkyHill", "LabGrape",
        // 챕터 1 다시 짝짓기(2026-10-11)로 빠진 옛 챕터 스테이지 — 챕터 2 이후 재료
        "LabTwoColors", "LabBrushChanges", "LabStain", "LabVioletPath", "LabMakeBlack", "LabBlackWing",
        "LabCrossing", "LabHive", "LabLastStroke",   // 덩굴 · 언덕 · 하늘을 새 소재 맵으로 바꾸며 빠짐(같은 날)
    };

    // 시험 목록 에셋을 이 순서로 맞춘다(없으면 만든다) — run_script(file=AgentScripts/Build/StageOrder.cs, entry=StageOrder.SetLabOrder)
    public static string SetLabOrder() => WriteCatalog(LabCatalogPath, LabFolder, LabOrder);

    // 따라 하기(튜토리얼, 2026-10-07): 레슨 i = 이 순서의 i번째 — Assets/Scripts/Game/Board/TutorialLessons.cs와 짝
    private const string TutorialCatalogPath = "Assets/Data/Tutorial/TutorialCatalog.asset";
    private const string TutorialFolder = "Assets/Data/Tutorial/Stages";
    private static readonly string[] TutorialOrder = { "TutorialBrush", "TutorialSweep", "TutorialMix", "TutorialUndo", "TutorialReview" };

    // entry=StageOrder.SetTutorialOrder
    public static string SetTutorialOrder() => WriteCatalog(TutorialCatalogPath, TutorialFolder, TutorialOrder);

    // 목록 에셋을 order 순서로 맞춘다(없으면 만든다)
    private static string WriteCatalog(string catalogPath, string folder, string[] order)
    {
        var stages = order.Select(name => AssetDatabase.LoadAssetAtPath<TextAsset>($"{folder}/{name}.json")).ToArray();
        string[] missing = order.Where((name, i) => stages[i] == null).ToArray();
        if (missing.Length > 0) return $"파일 없음: {string.Join(", ", missing)} — AgentScripts/Tools/Refresh.cs 먼저";
        var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<StageCatalog>();
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }

        var so = new SerializedObject(catalog);
        SerializedProperty list = so.FindProperty("_stages");
        list.arraySize = stages.Length;
        for (int i = 0; i < stages.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return $"목록 {stages.Length}개: {string.Join(" → ", order)} → {catalogPath}";
    }
}
