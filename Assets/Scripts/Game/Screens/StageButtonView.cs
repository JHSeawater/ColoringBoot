using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 스테이지 선택 화면의 육각 버튼 하나 — 잠김(흐림 + 자물쇠) · 열림 · 클리어(그 판 대표 색으로 채움) · 완벽(채움 + 별) · 다음에 풀 스테이지(테두리).
    // 벌집 배치(2026-11-23)에서 칸(정사각형)이 이웃과 겹치므로 육각형 안쪽만 누른 것으로 본다(ICanvasRaycastFilter)
    public sealed class StageButtonView : MonoBehaviour, ICanvasRaycastFilter
    {
        private const float HexRadius = 0.98f;              // 육각 스프라이트의 반지름(칸 반 변 대비) — SpriteBuilder.HexRadius
        private const float HalfWidth = 0.8660254f;         // 꼭짓점이 위인 정육각형의 반 너비 / 반지름 = √3/2
        private const float InvSqrt3 = 0.57735027f;

        [SerializeField] private Button _button;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _ring;
        [SerializeField] private TMP_Text _number;
        [SerializeField] private Image _lock;
        [SerializeField] private Image _star;

        private RectTransform _rect;

        public Button Button => _button;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        public void Set(int number, bool unlocked, bool perfect, bool next, Color fill, Color text)
        {
            _fill.color = fill;
            _number.text = number.ToString();
            _number.color = text;
            _number.gameObject.SetActive(unlocked);
            _lock.gameObject.SetActive(!unlocked);
            _star.gameObject.SetActive(perfect);
            _ring.gameObject.SetActive(next);
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPoint, eventCamera, out Vector2 local)) return false;
            Rect rect = _rect.rect;
            float radius = HexRadius * rect.height * 0.5f;
            float x = Mathf.Abs(local.x - rect.center.x);
            float y = Mathf.Abs(local.y - rect.center.y);
            return x <= radius * HalfWidth && y <= radius - x * InvSqrt3;
        }
    }
}
