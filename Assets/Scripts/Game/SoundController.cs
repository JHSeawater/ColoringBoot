using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 사운드 구조 (GDD §6, 앱인토스 심사 항목) — 켜고 끄기 + 창 · 탭이 포커스를 잃으면 정지. 소리 에셋은 아직 없다.
    // 켜고 끄기 상태 저장은 저장 인터페이스(Phase 4) 때. 브라우저는 첫 사용자 입력 전에는 소리를 막는다(버그 아님)
    public sealed class SoundController : MonoBehaviour
    {
        private const string OnText = "소리 켬";
        private const string OffText = "소리 끔";

        [SerializeField] private Button _toggleButton;
        [SerializeField] private TMP_Text _toggleLabel;

        private bool _muted;

        private void OnEnable()
        {
            _toggleButton.onClick.AddListener(Toggle);
            Apply();
        }

        private void OnDisable()
        {
            _toggleButton.onClick.RemoveListener(Toggle);
        }

        // 백그라운드 전환(다른 탭 · 앱으로 이동) 시 정지, 돌아오면 재개
        private void OnApplicationFocus(bool hasFocus)
        {
            AudioListener.pause = !hasFocus;
        }

        private void Toggle()
        {
            _muted = !_muted;
            Apply();
        }

        private void Apply()
        {
            AudioListener.volume = _muted ? 0f : 1f;
            _toggleLabel.text = _muted ? OffText : OnText;
        }
    }
}
