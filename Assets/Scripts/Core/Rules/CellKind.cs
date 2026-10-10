namespace ColoringBoot.Core
{
    // 칸 종류 (GDD §7 기믹 후보 — 2026-10-10 시제품, 규칙은 사용자 결정)
    public enum CellKind
    {
        Paint,   // 일반 칸
        Coated,  // 코팅 칸: 색이 바뀌지 않는다. 지나가는 붓에는 그 색이 묻는다(마르지 않는 물감)
        Wall,    // 벽: 색이 없고 고를 수 없다. 줄을 구간으로 끊는다 — 구간마다 따로 된 줄
        Water,   // 물 칸: 색이 없다. 지나가는 붓이 빈다(획은 줄 끝까지 이어진다)
    }
}
