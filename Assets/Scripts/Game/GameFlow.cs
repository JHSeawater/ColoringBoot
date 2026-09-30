using System;
using ColoringBoot.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 화면 흐름 (Phase 4.3) — 스테이지 선택 → 보드 → 다음 스테이지 · 목록, 옵션 · 기록 패널. 씬은 하나이고 패널을 켜고 끈다(다시 불러오지 않음).
    // 저장소 · 기록 · 광고 자리 · 설정을 여기서 한 번 만든다(static · 싱글턴 없음 — CLAUDE.md §5)
    public sealed class GameFlow : MonoBehaviour
    {
        private const string StageQuery = "stage";   // ?stage=<파일 이름> — 해금과 상관없이 그 스테이지를 바로 연다(QA)
        private const string StatsQuery = "stats";   // ?stats — 플레이테스트 기록 보기
        private const string PaletteQuery = "palette"; // ?palette=<이름> — 모든 스테이지를 그 팔레트로(QA · 휴대폰 색 확인)
        private const string ResetQuery = "reset";   // ?reset — 진행 · 기록 지우기 (휴대폰 하나로 여러 명이 테스트할 때)
        private const string NextText = "다음";
        private const string ListText = "목록";

        [SerializeField] private StageCatalog _catalog;
        [SerializeField] private PuzzleController _puzzle;
        [SerializeField] private SoundController _sound;
        [SerializeField] private GameObject _boardScreen;
        [SerializeField] private StageSelectView _select;
        [SerializeField] private OptionsView _options;
        [SerializeField] private StatsView _statsView;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _boardOptionsButton;
        [SerializeField] private Button _selectOptionsButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private TMP_Text _nextLabel;

        private SaveData _data;
        private IAdService _ads;
        private string[] _stages;    // 스테이지 파일 이름 — 기록의 키
        private int?[] _minMoves;
        private int _current = -1;
        private string _paletteOverride;

        private void Awake()
        {
            _data = new SaveData(new PlayerPrefsStore());
            _ads = new NoAdService();
            _stages = new string[_catalog.Stages.Count];
            _minMoves = new int?[_stages.Length];
            for (int i = 0; i < _stages.Length; i++)
            {
                _stages[i] = _catalog.Stages[i].name;
                try
                {
                    _minMoves[i] = Stage.Parse(_catalog.Stages[i].text).MinMoves;
                }
                catch (FormatException)
                {
                    // 여는 순간 PuzzleController가 오류를 알린다
                }
            }
        }

        private void OnEnable()
        {
            _backButton.onClick.AddListener(ShowSelect);
            _boardOptionsButton.onClick.AddListener(ShowOptions);
            _selectOptionsButton.onClick.AddListener(ShowOptions);
            _nextButton.onClick.AddListener(Next);
            _select.StageChosen += OpenStage;
            _options.Changed += ApplySettings;
        }

        private void OnDisable()
        {
            _backButton.onClick.RemoveListener(ShowSelect);
            _boardOptionsButton.onClick.RemoveListener(ShowOptions);
            _selectOptionsButton.onClick.RemoveListener(ShowOptions);
            _nextButton.onClick.RemoveListener(Next);
            _select.StageChosen -= OpenStage;
            _options.Changed -= ApplySettings;
        }

        // 모든 Awake · OnEnable 뒤에 첫 화면을 연다
        private void Start()
        {
            string url = Application.absoluteURL;
            if (UrlQuery.TryGet(url, ResetQuery, out _))
            {
                _data.ClearRecords();
                Debug.Log("주소에 ?reset — 진행 · 플레이 기록을 지웠습니다", this);
            }
            ApplySettings();
            if (UrlQuery.TryGet(url, PaletteQuery, out string palette) && palette.Length > 0) _paletteOverride = palette;

            int start = -1;
            if (UrlQuery.TryGet(url, StageQuery, out string name))
            {
                start = Array.FindIndex(_stages, s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase));
                if (start < 0) Debug.LogWarning($"주소의 스테이지 '{name}'를 찾지 못해 선택 화면을 엽니다", this);
            }
            if (start >= 0) OpenStage(start);
            else ShowSelect();

            if (UrlQuery.TryGet(url, StatsQuery, out _)) _statsView.Show(_catalog, _data.Stats);
        }

        private void ShowSelect()
        {
            _puzzle.Close();
            _current = -1;
            _boardScreen.SetActive(false);
            _select.Show(_stages, _minMoves, _data);
        }

        private void OpenStage(int index)
        {
            _select.gameObject.SetActive(false);
            _boardScreen.SetActive(true);
            if (!_puzzle.Open(_catalog.Stages[index], index + 1, _data, _paletteOverride))
            {
                ShowSelect();
                return;
            }
            _current = index;
            _nextLabel.text = index + 1 < _stages.Length ? NextText : ListText;
        }

        // 클리어 띠의 "다음" — 스테이지 사이 광고 자리를 거쳐 다음 스테이지. 마지막이면 목록(챕터 완성 연출은 Phase 5)
        private void Next()
        {
            if (_current < 0 || _current + 1 >= _stages.Length)
            {
                ShowSelect();
                return;
            }
            int next = _current + 1;
            _ads.ShowBetweenStages(() => OpenStage(next));
        }

        private void ShowOptions() => _options.Show(_data);

        private void ApplySettings()
        {
            _puzzle.SetSymbols(_data.Symbols);
            _sound.SetSoundOn(_data.Sound);
        }
    }
}
