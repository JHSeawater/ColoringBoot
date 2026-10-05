using System;
using ColoringBoot.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 보드 그리기와 보드 입력 — 칸 배치 · 화면 맞춤 · 드래그/탭 · 방향 버튼 · 붓질 미리보기.
    // 규칙 판단은 하지 않는다: 미리보기는 Core의 Trace로, 붓질은 BrushRequested로 알린다
    // 파일: BoardView.cs(필드 · 칸 만들기 · 그리기 · 배치 · 선택) · BoardView.Input.cs(끌기 · 탭 · 키보드) · BoardView.Preview.cs(붓질 미리보기) · BoardView.Motion.cs(연출) — 필드는 모두 이 파일에
    [RequireComponent(typeof(RectTransform))]
    public sealed partial class BoardView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float Sqrt3 = 1.7320508f;
        private const int DirectionCount = 6;
        private const int NoPointer = int.MinValue;
        // 아래 비율은 모두 칸 반지름(중심 → 꼭짓점) 기준 — 프로토타입 값
        private const float HitRadius = 0.95f;       // 탭 판정 거리
        private const float ButtonDistance = 1.32f;  // 칸 중심 → 방향 버튼 중심
        private const float DragThreshold = 0.45f;   // 이만큼 끌어야 방향이 정해진다
        private const float TrailOvershoot = 0.9f;   // 미리보기 선이 줄 양 끝 밖으로 나가는 길이
        private const float BrushWidth = 0.5f;       // 붓 색이 묻은 구간 굵기
        private const float EmptyWidth = 0.09f;      // 빈 붓 구간 굵기
        private const float KeyMinDot = 0.45f;       // 방향키 칸 이동: 이 각도(코사인) 안쪽 칸만 후보
        private const float KeyDistanceWeight = 6f;  // 방향키 칸 이동: 거리 벌점(반지름 × 이 값당 1)

        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Button _directionButtonPrefab;
        [SerializeField] private Color _emptyColor = new Color32(0xF6, 0xF7, 0xF3, 0xFF);
        [SerializeField] private Color _emptyTrailColor = new Color32(0x1C, 0x22, 0x2C, 0x8C);
        [Tooltip("미리보기 선의 불투명도")]
        [SerializeField] private float _trailAlpha = 0.6f;
        [Tooltip("방향 버튼 지름 (칸 반지름 기준)")]
        [SerializeField] private float _buttonDiameter = 0.84f;
        [Tooltip("칸 반지름 상한 (캔버스 단위) — 작은 보드가 지나치게 커지지 않게")]
        [SerializeField] private float _maxRadius = 150f;
        [Tooltip("화면 맞춤 여백 (칸 반지름 기준). 플레이 보드는 가장자리 칸의 방향 버튼까지 들어가게 1.8")]
        [SerializeField] private float _fitMargin = 1.8f;
        [Tooltip("켜면 현재 색 대신 목표 색을 그린다 — 목표 그림 썸네일용(입력 · 마커 · 막힘 표시 없음)")]
        [SerializeField] private bool _showTarget;
        [Tooltip("끄면 목표 마커 · 막힘 표시를 그리지 않는다 — 레벨 에디터의 획 기록용(목표가 아직 없다)")]
        [SerializeField] private bool _showJudgement = true;
        [SerializeField] private Color _symbolLight = Color.white;
        [SerializeField] private Color _symbolDark = new Color32(0x1C, 0x22, 0x2C, 0xFF);
        [Tooltip("칸 색의 밝기(Color.grayscale)가 이보다 크면 어두운 기호 — 기본 팔레트에서는 노랑만")]
        [SerializeField] private float _symbolDarkAbove = 0.6f;
        [Tooltip("붓질 물결 · 클리어 · 막힘 연출 값 — 비우면 연출 없음(목표 썸네일 · 레벨 에디터)")]
        [SerializeField] private MotionSettings _motion;

        // 붓질 요청 (칸 인덱스, 방향)
        public event Action<int, HexDirection> BrushRequested;

        private static readonly Vector2[] _directionVectors = CreateDirectionVectors();
        // 접근성 기호 — 색 값(비트)을 따르므로 팔레트와 상관없다 (GDD §6)
        private static readonly string[] _symbols = { "", "R", "Y", "RY", "B", "RB", "YB", "RYB" };

        private RectTransform _rect;
        private PuzzleSession _session;
        private Board _board;
        private ColorPalette _palette;
        private CellView[] _cells;
        private RectTransform[] _buttons;
        private Image[] _trail;
        private Vector2[] _centers;
        private int[] _traceCells;
        private PaintColor[] _traceBrushes;
        private int _selected = -1;
        private float _radius;
        private bool _interactable = true;
        private bool _layoutDirty;
        private bool _showSymbols;

        // 연출 (Phase 7.4) — 색은 이미 세션에 반영돼 있고, 화면만 늦게 따라간다
        private Coroutine _motionRoutine;
        private Vector2 _basePosition;
        private int[] _waveCells;
        private PaintColor[] _waveBefore;
        private int _waveCount;

        // 드래그: 처음 누른 손가락만 따라간다
        private int _pointerId = NoPointer;
        private int _dragCell;
        private Vector2 _dragStart;
        private int _dragDirection = -1;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _basePosition = _rect.anchoredPosition;
        }

        private void OnDisable() => StopMotion();

        // 스테이지마다 다시 부른다 — 이전 스테이지의 칸 · 선 · 버튼을 치우고 새로 만든다
        public void Build(PuzzleSession session, ColorPalette palette)
        {
            // 떼어 낸 뒤 지운다: Destroy는 프레임 끝에 일어나므로, 그 전에 자식 순서(칸 · 선 · 버튼)가 섞이지 않게
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
            StopMotion();
            _selected = -1;
            _pointerId = NoPointer;
            _dragDirection = -1;
            _interactable = true;

            _session = session;
            _board = session.Board;
            _palette = palette;
            int count = _board.CellCount;
            _cells = new CellView[count];
            _centers = new Vector2[count];
            _traceCells = new int[count];
            _traceBrushes = new PaintColor[count];
            _waveCells = new int[count];
            _waveBefore = new PaintColor[count];
            for (int i = 0; i < count; i++) _cells[i] = Instantiate(_cellPrefab, transform);

            // 미리보기 선: 줄 칸 수 + 1 구간이 최대 — 칸 위, 방향 버튼 아래에 그린다
            _trail = new Image[count + 1];
            for (int k = 0; k < _trail.Length; k++)
            {
                var segment = new GameObject("Trail", typeof(RectTransform), typeof(Image));
                segment.transform.SetParent(transform, false);
                _trail[k] = segment.GetComponent<Image>();
                _trail[k].raycastTarget = false;
                segment.SetActive(false);
            }

            // 방향 버튼: 누르면 긋고, 손가락 · 마우스를 올리면 미리보기
            _buttons = new RectTransform[DirectionCount];
            for (int d = 0; d < DirectionCount; d++)
            {
                var dir = (HexDirection)d;
                Button button = Instantiate(_directionButtonPrefab, transform);
                button.onClick.AddListener(() => OnDirectionClicked(dir));
                var trigger = button.gameObject.AddComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerEnter, () => ShowPreview(_selected, dir));
                AddTrigger(trigger, EventTriggerType.PointerExit, ClearPreview);
                button.gameObject.SetActive(false);
                _buttons[d] = (RectTransform)button.transform;
            }
            _layoutDirty = true;
        }

        // 지금 세션 상태 그대로 그린다 — 진행 중인 연출은 끝난 모습으로 정리한다
        public void Render()
        {
            StopMotion();
            for (int i = 0; i < _cells.Length; i++)
            {
                PaintColor target = _board.TargetOf(i);
                PaintColor color = _showTarget ? target : _session.ColorAt(i);
                _cells[i].SetFill(ColorOf(color));
                // 목표 마커: 아직 목표 색이 아니고 목표가 빈칸이 아닐 때 (프로토타입과 같음)
                _cells[i].SetMarker(_showJudgement && color != target && target != PaintColor.Empty, ColorOf(target));
                _cells[i].SetDead(_showJudgement && !_showTarget && _session.IsDeadCell(i));
                // 밝은 칸은 어두운 기호 — 팔레트마다 밝기가 다르다(파스텔은 대부분 밝음)
                _cells[i].SetSymbol(_showSymbols ? _symbols[(int)color] : "", ColorOf(color).grayscale > _symbolDarkAbove ? _symbolDark : _symbolLight);
            }
        }

        public void SetSymbols(bool visible)
        {
            _showSymbols = visible;
            Render();
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            if (interactable) return;
            _pointerId = NoPointer;
            ClearPreview();
            Select(-1);
        }

        private void OnRectTransformDimensionsChange()
        {
            _layoutDirty = true;
        }

        private void LateUpdate()
        {
            if (!_layoutDirty || _board == null) return;
            _layoutDirty = false;
            Layout();
        }

        // 보드 영역에 맞게 칸 반지름을 정하고 보드를 가운데에 놓는다
        private void Layout()
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < _centers.Length; i++)
            {
                Vector2 unit = ToLocal(_board.CoordOf(i));
                min = Vector2.Min(min, unit);
                max = Vector2.Max(max, unit);
            }

            Rect area = _rect.rect;
            Vector2 span = max - min + Vector2.one * (2f * _fitMargin);
            _radius = Mathf.Min(area.width / span.x, area.height / span.y, _maxRadius);
            // 칸 앵커는 보드 영역 가운데 — 위치는 가운데 기준 좌표(anchoredPosition)로 둔다
            Vector2 offset = -(min + max) * 0.5f * _radius;

            for (int i = 0; i < _cells.Length; i++)
            {
                _centers[i] = ToLocal(_board.CoordOf(i)) * _radius + offset;
                var cellRect = (RectTransform)_cells[i].transform;
                cellRect.anchoredPosition = _centers[i];
                cellRect.sizeDelta = Vector2.one * (2f * _radius);
            }
            PlaceButtons();
        }

        private void Select(int cell)
        {
            if (_selected >= 0) _cells[_selected].SetSelected(false);
            _selected = cell;
            if (_selected >= 0) _cells[_selected].SetSelected(true);
            ClearPreview();
            PlaceButtons();
        }

        // 선택한 칸 둘레에 방향 버튼을 놓는다. 1칸짜리 줄 방향은 긋는 의미가 없어 숨긴다
        private void PlaceButtons()
        {
            for (int d = 0; d < DirectionCount; d++)
            {
                var dir = (HexDirection)d;
                bool visible = _selected >= 0 && _board.LineLength(_selected, dir) > 1;
                _buttons[d].gameObject.SetActive(visible);
                if (!visible) continue;

                Vector2 toward = _directionVectors[d];
                _buttons[d].anchoredPosition = _centers[_selected] + toward * (ButtonDistance * _radius);
                _buttons[d].sizeDelta = Vector2.one * (_buttonDiameter * _radius);
                _buttons[d].localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(toward.y, toward.x) * Mathf.Rad2Deg);
            }
        }

        private void OnDirectionClicked(HexDirection dir)
        {
            int cell = _selected;
            Select(-1);
            if (cell >= 0) BrushRequested?.Invoke(cell, dir);
        }

        private Color ColorOf(PaintColor color) => color == PaintColor.Empty ? _emptyColor : _palette.Get(color);

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private static Vector2[] CreateDirectionVectors()
        {
            var vectors = new Vector2[DirectionCount];
            for (int d = 0; d < DirectionCount; d++) vectors[d] = ToLocal(((HexDirection)d).Delta()).normalized;
            return vectors;
        }

        // 축 좌표 → 반지름 1 기준 화면 위치. Unity는 y가 위쪽이라 r이 커질수록 아래로 (CLAUDE.md §3 화면 배치)
        private static Vector2 ToLocal(HexCoord coord) => new Vector2(Sqrt3 * (coord.Q + coord.R * 0.5f), -1.5f * coord.R);
    }
}
