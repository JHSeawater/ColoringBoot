using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

// 에디터 Console 창의 에러 항목을 읽는다 (진단용 run_script 빌더).
// MCP console 버퍼가 도메인 리로드 중 놓친 항목까지 본다 — console_status의 groundTruth.consoleErrors와 버퍼 수가 다를 때 사용.
// 사용: run_script file=AgentScripts/ConsoleDump.cs entry=ConsoleDump.Errors
public static class ConsoleDump
{
    // LogEntry.mode 중 에러 계열 비트: Error · Assert · Fatal · AssetImportError · ScriptingError · ScriptCompileError · GraphCompileError · ScriptingException · ScriptingAssertion
    const int ErrorMask = 1 | 2 | 16 | 64 | 256 | 2048 | 1048576 | 131072 | 2097152;
    const char NewLine = (char)10;

    public static string Errors()
    {
        var editorAsm = typeof(UnityEditor.Editor).Assembly;
        var logEntries = editorAsm.GetType("UnityEditor.LogEntries");
        var logEntryType = editorAsm.GetType("UnityEditor.LogEntry");
        var getEntry = logEntries?.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
        var messageField = logEntryType?.GetField("message");
        var modeField = logEntryType?.GetField("mode");
        if (getEntry == null || messageField == null || modeField == null)
            return "LogEntries reflection failed (Unity internal API changed)";

        var entry = Activator.CreateInstance(logEntryType);
        var counts = new Dictionary<string, int>();
        int total = (int)logEntries.GetMethod("StartGettingEntries").Invoke(null, null);
        try
        {
            for (int i = 0; i < total; i++)
            {
                getEntry.Invoke(null, new object[] { i, entry });
                if (((int)modeField.GetValue(entry) & ErrorMask) == 0) continue;
                string first = ((string)messageField.GetValue(entry) ?? "").Split(NewLine)[0];
                counts[first] = counts.TryGetValue(first, out int n) ? n + 1 : 1;
            }
        }
        finally
        {
            logEntries.GetMethod("EndGettingEntries").Invoke(null, null);
        }

        var sb = new StringBuilder($"entries={total}, errors={counts.Values.Sum()}, distinct={counts.Count}");
        foreach (var kv in counts.OrderByDescending(k => k.Value))
            sb.Append(NewLine).Append($"[{kv.Value}x] {kv.Key}");
        return sb.ToString();
    }
}
