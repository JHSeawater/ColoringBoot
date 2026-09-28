using System;
using System.Linq;
using System.Reflection;
using ColoringBoot.Core;
using ColoringBoot.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Phase 1.2 에디터 플레이 QA — run_script(file=AgentScripts/Phase1Qa.cs, entry=...)
// 탭은 실제 입력 경로를 탄다: 보드 영역에 클릭 이벤트(화면 좌표) → BoardView 칸 판정 → 방향 버튼 onClick → PuzzleController → 세션
public static class Phase1Qa
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
        BoardView view = UnityEngine.Object.FindAnyObjectByType<BoardView>();
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
        BoardView view = UnityEngine.Object.FindAnyObjectByType<BoardView>();
        Transform button = view.transform.GetChild(Session().Board.CellCount + dir);
        if (!button.gameObject.activeInHierarchy) return $"{(HexDirection)dir} 버튼이 숨겨져 있음";
        var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, button.position) };
        ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        return State();
    }

    public static string Restart()
    {
        var button = GameObject.Find("Canvas/RestartButton").GetComponent<Button>();
        if (!button.interactable) return "Restart 비활성";
        button.onClick.Invoke();
        return State();
    }

    public static string State()
    {
        PuzzleSession s = Session();
        string colors = string.Join(" ", Enumerable.Range(0, s.Board.CellCount).Select(i => (int)s.ColorAt(i)));
        return $"수 {s.MoveCount} · 성공 {s.IsSolved} · 막힘 {s.IsDead} · 색 [{colors}] · Clear 안내 {Active("ClearBanner")} · Stuck 안내 {Active("StuckBanner")} · Restart {GameObject.Find("Canvas/RestartButton").GetComponent<Button>().interactable}";
    }

    private static bool Active(string banner) => GameObject.Find("Canvas").transform.Find(banner).gameObject.activeSelf;

    private static string[] VisibleButtons(BoardView view)
    {
        int cells = Session().Board.CellCount;
        return Enumerable.Range(0, 6).Where(d => view.transform.GetChild(cells + d).gameObject.activeSelf).Select(d => ((HexDirection)d).ToString()).ToArray();
    }

    private static PuzzleSession Session()
    {
        var controller = UnityEngine.Object.FindAnyObjectByType<PuzzleController>();
        return (PuzzleSession)typeof(PuzzleController).GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller);
    }
}
