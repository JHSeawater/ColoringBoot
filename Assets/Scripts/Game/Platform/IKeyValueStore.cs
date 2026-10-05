namespace ColoringBoot.Game
{
    // 저장소 (CLAUDE.md §3 플랫폼 서비스 격리) — 진행 · 기록을 문자열로 읽고 쓴다.
    // 지금은 PlayerPrefsStore, 앱인토스 연동 때 네이티브 저장소 구현으로 바꾼다
    public interface IKeyValueStore
    {
        // 없으면 null
        string Load(string key);
        void Save(string key, string value);
        void Delete(string key);
    }
}
