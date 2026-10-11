using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 스테이지 선택 화면 — 목록 순서대로 육각 번호 버튼(격자 배치는 _grid의 GridLayoutGroup), 차례 해금 · 클리어 · 완벽 표시 (GDD §5).
    // 챕터 넘기기(2026-10-11): 작은 그림 양옆 ◀ ▶ — 잠긴 챕터는 버튼이 모두 잠기고 제목에 "잠김"
    public sealed class StageSelectView : MonoBehaviour
    {
        private const string LockedNotice = "앞 스테이지를 먼저 풀어 주세요";
        private const string ChapterLockedNotice = "앞 챕터 그림을 완성하면 열려요";
        private const string ChapterLockedTitle = "잠김";

        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;        // 챕터 부제 줄(장소와 계절) — 시험 목록(?lab)에서는 숨긴다
        [SerializeField] private RectTransform _grid;
        [SerializeField] private StageButtonView _buttonTemplate;
        [SerializeField] private GameObject _noticePanel;   // 안내 띠(바탕) — 글자는 _notice
        [SerializeField] private TMP_Text _notice;
        [SerializeField] private float _noticeSeconds = 2f;
        [SerializeField] private Button _prevChapterButton;
        [SerializeField] private Button _nextChapterButton;
        [SerializeField] private Color _lockedFill = new Color32(0xB3, 0xBD, 0xB7, 0xFF);
        [SerializeField] private Color _openFill = new Color32(0xF6, 0xF7, 0xF3, 0xFF);
        [SerializeField] private Color _clearedFill = new Color32(0x1C, 0x22, 0x2C, 0xFF);
        [SerializeField] private Color _openText = new Color32(0x1C, 0x22, 0x2C, 0xFF);
        [SerializeField] private Color _clearedText = new Color32(0xF6, 0xF7, 0xF3, 0xFF);

        private readonly List<StageButtonView> _buttons = new List<StageButtonView>();
        private IReadOnlyList<string> _stages;
        private IReadOnlyList<int?> _minMoves;
        private SaveData _data;
        private string _listName;
        private bool _allOpen;
        private bool _chapterLocked;
        private WaitForSeconds _noticeWait;
        private Coroutine _noticeRoutine;

        // 고른 스테이지(열린 것만 — 잠긴 버튼은 안내만 띄운다)
        public event Action<int> StageChosen;
        // 챕터 넘기기: -1 = 앞 챕터, +1 = 다음 챕터
        public event Action<int> ChapterStepped;

        private void OnEnable()
        {
            _prevChapterButton.onClick.AddListener(StepBack);
            _nextChapterButton.onClick.AddListener(StepForward);
        }

        private void OnDisable()
        {
            _prevChapterButton.onClick.RemoveListener(StepBack);
            _nextChapterButton.onClick.RemoveListener(StepForward);
        }

        private void StepBack() => ChapterStepped?.Invoke(-1);
        private void StepForward() => ChapterStepped?.Invoke(1);

        // stages: 스테이지 파일 이름(기록의 키), minMoves: 스테이지별 최소 수(완벽 판정).
        // title: 제목 줄의 목록 이름(챕터 제목 · "시험"), subtitle: 챕터 부제(없으면 그 줄을 숨김), allOpen: 해금과 상관없이 모두 열림(시험 목록),
        // chapterLocked: 앞 챕터를 아직 완성하지 않음 — 모두 잠김. hasPrev · hasNext: 넘길 챕터가 있으면 그쪽 화살표를 보인다
        public void Show(IReadOnlyList<string> stages, IReadOnlyList<int?> minMoves, SaveData data, string title, string subtitle, bool allOpen,
            bool chapterLocked, bool hasPrev, bool hasNext)
        {
            _stages = stages;
            _minMoves = minMoves;
            _data = data;
            _listName = title;
            _allOpen = allOpen;
            _chapterLocked = chapterLocked;
            _prevChapterButton.gameObject.SetActive(hasPrev);
            _nextChapterButton.gameObject.SetActive(hasNext);
            gameObject.SetActive(true);
            _subtitle.gameObject.SetActive(subtitle != null);
            if (subtitle != null) _subtitle.text = subtitle;
            _noticePanel.SetActive(false);
            while (_buttons.Count < stages.Count)
            {
                int index = _buttons.Count;
                StageButtonView button = Instantiate(_buttonTemplate, _grid);
                button.gameObject.SetActive(true);
                button.Button.onClick.AddListener(() => OnClicked(index));
                _buttons.Add(button);
            }
            // 목록이 바뀌어 짧아지면 남는 버튼을 숨긴다
            for (int i = 0; i < _buttons.Count; i++) _buttons[i].gameObject.SetActive(i < stages.Count);
            Refresh();
        }

        private void Refresh()
        {
            int next = _chapterLocked ? -1 : _data.Progress.NextStage(_stages);
            int cleared = 0;
            for (int i = 0; i < _stages.Count; i++)
            {
                string stage = _stages[i];
                bool unlocked = !_chapterLocked && (_allOpen || _data.Progress.IsUnlocked(_stages, i));
                bool isCleared = _data.Progress.IsCleared(stage);
                if (isCleared) cleared++;
                Color fill = !unlocked ? _lockedFill : isCleared ? _clearedFill : _openFill;
                _buttons[i].Set(i + 1, unlocked, _data.Progress.IsPerfect(stage, _minMoves[i]), i == next, fill, isCleared ? _clearedText : _openText);
            }
            _title.text = _chapterLocked ? $"{_listName} · {ChapterLockedTitle}" : $"{_listName} · {cleared} / {_stages.Count}";
        }

        private void OnClicked(int index)
        {
            if (!_chapterLocked && (_allOpen || _data.Progress.IsUnlocked(_stages, index)))
            {
                StageChosen?.Invoke(index);
                return;
            }
            if (_noticeRoutine != null) StopCoroutine(_noticeRoutine);
            _noticeRoutine = StartCoroutine(ShowNotice(_chapterLocked ? ChapterLockedNotice : LockedNotice));
        }

        private IEnumerator ShowNotice(string text)
        {
            _notice.text = text;
            _noticePanel.SetActive(true);
            yield return _noticeWait ??= new WaitForSeconds(_noticeSeconds);
            _noticePanel.SetActive(false);
            _noticeRoutine = null;
        }
    }
}
