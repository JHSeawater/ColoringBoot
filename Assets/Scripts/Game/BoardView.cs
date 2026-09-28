using System;
using ColoringBoot.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 보드 그리기와 보드 입력 — 칸 배치 · 화면 맞춤 · 드래그/탭 · 방향 버튼 · 붓질 미리보기.
    // 규칙 판단은 하지 않는다: 미리보기는 Core의 Trace로, 붓질은 BrushRequested로 알린다
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
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
        [SerializeField] private Color _symbolLight = Color.white;
        [SerializeField] private Color _symbolDark = new Color32(0x1C, 0x22, 0x2C, 0xFF);

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

        // 드래그: 처음 누른 손가락만 따라간다
        private int _pointerId = NoPointer;
        private int _dragCell;
        private Vector2 _dragStart;
        private int _dragDirection = -1;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        public void Build(PuzzleSession session, ColorPalette palette)
        {
            _session = session;
            _board = session.Board;
            _palette = palette;
            int count = _board.CellCount;
            _cells = new CellView[count];
            _centers = new Vector2[count];
            _traceCells = new int[count];
            _traceBrushes = new PaintColor[count];
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

        public void Render()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                PaintColor target = _board.TargetOf(i);
                PaintColor color = _showTarget ? target : _session.ColorAt(i);
                _cells[i].SetFill(ColorOf(color));
                // 목표 마커: 아직 목표 색이 아니고 목표가 빈칸이 아닐 때 (프로토타입과 같음)
                _cells[i].SetMarker(color != target && target != PaintColor.Empty, ColorOf(target));
                _cells[i].SetDead(!_showTarget && _session.IsDeadCell(i));
                // 노랑 · 빈칸처럼 밝은 칸은 어두운 기호
                _cells[i].SetSymbol(_showSymbols ? _symbols[(int)color] : "", color == PaintColor.Yellow ? _symbolDark : _symbolLight);
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

        // 키보드: 선택한 칸에서 want 쪽(화면 방향)의 가장 알맞은 칸으로. 선택이 없으면 첫 칸
        public void MoveSelection(Vector2 want)
        {
            if (!_interactable || _board == null) return;
            int current = _selected < 0 ? 0 : _selected;
            int best = current;
            float bestScore = float.MinValue;
            for (int i = 0; i < _centers.Length; i++)
            {
                if (i == current) continue;
                Vector2 offset = _centers[i] - _centers[current];
                float distance = offset.magnitude;
                float dot = Vector2.Dot(offset, want) / distance;
                if (dot < KeyMinDot) continue;
                float score = dot - distance / (_radius * KeyDistanceWeight);
                if (score <= bestScore) continue;
                bestScore = score;
                best = i;
            }
            Select(best);
        }

        // 키보드: 선택한 칸에서 dir로 긋는다. 선택이 없으면 첫 칸을 고르기만 한다(프로토타입과 같음)
        public void BrushSelected(HexDirection dir)
        {
            if (!_interactable || _board == null) return;
            if (_selected < 0) Select(0);
            else if (_board.LineLength(_selected, dir) > 1) OnDirectionClicked(dir);
        }

        public void ClearSelection() => Select(-1);

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_interactable || _board == null || _pointerId != NoPointer) return;
            Vector2 local = ToBoardLocal(eventData);
            int cell = CellAt(local);
            if (cell < 0)
            {
                Select(-1);
                return;
            }
            _pointerId = eventData.pointerId;
            _dragCell = cell;
            _dragStart = local;
            _dragDirection = -1;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            Vector2 delta = ToBoardLocal(eventData) - _dragStart;
            int direction = delta.magnitude < DragThreshold * _radius ? -1 : NearestDirection(delta);
            if (direction == _dragDirection) return;
            _dragDirection = direction;
            if (direction < 0)
            {
                ClearPreview();
                return;
            }
            Select(-1);
            ShowPreview(_dragCell, (HexDirection)direction);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            _pointerId = NoPointer;
            if (_dragDirection < 0)
            {
                // 끌지 않고 뗌 = 탭: 칸 선택(같은 칸이면 해제) → 방향 버튼
                Select(_dragCell == _selected ? -1 : _dragCell);
                return;
            }
            var dir = (HexDirection)_dragDirection;
            _dragDirection = -1;
            ClearPreview();
            if (_board.LineLength(_dragCell, dir) > 1) BrushRequested?.Invoke(_dragCell, dir);
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

        // 미리보기: 붓 경로(붓 색이 묻은 구간은 그 색, 빈 붓은 가는 선)와 칠해질 칸의 결과 색
        private void ShowPreview(int cell, HexDirection dir)
        {
            ClearPreview();
            if (cell < 0 || _board.LineLength(cell, dir) < 2) return;

            int count = _session.Trace(cell, dir, _traceCells, _traceBrushes);
            Vector2 toward = _directionVectors[(int)dir];
            Vector2 previous = _centers[_traceCells[0]] - toward * (TrailOvershoot * _radius);
            for (int k = 0; k <= count; k++)
            {
                Vector2 next = k < count ? _centers[_traceCells[k]] : _centers[_traceCells[count - 1]] + toward * (TrailOvershoot * _radius);
                PaintColor brush = k == 0 ? PaintColor.Empty : _traceBrushes[k - 1];
                DrawSegment(_trail[k], previous, next, brush);
                previous = next;
            }

            for (int k = 0; k < count; k++)
            {
                int i = _traceCells[k];
                PaintColor result = _traceBrushes[k];
                if (result != PaintColor.Empty && result != _session.ColorAt(i)) _cells[i].SetGhost(true, ColorOf(result));
            }
        }

        private void ClearPreview()
        {
            if (_trail == null) return;
            for (int k = 0; k < _trail.Length; k++) _trail[k].gameObject.SetActive(false);
            for (int i = 0; i < _cells.Length; i++) _cells[i].SetGhost(false, Color.clear);
        }

        private void DrawSegment(Image segment, Vector2 from, Vector2 to, PaintColor brush)
        {
            Vector2 delta = to - from;
            var rect = segment.rectTransform;
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, (brush == PaintColor.Empty ? EmptyWidth : BrushWidth) * _radius);
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            Color color = brush == PaintColor.Empty ? _emptyTrailColor : _palette.Get(brush);
            if (brush != PaintColor.Empty) color.a = _trailAlpha;
            segment.color = color;
            segment.gameObject.SetActive(true);
        }

        private int CellAt(Vector2 local)
        {
            int best = -1;
            float bestDistance = HitRadius * _radius;
            for (int i = 0; i < _centers.Length; i++)
            {
                float distance = Vector2.Distance(_centers[i], local);
                if (distance > bestDistance) continue;
                bestDistance = distance;
                best = i;
            }
            return best;
        }

        private static int NearestDirection(Vector2 delta)
        {
            int best = 0;
            float bestDot = float.MinValue;
            for (int d = 0; d < DirectionCount; d++)
            {
                float dot = Vector2.Dot(delta, _directionVectors[d]);
                if (dot <= bestDot) continue;
                bestDot = dot;
                best = d;
            }
            return best;
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

        // 화면 좌표 → 보드 영역 가운데 기준 좌표
        private Vector2 ToBoardLocal(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out Vector2 local);
            return local - _rect.rect.center;
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
