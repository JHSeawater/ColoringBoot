using System.Globalization;
using System.Text;

namespace ColoringBoot.Core
{
    // 스테이지 → 스테이지 코드(JSON). 프로토타입 · Stage.Parse와 같은 형식, 필드 순서 name · cells · palette · minMoves (한 줄)
    public static class StageWriter
    {
        public static string ToJson(Stage stage)
        {
            var json = new StringBuilder();
            json.Append("{\"name\":");
            AppendString(json, stage.Name);
            json.Append(",\"cells\":[");
            for (int i = 0; i < stage.Cells.Count; i++)
            {
                StageCell cell = stage.Cells[i];
                if (i > 0) json.Append(',');
                json.Append('[').Append(cell.Coord.Q).Append(',').Append(cell.Coord.R).Append(',')
                    .Append((int)cell.Start).Append(',').Append((int)cell.Target).Append(']');
            }
            json.Append(']');
            if (stage.Palette != null)
            {
                json.Append(",\"palette\":");
                AppendString(json, stage.Palette);
            }
            if (stage.MinMoves.HasValue) json.Append(",\"minMoves\":").Append(stage.MinMoves.Value);
            json.Append('}');
            return json.ToString();
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
