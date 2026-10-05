using System;
using System.Collections;
using ColoringBoot.Core;
using UnityEngine;

namespace ColoringBoot.Game
{
    // BoardView 연출(Phase 7.4) — 붓질 물결 · 클리어 반응 · 막힘 흔들림 (값은 MotionSettings, 필드는 BoardView.cs)
    public sealed partial class BoardView
    {
        // 붓질 물결: Render 뒤에 부른다. cells = 붓이 지나간 칸(출발 쪽부터, PuzzleSession.Trace), before = 긋기 전 색.
        // 칸마다 차례로 새 색이 되며 튄다. 끝나는 데 걸리는 시간(초)을 돌려준다 — 연출이 없으면 0
        public float PlayStroke(int[] cells, PaintColor[] before, int count)
        {
            if (_motion == null || count <= 0) return 0f;
            StopMotion();
            Array.Copy(cells, _waveCells, count);
            Array.Copy(before, _waveBefore, count);
            _waveCount = count;
            _motionRoutine = StartCoroutine(Wave());
            return (count - 1) * _motion.StrokeStep + _motion.PopSeconds;
        }

        // 클리어: 모든 칸이 차례로 튄다
        public float PlayClear()
        {
            if (_motion == null) return 0f;
            StopMotion();
            _motionRoutine = StartCoroutine(Ripple());
            return (_cells.Length - 1) * _motion.ClearStep + _motion.ClearPopSeconds;
        }

        // 막힘: 보드가 좌우로 흔들리다 멈춘다
        public float PlayShake()
        {
            if (_motion == null) return 0f;
            StopMotion();
            _motionRoutine = StartCoroutine(Shake());
            return _motion.ShakeSeconds;
        }

        private IEnumerator Wave()
        {
            float step = _motion.StrokeStep, pop = _motion.PopSeconds;
            float total = (_waveCount - 1) * step + pop;
            for (float time = 0f; time < total; time += Time.unscaledDeltaTime)
            {
                for (int k = 0; k < _waveCount; k++)
                {
                    int cell = _waveCells[k];
                    float local = time - k * step;
                    PaintColor now = _session.ColorAt(cell);
                    _cells[cell].SetFill(ColorOf(local < 0f ? _waveBefore[k] : now));
                    bool changed = now != _waveBefore[k];
                    _cells[cell].transform.localScale = Vector3.one * (changed ? Pop(local, pop, _motion.PopScale) : 1f);
                }
                yield return null;
            }
            FinishMotion();
        }

        private IEnumerator Ripple()
        {
            float step = _motion.ClearStep, pop = _motion.ClearPopSeconds;
            float total = (_cells.Length - 1) * step + pop;
            for (float time = 0f; time < total; time += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < _cells.Length; i++)
                    _cells[i].transform.localScale = Vector3.one * Pop(time - i * step, pop, _motion.ClearPopScale);
                yield return null;
            }
            FinishMotion();
        }

        private IEnumerator Shake()
        {
            float seconds = _motion.ShakeSeconds;
            for (float time = 0f; time < seconds; time += Time.unscaledDeltaTime)
            {
                float k = time / seconds;
                float x = _motion.ShakeAmplitude * Mathf.Sin(k * _motion.ShakeCycles * 2f * Mathf.PI) * (1f - k);
                _rect.anchoredPosition = _basePosition + new Vector2(x, 0f);
                yield return null;
            }
            FinishMotion();
        }

        // local초 지난 칸의 크기: 0 → 1 → 0을 반 사인으로(시작 전 · 끝난 뒤는 1)
        private static float Pop(float local, float seconds, float scale) =>
            local <= 0f || local >= seconds ? 1f : 1f + (scale - 1f) * Mathf.Sin(local / seconds * Mathf.PI);

        // 연출을 끝난 모습으로: 크기 · 위치 원래대로, 물결 중이던 칸은 지금 색으로
        private void StopMotion()
        {
            if (_motionRoutine == null) return;
            StopCoroutine(_motionRoutine);
            FinishMotion();
        }

        private void FinishMotion()
        {
            _motionRoutine = null;
            if (_cells == null) return;
            for (int k = 0; k < _waveCount; k++) _cells[_waveCells[k]].SetFill(ColorOf(_session.ColorAt(_waveCells[k])));
            _waveCount = 0;
            foreach (CellView cell in _cells) cell.transform.localScale = Vector3.one;
            _rect.anchoredPosition = _basePosition;
        }
    }
}
