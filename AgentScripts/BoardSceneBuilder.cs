using ColoringBoot.Game;
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

    // 프로토타입 라이트 테마
    private static readonly Color Lead = Hex("#1A1D23");    // 칸 테두리 · 마커 테두리
    private static readonly Color Warn = Hex("#E0246A");    // 막힘
    private static readonly Color Focus = Hex("#2E6BD1");   // 선택
    private static readonly Color Strong = Hex("#1C222C");  // 버튼 · 안내 바탕
    private static readonly Color StrongInk = Hex("#F6F7F3");

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
        SetRefs(view, ("_fill", fill), ("_marker", marker), ("_markerFill", markerFill), ("_deadRing", dead), ("_selectRing", select), ("_ghost", ghost));
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

        // 보드 영역: 위 안내 · 아래 버튼 자리를 뺀 나머지. 투명 Image가 탭을 받는다
        var boardArea = NewUI("BoardArea", canvasObject);
        Stretch(boardArea, new Vector2(40f, 360f), new Vector2(-40f, -240f));
        var hitArea = boardArea.AddComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0f);
        var boardView = boardArea.AddComponent<BoardView>();
        SetRefs(boardView,
            ("_cellPrefab", AssetDatabase.LoadAssetAtPath<CellView>(CellPrefabPath)),
            ("_directionButtonPrefab", AssetDatabase.LoadAssetAtPath<Button>(ButtonPrefabPath)));

        GameObject clearBanner = Banner(canvasObject, "ClearBanner", Strong, "Clear!");
        GameObject stuckBanner = Banner(canvasObject, "StuckBanner", Warn, "Stuck - press Restart");

        // 아래 버튼 줄 (한 손이 닿는 곳)
        Button undoButton = BottomButton(canvasObject, "UndoButton", "Undo", -260f);
        Button restartButton = BottomButton(canvasObject, "RestartButton", "Restart", 260f);

        var puzzle = new GameObject("Puzzle", typeof(PuzzleController));
        SetRefs(puzzle.GetComponent<PuzzleController>(),
            ("_stageCode", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/Stages/Grape.json")),
            ("_palette", AssetDatabase.LoadAssetAtPath<ColorPalette>("Assets/Data/Palettes/DefaultPalette.asset")),
            ("_boardView", boardView),
            ("_undoButton", undoButton),
            ("_restartButton", restartButton),
            ("_clearBanner", clearBanner),
            ("_stuckBanner", stuckBanner));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"씬 → {ScenePath} (루트 {scene.rootCount}개: {string.Join(", ", System.Array.ConvertAll(scene.GetRootGameObjects(), g => g.name))}), EventSystem {eventSystem.name}";
    }

    private static Button BottomButton(GameObject parent, string name, string text, float x)
    {
        var go = NewUI(name, parent);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, 180f);
        rect.sizeDelta = new Vector2(480f, 140f);
        var image = go.AddComponent<Image>();
        image.color = Strong;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        Label(go, text, 56f);
        return button;
    }

    // 위쪽 안내 띠 — 처음엔 숨김, PuzzleController가 켠다
    private static GameObject Banner(GameObject parent, string name, Color background, string text)
    {
        var banner = NewUI(name, parent);
        var rect = (RectTransform)banner.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(0f, -120f);
        rect.sizeDelta = new Vector2(-80f, 150f);
        var image = banner.AddComponent<Image>();
        image.color = background;
        image.raycastTarget = false;
        Label(banner, text, 64f);
        banner.SetActive(false);
        return banner;
    }

    private static void Label(GameObject parent, string text, float size)
    {
        var label = NewUI("Label", parent);
        Stretch(label, Vector2.zero, Vector2.zero);
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = StrongInk;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
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
