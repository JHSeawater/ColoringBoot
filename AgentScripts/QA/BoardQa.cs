using System;
using System.Linq;
using System.Reflection;
using ColoringBoot.Core;
using ColoringBoot.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 보드 에디터 플레이 QA (Phase 1.2~) — run_script(file=AgentScripts/QA/BoardQa.cs, entry=...)
// 첫 화면은 타이틀(Phase 7.3) → Press("TitleScreen/StartButton")으로 선택 화면
// 연속 플레이: ChooseStage(1) → SolveByDrag → Press("BoardScreen/ClearBanner/NextButton") 반복
// 시험 목록(?lab): Lab() → 선택 화면(모두 열림) → 같은 방식
// 따라 하기: 저장 키가 없으면 타이틀 [시작]이 레슨 1을 연다 → Drag로 안내한 획 → Press("BoardScreen/ClearBanner/NextButton"). 힌트: Press("BoardScreen/HintButton")
// 탭은 실제 입력 경로를 탄다: 보드 영역에 클릭 이벤트(화면 좌표) → BoardView 칸 판정 → 방향 버튼 onClick → PuzzleController → 세션
public static class BoardQa
{
    // 셰이더 비동기 컴파일 상태 (에디터는 컴파일이 끝나기 전 셰이더를 하늘색 대체 셰이더로 그린다)
    public static string Shaders() => $"anythingCompiling={ShaderUtil.anythingCompiling}, async={EditorSettings.asyncShaderCompilation}, gameView={Screen.width}x{Screen.height}";

    // Game 뷰를 1080×1920 세로로 (내부 API 리플렉션 — 실패하면 사용자에게 요청)
    public static string Portrait()
    {
        Assembly editor = typeof(EditorWindow).Assembly;
        Type sizes = editor.GetType("UnityEditor.GameViewSizes");
        object instance = sizes.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        object group = sizes.GetMethod("GetGroup").Invoke(instance, new object[] { (int)GameViewSizeGroupType.Standalone });
        Type sizeType = editor.GetType("UnityEditor.GameViewSize");
        Type sizeKind = editor.GetType("UnityEditor.GameViewSizeType");
        string[] names = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
        int index = Array.FindIndex(names, n => n.Contains("1080x1920"));
        if (index < 0)
        {
            object size = Activator.CreateInstance(sizeType, Enum.Parse(sizeKind, "FixedResolution"), 1080, 1920, "Portrait 1080x1920");
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
        }
        Type gameView = editor.GetType("UnityEditor.GameView");
        EditorWindow window = EditorWindow.GetWindow(gameView);
        gameView.GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(window, new object[] { index, null });
        return $"Game 뷰 크기 {index}번 선택: {names.ElementAtOrDefault(index) ?? "Portrait 1080x1920(추가)"}";
    }

    // 칸 (q, r)을 탭한다
    public static string TapCell(int q, int r)
    {
        BoardView view = Board();
        Board board = Session().Board;
        int cell = board.IndexOf(new HexCoord(q, r));
        if (cell < 0) return $"({q},{r}) 칸 없음";
        Transform cellTransform = view.transform.GetChild(cell);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, cellTransform.position);
        var data = new PointerEventData(EventSystem.current) { position = screen };
        ExecuteEvents.Execute(view.gameObject, data, ExecuteEvents.pointerClickHandler);
        return $"({q},{r}) 탭 @ {screen} → 보이는 방향 버튼: {string.Join(" ", VisibleButtons(view))}";
    }

    // 보이는 방향 버튼 중 dir(0 = 1시 … 5 = 11시)을 누른다
    public static string TapDirection(int dir)
    {
        BoardView view = Board();
        Transform button = view.transform.GetChild(ButtonIndex(dir));
        if (!button.gameObject.activeInHierarchy) return $"{(HexDirection)dir} 버튼이 숨겨져 있음";
        var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, button.position) };
        ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        return State();
    }

    // BoardView 자식 순서: 칸 N개 · 미리보기 선 N+1개 · 방향 버튼 6개
    private static int ButtonIndex(int dir) => 2 * Session().Board.CellCount + 1 + dir;

    // 칸 (q, r)을 누른 채 dir 쪽으로 반지름 × distance만큼 끈다. release=false면 누른 채 멈춰 미리보기를 남긴다
    public static string Drag(int q, int r, int dir, float distance, bool release)
    {
        BoardView view = Board();
        int cell = Session().Board.IndexOf(new HexCoord(q, r));
        if (cell < 0) return $"({q},{r}) 칸 없음";
        var cellRect = (RectTransform)view.transform.GetChild(cell);
        Vector2 start = RectTransformUtility.WorldToScreenPoint(null, cellRect.position);
        HexCoord d = ((HexDirection)dir).Delta();
        Vector2 toward = new Vector2(1.7320508f * (d.Q + d.R * 0.5f), -1.5f * d.R).normalized;
        float radiusPixels = cellRect.rect.width * 0.5f * cellRect.lossyScale.x;
        Vector2 end = start + toward * (distance * radiusPixels);

        var data = new PointerEventData(EventSystem.current) { pointerId = 0, position = start };
        ExecuteEvents.Execute(view.gameObject, data, ExecuteEvents.pointerDownHandler);
        for (int step = 1; step <= 4; step++)
        {
            data.position = Vector2.Lerp(start, end, step / 4f);
            ExecuteEvents.Execute(view.gameObject, data, ExecuteEvents.dragHandler);
        }
        int trail = Enumerable.Range(0, Session().Board.CellCount + 1).Count(k => view.transform.GetChild(Session().Board.CellCount + k).gameObject.activeSelf);
        int ghosts = Enumerable.Range(0, Session().Board.CellCount).Count(i => view.transform.GetChild(i).Find("Ghost").gameObject.activeSelf);
        string preview = $"미리보기 선 {trail}구간 · 결과 칸 {ghosts}개";
        if (!release) return preview;
        ExecuteEvents.Execute(view.gameObject, data, ExecuteEvents.pointerUpHandler);
        return $"{preview} → {State()}";
    }

    // 지금 스테이지를 솔버 풀이대로 실제 끌기로 둔다 (Phase 4.6 연속 플레이 — 다음 스테이지는 Press("BoardScreen/ClearBanner/NextButton"))
    public static string SolveByDrag()
    {
        Board board = Session().Board;
        SolveResult result = Solver.Solve(board, board.CreateStartState());
        if (!result.Solved) return "솔버가 못 풂";
        foreach (BoardMove move in result.Path)
        {
            HexCoord c = board.CoordOf(move.Cell);
            Drag(c.Q, c.R, (int)move.Direction, 1.2f, true);
        }
        return $"풀이 {result.Path.Count}수 → {Flow()}";
    }

    public static string Undo()
    {
        var button = GameObject.Find("Canvas/SafeArea/BoardScreen/UndoButton").GetComponent<Button>();
        if (!button.interactable) return "Undo 비활성";
        button.onClick.Invoke();
        return State();
    }

    public static string Restart()
    {
        var button = GameObject.Find("Canvas/SafeArea/BoardScreen/RestartButton").GetComponent<Button>();
        if (!button.interactable) return "Restart 비활성";
        button.onClick.Invoke();
        return State();
    }

    public static string State()
    {
        PuzzleSession s = Session();
        string colors = string.Join(" ", Enumerable.Range(0, s.Board.CellCount).Select(i => (int)s.ColorAt(i)));
        return $"수 {s.MoveCount} · 성공 {s.IsSolved} · 막힘 {s.IsDead} · 색 [{colors}] · Clear 안내 {Active("ClearBanner")} · Stuck 안내 {Active("StuckBanner")} · Undo {GameObject.Find("Canvas/SafeArea/BoardScreen/UndoButton").GetComponent<Button>().interactable} · Restart {GameObject.Find("Canvas/SafeArea/BoardScreen/RestartButton").GetComponent<Button>().interactable}";
    }

    // 화면 흐름 (Phase 4.3) — 지금 켜진 화면과 그 상태. 선택 화면 버튼: 번호(잠김이면 L) · *완벽 · >다음에 풀 스테이지 · #클리어(채움)
    public static string Flow()
    {
        Transform safe = GameObject.Find("Canvas/SafeArea").transform;
        bool select = safe.Find("SelectScreen").gameObject.activeSelf;
        bool board = safe.Find("BoardScreen").gameObject.activeSelf;
        bool chapter = safe.Find("ChapterScreen").gameObject.activeSelf;
        bool endless = safe.Find("EndlessScreen").gameObject.activeSelf;
        string text = $"타이틀 {safe.Find("TitleScreen").gameObject.activeSelf}(무한 버튼 {safe.Find("TitleScreen/EndlessButton").gameObject.activeSelf}) · 선택 {select} · 보드 {board} · 그림 {chapter} · 무한 {endless} · 옵션 {safe.Find("OptionsPanel").gameObject.activeSelf}";
        // 무한 모드(2026-10-07): 난이도 버튼 글자 · 목록의 무한 버튼 · 보드의 건너뛰기
        if (endless) text += " | " + string.Join(" / ", safe.Find("EndlessScreen").GetComponentsInChildren<TMPro.TMP_Text>().Where(l => l.transform.parent.name.EndsWith("Button") && l.transform.parent.name != "BackButton").Select(l => l.text.Replace("\n", " ")));
        if (select) text += $" | 목록의 무한 버튼 {safe.Find("SelectScreen/EndlessButton").gameObject.activeSelf}";
        if (board) text += $" | 건너뛰기 {safe.Find("BoardScreen/SkipButton").gameObject.activeSelf}";
        if (chapter)
        {
            Transform screen = safe.Find("ChapterScreen");
            text += $" | {screen.Find("Caption").GetComponentInChildren<TMPro.TMP_Text>().text} · 칠한 단계 {Painted(screen.Find("Picture"))} [{screen.Find("NextButton").GetComponentInChildren<TMPro.TMP_Text>().text}]";
        }
        if (select) text += $" | 작은 그림 칠한 단계 {Painted(safe.Find("SelectScreen/Picture"))}";
        if (select)
        {
            Transform grid = safe.Find("SelectScreen/Grid");
            var buttons = Enumerable.Range(0, grid.childCount).Select(i =>
            {
                Transform b = grid.GetChild(i);
                bool locked = b.Find("Lock").gameObject.activeSelf;
                bool cleared = b.GetComponent<Image>().color.r < 0.5f;
                return $"{(b.Find("Ring").gameObject.activeSelf ? ">" : "")}{(locked ? "L" : (i + 1).ToString())}{(cleared ? "#" : "")}{(b.Find("Star").gameObject.activeSelf ? "*" : "")}";
            });
            Transform notice = safe.Find("SelectScreen/Notice");
            text += $" | {safe.Find("SelectScreen/Title").GetComponentInChildren<TMPro.TMP_Text>().text} [{string.Join(" ", buttons)}]{(notice.gameObject.activeSelf ? " 안내: " + notice.GetComponentInChildren<TMPro.TMP_Text>().text : "")}";
        }
        if (board)
        {
            Transform screen = safe.Find("BoardScreen");
            text += $" | {screen.Find("StageName").GetComponentInChildren<TMPro.TMP_Text>().text} · {screen.Find("MoveCounter").GetComponentInChildren<TMPro.TMP_Text>().text}";
            if (Active("ClearBanner")) text += $" · 띠: {screen.Find("ClearBanner/Label").GetComponent<TMPro.TMP_Text>().text} [{screen.Find("ClearBanner/NextButton").GetComponentInChildren<TMPro.TMP_Text>().text}]";
            if (Active("StuckBanner")) text += " · 막힘 띠";
            // 따라 하기 · 힌트 안내(2026-10-07): 안내 띠 문구 · 손가락 표시 · 힌트 버튼
            if (Active("GuideBanner")) text += $" · 안내: {screen.Find("GuideBanner/Label").GetComponent<TMPro.TMP_Text>().text}";
            text += $" · 손가락 {screen.Find("BoardArea/Guide")?.gameObject.activeSelf == true} · 힌트 버튼 {screen.Find("HintButton").GetComponent<Button>().interactable}";
        }
        return text;
    }

    // 시험 목록(?lab)으로 바꿔 선택 화면을 연다 — 에디터에는 주소가 없어 GameFlow의 목록 전환(UseList)을 직접 부른다 (2026-10-06)
    public static string Lab()
    {
        var flow = UnityEngine.Object.FindAnyObjectByType<GameFlow>();
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        object lab = typeof(GameFlow).GetField("_labCatalog", Private).GetValue(flow);
        typeof(GameFlow).GetMethod("UseList", Private).Invoke(flow, new[] { lab, true });
        typeof(GameFlow).GetMethod("ShowSelect", Private).Invoke(flow, null);
        return Flow();
    }

    // 선택 화면에서 number번 스테이지 버튼을 누른다
    public static string ChooseStage(int number)
    {
        GameObject.Find("Canvas/SafeArea/SelectScreen/Grid").transform.GetChild(number - 1).GetComponent<Button>().onClick.Invoke();
        return Flow();
    }

    // Canvas/SafeArea 아래 경로의 버튼을 누른다 (예: BoardScreen/BackButton · BoardScreen/ClearBanner/NextButton · OptionsPanel/SymbolsButton)
    public static string Press(string path)
    {
        GameObject target = GameObject.Find("Canvas/SafeArea/" + path);
        if (target == null) return $"{path} 없음(꺼져 있음?)";
        target.GetComponent<Button>().onClick.Invoke();
        return Flow();
    }

    // 그림에서 보이는 단계 번호들 (Step01 …)
    private static string Painted(Transform picture) =>
        string.Join(",", Enumerable.Range(0, picture.childCount).Select(picture.GetChild)
            .Where(c => c.name.StartsWith("Step") && c.gameObject.activeSelf).Select(c => c.name.Substring(4)));

    private static bool Active(string banner) => GameObject.Find("Canvas/SafeArea/BoardScreen").transform.Find(banner).gameObject.activeSelf;

    private static string[] VisibleButtons(BoardView view)
    {
        int cells = Session().Board.CellCount;
        return Enumerable.Range(0, 6).Where(d => view.transform.GetChild(ButtonIndex(d)).gameObject.activeSelf).Select(d => ((HexDirection)d).ToString()).ToArray();
    }

    // 플레이 보드 (목표 썸네일도 BoardView라 이름으로 찾는다)
    private static BoardView Board() => GameObject.Find("Canvas/SafeArea/BoardScreen/BoardArea").GetComponent<BoardView>();

    private static PuzzleSession Session()
    {
        var controller = UnityEngine.Object.FindAnyObjectByType<PuzzleController>();
        return (PuzzleSession)typeof(PuzzleController).GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller);
    }
}
