using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 칸 하나의 그림 — 채움 색 · 목표 색 마커 · 막힘 링 · 선택 링 (Cell 프리팹)
    public sealed class CellView : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField] private GameObject _marker;
        [SerializeField] private Image _markerFill;
        [SerializeField] private GameObject _deadRing;
        [SerializeField] private GameObject _selectRing;

        public void SetFill(Color color) => _fill.color = color;

        public void SetMarker(bool visible, Color color)
        {
            _marker.SetActive(visible);
            _markerFill.color = color;
        }

        public void SetDead(bool dead) => _deadRing.SetActive(dead);
        public void SetSelected(bool selected) => _selectRing.SetActive(selected);
    }
}
