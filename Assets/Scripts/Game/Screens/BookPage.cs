using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 챕터 그림 책장(2026-11-24 사용자 — "실제 컬러링북처럼 책을 넘기는 페이지"): 겹친 종이 · 왼쪽 스프링은 빌더가 만들고, 이 컴포넌트는 장 넘기기만 한다.
    // 넘기기(2026-11-24 비교 페이지 B안 "들어 올려 넘기기") = 장이 왼쪽 제본을 축으로 화면 앞으로 들려 왼쪽으로 180° 넘어간다 — 반쯤 넘어가면 종이 뒷면,
    // 끝 무렵 사라진다(화면에 왼쪽 장이 없으므로). 셰이더 PageTurn. 앞으로(+1)는 지난 장이 넘어가며 새 장이 드러나고,
    // 뒤로(-1)는 새 장이 왼쪽에서 넘어와 지난 장을 덮는다. 부르기 전에 _page에 새 그림을 Show해 둔다(GameFlow)
    public sealed class BookPage : MonoBehaviour
    {
        private static readonly int AngleId = Shader.PropertyToID("_Angle");
        private static readonly int SpineId = Shader.PropertyToID("_Spine");
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int DistanceId = Shader.PropertyToID("_Distance");
        private static readonly int FadeId = Shader.PropertyToID("_Fade");
        private const float FadeFrom = 0.8f;   // 넘긴 정도가 이만큼을 넘으면 사라지기 시작

        [SerializeField] private ChapterView _page;       // 지금 장
        [SerializeField] private ChapterView _turning;    // 넘기는 동안만 켜는 지난 장(_page 바로 위나 아래)
        [SerializeField] private Shader _turnShader;      // ColoringBoot/UI/PageTurn — 없으면 넘기지 않고 바로 바뀐다
        [SerializeField] private float _seconds = 0.6f;
        [SerializeField] private float _distance = 4.5f;  // 원근 거리 = 장 너비 × 이 값(작을수록 원근이 세다 — 들린 쪽이 다가오므로 1보다 커야 한다)

        private readonly Vector3[] _corners = new Vector3[4];
        private Material _material;
        private Transform _canvas;
        private ChapterView _moving;
        private Coroutine _routine;

        // 넘기는 데 걸리는 초(셰이더가 없으면 0 — 바로 바뀜)
        public float Seconds => _turnShader != null ? _seconds : 0f;

        // fromArt · fromPainted: 넘기기 전 장(지난 챕터 그림과 칠한 단계). direction: +1 앞으로 · -1 뒤로
        public void Turn(ChapterArt fromArt, IReadOnlyList<bool> fromPainted, int direction)
        {
            Stop();
            if (_turnShader == null || !isActiveAndEnabled) return;
            if (_material == null) _material = new Material(_turnShader);
            // UI 꼭짓점은 맨 위 캔버스 공간이다(책장이 따로 캔버스여도 — 그쪽 공간으로 재면 원근 가운데가 어긋난다)
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>().rootCanvas.transform;
            bool forward = direction > 0;
            _turning.gameObject.SetActive(true);
            _turning.Show(fromArt, fromPainted);
            // 움직이는 장이 위에 — 앞으로는 지난 장, 뒤로는 새 장. 둘은 늘 붙어 있다(아래 겹친 종이 · 위 스프링 사이)
            int lower = Mathf.Min(_page.transform.GetSiblingIndex(), _turning.transform.GetSiblingIndex());
            _moving = forward ? _turning : _page;
            (forward ? _page : _turning).transform.SetSiblingIndex(lower);
            _moving.transform.SetSiblingIndex(lower + 1);
            SetMaterial(_moving, _material);
            SetAmount(forward ? 0f : 1f);
            _routine = StartCoroutine(Animate(forward));
        }

        private IEnumerator Animate(bool forward)
        {
            for (float time = 0f; time < _seconds; time += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, time / _seconds);
                SetAmount(forward ? k : 1f - k);
                yield return null;
            }
            _routine = null;
            Stop();
        }

        // 넘긴 정도(0 = 평평 · 0.5 = 제본에 직각, 옆에서 본 선 · 1 = 왼쪽으로 다 넘어감). 장 자리는 매번 다시 잰다(화면 크기 · 회전이 바뀌어도 제본에 붙게)
        private void SetAmount(float amount)
        {
            ((RectTransform)_page.transform).GetWorldCorners(_corners);
            Vector3 min = _canvas.InverseTransformPoint(_corners[0]);
            Vector3 max = _canvas.InverseTransformPoint(_corners[2]);
            _material.SetFloat(SpineId, min.x);
            _material.SetVector(CenterId, (min + max) * 0.5f);
            _material.SetFloat(DistanceId, (max.x - min.x) * _distance);
            _material.SetFloat(AngleId, amount * Mathf.PI);
            _material.SetFloat(FadeId, Mathf.Clamp01((1f - amount) / (1f - FadeFrom)));
        }

        // 넘기기를 끝낸 모습으로 — 움직이던 장은 평평하게, 지난 장은 끈다
        private void Stop()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            if (_moving != null) SetMaterial(_moving, null);
            _moving = null;
            if (_turning.gameObject.activeSelf) _turning.gameObject.SetActive(false);
        }

        private static void SetMaterial(ChapterView view, Material material)
        {
            foreach (Graphic graphic in view.GetComponentsInChildren<Graphic>(true)) graphic.material = material;
        }

        // 화면을 떠나 넘기기가 끊기면 다 넘긴 모습으로 둔다
        private void OnDisable()
        {
            if (_moving != null || _routine != null) Stop();
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
