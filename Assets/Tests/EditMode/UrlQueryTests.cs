using ColoringBoot.Game;
using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    // 주소 쿼리(2026-11-25 — 지적 M11): QA 주소 기능(?reset · ?unlock · ?stage= …)은 테스트 주소에서만 켜진다 —
    // 출시 주소에서 받은 링크 하나로 진행이 지워지거나 해금을 건너뛰지 않게 막는 관문(CLAUDE.md §5 · GameFlow.Awake)
    public class UrlQueryTests
    {
        [TestCase("https://jhseawater.github.io/ColoringBoot/?reset")]
        [TestCase("https://JHSeawater.GitHub.io/ColoringBoot/")]     // 호스트는 대소문자 무관
        [TestCase("http://localhost:5173/?unlock")]                   // 앱인토스 개발 서버
        [TestCase("http://127.0.0.1:8080/index.html?stage=Grape")]
        public void IsQaHost_TestAddresses(string url)
        {
            Assert.IsTrue(UrlQuery.IsQaHost(url));
        }

        [TestCase("https://example.com/ColoringBoot/?reset")]                 // 출시 주소(테스트 주소가 아님)
        [TestCase("https://jhseawater.github.io.example.com/?reset")]         // 테스트 주소로 시작하는 다른 호스트
        [TestCase("https://example.com/?from=jhseawater.github.io&reset")]    // 쿼리에만 테스트 주소
        [TestCase("file:///C:/ColoringBoot/index.html?reset")]               // 호스트 없음
        [TestCase("")]                                                        // 에디터(주소 없음)
        [TestCase(null)]
        [TestCase("not a url?reset")]
        public void IsQaHost_OtherAddresses(string url)
        {
            Assert.IsFalse(UrlQuery.IsQaHost(url));
        }

        [TestCase("https://x.io/?stage=Grape&stats", "stage", "Grape")]
        [TestCase("https://x.io/?stage=Grape&stats", "stats", "")]            // 값 없는 키
        [TestCase("https://x.io/?RESET", "reset", "")]                        // 키는 대소문자 무관
        [TestCase("https://x.io/?unlock#top", "unlock", "")]                  // # 뒤는 쿼리가 아님
        [TestCase("https://x.io/?palette=%EA%B0%80%EC%9D%84", "palette", "가을")]   // 퍼센트 인코딩을 푼다
        public void TryGet_FindsKey(string url, string key, string expected)
        {
            Assert.IsTrue(UrlQuery.TryGet(url, key, out string value));
            Assert.AreEqual(expected, value);
        }

        [TestCase("https://x.io/?stage=Grape", "reset")]
        [TestCase("https://x.io/reset", "reset")]                             // 쿼리 없음 — 경로의 같은 글자는 키가 아님
        [TestCase("https://x.io/?resetx&unlocked=1", "reset")]                // 이름이 정확히 같아야
        [TestCase("https://x.io/?stage=1#reset", "reset")]                    // # 뒤
        public void TryGet_MissingKey(string url, string key)
        {
            Assert.IsFalse(UrlQuery.TryGet(url, key, out string value));
            Assert.IsNull(value);
        }
    }
}
