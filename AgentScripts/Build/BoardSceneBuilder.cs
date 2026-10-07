using System.Linq;
using ColoringBoot.Game;
using ColoringBoot.LevelEditor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 프리팹 · 보드 씬 구성 (Phase 1.2에서 만들고 Phase 2에서 확장) — run_script(file=AgentScripts/Build/BoardSceneBuilder.cs, entry=...)
// BuildPrefabs → BuildScene 순서. 둘 다 다시 실행하면 같은 결과로 덮어쓴다(씬은 Main Camera만 남기고 다시 만든다)
public static class BoardSceneBuilder
{
    private const string SpriteFolder = "Assets/Art/Sprites/";
    private const string CellPrefabPath = "Assets/Prefabs/Cell.prefab";
    private const string ButtonPrefabPath = "Assets/Prefabs/DirectionButton.prefab";
    private const string OldScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ScenePath = "Assets/Scenes/Board.unity";
    private const string FontAssetPath = "Assets/Art/Fonts/Pretendard SDF.asset";
    // 게임이 여는 챕터(지금은 챕터 1) — 데이터는 챕터마다 Assets/Data/Chapters/ChapterN/ (2026-10-07)
    private const string ChapterFolder = "Assets/Data/Chapters/Chapter1";
    private const string ChapterPath = ChapterFolder + "/Chapter.asset";        // 제목 · 부제 · 목록 · 그림 — AgentScripts/Setup/Phase8ChapterFolders.cs가 만듦
    private const string CatalogPath = ChapterFolder + "/StageCatalog.asset";
    private const string LabCatalogPath = "Assets/Data/Lab/LabCatalog.asset";   // 시험 목록(?lab) — AgentScripts/Build/StageOrder.cs SetLabOrder 먼저
    private const string TutorialCatalogPath = "Assets/Data/Tutorial/TutorialCatalog.asset";   // 따라 하기 — StageOrder.SetTutorialOrder 먼저
    private const string PaletteCatalogPath = "Assets/Data/Palettes/PaletteCatalog.asset";
    private const string EndlessFolder = "Assets/Data/Endless";   // 무한 모드 퍼즐 묶음 — AgentScripts/Build/EndlessPoolBuilder.cs 먼저 (2026-10-07)
    private static readonly string[] EndlessTiers = { "Easy", "Normal", "Hard" };
    private const string EditorScenePath = "Assets/Scenes/LevelEditor.unity";
    private const string ThemePath = "Assets/Data/Settings/UiTheme.asset";             // 디자인 기준(Phase 7.1) — 없으면 기본값으로 만든다
    private const string MotionPath = "Assets/Data/Settings/MotionSettings.asset";     // 보드 연출 값(Phase 7.4) — 없으면 기본값으로 만든다
    private static readonly string[] PrototypeStages = { "Grape", "TwoColors", "BrushChanges", "Honeycomb", "Crossing", "Stain", "MakeBlack", "Hive", "LastStroke" };

    // 화면 색은 디자인 기준(UiTheme)에서 — 보드 칸 선택 링만 그대로
    private static UiTheme _theme;
    private static UiTheme T => _theme != null ? _theme : (_theme = LoadTheme());
    private static Color Lead => T.Ink;           // 칸 테두리 · 마커 테두리 · 외곽선
    private static Color Warn => T.Warn;          // 막힘
    private static readonly Color Focus = Hex("#2E6BD1");   // 보드 칸 선택
    private static Color Strong => T.Ink;         // 글자 · 채운 버튼 · 안내 바탕
    private static Color StrongInk => T.SurfaceInk;
    private static Color Muted => T.Muted;        // 보조 글자

    private static UiTheme LoadTheme() => LoadOrCreate<UiTheme>(ThemePath);

    // 값 에셋: 있으면 그대로(사람이 고친 값 유지), 없으면 코드 기본값으로 만든다
    private static TAsset LoadOrCreate<TAsset>(string path) where TAsset : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<TAsset>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        return asset;
    }

    public static string BuildPrefabs()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");

        // 칸: 모든 층이 칸 크기(반지름 × 2 정사각형)에 비례한다 — BoardView가 sizeDelta만 바꾼다
        var cell = NewUI("Cell", null);
        var view = cell.AddComponent<CellView>();
        AddImage("Border", cell, "HexFill", Lead, 0f, 1f);
        Image fill = AddImage("Fill", cell, "HexFill", Color.white, 0.04f, 0.96f);
        GameObject marker = AddImage("Marker", cell, "HexFill", Lead, 0.35f, 0.65f).gameObject;
        Image markerFill = AddImage("MarkerFill", marker, "HexFill", Color.white, 0.15f, 0.85f);
        GameObject dead = AddImage("DeadRing", cell, "HexRing", Warn, 0.1f, 0.9f).gameObject;
        GameObject select = AddImage("SelectRing", cell, "HexRing", Focus, 0.02f, 0.98f).gameObject;
        // 미리보기 결과: 칠해질 색을 작은 반투명 육각형으로(프로토타입 0.72배 · 불투명도 0.92)
        Image ghost = AddImage("Ghost", cell, "HexFill", new Color(1f, 1f, 1f, 0.92f), 0.14f, 0.86f);
        ghost.transform.SetSiblingIndex(fill.transform.GetSiblingIndex() + 1);
        dead.SetActive(false);
        select.SetActive(false);
        ghost.gameObject.SetActive(false);
        // 접근성 기호 (R · Y · B) — 칸 크기에 맞춰 글자 크기 자동
        TMP_Text symbol = Label(cell, "", 40f);
        symbol.gameObject.name = "Symbol";
        symbol.rectTransform.anchorMin = Vector2.one * 0.18f;
        symbol.rectTransform.anchorMax = Vector2.one * 0.82f;
        symbol.enableAutoSizing = true;
        symbol.fontSizeMin = 8f;
        symbol.fontSizeMax = 72f;
        symbol.gameObject.SetActive(false);
        SetRefs(view, ("_fill", fill), ("_marker", marker), ("_markerFill", markerFill), ("_deadRing", dead), ("_selectRing", select), ("_ghost", ghost), ("_symbol", symbol));
        PrefabUtility.SaveAsPrefabAsset(cell, CellPrefabPath);
        Object.DestroyImmediate(cell);

        // 방향 버튼: 원 + 오른쪽을 가리키는 화살표. BoardView가 방향에 맞게 돌린다
        var button = NewUI("DirectionButton", null);
        Image circle = button.AddComponent<Image>();
        circle.sprite = Sprite("Circle");
        circle.color = Strong;
        button.AddComponent<Button>().targetGraphic = circle;
        AddImage("Arrow", button, "Arrow", StrongInk, 0f, 1f);
        PrefabUtility.SaveAsPrefabAsset(button, ButtonPrefabPath);
        Object.DestroyImmediate(button);

        return $"프리팹 → {CellPrefabPath}, {ButtonPrefabPath}";
    }

    // 스테이지 목록: 프로토타입 스테이지 9개(프로토타입 순서) 중 빠진 것만 채운다 — 레벨 에디터가 추가한 스테이지는 지우지 않는다
    public static string BuildCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<StageCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        var so = new SerializedObject(catalog);
        SerializedProperty stages = so.FindProperty("_stages");
        int added = 0;
        foreach (string name in PrototypeStages)
        {
            var stage = AssetDatabase.LoadAssetAtPath<TextAsset>($"{ChapterFolder}/Stages/{name}.json")
                ?? throw new System.InvalidOperationException($"{name}.json 없음 — AgentScripts/Tools/Refresh.cs 먼저 실행");
            if (catalog.Stages.Contains(stage)) continue;
            stages.arraySize++;
            stages.GetArrayElementAtIndex(stages.arraySize - 1).objectReferenceValue = stage;
            added++;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return $"목록 {catalog.Stages.Count}개(추가 {added}) → {CatalogPath}";
    }

    // 레벨 에디터 씬 (에디터 전용 — 빌드 씬 목록에 넣지 않는다). 다 만들면 보드 씬을 다시 연다
    public static string BuildEditorScene()
    {
        Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorScenePath) != null
            ? EditorSceneManager.OpenScene(EditorScenePath)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Hex("#D9DFDC");

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var cellPrefab = AssetDatabase.LoadAssetAtPath<CellView>(CellPrefabPath);
        var buttonPrefab = AssetDatabase.LoadAssetAtPath<Button>(ButtonPrefabPath);

        // 위쪽 절반 = 보드 (아래쪽은 IMGUI 조작 패널)
        var gridArea = NewUI("PaintGrid", canvasObject);
        Stretch(gridArea, new Vector2(40f, 940f), new Vector2(-40f, -40f));
        gridArea.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        var grid = gridArea.AddComponent<PaintGridView>();
        SetRefs(grid, ("_cellPrefab", cellPrefab));

        // 획 기록용 게임 보드 원본 — 꺼 둔 채로, 기록할 때 복제해서 쓴다
        var recordArea = NewUI("RecordBoardTemplate", canvasObject);
        Stretch(recordArea, new Vector2(40f, 940f), new Vector2(-40f, -40f));
        recordArea.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        var recordView = recordArea.AddComponent<BoardView>();
        SetRefs(recordView, ("_cellPrefab", cellPrefab), ("_directionButtonPrefab", buttonPrefab));
        SetValues(recordView, ("_showJudgement", false));
        recordArea.SetActive(false);

        var editor = new GameObject("LevelEditor", typeof(LevelEditorController));
        SetRefs(editor.GetComponent<LevelEditorController>(),
            ("_grid", grid),
            ("_recordTemplate", recordView),
            ("_catalog", AssetDatabase.LoadAssetAtPath<StageCatalog>(CatalogPath)),
            ("_palettes", AssetDatabase.LoadAssetAtPath<PaletteCatalog>(PaletteCatalogPath)),
            ("_gameFont", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath)));

        EditorSceneManager.SaveScene(scene, EditorScenePath);
        int roots = scene.rootCount;
        EditorSceneManager.OpenScene(ScenePath);
        return $"에디터 씬 → {EditorScenePath} (루트 {roots}개, 빌드 목록 {EditorBuildSettings.scenes.Length}개 — 넣지 않음)";
    }

    public static string BuildScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OldScenePath) != null)
        {
            string error = AssetDatabase.RenameAsset(OldScenePath, "Board");
            if (!string.IsNullOrEmpty(error)) return $"씬 이름 변경 실패: {error}";
        }
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != "Main Camera") Object.DestroyImmediate(root);
            else root.GetComponent<Camera>().backgroundColor = T.Background;
        }

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // 기준 영역이 어느 비율에서도 다 보이게

        // 모든 UI는 안전영역 패널 아래 (노치 · Dynamic Island 침범 방지)
        var safeArea = NewUI("SafeArea", canvasObject);
        Stretch(safeArea, Vector2.zero, Vector2.zero);
        safeArea.AddComponent<SafeAreaFitter>();

        var cellPrefab = AssetDatabase.LoadAssetAtPath<CellView>(CellPrefabPath);
        var buttonPrefab = AssetDatabase.LoadAssetAtPath<Button>(ButtonPrefabPath);

        // 화면(타이틀 · 선택 · 보드 · 그림)은 GameFlow가 켜고 끈다. 옵션 · 기록 패널은 그 위를 덮는다. 모두 켜질 때 짧게 나타난다(ScreenFade)
        GameObject titleScreen = TitleScreen(safeArea, out Button startButton, out Button endlessButton);
        StageSelectView selectView = SelectScreen(safeArea, out Button selectOptionsButton, out ChapterView selectPicture, out Button selectEndlessButton);
        EndlessView endlessView = EndlessScreen(safeArea);
        var boardScreen = NewUI("BoardScreen", safeArea);
        Stretch(boardScreen, Vector2.zero, Vector2.zero);
        Fade(boardScreen);

        // 위 버튼 줄: 목록(왼쪽) · 옵션(오른쪽)
        Button backButton = TopButton(boardScreen, "BackButton", "목록", false);
        Button boardOptionsButton = TopButton(boardScreen, "OptionsButton", "옵션", true);
        // 무한 모드 건너뛰기 (2026-10-07) — 위 버튼 줄 가운데, 무한 모드에서만 GameFlow가 켠다
        Button skipButton = TopButton(boardScreen, "SkipButton", "건너뛰기", false);
        var skipRect = (RectTransform)skipButton.transform;
        skipRect.anchorMin = skipRect.anchorMax = skipRect.pivot = new Vector2(0.5f, 1f);
        skipRect.anchoredPosition = new Vector2(0f, -30f);
        skipRect.sizeDelta = new Vector2(240f, 90f);
        skipButton.gameObject.SetActive(false);

        // 스테이지 이름 · 수 카운터(왼쪽), 목표 그림 썸네일(오른쪽)
        TMP_Text stageName = TopLeftText(boardScreen, "StageName", -150f, 90f, 64f, Strong);
        TMP_Text moveCounter = TopLeftText(boardScreen, "MoveCounter", -250f, 70f, 52f, Muted);
        var target = NewUI("TargetView", boardScreen);
        var targetRect = (RectTransform)target.transform;
        targetRect.anchorMin = targetRect.anchorMax = targetRect.pivot = new Vector2(1f, 1f);
        targetRect.anchoredPosition = new Vector2(-40f, -140f);
        targetRect.sizeDelta = new Vector2(300f, 300f);
        var targetView = target.AddComponent<BoardView>();
        SetRefs(targetView, ("_cellPrefab", cellPrefab), ("_directionButtonPrefab", buttonPrefab));
        SetValues(targetView, ("_showTarget", true), ("_fitMargin", 0.6f), ("_maxRadius", 60f), ("_emptyColor", T.EmptyCell));

        // 색 조합표 한 줄
        var mix = NewUI("MixTable", boardScreen);
        var mixRect = (RectTransform)mix.transform;
        mixRect.anchorMin = mixRect.anchorMax = mixRect.pivot = new Vector2(0.5f, 1f);
        mixRect.anchoredPosition = new Vector2(0f, -460f);
        mixRect.sizeDelta = new Vector2(1000f, 60f);
        var layout = mix.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        layout.spacing = 4f;
        var mixTable = mix.AddComponent<MixTableView>();
        SetRefs(mixTable, ("_chipSprite", Sprite("HexFill")));
        SetValues(mixTable, ("_textColor", Muted));

        // 보드 영역: 위 줄 · 조합표 · 아래 안내와 버튼 자리를 뺀 나머지. 투명 Image가 탭 · 드래그를 받는다
        var boardArea = NewUI("BoardArea", boardScreen);
        Stretch(boardArea, new Vector2(40f, 420f), new Vector2(-40f, -540f));
        var hitArea = boardArea.AddComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0f);
        var boardView = boardArea.AddComponent<BoardView>();
        SetRefs(boardView, ("_cellPrefab", cellPrefab), ("_directionButtonPrefab", buttonPrefab));
        SetValues(boardView, ("_emptyColor", T.EmptyCell), ("_emptyTrailColor", new Color(Lead.r, Lead.g, Lead.b, 0.55f)));
        SetRefs(boardView, ("_motion", LoadOrCreate<MotionSettings>(MotionPath)));   // 플레이 보드만 연출(목표 썸네일 · 레벨 에디터는 없음)
        SetRefs(boardView, ("_guideSprite", Sprite("Circle")));                        // 따라 하기 · 힌트 손가락 표시도 플레이 보드만
        SetValues(boardView, ("_guideColor", new Color(Lead.r, Lead.g, Lead.b, 0.6f)));

        // 안내 띠 (아래 버튼 위). 클리어 안내에는 다음 버튼, 막힘 안내에는 되돌리기 버튼을 함께 둔다
        GameObject clearBanner = Banner(boardScreen, "ClearBanner", Strong, "", 300f);
        TMP_Text clearLabel = clearBanner.GetComponentInChildren<TMP_Text>();
        // 따라 하기 문구가 길어 한 줄 그대로 글자를 줄인다 (2026-10-07 — 줄바꿈을 두면 끝 낱말만 다음 줄로 꺾임)
        clearLabel.rectTransform.offsetMin = new Vector2(30f, 0f);
        clearLabel.textWrappingMode = TextWrappingModes.NoWrap;
        clearLabel.enableAutoSizing = true;
        clearLabel.fontSizeMin = 32f;
        clearLabel.fontSizeMax = 52f;
        Button nextButton = BannerButton(clearBanner, "NextButton", "다음", Strong);
        GameObject stuckBanner = Banner(boardScreen, "StuckBanner", Warn, "목표에 없는 색이 섞였어요", 300f);
        Button stuckUndoButton = BannerButton(stuckBanner, "UndoButton", "되돌리기", Warn);
        // 따라 하기 · 힌트 안내 한 줄 (2026-10-07) — 막힘 띠와 같은 자리, 길면 글자를 줄인다
        GameObject guideBanner = Banner(boardScreen, "GuideBanner", T.Next, "", 0f);
        TMP_Text guideLabel = guideBanner.GetComponentInChildren<TMP_Text>();
        guideLabel.rectTransform.offsetMin = new Vector2(30f, 0f);
        guideLabel.rectTransform.offsetMax = new Vector2(-30f, 0f);
        guideLabel.enableAutoSizing = true;
        guideLabel.fontSizeMin = 32f;
        guideLabel.fontSizeMax = 52f;

        // 아래 버튼 줄 (한 손이 닿는 곳) — 되돌리기 · 처음부터 · 힌트
        Button undoButton = BottomButton(boardScreen, "UndoButton", "되돌리기", -340f, 300f, false);
        Button restartButton = BottomButton(boardScreen, "RestartButton", "처음부터", 0f, 300f, false);
        Button hintButton = BottomButton(boardScreen, "HintButton", "힌트", 340f, 300f, false);
        boardScreen.SetActive(false);

        GameObject chapterScreen = ChapterScreen(safeArea, out ChapterView chapterPicture, out TMP_Text chapterCaption, out Button chapterNextButton);

        OptionsView optionsView = OptionsPanel(safeArea);
        // 플레이테스트 기록 (주소 ?stats) — 맨 위를 덮는 패널, 처음엔 숨김
        StatsView statsView = StatsPanel(safeArea);

        // 사운드 켜고 끄기(옵션 화면) · 백그라운드 정지
        var sound = new GameObject("Sound", typeof(SoundController));

        var puzzle = new GameObject("Puzzle", typeof(PuzzleController));
        var controller = puzzle.GetComponent<PuzzleController>();
        SetRefs(controller,
            ("_palettes", AssetDatabase.LoadAssetAtPath<PaletteCatalog>(PaletteCatalogPath)),
            ("_boardView", boardView),
            ("_targetView", targetView),
            ("_mixTable", mixTable),
            ("_stageName", stageName),
            ("_moveCounter", moveCounter),
            ("_undoButton", undoButton),
            ("_restartButton", restartButton),
            ("_stuckUndoButton", stuckUndoButton),
            ("_clearBanner", clearBanner),
            ("_clearLabel", clearLabel),
            ("_stuckBanner", stuckBanner),
            ("_hintButton", hintButton),
            ("_guideBanner", guideBanner),
            ("_guideLabel", guideLabel));
        // 보드를 덮는 패널 — 열려 있으면 키보드가 뒤의 보드를 움직이지 않는다(2026-10-05)
        SetArray(controller, "_overlays", optionsView.gameObject, statsView.gameObject);

        var game = new GameObject("Game", typeof(GameFlow));
        SetRefs(game.GetComponent<GameFlow>(),
            ("_chapter", AssetDatabase.LoadAssetAtPath<Chapter>(ChapterPath)),
            ("_puzzle", controller),
            ("_sound", sound.GetComponent<SoundController>()),
            ("_boardScreen", boardScreen),
            ("_select", selectView),
            ("_options", optionsView),
            ("_statsView", statsView),
            ("_backButton", backButton),
            ("_boardOptionsButton", boardOptionsButton),
            ("_selectOptionsButton", selectOptionsButton),
            ("_nextButton", nextButton),
            ("_nextLabel", nextButton.GetComponentInChildren<TMP_Text>()),
            ("_selectPicture", selectPicture),
            ("_chapterScreen", chapterScreen),
            ("_chapterPicture", chapterPicture),
            ("_chapterCaption", chapterCaption),
            ("_chapterNextButton", chapterNextButton),
            ("_chapterNextLabel", chapterNextButton.GetComponentInChildren<TMP_Text>()),
            ("_titleScreen", titleScreen),
            ("_startButton", startButton),
            ("_labCatalog", AssetDatabase.LoadAssetAtPath<StageCatalog>(LabCatalogPath)),
            ("_tutorialCatalog", AssetDatabase.LoadAssetAtPath<StageCatalog>(TutorialCatalogPath)),
            ("_endless", endlessView),
            ("_endlessButton", endlessButton),
            ("_selectEndlessButton", selectEndlessButton),
            ("_skipButton", skipButton));
        SetArray(game.GetComponent<GameFlow>(), "_endlessPools", System.Array.ConvertAll(EndlessTiers, tier =>
            (Object)(AssetDatabase.LoadAssetAtPath<TextAsset>($"{EndlessFolder}/{tier}.txt") ?? throw new System.InvalidOperationException($"{tier}.txt 없음 — EndlessPoolBuilder 먼저"))));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"씬 → {ScenePath} (루트 {scene.rootCount}개: {string.Join(", ", System.Array.ConvertAll(scene.GetRootGameObjects(), g => g.name))}), EventSystem {eventSystem.name}";
    }

    // 무한 모드 화면 (2026-10-07): 뒤로(왼쪽 위) · 제목 · 한 줄 설명 · 난이도 버튼 셋(글자는 GameFlow가 이름 · 최소 수 범위 · 푼 수로 채움)
    private static EndlessView EndlessScreen(GameObject parent)
    {
        var screen = NewUI("EndlessScreen", parent);
        Stretch(screen, Vector2.zero, Vector2.zero);
        Fade(screen);
        Button back = TopButton(screen, "BackButton", "뒤로", false);
        CenterText(screen, "Title", -300f, 140f, 110f, Strong).text = "무한 모드";
        CenterText(screen, "Subtitle", -420f, 70f, 46f, Muted).text = "난이도를 고르면 퍼즐이 계속 나와요";
        string[] names = { "EasyButton", "NormalButton", "HardButton" };
        var buttons = new Object[names.Length];
        var labels = new Object[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            var go = NewUI(names[i], screen);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -700f - i * 290f);
            rect.sizeDelta = new Vector2(760f, 240f);
            buttons[i] = OutlinedButton(go, "", 54f);
            labels[i] = go.GetComponentInChildren<TMP_Text>();
        }
        var view = screen.AddComponent<EndlessView>();
        SetArray(view, "_tierButtons", buttons);
        SetArray(view, "_tierLabels", labels);
        SetRefs(view, ("_backButton", back));
        screen.SetActive(false);
        return view;
    }

    // 타이틀 화면 (Phase 7.3): 기본색 세 칸(빨강 · 노랑 · 파랑) · 제목 · 한 줄 소개 · 시작 버튼. 로고 그림이 생기면 제목 글자 자리를 바꾼다
    private static GameObject TitleScreen(GameObject parent, out Button start, out Button endless)
    {
        var screen = NewUI("TitleScreen", parent);
        Stretch(screen, Vector2.zero, Vector2.zero);
        Fade(screen);
        var palette = AssetDatabase.LoadAssetAtPath<ColorPalette>("Assets/Data/Palettes/DefaultPalette.asset");
        var chips = new[] { ColoringBoot.Core.PaintColor.Red, ColoringBoot.Core.PaintColor.Yellow, ColoringBoot.Core.PaintColor.Blue };
        for (int i = 0; i < chips.Length; i++)
        {
            var chip = NewUI($"Chip{i + 1}", screen);
            var rect = (RectTransform)chip.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2((i - 1) * 150f, -600f);
            rect.sizeDelta = new Vector2(150f, 150f);   // 육각 스프라이트는 정사각형 안에 비율이 들어 있다
            var fill = chip.AddComponent<Image>();
            fill.sprite = Sprite("HexFill");
            fill.color = palette.Get(chips[i]);
            fill.raycastTarget = false;
            AddImage("Outline", chip, "HexLine", Lead, 0f, 1f);
        }
        CenterText(screen, "Title", -820f, 200f, 160f, Strong).text = "컬러링붓";
        CenterText(screen, "Subtitle", -960f, 70f, 48f, Muted).text = "붓으로 칠하는 육각 퍼즐";
        start = BottomButton(screen, "StartButton", "시작", 0f, 460f, true);
        // 무한 모드 (2026-10-07) — 시작 위, 따라 하기를 본 뒤에 GameFlow가 켠다
        endless = BottomButton(screen, "EndlessButton", "무한 모드", 0f, 460f, false);
        ((RectTransform)endless.transform).anchoredPosition = new Vector2(0f, 310f);
        endless.gameObject.SetActive(false);
        screen.SetActive(false);
        return screen;
    }

    // 가운데 정렬 글자 줄 (y = 위에서부터, 가운데 기준)
    private static TMP_Text CenterText(GameObject parent, string name, float y, float height, float size, Color color)
    {
        var go = NewUI(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(1000f, height);
        TMP_Text text = Label(go, "", size);
        text.color = color;
        return text;
    }

    // 화면 · 패널이 켜질 때 짧게 나타나게 (Phase 7.3)
    private static void Fade(GameObject screen)
    {
        screen.AddComponent<CanvasGroup>();
        screen.AddComponent<ScreenFade>();
    }

    // 플레이테스트 기록 패널: 어두운 바탕(아래 보드 입력을 막음) · 왼쪽 위부터 기록 글자 · 아래 닫기 버튼
    private static StatsView StatsPanel(GameObject parent)
    {
        var panel = NewUI("StatsPanel", parent);
        Stretch(panel, new Vector2(30f, 30f), new Vector2(-30f, -30f));
        Fade(panel);
        RoundImage(panel, "RoundFill", Strong);

        var textArea = NewUI("Text", panel);
        Stretch(textArea, new Vector2(40f, 200f), new Vector2(-40f, -40f));
        TMP_Text text = Label(textArea, "", 34f);
        text.alignment = TextAlignmentOptions.TopLeft;
        // 따라 하기 기록까지 한 화면에 들어가게 글자를 줄인다 (2026-10-07)
        text.enableAutoSizing = true;
        text.fontSizeMin = 20f;
        text.fontSizeMax = 34f;

        var close = NewUI("CloseButton", panel);
        var closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 100f);
        closeRect.sizeDelta = new Vector2(300f, 110f);
        Button closeButton = close.AddComponent<Button>();
        closeButton.targetGraphic = RoundImage(close, "RoundFill", T.Surface);
        Label(close, "닫기", 48f).color = Strong;

        var view = panel.AddComponent<StatsView>();
        SetRefs(view, ("_text", text), ("_closeButton", closeButton));
        panel.SetActive(false);
        return view;
    }

    // 챕터 그림 화면 (Phase 5): 진행 글자(왼쪽 위) · 큰 그림(캔버스 4:5) · 다음 버튼. 처음 클리어한 뒤 GameFlow가 켠다
    private static GameObject ChapterScreen(GameObject parent, out ChapterView picture, out TMP_Text caption, out Button next)
    {
        var screen = NewUI("ChapterScreen", parent);
        Stretch(screen, Vector2.zero, Vector2.zero);
        Fade(screen);
        caption = TopLeftText(screen, "Caption", -40f, 90f, 64f, Strong);
        picture = Picture(screen, -150f, new Vector2(960f, 1200f));
        next = BottomButton(screen, "NextButton", "다음", 0f, 460f, true);
        screen.SetActive(false);
        return screen;
    }

    // 챕터 그림 자리 — 위쪽 가운데, size는 캔버스 비율(4:5)로
    private static ChapterView Picture(GameObject parent, float y, Vector2 size)
    {
        var go = NewUI("Picture", parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = size;
        var view = go.AddComponent<ChapterView>();
        SetRefs(view, ("_frame", Sprite("FrameRing")));
        SetValues(view, ("_frameColor", Lead));
        return view;
    }

    // 스테이지 선택 화면: 제목(챕터 · 진행) · 옵션 버튼 · 챕터 그림(작게) · 육각 번호 버튼 격자(4열) · 잠김 안내. 버튼 원본은 꺼 둔 채 두고 StageSelectView가 복제한다
    private static StageSelectView SelectScreen(GameObject parent, out Button optionsButton, out ChapterView picture, out Button endlessButton)
    {
        var screen = NewUI("SelectScreen", parent);
        Stretch(screen, Vector2.zero, Vector2.zero);
        Fade(screen);
        TMP_Text title = TopLeftText(screen, "Title", -40f, 90f, 64f, Strong);
        optionsButton = TopButton(screen, "OptionsButton", "옵션", true);
        TMP_Text subtitle = TopLeftText(screen, "Subtitle", -125f, 60f, 40f, Muted);
        picture = Picture(screen, -210f, new Vector2(480f, 600f));

        var grid = NewUI("Grid", screen);
        var gridRect = (RectTransform)grid.transform;
        gridRect.anchorMin = gridRect.anchorMax = gridRect.pivot = new Vector2(0.5f, 1f);
        gridRect.anchoredPosition = new Vector2(0f, -850f);
        gridRect.sizeDelta = new Vector2(940f, 760f);
        var gridLayout = grid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(200f, 230f); // 꼭짓점이 위인 육각형 비율(너비 = 높이 × √3/2)
        gridLayout.spacing = new Vector2(36f, 16f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
        gridLayout.childAlignment = TextAnchor.UpperCenter;

        var notice = NewUI("Notice", screen);
        var noticeRect = (RectTransform)notice.transform;
        noticeRect.anchorMin = new Vector2(0f, 0f);
        noticeRect.anchorMax = new Vector2(1f, 0f);
        noticeRect.anchoredPosition = new Vector2(0f, 200f);
        noticeRect.sizeDelta = new Vector2(-80f, 110f);
        RoundImage(notice, "RoundFill", Strong).raycastTarget = false;
        TMP_Text noticeText = Label(notice, "", 46f);
        notice.SetActive(false);

        // 무한 모드 (2026-10-07) — 목록 아래(잠김 안내 띠 밑), 따라 하기를 본 뒤 챕터 목록에서만 GameFlow가 켠다
        endlessButton = BottomButton(screen, "EndlessButton", "무한 모드", 0f, 460f, false);
        var endlessRect = (RectTransform)endlessButton.transform;
        endlessRect.anchoredPosition = new Vector2(0f, 80f);
        endlessRect.sizeDelta = new Vector2(460f, 110f);
        endlessButton.gameObject.SetActive(false);

        // 버튼 원본: 육각 바탕 · 다음 스테이지 테두리 · 번호 · 자물쇠 · 별
        var template = NewUI("StageButtonTemplate", screen);
        ((RectTransform)template.transform).sizeDelta = gridLayout.cellSize;
        var fill = template.AddComponent<Image>();
        fill.sprite = Sprite("HexFill");
        Button button = template.AddComponent<Button>();
        button.targetGraphic = fill;
        AddImage("Outline", template, "HexLine", Lead, 0f, 1f);
        Image ring = AddImage("Ring", template, "HexRing", T.Next, -0.05f, 1.05f);
        TMP_Text number = Label(template, "", 72f);
        Image lockIcon = AddImage("Lock", template, "Lock", Muted, 0.3f, 0.7f);
        Image star = AddImage("Star", template, "Star", T.Star, 0.36f, 0.64f);
        var starRect = star.rectTransform;
        starRect.anchorMin = new Vector2(0.36f, 0.04f);
        starRect.anchorMax = new Vector2(0.64f, 0.28f);
        var buttonView = template.AddComponent<StageButtonView>();
        SetRefs(buttonView, ("_button", button), ("_fill", fill), ("_ring", ring), ("_number", number), ("_lock", lockIcon), ("_star", star));
        template.SetActive(false);

        var view = screen.AddComponent<StageSelectView>();
        SetRefs(view, ("_title", title), ("_subtitle", subtitle), ("_grid", gridRect), ("_buttonTemplate", buttonView), ("_noticePanel", notice), ("_notice", noticeText));
        SetValues(view, ("_lockedFill", T.Locked), ("_openFill", T.Surface), ("_clearedFill", Strong), ("_openText", Strong), ("_clearedText", StrongInk));
        screen.SetActive(false);
        return view;
    }

    // 옵션 패널: 어두운 바탕(아래 입력을 막음) · 제목 · 기호 · 소리 켜고 끄기 · 닫기
    private static OptionsView OptionsPanel(GameObject parent)
    {
        var panel = NewUI("OptionsPanel", parent);
        Stretch(panel, new Vector2(30f, 30f), new Vector2(-30f, -30f));
        Fade(panel);
        RoundImage(panel, "RoundFill", Strong);
        TMP_Text title = TopLeftText(panel, "Title", -40f, 90f, 64f, StrongInk);
        title.text = "옵션";

        Button symbols = PanelButton(panel, "SymbolsButton", "", 260f);
        Button sound = PanelButton(panel, "SoundButton", "", 80f);
        Button tutorial = PanelButton(panel, "TutorialButton", "규칙 다시 보기", -100f);
        Button close = PanelButton(panel, "CloseButton", "닫기", -300f);

        var view = panel.AddComponent<OptionsView>();
        SetRefs(view,
            ("_symbolsButton", symbols), ("_symbolsLabel", symbols.GetComponentInChildren<TMP_Text>()),
            ("_soundButton", sound), ("_soundLabel", sound.GetComponentInChildren<TMP_Text>()),
            ("_closeButton", close), ("_tutorialButton", tutorial));
        panel.SetActive(false);
        return view;
    }

    // 패널 가운데 줄의 밝은 버튼 (y = 가운데 기준)
    private static Button PanelButton(GameObject parent, string name, string text, float y)
    {
        var go = NewUI(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(700f, 130f);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = RoundImage(go, "RoundFill", T.Surface);
        Label(go, text, 52f).color = Strong;
        return button;
    }

    // 위 버튼 줄의 작은 버튼 (right = 오른쪽 끝)
    private static Button TopButton(GameObject parent, string name, string text, bool right)
    {
        var go = NewUI(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(right ? 1f : 0f, 1f);
        rect.anchoredPosition = new Vector2(right ? -40f : 40f, -30f);
        rect.sizeDelta = new Vector2(200f, 90f);
        return OutlinedButton(go, text, 44f);
    }

    // 안내 띠 오른쪽 끝의 밝은 버튼 (글자는 띠 색)
    private static Button BannerButton(GameObject banner, string name, string text, Color textColor)
    {
        var go = NewUI(name, banner);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-16f, 0f);
        rect.sizeDelta = new Vector2(260f, 90f);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = RoundImage(go, "RoundFill", T.Surface);
        Label(go, text, 44f).color = textColor;
        return button;
    }

    // primary = 갈색으로 채운 주요 버튼(다음), 아니면 외곽선 버튼
    private static Button BottomButton(GameObject parent, string name, string text, float x, float width, bool primary)
    {
        var go = NewUI(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, 150f);
        rect.sizeDelta = new Vector2(width, 130f);
        if (!primary) return OutlinedButton(go, text, 48f);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = RoundImage(go, "RoundFill", Strong);
        Label(go, text, 48f);
        return button;
    }

    // 종이색 바탕 + 갈색 외곽선 + 갈색 글자 (디자인 시안 A)
    private static Button OutlinedButton(GameObject go, string text, float size)
    {
        Button button = go.AddComponent<Button>();
        button.targetGraphic = RoundImage(go, "RoundFill", T.Surface);
        Image outline = AddImage("Outline", go, "RoundRing", Lead, 0f, 1f);
        outline.type = Image.Type.Sliced;
        Label(go, text, size).color = Strong;
        return button;
    }

    // 9-slice 둥근 사각형 Image (크기와 상관없이 모서리 반지름 그대로)
    private static Image RoundImage(GameObject go, string sprite, Color color)
    {
        var image = go.AddComponent<Image>();
        image.sprite = Sprite(sprite);
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    // 아래 버튼 위의 안내 띠 — 처음엔 숨김, PuzzleController가 켠다. rightSpace만큼 오른쪽을 버튼 자리로 비운다
    private static GameObject Banner(GameObject parent, string name, Color background, string text, float rightSpace)
    {
        var banner = NewUI(name, parent);
        var rect = (RectTransform)banner.transform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(0f, 330f);
        rect.sizeDelta = new Vector2(-80f, 120f);
        RoundImage(banner, "RoundFill", background).raycastTarget = false;
        Fade(banner);
        Label(banner, text, 52f).rectTransform.offsetMax = new Vector2(-rightSpace, 0f);
        banner.SetActive(false);
        return banner;
    }

    // 왼쪽 위 글자 줄 (스테이지 이름 · 수 카운터)
    private static TMP_Text TopLeftText(GameObject parent, string name, float y, float height, float size, Color color)
    {
        var go = NewUI(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(60f, y);
        rect.sizeDelta = new Vector2(600f, height);
        TMP_Text text = Label(go, "", size);
        text.color = color;
        text.alignment = TextAlignmentOptions.Left;
        return text;
    }

    private static void SetValues(Object target, params (string field, object value)[] values)
    {
        var so = new SerializedObject(target);
        foreach ((string field, object value) in values)
        {
            SerializedProperty property = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name}에 필드 {field} 없음");
            if (value is bool b) property.boolValue = b;
            else if (value is Color c) property.colorValue = c;
            else property.floatValue = (float)value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_Text Label(GameObject parent, string text, float size)
    {
        var label = NewUI("Label", parent);
        Stretch(label, Vector2.zero, Vector2.zero);
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) ?? throw new System.InvalidOperationException("한글 폰트 에셋 없음 — AgentScripts/Build/FontBuilder.cs 먼저 실행");
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = StrongInk;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static GameObject NewUI(string name, GameObject parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static Image AddImage(string name, GameObject parent, string sprite, Color color, float min, float max)
    {
        var go = NewUI(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.one * min;
        rect.anchorMax = Vector2.one * max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = go.AddComponent<Image>();
        image.sprite = Sprite(sprite);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(GameObject go, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetRefs(Object target, params (string field, Object value)[] refs)
    {
        var so = new SerializedObject(target);
        foreach ((string field, Object value) in refs)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null) throw new System.ArgumentException($"{target.GetType().Name}에 필드 {field} 없음");
            // 디스크에 막 쓴 에셋은 임포트 전이라 null로 읽힌다(2026-09-28: Grape.json) — 조용히 비우지 말고 멈춘다
            if (value == null) throw new System.ArgumentException($"{target.GetType().Name}.{field}에 넣을 에셋을 찾지 못함 — AgentScripts/Tools/Refresh.cs 먼저 실행");
            property.objectReferenceValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(Object target, string field, params Object[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name}에 필드 {field} 없음");
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + name + ".png");

    private static Color Hex(string html)
    {
        ColorUtility.TryParseHtmlString(html, out Color color);
        return color;
    }
}
