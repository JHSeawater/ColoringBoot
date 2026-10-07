using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColoringBoot.Core;
using ColoringBoot.Game;
using ColoringBoot.LevelEditor;
using UnityEngine;
using UnityEngine.EventSystems;

// 레벨 에디터 플레이 QA (LevelEditor.unity를 플레이한 상태에서) — run_script(file=AgentScripts/QA/LevelEditorQa.cs, entry=...)
// 칠하기 · 획 긋기는 실제 포인터 이벤트로, IMGUI 버튼 동작은 컨트롤러의 private 메서드를 리플렉션으로 부른다
public static class LevelEditorQa
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Clear()
    {
        Field<Dictionary<HexCoord, (PaintColor, PaintColor)>>("_cells").Clear();
        Call("RenderGrid");
        return State();
    }

    // paint: 0 = 칸 없애기, 1 = 빈칸, 2~8 = 색 1~7. target: 목표 층이면 true
    public static string Paint(int q, int r, int paint, bool target)
    {
        Set("_paint", paint);
        Set("_targetLayer", target);
        PaintGridView grid = Object.FindAnyObjectByType<PaintGridView>();
        var coords = (List<HexCoord>)typeof(PaintGridView).GetField("_coords", Private).GetValue(grid);
        int index = coords.IndexOf(new HexCoord(q, r));
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, grid.transform.GetChild(index).position);
        ExecuteEvents.Execute(grid.gameObject, new PointerEventData(EventSystem.current) { position = screen }, ExecuteEvents.pointerDownHandler);
        return State();
    }

    public static string Record()
    {
        Call("StartRecording");
        return State();
    }

    // 획 기록 보드에서 (q, r)을 dir 쪽으로 끈다 (게임과 같은 드래그)
    public static string Drag(int q, int r, int dir)
    {
        var session = Field<PuzzleSession>("_record");
        var view = Field<BoardView>("_recordView");
        int cell = session.Board.IndexOf(new HexCoord(q, r));
        var cellRect = (RectTransform)view.transform.GetChild(cell);
        Vector2 start = RectTransformUtility.WorldToScreenPoint(null, cellRect.position);
        HexCoord d = ((HexDirection)dir).Delta();
        Vector2 toward = new Vector2(1.7320508f * (d.Q + d.R * 0.5f), -1.5f * d.R).normalized;
        float radius = cellRect.rect.width * 0.5f * cellRect.lossyScale.x;
        var data = new PointerEventData(EventSystem.current) { pointerId = 0, position = start };
        ExecuteEvents.Execute(view.gameObject, data, ExecuteEvents.pointerDownHandler);
        data.position = start + toward * radius;
        ExecuteEvents.Execute(view.gameObject, data, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(view.gameObject, data, ExecuteEvents.pointerUpHandler);
        return $"획 {session.MoveCount} · 색 [{string.Join(" ", Enumerable.Range(0, session.Board.CellCount).Select(i => (int)session.ColorAt(i)))}]";
    }

    public static string Keep(bool keep)
    {
        Call("StopRecording", keep);
        return State();
    }

    public static string Check()
    {
        var controller = Controller();
        object stage = typeof(LevelEditorController).GetMethod("BuildStage", Private).Invoke(controller, new object[] { null });
        object result = typeof(LevelEditorController).GetMethod("Solve", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new[] { stage });
        return (string)typeof(LevelEditorController).GetMethod("Describe", Private).Invoke(controller, new[] { result });
    }

    public static string Save(string name, string file)
    {
        Set("_name", name);
        Set("_fileName", file);
        Call("Save");
        return State();
    }

    public static string Generate()
    {
        Call("Generate");
        return State();
    }

    // 생성 조건 (2026-10-07): shape = 모양 번호(6 = 그린 모양), mixed · trap = 단계 번호(0 = 끔), targetColors = "3,6"처럼 목표에 쓸 색(빈 문자열 = 모두)
    public static string GenerateWith(int shape, int seeds, int moves, int mixed, int trap, string targetColors)
    {
        Set("_genShape", shape);
        Set("_genSeeds", seeds);
        Set("_genMoves", moves);
        Set("_genMixed", mixed);
        Set("_genTrap", trap);
        bool[] colors = Field<bool[]>("_genTargetColors");
        int[] picked = targetColors.Split(',').Where(s => s.Length > 0).Select(int.Parse).ToArray();
        for (int c = 0; c < colors.Length; c++) colors[c] = picked.Length == 0 || picked.Contains(c + 1);
        Call("Generate");
        return State();
    }

    // 그림 → 시작 칸 찾기: 지금 칠한 목표 그림으로. palette = 색 옵션 번호(1 = 섞인 색 포함 → 시작 색도 섞인 색 허용), order = 순서 옵션 번호(0 엄격 · 1 보통 · 2 상관없음)
    public static string FindSeeds(int seeds, int moves, int palette, int order)
    {
        Set("_genSeeds", seeds);
        Set("_genMoves", moves);
        Set("_genPalette", palette);
        Set("_genOrder", order);
        Call("FindSeeds");
        return State();
    }

    public static string State()
    {
        var cells = Field<Dictionary<HexCoord, (PaintColor start, PaintColor target)>>("_cells");
        string list = string.Join(" ", cells.OrderBy(c => c.Key.R).ThenBy(c => c.Key.Q).Select(c => $"({c.Key.Q},{c.Key.R}):{(int)c.Value.start}/{(int)c.Value.target}"));
        return $"칸 {cells.Count} [{list}] · 기록 중 {Field<PuzzleSession>("_record") != null} · 상태: {Field<string>("_status")}";
    }

    private static LevelEditorController Controller() => Object.FindAnyObjectByType<LevelEditorController>();
    private static T Field<T>(string name) => (T)typeof(LevelEditorController).GetField(name, Private).GetValue(Controller());
    private static void Set(string name, object value) => typeof(LevelEditorController).GetField(name, Private).SetValue(Controller(), value);
    private static void Call(string name, params object[] args) => typeof(LevelEditorController).GetMethod(name, Private).Invoke(Controller(), args);
}
