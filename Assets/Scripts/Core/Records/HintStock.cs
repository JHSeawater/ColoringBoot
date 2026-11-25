using System.Globalization;
using System.Text;

namespace ColoringBoot.Core
{
    // 힌트 개수 (GDD §6, 2026-11-25) — 최대 Max개, 모자라면 ChargeSeconds마다 1개씩 찬다(앱이 꺼져 있던 시간도 친다).
    // 시각은 인자로 받는다(유닉스 초) — 시계는 표현 계층이 읽는다. 저장은 문자열로 — 어디에 쓸지는 표현 계층이 정한다
    public sealed class HintStock
    {
        public const int Max = 3;
        public const int ChargeSeconds = 1800;

        private const string HeaderName = "hints";
        private const int Version = 1;
        private static readonly string Header = RecordHeader.Make(HeaderName, Version);
        private const char Separator = '\t';

        private int _count = Max;
        private long _anchor;   // 충전 시계가 시작된 시각 — 가득(_count == Max)일 때는 쓰지 않는다
        private bool _clockReset;   // 시계가 뒤로 가 충전 시계를 다시 세기 시작했다 — TakeClockReset으로 물을 때까지 기억한다

        // 지난 시간만큼 채운 뒤의 개수
        public int Count(long now)
        {
            Charge(now);
            return _count;
        }

        // 다음 1개까지 남은 초 — 가득이면 0
        public long SecondsToNext(long now)
        {
            Charge(now);
            return _count >= Max ? 0 : _anchor + ChargeSeconds - now;
        }

        // 1개를 쓴다. 없으면 false(아무것도 바뀌지 않음). 가득에서 쓰면 그 시각부터 충전 시계가 간다
        public bool TrySpend(long now)
        {
            Charge(now);
            if (_count == 0) return false;
            if (_count == Max) _anchor = now;
            _count--;
            return true;
        }

        // 한도까지 채운다(광고 보상 — Task 9.4)
        public void Fill() => _count = Max;

        // 시계가 뒤로 가 충전 시계를 지금부터 다시 세기로 했는가 — 한 번 물으면 지워진다. true면 저장한다(표현 계층):
        // 저장하지 않으면 저장된 시각이 계속 미래라, 앱을 켤 때마다 충전이 처음부터 다시 시작한다(2026-11-26 코드 리뷰 — 0개일 때는 힌트를 써서 저장되는 길도 없다)
        public bool TakeClockReset()
        {
            bool reset = _clockReset;
            _clockReset = false;
            return reset;
        }

        // 시계가 뒤로 갔으면(now < _anchor) 늘리지 않고 충전 시계를 지금부터 다시 센다
        private void Charge(long now)
        {
            if (_count >= Max) return;
            if (now < _anchor)
            {
                _anchor = now;
                _clockReset = true;
                return;
            }
            long charged = (now - _anchor) / ChargeSeconds;
            if (charged >= Max - _count)
            {
                _count = Max;
                return;
            }
            _count += (int)charged;
            _anchor += charged * ChargeSeconds;
        }

        // 줄 하나: 개수 \t 충전 시계가 시작된 시각
        public string Serialize() =>
            new StringBuilder(Header).Append('\n')
                .Append(_count.ToString(CultureInfo.InvariantCulture)).Append(Separator)
                .Append(_anchor.ToString(CultureInfo.InvariantCulture)).Append('\n').ToString();

        // 머리줄이 다르거나 비어 있으면 처음 상태(가득), 읽을 수 없는 줄 · 범위 밖 값은 건너뛴다(status로 알림). 줄이 여러 개면 마지막 줄이 이긴다
        public static HintStock Deserialize(string text, out RecordReadStatus status)
        {
            var stock = new HintStock();
            status = RecordHeader.Read(text, HeaderName, Version, Separator, (header, f) =>
            {
                if (f.Length != 2
                    || !int.TryParse(f[0], NumberStyles.None, CultureInfo.InvariantCulture, out int count)
                    || !long.TryParse(f[1], NumberStyles.None, CultureInfo.InvariantCulture, out long anchor)
                    || count > Max) return false;
                stock._count = count;
                stock._anchor = anchor;
                return true;
            }, Header);
            return stock;
        }
    }
}
