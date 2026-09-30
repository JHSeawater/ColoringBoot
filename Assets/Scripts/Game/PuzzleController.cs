using System;
using ColoringBoot.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 한 판 진행 — 스테이지를 읽어 세션을 만들고, 보드 입력 · 키보드를 붓질 · 되돌리기로 옮기고, 안내와 버튼 상태를 맞춘다.
    // 클리어 기록(Progress)과 플레이테스트 기록(PlayStats)을 저장소에 남긴다 (Phase 4.2)
    public sealed class PuzzleController : MonoBehaviour
    {
        private const string StageQuery = "stage";
        private const string StatsQuery = "stats";   // ?stats — 플레이테스트 기록 보기
        private const string ResetQuery = "reset";   // ?reset — 진행 · 기록 지우기 (휴대폰 하나로 여러 명이 테스트할 때)
        private const string ProgressKey = "ColoringBoot.Progress";
        private const string StatsKey = "ColoringBoot.PlayStats";

        [Tooltip("스테이지 목록 — 첫 스테이지를 열고, 주소 ?stage=<파일 이름>이 있으면 그것을 연다 (스테이지 선택은 Phase 4)")]
        [SerializeField] private StageCatalog _catalog;
        [SerializeField] private ColorPalette _palette;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private BoardView _targetView;
        [SerializeField] private MixTableView _mixTable;
        [SerializeField] private TMP_Text _stageName;
        [SerializeField] private TMP_Text _moveCounter;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _symbolsButton;
        [SerializeField] private Button _stuckUndoButton;
        [SerializeField] private GameObject _clearBanner;
        [SerializeField] private GameObject _stuckBanner;
        [SerializeField] private StatsView _statsView;

        private IKeyValueStore _store;
        private Progress _progress;
        private PlayStats _stats;
        private string _stageId;   // 스테이지 파일 이름 — 기록의 키
        private PuzzleSession _session;
        private bool _symbols;
        private int? _minMoves;

        private void Awake()
        {
            string url = Application.absoluteURL;
            _store = new PlayerPrefsStore();
            if (UrlQuery.TryGet(url, ResetQuery, out _))
            {
                _store.Delete(ProgressKey);
                _store.Delete(StatsKey);
                Debug.Log("주소에 ?reset — 진행 · 플레이 기록을 지웠습니다", this);
            }
            _progress = Progress.Deserialize(_store.Load(ProgressKey));
            _stats = PlayStats.Deserialize(_store.Load(StatsKey));

            TextAsset asset = ChooseStage(url);
            _stageId = asset.name;
            Stage stage;
            try
            {
                stage = Stage.Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"스테이지 코드를 읽지 못했습니다: {e.Message}", this);
                enabled = false;
                return;
            }

            if (stage.Palette != null)
                Debug.LogWarning($"팔레트 '{stage.Palette}' 대신 기본 팔레트를 씁니다 — 팔레트 선택은 Phase 4", this);

            _session = new PuzzleSession(new Board(stage));
            _boardView.Build(_session, _palette);
            _targetView.Build(_session, _palette);
            _targetView.SetInteractable(false);
            _mixTable.Build(_palette);
            _stageName.text = stage.Name;
            _minMoves = stage.MinMoves;

            _stats.Opened(_stageId);
            SaveStats();
            if (UrlQuery.TryGet(url, StatsQuery, out _)) _statsView.Show(_catalog, _stats);
        }

        // 주소에 ?stage=<이름>이 있으면 목록에서 그 스테이지, 아니면 목록의 첫 스테이지 (WebGL만 — 에디터는 주소가 비어 있다)
        private TextAsset ChooseStage(string url)
        {
            TextAsset first = _catalog.Stages[0];
            if (!UrlQuery.TryGet(url, StageQuery, out string name)) return first;
            TextAsset stage = _catalog.Find(name);
            if (stage != null) return stage;
            Debug.LogWarning($"주소의 스테이지 '{name}'를 찾지 못해 첫 스테이지를 엽니다", this);
            return first;
        }

        private void OnEnable()
        {
            _boardView.BrushRequested += OnBrushRequested;
            _undoButton.onClick.AddListener(Undo);
            _stuckUndoButton.onClick.AddListener(Undo);
            _restartButton.onClick.AddListener(Restart);
            _symbolsButton.onClick.AddListener(ToggleSymbols);
            Refresh();
        }

        private void OnDisable()
        {
            _boardView.BrushRequested -= OnBrushRequested;
            _undoButton.onClick.RemoveListener(Undo);
            _stuckUndoButton.onClick.RemoveListener(Undo);
            _restartButton.onClick.RemoveListener(Restart);
            _symbolsButton.onClick.RemoveListener(ToggleSymbols);
        }

        // PC 입력 (CLAUDE.md §6): Ctrl+Z 되돌리기 · 방향키 칸 선택 · 숫자키 1 · 3 · 5 · 7 · 9 · 0(11시) 붓질 · Esc 선택 해제
        private void Update()
        {
            // 걸린 시간은 앱이 앞에 있고 아직 풀지 못했을 때만 센다
            if (!_session.IsSolved && Application.isFocused) _stats.AddTime(_stageId, Time.unscaledDeltaTime);

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.zKey.wasPressedThisFrame && (keyboard.ctrlKey.isPressed || keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed))
            {
                Undo();
                return;
            }
            if (keyboard.escapeKey.wasPressedThisFrame) _boardView.ClearSelection();
            if (keyboard.rightArrowKey.wasPressedThisFrame) _boardView.MoveSelection(Vector2.right);
            if (keyboard.leftArrowKey.wasPressedThisFrame) _boardView.MoveSelection(Vector2.left);
            if (keyboard.upArrowKey.wasPressedThisFrame) _boardView.MoveSelection(Vector2.up);
            if (keyboard.downArrowKey.wasPressedThisFrame) _boardView.MoveSelection(Vector2.down);
            if (Pressed(keyboard.digit1Key, keyboard.numpad1Key)) _boardView.BrushSelected(HexDirection.Clock1);
            if (Pressed(keyboard.digit3Key, keyboard.numpad3Key)) _boardView.BrushSelected(HexDirection.Clock3);
            if (Pressed(keyboard.digit5Key, keyboard.numpad5Key)) _boardView.BrushSelected(HexDirection.Clock5);
            if (Pressed(keyboard.digit7Key, keyboard.numpad7Key)) _boardView.BrushSelected(HexDirection.Clock7);
            if (Pressed(keyboard.digit9Key, keyboard.numpad9Key)) _boardView.BrushSelected(HexDirection.Clock9);
            if (Pressed(keyboard.digit0Key, keyboard.numpad0Key)) _boardView.BrushSelected(HexDirection.Clock11);
        }

        private static bool Pressed(KeyControl digit, KeyControl numpad) => digit.wasPressedThisFrame || numpad.wasPressedThisFrame;

        private void OnBrushRequested(int cell, HexDirection dir)
        {
            if (_session.IsSolved) return;
            if (!_session.Brush(cell, dir)) return;
            _stats.Stroked(_stageId);
            if (_session.IsSolved)
            {
                _progress.RecordClear(_stageId, _session.MoveCount);
                _stats.Cleared(_stageId, _session.MoveCount);
                _store.Save(ProgressKey, _progress.Serialize());
            }
            SaveStats();
            Refresh();
        }

        private void Undo()
        {
            if (!_session.Undo()) return;
            _stats.Undid(_stageId);
            SaveStats();
            Refresh();
        }

        private void Restart()
        {
            if (_session.MoveCount == 0) return;
            _session.Restart();
            _stats.Restarted(_stageId);
            SaveStats();
            Refresh();
        }

        private void SaveStats() => _store.Save(StatsKey, _stats.Serialize());

        // 앱이 뒤로 가면 쌓인 시간을 저장한다 (WebGL은 탭을 닫을 때 알림이 없다)
        private void OnApplicationFocus(bool focus)
        {
            if (!focus && _stats != null) SaveStats();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _stats != null) SaveStats();
        }

        // 접근성 기호 켜고 끄기 (보드와 목표 썸네일 모두)
        private void ToggleSymbols()
        {
            _symbols = !_symbols;
            _boardView.SetSymbols(_symbols);
            _targetView.SetSymbols(_symbols);
        }

        private void Refresh()
        {
            _boardView.Render();
            _targetView.Render();
            // 수 카운터: 현재 / 최소 (최소 수가 없는 스테이지는 현재만)
            _moveCounter.text = _minMoves.HasValue ? $"{_session.MoveCount} / {_minMoves.Value}수" : $"{_session.MoveCount}수";
            bool solved = _session.IsSolved;
            _boardView.SetInteractable(!solved); // 클리어하면 보드 입력을 잠근다(되돌리면 풀린다)
            _clearBanner.SetActive(solved);
            _stuckBanner.SetActive(!solved && _session.IsDead);
            _undoButton.interactable = _session.MoveCount > 0;
            _restartButton.interactable = _session.MoveCount > 0;
        }
    }
}
