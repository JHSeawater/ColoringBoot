using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ColoringBoot.Core
{
    // 한 스테이지의 플레이테스트 기록 — 첫 클리어까지 (GDD §9 "추론인가 찍기인가")
    public readonly struct StageStats
    {
        public StageStats(int opens, int strokes, int undos, int restarts, int hints, double seconds, int? clearMoves)
        {
            Opens = opens;
            Strokes = strokes;
            Undos = undos;
            Restarts = restarts;
            Hints = hints;
            Seconds = seconds;
            ClearMoves = clearMoves;
        }

        public int Opens { get; }        // 열어 본 횟수
        public int Strokes { get; }      // 둔 수 합계 (되돌린 수 포함)
        public int Undos { get; }
        public int Restarts { get; }
        public int Hints { get; }        // 힌트를 본 횟수 (2026-10-07)
        public double Seconds { get; }   // 걸린 시간 (앱이 앞에 있을 때만)
        public int? ClearMoves { get; }  // 클리어했을 때 수 — 없으면 아직 못 풂
    }

    // 스테이지별 플레이테스트 기록 (2026-09-30 사용자 결정). 첫 클리어 뒤로는 세지 않는다 — 다시 하기는 추론을 보여 주지 않으므로
    public sealed class PlayStats
    {
        private const string HeaderName = "stats";
        private const int Version = 2;   // 2026-10-07: 힌트 횟수 칸 추가
        private static readonly string Header = RecordHeader.Make(HeaderName, Version);
        private static readonly string HeaderV1 = RecordHeader.Make(HeaderName, 1);   // 옛 형식(힌트 칸 없음)도 읽는다
        private const char Separator = '\t';
        private const string NotCleared = "-";

        private sealed class Entry
        {
            public int Opens, Strokes, Undos, Restarts, Hints;
            public double Seconds;
            public int? ClearMoves;
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);

        public StageStats Get(string stage)
        {
            if (!_entries.TryGetValue(stage, out Entry e)) return default;
            return new StageStats(e.Opens, e.Strokes, e.Undos, e.Restarts, e.Hints, e.Seconds, e.ClearMoves);
        }

        // 이름이 prefix로 시작하는 스테이지들의 합 — 무한 모드 난이도별 기록(2026-10-07). 클리어 수는 합할 수 없어 null
        public StageStats Sum(string prefix)
        {
            int opens = 0, strokes = 0, undos = 0, restarts = 0, hints = 0;
            double seconds = 0;
            foreach (KeyValuePair<string, Entry> entry in _entries)
            {
                if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal)) continue;
                Entry e = entry.Value;
                opens += e.Opens;
                strokes += e.Strokes;
                undos += e.Undos;
                restarts += e.Restarts;
                hints += e.Hints;
                seconds += e.Seconds;
            }
            return new StageStats(opens, strokes, undos, restarts, hints, seconds, null);
        }

        public void Opened(string stage) => Count(stage, e => e.Opens++);
        public void Stroked(string stage) => Count(stage, e => e.Strokes++);
        public void Undid(string stage) => Count(stage, e => e.Undos++);
        public void Restarted(string stage) => Count(stage, e => e.Restarts++);
        public void Hinted(string stage) => Count(stage, e => e.Hints++);

        public void AddTime(string stage, double seconds)
        {
            if (seconds <= 0) return;
            Entry e = Open(stage);
            if (e != null) e.Seconds += seconds;
        }

        public void Cleared(string stage, int moves) => Count(stage, e => e.ClearMoves = moves);

        // 한 줄에 한 스테이지: 파일 이름 \t 열기 \t 둔 수 \t 되돌리기 \t 처음부터 \t 힌트 \t 초 \t 클리어 수(없으면 -)
        public string Serialize()
        {
            var text = new StringBuilder(Header).Append('\n');
            foreach (KeyValuePair<string, Entry> pair in _entries)
            {
                Entry e = pair.Value;
                text.Append(pair.Key).Append(Separator).Append(e.Opens).Append(Separator).Append(e.Strokes).Append(Separator)
                    .Append(e.Undos).Append(Separator).Append(e.Restarts).Append(Separator).Append(e.Hints).Append(Separator)
                    .Append(e.Seconds.ToString("0.0", CultureInfo.InvariantCulture)).Append(Separator)
                    .Append(e.ClearMoves.HasValue ? e.ClearMoves.Value.ToString(CultureInfo.InvariantCulture) : NotCleared).Append('\n');
            }
            return text.ToString();
        }

        // 머리줄이 다르거나 비어 있으면 빈 기록, 읽을 수 없는 줄은 건너뛴다. 옛 형식(stats 1 — 힌트 칸 없음)은 힌트 0으로 읽는다.
        // 버린 것이 있으면 status로 알린다(2026-11-19) — 공백뿐인 줄 · 같은 스테이지 줄(뒤 줄이 이김)은 버림이 아니다
        public static PlayStats Deserialize(string text, out RecordReadStatus status)
        {
            var stats = new PlayStats();
            status = RecordHeader.Read(text, HeaderName, Version, Separator, (header, f) =>
            {
                if (!TryReadLine(f, header == Header, out string stage, out Entry entry)) return false;
                stats._entries[stage] = entry;
                return true;
            }, Header, HeaderV1);
            return stats;
        }

        private static bool TryReadLine(string[] f, bool hasHints, out string stage, out Entry entry)
        {
            int fields = hasHints ? 8 : 7;
            stage = f[0];
            entry = null;
            if (f.Length != fields || !Progress.IsValidStage(f[0])) return false;
            if (!TryCount(f[1], out int opens) || !TryCount(f[2], out int strokes) || !TryCount(f[3], out int undos) || !TryCount(f[4], out int restarts)) return false;
            int hints = 0;
            if (hasHints && !TryCount(f[5], out hints)) return false;
            if (!double.TryParse(f[fields - 2], NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) || seconds < 0) return false;
            int? clearMoves = null;
            if (f[fields - 1] != NotCleared)
            {
                if (!TryCount(f[fields - 1], out int moves) || moves < 1) return false;
                clearMoves = moves;
            }
            entry = new Entry { Opens = opens, Strokes = strokes, Undos = undos, Restarts = restarts, Hints = hints, Seconds = seconds, ClearMoves = clearMoves };
            return true;
        }

        private static bool TryCount(string text, out int value) =>
            int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);

        private void Count(string stage, Action<Entry> change)
        {
            Entry e = Open(stage);
            if (e != null) change(e);
        }

        // 기록할 항목 — 이미 클리어한 스테이지면 null(더 세지 않음)
        private Entry Open(string stage)
        {
            Progress.ValidateStage(stage);
            if (!_entries.TryGetValue(stage, out Entry e))
            {
                e = new Entry();
                _entries.Add(stage, e);
            }
            return e.ClearMoves.HasValue ? null : e;
        }
    }
}
