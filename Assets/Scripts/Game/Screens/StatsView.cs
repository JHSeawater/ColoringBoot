using System;
using System.Collections.Generic;
using System.Text;
using ColoringBoot.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColoringBoot.Game
{
    // 플레이테스트 기록 보기 (주소 ?stats) — 스테이지 목록 순서대로 첫 클리어까지의 기록, 그 아래 따라 하기 · 무한 모드 난이도별 합계(2026-10-07). 사진으로 찍어 모은다(GDD §9)
    public sealed class StatsView : MonoBehaviour
    {
        private const int SecondsPerMinute = 60;

        [SerializeField] private TMP_Text _text;
        [SerializeField] private Button _closeButton;

        private void OnEnable() => _closeButton.onClick.AddListener(Hide);
        private void OnDisable() => _closeButton.onClick.RemoveListener(Hide);

        public void Show(StageCatalog catalog, StageCatalog tutorial, SaveData data, IReadOnlyList<string> endlessKeys, IReadOnlyList<string> endlessNames)
        {
            PlayStats stats = data.Stats;
            var text = new StringBuilder("플레이 기록\n");
            for (int i = 0; i < catalog.Stages.Count; i++) AppendStage(text, i + 1, catalog.Stages[i], stats);
            text.Append("\n따라 하기\n");
            for (int i = 0; i < tutorial.Stages.Count; i++) AppendStage(text, i + 1, tutorial.Stages[i], stats);
            text.Append("\n무한 모드\n");
            for (int i = 0; i < endlessKeys.Count; i++)
            {
                string key = endlessKeys[i];
                StageStats s = stats.Sum("Endless" + key);
                int seconds = (int)s.Seconds;
                text.Append($"\n{endlessNames[i]} — 푼 {data.Endless.Solved(key)} · 건너뜀 {data.Endless.Skipped(key)} · {seconds / SecondsPerMinute}분 {seconds % SecondsPerMinute}초\n");
                text.Append($"    열기 {s.Opens} · 붓질 {s.Strokes} · 되돌리기 {s.Undos} · 처음부터 {s.Restarts} · 힌트 {s.Hints}\n");
            }
            _text.text = text.ToString();
            gameObject.SetActive(true);
        }

        private static void AppendStage(StringBuilder text, int number, TextAsset asset, PlayStats stats)
        {
            string name = asset.name;
            int? minMoves = null;
            try
            {
                Stage stage = Stage.Parse(asset.text);
                name = stage.Name;
                minMoves = stage.MinMoves;
            }
            catch (FormatException)
            {
                // 이름만 파일 이름으로 보여 준다
            }

            StageStats s = stats.Get(asset.name);
            text.Append('\n').Append(number).Append(". ").Append(name).Append(" — ");
            if (s.Opens == 0)
            {
                text.Append("기록 없음\n");
                return;
            }
            text.Append(s.ClearMoves.HasValue ? $"클리어 {s.ClearMoves.Value}수" : "못 풂");
            if (minMoves.HasValue) text.Append($"(최소 {minMoves.Value})");
            int seconds = (int)s.Seconds;
            text.Append($" · {seconds / SecondsPerMinute}분 {seconds % SecondsPerMinute}초\n");
            text.Append($"    열기 {s.Opens} · 붓질 {s.Strokes} · 되돌리기 {s.Undos} · 처음부터 {s.Restarts} · 힌트 {s.Hints}\n");
        }

        private void Hide() => gameObject.SetActive(false);
    }
}
