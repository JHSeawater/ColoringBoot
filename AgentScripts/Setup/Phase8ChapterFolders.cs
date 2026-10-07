using System.Collections.Generic;
using System.Linq;
using ColoringBoot.Game;
using UnityEditor;
using UnityEngine;

// 데이터를 챕터 단위 폴더로 옮김 (Phase 8 구조 정리, 2026-10-07 앞당겨 함) — AssetDatabase.MoveAsset이라 GUID · 씬 참조 · 파일 이름(진행 기록의 키)은 그대로.
//   Data/Chapters/Chapter1/  Stages/*.json · StageCatalog · ChapterArt(옛 Chapter1Art) · Chapter(새 — 제목 · 부제 · 목록 · 그림)
//   Data/Tutorial/  Stages/*.json · TutorialCatalog      Data/Lab/  Stages/*.json · LabCatalog
//   Data/Palettes/  + PaletteCatalog                     Data/Settings/  UiTheme · MotionSettings
// Preview(읽기 전용 — 옮길 목록) → Apply. 옮긴 뒤 빌더 경로 상수는 각 빌더에 반영되어 있다
public static class Phase8ChapterFolders
{
    private const string Data = "Assets/Data";
    private const string Chapter1 = Data + "/Chapters/Chapter1";
    private const string ChapterTitle = "챕터 1";
    private const string ChapterSubtitle = "포도밭 오후";

    private static List<(string from, string to)> Moves()
    {
        var moves = new List<(string, string)>();
        foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { Data + "/Stages" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".json")) continue;
            string file = path.Substring(path.LastIndexOf('/') + 1);
            string folder = path.Contains("/Stages/Tutorial/") ? Data + "/Tutorial/Stages"
                : path.Contains("/Stages/Lab/") ? Data + "/Lab/Stages"
                : Chapter1 + "/Stages";
            moves.Add((path, $"{folder}/{file}"));
        }
        moves.Add((Data + "/StageCatalog.asset", Chapter1 + "/StageCatalog.asset"));
        moves.Add((Data + "/Chapter1Art.asset", Chapter1 + "/ChapterArt.asset"));
        moves.Add((Data + "/TutorialCatalog.asset", Data + "/Tutorial/TutorialCatalog.asset"));
        moves.Add((Data + "/LabCatalog.asset", Data + "/Lab/LabCatalog.asset"));
        moves.Add((Data + "/PaletteCatalog.asset", Data + "/Palettes/PaletteCatalog.asset"));
        moves.Add((Data + "/UiTheme.asset", Data + "/Settings/UiTheme.asset"));
        moves.Add((Data + "/MotionSettings.asset", Data + "/Settings/MotionSettings.asset"));
        return moves;
    }

    public static string Preview()
    {
        List<(string from, string to)> moves = Moves();
        string[] missing = moves.Where(m => AssetDatabase.LoadMainAssetAtPath(m.from) == null).Select(m => m.from).ToArray();
        return $"옮길 것 {moves.Count}개(없는 것 {missing.Length}: {string.Join(", ", missing)}) + 새 {Chapter1}/Chapter.asset ('{ChapterTitle}' · '{ChapterSubtitle}')\n" +
               string.Join("\n", moves.Select(m => $"{m.from} → {m.to}"));
    }

    public static string Apply()
    {
        List<(string from, string to)> moves = Moves();
        var errors = new List<string>();
        foreach ((string from, string to) in moves)
        {
            EnsureFolder(to.Substring(0, to.LastIndexOf('/')));
            string error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error)) errors.Add($"{from}: {error}");
        }
        // 비게 된 옛 스테이지 폴더(Stages/Lab · Stages/Tutorial · Stages) 정리
        foreach (string folder in new[] { Data + "/Stages/Lab", Data + "/Stages/Tutorial", Data + "/Stages" })
        {
            if (AssetDatabase.IsValidFolder(folder) && AssetDatabase.FindAssets("", new[] { folder }).Length == 0) AssetDatabase.DeleteAsset(folder);
        }

        var chapter = ScriptableObject.CreateInstance<Chapter>();
        AssetDatabase.CreateAsset(chapter, Chapter1 + "/Chapter.asset");
        var so = new SerializedObject(chapter);
        so.FindProperty("_title").stringValue = ChapterTitle;
        so.FindProperty("_subtitle").stringValue = ChapterSubtitle;
        so.FindProperty("_stages").objectReferenceValue = AssetDatabase.LoadAssetAtPath<StageCatalog>(Chapter1 + "/StageCatalog.asset");
        so.FindProperty("_art").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ChapterArt>(Chapter1 + "/ChapterArt.asset");
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return $"옮김 {moves.Count - errors.Count}/{moves.Count} · 챕터 에셋 목록 {chapter.Stages?.Stages.Count} · 그림 단계 {chapter.Art?.Steps.Count}" +
               (errors.Count > 0 ? "\n오류: " + string.Join("\n", errors) : "");
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = folder.Substring(0, folder.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder.Substring(folder.LastIndexOf('/') + 1));
    }
}
