using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ColoringBoot.Game;
using UnityEditor;
using UnityEngine;

// 챕터 그림 에셋 (Phase 5.3) — 먼저 python AgentScripts/Build/ChapterArtExport.py(원본 → 게임 크기 · 여백 자르기 · layout.json)
// run_script(file=AgentScripts/Build/ChapterArtBuilder.cs, entry=ChapterArtBuilder.Build, args=[챕터 번호]). 다시 실행하면 같은 결과로 덮어쓴다
// 그림 조각은 Assets/Art/Chapters/ChapterN/, 에셋은 Assets/Data/Chapters/ChapterN/ChapterArt.asset (챕터 번호 인자 — 2026-10-07)
public static class ChapterArtBuilder
{
    public static string Build(int chapter)
    {
        string folder = $"Assets/Art/Chapters/Chapter{chapter}";
        string assetPath = $"Assets/Data/Chapters/Chapter{chapter}/ChapterArt.asset";
        AssetDatabase.Refresh();
        Layout layout = Layout.Read(File.ReadAllText($"{folder}/layout.json"));
        foreach (Entry entry in layout.steps.Prepend(layout.line))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath($"{folder}/{entry.file}");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            // WebGL은 ASTC(2026-10-11 사용자 결정): 휴대폰(앱인토스)이 그대로 써 메모리가 줄고 불러올 때 압축을 풀지 않는다 — PC 브라우저는 ASTC를 못 써 불러올 때 푼다.
            // 선화는 선이 뭉개지지 않게 4×4, 색칠 단계는 6×6. UI 스프라이트(SpriteBuilder)는 압축하지 않는다
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "WebGL",
                overridden = true,
                maxTextureSize = 2048,
                format = entry == layout.line ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6,
            });
            importer.SaveAndReimport();
        }

        var art = AssetDatabase.LoadAssetAtPath<ChapterArt>(assetPath);
        if (art == null)
        {
            art = ScriptableObject.CreateInstance<ChapterArt>();
            AssetDatabase.CreateAsset(art, assetPath);
        }
        var so = new SerializedObject(art);
        so.FindProperty("_canvas").vector2IntValue = new Vector2Int(layout.width, layout.height);
        SetLayer(so.FindProperty("_line"), layout.line, folder);
        SerializedProperty steps = so.FindProperty("_steps");
        steps.arraySize = layout.steps.Length;
        for (int i = 0; i < layout.steps.Length; i++) SetLayer(steps.GetArrayElementAtIndex(i), layout.steps[i], folder);
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return $"{assetPath}: 캔버스 {layout.width}×{layout.height} · 선화 + 단계 {layout.steps.Length}개";
    }

    private static void SetLayer(SerializedProperty layer, Entry entry, string folder)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{entry.file}")
            ?? throw new InvalidOperationException($"{entry.file} 스프라이트 없음");
        layer.FindPropertyRelative("_sprite").objectReferenceValue = sprite;
        layer.FindPropertyRelative("_rect").rectIntValue = new RectInt(entry.x, entry.y, entry.w, entry.h);
    }

    // layout.json (ChapterArtExport.py) — run_script 임시 어셈블리의 타입은 JsonUtility가 채우지 않아(2026-10-03 실측: 모든 필드가 비어 있음) 정규식으로 읽는다
    private sealed class Layout
    {
        public int width, height;
        public Entry line;
        public Entry[] steps;

        public static Layout Read(string json)
        {
            Entry[] entries = Regex.Matches(json, "\"file\":\\s*\"([^\"]+)\",\\s*\"x\":\\s*(-?\\d+),\\s*\"y\":\\s*(-?\\d+),\\s*\"w\":\\s*(\\d+),\\s*\"h\":\\s*(\\d+)")
                .Cast<Match>()
                .Select(m => new Entry { file = m.Groups[1].Value, x = int.Parse(m.Groups[2].Value), y = int.Parse(m.Groups[3].Value), w = int.Parse(m.Groups[4].Value), h = int.Parse(m.Groups[5].Value) })
                .ToArray();
            return new Layout
            {
                width = int.Parse(Regex.Match(json, "\"width\":\\s*(\\d+)").Groups[1].Value),
                height = int.Parse(Regex.Match(json, "\"height\":\\s*(\\d+)").Groups[1].Value),
                line = entries.Single(e => e.file.StartsWith("00_")),
                steps = entries.Where(e => !e.file.StartsWith("00_")).OrderBy(e => e.file, StringComparer.Ordinal).ToArray(),
            };
        }
    }

    private sealed class Entry
    {
        public string file;
        public int x, y, w, h;
    }
}
