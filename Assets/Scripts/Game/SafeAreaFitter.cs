using UnityEngine;

namespace ColoringBoot.Game
{
    // 안전영역(노치 · Dynamic Island)에 맞춰 이 패널의 앵커를 조정한다 (GDD §6, CLAUDE.md §6).
    // 화면 크기나 안전영역이 바뀔 때만 다시 맞춘다. WebGL이 실제 노치 값을 주는지는 휴대폰에서 확인한다
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
            if (safe == _applied && _screen.x == Screen.width && _screen.y == Screen.height) return;
            _applied = safe;
            _screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            _rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            _rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        }
    }
}
