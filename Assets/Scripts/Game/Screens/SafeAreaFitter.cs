using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace ColoringBoot.Game
{
    // 안전영역(노치 · Dynamic Island)에 맞춰 이 패널의 앵커를 조정한다 (GDD §6, CLAUDE.md §6).
    // 화면 크기 · 안전영역이 바뀌거나(매 프레임 확인) 토스 인셋 번호가 바뀌거나 토스 값 읽기에 실패했을 때(TossPollInterval마다 확인) 다시 맞춘다.
    // WebGL 빌드는 앱인토스 SDK 값이 있으면 Screen.safeArea와 겹치는 쪽(더 좁은 영역)을 쓴다(토스 상단 ⋯ ✕ 영역까지 비움)
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _applied;
        private Vector2Int _screen;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        private void Update()
        {
            Rect safe = Screen.safeArea;
            bool screenChanged = safe != _applied || _screen.x != Screen.width || _screen.y != Screen.height;
            if (!screenChanged && !TossChanged()) return;
            _applied = safe;
            _screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Vector2 min = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            Vector2 max = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
#if UNITY_WEBGL && !UNITY_EDITOR
            ApplyTossInsets(ref min, ref max);
#endif
            _rect.anchorMin = min;
            _rect.anchorMax = max;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // 토스 ✕ 버튼: 공식 위치는 인셋 top 아래 iOS 5 · Android 10 CSS px(큰 쪽을 쓴다). 높이는 공식 값이 없어 캡처 추정(약 34)에 여유를 둔 값이다
        private const float CloseButtonOffset = 10f;
        private const float CloseBarHeight = 44f;
        private const float CloseBarGap = 8f;
        // 토스 여백을 뺀 패널이 캔버스의 이 비율보다 작으면(가로 · 분할 화면 · 떠 있는 창) 배치가 무너지므로 토스 값을 쓰지 않는다
        private const float MinPanelFraction = 0.5f;
        // 토스 인셋 번호 확인 · 읽기 실패 재시도 간격(초). 토스 밖 WebGL도 이 간격으로만 JS를 부른다
        private const float TossPollInterval = 0.5f;

        // 로그 종류마다 로드당 1회
        private const int LoggedOutsideToss = 1;
        private const int LoggedReadFailed = 2;
        private const int LoggedSubscribeFailed = 4;
        private const int LoggedTooSmall = 8;

        private static readonly float[] TossValues = new float[6];
        private int _logged;
        private int _tossVersion;
        private bool _tossReadFailed;
        private float _nextTossPoll;

        [DllImport("__Internal")] private static extern int CbSafeAreaVersion();
        [DllImport("__Internal")] private static extern int CbSafeAreaRead(float[] values);

        // 번호가 바뀌었거나 지난 읽기가 실패했으면 다시 맞춘다
        private bool TossChanged()
        {
            if (Time.unscaledTime < _nextTossPoll) return false;
            _nextTossPoll = Time.unscaledTime + TossPollInterval;
            int version = CbSafeAreaVersion();
            if (version == _tossVersion && !_tossReadFailed) return false;
            _tossVersion = version;
            return true;
        }

        // 인셋과 캔버스 크기가 모두 CSS 픽셀이라 배율 없이 나눈다. 토스 값을 못 쓰면 min · max(Screen.safeArea)를 그대로 두고 이유를 로그로 남긴다
        private void ApplyTossInsets(ref Vector2 min, ref Vector2 max)
        {
            int status = CbSafeAreaRead(TossValues);
            _tossReadFailed = status < 0;
            if (status == 0)
            {
                // 토스 브리지는 나중에 생길 수 있다 — 생기면 CbSafeAreaVersion이 올라 다시 맞춘다
                LogOnce(LoggedOutsideToss, LogType.Log, "window.AppsInToss가 없어 Screen.safeArea로 배치합니다(토스 밖). 브리지가 생기면 토스 값으로 다시 맞춥니다.");
                return;
            }
            if (status < 0)
            {
                LogOnce(LoggedReadFailed, LogType.Error, "토스 브리지는 있는데 안전영역을 읽지 못해 Screen.safeArea로 배치합니다 — 읽힐 때까지 다시 시도합니다. 계속되면 상단 버튼이 토스 ⋯ ✕에 가려집니다(SDK 브리지 이름 변경 확인).");
                return;
            }
            if (status == 2)
                LogOnce(LoggedSubscribeFailed, LogType.Warning, "토스 안전영역 변경 구독이 아직 안 됐습니다 — 지금 값은 적용하고 구독은 계속 다시 시도합니다.");

            float width = TossValues[4];
            float height = TossValues[5];
            float topInset = TossValues[0] + CloseButtonOffset + CloseBarHeight + CloseBarGap;
            Vector2 tossMin = new Vector2(TossValues[2] / width, TossValues[1] / height);
            Vector2 tossMax = new Vector2(1f - TossValues[3] / width, 1f - topInset / height);
            if (tossMax.x - tossMin.x < MinPanelFraction || tossMax.y - tossMin.y < MinPanelFraction)
            {
                LogOnce(LoggedTooSmall, LogType.Warning, "캔버스가 작아 토스 여백을 빼면 패널이 절반보다 작아집니다 — Screen.safeArea로 배치합니다(상단 버튼이 토스 ⋯ ✕에 가려질 수 있음).");
                return;
            }
            // 토스 값과 Screen.safeArea(기기 노치 · 홈 표시줄) 중 축마다 더 안쪽을 쓴다 — 토스가 bottom 0을 줘도 홈 표시줄을 피한다
            min = Vector2.Max(min, tossMin);
            max = Vector2.Min(max, tossMax);
        }

        private void LogOnce(int flag, LogType type, string message)
        {
            if ((_logged & flag) != 0) return;
            _logged |= flag;
            Debug.unityLogger.Log(type, (object)message, this);
        }
#else
        private static bool TossChanged() => false;
#endif
    }
}
