using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ColoringBoot.Game
{
    // 스테이지 선택 화면 — 목록 순서대로 육각 번호 버튼(격자 배치는 _grid의 GridLayoutGroup), 차례 해금 · 클리어 · 완벽 표시 (GDD §5)
    public sealed class StageSelectView : MonoBehaviour
    {
        private const string LockedNotice = "앞 스테이지를 먼저 풀어 주세요";

        [SerializeField] private TMP_Text _title;
        [SerializeField] private RectTransform _grid;
        [SerializeField] private StageButtonView _buttonTemplate;
        [SerializeField] private GameObject _noticePanel;   // 안내 띠(바탕) — 글자는 _notice
        [SerializeField] private TMP_Text _notice;
        [SerializeField] private float _noticeSeconds = 2f;
        [SerializeField] private string _chapterName = "챕터 1";
        [SerializeField] private Color _lockedFill = new Color32(0xB3, 0xBD, 0xB7, 0xFF);
        [SerializeField] private Color _openFill = new Color32(0xF6, 0xF7, 0xF3, 0xFF);
        [SerializeField] private Color _clearedFill = new Color32(0x1C, 0x22, 0x2C, 0xFF);
        [SerializeField] private Color _openText = new Color32(0x1C, 0x22, 0x2C, 0xFF);
        [SerializeField] private Color _clearedText = new Color32(0xF6, 0xF7, 0xF3, 0xFF);

        private readonly List<StageButtonView> _buttons = new List<StageButtonView>();
        private IReadOnlyList<string> _stages;
        private IReadOnlyList<int?> _minMoves;
        private SaveData _data;
        private WaitForSeconds _noticeWait;
        private Coroutine _noticeRoutine;

        // 고른 스테이지(열린 것만 — 잠긴 버튼은 안내만 띄운다)
        public event Action<int> StageChosen;

        // stages: 스테이지 파일 이름(기록의 키), minMoves: 스테이지별 최소 수(완벽 판정)
        public void Show(IReadOnlyList<string> stages, IReadOnlyList<int?> minMoves, SaveData data)
        {
            _stages = stages;
            _minMoves = minMoves;
            _data = data;
            gameObject.SetActive(true);
            _noticePanel.SetActive(false);
            while (_buttons.Count < stages.Count)
            {
                int index = _buttons.Count;
                StageButtonView button = Instantiate(_buttonTemplate, _grid);
                button.gameObject.SetActive(true);
                button.Button.onClick.AddListener(() => OnClicked(index));
                _buttons.Add(button);
            }
            Refresh();
        }

        private void Refresh()
        {
            int next = _data.Progress.NextStage(_stages);
            int cleared = 0;
            for (int i = 0; i < _buttons.Count; i++)
            {
                string stage = _stages[i];
                bool unlocked = _data.Progress.IsUnlocked(_stages, i);
                bool isCleared = _data.Progress.IsCleared(stage);
                if (isCleared) cleared++;
                Color fill = !unlocked ? _lockedFill : isCleared ? _clearedFill : _openFill;
                _buttons[i].Set(i + 1, unlocked, _data.Progress.IsPerfect(stage, _minMoves[i]), i == next, fill, isCleared ? _clearedText : _openText);
            }
            _title.text = $"{_chapterName} · {cleared} / {_stages.Count}";
        }

        private void OnClicked(int index)
        {
            if (_data.Progress.IsUnlocked(_stages, index))
            {
                StageChosen?.Invoke(index);
                return;
            }
            if (_noticeRoutine != null) StopCoroutine(_noticeRoutine);
            _noticeRoutine = StartCoroutine(ShowNotice());
        }

        private IEnumerator ShowNotice()
        {
            _notice.text = LockedNotice;
            _noticePanel.SetActive(true);
            yield return _noticeWait ??= new WaitForSeconds(_noticeSeconds);
            _noticePanel.SetActive(false);
            _noticeRoutine = null;
        }
    }
}
