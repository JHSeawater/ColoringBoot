using System.IO;
using System.Linq;
using ColoringBoot.Game;
using UnityEditor;
using UnityEngine;

// 스테이지 하나를 그 챕터 목록에서 빼고 JSON 파일을 지운다 — run_script(file=AgentScripts/Tools/RemoveStage.cs, entry=RemoveStage.Remove, args=["파일 이름"])
// 챕터 폴더(Assets/Data/Chapters/ChapterN/Stages)에서 파일 이름으로 찾는다 — 파일 이름은 챕터끼리 겹치지 않는다(ContentRegressionTests)
// 지운 뒤 FontBuilder.Build → BoardSceneBuilder.BuildPrefabs → BuildEditorScene → BuildScene(그 이름에만 쓰인 글자 정리)
public static class RemoveStage
{
    private const string ChaptersFolder = "Assets/Data/Chapters";

    public static string Remove(string file)
    {
        string path = AssetDatabase.FindAssets($"{file} t:TextAsset", new[] { ChaptersFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == file && p.EndsWith(".json"));
        if (path == null) return $"없음: {ChaptersFolder}/*/Stages/{file}.json";
        var stage = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

        // Stages/의 부모 = 챕터 폴더 — 그 챕터의 목록에서 뺀다
        string chapterFolder = Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace(Path.DirectorySeparatorChar, '/');
        var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>($"{chapterFolder}/StageCatalog.asset");
        var so = new SerializedObject(catalog);
        SerializedProperty stages = so.FindProperty("_stages");
        for (int i = stages.arraySize - 1; i >= 0; i--)
        {
            if (stages.GetArrayElementAtIndex(i).objectReferenceValue != stage) continue;
            stages.GetArrayElementAtIndex(i).objectReferenceValue = null;
            stages.DeleteArrayElementAtIndex(i);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        bool deleted = AssetDatabase.DeleteAsset(path);
        return $"{chapterFolder} 목록 {catalog.Stages.Count}개 · 파일 삭제 {deleted}";
    }
}
