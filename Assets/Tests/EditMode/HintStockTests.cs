using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    // 힌트 개수 (GDD §6, 2026-11-25, Docs/plans/2026-11-25-hint-stock.md) — 최대 3개 · 1800초마다 1개. 시각은 유닉스 초로 넘긴다
    public class HintStockTests
    {
        private const long T0 = 1_800_000_000;   // 테스트 기준 시각
        private const int Charge = HintStock.ChargeSeconds;

        // 가득에서 T0에 n개를 쓴 상태 — 충전 시계는 T0부터
        private static HintStock Spent(int n)
        {
            var stock = new HintStock();
            for (int i = 0; i < n; i++) Assert.IsTrue(stock.TrySpend(T0));
            return stock;
        }

        [Test]
        public void New_IsFull()
        {
            var stock = new HintStock();

            Assert.AreEqual(3, HintStock.Max);
            Assert.AreEqual(1800, HintStock.ChargeSeconds);
            Assert.AreEqual(3, stock.Count(T0));
            Assert.AreEqual(0, stock.SecondsToNext(T0));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("hints 1\n")]
        public void EmptyText_IsFullAndOk(string text)
        {
            HintStock stock = HintStock.Deserialize(text, out RecordReadStatus status);

            Assert.AreEqual(RecordReadStatus.Ok, status);
            Assert.AreEqual(3, stock.Count(T0));
            Assert.AreEqual(0, stock.SecondsToNext(T0));
        }

        [Test]
        public void SpendFromFull_StartsClockAtThatTime()
        {
            var stock = new HintStock();
            Assert.AreEqual(3, stock.Count(T0 - 100_000), "가득일 때는 시각이 지나도 그대로");

            Assert.IsTrue(stock.TrySpend(T0));

            Assert.AreEqual(2, stock.Count(T0));
            Assert.AreEqual(Charge, stock.SecondsToNext(T0));
            Assert.AreEqual(2, stock.Count(T0 + Charge - 1), "1799초 뒤에는 그대로");
            Assert.AreEqual(1, stock.SecondsToNext(T0 + Charge - 1));
            Assert.AreEqual(3, stock.Count(T0 + Charge), "1800초 뒤 1개");
            Assert.AreEqual(0, stock.SecondsToNext(T0 + Charge));
        }

        [Test]
        public void Charge_StopsAtMax()
        {
            HintStock stock = Spent(3);
            Assert.AreEqual(0, stock.Count(T0));

            Assert.AreEqual(3, stock.Count(T0 + 3 * Charge), "5400초 뒤 3개");
            Assert.AreEqual(3, Spent(3).Count(T0 + 100 * Charge), "한도를 넘지 않음");
        }

        [Test]
        public void Charge_KeepsRemainder()
        {
            HintStock stock = Spent(3);

            Assert.AreEqual(1, stock.Count(T0 + 2700), "2700초 뒤 1개");
            Assert.AreEqual(900, stock.SecondsToNext(T0 + 2700), "몫이 이어짐");
            Assert.AreEqual(2, stock.Count(T0 + 2 * Charge));
        }

        [Test]
        public void SpendBelowMax_KeepsClock()
        {
            HintStock stock = Spent(1);

            Assert.IsTrue(stock.TrySpend(T0 + 600));

            Assert.AreEqual(1, stock.Count(T0 + 600));
            Assert.AreEqual(Charge - 600, stock.SecondsToNext(T0 + 600), "충전 시계는 첫 힌트를 쓴 시각부터");
        }

        [Test]
        public void TrySpend_AtZero_FailsAndChangesNothing()
        {
            HintStock stock = Spent(3);
            string before = stock.Serialize();

            Assert.IsFalse(stock.TrySpend(T0 + 100));

            Assert.AreEqual(before, stock.Serialize(), "개수 · 시각 그대로");
            Assert.AreEqual(0, stock.Count(T0 + 100));
            Assert.AreEqual(Charge - 100, stock.SecondsToNext(T0 + 100));
        }

        [Test]
        public void ClockMovedBack_DoesNotChargeAndRestartsClock()
        {
            HintStock stock = Spent(3);

            Assert.AreEqual(0, stock.Count(T0 - 5000), "시계가 뒤로 가면 늘지 않음");
            Assert.AreEqual(Charge, stock.SecondsToNext(T0 - 5000));
            Assert.AreEqual(0, stock.Count(T0 - 5000 + Charge - 1));
            Assert.AreEqual(1, stock.Count(T0 - 5000 + Charge), "그 뒤 1800초가 지나야 1개");
        }

        // 시계를 바로잡았으면 한 번 알린다(2026-11-26 코드 리뷰) — 표현 계층이 그때 저장해야 다음 실행이 이어서 센다
        [Test]
        public void ClockMovedBack_ReportsResetOnce()
        {
            HintStock stock = Spent(3);
            Assert.IsFalse(stock.TakeClockReset(), "평소에는 알리지 않음");
            stock.Count(T0 + 100);
            Assert.IsFalse(stock.TakeClockReset(), "시계가 앞으로 가는 것은 바로잡기가 아님");

            stock.Count(T0 - 5000);

            Assert.IsTrue(stock.TakeClockReset());
            Assert.IsFalse(stock.TakeClockReset(), "한 번 물으면 지워짐");
            Assert.AreEqual($"hints 1\n0\t{T0 - 5000}\n", stock.Serialize(), "바로잡은 시각이 저장 값에 들어감");
        }

        // 저장된 충전 시각이 미래(시계가 앞서 있을 때 힌트를 다 씀 → 시계를 바로잡음): 바로잡은 값을 저장하면 다음 실행이 이어서 세고,
        // 저장하지 않으면 켤 때마다 30분이 처음부터 다시 시작한다
        [Test]
        public void FutureAnchor_SavedAfterReset_NextRunContinues()
        {
            string saved = $"hints 1\n0\t{T0 + 86400}\n";
            HintStock first = HintStock.Deserialize(saved, out _);
            Assert.AreEqual(0, first.Count(T0));
            Assert.AreEqual(Charge, first.SecondsToNext(T0));
            Assert.IsTrue(first.TakeClockReset());

            HintStock next = HintStock.Deserialize(first.Serialize(), out RecordReadStatus status);
            Assert.AreEqual(RecordReadStatus.Ok, status);
            Assert.AreEqual(Charge - 1200, next.SecondsToNext(T0 + 1200), "저장했으면 20분 뒤 실행은 10분 남음");
            Assert.IsFalse(next.TakeClockReset());
            Assert.AreEqual(1, next.Count(T0 + Charge));

            HintStock unsaved = HintStock.Deserialize(saved, out _);
            Assert.AreEqual(Charge, unsaved.SecondsToNext(T0 + 1200), "저장하지 않았다면 다시 30분");
        }

        // 가득일 때는 충전 시각을 쓰지 않는다 — 미래 시각이 남아 있어도 바로잡을 것이 없다
        [Test]
        public void FutureAnchor_WhenFull_NoReset()
        {
            HintStock stock = HintStock.Deserialize($"hints 1\n3\t{T0 + 86400}\n", out _);

            Assert.AreEqual(3, stock.Count(T0));
            Assert.IsFalse(stock.TakeClockReset());
        }

        [Test]
        public void Fill_GivesMax()
        {
            HintStock stock = Spent(3);

            stock.Fill();

            Assert.AreEqual(3, stock.Count(T0));
            Assert.AreEqual(0, stock.SecondsToNext(T0));
            Assert.IsTrue(stock.TrySpend(T0 + 50));
            Assert.AreEqual(Charge, stock.SecondsToNext(T0 + 50), "채운 뒤 쓰면 그 시각부터 다시");
        }

        [Test]
        public void RoundTrip_KeepsCountAndClock()
        {
            HintStock stock = Spent(2);
            string text = stock.Serialize();

            HintStock read = HintStock.Deserialize(text, out RecordReadStatus status);

            Assert.AreEqual(RecordReadStatus.Ok, status, "끝의 빈 줄은 버림이 아님");
            StringAssert.StartsWith("hints 1\n", text);
            Assert.AreEqual($"hints 1\n1\t{T0}\n", text);
            Assert.AreEqual(1, read.Count(T0 + 600));
            Assert.AreEqual(Charge - 600, read.SecondsToNext(T0 + 600));
            Assert.AreEqual(text, read.Serialize());
        }

        // 앱이 꺼져 있던 시간도 친다 — 저장한 (개수, 시각)에서 다시 계산
        [Test]
        public void Deserialize_ChargesForTimeAway()
        {
            HintStock read = HintStock.Deserialize($"hints 1\n0\t{T0}\n", out _);

            Assert.AreEqual(1, read.Count(T0 + 31 * 60));
            Assert.AreEqual(29 * 60, read.SecondsToNext(T0 + 31 * 60));
        }

        [TestCase("hints 1\nbroken\n")]
        [TestCase("hints 1\nx\t100\n")]
        [TestCase("hints 1\n2\tx\n")]
        [TestCase("hints 1\n-1\t100\n")]
        [TestCase("hints 1\n4\t100\n")]
        [TestCase("hints 1\n2\t-100\n")]
        [TestCase("hints 1\n2\t100\t7\n")]
        public void BrokenOrOutOfRangeLine_IsDroppedAndStartsFull(string text)
        {
            HintStock read = HintStock.Deserialize(text, out RecordReadStatus status);

            Assert.AreEqual(RecordReadStatus.LinesDropped, status);
            Assert.AreEqual(3, read.Count(T0));
        }

        [Test]
        public void SeveralLines_LastWins()
        {
            HintStock read = HintStock.Deserialize($"hints 1\n2\t{T0}\n0\t{T0}\n", out RecordReadStatus status);

            Assert.AreEqual(RecordReadStatus.Ok, status);
            Assert.AreEqual(0, read.Count(T0));
        }

        [TestCase("hints 2\n1\t100\n", RecordReadStatus.NewerVersion)]
        [TestCase("garbage", RecordReadStatus.Unreadable)]
        [TestCase("hints x\n1\t100\n", RecordReadStatus.Unreadable)]
        [TestCase("endless 1\n1\t100\n", RecordReadStatus.Unreadable)]
        public void UnknownHeader_StartsFull(string text, RecordReadStatus expected)
        {
            HintStock read = HintStock.Deserialize(text, out RecordReadStatus status);

            Assert.AreEqual(expected, status);
            Assert.AreEqual(3, read.Count(T0));
        }
    }
}
