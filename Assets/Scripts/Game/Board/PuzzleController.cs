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
        [SerializeField] private GameObject[] _overlays;   // 보드를 덮는 패널(옵션 · 기록) — 하나라도 열려 있으면 키보드 입력을 받지 않는다
        [SerializeField] private Button _hintButton;
        [SerializeField] private GameObject _guideBanner;  // 따라 하기 · 힌트 안내 한 줄 (2026-10-07)
        [SerializeField] private TMP_Text _guideLabel;

        private const int HintLimit = 200000;     // 힌트 탐색 상한(상태 하나당) — 지금 스테이지 최악 4만여 개(2026-10-07 에디터 실측 327 ms)
        private const long SlowHintMs = 50;       // 힌트 계산이 이보다 오래 걸리면 콘솔에 남긴다(휴대폰 확인용)
        private const string HintNotFound = "힌트를 찾지 못했어요";
        private const float GoalPulseScale = 0.12f;   // 따라 하기 1 클리어: 목표 그림이 커졌다 작아지는 정도 · 한 번 걸리는 초
        private const float GoalPulseCycle = 1f;

        private IAdService _ads;
        private int _lesson = -1;    // 따라 하기 레슨(-1 = 아님) — TutorialLessons
        private bool _trapDone;      // 레슨의 일부러 막혀 보기를 마쳤는가
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
        private Coroutine _goalPulse;     // 목표 그림 강조(따라 하기 1 클리어 띠가 떠 있는 동안)

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
            _lesson = -1;
            _trapDone = false;
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

        // 따라 하기 레슨을 연다(TutorialLessons의 lesson번째) — 정해진 획만 그을 수 있고, 손가락 표시와 한 줄 설명이 나온다
        public bool OpenLesson(TextAsset asset, int lesson, SaveData data)
        {
            if (!Open(asset, lesson + 1, data)) return false;
            _lesson = lesson;
            Refresh();
            return true;
        }

        // 광고 자리 — 힌트는 보상형 광고를 거친다(지금은 NoAdService라 바로 준다)
        public void SetAds(IAdService ads) => _ads = ads;

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
            StopGoalPulse();
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
            _hintButton.onClick.AddListener(RequestHint);
        }

        private void OnDisable()
        {
            _boardView.BrushRequested -= OnBrushRequested;
            _undoButton.onClick.RemoveListener(Undo);
            _stuckUndoButton.onClick.RemoveListener(Undo);
            _restartButton.onClick.RemoveListener(Restart);
            _hintButton.onClick.RemoveListener(RequestHint);
        }

        // PC 입력 (CLAUDE.md §6): Ctrl+Z 되돌리기 · 방향키 칸 선택 · 숫자키 1 · 3 · 5 · 7 · 9 · 0(11시) 붓질 · Esc 선택 해제
        private void Update()
        {
            if (_session == null) return;
            // 걸린 시간은 앱이 앞에 있고 아직 풀지 못했을 때만 센다
            if (!_session.IsSolved && Application.isFocused) _data.Stats.AddTime(_stageId, Time.unscaledDeltaTime);
            // 패널이 보드를 덮고 있으면 키보드로 뒤의 보드를 움직이지 않는다 (2026-10-05 코드 점검 8)
            if (IsCovered()) return;

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
            // 따라 하기: 안내한 줄 · 방향만 긋는다(같은 줄의 어느 칸이든 — 혼자 풀기 레슨은 아무 획이나). 아니면 안내를 다시 보인다
            if (_lesson >= 0 && !TutorialLessons.IsFree(_lesson) && !LessonAllows(cell, dir))
            {
                ShowLessonGuide();
                return;
            }
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
                if (_lesson >= 0 && TutorialLessons.PointsAtGoal(_lesson)) _goalPulse = StartCoroutine(PulseGoal());
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

        // 목표 그림 강조: 커졌다 작아지기를 되풀이한다(멈추면 원래 크기)
        private IEnumerator PulseGoal()
        {
            Transform goal = _targetView.transform;
            for (float time = 0f; ; time += Time.unscaledDeltaTime)
            {
                float k = 0.5f - 0.5f * Mathf.Cos(time / GoalPulseCycle * 2f * Mathf.PI);
                goal.localScale = Vector3.one * (1f + GoalPulseScale * k);
                yield return null;
            }
        }

        private void StopGoalPulse()
        {
            if (_goalPulse != null) StopCoroutine(_goalPulse);
            _goalPulse = null;
            _targetView.transform.localScale = Vector3.one;
        }

        private bool IsCovered()
        {
            foreach (GameObject overlay in _overlays)
            {
                if (overlay != null && overlay.activeInHierarchy) return true;
            }
            return false;
        }

        // 되돌리기 · 처음부터는 끌던 획을 버린다 — 바뀐 상태에 옛 미리보기 · 옛 획이 남지 않게 (2026-10-05 코드 점검 9)
        private void Undo()
        {
            bool wasDead = _session != null && _session.IsDead;
            if (_session == null || !_session.Undo()) return;
            if (_lesson >= 0 && wasDead) _trapDone = true;   // 따라 하기: 막혔다가 되돌리면 본 풀이로
            StopAfterStroke();
            _boardView.CancelDrag();
            _data.Stats.Undid(_stageId);
            _data.SaveStats();
            Refresh();
        }

        private void Restart()
        {
            if (_session == null || _session.MoveCount == 0) return;
            if (_lesson >= 0 && _session.IsDead) _trapDone = true;
            StopAfterStroke();
            _boardView.CancelDrag();
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
            StopGoalPulse();
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
                if (_lesson >= 0) _clearLabel.text = TutorialLessons.Done(_lesson);
                else _clearLabel.text = _data.Progress.IsPerfect(_stageId, _minMoves) ? $"완벽! 최소 {best.Value}수" : $"완성! 최고 {best.Value}수";
            }
            _stuckBanner.SetActive(!solved && _session.IsDead && !holdBanners);
            _undoButton.interactable = _session.MoveCount > 0;
            _restartButton.interactable = _session.MoveCount > 0;
            // 안내(따라 하기 · 힌트)는 상태가 바뀔 때마다 지운다 — 따라 하기면 다음 획 안내를 다시 보인다. 따라 하기에서는 힌트를 쓰지 않는다(버튼은 흐리게 — 혼자 풀기 레슨만 씀)
            _boardView.HideGuide();
            _guideBanner.SetActive(false);
            _hintButton.interactable = !solved && (_lesson < 0 || TutorialLessons.IsFree(_lesson));
            if (_lesson >= 0 && !solved) ShowLessonGuide();
        }

        // 따라 하기: 지금 그을 획이 있으면 손가락 표시 · 한 줄 설명(막혀서 되돌려야 할 때는 막힘 띠의 되돌리기를 쓴다). 혼자 풀기 레슨은 안내 한 줄만
        private void ShowLessonGuide()
        {
            if (_session.IsDead) return;
            if (TutorialLessons.IsFree(_lesson))
            {
                ShowGuideText(TutorialLessons.Note(_lesson));
                return;
            }
            if (!TutorialLessons.TryGetGuide(_lesson, _session.MoveCount, _trapDone, out TutorialLessons.Guide guide)) return;
            _boardView.ShowGuide(_session.Board.IndexOf(guide.Cell), guide.Direction);
            ShowGuideText(guide.Caption);
        }

        // 안내한 줄 · 방향인가 — 같은 줄이면 어느 칸에서 그어도 결과가 같다(CLAUDE.md §3)
        private bool LessonAllows(int cell, HexDirection dir)
        {
            if (_session.IsDead || !TutorialLessons.TryGetGuide(_lesson, _session.MoveCount, _trapDone, out TutorialLessons.Guide guide)) return false;
            return dir == guide.Direction && _session.Board.CoordOf(cell).LineKey(dir) == guide.Cell.LineKey(dir);
        }

        // 힌트 버튼 — 보상형 광고 자리를 거친다(지금은 바로). 횟수 제한 없음 · "완벽"과 무관 (GDD §5 · §6, 2026-10-07)
        private void RequestHint()
        {
            if (_session == null || _session.IsSolved || (_lesson >= 0 && !TutorialLessons.IsFree(_lesson))) return;
            _ads.ShowRewarded(ShowHint);
        }

        private void ShowHint()
        {
            if (_session == null || _session.IsSolved) return;
            if (_afterStroke != null)
            {
                // 붓질 연출 중이면 끝난 모습으로 정리한 뒤(막힘 띠가 나중에 겹쳐 뜨지 않게)
                StopAfterStroke();
                Refresh();
            }
            _boardView.CancelDrag();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Hint hint = _session.FindHint(HintLimit);
            if (watch.ElapsedMilliseconds > SlowHintMs) Debug.Log($"힌트 계산 {watch.ElapsedMilliseconds} ms ({_stageId})", this);
            _data.Stats.Hinted(_stageId);
            _data.SaveStats();

            switch (hint.Kind)
            {
                case HintKind.Next:
                    // 줄의 출발 끝(붓이 처음 지나는 칸)에서 그 방향으로 끄는 모습을 보인다
                    _session.Trace(hint.Move.Cell, hint.Move.Direction, _path, _pathBrushes);
                    _boardView.ShowGuide(_path[0], hint.Move.Direction);
                    ShowGuideText($"표시한 대로 그어 보세요 · 남은 {hint.Count}수");
                    break;
                case HintKind.Undo:
                    ShowGuideText($"지금은 풀 수 없어요 · {hint.Count}수 되돌려 보세요");
                    break;
                default:
                    ShowGuideText(HintNotFound);
                    break;
            }
        }

        // 안내 띠는 막힘 띠와 같은 자리 — 안내를 보이는 동안 막힘 띠를 가린다(다음 Refresh에서 돌아온다)
        private void ShowGuideText(string text)
        {
            _stuckBanner.SetActive(false);
            _guideLabel.text = text;
            _guideBanner.SetActive(true);
        }
    }
}
