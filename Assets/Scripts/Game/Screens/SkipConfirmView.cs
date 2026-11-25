using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 스케치북 건너뛰기 확인 창 (GDD §5, 2026-11-25) — 안 풀린 판을 건너뛰면 힌트 1개를 쓴다는 것을 묻는다.
    // [건너뛰기]는 창을 닫고 Confirmed로 알린다(GameFlow가 힌트를 쓰고 넘긴다). [취소] · Android 뒤로가기는 닫기만. 빈 곳을 눌러도 닫히지 않는다
    public sealed class SkipConfirmView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _countLabel;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        public event Action Confirmed;

        // hints: 지금 남은 힌트 개수 — 창을 열 때 한 번 쓴다
        public void Show(int hints)
        {
            _countLabel.text = $"남은 힌트 {hints}개";
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void OnEnable()
        {
            _confirmButton.onClick.AddListener(Confirm);
            _cancelButton.onClick.AddListener(Hide);
        }

        private void OnDisable()
        {
            _confirmButton.onClick.RemoveListener(Confirm);
            _cancelButton.onClick.RemoveListener(Hide);
        }

        // 먼저 닫는다 — 두 번 눌러도 한 번만 처리된다
        private void Confirm()
        {
            Hide();
            Confirmed?.Invoke();
        }
    }
}
