using System.Collections;
using ColoringBoot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // BoardView 붓질 미리보기 — 붓 경로 선 · 칠해질 칸의 결과 색 (경로는 Core Trace, 필드는 BoardView.cs) · 안내 손가락 표시
    public sealed partial class BoardView
    {
        // 안내 (튜토리얼 · 힌트, 2026-10-07): cell에서 dir 쪽으로 끄는 손가락 표시 + 그 획의 미리보기. HideGuide · 다음 끌기까지 남는다
        public void ShowGuide(int cell, HexDirection dir)
        {
            StopGuide();
            if (_layoutDirty)
            {
                // 스테이지를 연 프레임이면 칸 자리가 아직 없다 — 먼저 배치한다
                _layoutDirty = false;
                Layout();
            }
            ShowPreview(cell, dir);
            if (_guide == null) return;
            _guideCell = cell;
            _guideDirection = dir;
            _guide.gameObject.SetActive(true);
            _guideRoutine = StartCoroutine(AnimateGuide());
        }

        public void HideGuide()
        {
            StopGuide();
            ClearPreview();
        }

        private void StopGuide()
        {
            if (_guideRoutine != null) StopCoroutine(_guideRoutine);
            _guideRoutine = null;
            if (_guide != null) _guide.gameObject.SetActive(false);
        }

        // 손가락 표시: 칸에서 방향 쪽으로 움직였다가 흐려지기를 되풀이한다
        private IEnumerator AnimateGuide()
        {
            RectTransform rect = _guide.rectTransform;
            for (float time = 0f; ; time += Time.unscaledDeltaTime)
            {
                float k = time % GuideCycle / GuideCycle;
                float move = Mathf.Clamp01(k / GuideMovePart);
                float fade = k <= GuideMovePart ? 1f : 1f - (k - GuideMovePart) / (1f - GuideMovePart);
                rect.anchoredPosition = _centers[_guideCell] + _directionVectors[(int)_guideDirection] * (move * GuideDistance * _radius);
                rect.sizeDelta = Vector2.one * (GuideSize * _radius);
                Color color = _guideColor;
                color.a *= fade;
                _guide.color = color;
                yield return null;
            }
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

            // 결과 색은 칠해지는 칸(일반 칸)에만 — 물 · 코팅 칸은 색이 바뀌지 않는다
            for (int k = 0; k < count; k++)
            {
                int i = _traceCells[k];
                PaintColor result = _traceBrushes[k];
                if (_board.KindOf(i) == CellKind.Paint && result != PaintColor.Empty && result != _session.ColorAt(i)) _cells[i].SetGhost(true, ColorOf(result));
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
