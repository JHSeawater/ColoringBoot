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

        // 다음에 풀 스테이지 — 열려 있고 아직 클리어하지 않은 첫 스테이지. 모두 클리어했으면 -1 (선택 화면에서 강조)
        public int NextStage(IReadOnlyList<string> order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            for (int i = 0; i < order.Count; i++)
            {
                if (!IsCleared(order[i]) && IsUnlocked(order, i)) return i;
            }
            return -1;
        }

        // 챕터 진행 (Phase 8 구조 정리, 2026-10-07) — 챕터 = 스테이지 순서 목록, 그림 단계 i ↔ i번째 스테이지(GDD §5)

        // 클리어한 스테이지 수 = 그림에서 칠한 단계 수
        public int ClearedCount(IReadOnlyList<string> order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            int count = 0;
            foreach (string stage in order)
            {
                if (IsCleared(stage)) count++;
            }
            return count;
        }

        // 챕터 완성(그림 완성): 모든 스테이지를 클리어했다
        public bool IsComplete(IReadOnlyList<string> order) => ClearedCount(order) == order.Count;

        // 칠한 단계: painted[i] = i번째 스테이지를 클리어했는가. 배열은 호출 쪽이 만들어 다시 쓴다(order 길이 이상)
        public void FillPainted(IReadOnlyList<string> order, bool[] painted)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (painted == null || painted.Length < order.Count) throw new ArgumentException("배열이 스테이지 수보다 짧습니다", nameof(painted));
            for (int i = 0; i < order.Count; i++) painted[i] = IsCleared(order[i]);
        }

        // 챕터 사이 해금 (2026-10-07 사용자 결정): 첫 챕터이거나 바로 앞 챕터를 완성했으면 열린다 — 스테이지 차례 해금과 같은 방식
        public bool IsChapterUnlocked(IReadOnlyList<IReadOnlyList<string>> chapters, int index)
        {
            if (chapters == null) throw new ArgumentNullException(nameof(chapters));
            if (index < 0 || index >= chapters.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return index == 0 || IsComplete(chapters[index - 1]);
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
