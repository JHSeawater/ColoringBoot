using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// 한글 폰트 에셋 생성 — 동글(Dongle) Bold, 쓰는 글자만 담은 고정(Static) 아틀라스 (Phase 2 사용자 결정 2026-09-28 · 폰트는 2026-11-23 Pretendard SemiBold → 동글, 사용자)
// 글자 출처: 씬 구성 스크립트 · Game 코드의 문자열 리터럴(주석 제외) + 스테이지 JSON의 name + 챕터 제목 · 부제(Chapter 에셋) + ASCII.
// 문구나 스테이지 이름을 바꾸면 다시 실행한다 — run_script(file=AgentScripts/Build/FontBuilder.cs, entry=FontBuilder.Build)
public static class FontBuilder
{
    private const string FontPath = "Assets/Art/Fonts/Dongle-Bold.ttf";
    private const string AssetPath = "Assets/Art/Fonts/Dongle SDF.asset";
    private const string AssetName = "Dongle SDF";
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    // 동글은 em 안에서 한글이 작다(높이 0.35em · 폭 0.45em — Pretendard는 0.88 · 0.83). 아틀라스 속 글자 픽셀이 Pretendard 56일 때와 비슷하게 84,
    // 패딩도 같은 비율(옛 56 · 6 — 64는 254자에서 두 장이 됨, 2026-09-30)
    private const int SamplingSize = 84;
    private const int Padding = 9;
    // 화면 글자 배율 — 코드 · 씬의 글자 크기는 그대로 두고 폰트 에셋이 키운다(TMP FaceInfo.scale). 1.75면 줄 길이가 Pretendard와 거의 같다
    // (1.5는 버튼 글자가 작아 보였다 — 2026-11-23 에디터 캡처)
    private const float FaceScale = 1.75f;
    // 줄 위 · 아래 선(포인트 크기 배수) = 폰트의 OS/2 typo 값. 동글은 받침 글자가 기준선 아래로 내려간다(돌 -0.40 ~ 리 0.53em) — 이 범위의 가운데가 줄 가운데.
    // 폰트 에셋 기본 값(hhea 0.85 · -0.6)이면 가운데 정렬한 글자가 위로 뜨고, 받침만 보고 좁힌 값(0.69 · -0.17)은 받침이 아래 줄과 겹쳤다(2026-11-23)
    private const float AscentLine = 0.57f;
    private const float DescentLine = -0.43f;
    private const int AtlasSize = 1024;

    private static readonly string[] _sourceFiles = { "AgentScripts/Build/BoardSceneBuilder.cs" };
    // 아래 두 폴더는 하위 폴더까지 읽는다(2026-10-05 — Game 스크립트를 하위 폴더로 나눔 · 2026-10-07 챕터 · 따라 하기 · 시험 목록 폴더)
    private const string GameCodeFolder = "Assets/Scripts/Game";
    private const string StageFolder = "Assets/Data";

    // 읽기 전용 — 지금 코드 · 스테이지에서 모을 글자만 돌려준다(폰트 에셋은 그대로). 폴더를 옮기기 전후 비교용
    public static string Preview()
    {
        string characters = CollectCharacters();
        return $"글자 {characters.Length}개: {characters}";
    }

    public static string Build()
    {
        string characters = CollectCharacters();
        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null) return $"폰트 없음: {FontPath}";

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, SamplingSize, Padding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
        asset.name = AssetName;
        asset.TryAddCharacters(characters, out string missing);
        asset.atlasPopulationMode = AtlasPopulationMode.Static;

        // 같은 경로에 있으면 지우고 새로 만든다 — 참조는 씬 구성 스크립트가 다시 건다
        AssetDatabase.DeleteAsset(AssetPath);
        AssetDatabase.CreateAsset(asset, AssetPath);
        foreach (Texture2D atlas in asset.atlasTextures)
        {
            atlas.name = AssetName + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, asset);
        }
        asset.material.name = AssetName + " Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);

        // 고정 아틀라스는 런타임에 원본 폰트가 필요 없다 — 참조를 비워 TTF(4.5 MB)가 빌드에 딸려 가지 않게
        var so = new SerializedObject(asset);
        so.FindProperty("m_SourceFontFile").objectReferenceValue = null;
        so.FindProperty("m_FaceInfo.m_Scale").floatValue = FaceScale;
        so.FindProperty("m_FaceInfo.m_AscentLine").floatValue = AscentLine * SamplingSize;
        so.FindProperty("m_FaceInfo.m_DescentLine").floatValue = DescentLine * SamplingSize;
        so.FindProperty("m_FaceInfo.m_LineHeight").floatValue = (AscentLine - DescentLine) * SamplingSize;
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
        IEnumerable<string> code = _sourceFiles.Concat(Directory.GetFiles(GameCodeFolder, "*.cs", SearchOption.AllDirectories));
        foreach (string file in code)
        {
            foreach (string line in File.ReadAllLines(file, Encoding.UTF8))
            {
                if (line.Contains("Debug.Log") || line.Contains("Exception(") || line.Contains("[Tooltip(") || line.Contains("[Header(")) continue; // 콘솔 로그 · 예외 메시지 · 인스펙터 설명 · 머리글은 화면에 안 나온다
                string body = line.Split(new[] { "//" }, System.StringSplitOptions.None)[0]; // 줄 주석 제외
                foreach (Match m in literal.Matches(body)) Add(set, m.Groups[1].Value);
            }
        }

        var name = new Regex("\"name\"\\s*:\\s*\"([^\"]*)\"");
        foreach (string file in Directory.GetFiles(StageFolder, "*.json", SearchOption.AllDirectories))
            Add(set, name.Match(File.ReadAllText(file, Encoding.UTF8)).Groups[1].Value);
        foreach (string guid in AssetDatabase.FindAssets("t:Chapter"))
        {
            var chapter = AssetDatabase.LoadAssetAtPath<ColoringBoot.Game.Chapter>(AssetDatabase.GUIDToAssetPath(guid));
            Add(set, chapter.Title + chapter.Subtitle);
        }

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
