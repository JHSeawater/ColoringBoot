using System.Collections.Generic;
using System.Linq;
using ColoringBoot.Core;
using ColoringBoot.Game;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// /qa-scene 2~4절 점검 (읽기 전용 — 아무것도 바꾸지 않는다) — run_script(file=AgentScripts/QA/QaScene.cs, entry=QaScene.Check)
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
        foreach (string panel in new[] { "TitleScreen", "SelectScreen", "BoardScreen", "ChapterScreen", "OptionsPanel", "StatsPanel" })
        {
            Expect(safeArea.Find(panel) != null && !safeArea.Find(panel).gameObject.activeSelf, $"{panel} 있음 · 처음엔 꺼짐");
            Expect(safeArea.Find(panel)?.GetComponent<ScreenFade>() != null, $"{panel} 전환(ScreenFade)");
        }

        Transform area = safeArea.Find("BoardScreen/BoardArea");
        Image hit = area.GetComponent<Image>();
        Expect(hit.color.a == 0f && hit.raycastTarget, "BoardArea 투명 · raycast");
        CheckBoardView(area.GetComponent<BoardView>(), false, Expect);
        CheckBoardView(safeArea.Find("BoardScreen/TargetView").GetComponent<BoardView>(), true, Expect);
        // 연출(Phase 7.4): 플레이 보드만 MotionSettings — 목표 썸네일은 없음, 안내 띠는 나타나며 켜짐
        Expect(new SerializedObject(area.GetComponent<BoardView>()).FindProperty("_motion").objectReferenceValue is MotionSettings, "BoardArea 연출 값(MotionSettings)");
        Expect(new SerializedObject(safeArea.Find("BoardScreen/TargetView").GetComponent<BoardView>()).FindProperty("_motion").objectReferenceValue == null, "TargetView 연출 없음");
        // 따라 하기 · 힌트 손가락 표시(2026-10-07): 플레이 보드만
        Expect(new SerializedObject(area.GetComponent<BoardView>()).FindProperty("_guideSprite").objectReferenceValue != null, "BoardArea 안내 손가락 표시");
        Expect(new SerializedObject(safeArea.Find("BoardScreen/TargetView").GetComponent<BoardView>()).FindProperty("_guideSprite").objectReferenceValue == null, "TargetView 안내 표시 없음");
        foreach (string banner in new[] { "BoardScreen/ClearBanner", "BoardScreen/StuckBanner", "BoardScreen/GuideBanner" })
            Expect(safeArea.Find(banner)?.GetComponent<ScreenFade>() != null, $"{banner} 전환(ScreenFade)");

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
            Expect(text.font == font, $"폰트 {text.transform.parent.name}/{text.name}");
        // 씬 글자가 모두 폰트에 있다 — 없으면 게임에 □(폰트 빌더를 다시 실행, 2026-10-05)
        string sceneMissing = MissingGlyphs(font, canvas.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text));
        Expect(sceneMissing.Length == 0, $"씬 글자가 폰트에 없음 [{sceneMissing}]");

        CheckRefs(Object.FindAnyObjectByType<PuzzleController>(), Expect,
            "_palettes", "_boardView", "_targetView", "_mixTable", "_stageName", "_moveCounter",
            "_undoButton", "_restartButton", "_stuckUndoButton", "_clearBanner", "_clearLabel", "_stuckBanner",
            "_hintButton", "_guideBanner", "_guideLabel");
        // 보드를 덮는 패널(옵션 · 기록) — 열려 있으면 키보드 입력을 받지 않는다(2026-10-05)
        SerializedProperty overlays = new SerializedObject(Object.FindAnyObjectByType<PuzzleController>()).FindProperty("_overlays");
        Expect(overlays.arraySize == 2 && (overlays.GetArrayElementAtIndex(0).objectReferenceValue as GameObject)?.name == "OptionsPanel"
            && (overlays.GetArrayElementAtIndex(1).objectReferenceValue as GameObject)?.name == "StatsPanel", "PuzzleController._overlays = 옵션 · 기록 패널");
        var flow = Object.FindAnyObjectByType<GameFlow>();
        CheckRefs(flow, Expect, "_chapter", "_puzzle", "_sound", "_boardScreen", "_select", "_options", "_statsView",
            "_backButton", "_boardOptionsButton", "_selectOptionsButton", "_nextButton", "_nextLabel",
            "_selectPicture", "_chapterScreen", "_chapterPicture", "_chapterCaption", "_chapterNextButton", "_chapterNextLabel",
            "_titleScreen", "_startButton", "_labCatalog", "_tutorialCatalog");
        foreach (string picture in new[] { "SelectScreen/Picture", "ChapterScreen/Picture" })
            Expect(safeArea.Find(picture)?.GetComponent<ChapterView>() != null, $"{picture} ChapterView");
        CheckRefs(Object.FindAnyObjectByType<StageSelectView>(FindObjectsInactive.Include), Expect, "_title", "_subtitle", "_grid", "_buttonTemplate", "_noticePanel", "_notice");
        CheckRefs(Object.FindAnyObjectByType<StageButtonView>(FindObjectsInactive.Include), Expect, "_button", "_fill", "_ring", "_number", "_lock", "_star");
        CheckRefs(Object.FindAnyObjectByType<OptionsView>(FindObjectsInactive.Include), Expect, "_symbolsButton", "_symbolsLabel", "_soundButton", "_soundLabel", "_closeButton", "_tutorialButton");
        CheckRefs(Object.FindAnyObjectByType<StatsView>(FindObjectsInactive.Include), Expect, "_text", "_closeButton");
        Expect(Object.FindAnyObjectByType<SoundController>() != null, "SoundController 있음");
        // 챕터 에셋(2026-10-07): 제목 · 부제가 있고 글자가 폰트에 있음 · 목록 · 그림이 연결됨
        var chapter = (Chapter)new SerializedObject(flow).FindProperty("_chapter").objectReferenceValue;
        Expect(!string.IsNullOrWhiteSpace(chapter.Title) && !string.IsNullOrWhiteSpace(chapter.Subtitle) && chapter.Stages != null && chapter.Art != null, $"챕터 '{chapter.Title}' · '{chapter.Subtitle}' 목록 · 그림 연결");
        string chapterMissing = MissingGlyphs(font, new[] { chapter.Title, chapter.Subtitle });
        Expect(chapterMissing.Length == 0, $"챕터 제목 · 부제 글자가 폰트에 없음 [{chapterMissing}]");
        StageCatalog catalog = chapter.Stages;
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
        string nameMissing = MissingGlyphs(font, catalog.Stages.Select(s => Stage.Parse(s.text).Name));
        Expect(nameMissing.Length == 0, $"스테이지 이름 글자가 폰트에 없음 [{nameMissing}]");

        // 시험 목록(주소 ?lab, 2026-10-06): 읽힘 · 풀림 · minMoves 일치 · 이름 글자 · 파일 이름(기록의 키)이 챕터 목록과 겹치지 않음
        var lab = (StageCatalog)new SerializedObject(flow).FindProperty("_labCatalog").objectReferenceValue;
        Expect(lab.Stages.Count > 0 && lab.Stages.All(s => s != null), "시험 목록 비지 않음 · 빈 칸 없음");
        foreach (TextAsset asset in lab.Stages)
        {
            Stage stage = Stage.Parse(asset.text);
            var board = new Board(stage);
            SolveResult solved = Solver.Solve(board, board.CreateStartState());
            Expect(solved.Solved && stage.MinMoves == solved.Path.Count, $"시험 {asset.name} 풀림 · minMoves {stage.MinMoves} = 솔버 {(solved.Solved ? solved.Path.Count : -1)}");
        }
        string labMissing = MissingGlyphs(font, lab.Stages.Select(s => Stage.Parse(s.text).Name));
        Expect(labMissing.Length == 0, $"시험 목록 이름 글자가 폰트에 없음 [{labMissing}]");
        Expect(!lab.Stages.Any(s => catalog.Stages.Any(c => c.name == s.name)), "시험 목록 파일 이름이 챕터 목록과 겹치지 않음");

        // 따라 하기(2026-10-07): 레슨 수 = 목록 수 · 안내한 획대로 그으면 풀림(= 최소 수) · 일부러 막히는 획은 막힘 · 문구 글자 · 파일 이름이 다른 목록과 안 겹침
        var tutorial = (StageCatalog)new SerializedObject(flow).FindProperty("_tutorialCatalog").objectReferenceValue;
        Expect(tutorial.Stages.Count == TutorialLessons.Count && tutorial.Stages.All(s => s != null), $"따라 하기 목록 {tutorial.Stages.Count}개 = 레슨 {TutorialLessons.Count}개");
        for (int i = 0; i < Mathf.Min(tutorial.Stages.Count, TutorialLessons.Count); i++)
        {
            Stage stage = Stage.Parse(tutorial.Stages[i].text);
            var board = new Board(stage);
            PaintColor[] state = board.CreateStartState();
            if (TutorialLessons.IsFree(i))
            {
                // 혼자 풀기(복습): 솔버로 풀리고 최소 수가 맞는가
                SolveResult free = Solver.Solve(board, state);
                Expect(free.Solved && stage.MinMoves == free.Path.Count, $"따라 하기 {i + 1}(혼자 풀기) 풀림 · minMoves {stage.MinMoves} = 솔버 {(free.Solved ? free.Path.Count : -1)}");
                continue;
            }
            bool brushed = TutorialLessons.Path(i).All(g => board.IndexOf(g.Cell) >= 0 && board.Brush(state, board.IndexOf(g.Cell), g.Direction));
            Expect(brushed && board.IsSolved(state) && stage.MinMoves == TutorialLessons.Path(i).Count, $"따라 하기 {i + 1} 안내대로 풀림 · 최소 {stage.MinMoves}수");
            TutorialLessons.Guide? trap = TutorialLessons.Trap(i);
            if (trap.HasValue)
            {
                PaintColor[] trapped = board.CreateStartState();
                int cell = board.IndexOf(trap.Value.Cell);
                Expect(cell >= 0 && board.Brush(trapped, cell, trap.Value.Direction) && board.IsDead(trapped), $"따라 하기 {i + 1} 일부러 막히는 획이 막힘");
            }
        }
        string lessonMissing = MissingGlyphs(font, TutorialLessons.Texts().Concat(tutorial.Stages.Select(s => Stage.Parse(s.text).Name)));
        Expect(lessonMissing.Length == 0, $"따라 하기 문구 · 이름 글자가 폰트에 없음 [{lessonMissing}]");
        Expect(!tutorial.Stages.Any(s => catalog.Stages.Concat(lab.Stages).Any(c => c.name == s.name)), "따라 하기 파일 이름이 다른 목록과 겹치지 않음");

        // 챕터 그림 (Phase 5): 단계 수 = 스테이지 수(단계 i ↔ 스테이지 i) · 조각이 모두 있음 · 캔버스 4:5
        ChapterArt art = chapter.Art;
        Expect(art.Steps.Count == catalog.Stages.Count, $"그림 단계 {art.Steps.Count}개 = 스테이지 {catalog.Stages.Count}개");
        Expect(art.Line.Sprite != null && art.Steps.All(s => s.Sprite != null && s.Rect.width > 0 && s.Rect.height > 0), "그림 선화 · 단계 조각 모두 있음");
        Expect(art.Canvas.x * 5 == art.Canvas.y * 4, $"그림 캔버스 4:5 ({art.Canvas.x}×{art.Canvas.y})");

        CheckRefs(Object.FindAnyObjectByType<MixTableView>(FindObjectsInactive.Include), Expect, "_chipSprite");
        CheckRefs(AssetDatabase.LoadAssetAtPath<CellView>("Assets/Prefabs/Cell.prefab"), Expect,
            "_fill", "_marker", "_markerFill", "_deadRing", "_selectRing", "_ghost", "_symbol");
        Expect(AssetDatabase.LoadAssetAtPath<Button>("Assets/Prefabs/DirectionButton.prefab").targetGraphic != null, "방향 버튼 targetGraphic");

        // 팔레트 목록: 첫 칸 = 기본(default) · 이름이 비지 않고 겹치지 않음 · 팔레트마다 7색 · 알파 1
        var palettes = AssetDatabase.LoadAssetAtPath<PaletteCatalog>("Assets/Data/Palettes/PaletteCatalog.asset");
        Expect(palettes != null && palettes.Palettes.Count >= 2 && palettes.Palettes.All(p => p != null), "팔레트 목록 2개 이상 · 빈 칸 없음");
        Expect(palettes.Default.Id == "default", "첫 팔레트 = default");
        Expect(palettes.Palettes.Select(p => p.Id).Distinct().Count() == palettes.Palettes.Count && palettes.Palettes.All(p => !string.IsNullOrWhiteSpace(p.Id)), "팔레트 이름 겹침 · 빈 이름 없음");
        foreach (ColorPalette each in palettes.Palettes)
        {
            var colors = new SerializedObject(each).FindProperty("_colors");
            Expect(colors.arraySize == 7 && Enumerable.Range(0, 7).All(i => colors.GetArrayElementAtIndex(i).colorValue.a == 1f), $"팔레트 {each.Id} 7색 · 알파 1");
        }
        foreach (TextAsset stage in catalog.Stages.Concat(lab.Stages).Concat(tutorial.Stages))
            Expect(palettes.IndexOf(Stage.Parse(stage.text).Palette) >= 0, $"스테이지 {stage.name}의 팔레트가 목록에 있음");
        foreach (string name in new[] { "HexFill", "HexRing", "Circle", "Arrow", "Lock", "Star", "RoundFill", "RoundRing", "FrameRing", "HexLine" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath($"Assets/Art/Sprites/{name}.png");
            Expect(importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single, $"스프라이트 {name}");
        }
        Expect(TMP_Settings.defaultFontAsset == font, "TMP 기본 폰트 = Pretendard");
        Expect(font.atlasPopulationMode.ToString() == "Static", "폰트 Static"); // TMP가 Static을 폐기 예정으로 표시 — 이름으로 비교(경고 회피)
        Expect(new SerializedObject(font).FindProperty("m_SourceFontFile").objectReferenceValue == null, "폰트 원본 참조 비움");

        return fails.Count == 0 ? $"통과 {count}항목" : $"실패 {fails.Count}/{count}: {string.Join(" · ", fails)}";
    }

    // 폰트에 없는 글자(제어 문자 제외)를 겹치지 않게 모은다
    private static string MissingGlyphs(TMP_FontAsset font, IEnumerable<string> texts) =>
        new string(texts.Where(t => t != null).SelectMany(t => t).Where(c => !char.IsControl(c) && !font.HasCharacter(c)).Distinct().ToArray());

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
