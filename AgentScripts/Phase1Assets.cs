using System.IO;
using UnityEditor;
using UnityEngine;
using ColoringBoot.Game;

// Phase 1.2 에셋 준비 — run_script(file=AgentScripts/Phase1Assets.cs, entry=...)
// ImportTmp: TMP Essential Resources를 대화상자 없이 임포트(메뉴로 하면 임포트 창이 떠서 MCP가 멈춘다)
// CreatePalette: 기본 팔레트(프로토타입 라이트 테마 --p1 ~ --p7). 다시 실행하면 값을 덮어쓴다
public static class Phase1Assets
{
    private const string TmpPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
    private const string PalettePath = "Assets/Data/Palettes/DefaultPalette.asset";

    public static string ImportTmp()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return "이미 임포트됨";
        UnityEditor.AssetPackage.Package.Import(TmpPackage, false); // 임포트가 끝날 때까지 1분 넘게 메인 스레드를 잡아 MCP가 시간 초과할 수 있다(2026-09-28)
        return "임포트 요청함 — Assets/TextMesh Pro 생성을 확인할 것";
    }

    public static string CreatePalette()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PalettePath));
        var palette = AssetDatabase.LoadAssetAtPath<ColorPalette>(PalettePath);
        if (palette == null)
        {
            palette = ScriptableObject.CreateInstance<ColorPalette>();
            AssetDatabase.CreateAsset(palette, PalettePath);
        }

        // 값 1~7: 빨강 · 노랑 · 주황 · 파랑 · 보라 · 초록 · 검정
        string[] hex = { "#D8402F", "#F1B928", "#EE7E27", "#2D69CF", "#8A46B3", "#2B9D57", "#25262D" };
        var so = new SerializedObject(palette);
        SerializedProperty colors = so.FindProperty("_colors");
        colors.arraySize = hex.Length;
        for (int i = 0; i < hex.Length; i++)
        {
            ColorUtility.TryParseHtmlString(hex[i], out Color color);
            colors.GetArrayElementAtIndex(i).colorValue = color;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return $"팔레트 → {PalettePath}";
    }
}
