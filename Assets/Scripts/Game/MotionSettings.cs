using UnityEngine;

namespace ColoringBoot.Game
{
    // 보드 연출 값 (Phase 7.4) — 붓질 물결 · 클리어 반응 · 막힘 흔들림. BoardView가 참조하고, 값을 바꾸면 바로 반영된다(빌더 다시 실행 필요 없음)
    [CreateAssetMenu(menuName = "ColoringBoot/Motion Settings", fileName = "MotionSettings")]
    public sealed class MotionSettings : ScriptableObject
    {
        [Header("붓질 — 색이 줄을 따라 번진다")]
        [SerializeField] private float _strokeStep = 0.04f;    // 칸 하나 넘어가는 시간(초)
        [SerializeField] private float _popSeconds = 0.15f;    // 칸이 튀었다 돌아오는 시간
        [SerializeField] private float _popScale = 1.12f;
        [Header("클리어 — 모든 칸이 차례로 튄다")]
        [SerializeField] private float _clearStep = 0.03f;
        [SerializeField] private float _clearPopSeconds = 0.2f;
        [SerializeField] private float _clearPopScale = 1.15f;
        [Header("막힘 — 보드가 좌우로 흔들린다")]
        [SerializeField] private float _shakeSeconds = 0.35f;
        [SerializeField] private float _shakeAmplitude = 18f;  // 캔버스 단위
        [SerializeField] private float _shakeCycles = 4f;      // 흔들리는 횟수

        public float StrokeStep => _strokeStep;
        public float PopSeconds => _popSeconds;
        public float PopScale => _popScale;
        public float ClearStep => _clearStep;
        public float ClearPopSeconds => _clearPopSeconds;
        public float ClearPopScale => _clearPopScale;
        public float ShakeSeconds => _shakeSeconds;
        public float ShakeAmplitude => _shakeAmplitude;
        public float ShakeCycles => _shakeCycles;
    }
}
