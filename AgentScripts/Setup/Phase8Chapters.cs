using ColoringBoot.Game;
using UnityEditor;
using UnityEngine;

// 챕터 2~5 데이터 만들기 (2026-11-15, 계획 Docs/plans/2026-11-14-chapters-2-5.md 5~8단계 ①)
// run_script(file=AgentScripts/Setup/Phase8Chapters.cs, entry=Phase8Chapters.Create, args=[챕터 번호, "부제"])
//   — Assets/Data/Chapters/ChapterN/ 에 Stages/ 폴더 · StageCatalog · ChapterArt · Chapter(제목 "챕터 N" · 부제 · 목록 · 그림)를 만든다.
//   이미 있는 에셋은 그대로 두고(GUID 유지) 제목 · 부제 · 연결만 맞춘다 — 다시 실행해도 같은 결과.
// 다음: 스테이지를 만들고(레벨 에디터 BuildEditorScene(N)) → StageOrder.SetOrder(N) → 그림 ChapterArtExport · ChapterArtBuilder.Build(N)
//   → BoardSceneBuilder.Chapters에 N을 더하고 FontBuilder.Build → BuildPrefabs → BuildScene
public static class Phase8Chapters
{
    public static string Create(int chapter, string subtitle)
    {
        if (chapter < 2) return "챕터 1은 Phase8ChapterFolders.cs가 만들었다 — 2 이상만";
        if (string.IsNullOrWhiteSpace(subtitle)) return "부제가 비어 있음";
        string folder = $"Assets/Data/Chapters/Chapter{chapter}";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Data/Chapters", $"Chapter{chapter}");
        if (!AssetDatabase.IsValidFolder(folder + "/Stages")) AssetDatabase.CreateFolder(folder, "Stages");

        StageCatalog catalog = LoadOrCreate<StageCatalog>(folder + "/StageCatalog.asset");
        ChapterArt art = LoadOrCreate<ChapterArt>(folder + "/ChapterArt.asset");
        Chapter asset = LoadOrCreate<Chapter>(folder + "/Chapter.asset");
        var so = new SerializedObject(asset);
        so.FindProperty("_title").stringValue = $"챕터 {chapter}";
        so.FindProperty("_subtitle").stringValue = subtitle;
        so.FindProperty("_stages").objectReferenceValue = catalog;
        so.FindProperty("_art").objectReferenceValue = art;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return $"{folder}: '{asset.Title}' · '{asset.Subtitle}' · 목록 {catalog.Stages.Count}개 · 그림 단계 {art.Steps.Count}개";
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
