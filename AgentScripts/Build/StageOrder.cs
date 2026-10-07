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
        // 챕터 1: 섞기 → 붓 색 바뀜 → 섞인 색으로 시작 → 세 기본색 → (파스텔 쉬어 가기) → 검정 → 큰 보드
        new[]
        {
            "TwoColors", "BrushChanges", "Honeycomb", "Grape", "Stain", "VioletPath",
            "MakeBlack", "Crossing", "BlackWing", "Hive", "LastStroke",
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
        "LabBasket", "LabPath", "LabLeaf", "LabButterfly", "LabSun", "LabSkyHill", "LabBee", "LabGrape",
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
