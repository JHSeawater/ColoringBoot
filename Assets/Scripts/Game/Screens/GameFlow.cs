using System;
using System.Linq;
using ColoringBoot.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 화면 흐름 (Phase 4.3) — 타이틀(Phase 7.3) → 스테이지 선택 → 보드 → 다음 스테이지 · 목록, 옵션 · 기록 패널. 씬은 하나이고 패널을 켜고 끈다(다시 불러오지 않음).
    // 저장소 · 기록 · 광고 자리 · 설정을 여기서 한 번 만든다(static · 싱글턴 없음 — CLAUDE.md §5)
    public sealed class GameFlow : MonoBehaviour
    {
        private const string StageQuery = "stage";   // ?stage=<파일 이름> — 해금과 상관없이 그 스테이지를 바로 연다(QA)
        private const string StatsQuery = "stats";   // ?stats — 플레이테스트 기록 보기
        private const string PaletteQuery = "palette"; // ?palette=<이름> — 모든 스테이지를 그 팔레트로(QA · 휴대폰 색 확인)
        private const string ResetQuery = "reset";   // ?reset — 진행 · 기록 지우기 (휴대폰 하나로 여러 명이 테스트할 때)
        private const string LabQuery = "lab";       // ?lab — 시험 목록(소재 맵 시험, 2026-10-06): 모두 열림 · 그림 화면 없음
        private const string LabTitle = "시험";
        private const string NextText = "다음";
        private const string ListText = "목록";
        private const string CompleteText = "그림 완성!";
        // 무한 모드 난이도(2026-10-07): 기록의 키(EndlessProgress · 퍼즐 id "Endless{키}{번호}") · 화면 이름 — _endlessPools와 같은 순서
        private static readonly string[] EndlessKeys = { "Easy", "Normal", "Hard" };
        private static readonly string[] EndlessNames = { "쉬움", "보통", "어려움" };

        [SerializeField] private Chapter _chapter;   // 지금 여는 챕터 — 제목 · 스테이지 목록 · 그림 (Phase 8 구조 정리, 2026-10-07)
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
        [Header("챕터 그림 (Phase 5)")]
        [SerializeField] private ChapterView _selectPicture;   // 선택 화면 위쪽(작게)
        [SerializeField] private GameObject _chapterScreen;    // 처음 클리어한 뒤 크게 칠하는 화면
        [SerializeField] private ChapterView _chapterPicture;
        [SerializeField] private TMP_Text _chapterCaption;
        [SerializeField] private Button _chapterNextButton;
        [SerializeField] private TMP_Text _chapterNextLabel;
        [Header("타이틀 (Phase 7.3)")]
        [SerializeField] private GameObject _titleScreen;      // 실행하면 처음 보는 화면(주소에 ?stage=가 있으면 건너뜀)
        [SerializeField] private Button _startButton;
        [Header("시험 목록 (?lab)")]
        [SerializeField] private StageCatalog _labCatalog;
        [Header("따라 하기 (튜토리얼)")]
        [SerializeField] private StageCatalog _tutorialCatalog;   // 레슨 i = TutorialLessons의 i번째 (2026-10-07)
        [Header("무한 모드 (2026-10-07)")]
        [SerializeField] private TextAsset[] _endlessPools;    // 쉬움 · 보통 · 어려움 — Assets/Data/Endless/*.txt(한 줄에 스테이지 코드 하나)
        [SerializeField] private EndlessView _endless;
        [SerializeField] private Button _endlessButton;        // 타이틀 — 따라 하기를 본 뒤에 보인다
        [SerializeField] private Button _selectEndlessButton;  // 스테이지 목록 아래 — 타이틀은 처음에만 보이므로
        [SerializeField] private Button _skipButton;           // 보드 위쪽 가운데 — 무한 모드에서만

        private SaveData _data;
        private IAdService _ads;
        private StageCatalog _list;  // 지금 여는 목록 — 챕터(_chapter) 또는 시험 목록(_labCatalog)
        private bool _lab;
        private int _lesson = -1;    // 따라 하기 중이면 레슨 번호
        private string[] _stages;    // 스테이지 파일 이름 — 기록의 키
        private int?[] _minMoves;
        private int _current = -1;
        private bool _paintShown;    // 지금 판의 첫 클리어 연출을 이미 보여 줬는가
        private bool[] _painted;
        private string _paletteOverride;
        private int _endlessTier = -1;      // 무한 모드 중이면 난이도
        private bool _endlessFromTitle;     // 난이도 화면의 뒤로 → 타이틀(아니면 목록)
        private Stage[][] _endlessStages;   // 난이도별 퍼즐 — 처음 쓸 때 읽는다

        private void Awake()
        {
            _data = new SaveData(new PlayerPrefsStore());
            _ads = new NoAdService();
            _puzzle.SetAds(_ads);
            UseList(_chapter.Stages, false);
            if (_chapter.Art.Steps.Count != _stages.Length)
                Debug.LogWarning($"챕터 그림 단계 {_chapter.Art.Steps.Count}개 ≠ 스테이지 {_stages.Length}개 — 앞에서부터 짝짓습니다", this);
        }

        // 여는 목록을 정한다 — 스테이지 파일 이름(기록의 키)과 최소 수를 그 목록에서 읽는다. lab: 시험 목록(모두 열림 · 그림 없음)
        private void UseList(StageCatalog list, bool lab)
        {
            _list = list;
            _lab = lab;
            _stages = new string[list.Stages.Count];
            _minMoves = new int?[_stages.Length];
            for (int i = 0; i < _stages.Length; i++)
            {
                _stages[i] = list.Stages[i].name;
                try
                {
                    _minMoves[i] = Stage.Parse(list.Stages[i].text).MinMoves;
                }
                catch (FormatException)
                {
                    // 여는 순간 PuzzleController가 오류를 알린다
                }
            }
            _painted = new bool[_stages.Length];
        }

        private void OnEnable()
        {
            _backButton.onClick.AddListener(LeaveBoard);
            _boardOptionsButton.onClick.AddListener(ShowOptions);
            _selectOptionsButton.onClick.AddListener(ShowOptions);
            _nextButton.onClick.AddListener(Next);
            _chapterNextButton.onClick.AddListener(ContinueAfterPicture);
            _startButton.onClick.AddListener(StartGame);
            _endlessButton.onClick.AddListener(ShowEndlessFromTitle);
            _selectEndlessButton.onClick.AddListener(ShowEndlessFromSelect);
            _skipButton.onClick.AddListener(SkipEndless);
            _endless.TierChosen += OpenEndless;
            _endless.Back += LeaveEndless;
            _select.StageChosen += OpenStage;
            _options.Changed += ApplySettings;
            _options.TutorialRequested += StartTutorial;
        }

        private void OnDisable()
        {
            _backButton.onClick.RemoveListener(LeaveBoard);
            _boardOptionsButton.onClick.RemoveListener(ShowOptions);
            _selectOptionsButton.onClick.RemoveListener(ShowOptions);
            _nextButton.onClick.RemoveListener(Next);
            _chapterNextButton.onClick.RemoveListener(ContinueAfterPicture);
            _startButton.onClick.RemoveListener(StartGame);
            _endlessButton.onClick.RemoveListener(ShowEndlessFromTitle);
            _selectEndlessButton.onClick.RemoveListener(ShowEndlessFromSelect);
            _skipButton.onClick.RemoveListener(SkipEndless);
            _endless.TierChosen -= OpenEndless;
            _endless.Back -= LeaveEndless;
            _select.StageChosen -= OpenStage;
            _options.Changed -= ApplySettings;
            _options.TutorialRequested -= StartTutorial;
        }

        // 타이틀의 [시작] — 따라 하기를 아직 안 봤으면 그것부터, 봤으면 목록
        private void StartGame()
        {
            if (_data.TutorialSeen) ShowSelect();
            else StartTutorial();
        }

        // 따라 하기 처음부터 (첫 시작 · 옵션 "규칙 다시 보기")
        private void StartTutorial() => OpenLesson(0);

        // 따라 하기 레슨을 연다 (GDD §6, 2026-10-07) — 보드 화면을 그대로 쓰고, 클리어 띠 "다음"으로 다음 레슨 · 마지막이면 목록
        private void OpenLesson(int lesson)
        {
            _titleScreen.SetActive(false);
            _select.gameObject.SetActive(false);
            _chapterScreen.SetActive(false);
            _boardScreen.SetActive(true);
            _current = -1;
            _lesson = lesson;
            _endlessTier = -1;
            _skipButton.gameObject.SetActive(false);
            if (!_puzzle.OpenLesson(_tutorialCatalog.Stages[lesson], lesson, _data))
            {
                ShowSelect();
                return;
            }
            _nextLabel.text = NextText;
        }

        // 모든 Awake · OnEnable 뒤에 첫 화면을 연다
        private void Start()
        {
            string url = Application.absoluteURL;
            // QA 주소 기능(?reset · ?palette= · ?lab · ?stage= · ?stats)은 테스트 주소에서만 (2026-10-07, UrlQuery.IsQaHost)
            bool qa = UrlQuery.IsQaHost(url);
            if (qa && UrlQuery.TryGet(url, ResetQuery, out _))
            {
                _data.ClearRecords();
                Debug.Log("주소에 ?reset — 진행 · 플레이 기록을 지웠습니다", this);
            }
            ApplySettings();
            if (qa && UrlQuery.TryGet(url, PaletteQuery, out string palette) && palette.Length > 0) _paletteOverride = palette;
            if (qa && UrlQuery.TryGet(url, LabQuery, out _)) UseList(_labCatalog, true);

            int start = -1;
            if (qa && UrlQuery.TryGet(url, StageQuery, out string name))
            {
                start = Array.FindIndex(_stages, s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase));
                if (start < 0) Debug.LogWarning($"주소의 스테이지 '{name}'를 찾지 못해 타이틀을 엽니다", this);
            }
            if (start >= 0) OpenStage(start);
            else if (_lab) ShowSelect();
            else ShowTitle();

            if (qa && UrlQuery.TryGet(url, StatsQuery, out _)) _statsView.Show(_list, _tutorialCatalog, _data, EndlessKeys, EndlessNames);
        }

        // 목록. 처음 클리어하고 그림 화면을 거치지 않고 왔으면(목록 버튼) 작은 그림에서 그 단계를 칠한다. 시험 목록에는 그림이 없다
        private void ShowSelect()
        {
            if (_lesson >= 0)
            {
                // 따라 하기를 끝냈거나 목록으로 나갔다 — 다음부터는 자동으로 열지 않는다
                _lesson = -1;
                _data.TutorialSeen = true;
            }
            int justPainted = !_lab && _current >= 0 && _puzzle.FirstClear && !_paintShown ? _current : -1;
            _puzzle.Close();
            _current = -1;
            _endlessTier = -1;
            _titleScreen.SetActive(false);
            _boardScreen.SetActive(false);
            _chapterScreen.SetActive(false);
            _endless.gameObject.SetActive(false);
            _selectEndlessButton.gameObject.SetActive(!_lab && _data.TutorialSeen);
            _select.Show(_stages, _minMoves, _data, _lab ? LabTitle : _chapter.Title, _lab ? null : _chapter.Subtitle, _lab);
            _selectPicture.gameObject.SetActive(!_lab);
            if (!_lab) _selectPicture.Show(_chapter.Art, UpdatePainted(), justPainted);
        }

        private void OpenStage(int index)
        {
            _titleScreen.SetActive(false);
            _select.gameObject.SetActive(false);
            _chapterScreen.SetActive(false);
            _boardScreen.SetActive(true);
            _paintShown = false;
            _lesson = -1;
            _endlessTier = -1;
            _skipButton.gameObject.SetActive(false);
            if (!_puzzle.Open(_list.Stages[index], index + 1, _data, _paletteOverride))
            {
                ShowSelect();
                return;
            }
            _current = index;
            _nextLabel.text = index + 1 < _stages.Length ? NextText : ListText;
        }

        // 클리어 띠의 "다음" — 처음 클리어했으면 그림 화면에서 그 단계를 칠한 뒤, 아니면 바로 다음 스테이지(마지막이면 목록)
        private void Next()
        {
            if (_endlessTier >= 0)
            {
                int tier = _endlessTier;
                FinishEndless();
                _ads.ShowBetweenStages(() => OpenEndless(tier));
                return;
            }
            if (_lesson >= 0)
            {
                if (_lesson + 1 < _tutorialCatalog.Stages.Count) OpenLesson(_lesson + 1);
                else ShowSelect();
                return;
            }
            if (!_lab && _current >= 0 && _puzzle.FirstClear && !_paintShown)
            {
                ShowPicture(_current);
                return;
            }
            ContinueAfterPicture();
        }

        // 그림 화면: 칠한 단계 연출, 모든 단계를 칠했으면 완성 연출. 버튼은 다음 스테이지(마지막이면 목록)
        private void ShowPicture(int step)
        {
            _paintShown = true;
            _puzzle.Close();
            _boardScreen.SetActive(false);
            _chapterScreen.SetActive(true);
            bool[] painted = UpdatePainted();
            int count = _data.Progress.ClearedCount(_stages);
            bool complete = _data.Progress.IsComplete(_stages);
            _chapterCaption.text = complete ? CompleteText : $"색칠 {count} / {painted.Length}";
            _chapterNextLabel.text = step + 1 < _stages.Length ? NextText : ListText;
            _chapterPicture.Show(_chapter.Art, painted, step, complete);
        }

        // 다음 스테이지 — 스테이지 사이 광고 자리를 거친다. 마지막이면 목록
        private void ContinueAfterPicture()
        {
            if (_current < 0 || _current + 1 >= _stages.Length)
            {
                ShowSelect();
                return;
            }
            int next = _current + 1;
            _ads.ShowBetweenStages(() => OpenStage(next));
        }

        // 단계 i = i번째 스테이지를 클리어했는가
        private bool[] UpdatePainted()
        {
            _data.Progress.FillPainted(_stages, _painted);
            return _painted;
        }

        private void ShowOptions() => _options.Show(_data);

        // 타이틀 — 무한 모드 버튼은 따라 하기를 본 뒤에 보인다
        private void ShowTitle()
        {
            _select.gameObject.SetActive(false);
            _boardScreen.SetActive(false);
            _chapterScreen.SetActive(false);
            _endless.gameObject.SetActive(false);
            _endlessButton.gameObject.SetActive(_data.TutorialSeen);
            _titleScreen.SetActive(true);
        }

        // 보드의 목록 버튼 — 무한 모드면 난이도 화면, 아니면 스테이지 목록
        private void LeaveBoard()
        {
            if (_endlessTier < 0)
            {
                ShowSelect();
                return;
            }
            FinishEndless();
            ShowEndless();
        }

        private void ShowEndlessFromTitle()
        {
            _endlessFromTitle = true;
            ShowEndless();
        }

        private void ShowEndlessFromSelect()
        {
            _endlessFromTitle = false;
            ShowEndless();
        }

        private void LeaveEndless()
        {
            if (_endlessFromTitle) ShowTitle();
            else ShowSelect();
        }

        // 무한 모드 난이도 화면 (GDD §5, 2026-10-07) — 난이도마다 이름 · 최소 수 범위 · 푼 수
        private void ShowEndless()
        {
            _puzzle.Close();
            _endlessTier = -1;
            _current = -1;
            _lesson = -1;
            _titleScreen.SetActive(false);
            _select.gameObject.SetActive(false);
            _boardScreen.SetActive(false);
            _chapterScreen.SetActive(false);
            var labels = new string[EndlessKeys.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                Stage[] pool = EndlessPool(i);
                labels[i] = $"{EndlessNames[i]}\n{pool.Min(s => s.MinMoves ?? 0)}~{pool.Max(s => s.MinMoves ?? 0)}수 · 푼 {_data.Endless.Solved(EndlessKeys[i])}개";
            }
            _endless.Show(labels);
        }

        // 난이도의 지금 퍼즐을 연다 — 기록의 키는 퍼즐마다 따로("EndlessNormal0012")
        private void OpenEndless(int tier)
        {
            Stage[] pool = EndlessPool(tier);
            int index = _data.Endless.Current(EndlessKeys[tier]) % pool.Length;
            _titleScreen.SetActive(false);
            _endless.gameObject.SetActive(false);
            _select.gameObject.SetActive(false);
            _chapterScreen.SetActive(false);
            _boardScreen.SetActive(true);
            _endlessTier = tier;
            _current = -1;
            _lesson = -1;
            _puzzle.Open(pool[index], $"Endless{EndlessKeys[tier]}{index:D4}", $"{EndlessNames[tier]} {index + 1}번", _data, _paletteOverride);
            _nextLabel.text = NextText;
            _skipButton.gameObject.SetActive(true);
        }

        // 풀린 판을 떠나면(다음 · 목록) 다음 퍼즐로 넘기고 푼 수를 센다
        private void FinishEndless()
        {
            if (_endlessTier < 0 || !_puzzle.IsSolved) return;
            _data.Endless.Solve(EndlessKeys[_endlessTier], EndlessPool(_endlessTier).Length);
            _data.SaveEndless();
        }

        // 건너뛰기 — 안 풀린 판이면 건너뛴 수를 센다(풀린 판이면 푼 것으로)
        private void SkipEndless()
        {
            if (_endlessTier < 0) return;
            int tier = _endlessTier;
            if (_puzzle.IsSolved)
            {
                FinishEndless();
            }
            else
            {
                _data.Endless.Skip(EndlessKeys[tier], EndlessPool(tier).Length);
                _data.SaveEndless();
            }
            OpenEndless(tier);
        }

        private Stage[] EndlessPool(int tier)
        {
            if (_endlessStages == null) _endlessStages = new Stage[EndlessKeys.Length][];
            if (_endlessStages[tier] == null)
                _endlessStages[tier] = _endlessPools[tier].text.Split('\n').Where(line => line.Trim().Length > 0).Select(Stage.Parse).ToArray();
            return _endlessStages[tier];
        }

        private void ApplySettings()
        {
            _puzzle.SetSymbols(_data.Symbols);
            _sound.SetSoundOn(_data.Sound);
        }
    }
}
