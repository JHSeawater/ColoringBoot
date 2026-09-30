using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// 한글 폰트 에셋 생성 — Pretendard SemiBold, 쓰는 글자만 담은 고정(Static) 아틀라스 (Phase 2, 사용자 결정 2026-09-28)
// 글자 출처: 씬 구성 스크립트 · Game 코드의 문자열 리터럴(주석 제외) + 스테이지 JSON의 name + ASCII.
// 문구나 스테이지 이름을 바꾸면 다시 실행한다 — run_script(file=AgentScripts/Phase2Font.cs, entry=Phase2Font.Build)
public static class Phase2Font
{
    private const string FontPath = "Assets/Art/Fonts/Pretendard-SemiBold.ttf";
    private const string AssetPath = "Assets/Art/Fonts/Pretendard SDF.asset";
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    private const int SamplingSize = 56;  // 1024² 한 장에 들어가게 (64는 254자에서 두 장이 됨 — 2026-09-30)
    private const int Padding = 6;
    private const int AtlasSize = 1024;

    private static readonly string[] _sourceFiles = { "AgentScripts/BoardSceneBuilder.cs" };
    private const string GameCodeFolder = "Assets/Scripts/Game";
    private const string StageFolder = "Assets/Data/Stages";

    public static string Build()
    {
        string characters = CollectCharacters();
        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null) return $"폰트 없음: {FontPath}";

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, SamplingSize, Padding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
        asset.name = "Pretendard SDF";
        asset.TryAddCharacters(characters, out string missing);
        asset.atlasPopulationMode = AtlasPopulationMode.Static;

        // 같은 경로에 있으면 지우고 새로 만든다 — 참조는 씬 구성 스크립트가 다시 건다
        AssetDatabase.DeleteAsset(AssetPath);
        AssetDatabase.CreateAsset(asset, AssetPath);
        foreach (Texture2D atlas in asset.atlasTextures)
        {
            atlas.name = "Pretendard SDF Atlas";
            AssetDatabase.AddObjectToAsset(atlas, asset);
        }
        asset.material.name = "Pretendard SDF Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);

        // 고정 아틀라스는 런타임에 원본 폰트가 필요 없다 — 참조를 비워 TTF(2.6 MB)가 빌드에 딸려 가지 않게
        var so = new SerializedObject(asset);
        so.FindProperty("m_SourceFontFile").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();

        // TMP 기본 폰트로 지정 — 새로 만드는 TMP 글자도 한글이 나오게
        var settings = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(TmpSettingsPath));
        settings.FindProperty("m_defaultFontAsset").objectReferenceValue = asset;
        settings.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        return $"글자 {characters.Length}개 · 아틀라스 {asset.atlasTextures.Length}장({AtlasSize}²) · 빠진 글자 [{missing}] → {AssetPath}";
    }

    private static string CollectCharacters()
    {
        var set = new SortedSet<char>();
        for (char c = ' '; c <= '~'; c++) set.Add(c);

        var literal = new Regex("\"((?:[^\"\\\\]|\\\\.)*)\"");
        IEnumerable<string> code = _sourceFiles.Concat(Directory.GetFiles(GameCodeFolder, "*.cs"));
        foreach (string file in code)
        {
            foreach (string line in File.ReadAllLines(file, Encoding.UTF8))
            {
                if (line.Contains("Debug.Log") || line.Contains("Exception(")) continue; // 콘솔 로그 · 예외 메시지는 화면에 안 나온다
                string body = line.Split(new[] { "//" }, System.StringSplitOptions.None)[0]; // 줄 주석 제외
                foreach (Match m in literal.Matches(body)) Add(set, m.Groups[1].Value);
            }
        }

        var name = new Regex("\"name\"\\s*:\\s*\"([^\"]*)\"");
        foreach (string file in Directory.GetFiles(StageFolder, "*.json"))
            Add(set, name.Match(File.ReadAllText(file, Encoding.UTF8)).Groups[1].Value);

        return new string(set.ToArray());
    }

    private static void Add(SortedSet<char> set, string text)
    {
        foreach (char c in text)
        {
            if (!char.IsControl(c)) set.Add(c);
        }
    }
}
