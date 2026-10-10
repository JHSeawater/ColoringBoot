using System.Globalization;
using System.Text;

namespace ColoringBoot.Core
{
    // 스테이지 → 스테이지 코드(JSON). 프로토타입 · Stage.Parse와 같은 형식, 필드 순서 name · cells · walls · water · coated · palette · minMoves (한 줄).
    // 기믹 칸 필드는 그 칸이 있을 때만 쓴다 — 기믹 없는 스테이지는 프로토타입 형식 그대로
    public static class StageWriter
    {
        public static string ToJson(Stage stage)
        {
            var json = new StringBuilder();
            json.Append("{\"name\":");
            AppendString(json, stage.Name);
            json.Append(",\"cells\":[");
            bool first = true;
            foreach (StageCell cell in stage.Cells)
            {
                if (cell.Kind == CellKind.Wall || cell.Kind == CellKind.Water) continue;
                if (!first) json.Append(',');
                first = false;
                json.Append('[').Append(cell.Coord.Q).Append(',').Append(cell.Coord.R).Append(',')
                    .Append((int)cell.Start).Append(',').Append((int)cell.Target).Append(']');
            }
            json.Append(']');
            AppendCoords(json, stage, CellKind.Wall, "walls");
            AppendCoords(json, stage, CellKind.Water, "water");
            AppendCoords(json, stage, CellKind.Coated, "coated");
            if (stage.Palette != null)
            {
                json.Append(",\"palette\":");
                AppendString(json, stage.Palette);
            }
            if (stage.MinMoves.HasValue) json.Append(",\"minMoves\":").Append(stage.MinMoves.Value);
            json.Append('}');
            return json.ToString();
        }

        // ,"key":[[q,r],...] — 그 종류의 칸이 없으면 쓰지 않는다
        private static void AppendCoords(StringBuilder json, Stage stage, CellKind kind, string key)
        {
            bool first = true;
            foreach (StageCell cell in stage.Cells)
            {
                if (cell.Kind != kind) continue;
                json.Append(first ? $",\"{key}\":[" : ",");
                first = false;
                json.Append('[').Append(cell.Coord.Q).Append(',').Append(cell.Coord.R).Append(']');
            }
            if (!first) json.Append(']');
        }

        private static void AppendString(StringBuilder json, string text)
        {
            json.Append('"');
            foreach (char c in text)
            {
                if (c == '"' || c == '\\') json.Append('\\').Append(c);
                else if (c < ' ') json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else json.Append(c);
            }
            json.Append('"');
        }
    }
}
