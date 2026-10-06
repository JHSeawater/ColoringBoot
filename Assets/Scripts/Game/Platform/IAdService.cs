using System;

namespace ColoringBoot.Game
{
    // 광고 자리 (GDD §6 · CLAUDE.md §3) — 앱인토스 연동 때 구현한다. 그전에는 NoAdService(바로 끝남)
    public interface IAdService
    {
        // 스테이지 사이 광고. 끝나면(또는 광고가 없으면) onDone
        void ShowBetweenStages(Action onDone);

        // 보상형 광고 — 다 보면 onRewarded (힌트, 2026-10-07). 광고가 없으면 바로 준다
        void ShowRewarded(Action onRewarded);
    }

    public sealed class NoAdService : IAdService
    {
        public void ShowBetweenStages(Action onDone) => onDone?.Invoke();
        public void ShowRewarded(Action onRewarded) => onRewarded?.Invoke();
    }
}
