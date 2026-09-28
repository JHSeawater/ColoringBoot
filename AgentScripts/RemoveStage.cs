using ColoringBoot.Game;
using UnityEditor;
using UnityEngine;

// 스테이지 하나를 목록에서 빼고 JSON 파일을 지운다 — run_script(file=AgentScripts/RemoveStage.cs, entry=RemoveStage.Remove, args=["파일 이름"])
// 지운 뒤 Phase2Font.Build → BoardSceneBuilder.BuildPrefabs → BuildEditorScene → BuildScene(그 이름에만 쓰인 글자 정리)
public static class RemoveStage
{
    public static string Remove(string file)
    {
        string path = $"Assets/Data/Stages/{file}.json";
        var stage = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (stage == null) return $"없음: {path}";

        var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>("Assets/Data/StageCatalog.asset");
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
        return $"목록 {catalog.Stages.Count}개 · 파일 삭제 {deleted}";
    }
}
