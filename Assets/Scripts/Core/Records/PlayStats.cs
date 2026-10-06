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
        private const string Header = "stats 2";     // 2026-10-07: 힌트 횟수 칸 추가
        private const string HeaderV1 = "stats 1";   // 옛 형식(힌트 칸 없음)도 읽는다
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

        // 머리줄이 다르거나 비어 있으면 빈 기록, 읽을 수 없는 줄은 건너뛴다. 옛 형식(stats 1 — 힌트 칸 없음)은 힌트 0으로 읽는다
        public static PlayStats Deserialize(string text)
        {
            var stats = new PlayStats();
            string[] lines = (text ?? "").Split('\n');
            string header = lines[0].Trim();
            if (header != Header && header != HeaderV1) return stats;
            bool hasHints = header == Header;
            int fields = hasHints ? 8 : 7;
            for (int i = 1; i < lines.Length; i++)
            {
                string[] f = lines[i].Trim().Split(Separator);
                if (f.Length != fields || !Progress.IsValidStage(f[0])) continue;
                if (!TryCount(f[1], out int opens) || !TryCount(f[2], out int strokes) || !TryCount(f[3], out int undos) || !TryCount(f[4], out int restarts)) continue;
                int hints = 0;
                if (hasHints && !TryCount(f[5], out hints)) continue;
                if (!double.TryParse(f[fields - 2], NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) || seconds < 0) continue;
                int? clearMoves = null;
                if (f[fields - 1] != NotCleared)
                {
                    if (!TryCount(f[fields - 1], out int moves) || moves < 1) continue;
                    clearMoves = moves;
                }
                stats._entries[f[0]] = new Entry { Opens = opens, Strokes = strokes, Undos = undos, Restarts = restarts, Hints = hints, Seconds = seconds, ClearMoves = clearMoves };
            }
            return stats;
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
