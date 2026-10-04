using System.IO;
using ColoringBoot.Game;
using UnityEditor;
using UnityEngine;

// 팔레트 에셋 (Phase 4.4 · 점검 F2, 사용자 결정 2026-09-30) — run_script(file=AgentScripts/Build/PaletteBuilder.cs, entry=PaletteBuilder.Build)
// 기본 팔레트에 이름(default)을 붙이고, 파스텔 팔레트(pastel)와 팔레트 목록(PaletteCatalog — 첫 칸 = 기본)을 만든다. 다시 실행하면 값을 덮어쓴다.
// 파스텔 색은 GDD §2.3 예시(분홍 · 레몬 · 하늘 → 살구 · 연보라 · 연두 · 차콜)를 옮긴 초안 — 휴대폰 확인 뒤 여기서 고친다
public static class PaletteBuilder
{
    private const string Folder = "Assets/Data/Palettes";
    private const string DefaultPath = Folder + "/DefaultPalette.asset";
    private const string PastelPath = Folder + "/PastelPalette.asset";
    private const string CatalogPath = "Assets/Data/PaletteCatalog.asset";

    // 값 1~7 순서: 빨강 계열 · 노랑 계열 · 주황 계열 · 파랑 계열 · 보라 계열 · 초록 계열 · 검정 계열
    private static readonly string[] PastelColors = { "#F08BA8", "#EFD95A", "#F5A870", "#7DB6EC", "#B49AE0", "#98D07C", "#4A4643" };

    public static string Build()
    {
        Directory.CreateDirectory(Folder);
        var defaultPalette = AssetDatabase.LoadAssetAtPath<ColorPalette>(DefaultPath);
        if (defaultPalette == null) return $"기본 팔레트 없음: {DefaultPath} — Phase1Assets.CreatePalette 먼저";
        SetId(defaultPalette, "default");

        ColorPalette pastel = LoadOrCreate<ColorPalette>(PastelPath);
        SetId(pastel, "pastel");
        var so = new SerializedObject(pastel);
        SerializedProperty colors = so.FindProperty("_colors");
        colors.arraySize = PastelColors.Length;
        for (int i = 0; i < PastelColors.Length; i++)
        {
            ColorUtility.TryParseHtmlString(PastelColors[i], out Color color);
            colors.GetArrayElementAtIndex(i).colorValue = color;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        PaletteCatalog catalog = LoadOrCreate<PaletteCatalog>(CatalogPath);
        var catalogSo = new SerializedObject(catalog);
        SerializedProperty list = catalogSo.FindProperty("_palettes");
        list.arraySize = 2;
        list.GetArrayElementAtIndex(0).objectReferenceValue = defaultPalette;
        list.GetArrayElementAtIndex(1).objectReferenceValue = pastel;
        catalogSo.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        return $"팔레트 목록 → {CatalogPath}: default · pastel ({PastelPath})";
    }

    private static void SetId(ColorPalette palette, string id)
    {
        var so = new SerializedObject(palette);
        so.FindProperty("_id").stringValue = id;
        so.ApplyModifiedPropertiesWithoutUndo();
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
