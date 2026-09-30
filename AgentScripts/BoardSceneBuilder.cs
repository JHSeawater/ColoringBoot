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

// 프리팹 · 보드 씬 구성 (Phase 1.2에서 만들고 Phase 2에서 확장) — run_script(file=AgentScripts/BoardSceneBuilder.cs, entry=...)
// BuildPrefabs → BuildScene 순서. 둘 다 다시 실행하면 같은 결과로 덮어쓴다(씬은 Main Camera만 남기고 다시 만든다)
public static class BoardSceneBuilder
{
    private const string SpriteFolder = "Assets/Art/Sprites/";
    private const string CellPrefabPath = "Assets/Prefabs/Cell.prefab";
    private const string ButtonPrefabPath = "Assets/Prefabs/DirectionButton.prefab";
    private const string OldScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ScenePath = "Assets/Scenes/Board.unity";
    private const string FontAssetPath = "Assets/Art/Fonts/Pretendard SDF.asset";
    private const string CatalogPath = "Assets/Data/StageCatalog.asset";
    private const string EditorScenePath = "Assets/Scenes/LevelEditor.unity";
    private static readonly string[] PrototypeStages = { "Grape", "TwoColors", "BrushChanges", "Honeycomb", "Crossing", "Stain", "MakeBlack", "Hive", "LastStroke" };

    // 프로토타입 라이트 테마
    private static readonly Color Lead = Hex("#1A1D23");    // 칸 테두리 · 마커 테두리
    private static readonly Color Warn = Hex("#E0246A");    // 막힘
    private static readonly Color Focus = Hex("#2E6BD1");   // 선택
    private static readonly Color Strong = Hex("#1C222C");  // 버튼 · 안내 바탕
    private static readonly Color StrongInk = Hex("#F6F7F3");
    private static readonly Color Muted = Hex("#56606C");     // 보조 글자

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
            var stage = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/Data/Stages/{name}.json")
                ?? throw new System.InvalidOperationException($"{name}.json 없음 — AgentScripts/Refresh.cs 먼저 실행");
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
            ("_palette", AssetDatabase.LoadAssetAtPath<ColorPalette>("Assets/Data/Palettes/DefaultPalette.asset")),
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

        // 화면 두 개(선택 · 보드)는 GameFlow가 켜고 끈다. 옵션 · 기록 패널은 그 위를 덮는다
        StageSelectView selectView = SelectScreen(safeArea, out Button selectOptionsButton);
        var boardScreen = NewUI("BoardScreen", safeArea);
        Stretch(boardScreen, Vector2.zero, Vector2.zero);

        // 위 버튼 줄: 목록(왼쪽) · 옵션(오른쪽)
        Button backButton = TopButton(boardScreen, "BackButton", "목록", false);
        Button boardOptionsButton = TopButton(boardScreen, "OptionsButton", "옵션", true);

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
        SetValues(targetView, ("_showTarget", true), ("_fitMargin", 0.6f), ("_maxRadius", 60f));

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

        // 보드 영역: 위 줄 · 조합표 · 아래 안내와 버튼 자리를 뺀 나머지. 투명 Image가 탭 · 드래그를 받는다
        var boardArea = NewUI("BoardArea", boardScreen);
        Stretch(boardArea, new Vector2(40f, 420f), new Vector2(-40f, -540f));
        var hitArea = boardArea.AddComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0f);
        var boardView = boardArea.AddComponent<BoardView>();
        SetRefs(boardView, ("_cellPrefab", cellPrefab), ("_directionButtonPrefab", buttonPrefab));

        // 안내 띠 (아래 버튼 위). 클리어 안내에는 다음 버튼, 막힘 안내에는 되돌리기 버튼을 함께 둔다
        GameObject clearBanner = Banner(boardScreen, "ClearBanner", Strong, "", 300f);
        TMP_Text clearLabel = clearBanner.GetComponentInChildren<TMP_Text>();
        Button nextButton = BannerButton(clearBanner, "NextButton", "다음", Strong);
        GameObject stuckBanner = Banner(boardScreen, "StuckBanner", Warn, "목표에 없는 색이 섞였어요", 300f);
        Button stuckUndoButton = BannerButton(stuckBanner, "UndoButton", "되돌리기", Warn);

        // 아래 버튼 줄 (한 손이 닿는 곳) — 자주 누르는 둘만
        Button undoButton = BottomButton(boardScreen, "UndoButton", "되돌리기", -250f, 460f);
        Button restartButton = BottomButton(boardScreen, "RestartButton", "처음부터", 250f, 460f);
        boardScreen.SetActive(false);

        OptionsView optionsView = OptionsPanel(safeArea);
        // 플레이테스트 기록 (주소 ?stats) — 맨 위를 덮는 패널, 처음엔 숨김
        StatsView statsView = StatsPanel(safeArea);

        // 사운드 켜고 끄기(옵션 화면) · 백그라운드 정지
        var sound = new GameObject("Sound", typeof(SoundController));

        var puzzle = new GameObject("Puzzle", typeof(PuzzleController));
        var controller = puzzle.GetComponent<PuzzleController>();
        SetRefs(controller,
            ("_palette", AssetDatabase.LoadAssetAtPath<ColorPalette>("Assets/Data/Palettes/DefaultPalette.asset")),
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
            ("_stuckBanner", stuckBanner));

        var game = new GameObject("Game", typeof(GameFlow));
        SetRefs(game.GetComponent<GameFlow>(),
            ("_catalog", AssetDatabase.LoadAssetAtPath<StageCatalog>(CatalogPath)),
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
            ("_nextLabel", nextButton.GetComponentInChildren<TMP_Text>()));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"씬 → {ScenePath} (루트 {scene.rootCount}개: {string.Join(", ", System.Array.ConvertAll(scene.GetRootGameObjects(), g => g.name))}), EventSystem {eventSystem.name}";
    }

    // 플레이테스트 기록 패널: 어두운 바탕(아래 보드 입력을 막음) · 왼쪽 위부터 기록 글자 · 아래 닫기 버튼
    private static StatsView StatsPanel(GameObject parent)
    {
        var panel = NewUI("StatsPanel", parent);
        Stretch(panel, new Vector2(30f, 30f), new Vector2(-30f, -30f));
        var background = panel.AddComponent<Image>();
        background.color = Strong;

        var textArea = NewUI("Text", panel);
        Stretch(textArea, new Vector2(40f, 200f), new Vector2(-40f, -40f));
        TMP_Text text = Label(textArea, "", 34f);
        text.alignment = TextAlignmentOptions.TopLeft;

        var close = NewUI("CloseButton", panel);
        var closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 100f);
        closeRect.sizeDelta = new Vector2(300f, 110f);
        var closeImage = close.AddComponent<Image>();
        closeImage.color = StrongInk;
        Button closeButton = close.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        Label(close, "닫기", 48f).color = Strong;

        var view = panel.AddComponent<StatsView>();
        SetRefs(view, ("_text", text), ("_closeButton", closeButton));
        panel.SetActive(false);
        return view;
    }

    // 스테이지 선택 화면: 제목(챕터 · 진행) · 옵션 버튼 · 육각 번호 버튼 격자(3열) · 잠김 안내. 버튼 원본은 꺼 둔 채 두고 StageSelectView가 복제한다
    private static StageSelectView SelectScreen(GameObject parent, out Button optionsButton)
    {
        var screen = NewUI("SelectScreen", parent);
        Stretch(screen, Vector2.zero, Vector2.zero);
        TMP_Text title = TopLeftText(screen, "Title", -40f, 90f, 64f, Strong);
        optionsButton = TopButton(screen, "OptionsButton", "옵션", true);

        var grid = NewUI("Grid", screen);
        var gridRect = (RectTransform)grid.transform;
        gridRect.anchorMin = gridRect.anchorMax = gridRect.pivot = new Vector2(0.5f, 1f);
        gridRect.anchoredPosition = new Vector2(0f, -220f);
        gridRect.sizeDelta = new Vector2(940f, 1400f);
        var gridLayout = grid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(260f, 300f); // 꼭짓점이 위인 육각형 비율(너비 = 높이 × √3/2)
        gridLayout.spacing = new Vector2(40f, 20f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        gridLayout.childAlignment = TextAnchor.UpperCenter;

        var notice = NewUI("Notice", screen);
        var noticeRect = (RectTransform)notice.transform;
        noticeRect.anchorMin = new Vector2(0f, 0f);
        noticeRect.anchorMax = new Vector2(1f, 0f);
        noticeRect.anchoredPosition = new Vector2(0f, 200f);
        noticeRect.sizeDelta = new Vector2(-80f, 110f);
        var noticeImage = notice.AddComponent<Image>();
        noticeImage.color = Strong;
        noticeImage.raycastTarget = false;
        TMP_Text noticeText = Label(notice, "", 46f);
        notice.SetActive(false);

        // 버튼 원본: 육각 바탕 · 다음 스테이지 테두리 · 번호 · 자물쇠 · 별
        var template = NewUI("StageButtonTemplate", screen);
        ((RectTransform)template.transform).sizeDelta = gridLayout.cellSize;
        var fill = template.AddComponent<Image>();
        fill.sprite = Sprite("HexFill");
        Button button = template.AddComponent<Button>();
        button.targetGraphic = fill;
        Image ring = AddImage("Ring", template, "HexRing", Focus, 0f, 1f);
        TMP_Text number = Label(template, "", 96f);
        Image lockIcon = AddImage("Lock", template, "Lock", new Color(Strong.r, Strong.g, Strong.b, 0.55f), 0.3f, 0.7f);
        Image star = AddImage("Star", template, "Star", Hex("#F1B928"), 0.36f, 0.64f);
        var starRect = star.rectTransform;
        starRect.anchorMin = new Vector2(0.36f, 0.04f);
        starRect.anchorMax = new Vector2(0.64f, 0.28f);
        var buttonView = template.AddComponent<StageButtonView>();
        SetRefs(buttonView, ("_button", button), ("_fill", fill), ("_ring", ring), ("_number", number), ("_lock", lockIcon), ("_star", star));
        template.SetActive(false);

        var view = screen.AddComponent<StageSelectView>();
        SetRefs(view, ("_title", title), ("_grid", gridRect), ("_buttonTemplate", buttonView), ("_noticePanel", notice), ("_notice", noticeText));
        screen.SetActive(false);
        return view;
    }

    // 옵션 패널: 어두운 바탕(아래 입력을 막음) · 제목 · 기호 · 소리 켜고 끄기 · 닫기
    private static OptionsView OptionsPanel(GameObject parent)
    {
        var panel = NewUI("OptionsPanel", parent);
        Stretch(panel, new Vector2(30f, 30f), new Vector2(-30f, -30f));
        panel.AddComponent<Image>().color = Strong;
        TMP_Text title = TopLeftText(panel, "Title", -40f, 90f, 64f, StrongInk);
        title.text = "옵션";

        Button symbols = PanelButton(panel, "SymbolsButton", "", 260f);
        Button sound = PanelButton(panel, "SoundButton", "", 80f);
        Button close = PanelButton(panel, "CloseButton", "닫기", -300f);

        var view = panel.AddComponent<OptionsView>();
        SetRefs(view,
            ("_symbolsButton", symbols), ("_symbolsLabel", symbols.GetComponentInChildren<TMP_Text>()),
            ("_soundButton", sound), ("_soundLabel", sound.GetComponentInChildren<TMP_Text>()),
            ("_closeButton", close));
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
        var image = go.AddComponent<Image>();
        image.color = StrongInk;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
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
        var image = go.AddComponent<Image>();
        image.color = Strong;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        Label(go, text, 44f);
        return button;
    }

    // 안내 띠 오른쪽 끝의 밝은 버튼 (글자는 띠 색)
    private static Button BannerButton(GameObject banner, string name, string text, Color textColor)
    {
        var go = NewUI(name, banner);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-16f, 0f);
        rect.sizeDelta = new Vector2(260f, 90f);
        var image = go.AddComponent<Image>();
        image.color = StrongInk;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        Label(go, text, 44f).color = textColor;
        return button;
    }

    private static Button BottomButton(GameObject parent, string name, string text, float x, float width)
    {
        var go = NewUI(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, 150f);
        rect.sizeDelta = new Vector2(width, 130f);
        var image = go.AddComponent<Image>();
        image.color = Strong;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        Label(go, text, 48f);
        return button;
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
        var image = banner.AddComponent<Image>();
        image.color = background;
        image.raycastTarget = false;
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
            else property.floatValue = (float)value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_Text Label(GameObject parent, string text, float size)
    {
        var label = NewUI("Label", parent);
        Stretch(label, Vector2.zero, Vector2.zero);
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) ?? throw new System.InvalidOperationException("한글 폰트 에셋 없음 — AgentScripts/Phase2Font.cs 먼저 실행");
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
            if (value == null) throw new System.ArgumentException($"{target.GetType().Name}.{field}에 넣을 에셋을 찾지 못함 — AgentScripts/Refresh.cs 먼저 실행");
            property.objectReferenceValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + name + ".png");

    private static Color Hex(string html)
    {
        ColorUtility.TryParseHtmlString(html, out Color color);
        return color;
    }
}
