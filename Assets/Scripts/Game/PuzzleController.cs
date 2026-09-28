using System;
using ColoringBoot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 한 판 진행 — 스테이지를 읽어 세션을 만들고, 보드 입력을 붓질로 옮기고, 클리어 · 막힘 안내와 처음부터 버튼을 맞춘다
    public sealed class PuzzleController : MonoBehaviour
    {
        [SerializeField] private TextAsset _stageCode;
        [SerializeField] private ColorPalette _palette;
        [SerializeField] private BoardView _boardView;
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
            _boardView.Build(_session.Board, _palette);
        }

        private void OnEnable()
        {
            _boardView.BrushRequested += OnBrushRequested;
            _restartButton.onClick.AddListener(Restart);
            Refresh();
        }

        private void OnDisable()
        {
            _boardView.BrushRequested -= OnBrushRequested;
            _restartButton.onClick.RemoveListener(Restart);
        }

        private void OnBrushRequested(int cell, HexDirection dir)
        {
            if (_session.IsSolved) return;
            if (_session.Brush(cell, dir)) Refresh();
        }

        private void Restart()
        {
            _session.Restart();
            Refresh();
        }

        private void Refresh()
        {
            _boardView.Render(_session);
            bool solved = _session.IsSolved;
            _boardView.SetInteractable(!solved); // 클리어하면 보드 입력을 잠근다
            _clearBanner.SetActive(solved);
            _stuckBanner.SetActive(!solved && _session.IsDead);
            _restartButton.interactable = _session.MoveCount > 0;
        }
    }
}
