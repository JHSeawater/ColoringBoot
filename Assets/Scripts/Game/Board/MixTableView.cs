using ColoringBoot.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 색 조합표 — 스테이지 팔레트의 기본 3색 · 섞인 3색 · 세 색이 모두 섞인 색 (GDD §6).
    // 기본 팔레트가 아니어도 섞은 결과를 추론할 수 있게 한다. 가로 배치는 이 오브젝트의 HorizontalLayoutGroup이 한다
    public sealed class MixTableView : MonoBehaviour
    {
        private static readonly PaintColor[][] _rows =
        {
            new[] { PaintColor.Red, PaintColor.Yellow, PaintColor.Orange },
            new[] { PaintColor.Yellow, PaintColor.Blue, PaintColor.Green },
            new[] { PaintColor.Red, PaintColor.Blue, PaintColor.Purple },
            new[] { PaintColor.Red, PaintColor.Yellow, PaintColor.Blue, PaintColor.Black },
        };

        [SerializeField] private Sprite _chipSprite;
        [SerializeField] private float _chipSize = 40f;
        [SerializeField] private float _signWidth = 22f;
        [SerializeField] private float _rowGap = 24f;
        [SerializeField] private float _fontSize = 36f;
        [SerializeField] private Color _textColor = new Color32(0x1C, 0x22, 0x2C, 0xFF);

        // 스테이지마다 다시 부른다 (팔레트가 스테이지마다 다를 수 있다)
        public void Build(ColorPalette palette)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
            for (int row = 0; row < _rows.Length; row++)
            {
                if (row > 0) NewChild("Gap", _rowGap);
                PaintColor[] colors = _rows[row];
                for (int i = 0; i < colors.Length; i++)
                {
                    if (i > 0) AddSign(i == colors.Length - 1 ? "=" : "+");
                    var chip = NewChild("Chip", _chipSize).AddComponent<Image>();
                    chip.sprite = _chipSprite;
                    chip.color = palette.Get(colors[i]);
                    chip.raycastTarget = false;
                }
            }
        }

        private void AddSign(string sign)
        {
            var text = NewChild("Sign", _signWidth).AddComponent<TextMeshProUGUI>();
            text.text = sign;
            text.fontSize = _fontSize;
            text.color = _textColor;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        private GameObject NewChild(string name, float width)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(width, _chipSize);
            return go;
        }
    }
}
