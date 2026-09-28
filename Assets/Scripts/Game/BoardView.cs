using System;
using ColoringBoot.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 보드 그리기와 탭 입력 — 칸 배치 · 화면 맞춤 · 칸 선택 · 방향 버튼. 규칙 판단은 하지 않고 붓질 요청만 알린다
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardView : MonoBehaviour, IPointerClickHandler
    {
        private const float Sqrt3 = 1.7320508f;
        private const int DirectionCount = 6;
        // 아래 비율은 모두 칸 반지름(중심 → 꼭짓점) 기준 — 프로토타입 값
        private const float HitRadius = 0.95f;      // 탭 판정 거리
        private const float ButtonDistance = 1.32f; // 칸 중심 → 방향 버튼 중심
        private const float FitMargin = 1.8f;       // 화면 맞춤 여백: 가장자리 칸의 방향 버튼까지 들어가게

        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Button _directionButtonPrefab;
        [SerializeField] private Color _emptyColor = new Color32(0xF6, 0xF7, 0xF3, 0xFF);
        [Tooltip("방향 버튼 지름 (칸 반지름 기준)")]
        [SerializeField] private float _buttonDiameter = 0.84f;
        [Tooltip("칸 반지름 상한 (캔버스 단위) — 작은 보드가 지나치게 커지지 않게")]
        [SerializeField] private float _maxRadius = 150f;

        // 붓질 요청 (칸 인덱스, 방향)
        public event Action<int, HexDirection> BrushRequested;

        private RectTransform _rect;
        private Board _board;
        private ColorPalette _palette;
        private CellView[] _cells;
        private RectTransform[] _buttons;
        private Vector2[] _centers;
        private int _selected = -1;
        private float _radius;
        private bool _interactable = true;
        private bool _layoutDirty;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        public void Build(Board board, ColorPalette palette)
        {
            _board = board;
            _palette = palette;
            _cells = new CellView[board.CellCount];
            _centers = new Vector2[board.CellCount];
            for (int i = 0; i < _cells.Length; i++) _cells[i] = Instantiate(_cellPrefab, transform);

            // 방향 버튼은 칸보다 뒤에 만들어 위에 그려지고 탭도 먼저 받는다
            _buttons = new RectTransform[DirectionCount];
            for (int d = 0; d < DirectionCount; d++)
            {
                var dir = (HexDirection)d;
                Button button = Instantiate(_directionButtonPrefab, transform);
                button.onClick.AddListener(() => OnDirectionClicked(dir));
                button.gameObject.SetActive(false);
                _buttons[d] = (RectTransform)button.transform;
            }
            _layoutDirty = true;
        }

        public void Render(PuzzleSession session)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                PaintColor color = session.ColorAt(i);
                PaintColor target = _board.TargetOf(i);
                _cells[i].SetFill(ColorOf(color));
                // 목표 마커: 아직 목표 색이 아니고 목표가 빈칸이 아닐 때 (프로토타입과 같음)
                _cells[i].SetMarker(color != target && target != PaintColor.Empty, ColorOf(target));
                _cells[i].SetDead(session.IsDeadCell(i));
            }
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            if (!interactable) Select(-1);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable || _board == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out Vector2 local);
            int cell = CellAt(local - _rect.rect.center); // 피벗 기준 → 가운데 기준
            Select(cell == _selected ? -1 : cell);
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
            Vector2 span = max - min + Vector2.one * (2f * FitMargin);
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

        private void Select(int cell)
        {
            if (_selected >= 0) _cells[_selected].SetSelected(false);
            _selected = cell;
            if (_selected >= 0) _cells[_selected].SetSelected(true);
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

                Vector2 toward = ToLocal(dir.Delta()).normalized;
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

        // 축 좌표 → 반지름 1 기준 화면 위치. Unity는 y가 위쪽이라 r이 커질수록 아래로 (CLAUDE.md §3 화면 배치)
        private static Vector2 ToLocal(HexCoord coord) => new Vector2(Sqrt3 * (coord.Q + coord.R * 0.5f), -1.5f * coord.R);
    }
}
