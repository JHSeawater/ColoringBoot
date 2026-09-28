using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 칸 하나의 그림 — 채움 색 · 목표 색 마커 · 막힘 링 · 선택 링 · 붓질 미리보기 결과 · 색 기호 (Cell 프리팹)
    public sealed class CellView : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField] private GameObject _marker;
        [SerializeField] private Image _markerFill;
        [SerializeField] private GameObject _deadRing;
        [SerializeField] private GameObject _selectRing;
        [SerializeField] private Image _ghost;
        [SerializeField] private TMP_Text _symbol;

        public void SetFill(Color color) => _fill.color = color;

        public void SetMarker(bool visible, Color color)
        {
            _marker.SetActive(visible);
            _markerFill.color = color;
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
        }
    }
}
