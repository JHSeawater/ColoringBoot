using System.Linq;
using ColoringBoot.Game;
using UnityEditor;
using UnityEngine;

// 챕터 1 스테이지 순서 (Phase 4.5, 사용자 결정 2026-10-02) — run_script(file=AgentScripts/Build/StageOrder.cs, entry=StageOrder.SetOrder)
// StageCatalog를 이 순서로 맞춘다(다시 실행해도 같은 결과). 목록에 있는데 여기 없는 스테이지가 있으면 바꾸지 않고 알린다
public static class StageOrder
{
    private const string CatalogPath = "Assets/Data/StageCatalog.asset";
    private const string StageFolder = "Assets/Data/Stages";

    // 섞기 → 붓 색 바뀜 → 섞인 색으로 시작 → 세 기본색 → (파스텔 쉬어 가기) → 검정 → 큰 보드
    private static readonly string[] Order =
    {
        "TwoColors", "BrushChanges", "Honeycomb", "Grape", "Stain", "VioletPath",
        "MakeBlack", "Crossing", "BlackWing", "Hive", "LastStroke",
    };

    public static string SetOrder()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(CatalogPath);
        var stages = Order.Select(name => AssetDatabase.LoadAssetAtPath<TextAsset>($"{StageFolder}/{name}.json")).ToArray();
        string[] missing = Order.Where((name, i) => stages[i] == null).ToArray();
        if (missing.Length > 0) return $"파일 없음: {string.Join(", ", missing)}";
        string[] extra = catalog.Stages.Where(s => s != null && !Order.Contains(s.name)).Select(s => s.name).ToArray();
        if (extra.Length > 0) return $"순서에 없는 스테이지가 목록에 있음(바꾸지 않음): {string.Join(", ", extra)}";

        var so = new SerializedObject(catalog);
        SerializedProperty list = so.FindProperty("_stages");
        list.arraySize = stages.Length;
        for (int i = 0; i < stages.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return $"목록 {stages.Length}개: {string.Join(" → ", Order)}";
    }
}
