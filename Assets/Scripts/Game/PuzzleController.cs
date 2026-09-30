using System;
using ColoringBoot.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 한 판 진행 — GameFlow가 연 스테이지로 세션을 만들고, 보드 입력 · 키보드를 붓질 · 되돌리기로 옮기고, 안내와 버튼 상태를 맞춘다.
    // 클리어 기록(Progress)과 플레이테스트 기록(PlayStats)을 SaveData에 남긴다
    public sealed class PuzzleController : MonoBehaviour
    {
        [SerializeField] private ColorPalette _palette;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private BoardView _targetView;
        [SerializeField] private MixTableView _mixTable;
        [SerializeField] private TMP_Text _stageName;
        [SerializeField] private TMP_Text _moveCounter;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _stuckUndoButton;
        [SerializeField] private GameObject _clearBanner;
        [SerializeField] private TMP_Text _clearLabel;
        [SerializeField] private GameObject _stuckBanner;

        private SaveData _data;
        private string _stageId;   // 스테이지 파일 이름 — 기록의 키
        private PuzzleSession _session;
        private int? _minMoves;
        private bool _symbols;

        // 스테이지를 연다(같은 보드 화면을 다시 쓴다). 스테이지 코드를 읽지 못하면 false
        public bool Open(TextAsset asset, int number, SaveData data)
        {
            Stage stage;
            try
            {
                stage = Stage.Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"스테이지 '{asset.name}' 코드를 읽지 못했습니다: {e.Message}", this);
                return false;
            }
            if (stage.Palette != null)
                Debug.LogWarning($"팔레트 '{stage.Palette}' 대신 기본 팔레트를 씁니다 — 팔레트 적용은 Phase 4.4", this);

            _data = data;
            _stageId = asset.name;
            _session = new PuzzleSession(new Board(stage));
            _minMoves = stage.MinMoves;
            _boardView.Build(_session, _palette);
            _targetView.Build(_session, _palette);
            _targetView.SetInteractable(false);
            _mixTable.Build(_palette);
            _boardView.SetSymbols(_symbols);
            _targetView.SetSymbols(_symbols);
            _stageName.text = $"{number}. {stage.Name}";

            _data.Stats.Opened(_stageId);
            _data.SaveStats();
            Refresh();
            return true;
        }

        // 보드를 떠난다(목록으로). 풀던 판은 저장하지 않는다(2026-09-30 사용자 결정)
        public void Close()
        {
            if (_session != null) _data.SaveStats();
            _session = null;
        }

        // 접근성 기호 켜고 끄기 (보드와 목표 썸네일 모두) — 옵션 화면에서
        public void SetSymbols(bool visible)
        {
            _symbols = visible;
            if (_session == null) return;
            _boardView.SetSymbols(visible);
            _targetView.SetSymbols(visible);
        }

        private void OnEnable()
        {
            _boardView.BrushRequested += OnBrushRequested;
            _undoButton.onClick.AddListener(Undo);
            _stuckUndoButton.onClick.AddListener(Undo);
            _restartButton.onClick.AddListener(Restart);
        }

        private void OnDisable()
        {
            _boardView.BrushRequested -= OnBrushRequested;
            _undoButton.onClick.RemoveListener(Undo);
            _stuckUndoButton.onClick.RemoveListener(Undo);
            _restartButton.onClick.RemoveListener(Restart);
        }

        // PC 입력 (CLAUDE.md §6): Ctrl+Z 되돌리기 · 방향키 칸 선택 · 숫자키 1 · 3 · 5 · 7 · 9 · 0(11시) 붓질 · Esc 선택 해제
        private void Update()
        {
            if (_session == null) return;
            // 걸린 시간은 앱이 앞에 있고 아직 풀지 못했을 때만 센다
            if (!_session.IsSolved && Application.isFocused) _data.Stats.AddTime(_stageId, Time.unscaledDeltaTime);

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
            if (_session == null || _session.IsSolved) return;
            if (!_session.Brush(cell, dir)) return;
            _data.Stats.Stroked(_stageId);
            if (_session.IsSolved)
            {
                _data.Progress.RecordClear(_stageId, _session.MoveCount);
                _data.Stats.Cleared(_stageId, _session.MoveCount);
                _data.SaveProgress();
            }
            _data.SaveStats();
            Refresh();
        }

        private void Undo()
        {
            if (_session == null || !_session.Undo()) return;
            _data.Stats.Undid(_stageId);
            _data.SaveStats();
            Refresh();
        }

        private void Restart()
        {
            if (_session == null || _session.MoveCount == 0) return;
            _session.Restart();
            _data.Stats.Restarted(_stageId);
            _data.SaveStats();
            Refresh();
        }

        // 앱이 뒤로 가면 쌓인 시간을 저장한다 (WebGL은 탭을 닫을 때 알림이 없다)
        private void OnApplicationFocus(bool focus)
        {
            if (!focus && _session != null) _data.SaveStats();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _session != null) _data.SaveStats();
        }

        private void Refresh()
        {
            _boardView.Render();
            _targetView.Render();
            // 수 카운터: 현재 / 최소 (최소 수가 없는 스테이지는 현재만) · 최고 기록
            int? best = _data.Progress.BestMoves(_stageId);
            string counter = _minMoves.HasValue ? $"{_session.MoveCount} / {_minMoves.Value}수" : $"{_session.MoveCount}수";
            _moveCounter.text = best.HasValue ? $"{counter} · 최고 {best.Value}" : counter;
            bool solved = _session.IsSolved;
            _boardView.SetInteractable(!solved); // 클리어하면 보드 입력을 잠근다(되돌리면 풀린다)
            _clearBanner.SetActive(solved);
            if (solved)
            {
                _clearLabel.text = _data.Progress.IsPerfect(_stageId, _minMoves) ? $"완벽! 최소 {best.Value}수" : $"완성! 최고 {best.Value}수";
            }
            _stuckBanner.SetActive(!solved && _session.IsDead);
            _undoButton.interactable = _session.MoveCount > 0;
            _restartButton.interactable = _session.MoveCount > 0;
        }
    }
}
