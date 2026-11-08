using UnityEngine;

namespace ColoringBoot.Game
{
    // 사운드 (GDD §6, 앱인토스 심사 항목) — 효과음 재생 + 켜고 끄기 + 창 · 탭이 포커스를 잃으면 정지.
    // 켜고 끄기는 옵션 화면에서(GameFlow가 저장된 설정으로 부른다). 브라우저는 첫 사용자 입력 전에는 소리를 막는다(버그 아님)
    public sealed class SoundController : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip _buttonClip;
        [SerializeField] private AudioClip _popClip;        // 칸 하나가 칠해질 때
        [SerializeField] private AudioClip _wrongClip;      // 목표에 없는 색이 섞여 막히는 획
        [SerializeField] private AudioClip _clearPopClip;   // 클리어 연출에서 칸 하나가 커질 때
        [SerializeField] private AudioClip[] _strokeClips;  // move-1 ~ move-8 순서

        public void SetSoundOn(bool on)
        {
            AudioListener.volume = on ? 1f : 0f;
        }

        // 씬의 모든 UI 버튼 OnClick에 빌더가 연결한다
        public void PlayButton() => _source.PlayOneShot(_buttonClip);

        // 획이 실제로 칠해졌을 때. moveCount는 이 획까지의 수(되돌리면 줄어든다)
        public void PlayStroke(int moveCount, int? minMoves)
        {
            AudioClip clip = StrokeClip(moveCount, minMoves);
            if (clip != null) _source.PlayOneShot(clip);
        }

        public void PlayWrong() => _source.PlayOneShot(_wrongClip);

        public void PlayCellPainted() => _source.PlayOneShot(_popClip);

        public void PlayClearPop() => _source.PlayOneShot(_clearPopClip);

        // 마지막 최소 획이 항상 move-8이 되도록 끝을 맞춘다 — 최소 2수면 7 · 8, 최소 8수면 1 ~ 8. 최소 수를 넘거나 최소 수가 없는 판은 없음(칸 소리만)
        private AudioClip StrokeClip(int moveCount, int? minMoves)
        {
            if (!minMoves.HasValue || moveCount > minMoves.Value) return null;
            int index = _strokeClips.Length - minMoves.Value + moveCount - 1;
            return index >= 0 ? _strokeClips[index] : null;
        }

        // 백그라운드 전환(다른 탭 · 앱으로 이동) 시 정지, 돌아오면 재개
        private void OnApplicationFocus(bool hasFocus)
        {
            AudioListener.pause = !hasFocus;
        }
    }
}
