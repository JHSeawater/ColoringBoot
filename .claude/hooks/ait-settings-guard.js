// PreToolUse(Bash) 훅 — 앱인토스 SDK 빌드 값이 남은 ProjectSettings.asset의 커밋을 막는다 (CLAUDE.md §4).
// SDK 빌드는 디스크에 AITTemplate · 메모리 1024 · 디버그 심볼 등을 남긴다(Assets/Editor/AitSettingsCleanup.cs가 보통 정리한다).
// git commit 명령이면 스테이징된 파일을, 같은 명령에 git add · commit -a가 섞여 있으면 작업 트리 파일도 본다.
const { execFileSync } = require("child_process");
const fs = require("fs");
const path = require("path");

const FILE = "ProjectSettings/ProjectSettings.asset";
const MARK = "webGLTemplate: PROJECT:AITTemplate";

let input = "";
process.stdin.on("data", (c) => (input += c)).on("end", () => {
    let command = "";
    try {
        command = (JSON.parse(input).tool_input || {}).command || "";
    } catch (e) {
        return;
    }
    if (!/\bgit\b[^\n]*\bcommit\b/.test(command)) return;

    const root = process.env.CLAUDE_PROJECT_DIR || process.cwd();
    let found = "";
    try {
        const staged = execFileSync("git", ["show", ":" + FILE], { cwd: root, encoding: "utf8", stdio: ["ignore", "pipe", "ignore"] });
        if (staged.includes(MARK)) found = "스테이징된";
    } catch (e) {
        // 추적하지 않거나 git이 없으면 판단하지 않는다
    }
    const addsWorkTree = /\bgit\b[^\n]*\badd\b/.test(command) || /\bcommit\b[^\n]*\s(-[a-zA-Z]*a[a-zA-Z]*|--all)\b/.test(command);
    if (!found && addsWorkTree) {
        try {
            if (fs.readFileSync(path.join(root, FILE), "utf8").includes(MARK)) found = "작업 트리의";
        } catch (e) {
            // 파일이 없으면 판단하지 않는다
        }
    }
    if (!found) return;
    console.error(
        "BLOCKED: " + found + " " + FILE + "에 앱인토스 SDK 빌드 값(" + MARK + ")이 남아 있습니다 — 커밋하지 않습니다(CLAUDE.md §4).\n" +
        "git checkout -- " + FILE + " 뒤 에디터에서 PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, true)를 하고 다시 커밋하세요(스테이징했다면 git restore --staged " + FILE + " 먼저)."
    );
    process.exit(2);
});
