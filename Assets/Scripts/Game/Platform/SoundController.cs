using UnityEngine;

namespace ColoringBoot.Game
{
    // 사운드 구조 (GDD §6, 앱인토스 심사 항목) — 켜고 끄기 + 창 · 탭이 포커스를 잃으면 정지. 소리 에셋은 아직 없다.
    // 켜고 끄기는 옵션 화면에서(GameFlow가 저장된 설정으로 부른다). 브라우저는 첫 사용자 입력 전에는 소리를 막는다(버그 아님)
    public sealed class SoundController : MonoBehaviour
    {
        public void SetSoundOn(bool on)
        {
            AudioListener.volume = on ? 1f : 0f;
        }

        // 백그라운드 전환(다른 탭 · 앱으로 이동) 시 정지, 돌아오면 재개
        private void OnApplicationFocus(bool hasFocus)
        {
            AudioListener.pause = !hasFocus;
        }
    }
}
