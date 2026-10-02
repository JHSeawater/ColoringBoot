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

        // 화면 · 패널은 처음엔 모두 꺼져 있다(GameFlow가 켠다) — 꺼진 오브젝트는 transform.Find로 찾는다
        Transform safeArea = canvas.transform.Find("SafeArea");
        foreach (string panel in new[] { "SelectScreen", "BoardScreen", "OptionsPanel", "StatsPanel" })
            Expect(safeArea.Find(panel) != null && !safeArea.Find(panel).gameObject.activeSelf, $"{panel} 있음 · 처음엔 꺼짐");

        Transform area = safeArea.Find("BoardScreen/BoardArea");
        Image hit = area.GetComponent<Image>();
        Expect(hit.color.a == 0f && hit.raycastTarget, "BoardArea 투명 · raycast");
        CheckBoardView(area.GetComponent<BoardView>(), false, Expect);
        CheckBoardView(safeArea.Find("BoardScreen/TargetView").GetComponent<BoardView>(), true, Expect);

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
            Expect(text.font == font, $"폰트 {text.transform.parent.name}/{text.name}");

        CheckRefs(Object.FindAnyObjectByType<PuzzleController>(), Expect,
            "_palettes", "_boardView", "_targetView", "_mixTable", "_stageName", "_moveCounter",
            "_undoButton", "_restartButton", "_stuckUndoButton", "_clearBanner", "_clearLabel", "_stuckBanner");
        var flow = Object.FindAnyObjectByType<GameFlow>();
        CheckRefs(flow, Expect, "_catalog", "_puzzle", "_sound", "_boardScreen", "_select", "_options", "_statsView",
            "_backButton", "_boardOptionsButton", "_selectOptionsButton", "_nextButton", "_nextLabel");
        CheckRefs(Object.FindAnyObjectByType<StageSelectView>(FindObjectsInactive.Include), Expect, "_title", "_grid", "_buttonTemplate", "_noticePanel", "_notice");
        CheckRefs(Object.FindAnyObjectByType<StageButtonView>(FindObjectsInactive.Include), Expect, "_button", "_fill", "_ring", "_number", "_lock", "_star");
        CheckRefs(Object.FindAnyObjectByType<OptionsView>(FindObjectsInactive.Include), Expect, "_symbolsButton", "_symbolsLabel", "_soundButton", "_soundLabel", "_closeButton");
        CheckRefs(Object.FindAnyObjectByType<StatsView>(FindObjectsInactive.Include), Expect, "_text", "_closeButton");
        Expect(Object.FindAnyObjectByType<SoundController>() != null, "SoundController 있음");
        var catalog = (StageCatalog)new SerializedObject(flow).FindProperty("_catalog").objectReferenceValue;
        Expect(catalog.Stages.Count >= 9, "목록 9개 이상");
        for (int i = 0; i < catalog.Stages.Count; i++)
            Expect(catalog.Stages[i] != null && Stage.Parse(catalog.Stages[i].text).Cells.Count > 0, $"목록[{i}] 읽힘");
        // 목록의 모든 스테이지가 솔버로 풀리고 저장된 최소 수와 같다 (스테이지를 바꿀 때마다 걸러진다, Phase 4.5)
        foreach (TextAsset asset in catalog.Stages)
        {
            Stage stage = Stage.Parse(asset.text);
            var board = new Board(stage);
            SolveResult solved = Solver.Solve(board, board.CreateStartState());
            Expect(solved.Solved && stage.MinMoves == solved.Path.Count, $"스테이지 {asset.name} 풀림 · minMoves {stage.MinMoves} = 솔버 {(solved.Solved ? solved.Path.Count : -1)}");
        }

        CheckRefs(Object.FindAnyObjectByType<MixTableView>(FindObjectsInactive.Include), Expect, "_chipSprite");
        CheckRefs(AssetDatabase.LoadAssetAtPath<CellView>("Assets/Prefabs/Cell.prefab"), Expect,
            "_fill", "_marker", "_markerFill", "_deadRing", "_selectRing", "_ghost", "_symbol");
        Expect(AssetDatabase.LoadAssetAtPath<Button>("Assets/Prefabs/DirectionButton.prefab").targetGraphic != null, "방향 버튼 targetGraphic");

        // 팔레트 목록: 첫 칸 = 기본(default) · 이름이 비지 않고 겹치지 않음 · 팔레트마다 7색 · 알파 1
        var palettes = AssetDatabase.LoadAssetAtPath<PaletteCatalog>("Assets/Data/PaletteCatalog.asset");
        Expect(palettes != null && palettes.Palettes.Count >= 2 && palettes.Palettes.All(p => p != null), "팔레트 목록 2개 이상 · 빈 칸 없음");
        Expect(palettes.Default.Id == "default", "첫 팔레트 = default");
        Expect(palettes.Palettes.Select(p => p.Id).Distinct().Count() == palettes.Palettes.Count && palettes.Palettes.All(p => !string.IsNullOrWhiteSpace(p.Id)), "팔레트 이름 겹침 · 빈 이름 없음");
        foreach (ColorPalette each in palettes.Palettes)
        {
            var colors = new SerializedObject(each).FindProperty("_colors");
            Expect(colors.arraySize == 7 && Enumerable.Range(0, 7).All(i => colors.GetArrayElementAtIndex(i).colorValue.a == 1f), $"팔레트 {each.Id} 7색 · 알파 1");
        }
        foreach (TextAsset stage in catalog.Stages)
            Expect(palettes.IndexOf(Stage.Parse(stage.text).Palette) >= 0, $"스테이지 {stage.name}의 팔레트가 목록에 있음");
        foreach (string name in new[] { "HexFill", "HexRing", "Circle", "Arrow", "Lock", "Star" })
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
