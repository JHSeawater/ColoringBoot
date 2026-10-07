using UnityEditor;

// 디스크에 직접 쓴 에셋을 임포트시킨다 — run_script(file=AgentScripts/Tools/Refresh.cs, entry=Refresh.All)
public static class Refresh
{
    public static string All()
    {
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/Data/Chapters/Chapter1/Stages/Grape.json") != null ? "Grape.json 임포트됨" : "Grape.json 없음";
    }
}
