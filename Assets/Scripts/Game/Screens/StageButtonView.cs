using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 스테이지 선택 화면의 육각 버튼 하나 — 잠김(흐림 + 자물쇠) · 열림 · 클리어(채움) · 완벽(채움 + 별) · 다음에 풀 스테이지(테두리)
    public sealed class StageButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _ring;
        [SerializeField] private TMP_Text _number;
        [SerializeField] private Image _lock;
        [SerializeField] private Image _star;

        public Button Button => _button;

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
    }
}
