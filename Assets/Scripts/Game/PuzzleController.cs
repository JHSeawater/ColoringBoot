using System;
using ColoringBoot.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 한 판 진행 — 스테이지를 읽어 세션을 만들고, 보드 입력 · 키보드를 붓질 · 되돌리기로 옮기고, 안내와 버튼 상태를 맞춘다
    public sealed class PuzzleController : MonoBehaviour
    {
        private const string StageQuery = "stage=";

        [SerializeField] private TextAsset _stageCode;
        [Tooltip("주소 ?stage=<파일 이름>으로 열 수 있는 스테이지 (QA용 — 스테이지 선택은 Phase 4)")]
        [SerializeField] private TextAsset[] _queryStages;
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

        private PuzzleSession _session;
        private bool _symbols;
        private int? _minMoves;

        private void Awake()
        {
            Stage stage;
            try
            {
                stage = Stage.Parse(ChooseStage().text);
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
        }

        // 주소에 ?stage=<이름>이 있으면 그 스테이지, 아니면 기본 스테이지 (WebGL만 — 에디터는 주소가 비어 있다)
        private TextAsset ChooseStage()
        {
            string url = Application.absoluteURL;
            int start = url.IndexOf(StageQuery, StringComparison.Ordinal);
            if (start < 0) return _stageCode;
            string name = url.Substring(start + StageQuery.Length);
            int end = name.IndexOfAny(new[] { '&', '#' });
            if (end >= 0) name = name.Substring(0, end);
            foreach (TextAsset stage in _queryStages)
            {
                if (string.Equals(stage.name, name, StringComparison.OrdinalIgnoreCase)) return stage;
            }
            Debug.LogWarning($"주소의 스테이지 '{name}'를 찾지 못해 기본 스테이지를 엽니다", this);
            return _stageCode;
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
            if (_session.Brush(cell, dir)) Refresh();
        }

        private void Undo()
        {
            if (_session.Undo()) Refresh();
        }

        private void Restart()
        {
            _session.Restart();
            Refresh();
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
