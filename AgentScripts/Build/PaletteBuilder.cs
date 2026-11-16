using System.IO;
using ColoringBoot.Game;
using UnityEditor;
using UnityEngine;

// 팔레트 에셋 (Phase 4.4 · 점검 F2, 사용자 결정 2026-09-30) — run_script(file=AgentScripts/Build/PaletteBuilder.cs, entry=PaletteBuilder.Build)
// 기본 팔레트에 이름(default)을 붙이고, 파스텔 팔레트(pastel)와 팔레트 목록(PaletteCatalog — 첫 칸 = 기본)을 만든다. 다시 실행하면 값을 덮어쓴다.
// 파스텔 색은 GDD §2.3 예시(분홍 · 레몬 · 하늘 → 살구 · 연보라 · 연두 · 차콜)를 옮긴 초안 — 휴대폰 확인 뒤 여기서 고친다
// 정원 · 봄볕은 챕터 2 그림의 소재 색에 맞춘 팔레트(2026-11-13, DECISIONS) — 색을 바꾸면 QaScene의 팔레트 색 검사(CIEDE2000 20 이상 · 7번이 가장 어두움)를 돌린다
public static class PaletteBuilder
{
    private const string Folder = "Assets/Data/Palettes";
    private const string DefaultPath = Folder + "/DefaultPalette.asset";
    private const string CatalogPath = Folder + "/PaletteCatalog.asset";

    // 기본 다음 칸부터 목록 순서 — (이름, 에셋 파일, 값 1~7: 빨강 계열 · 노랑 계열 · 주황 계열 · 파랑 계열 · 보라 계열 · 초록 계열 · 검정 계열)
    private static readonly (string id, string file, string[] colors)[] Palettes =
    {
        ("pastel", "PastelPalette", new[] { "#F08BA8", "#EFD95A", "#F5A870", "#7DB6EC", "#B49AE0", "#98D07C", "#4A4643" }),
        ("garden", "GardenPalette", new[] { "#F23B2E", "#FAD80F", "#F98E1C", "#2A7FF0", "#B462D8", "#4DBA1C", "#6E3515" }),
        ("spring", "SpringPalette", new[] { "#FB7499", "#FFF48C", "#F9B25A", "#5EC2F8", "#C7A0EC", "#7EF224", "#857A75" }),
        // 챕터 3 여름 바닷가(2026-11-16 — 그림 ArtSource/Chapter3 소재 색에 맞춤, CIEDE2000 20 이상 · 7번이 가장 어두움)
        // 바닷가: 게 빨강 · 해 노랑 · 불가사리 주황 · 바다 청록 · 파라솔 보라 · 야자 잎 초록 · 야자 줄기 갈색
        ("beach", "BeachPalette", new[] { "#FD4A32", "#FED404", "#FE8C02", "#01A6BB", "#BC62D3", "#0F9723", "#8A4B1E" }),
        // 물가: 조개 분홍 · 모래 · 모래성 · 하늘 · 연보라 · 얕은 물(민트) · 갈매기 회색(7번이라 그림보다 어둡게)
        ("shore", "ShorePalette", new[] { "#FCB0DE", "#F3DA8A", "#F0962A", "#8ED3FB", "#A98BEF", "#66DFC0", "#7D7B78" }),
        // 챕터 4 가을 숲길(2026-11-16 — 그림 ArtSource/Chapter4 소재 색에 맞춤, 같은 기준)
        // 단풍: 단풍 · 버섯 갓 빨강 · 노란 단풍 · 호박 주황 · 파랑(그림에 없음 — 섞는 재료) · 꽃 자주 · 덤불 · 줄기 초록 · 나무 줄기 갈색
        ("autumn", "AutumnPalette", new[] { "#FC3128", "#FED216", "#FD8A03", "#4A74E8", "#D04ECF", "#65B924", "#8A4017" }),
        // 노을: 노을 분홍 · 오솔길 살구 · 다람쥐 주황 · 푸른빛(섞는 재료) · 먼 산 보라 · 풀밭 연두 · 도토리 갓 밤색(7번이라 그림보다 어둡게)
        ("dusk", "DuskPalette", new[] { "#FB7982", "#F8CC85", "#E06426", "#7FA6F2", "#8E67BE", "#C6E21C", "#7A3A14" }),
    };

    public static string Build()
    {
        Directory.CreateDirectory(Folder);
        var defaultPalette = AssetDatabase.LoadAssetAtPath<ColorPalette>(DefaultPath);
        if (defaultPalette == null) return $"기본 팔레트 없음: {DefaultPath} — Phase1Assets.CreatePalette 먼저";
        SetId(defaultPalette, "default");

        PaletteCatalog catalog = LoadOrCreate<PaletteCatalog>(CatalogPath);
        var catalogSo = new SerializedObject(catalog);
        SerializedProperty list = catalogSo.FindProperty("_palettes");
        list.arraySize = 1 + Palettes.Length;
        list.GetArrayElementAtIndex(0).objectReferenceValue = defaultPalette;

        for (int p = 0; p < Palettes.Length; p++)
        {
            ColorPalette palette = LoadOrCreate<ColorPalette>($"{Folder}/{Palettes[p].file}.asset");
            SetId(palette, Palettes[p].id);
            var so = new SerializedObject(palette);
            SerializedProperty colors = so.FindProperty("_colors");
            colors.arraySize = Palettes[p].colors.Length;
            for (int i = 0; i < Palettes[p].colors.Length; i++)
            {
                ColorUtility.TryParseHtmlString(Palettes[p].colors[i], out Color color);
                colors.GetArrayElementAtIndex(i).colorValue = color;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            list.GetArrayElementAtIndex(1 + p).objectReferenceValue = palette;
        }
        catalogSo.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        return $"팔레트 목록 → {CatalogPath}: default · {string.Join(" · ", System.Array.ConvertAll(Palettes, x => x.id))}";
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
