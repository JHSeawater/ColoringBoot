using System;

namespace ColoringBoot.Core
{
    // 칸과 붓의 색. 기본색 비트마스크(빨강 1 · 노랑 2 · 파랑 4)이고 혼합은 OR (GDD §2.3)
    [Flags]
    public enum PaintColor : byte
    {
        Empty = 0,
        Red = 1,
        Yellow = 2,
        Orange = Red | Yellow,
        Blue = 4,
        Purple = Red | Blue,
        Green = Yellow | Blue,
        Black = Red | Yellow | Blue,
    }
}
