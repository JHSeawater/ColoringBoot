using System;
using ColoringBoot.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 한 판 진행 — 스테이지를 읽어 세션을 만들고, 보드 입력 · 키보드를 붓질 · 되돌리기로 옮기고, 안내와 버튼 상태를 맞춘다
    public sealed class PuzzleController : MonoBehaviour
    {
        [SerializeField] private TextAsset _stageCode;
        [SerializeField] private ColorPalette _palette;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private GameObject _clearBanner;
        [SerializeField] private GameObject _stuckBanner;

        private PuzzleSession _session;

        private void Awake()
        {
            Stage stage;
            try
            {
                stage = Stage.Parse(_stageCode.text);
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
        }

        private void OnEnable()
        {
            _boardView.BrushRequested += OnBrushRequested;
            _undoButton.onClick.AddListener(Undo);
            _restartButton.onClick.AddListener(Restart);
            Refresh();
        }

        private void OnDisable()
        {
            _boardView.BrushRequested -= OnBrushRequested;
            _undoButton.onClick.RemoveListener(Undo);
            _restartButton.onClick.RemoveListener(Restart);
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

        private void Refresh()
        {
            _boardView.Render();
            bool solved = _session.IsSolved;
            _boardView.SetInteractable(!solved); // 클리어하면 보드 입력을 잠근다(되돌리면 풀린다)
            _clearBanner.SetActive(solved);
            _stuckBanner.SetActive(!solved && _session.IsDead);
            _undoButton.interactable = _session.MoveCount > 0;
            _restartButton.interactable = _session.MoveCount > 0;
        }
    }
}
