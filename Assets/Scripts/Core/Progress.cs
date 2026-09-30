using System;
using System.Collections.Generic;
using System.Text;

namespace ColoringBoot.Core
{
    // 스테이지별 진행 기록 — 클리어 여부 · 최고 기록(가장 적게 둔 수) · 차례 해금 · "완벽" (GDD §5, 2026-09-30 사용자 결정).
    // 스테이지는 파일 이름으로 구분한다(표시 이름을 바꿔도 기록이 남는다). 저장은 문자열로 — 어디에 쓸지는 표현 계층이 정한다
    public sealed class Progress
    {
        private const string Header = "progress 1";
        private const char Separator = '\t';

        private readonly Dictionary<string, int> _best = new Dictionary<string, int>(StringComparer.Ordinal);

        public bool IsCleared(string stage) => _best.ContainsKey(stage);

        public int? BestMoves(string stage) => _best.TryGetValue(stage, out int moves) ? moves : (int?)null;

        // 최고 기록이 스테이지의 최소 수와 같으면 "완벽". 최소 수가 없는 스테이지는 완벽 표시가 없다
        public bool IsPerfect(string stage, int? minMoves) => minMoves.HasValue && _best.TryGetValue(stage, out int moves) && moves <= minMoves.Value;

        // 차례 해금: 첫 스테이지이거나 바로 앞 스테이지를 클리어했으면 열린다
        public bool IsUnlocked(IReadOnlyList<string> order, int index)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (index < 0 || index >= order.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return index == 0 || IsCleared(order[index - 1]);
        }

        // 클리어 기록. 처음 클리어했거나 더 적은 수로 풀었으면 true
        public bool RecordClear(string stage, int moves)
        {
            ValidateStage(stage);
            if (moves < 1) throw new ArgumentOutOfRangeException(nameof(moves));
            if (_best.TryGetValue(stage, out int best) && best <= moves) return false;
            _best[stage] = moves;
            return true;
        }

        // 한 줄에 한 스테이지: 파일 이름 \t 최고 기록
        public string Serialize()
        {
            var text = new StringBuilder(Header).Append('\n');
            foreach (KeyValuePair<string, int> entry in _best) text.Append(entry.Key).Append(Separator).Append(entry.Value).Append('\n');
            return text.ToString();
        }

        // 머리줄이 다르거나 비어 있으면 빈 진행, 읽을 수 없는 줄은 건너뛴다 — 저장 값이 깨져도 게임은 시작한다
        public static Progress Deserialize(string text)
        {
            var progress = new Progress();
            string[] lines = (text ?? "").Split('\n');
            if (lines[0].Trim() != Header) return progress;
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Trim().Split(Separator);
                if (fields.Length != 2 || !IsValidStage(fields[0]) || !int.TryParse(fields[1], out int moves) || moves < 1) continue;
                progress.RecordClear(fields[0], moves);
            }
            return progress;
        }

        internal static bool IsValidStage(string stage)
        {
            if (string.IsNullOrEmpty(stage)) return false;
            foreach (char c in stage)
            {
                if (char.IsWhiteSpace(c) || char.IsControl(c)) return false;
            }
            return true;
        }

        internal static void ValidateStage(string stage)
        {
            if (!IsValidStage(stage)) throw new ArgumentException($"스테이지 이름이 비었거나 공백이 있습니다: '{stage}'", nameof(stage));
        }
    }
}
