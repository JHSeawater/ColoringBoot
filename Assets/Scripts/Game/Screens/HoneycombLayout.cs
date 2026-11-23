using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 스테이지 버튼 벌집 배치(2026-11-23 사용자 — 선택 화면 A안): 줄마다 넓은 줄(_wideRow개) · 좁은 줄(하나 적게)을 번갈아 놓고 좁은 줄을 반 칸 민다.
    // 칸은 정사각형(육각 스프라이트가 정사각형 안의 정육각형). 마지막 줄이 덜 차면 왼쪽 자리부터 — 맞물림이 깨지지 않게 줄 너비로 가운데 맞추지 않는다
    public sealed class HoneycombLayout : LayoutGroup
    {
        [SerializeField] private float _cell = 259f;        // 칸 한 변
        [SerializeField] private float _columnStep = 236f;  // 같은 줄 칸 가운데 사이
        [SerializeField] private float _rowStep = 204f;     // 줄 사이(위아래 칸 가운데 사이)
        [SerializeField] private int _wideRow = 4;

        private float Width => _cell + (_wideRow - 1) * _columnStep;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            SetLayoutInputForAxis(Width, Width, Width, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            int rows = RowOf(rectChildren.Count - 1) + 1;
            float height = rectChildren.Count == 0 ? 0f : _cell + (rows - 1) * _rowStep;
            SetLayoutInputForAxis(height, height, height, -1f, 1);
        }

        public override void SetLayoutHorizontal() => Place(0);
        public override void SetLayoutVertical() => Place(1);

        private void Place(int axis)
        {
            float left = (rectTransform.rect.width - Width) * 0.5f;
            int rowStart = 0;
            for (int row = 0; rowStart < rectChildren.Count; row++)
            {
                int count = row % 2 == 0 ? _wideRow : _wideRow - 1;
                float x = left + (row % 2 == 0 ? 0f : _columnStep * 0.5f);
                for (int k = 0; k < count && rowStart + k < rectChildren.Count; k++)
                {
                    RectTransform child = rectChildren[rowStart + k];
                    if (axis == 0) SetChildAlongAxis(child, 0, x + k * _columnStep, _cell);
                    else SetChildAlongAxis(child, 1, row * _rowStep, _cell);
                }
                rowStart += count;
            }
        }

        // index번째 칸의 줄 번호(넓은 줄 · 좁은 줄을 번갈아)
        private int RowOf(int index)
        {
            if (index < 0) return -1;
            int pair = 2 * _wideRow - 1;
            int row = index / pair * 2;
            return index % pair < _wideRow ? row : row + 1;
        }
    }
}
