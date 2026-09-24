# Development Log (개발 일지)

이 문서는 프로젝트의 개발 타임라인, 주요 변경 사항, 마주친 버그와 해결 방법을 기록한다.

## [작성 규칙]
1. 나(AI)는 Task(예: Phase 1)를 완료하거나 중요한 버그를 수정했을 때, 이 문서의 **최상단(이 규칙 바로 아래)**에 새 로그를 추가한다.
2. 각 로그는 **날짜(YYYY-MM-DD), 제목, 작업 내용, 해결된 이슈**를 포함한다.

---

### 📅 [2026-09-24] 작업 환경 구축 — CLAUDE.md 재작성 · Git · 편집 가드 훅 · 스킬

> **다른 AI 세션을 위한 요지**: 이 프로젝트는 Unity CLI 공식 MCP(`unity-editor-mcp`)로 에디터와 연결된다. Labyrinth의 `UnityMCP`(`manage_scene` 등)와 `coplay-mcp`는 쓰지 않는다. `.unity`/`.prefab` 텍스트 편집은 훅이 차단한다(의도된 가드).

#### 1. 배경

Labyrinth(2D 회전 미로) 프로젝트의 CLAUDE.md를 가져와 이 프로젝트(육각 붓 퍼즐, WebGL)에 맞게 다시 썼다. 사용자 결정: TDD.md는 당분간 두지 않음 · Git 초기화 · 편집 가드 훅과 `unity-pipeline` · `/phase-close` 스킬 셋업 · `/qa-scene`은 보드 씬이 생긴 뒤 작성 · Task.md는 Labyrinth 체계(DoD · 태그 · Phase 헤더) 이식.

#### 2. 환경 실측

| 항목 | 결과 |
|---|---|
| Unity | 6000.6.2f1, URP 17.6, Input System 1.20(New 전용), Test Framework 1.8 |
| MCP | `unity-editor-mcp` = Unity CLI 1.0.0-beta.9의 `unity mcp` → `com.unity.pipeline` 0.7.0-exp.1 HTTP 서버. `editor_status` ready, projectPath 일치 |
| `coplay-mcp` | 사용자 레벨 등록, 프로젝트에 Coplay 패키지 없음. `list_unity_project_roots` 응답 120초 초과 → 사용 안 함 |
| 빌드 | 처음엔 WebGL Build Support 미설치(Android · Windows만) → 사용자가 설치, 에디터 재시작 후 `list_build_targets`에서 WebGL `isInstalled: true` 확인. 활성 타깃은 아직 StandaloneWindows64. 설치 직후 Unity가 ProjectSettings에 WebGL용 스크립팅 심볼 `SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY`(AI 패키지가 넣는 심볼, Standalone과 동일)를 자동 추가 |
| 씬 | URP 3D 템플릿 `SampleScene`(Main Camera · Directional Light · Global Volume), 스크립트 0개 |
| 콘솔 | 에러 1건 `Unable to join player connection multicast group (err: 10013)` — Windows 네트워크 권한 관련, 무해 |

#### 3. 작업 내용

* **CLAUDE.md 재작성**: 회전 물리(B방식) · Box2D · 풀링 등 Labyrinth 전용 내용을 삭제. 새 MCP 도구 체계(코드 수정 → `recompile` → `run_tests` 루프, 비동기 폴링, `confirm`/`dry_run`), 게임 규칙 핵심(비트마스크 색 · 막힘 판정식 · 육각 방향 표 · 붓질 의사코드), WebGL 제약, 스킬 표, 자주 하는 실수를 신설. 원본의 이스케이프된 마크다운(`\#`, `\*\*`)도 정상화.
* **Git**: `git init -b main`. `.gitignore`는 Labyrinth 것에서 그 프로젝트 전용 항목(스크린샷 폴더 · `.utmp/`)만 뺀 Unity 표준 + `/.claude/settings.local.json`.
* **편집 가드 훅** (`.claude/settings.json`): Labyrinth 훅을 이식하고 매처에 `mcp__unity-editor-mcp__write_text_file`을 추가(인자 `path`도 검사), 안내 문구를 새 MCP 도구로 교체. 백슬래시를 직접 입력하지 않도록 JSON은 Python으로 생성.
* **스킬**: `unity-pipeline`(패키지 동봉 공식 스킬 사본 + 출처 · MCP 대응 안내 3줄), `/phase-close`(Labyrinth판에서 TDD 점검 · 이월 선례를 빼고, GDD는 승인 후 수정, EditMode 테스트 전체 통과 게이트 추가).
* **사용자 결정 반영**: MCP는 `unity-editor-mcp`만 사용(다른 MCP 불필요) → CLAUDE.md에 명시하고 스킬 표에서 `claude-in-chrome` 삭제. GitHub 원격 `origin` = `JHSeawater/ColoringBoot` 등록 — 원격에 GitHub 초기 커밋 3개(최종 파일 0개)가 있어 그 위에 초기 커밋을 얹었다(강제 push 불필요).
* **push · 사용자 설정**: GitHub에 push(`f3ce685..5aa49dd`). 사용자 설정(`~/.claude/settings.json`)의 자동 모드 환경 설명에 ColoringBoot를 신뢰 저장소로 추가(Labyrinth 항목 유지, JSON 유효성 확인).
* **CLAUDE.md 최종 점검**: 웹 프로토타입(GDD §9)을 열람해 §3 규칙 서술(방향 표 · 같은 줄 판정 · 처리 순서 · 막힘/성공)과 대조 — 일치. 문서 맵에 프로토타입(참조 구현 · 스테이지 코드 9개)과 GDD §5 · §9 · §14 추가. 화면 배치 공식(Unity y축 반전) · 런타임 탐색 상한 · 숫자키 매핑 · 런타임 확인 절차, 자주 하는 실수 3건(y축 부호 · 플레이 모드 중 수정 · Windows 도구 환경) 보강.

#### 4. 검증

* **훅**: Git Bash로 가짜 입력 8종 파이프 테스트 — 차단 4종(`.unity` Windows 경로 · `.prefab` · MCP `write_text_file` · 대문자 `.UNITY`) exit 2, 통과 4종(`.cs` · 내용에 ".unity"가 든 `.md` · `.unity.meta` · 깨진 JSON) exit 0. 이어 실제 세션에서 임시 폴더의 더미 `.unity` Write → 차단 확인(재시작 없이 즉시 작동, 한글 메시지 정상).
  * 주의: Python `subprocess`로 `bash`를 부르면 WSL bash가 잡혀 `node: command not found`(exit 127)가 난다 — 훅 결함이 아니라 테스트 환경 문제. Git Bash 경로를 명시해야 한다.
* **붓질 규칙 서술**: CLAUDE.md §3 규칙(줄 전체 · 반대편 끝 출발 · 빈자리 건너감)으로 포도 스테이지를 시뮬레이션 → 최소 5수, 120가지 순서 중 8가지 성공. GDD §3 수치와 일치.
* **MCP 재시작 직후 시간 초과**: WebGL 모듈 설치로 에디터가 재시작된 뒤 첫 `list_build_targets` MCP 호출이 60초 시간 초과. `editor_status`는 ready · 대화상자 없음, 같은 명령을 CLI로 1.6초, MCP 재시도도 즉시 성공 → 일시적 현상으로 보고 CLAUDE.md §2에 재시도 규칙으로 기록.

* **해결된 이슈**:
  * CLAUDE.md가 다른 프로젝트(Labyrinth) 기준이던 문제 — 이 프로젝트 기준으로 재작성
  * 버전 관리 부재 — Git 초기화
  * 씬/프리팹 텍스트 직접 편집 사고 가능성 — 훅으로 차단(새 MCP의 `write_text_file` 경로 포함)
  * WebGL Build Support 미설치 — 사용자 설치 후 에디터 인식 확인
* **남은 일**: Task.md 작성(Labyrinth 체계 · Phase 0~6 구성 확정됨)
