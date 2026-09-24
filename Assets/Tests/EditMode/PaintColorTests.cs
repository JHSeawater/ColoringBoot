using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    public class PaintColorTests
    {
        // 스모크 테스트: asmdef · 테스트 러너 파이프라인 확인을 겸한 색 혼합 첫 규칙 (GDD §2.3)
        [Test]
        public void RedOrYellow_IsOrange()
        {
            Assert.AreEqual(PaintColor.Orange, PaintColor.Red | PaintColor.Yellow);
        }
    }
}
