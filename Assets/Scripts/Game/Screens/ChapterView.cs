using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 챕터 그림 (Phase 5) — 종이색 바탕 · 칠한 단계 레이어 · 선화를 겹쳐 그린다. 크기는 이 RectTransform을 따른다(캔버스 비율로 배치할 것).
    // 막 칠한 단계는 서서히 나타나며 살짝 커졌다 돌아오고, 완성이면 그 뒤 그림 전체가 한 번 더 커졌다 돌아온다
    public sealed class ChapterView : MonoBehaviour
    {
        [SerializeField] private Color _paper = new Color32(0xFB, 0xFA, 0xF7, 0xFF);
        [SerializeField] private float _paintSeconds = 0.6f;
        [SerializeField] private float _paintScale = 1.06f;
        [SerializeField] private float _completeSeconds = 0.8f;
        [SerializeField] private float _completeScale = 1.04f;
        [SerializeField] private Sprite _frame;          // 그림 테두리(9-slice, Phase 7.1) — 없으면 안 그림
        [SerializeField] private Color _frameColor = Color.black;
        [SerializeField] private float _frameOutset = 7f; // 그림 밖으로 넓히는 폭 = 테두리 선 두께
        [Header("막 칠한 단계 확대 (2026-11-12 — 클리어 띠 위 카드만. 그림 층만 키우고 그림 칸에서 자른다 — 테두리는 그대로)")]
        [SerializeField] private float _focusZoom = 1f;       // 최대 확대 배율 — 1 이하면 확대하지 않는다
        [SerializeField] private float _focusHold = 0.25f;    // 칠한 뒤 확대한 채로 머무는 초
        [SerializeField] private float _zoomOutSeconds = 0.5f;

        private const float FocusFill = 0.8f;   // 확대했을 때 단계가 그림 칸에서 차지하는 비율

        private readonly List<Image> _steps = new List<Image>();
        private ChapterArt _art;
        private Coroutine _routine;
        private RectTransform _content;   // 바탕 · 단계 · 선화를 담는 층 — 확대는 이 층만(Clip 안, 가운데 피벗 · 원래 자리 0)

        // painted[i] = i번째 단계를 칠했는가. animateStep: 막 칠한 단계(-1이면 연출 없음). complete: 그 뒤 완성 연출
        public void Show(ChapterArt art, IReadOnlyList<bool> painted, int animateStep = -1, bool complete = false)
        {
            if (art != _art) Build(art);
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            ResetZoom();
            for (int i = 0; i < _steps.Count; i++)
            {
                _steps[i].gameObject.SetActive(i < painted.Count && painted[i]);
                _steps[i].color = Color.white;
                _steps[i].transform.localScale = Vector3.one;
            }
            if (animateStep >= 0 && animateStep < _steps.Count && isActiveAndEnabled)
            {
                _routine = StartCoroutine(Paint(_steps[animateStep], complete));
            }
        }

        private void Build(ChapterArt art)
        {
            _art = art;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
            _steps.Clear();

            // Clip(그림 칸 · 확대를 쓰면 RectMask2D) → Content(확대 대상) → 바탕 · 단계 · 선화. 테두리는 Clip 밖이라 확대되지도 잘리지도 않는다
            RectTransform clip = AddLayer("Clip", transform);
            if (_focusZoom > 1f) clip.gameObject.AddComponent<RectMask2D>();
            _content = AddLayer("Content", clip);
            AddImage("Paper", null, new RectInt(0, 0, art.Canvas.x, art.Canvas.y)).color = _paper;
            for (int i = 0; i < art.Steps.Count; i++) _steps.Add(AddImage($"Step{i + 1:00}", art.Steps[i].Sprite, art.Steps[i].Rect));
            AddImage("Line", art.Line.Sprite, art.Line.Rect);
            if (_frame != null) AddFrame();
        }

        // 부모를 가득 채우는 빈 층
        private static RectTransform AddLayer(string name, Transform parent)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        private void AddFrame()
        {
            var go = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = -Vector2.one * _frameOutset;
            rt.offsetMax = Vector2.one * _frameOutset;
            var image = go.GetComponent<Image>();
            image.sprite = _frame;
            image.type = Image.Type.Sliced;
            image.color = _frameColor;
            image.raycastTarget = false;
        }

        // rect: 캔버스 픽셀(왼쪽 위 기준) → 이 뷰 안의 비율 앵커. 조각이 캔버스 밖으로 조금 넘쳐도 된다(투명 여백)
        private Image AddImage(string name, Sprite sprite, RectInt rect)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_content, false);
            float w = _art.Canvas.x, h = _art.Canvas.y;
            rt.anchorMin = new Vector2(rect.x / w, 1f - (rect.y + rect.height) / h);
            rt.anchorMax = new Vector2((rect.x + rect.width) / w, 1f - rect.y / h);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private IEnumerator Paint(Image step, bool complete)
        {
            Transform t = step.transform;
            float zoom = FocusOn((RectTransform)t, out Vector2 offset);
            if (zoom > 1f)
            {
                _content.localScale = Vector3.one * zoom;
                _content.anchoredPosition = offset;
            }
            for (float time = 0f; time < _paintSeconds; time += Time.unscaledDeltaTime)
            {
                float k = time / _paintSeconds;
                step.color = new Color(1f, 1f, 1f, k);
                t.localScale = Vector3.one * Mathf.Lerp(1f, _paintScale, Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            step.color = Color.white;
            t.localScale = Vector3.one;
            if (zoom > 1f)
            {
                for (float time = 0f; time < _focusHold; time += Time.unscaledDeltaTime) yield return null;
                for (float time = 0f; time < _zoomOutSeconds; time += Time.unscaledDeltaTime)
                {
                    float k = Mathf.SmoothStep(0f, 1f, time / _zoomOutSeconds);
                    _content.localScale = Vector3.one * Mathf.Lerp(zoom, 1f, k);
                    _content.anchoredPosition = offset * (1f - k);
                    yield return null;
                }
                ResetZoom();
            }
            if (complete)
            {
                for (float time = 0f; time < _completeSeconds; time += Time.unscaledDeltaTime)
                {
                    transform.localScale = Vector3.one * Mathf.Lerp(1f, _completeScale, Mathf.Sin(time / _completeSeconds * Mathf.PI));
                    yield return null;
                }
                transform.localScale = Vector3.one;
            }
            _routine = null;
        }

        // 막 칠한 단계가 그림 칸의 FocusFill만큼 차도록 확대 배율(1 ~ _focusZoom)과 그림 층의 자리 옮김을 구한다. 그림 밖(빈 곳)은 보이지 않게 자리를 막는다
        private float FocusOn(RectTransform step, out Vector2 offset)
        {
            offset = Vector2.zero;
            if (_focusZoom <= 1f) return 1f;
            Vector2 size = _content.rect.size;
            Vector2 min = Vector2.Max(step.anchorMin, Vector2.zero), max = Vector2.Min(step.anchorMax, Vector2.one);
            Vector2 span = max - min;
            if (span.x <= 0f || span.y <= 0f) return 1f;   // 한 축이라도 그림 밖이거나 크기가 0인 조각 — 확대하지 않는다
            float zoom = Mathf.Clamp(FocusFill / Mathf.Max(span.x, span.y), 1f, _focusZoom);
            if (zoom <= 1f) return 1f;
            // 그림 층은 가운데 피벗 · 원래 자리 0 — 조각 가운데를 그림 칸 가운데로, 단 키운 그림이 칸을 늘 덮게(±(zoom-1)/2 칸)
            Vector2 target = Vector2.Scale((min + max) * 0.5f - new Vector2(0.5f, 0.5f), size);
            Vector2 limit = (zoom - 1f) * 0.5f * size;
            offset = Vector2.Max(-limit, Vector2.Min(limit, -target * zoom));
            return zoom;
        }

        // 확대 · 완성 연출 전 크기로
        private void ResetZoom()
        {
            transform.localScale = Vector3.one;
            if (_content == null) return;
            _content.localScale = Vector3.one;
            _content.anchoredPosition = Vector2.zero;
        }

        // 화면을 떠나 연출이 끊기면 다 칠한 모습으로 둔다
        private void OnDisable()
        {
            if (_routine == null) return;
            StopCoroutine(_routine);
            _routine = null;
            ResetZoom();
            foreach (Image step in _steps)
            {
                step.color = Color.white;
                step.transform.localScale = Vector3.one;
            }
        }
    }
}
