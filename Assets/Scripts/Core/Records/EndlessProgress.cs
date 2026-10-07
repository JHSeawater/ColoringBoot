using System;
using System.Collections.Generic;
using System.Text;

namespace ColoringBoot.Core
{
    // 무한 모드 진행 (GDD §5, 2026-10-07) — 난이도마다 지금 퍼즐 번호 · 푼 수 · 건너뛴 수.
    // 퍼즐은 묶음 순서대로 내고, 끝까지 가면 처음으로 돌아간다. 저장은 문자열로 — 어디에 쓸지는 표현 계층이 정한다
    public sealed class EndlessProgress
    {
        private const string Header = "endless 1";
        private const char Separator = '\t';

        private sealed class Entry
        {
            public int Current, Solved, Skipped;
        }

        private readonly Dictionary<string, Entry> _tiers = new Dictionary<string, Entry>(StringComparer.Ordinal);

        public int Current(string tier) => _tiers.TryGetValue(tier, out Entry e) ? e.Current : 0;
        public int Solved(string tier) => _tiers.TryGetValue(tier, out Entry e) ? e.Solved : 0;
        public int Skipped(string tier) => _tiers.TryGetValue(tier, out Entry e) ? e.Skipped : 0;

        // 지금 퍼즐을 풀었다 → 다음 퍼즐로
        public void Solve(string tier, int poolSize) => Advance(tier, poolSize).Solved++;

        // 지금 퍼즐을 건너뛴다 → 다음 퍼즐로
        public void Skip(string tier, int poolSize) => Advance(tier, poolSize).Skipped++;

        private Entry Advance(string tier, int poolSize)
        {
            Progress.ValidateStage(tier);
            if (poolSize < 1) throw new ArgumentOutOfRangeException(nameof(poolSize));
            if (!_tiers.TryGetValue(tier, out Entry e))
            {
                e = new Entry();
                _tiers.Add(tier, e);
            }
            e.Current = (e.Current + 1) % poolSize;
            return e;
        }

        // 한 줄에 한 난이도: 이름 \t 지금 번호 \t 푼 수 \t 건너뛴 수
        public string Serialize()
        {
            var text = new StringBuilder(Header).Append('\n');
            foreach (KeyValuePair<string, Entry> t in _tiers)
                text.Append(t.Key).Append(Separator).Append(t.Value.Current).Append(Separator).Append(t.Value.Solved).Append(Separator).Append(t.Value.Skipped).Append('\n');
            return text.ToString();
        }

        // 머리줄이 다르거나 비어 있으면 빈 진행, 읽을 수 없는 줄은 건너뛴다
        public static EndlessProgress Deserialize(string text)
        {
            var progress = new EndlessProgress();
            string[] lines = (text ?? "").Split('\n');
            if (lines[0].Trim() != Header) return progress;
            for (int i = 1; i < lines.Length; i++)
            {
                string[] f = lines[i].Trim().Split(Separator);
                if (f.Length != 4 || !Progress.IsValidStage(f[0])) continue;
                if (!int.TryParse(f[1], out int current) || !int.TryParse(f[2], out int solved) || !int.TryParse(f[3], out int skipped)) continue;
                if (current < 0 || solved < 0 || skipped < 0) continue;
                progress._tiers[f[0]] = new Entry { Current = current, Solved = solved, Skipped = skipped };
            }
            return progress;
        }
    }
}
