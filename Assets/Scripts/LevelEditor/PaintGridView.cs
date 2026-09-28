using System;
using System.Collections.Generic;
using ColoringBoot.Core;
using ColoringBoot.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ColoringBoot.LevelEditor
{
    // 칠하기 격자 — 반지름 4 육각 영역의 모든 자리(프로토타입 에디터와 같음). 있는 칸은 편집 중인 층의 색 + 다른 층 색 마커,
    // 없는 자리는 흐리게. 누르거나 끌고 지나간 자리를 Painted로 알린다
    [RequireComponent(typeof(RectTransform))]
    public sealed class PaintGridView : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public const int Radius = 4;
        private const float Sqrt3 = 1.7320508f;
        private const float HitRadius = 0.95f;
        private const float FitMargin = 0.6f;
        private const float AbsentAlpha = 0.2f;

        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Color _emptyColor = new Color32(0xF6, 0xF7, 0xF3, 0xFF);

        public event Action<HexCoord> Painted;

        private readonly List<HexCoord> _coords = new List<HexCoord>();
        private RectTransform _rect;
        private CellView[] _cells;
        private CanvasGroup[] _groups;
        private Vector2[] _centers;
        private float _radius;
        private bool _layoutDirty = true;
        private int _lastPainted = -1;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            for (int q = -Radius; q <= Radius; q++)
                for (int r = -Radius; r <= Radius; r++)
                    if (Math.Abs(q + r) <= Radius) _coords.Add(new HexCoord(q, r));

            _cells = new CellView[_coords.Count];
            _groups = new CanvasGroup[_coords.Count];
            _centers = new Vector2[_coords.Count];
            for (int i = 0; i < _coords.Count; i++)
            {
                _cells[i] = Instantiate(_cellPrefab, transform);
                _groups[i] = _cells[i].gameObject.AddComponent<CanvasGroup>();
                _groups[i].blocksRaycasts = false;
            }
        }

        public void Render(IReadOnlyDictionary<HexCoord, (PaintColor start, PaintColor target)> cells, bool targetLayer, ColorPalette palette)
        {
            for (int i = 0; i < _coords.Count; i++)
            {
                bool present = cells.TryGetValue(_coords[i], out (PaintColor start, PaintColor target) cell);
                PaintColor shown = targetLayer ? cell.target : cell.start;
                PaintColor other = targetLayer ? cell.start : cell.target;
                _groups[i].alpha = present ? 1f : AbsentAlpha;
                _cells[i].SetFill(present ? ColorOf(shown, palette) : _emptyColor);
                _cells[i].SetMarker(present && other != shown, ColorOf(other, palette));
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _lastPainted = -1;
            PaintAt(eventData);
        }

        public void OnDrag(PointerEventData eventData) => PaintAt(eventData);

        private void OnRectTransformDimensionsChange() => _layoutDirty = true;

        private void LateUpdate()
        {
            if (!_layoutDirty || _cells == null) return;
            _layoutDirty = false;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (HexCoord coord in _coords)
            {
                min = Vector2.Min(min, ToLocal(coord));
                max = Vector2.Max(max, ToLocal(coord));
            }
            Rect area = _rect.rect;
            Vector2 span = max - min + Vector2.one * (2f * FitMargin);
            _radius = Mathf.Min(area.width / span.x, area.height / span.y);
            Vector2 offset = -(min + max) * 0.5f * _radius;
            for (int i = 0; i < _cells.Length; i++)
            {
                _centers[i] = ToLocal(_coords[i]) * _radius + offset;
                var cellRect = (RectTransform)_cells[i].transform;
                cellRect.anchoredPosition = _centers[i];
                cellRect.sizeDelta = Vector2.one * (2f * _radius);
            }
        }

        private void PaintAt(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out Vector2 local);
            local -= _rect.rect.center;
            for (int i = 0; i < _centers.Length; i++)
            {
                if (Vector2.Distance(_centers[i], local) > HitRadius * _radius || i == _lastPainted) continue;
                _lastPainted = i;
                Painted?.Invoke(_coords[i]);
                return;
            }
        }

        private Color ColorOf(PaintColor color, ColorPalette palette) => color == PaintColor.Empty ? _emptyColor : palette.Get(color);

        private static Vector2 ToLocal(HexCoord coord) => new Vector2(Sqrt3 * (coord.Q + coord.R * 0.5f), -1.5f * coord.R);
    }
}
