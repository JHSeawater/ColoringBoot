using System;
using System.Collections;
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
        [SerializeField] private PaletteCatalog _palettes;
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
        // 붓질 연출 (Phase 7.4): 긋기 전 붓 경로와 그 칸들의 색 — 스테이지를 열 때 칸 수만큼 만든다
        private int[] _path;
        private PaintColor[] _pathBrushes;
        private PaintColor[] _pathBefore;
        private Coroutine _afterStroke;   // 물결이 끝난 뒤 클리어 반응 · 막힘 흔들림 → 안내 띠

        // 이번에 연 판에서 처음 클리어했는가 — 그림 칠하기 연출(GameFlow, Phase 5). 되돌렸다 다시 풀어도 유지
        public bool FirstClear { get; private set; }

        // 스테이지를 연다(같은 보드 화면을 다시 쓴다). 스테이지 코드를 읽지 못하면 false.
        // paletteOverride: 주소 ?palette= (QA — 모든 스테이지를 그 팔레트로), 없으면 스테이지의 palette
        public bool Open(TextAsset asset, int number, SaveData data, string paletteOverride = null)
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
            ColorPalette palette = ChoosePalette(paletteOverride ?? stage.Palette, asset.name);

            _data = data;
            _stageId = asset.name;
            FirstClear = false;
            StopAfterStroke();
            _session = new PuzzleSession(new Board(stage));
            _minMoves = stage.MinMoves;
            int cells = _session.Board.CellCount;
            _path = new int[cells];
            _pathBrushes = new PaintColor[cells];
            _pathBefore = new PaintColor[cells];
            _boardView.Build(_session, palette);
            _targetView.Build(_session, palette);
            _targetView.SetInteractable(false);
            _mixTable.Build(palette);
            _boardView.SetSymbols(_symbols);
            _targetView.SetSymbols(_symbols);
            _stageName.text = $"{number}. {stage.Name}";

            _data.Stats.Opened(_stageId);
            _data.SaveStats();
            Refresh();
            return true;
        }

        // 스테이지의 팔레트 이름 → 팔레트 에셋. 이름이 없으면 기본, 모르는 이름이면 기본 + 경고
        private ColorPalette ChoosePalette(string id, string stage)
        {
            int index = _palettes.IndexOf(id);
            if (index >= 0) return _palettes.Palettes[index];
            Debug.LogWarning($"스테이지 '{stage}'의 팔레트 '{id}'가 목록에 없어 기본 팔레트를 씁니다", this);
            return _palettes.Default;
        }

        // 보드를 떠난다(목록으로). 풀던 판은 저장하지 않는다(2026-09-30 사용자 결정)
        public void Close()
        {
            StopAfterStroke();
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
            int count = _session.Trace(cell, dir, _path, _pathBrushes);
            for (int k = 0; k < count; k++) _pathBefore[k] = _session.ColorAt(_path[k]);
            bool wasDead = _session.IsDead;
            if (!_session.Brush(cell, dir)) return;
            _data.Stats.Stroked(_stageId);
            if (_session.IsSolved)
            {
                if (!_data.Progress.IsCleared(_stageId)) FirstClear = true;
                _data.Progress.RecordClear(_stageId, _session.MoveCount);
                _data.Stats.Cleared(_stageId, _session.MoveCount);
                _data.SaveProgress();
            }
            _data.SaveStats();
            // 클리어 · 새로 막힘은 물결이 끝난 뒤 반응과 함께 안내 띠를 띄운다(이미 막혀 있었으면 띠는 그대로)
            bool react = _session.IsSolved || (_session.IsDead && !wasDead);
            StopAfterStroke();
            Refresh(react);
            float wave = _boardView.PlayStroke(_path, _pathBefore, count);
            if (react) _afterStroke = StartCoroutine(AfterStroke(wave, _session.IsSolved));
        }

        private IEnumerator AfterStroke(float wave, bool solved)
        {
            yield return Wait(wave);
            if (solved)
            {
                yield return Wait(_boardView.PlayClear());
                _clearBanner.SetActive(true);
            }
            else
            {
                _boardView.PlayShake();
                _stuckBanner.SetActive(true);
            }
            _afterStroke = null;
        }

        private static IEnumerator Wait(float seconds)
        {
            for (float time = 0f; time < seconds; time += Time.unscaledDeltaTime) yield return null;
        }

        private void StopAfterStroke()
        {
            if (_afterStroke == null) return;
            StopCoroutine(_afterStroke);
            _afterStroke = null;
        }

        private void Undo()
        {
            if (_session == null || !_session.Undo()) return;
            StopAfterStroke();
            _data.Stats.Undid(_stageId);
            _data.SaveStats();
            Refresh();
        }

        private void Restart()
        {
            if (_session == null || _session.MoveCount == 0) return;
            StopAfterStroke();
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

        // holdBanners: 클리어 · 막힘 띠를 아직 띄우지 않는다(붓질 연출 뒤 AfterStroke가 띄움)
        private void Refresh(bool holdBanners = false)
        {
            _boardView.Render();
            _targetView.Render();
            // 수 카운터: 현재 / 최소 (최소 수가 없는 스테이지는 현재만) · 최고 기록
            int? best = _data.Progress.BestMoves(_stageId);
            string counter = _minMoves.HasValue ? $"{_session.MoveCount} / {_minMoves.Value}수" : $"{_session.MoveCount}수";
            _moveCounter.text = best.HasValue ? $"{counter} · 최고 {best.Value}" : counter;
            bool solved = _session.IsSolved;
            _boardView.SetInteractable(!solved); // 클리어하면 보드 입력을 잠근다(되돌리면 풀린다)
            _clearBanner.SetActive(solved && !holdBanners);
            if (solved)
            {
                _clearLabel.text = _data.Progress.IsPerfect(_stageId, _minMoves) ? $"완벽! 최소 {best.Value}수" : $"완성! 최고 {best.Value}수";
            }
            _stuckBanner.SetActive(!solved && _session.IsDead && !holdBanners);
            _undoButton.interactable = _session.MoveCount > 0;
            _restartButton.interactable = _session.MoveCount > 0;
        }
    }
}
