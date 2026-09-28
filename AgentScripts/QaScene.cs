using System.Collections.Generic;
using System.Linq;
using ColoringBoot.Core;
using ColoringBoot.Game;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// /qa-scene 2~4절 점검 (읽기 전용 — 아무것도 바꾸지 않는다) — run_script(file=AgentScripts/QaScene.cs, entry=QaScene.Check)
// 실패한 항목만 모아 돌려준다. 모두 통과면 "통과 N항목"
public static class QaScene
{
    private const string FontPath = "Assets/Art/Fonts/Pretendard SDF.asset";

    public static string Check()
    {
        var fails = new List<string>();
        int count = 0;
        void Expect(bool ok, string item) { count++; if (!ok) fails.Add(item); }

        GameObject canvas = GameObject.Find("Canvas");
        Expect(canvas.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay, "Canvas renderMode");
        var scaler = canvas.GetComponent<CanvasScaler>();
        Expect(scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.referenceResolution == new Vector2(1080, 1920) && scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand, "CanvasScaler");

        var area = GameObject.Find("Canvas/SafeArea/BoardArea");
        Image hit = area.GetComponent<Image>();
        Expect(hit.color.a == 0f && hit.raycastTarget, "BoardArea 투명 · raycast");
        CheckBoardView(area.GetComponent<BoardView>(), false, Expect);
        CheckBoardView(GameObject.Find("Canvas/SafeArea/TargetView").GetComponent<BoardView>(), true, Expect);

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
            Expect(text.font == font, $"폰트 {text.transform.parent.name}/{text.name}");

        CheckRefs(Object.FindAnyObjectByType<PuzzleController>(), Expect,
            "_catalog", "_palette", "_boardView", "_targetView", "_mixTable", "_stageName", "_moveCounter",
            "_undoButton", "_restartButton", "_symbolsButton", "_stuckUndoButton", "_clearBanner", "_stuckBanner");
        var catalog = (StageCatalog)new SerializedObject(Object.FindAnyObjectByType<PuzzleController>()).FindProperty("_catalog").objectReferenceValue;
        Expect(catalog.Stages.Count >= 9, "목록 9개 이상");
        for (int i = 0; i < catalog.Stages.Count; i++)
            Expect(catalog.Stages[i] != null && Stage.Parse(catalog.Stages[i].text).Cells.Count > 0, $"목록[{i}] 읽힘");
        Expect(catalog.Stages[0].name == "Grape" && Stage.Parse(catalog.Stages[0].text).Cells.Count == 10, "목록 첫 스테이지 = 포도 10칸");

        CheckRefs(Object.FindAnyObjectByType<SoundController>(), Expect, "_toggleButton", "_toggleLabel");
        CheckRefs(Object.FindAnyObjectByType<MixTableView>(), Expect, "_chipSprite");
        CheckRefs(AssetDatabase.LoadAssetAtPath<CellView>("Assets/Prefabs/Cell.prefab"), Expect,
            "_fill", "_marker", "_markerFill", "_deadRing", "_selectRing", "_ghost", "_symbol");
        Expect(AssetDatabase.LoadAssetAtPath<Button>("Assets/Prefabs/DirectionButton.prefab").targetGraphic != null, "방향 버튼 targetGraphic");

        var palette = new SerializedObject(AssetDatabase.LoadAssetAtPath<ColorPalette>("Assets/Data/Palettes/DefaultPalette.asset")).FindProperty("_colors");
        Expect(palette.arraySize == 7 && Enumerable.Range(0, 7).All(i => palette.GetArrayElementAtIndex(i).colorValue.a == 1f), "팔레트 7색 · 알파 1");
        foreach (string name in new[] { "HexFill", "HexRing", "Circle", "Arrow" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath($"Assets/Art/Sprites/{name}.png");
            Expect(importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single, $"스프라이트 {name}");
        }
        Expect(TMP_Settings.defaultFontAsset == font, "TMP 기본 폰트 = Pretendard");
        Expect(font.atlasPopulationMode.ToString() == "Static", "폰트 Static"); // TMP가 Static을 폐기 예정으로 표시 — 이름으로 비교(경고 회피)
        Expect(new SerializedObject(font).FindProperty("m_SourceFontFile").objectReferenceValue == null, "폰트 원본 참조 비움");

        return fails.Count == 0 ? $"통과 {count}항목" : $"실패 {fails.Count}/{count}: {string.Join(" · ", fails)}";
    }

    private static void CheckBoardView(BoardView view, bool target, System.Action<bool, string> expect)
    {
        var so = new SerializedObject(view);
        expect(so.FindProperty("_showTarget").boolValue == target, $"{view.name} _showTarget");
        float margin = so.FindProperty("_fitMargin").floatValue;
        expect(target ? margin < 1f : Mathf.Approximately(margin, 1.8f), $"{view.name} _fitMargin");
        expect(so.FindProperty("_cellPrefab").objectReferenceValue != null && so.FindProperty("_directionButtonPrefab").objectReferenceValue != null, $"{view.name} 프리팹 참조");
    }

    private static void CheckRefs(Object target, System.Action<bool, string> expect, params string[] fields)
    {
        var so = new SerializedObject(target);
        foreach (string field in fields) expect(so.FindProperty(field).objectReferenceValue != null, $"{target.GetType().Name}.{field}");
    }
}
