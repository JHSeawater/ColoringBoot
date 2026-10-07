using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 무한 모드 난이도 고르기 (GDD §5, 2026-10-07) — 난이도 버튼(이름 · 최소 수 범위 · 푼 수) · 뒤로(타이틀)
    public sealed class EndlessView : MonoBehaviour
    {
        [SerializeField] private Button[] _tierButtons;   // 쉬움 · 보통 · 어려움 순서
        [SerializeField] private TMP_Text[] _tierLabels;
        [SerializeField] private Button _backButton;

        public event Action<int> TierChosen;
        public event Action Back;

        private void OnEnable()
        {
            for (int i = 0; i < _tierButtons.Length; i++)
            {
                int tier = i;
                _tierButtons[i].onClick.AddListener(() => TierChosen?.Invoke(tier));
            }
            _backButton.onClick.AddListener(OnBack);
        }

        private void OnDisable()
        {
            foreach (Button button in _tierButtons) button.onClick.RemoveAllListeners();   // 코드로 단 것만 지운다(인스펙터 연결 없음)
            _backButton.onClick.RemoveListener(OnBack);
        }

        private void OnBack() => Back?.Invoke();

        // labels[i]: 난이도 i 버튼 글자(GameFlow가 이름 · 범위 · 푼 수로 만든다)
        public void Show(IReadOnlyList<string> labels)
        {
            for (int i = 0; i < _tierLabels.Length; i++) _tierLabels[i].text = labels[i];
            gameObject.SetActive(true);
        }
    }
}
