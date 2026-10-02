using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ColoringBoot.Game;
using UnityEditor;
using UnityEngine;

// 챕터 그림 에셋 (Phase 5.3) — 먼저 python AgentScripts/ChapterArtExport.py(원본 → 게임 크기 · 여백 자르기 · layout.json)
// run_script(file=AgentScripts/ChapterArtBuilder.cs, entry=ChapterArtBuilder.Build). 다시 실행하면 같은 결과로 덮어쓴다
public static class ChapterArtBuilder
{
    private const string Folder = "Assets/Art/Chapters/Chapter1";
    private const string AssetPath = "Assets/Data/Chapter1Art.asset";

    public static string Build()
    {
        AssetDatabase.Refresh();
        Layout layout = Layout.Read(File.ReadAllText($"{Folder}/layout.json"));
        foreach (Entry entry in layout.steps.Prepend(layout.line))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath($"{Folder}/{entry.file}");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        var art = AssetDatabase.LoadAssetAtPath<ChapterArt>(AssetPath);
        if (art == null)
        {
            art = ScriptableObject.CreateInstance<ChapterArt>();
            AssetDatabase.CreateAsset(art, AssetPath);
        }
        var so = new SerializedObject(art);
        so.FindProperty("_canvas").vector2IntValue = new Vector2Int(layout.width, layout.height);
        SetLayer(so.FindProperty("_line"), layout.line);
        SerializedProperty steps = so.FindProperty("_steps");
        steps.arraySize = layout.steps.Length;
        for (int i = 0; i < layout.steps.Length; i++) SetLayer(steps.GetArrayElementAtIndex(i), layout.steps[i]);
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return $"{AssetPath}: 캔버스 {layout.width}×{layout.height} · 선화 + 단계 {layout.steps.Length}개";
    }

    private static void SetLayer(SerializedProperty layer, Entry entry)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{entry.file}")
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
