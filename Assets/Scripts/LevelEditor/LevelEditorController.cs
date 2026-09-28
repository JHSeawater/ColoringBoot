using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ColoringBoot.Core;
using ColoringBoot.Game;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ColoringBoot.LevelEditor
{
    // 레벨 에디터 (GDD §8, 에디터 전용 씬 LevelEditor.unity를 플레이해서 쓴다 — 빌드에 들어가지 않음).
    // 칠하기(시작 · 목표 층) → 획 기록(게임 보드 그대로) → 목표 저장 → 풀이 검사 → 저장(minMoves는 솔버가 채움, 목록에 등록).
    // 조작 패널은 IMGUI(OnGUI) — 에디터 도구라 게임 UI 폰트 아틀라스와 무관하게 한글이 나온다
    public sealed class LevelEditorController : MonoBehaviour
    {
        private const int MaxCells = 40;               // GDD §6 스테이지 최대 칸 수
        private const string StageFolder = "Assets/Data/Stages";
        private const float GenerateSeconds = 4f;      // 생성기 한 번에 쓸 시간 (프로토타입과 같음)
        private const float ReferenceHeight = 1920f;   // IMGUI 배율 기준
        private const float PanelTop = 1000f;          // 패널은 화면 아래쪽 (보드 영역은 씬에서 위쪽)
        private const int FontSize = 30;
        private static readonly Color PanelText = new Color32(0x1C, 0x22, 0x2C, 0xFF); // 밝은 바탕 위 글자

        private static readonly string[] _paintNames = { "칸 없애기", "빈칸", "빨강", "노랑", "주황", "파랑", "보라", "초록", "검정" };
        private static readonly string[] _shapeNames = { "작은 육각형", "큰 육각형", "마름모", "삼각형", "불규칙", "아무거나" };
        private static readonly string[] _genPaletteNames = { "원색만", "섞인 색 포함", "빨강 · 노랑만" };
        private static readonly PaintColor[][] _genPalettes =
        {
            new[] { PaintColor.Red, PaintColor.Yellow, PaintColor.Blue },
            new[] { PaintColor.Red, PaintColor.Yellow, PaintColor.Blue, PaintColor.Orange, PaintColor.Purple, PaintColor.Green },
            new[] { PaintColor.Red, PaintColor.Yellow },
        };
        private static readonly string[] _orderNames = { "엄격", "보통", "상관없음" };
        private static readonly double[] _orderRatios = { 0.05, 0.2, 1.0 };
        private static readonly Regex _fileNamePattern = new Regex("^[A-Za-z][A-Za-z0-9_]*$");

        [SerializeField] private PaintGridView _grid;
        [SerializeField] private BoardView _recordTemplate;
        [SerializeField] private StageCatalog _catalog;
        [SerializeField] private ColorPalette _palette;
        [SerializeField] private TMP_FontAsset _gameFont;

        private readonly Dictionary<HexCoord, (PaintColor start, PaintColor target)> _cells = new Dictionary<HexCoord, (PaintColor, PaintColor)>();
        private string _name = "새 스테이지";
        private string _fileName = "NewStage";
        private string _paletteName = "";
        private string _loadedFile;
        private bool _targetLayer;
        private int _paint = 2; // 빨강
        private int _catalogIndex;
        private string _status = "";

        private PuzzleSession _record;
        private BoardView _recordView;

        private int _genShape;
        private int _genSeeds = 3;
        private int _genMoves = 5;
        private int _genPalette;
        private int _genOrder = 1;
        private bool _genAllowBlack = true;

        private void OnEnable() => _grid.Painted += OnPainted;
        private void OnDisable() => _grid.Painted -= OnPainted;

        private void Start()
        {
            if (_catalog.Stages.Count > 0) Load(_catalog.Stages[0]);
        }

        private void OnPainted(HexCoord coord)
        {
            if (_record != null) return;
            PaintColor color = (PaintColor)Math.Max(0, _paint - 1);
            if (_paint == 0)
            {
                _cells.Remove(coord);
            }
            else
            {
                if (!_cells.TryGetValue(coord, out (PaintColor start, PaintColor target) cell))
                {
                    if (_cells.Count >= MaxCells)
                    {
                        _status = $"칸은 {MaxCells}개까지예요 (GDD §6).";
                        return;
                    }
                    cell = (PaintColor.Empty, PaintColor.Empty);
                }
                _cells[coord] = _targetLayer ? (cell.start, color) : (color, cell.target);
            }
            RenderGrid();
        }

        private void RenderGrid() => _grid.Render(_cells, _targetLayer, _palette);

        private void OnGUI()
        {
            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.skin.button.fontSize = GUI.skin.label.fontSize = GUI.skin.textField.fontSize = GUI.skin.toggle.fontSize = FontSize;
            GUI.skin.label.wordWrap = true;
            GUI.skin.label.normal.textColor = GUI.skin.toggle.normal.textColor = GUI.skin.toggle.onNormal.textColor = PanelText;

            GUILayout.BeginArea(new Rect(20f, PanelTop, Screen.width / scale - 40f, ReferenceHeight - PanelTop - 20f));
            if (_record != null) RecordPanel();
            else EditPanel();
            GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        private void EditPanel()
        {
            GUILayout.BeginHorizontal();
            bool target = GUILayout.Toolbar(_targetLayer ? 1 : 0, new[] { "시작 칠하기", "목표 칠하기" }) == 1;
            if (target != _targetLayer)
            {
                _targetLayer = target;
                RenderGrid();
            }
            GUILayout.Label($"{_cells.Count}칸", GUILayout.Width(120f));
            GUILayout.EndHorizontal();
            _paint = GUILayout.SelectionGrid(_paint, _paintNames, 5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("획 긋기 시작")) StartRecording();
            if (GUILayout.Button("풀이 검사")) _status = Describe(Solve(BuildStage(null)));
            if (GUILayout.Button("모두 지우기"))
            {
                _cells.Clear();
                RenderGrid();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"모양: {_shapeNames[_genShape]}")) _genShape = (_genShape + 1) % _shapeNames.Length;
            if (GUILayout.Button($"시작 색 칸 {_genSeeds}")) _genSeeds = _genSeeds >= 6 ? 2 : _genSeeds + 1; // 2~6 순환
            if (GUILayout.Button($"목표 수 {_genMoves}")) _genMoves = _genMoves >= 8 ? 2 : _genMoves + 1; // 2~8 순환
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"색: {_genPaletteNames[_genPalette]}")) _genPalette = (_genPalette + 1) % _genPalettes.Length;
            if (GUILayout.Button($"순서: {_orderNames[_genOrder]}")) _genOrder = (_genOrder + 1) % _orderNames.Length;
            _genAllowBlack = GUILayout.Toggle(_genAllowBlack, "검정 허용");
            if (GUILayout.Button("랜덤 생성")) Generate();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("이름", GUILayout.Width(80f));
            _name = GUILayout.TextField(_name);
            GUILayout.Label("파일", GUILayout.Width(80f));
            _fileName = GUILayout.TextField(_fileName);
            GUILayout.Label("팔레트", GUILayout.Width(110f));
            _paletteName = GUILayout.TextField(_paletteName, GUILayout.Width(160f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            int count = _catalog.Stages.Count;
            if (GUILayout.Button("◀", GUILayout.Width(80f))) _catalogIndex = (_catalogIndex + count - 1) % count;
            GUILayout.Label($"{_catalogIndex + 1}/{count} {_catalog.Stages[_catalogIndex].name}", GUILayout.Width(360f));
            if (GUILayout.Button("▶", GUILayout.Width(80f))) _catalogIndex = (_catalogIndex + 1) % count;
            if (GUILayout.Button("불러오기")) Load(_catalog.Stages[_catalogIndex]);
            if (GUILayout.Button("저장")) Save();
            GUILayout.EndHorizontal();
        }

        private void RecordPanel()
        {
            GUILayout.Label($"획을 그어 목표를 만드세요 — {_record.MoveCount}획 (게임과 같은 드래그 · 방향 버튼)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("되돌리기") && _record.Undo()) _recordView.Render();
            if (GUILayout.Button("취소")) StopRecording(false);
            if (GUILayout.Button("이 모습을 목표로 저장")) StopRecording(true);
            GUILayout.EndHorizontal();
        }

        // 획 기록: 지금 시작 상태로 게임 보드를 띄운다(목표 없음 — 마커 · 막힘 표시 끔)
        private void StartRecording()
        {
            if (_cells.Count < 2)
            {
                _status = "칸이 두 개 이상 필요해요.";
                return;
            }
            var stage = new Stage(_name, _cells.Select(c => new StageCell(c.Key, c.Value.start, PaintColor.Empty)).ToArray());
            _record = new PuzzleSession(new Board(stage));
            _recordView = Instantiate(_recordTemplate, _recordTemplate.transform.parent);
            _recordView.gameObject.SetActive(true);
            _recordView.Build(_record, _palette);
            _recordView.BrushRequested += OnRecordBrush;
            _recordView.Render();
            _grid.gameObject.SetActive(false);
            _status = "";
        }

        private void OnRecordBrush(int cell, HexDirection dir)
        {
            if (_record.Brush(cell, dir)) _recordView.Render();
        }

        private void StopRecording(bool keep)
        {
            if (keep)
            {
                for (int i = 0; i < _record.Board.CellCount; i++)
                {
                    HexCoord coord = _record.Board.CoordOf(i);
                    _cells[coord] = (_cells[coord].start, _record.ColorAt(i));
                }
                _targetLayer = true;
                _status = $"{_record.MoveCount}획으로 만든 모습을 목표로 저장했어요. 풀이 검사로 더 짧은 풀이가 있는지 확인해 보세요.";
            }
            _recordView.BrushRequested -= OnRecordBrush;
            Destroy(_recordView.gameObject);
            _recordView = null;
            _record = null;
            _grid.gameObject.SetActive(true);
            RenderGrid();
        }

        private void Generate()
        {
            var options = new GeneratorOptions
            {
                Shape = (BoardShape)_genShape,
                Seeds = _genSeeds,
                Moves = _genMoves,
                MinSolve = _genMoves,
                Palette = _genPalettes[_genPalette],
                AllowBlack = _genAllowBlack,
                MaxOrderRatio = _orderRatios[_genOrder],
            };
            uint seed = (uint)Environment.TickCount;
            var random = new SeededRandom(seed);
            float until = Time.realtimeSinceStartup + GenerateSeconds;
            GeneratedStage generated = null;
            while (generated == null && Time.realtimeSinceStartup < until) generated = Generator.Generate(options, random, "랜덤");
            if (generated == null)
            {
                _status = "조건에 맞는 스테이지를 찾지 못했어요. 목표 수를 줄이거나 순서 조건을 완화해 보세요.";
                return;
            }
            SetStage(generated.Stage);
            _name = "랜덤";
            _fileName = $"Random{seed}";
            _loadedFile = null;
            string order = generated.Order.HasValue ? $", 순서 {generated.Order.Value.Succeeded}/{generated.Order.Value.Total}" : "";
            _status = $"생성: {_shapeNames[(int)generated.Shape]} {generated.Stage.Cells.Count}칸, 최소 {generated.Stage.MinMoves}수{order} (시드 {seed})";
        }

        private void Load(TextAsset asset)
        {
            try
            {
                Stage stage = Stage.Parse(asset.text);
                SetStage(stage);
                _name = stage.Name;
                _fileName = asset.name;
                _paletteName = stage.Palette ?? "";
                _loadedFile = asset.name;
                _status = $"'{asset.name}'을(를) 불러왔어요.";
            }
            catch (FormatException e)
            {
                _status = e.Message;
            }
        }

        // 편집 범위(반지름 4) 안으로 옮겨 담는다 — 프로토타입 fromLevel과 같은 가운데 맞춤
        private void SetStage(Stage stage)
        {
            int minQ = stage.Cells.Min(c => c.Coord.Q), maxQ = stage.Cells.Max(c => c.Coord.Q);
            int minR = stage.Cells.Min(c => c.Coord.R), maxR = stage.Cells.Max(c => c.Coord.R);
            int centerQ = (int)Math.Floor((minQ + maxQ) / 2.0 + 0.5), centerR = (int)Math.Floor((minR + maxR) / 2.0 + 0.5);
            _cells.Clear();
            foreach (StageCell cell in stage.Cells)
                _cells[new HexCoord(cell.Coord.Q - centerQ, cell.Coord.R - centerR)] = (cell.Start, cell.Target);
            _targetLayer = false;
            RenderGrid();
        }

        // 칸 순서는 프로토타입 toLevel과 같이 r → q
        private Stage BuildStage(int? minMoves)
        {
            StageCell[] cells = _cells.OrderBy(c => c.Key.R).ThenBy(c => c.Key.Q)
                .Select(c => new StageCell(c.Key, c.Value.start, c.Value.target)).ToArray();
            return new Stage(_name, cells, string.IsNullOrWhiteSpace(_paletteName) ? null : _paletteName.Trim(), minMoves);
        }

        private static SolveResult Solve(Stage stage)
        {
            var board = new Board(stage);
            return Solver.Solve(board, board.CreateStartState());
        }

        private string Describe(SolveResult result)
        {
            if (result.DeadAtStart) return "시작 상태부터 목표와 맞지 않는 칸이 있어요. 목표 색에 없는 색이 시작 칸에 들어가 있는지 확인하세요.";
            if (result.Limited) return $"경우의 수가 너무 많아 끝까지 검사하지 못했어요(상태 {result.Explored:N0}개). 칸 수나 시작 색을 줄여 보세요.";
            if (!result.Solved) return "풀 수 없는 스테이지예요.";
            if (result.Path.Count == 0) return "시작 상태가 이미 목표와 같아요.";
            var board = new Board(BuildStage(null));
            OrderSensitivity? order = Solver.MeasureOrder(board, board.CreateStartState(), result.Path);
            string orderText = order.HasValue ? $" 최적 풀이의 순서를 바꾸면 {order.Value.Total}가지 중 {order.Value.Succeeded}가지만 성공해요." : " (8수 이상은 순서 민감도를 계산하지 않아요)";
            return $"풀 수 있어요. 최소 {result.Path.Count}수, 탐색한 상태 {result.Explored:N0}개.{orderText}";
        }

        // 저장: 솔버가 minMoves를 채우고, JSON을 쓰고, 목록에 없으면 끝에 등록한다
        private void Save()
        {
            if (_cells.Count < 2)
            {
                _status = "칸이 두 개 이상 필요해요.";
                return;
            }
            if (!_fileNamePattern.IsMatch(_fileName))
            {
                _status = "파일 이름은 영문자로 시작하는 영문 · 숫자 · _ 만 써요 (예: Grape2).";
                return;
            }
            string path = $"{StageFolder}/{_fileName}.json";
            if (File.Exists(path) && _fileName != _loadedFile)
            {
                _status = $"'{_fileName}.json'이 이미 있어요. 다른 파일 이름을 쓰거나, 그 스테이지를 불러온 뒤 저장하세요.";
                return;
            }
            SolveResult result = Solve(BuildStage(null));
            if (!result.Solved && !result.Limited || result.Solved && result.Path.Count == 0)
            {
                _status = "저장하지 않았어요 — " + Describe(result);
                return;
            }

            Stage stage = BuildStage(result.Solved ? result.Path.Count : (int?)null);
            File.WriteAllText(path, StageWriter.ToJson(stage) + "\n", new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (!_catalog.Stages.Contains(asset))
            {
                var so = new SerializedObject(_catalog);
                SerializedProperty stages = so.FindProperty("_stages");
                stages.arraySize++;
                stages.GetArrayElementAtIndex(stages.arraySize - 1).objectReferenceValue = asset;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
            _loadedFile = _fileName;
            _catalogIndex = _catalog.Stages.ToList().IndexOf(asset);

            string min = result.Solved ? $"최소 {result.Path.Count}수" : "최소 수 없음(탐색 상한)";
            string font = _gameFont.HasCharacters(_name, out List<char> missing) ? "" :
                $" 게임 폰트에 없는 글자 [{new string(missing.ToArray())}] — AgentScripts/Phase2Font.cs를 다시 실행하세요.";
            _status = $"'{path}'에 저장했어요({min}, 목록 {_catalogIndex + 1}번째).{font}";
        }
    }
}
