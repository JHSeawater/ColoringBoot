using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 옵션 화면 — 접근성 기호 · 소리 켜고 끄기 (보드 아래 버튼에서 옮김, 2026-09-29 사용자 요청). 바꾸면 SaveData에 저장하고 Changed로 알린다
    public sealed class OptionsView : MonoBehaviour
    {
        private const string SymbolsOn = "기호 표시: 켬";
        private const string SymbolsOff = "기호 표시: 끔";
        private const string SoundOn = "소리: 켬";
        private const string SoundOff = "소리: 끔";

        [SerializeField] private Button _symbolsButton;
        [SerializeField] private TMP_Text _symbolsLabel;
        [SerializeField] private Button _soundButton;
        [SerializeField] private TMP_Text _soundLabel;
        [SerializeField] private Button _closeButton;

        private SaveData _data;

        // 설정이 바뀌었다 (GameFlow가 보드 · 소리에 적용)
        public event Action Changed;

        public void Show(SaveData data)
        {
            _data = data;
            gameObject.SetActive(true);
            Refresh();
        }

        private void OnEnable()
        {
            _symbolsButton.onClick.AddListener(ToggleSymbols);
            _soundButton.onClick.AddListener(ToggleSound);
            _closeButton.onClick.AddListener(Hide);
        }

        private void OnDisable()
        {
            _symbolsButton.onClick.RemoveListener(ToggleSymbols);
            _soundButton.onClick.RemoveListener(ToggleSound);
            _closeButton.onClick.RemoveListener(Hide);
        }

        private void ToggleSymbols()
        {
            _data.Symbols = !_data.Symbols;
            Refresh();
            Changed?.Invoke();
        }

        private void ToggleSound()
        {
            _data.Sound = !_data.Sound;
            Refresh();
            Changed?.Invoke();
        }

        private void Hide() => gameObject.SetActive(false);

        private void Refresh()
        {
            _symbolsLabel.text = _data.Symbols ? SymbolsOn : SymbolsOff;
            _soundLabel.text = _data.Sound ? SoundOn : SoundOff;
        }
    }
}
