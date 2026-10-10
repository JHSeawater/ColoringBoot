using ColoringBoot.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 칸 하나의 그림 — 채움 색 · 목표 색 마커 · 막힘 링 · 선택 링 · 붓질 미리보기 결과 · 색 기호 · 기믹 무늬 (Cell 프리팹)
    public sealed class CellView : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField] private GameObject _marker;
        [SerializeField] private Image _markerFill;
        [SerializeField] private GameObject _deadRing;
        [SerializeField] private GameObject _selectRing;
        [SerializeField] private Image _ghost;
        [SerializeField] private TMP_Text _symbol;
        [Tooltip("기호 자리(칸 기준 앵커) — 보통은 가운데, 목표 색 마커가 보이면 마커 위쪽으로 올린다(겹침 방지, 2026-10-01 사용자 결정)")]
        [SerializeField] private Vector2 _symbolMin = new Vector2(0.18f, 0.18f);
        [SerializeField] private Vector2 _symbolMax = new Vector2(0.82f, 0.82f);
        [SerializeField] private Vector2 _symbolAboveMarkerMin = new Vector2(0.28f, 0.64f);
        [SerializeField] private Vector2 _symbolAboveMarkerMax = new Vector2(0.72f, 0.88f);
        // 기믹 칸 무늬 (2026-10-10 시제품) — 벽 · 물 칸은 바탕색도 고정, 코팅 칸은 칸 색 위에 광택
        [SerializeField] private Image _overlay;
        [SerializeField] private Sprite _wallSprite;
        [SerializeField] private Sprite _waterSprite;
        [SerializeField] private Sprite _coatSprite;
        [SerializeField] private Color _wallFill = new Color32(0xA8, 0x97, 0x82, 0xFF);
        [SerializeField] private Color _wallLine = new Color32(0xE3, 0xDA, 0xC6, 0xFF);
        [SerializeField] private Color _waterFill = new Color32(0xDC, 0xEB, 0xEA, 0xFF);
        [SerializeField] private Color _waterLine = new Color32(0x5F, 0x95, 0xA3, 0xFF);
        [SerializeField] private Color _coatLine = new Color(1f, 1f, 1f, 0.8f);

        private bool _fixedFill;   // 벽 · 물 칸은 색이 없어 바탕색이 바뀌지 않는다

        public void SetFill(Color color)
        {
            if (!_fixedFill) _fill.color = color;
        }

        // 칸 종류에 맞는 바탕 · 무늬 — 칸을 만들 때 한 번 부른다
        public void SetKind(CellKind kind)
        {
            _fixedFill = kind == CellKind.Wall || kind == CellKind.Water;
            _overlay.gameObject.SetActive(kind != CellKind.Paint);
            switch (kind)
            {
                case CellKind.Wall:
                    _fill.color = _wallFill;
                    SetOverlay(_wallSprite, _wallLine);
                    break;
                case CellKind.Water:
                    _fill.color = _waterFill;
                    SetOverlay(_waterSprite, _waterLine);
                    break;
                case CellKind.Coated:
                    SetOverlay(_coatSprite, _coatLine);
                    break;
            }
        }

        public void SetMarker(bool visible, Color color)
        {
            _marker.SetActive(visible);
            _markerFill.color = color;
            PlaceSymbol();
        }

        public void SetDead(bool dead) => _deadRing.SetActive(dead);
        public void SetSelected(bool selected) => _selectRing.SetActive(selected);

        // 미리보기: 이 칸이 칠해질 색을 작은 반투명 육각형으로. 알파는 프리팹 값을 유지한다
        public void SetGhost(bool visible, Color color)
        {
            _ghost.gameObject.SetActive(visible);
            color.a = _ghost.color.a;
            _ghost.color = color;
        }

        // 접근성 기호 (R · Y · B 조합). 빈 문자열이면 숨김
        public void SetSymbol(string text, Color color)
        {
            _symbol.gameObject.SetActive(text.Length > 0);
            _symbol.text = text;
            _symbol.color = color;
            PlaceSymbol();
        }

        private void SetOverlay(Sprite sprite, Color color)
        {
            _overlay.sprite = sprite;
            _overlay.color = color;
        }

        // 마커가 보이면 기호를 마커 위로 — 글자 크기는 자동 맞춤이라 좁은 자리에서 작아진다
        private void PlaceSymbol()
        {
            bool above = _marker.activeSelf;
            _symbol.rectTransform.anchorMin = above ? _symbolAboveMarkerMin : _symbolMin;
            _symbol.rectTransform.anchorMax = above ? _symbolAboveMarkerMax : _symbolMax;
        }
    }
}
