using ColoringBoot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // BoardView 붓질 미리보기 — 붓 경로 선 · 칠해질 칸의 결과 색 (경로는 Core Trace, 필드는 BoardView.cs)
    public sealed partial class BoardView
    {
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
    }
}
