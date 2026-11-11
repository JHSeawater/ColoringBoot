using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace ColoringBoot.Core.Tests
{
    // 런타임 코드(Core · Game) 금지 패턴 — CLAUDE.md §5 · §6 (2026-11-11)
    // Core의 UnityEngine 참조는 asmdef(noEngineReferences)가 막으므로 여기서 보지 않는다.
    // LevelEditor는 에디터 전용이라 System.IO로 스테이지 파일을 저장한다 — 범위 밖
    public class SourceRuleTests
    {
        private static readonly string[] Folders = { "Scripts/Core", "Scripts/Game" };

        private static readonly (string Rule, Regex Pattern, string AllowedFile)[] Rules =
        {
            ("스레드 금지(WebGL 단일 스레드)", new Regex(@"\b(Thread|ThreadPool)\b|\bTask\.Run\b|\bParallel\."), null),
            ("System.IO 금지(저장은 IKeyValueStore)", new Regex(@"\bSystem\.IO\b"), null),
            ("PlayerPrefs 직접 호출 금지(IKeyValueStore 뒤로)", new Regex(@"\bPlayerPrefs\b"), "PlayerPrefsStore.cs"),
            ("LoadScene 금지(화면 전환은 GameFlow)", new Regex(@"\bLoadScene"), null),
        };

        [Test]
        public void RuntimeCode_HasNoForbiddenPatterns()
        {
            var violations = new List<string>();
            foreach (string folder in Folders)
            foreach (string path in Directory.GetFiles(Path.Combine(Application.dataPath, folder), "*.cs", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string code = StripComment(lines[i]);
                    foreach (var rule in Rules)
                        if (rule.Pattern.IsMatch(code) && Path.GetFileName(path) != rule.AllowedFile)
                            violations.Add($"{path.Substring(Application.dataPath.Length + 1).Replace('\\', '/')}:{i + 1} {rule.Rule} — {lines[i].Trim()}");
                }
            }

            Assert.IsEmpty(violations, string.Join("\n", violations));
        }

        // 줄 주석을 뗀다. 문자열 안의 "//" 뒤도 잘리지만 놓치는 쪽이라 오탐은 생기지 않는다
        private static string StripComment(string line)
        {
            int index = line.IndexOf("//");
            return index < 0 ? line : line.Substring(0, index);
        }
    }
}
