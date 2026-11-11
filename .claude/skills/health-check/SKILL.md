---
name: health-check
description: 프로젝트가 지금 실제로 돌아가는지 한 번에 점검한다(읽기 전용) — 컴파일 · 콘솔 에러 → EditMode 테스트 전체(금지 패턴 SourceRuleTests · 회귀 테스트 포함) → 보드 씬 점검(QaScene) → 에디터 플레이 핵심 흐름(BoardQa). 작업을 마칠 때 · 빌드 전 · 세션 끝에 쓴다. `quick`이면 플레이 흐름을 뺀다. 코드 품질 리뷰는 `/code-audit`.
argument-hint: "[quick]"
---

# /health-check — 실행 점검 (읽기 전용)

인자: $ARGUMENTS (`quick`이면 4단계를 건너뛴다)

**아무것도 고치지 않는다.** 실패는 보고만 하고, 수정은 사용자가 고른 뒤 따로 한다. 단계마다 결과를 기록하고, 앞 단계가 실패해도 뒤 단계가 의미 있으면 계속한다(컴파일 실패만 예외 — 그 뒤는 모두 "확인 못 함").

## 0. 전제
- `editor_status`: `ready` · `playMode: stopped` · `projectPath`가 이 프로젝트. Play Mode면 멈추라고 알리고 중단. `blocked_by_dialog`면 대화상자 내용을 알리고 중단(CLAUDE.md §3).
- 테스트와 씬 조회 · 플레이를 동시에 하지 않는다(테스트 러너의 임시 씬이 잡힘).

## 1. 컴파일 · 콘솔
1. `recompile` → `recompile_status`가 `completed`/`up_to_date`가 될 때까지 폴링 → `editor_status`가 `ready`인지.
2. `console_status`: `compilationFailed` · `groundTruth.consoleErrors`. 에러가 있으면 `console`(level=error)로 읽고, 수가 다르면 `run_script`(file=`AgentScripts/QA/ConsoleDump.cs`, entry=`ConsoleDump.Errors`). 무시할 로그: `Unable to join player connection multicast group (err: 10013)`.
3. 컴파일 실패면 여기서 멈추고 에러 원문을 보고한다.

## 2. EditMode 테스트 전체
- `run_tests`(mode=editor, async_tests=true) → 끝날 때까지 `test_status`.
- 보고: 전체 · 통과 · 실패 수, 실패마다 테스트 이름 · 메시지 첫 줄. 특히 확인할 것 — `SourceRuleTests`(런타임 코드 금지 패턴 — 스레드 · `System.IO` · `PlayerPrefs` 직접 호출 · `LoadScene`), `GrapeRegressionTests` · `PrototypeStageRegressionTests` · `ContentRegressionTests`(규칙 · 콘텐츠 회귀).

## 3. 보드 씬 점검
- `list_open_scenes`로 활성 씬이 `Assets/Scenes/Board.unity`인지 본다. 아니면 이 단계와 4단계를 "확인 못 함(활성 씬이 다름)"으로 적는다 — 씬을 바꿔 열지 않는다(저장 안 된 변경이 있을 수 있음).
- `run_script`(file=`AgentScripts/QA/QaScene.cs`, entry=`QaScene.Check`) — 참조 누락 · 폰트 글자 · 팔레트 · 스테이지가 풀리는지 · 그림 단계 수. 씬이 `isDirty`면 함께 보고.
- 계층까지 하나하나 보려면 `/qa-scene`을 따로 쓴다(이 스킬은 `QaScene.Check` 결과만).

## 4. 에디터 플레이 핵심 흐름 (`quick`이면 건너뜀)
함수와 순서는 `AgentScripts/README.md` "BoardQa 사용 순서", 요령은 CLAUDE.md §3 "에디터 플레이 확인". 각 호출은 `run_script`(file=`AgentScripts/QA/BoardQa.cs`)로 **따로** 부른다(같은 호출에서 열고 끌면 칸 배치 전이라 엉뚱한 칸에 그어진다).
1. 시작 전에 저장 키를 지워 같은 조건에서 시작한다 — `eval`: `ColoringBoot.Progress` · `ColoringBoot.PlayStats` · `ColoringBoot.Symbols` · `ColoringBoot.Sound` · `ColoringBoot.Tutorial` · `ColoringBoot.Endless`를 `PlayerPrefs.DeleteKey` → `PlayerPrefs.Save()`.
2. `editor_play` → `eval`로 `Application.runInBackground = true` → `Time.frameCount`가 느는지(안 늘면 `set_autotick` enable=true).
3. `Flow` — 타이틀 화면인지 → `Press("TitleScreen/StartButton")` → `Flow`(저장 키가 없으니 따라 하기 1이 열려야 한다) → `Press("BoardScreen/BackButton")` → `Flow`(선택 화면 · 1번만 열림).
4. `ChooseStage(1)` → (다음 호출) `State` → `SolveByDrag` → 약 1.5초 뒤 `State`(클리어 · 수 = 최소 수) → `Press("BoardScreen/ClearBanner/NextButton")` → `Flow`(다음 스테이지 또는 그림 화면).
5. 열린 판에서 `Undo` · `Restart`가 에러 없이 도는지 `State`로 확인.
6. `console`(level=error)로 플레이 중 에러 → `editor_stop` → 1과 같은 키를 다시 지운다 → `editor_status`로 멈췄는지, 활성 씬이 그대로인지.
- 단계 결과를 출력 문구로 판정할 수 없으면 "확인 못 함"으로 적는다. 화면 판단이 필요하면 `capture_game_view`(source=screen)를 쓰되 `save_path`는 쓰지 않는다.

## 5. 보고
1. 요약 한 줄: `통과 N / 실패 N / 확인 못 함 N` (quick이면 그렇게 적는다).
2. 표: 단계 · 결과(통과 / 실패 / 확인 못 함) · 근거(수치 · 메시지 원문).
3. **오류**: 실패 항목마다 원문 · 추정 원인(추정이라고 밝힌다) · 고칠 곳 후보. 실행하지 않는다.
4. **확인 못 한 것**: 항상 적는다 — WebGL 빌드 · 휴대폰 실기 · 키보드 입력 · 첫 로딩 · 재미와 그림 판단은 이 점검 범위 밖(CLAUDE.md §6).
5. 조치할 항목이 있으면 `Docs/Task.md`로 옮길지 묻는다(보고서를 할 일 목록으로 쓰지 않는다 — CLAUDE.md §0).

## 하지 말 것
- 코드 · 씬 · 에셋 · 설정 수정, 빌드, 커밋.
- 실패한 테스트를 다시 돌려 통과한 결과만 보고하기. 확인 못 한 단계를 통과로 적기.
