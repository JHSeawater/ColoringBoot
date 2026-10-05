using System.Collections;
using UnityEngine;

namespace ColoringBoot.Game
{
    // 화면 전환 (Phase 7.3) — 이 화면이 켜질 때마다 짧게 나타난다. 화면을 켜고 끄는 쪽(GameFlow)은 그대로 SetActive만 한다.
    // 투명도만 바꾸고 입력은 막지 않는다(전환 중 누른 버튼도 그대로 동작)
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFade : MonoBehaviour
    {
        [SerializeField] private float _seconds = 0.2f;

        private CanvasGroup _group;

        private void Awake() => _group = GetComponent<CanvasGroup>();

        private void OnEnable()
        {
            if (_seconds <= 0f) return;
            _group.alpha = 0f;
            StartCoroutine(FadeIn());
        }

        // 꺼지면 코루틴도 멈춘다 — 다음에 켤 때 다시 0부터
        private void OnDisable() => _group.alpha = 1f;

        private IEnumerator FadeIn()
        {
            for (float time = 0f; time < _seconds; time += Time.unscaledDeltaTime)
            {
                _group.alpha = time / _seconds;
                yield return null;
            }
            _group.alpha = 1f;
        }
    }
}
