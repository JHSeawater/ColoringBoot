using System.Linq;
using System.Text;
using ColoringBoot.Game;
using UnityEditor;

// 임시 챕터 2 (2026-10-11 사용자 결정 — 진짜 챕터 2 전에 여러 챕터 흐름을 확인, 게임에는 주소에 ?chapters가 있을 때만 들어감 · GameFlow._testChapters)
// 스테이지 = 시험 목록의 옛 챕터 판을 Temp01~11로 복사(파일 이름 = 진행 기록의 키라 시험 목록과 겹치지 않게 — 내용 · 화면 이름은 그대로),
// 그림 = 임시 도안(ArtSource/_archive/Chapter1-temp — 옛 챕터 1 순서로 그린 것이라 단계 소재가 옛 판과 맞음. 3번 벌은 옛 벌집 판이 챕터 1로 가서 하늘과 언덕)
// 실행: python AgentScripts/Build/ChapterArtExport.py ArtSource/_archive/Chapter1-temp Assets/Art/Chapters/Chapter2
//   → run_script(file=AgentScripts/Setup/Phase8TempChapter2.cs, entry=Phase8TempChapter2.Apply) → ChapterArtBuilder.Build(2) → Apply 다시(그림 연결) → StageOrder.SetOrder(2)
// 진짜 챕터 2를 만들 때 이 폴더를 갈아 끼우고 BoardSceneBuilder의 챕터 목록으로 옮긴다
public static class Phase8TempChapter2
{
    private const string Folder = "Assets/Data/Chapters/Chapter2";
    private const string Title = "챕터 2";
    private const string Subtitle = "임시 그림";

    // 그림 단계 순서(해 · 구름 · 벌 · 포도 · 언덕 · 길 · 덩굴 · 울타리 · 나비 · 벌집 · 하늘)대로 — Temp01 = 첫째
    private static readonly string[] LabSources =
    {
        "LabTwoColors", "LabBrushChanges", "LabSkyHill", "LabGrape", "LabStain", "LabVioletPath",
        "LabMakeBlack", "LabCrossing", "LabBlackWing", "LabHive", "LabLastStroke",
    };

    public static string Apply()
    {
        var log = new StringBuilder();
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Data/Chapters", "Chapter2");
        if (!AssetDatabase.IsValidFolder(Folder + "/Stages")) AssetDatabase.CreateFolder(Folder, "Stages");
        for (int i = 0; i < LabSources.Length; i++)
        {
            string to = $"{Folder}/Stages/Temp{i + 1:D2}.json";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(to) != null) continue;
            if (!AssetDatabase.CopyAsset($"Assets/Data/Lab/Stages/{LabSources[i]}.json", to)) return $"복사 실패: {LabSources[i]} → {to}\n{log}";
            log.AppendLine($"{LabSources[i]} → {to}");
        }

        var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>($"{Folder}/StageCatalog.asset");
        if (catalog == null)
        {
            catalog = UnityEngine.ScriptableObject.CreateInstance<StageCatalog>();
            AssetDatabase.CreateAsset(catalog, $"{Folder}/StageCatalog.asset");
            log.AppendLine("StageCatalog 만듦(순서는 StageOrder.SetOrder(2))");
        }
        var chapter = AssetDatabase.LoadAssetAtPath<Chapter>($"{Folder}/Chapter.asset");
        if (chapter == null)
        {
            chapter = UnityEngine.ScriptableObject.CreateInstance<Chapter>();
            AssetDatabase.CreateAsset(chapter, $"{Folder}/Chapter.asset");
        }
        var art = AssetDatabase.LoadAssetAtPath<ChapterArt>($"{Folder}/ChapterArt.asset");
        var so = new SerializedObject(chapter);
        so.FindProperty("_title").stringValue = Title;
        so.FindProperty("_subtitle").stringValue = Subtitle;
        so.FindProperty("_stages").objectReferenceValue = catalog;
        so.FindProperty("_art").objectReferenceValue = art;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        log.Append($"Chapter.asset: {Title} · {Subtitle} · 그림 {(art != null ? "연결" : "아직 없음 — ChapterArtBuilder.Build(2) 뒤 다시 실행")}");
        return log.ToString();
    }
}
