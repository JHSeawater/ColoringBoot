using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

// 챕터 1 스테이지 ↔ 그림 소재 다시 짝짓기 (2026-10-11 사용자 결정 — Task.md Phase 8 콘텐츠)
// 소재마다 어울리는 스테이지를 챕터 · 시험 목록에서 골라 소재 이름 파일로 옮기고(MoveAsset — GUID 그대로), 빠지는 6개는 시험 목록으로 보낸다.
// 실행: run_script(file=AgentScripts/Setup/Phase8Chapter1Pairing.cs, entry=Phase8Chapter1Pairing.Preview) → Apply. 뒤에 StageOrder.SetLabOrder,
// 챕터 1 StageCatalog에서 시험 폴더로 옮긴 6개를 뺀 다음(SetOrder가 순서에 없는 항목이 있으면 멈춤 — 2026-10-11 eval로 뺌) StageOrder.SetOrder(1)
// 같은 날 고침(사용자 지적 — 7 · 8 · 10 · 11번이 그림과 상관없는 옛 스테이지): 파일 내용만 바꿈(GUID 그대로) — 포도 ↔ 시험 포도 내용 맞바꿈,
// 덩굴 · 언덕 · 하늘 = AgentScripts/Tools/SubjectLab.cs 후보, 옛 내용(엇갈림 · 큰 벌집 · 마지막 한 획)은 Lab/LabCrossing · LabHive · LabLastStroke로
public static class Phase8Chapter1Pairing
{
    private const string Chapter = "Assets/Data/Chapters/Chapter1/Stages";
    private const string Lab = "Assets/Data/Lab/Stages";

    // (옮길 파일, 새 경로, 새 화면 이름 — null이면 그대로)
    private static readonly (string from, string to, string name)[] Moves =
    {
        ($"{Lab}/LabBasket.json", $"{Chapter}/Basket.json", null),
        ($"{Chapter}/Honeycomb.json", $"{Chapter}/Beehive.json", null),
        ($"{Lab}/LabLeaf.json", $"{Chapter}/Leaf.json", null),
        ($"{Lab}/LabSun.json", $"{Chapter}/Sun.json", null),
        ($"{Lab}/LabButterfly.json", $"{Chapter}/Butterfly.json", null),
        ($"{Lab}/LabPath.json", $"{Chapter}/Path.json", null),
        ($"{Chapter}/Crossing.json", $"{Chapter}/Vine.json", "덩굴"),
        ($"{Lab}/LabBee.json", $"{Chapter}/Bee.json", null),
        ($"{Chapter}/Hive.json", $"{Chapter}/Field.json", "언덕"),
        ($"{Chapter}/LastStroke.json", $"{Chapter}/Sky.json", "하늘"),
        // 빠지는 챕터 스테이지 → 시험 목록(챕터 2 이후 재료)
        ($"{Chapter}/TwoColors.json", $"{Lab}/LabTwoColors.json", null),
        ($"{Chapter}/BrushChanges.json", $"{Lab}/LabBrushChanges.json", null),
        ($"{Chapter}/Stain.json", $"{Lab}/LabStain.json", null),
        ($"{Chapter}/MakeBlack.json", $"{Lab}/LabMakeBlack.json", null),
        ($"{Chapter}/BlackWing.json", $"{Lab}/LabBlackWing.json", null),
        ($"{Chapter}/VioletPath.json", $"{Lab}/LabVioletPath.json", null),
    };

    public static string Preview() => string.Join("\n", Moves.Select(m =>
        $"{(File.Exists(m.from) ? "" : "없음! ")}{m.from} → {m.to}{(m.name != null ? $" · 이름 {m.name}" : "")}{(File.Exists(m.to) ? " (대상이 이미 있음!)" : "")}"));

    public static string Apply()
    {
        var log = new StringBuilder();
        foreach ((string from, string to, string name) in Moves)
        {
            string error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error)) return $"중단 — {from}: {error}\n{log}";
            if (name != null)
            {
                string text = File.ReadAllText(to);
                int start = text.IndexOf("\"name\":\"") + 8;
                int end = text.IndexOf('"', start);
                File.WriteAllText(to, text.Substring(0, start) + name + text.Substring(end), new UTF8Encoding(false));
                AssetDatabase.ImportAsset(to);
            }
            log.AppendLine($"{from} → {to}{(name != null ? $" · {name}" : "")}");
        }
        AssetDatabase.SaveAssets();
        return log.ToString();
    }
}
