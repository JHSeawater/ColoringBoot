using System.IO;
using System.Linq;
using System.Text;
using ColoringBoot.Core;
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

    // SubjectLab 후보 하나를 챕터의 스테이지 파일로 넣는다 — args=[챕터 번호, "후보 파일 키"(Builds/SubjectLab/<키>.json), 후보 번호(1~), "파일 이름"]
    // 레벨 에디터 저장과 같은 형식(StageWriter JSON + 줄바꿈, minMoves는 후보에 든 솔버 값 — 여기서 다시 풀어 확인한다). 목록 등록은 StageOrder.SetOrder(N).
    // 파일 이름은 진행 기록의 키 — Assets/Data 전체에서 겹치면 넣지 않는다(대소문자 무시). 같은 챕터의 같은 이름은 덮어쓴다(후보를 바꿀 때)
    public static string AddStage(int chapter, string key, int index, string fileName)
    {
        string source = $"Builds/SubjectLab/{key}.json";
        if (!File.Exists(source)) return $"후보 파일 없음: {source}";
        string[] lines = File.ReadAllLines(source).Where(l => l.StartsWith("{")).ToArray();
        if (index < 1 || index > lines.Length) return $"후보 {index}번 없음(1~{lines.Length})";
        string line = lines[index - 1].TrimEnd(',');
        const string marker = "\"stage\":";
        string json = line.Substring(line.IndexOf(marker) + marker.Length);
        json = json.Substring(0, json.Length - 1);   // 후보 줄을 닫는 중괄호

        Stage stage = Stage.Parse(json);
        var board = new Board(stage);
        SolveResult solved = Solver.Solve(board, board.CreateStartState());
        if (!solved.Solved || stage.MinMoves != solved.Path.Count) return $"풀이 확인 실패: minMoves {stage.MinMoves} · 솔버 {(solved.Solved ? solved.Path.Count : -1)}";

        string folder = $"Assets/Data/Chapters/Chapter{chapter}/Stages";
        string path = $"{folder}/{fileName}.json";
        string other = Directory.GetFiles("Assets/Data", "*.json", SearchOption.AllDirectories)
            .Select(f => f.Replace(Path.DirectorySeparatorChar, '/'))
            .FirstOrDefault(f => string.Equals(Path.GetFileNameWithoutExtension(f), fileName, System.StringComparison.OrdinalIgnoreCase)
                && !string.Equals(f, path, System.StringComparison.OrdinalIgnoreCase));
        if (other != null) return $"'{other}'와 파일 이름이 겹침(진행 기록 키)";
        File.WriteAllText(path, json + "\n", new UTF8Encoding(false));
        AssetDatabase.ImportAsset(path);
        return $"{path}: {stage.Name} · {stage.Cells.Count}칸 · {stage.Palette ?? "default"} · 최소 {stage.MinMoves}수 ← {key}#{index}";
    }

    // 여러 개를 한 번에 — args=[챕터 번호, "키:번호:파일 이름,키:번호:파일 이름,…"]
    public static string AddStages(int chapter, string picks) =>
        string.Join("\n", picks.Split(',').Select(p => p.Split(':')).Select(p => AddStage(chapter, p[0], int.Parse(p[1]), p[2])));

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
