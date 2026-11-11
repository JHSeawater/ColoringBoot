# AGENTS.md — Codex 작업 진입점

이 프로젝트는 Claude Code와 Codex가 같은 문서와 작업 기록을 사용한다. 이 파일은 Codex가 기존 지침을 찾아 읽도록 연결하는 진입점이며, 프로젝트 규칙의 정본은 `CLAUDE.md`다. 규칙을 여기로 복사하거나 Codex 전용 Task · 개발 일지를 만들지 않는다. 루트의 이 진입점 외 새 프로젝트 문서는 `Docs/`에 둔다.

## 작업 전에 읽을 문서

1. **`CLAUDE.md` 전체를 먼저 읽고 따른다.** 문서 지도, 한국어 출력, 실행 방식(Direct / Inline Plan / Full Plan), 게임 규칙, Unity 검증 절차, Git · 배포 제한이 들어 있다.
2. `Docs/Task.md`의 "지금 상태"와 요청에 해당하는 항목을 읽는다. 실제 작업 현황은 `git status --short`와 대조하고, 기존 미커밋 변경을 보존한다.
3. 기능 작업 전 `Docs/TDD.md`의 해당 절과 `Docs/DECISIONS.md`의 관련 결정을 읽는다. 게임 규칙은 `Docs/GDD.md`가 정본이며, 수정은 기존 승인 절차를 따른다.
4. 아래 경로의 파일을 다루기 전에는 **`.claude/rules/game-code.md`를 직접 읽고 적용한다.** Claude의 경로별 규칙 자동 로딩에 의존하지 않는다.
   - `Assets/Scripts/Game/`
   - `Assets/Scripts/LevelEditor/`
   - `Assets/Data/`
5. 빌더 · QA 스크립트 사용이나 에셋 이동 전에는 `AgentScripts/README.md`, 아트 작업 전에는 `Docs/ArtSpec.md`를 읽는다.

## 기존 스킬 사용

`.claude/skills/`의 문서는 공통 작업 절차로 사용한다. Codex의 스킬 목록이나 슬래시 명령에 자동 등록되었다고 가정하지 않는다. 사용자가 스킬 이름을 부르거나 `CLAUDE.md`의 실행 조건에 해당하면, 아래 파일을 직접 읽고 절차를 수행한다. 실제 제공되지 않는 도구나 명령을 실행한 것처럼 보고하지 않는다.

| 상황 · 이름 | 읽을 파일 |
|---|---|
| 세션 시작 · 재개, `resume` | `.claude/skills/resume/SKILL.md` |
| Full Plan 작성, `/plan` | `.claude/skills/plan/SKILL.md` |
| 새 세션에서 계획 검수, `/review-plan` | `.claude/skills/review-plan/SKILL.md` |
| Unity 에디터 · CLI 조작, `unity-pipeline` | `.claude/skills/unity-pipeline/SKILL.md` |
| 씬 배선 점검, `/qa-scene` | `.claude/skills/qa-scene/SKILL.md` |
| 실행 점검, `/health-check` | `.claude/skills/health-check/SKILL.md` |
| 사용자가 요청한 코드 점검, `/code-audit` | `.claude/skills/code-audit/SKILL.md` |
| Phase 종료, `/phase-close` | `.claude/skills/phase-close/SKILL.md` |

스킬이 Claude 전용 기능(기본 `/code-review`, 플러그인, 서브에이전트 등)을 요구하면 현재 도구로 수행할 수 있는 범위를 확인한다. 다른 방식으로 수행한 검토는 그 방법과 한계를 명시하고, 원래 검증을 통과한 것으로 처리하지 않는다. 읽기 전용 점검 · 사용자 호출 전용 조건 · 계획 승인 게이트는 그대로 지킨다.

## 도구와 안전 규칙

- `.claude/settings.json`의 훅과 `.claude/settings.local.json`의 권한 · 연결 설정이 Codex에도 적용된다고 가정하지 않는다. 문서를 읽는 설정과 실제 도구 연결은 별개다.
- `.unity` · `.prefab`는 텍스트로 직접 편집하지 않는다. `CLAUDE.md`의 빌더와 Unity Pipeline 절차를 사용한다. 훅 유무와 관계없이 같은 규칙을 지킨다.
- Unity 작업은 기존 `unity-editor-mcp` 또는 `unity` CLI를 사용한다. 첫 조회에서 `editor_status.projectPath`가 이 프로젝트인지 확인한다. 도구가 없으면 사용 가능 여부를 확인하고 미연결 사실을 보고한다. 연결하지 못한 상태에서 에디터 · 테스트 · 빌드 성공을 주장하지 않는다.
- `Claude in Chrome` 등 Claude 전용 브라우저 도구는 현재 사용 가능한 실제 브라우저 도구로 대체할 수 있으나, WebGL이 실행되는 브라우저에서 확인한다는 검증 조건은 유지한다.
- 커밋 · push는 사용자 요청이나 승인 범위에서만 한다. 커밋 전에는 `.claude/hooks/ait-settings-guard.js`의 차단 조건도 확인한다. 스테이징된 `ProjectSettings/ProjectSettings.asset`에 `webGLTemplate: PROJECT:AITTemplate`가 남아 있으면 커밋하지 않는다. 작업 트리까지 함께 스테이징하는 경우 해당 파일의 작업 트리 값도 확인한다. 사용자 설정을 일괄 되돌려 해결하지 않는다.
- `.claude`의 설정 · 훅 · 스킬을 바꿀 때는 Claude Code에도 영향을 준다는 점을 고려한다. 도구 연결이나 전역 설정을 문서 연결 작업에 섞어 변경하지 않는다.

## 작업 기록

작업 현황 · 결정 · 검증 근거는 기존 `Docs/Task.md`, `Docs/DECISIONS.md`, `Docs/DevelopLog.md`, `Docs/BuildHistory.md`에 해당 규칙대로 남긴다. Claude와 Codex 사이의 인수인계도 같은 문서를 사용한다. 테스트로 확인한 것, 에디터 · 브라우저에서 본 것, 확인하지 못한 것을 구분한다.
