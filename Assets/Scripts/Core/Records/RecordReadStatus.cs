using System;
using System.Globalization;

namespace ColoringBoot.Core
{
    // 저장 값을 읽은 결과 (2026-11-19, Docs/plans/2026-11-18-save-read-failure.md) — 버린 것이 있는지 알린다.
    // 로그 · 원본 백업 · 쓰기 막기는 표현 계층(SaveData)이 정한다
    public enum RecordReadStatus
    {
        Ok,            // 정상 — 값 없음 · 빈 값(첫 실행) 포함
        LinesDropped,  // 머리줄은 맞고 읽을 수 없는 줄을 건너뜀(공백뿐인 줄은 세지 않음)
        NewerVersion,  // 머리줄 이름은 같고 번호가 이 빌드가 아는 것보다 큼 — 새 빌드가 쓴 값
        Unreadable,    // 값이 있는데 머리줄을 모름
    }

    internal static class RecordHeader
    {
        // 머리줄 = "이름 번호"("progress 1"). 각 기록은 이름 · 번호를 상수로 두고 이것으로 머리줄을 만든다
        public static string Make(string name, int version) =>
            name + " " + version.ToString(CultureInfo.InvariantCulture);

        // 저장 값 읽기 공용 뼈대(세 기록이 같이 씀). 공백뿐인 값은 Ok(첫 실행 — 버릴 기록이 없다). 첫 줄이 headers에 없으면 줄을 읽지 않고 Unknown으로 판정.
        // 있으면 다음 줄들을 separator로 나눠 readLine(머리줄, 칸들)에 넘긴다 — 공백뿐인 줄은 건너뛰고, false를 돌려준 줄은 버린 것(LinesDropped)
        public static RecordReadStatus Read(string text, string name, int version, char separator, Func<string, string[], bool> readLine, params string[] headers)
        {
            if (string.IsNullOrWhiteSpace(text)) return RecordReadStatus.Ok;
            string[] lines = text.Split('\n');
            string header = lines[0].Trim();
            if (Array.IndexOf(headers, header) < 0) return Unknown(header, name, version);
            var status = RecordReadStatus.Ok;
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) continue;
                if (!readLine(header, line.Split(separator))) status = RecordReadStatus.LinesDropped;
            }
            return status;
        }

        // 모르는 머리줄의 상태. name · known은 이 빌드가 쓰는 머리줄의 이름과 번호 — 이름이 같고 번호가 정수로 더 크면 새 버전, 그 밖은 못 읽음.
        // 넓게 잡으면 깨진 값에서도 저장이 막히므로 좁게 본다
        private static RecordReadStatus Unknown(string header, string name, int known)
        {
            string prefix = name + " ";
            return header.StartsWith(prefix, StringComparison.Ordinal)
                && int.TryParse(header.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int version)
                && version > known
                ? RecordReadStatus.NewerVersion
                : RecordReadStatus.Unreadable;
        }
    }
}
