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

        private readonly List<Image> _steps = new List<Image>();
        private ChapterArt _art;
        private Coroutine _routine;

        // painted[i] = i번째 단계를 칠했는가. animateStep: 막 칠한 단계(-1이면 연출 없음). complete: 그 뒤 완성 연출
        public void Show(ChapterArt art, IReadOnlyList<bool> painted, int animateStep = -1, bool complete = false)
        {
            if (art != _art) Build(art);
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            transform.localScale = Vector3.one;
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

            AddImage("Paper", null, new RectInt(0, 0, art.Canvas.x, art.Canvas.y)).color = _paper;
            for (int i = 0; i < art.Steps.Count; i++) _steps.Add(AddImage($"Step{i + 1:00}", art.Steps[i].Sprite, art.Steps[i].Rect));
            AddImage("Line", art.Line.Sprite, art.Line.Rect);
        }

        // rect: 캔버스 픽셀(왼쪽 위 기준) → 이 뷰 안의 비율 앵커. 조각이 캔버스 밖으로 조금 넘쳐도 된다(투명 여백)
        private Image AddImage(string name, Sprite sprite, RectInt rect)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
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
            for (float time = 0f; time < _paintSeconds; time += Time.unscaledDeltaTime)
            {
                float k = time / _paintSeconds;
                step.color = new Color(1f, 1f, 1f, k);
                t.localScale = Vector3.one * Mathf.Lerp(1f, _paintScale, Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            step.color = Color.white;
            t.localScale = Vector3.one;
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

        // 화면을 떠나 연출이 끊기면 다 칠한 모습으로 둔다
        private void OnDisable()
        {
            if (_routine == null) return;
            StopCoroutine(_routine);
            _routine = null;
            transform.localScale = Vector3.one;
            foreach (Image step in _steps)
            {
                step.color = Color.white;
                step.transform.localScale = Vector3.one;
            }
        }
    }
}
