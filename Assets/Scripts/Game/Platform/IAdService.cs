using System;

namespace ColoringBoot.Game
{
    // 광고 자리 (GDD §6 · CLAUDE.md §5 플랫폼 서비스) — 앱인토스 연동 때 구현한다. 그전에는 NoAdService(바로 끝남)
    public interface IAdService
    {
        // 스테이지 사이 광고. 끝나면(또는 광고가 없으면) onDone
        void ShowBetweenStages(Action onDone);

        // 보상형 광고 — 다 보면 onRewarded. 광고가 없으면 바로 준다.
        // 쓰임: 힌트를 한도까지 채우기(HintStock.Fill — Task 9.4 광고 연동 때 연결, 2026-11-25). 힌트 · 건너뛰기는 더는 광고를 거치지 않는다(힌트 개수를 쓴다)
        void ShowRewarded(Action onRewarded);
    }

    public sealed class NoAdService : IAdService
    {
        public void ShowBetweenStages(Action onDone) => onDone?.Invoke();
        public void ShowRewarded(Action onRewarded) => onRewarded?.Invoke();
    }
}
