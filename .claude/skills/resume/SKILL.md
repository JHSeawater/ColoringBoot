---
name: resume
description: 세션을 시작하거나 쉬었다가 다시 이어 갈 때 지금 상태와 다음 할 일을 10줄 이내로 정리한다(읽기 전용). "어디까지 했지", "이어서 하자", 새 세션의 첫 질문에 쓴다. 이미 진행 중인 작업 도중에는 쓰지 않는다.
---

# resume — 지금 상태 확인 (읽기 전용)

## 절차
1. `Docs/Task.md` "지금 상태"와 현재 Phase의 미완(`[ ]`) 항목을 읽는다.
2. `git status --short` · `git branch --show-current` · `git log --oneline -5`.
3. `Docs/DevelopLog.md` 최상단 기록 1~2개, `Docs/DECISIONS.md` 최신 항목과 미결 항목.
4. `Docs/BuildHistory.md` 표의 마지막 줄(지금 배포본).
5. MCP `editor_status`(연결 · `projectPath`가 이 프로젝트인지)와 `console_status`(컴파일 오류). 연결이 안 되면 그 사실만 적는다.
6. 기록과 실제를 대조한다: "지금 상태"의 마지막 작업이 최근 커밋 · DevelopLog와 맞는지, 커밋 안 된 변경이 있는지(누가 만든 변경인지 모르면 모른다고), 최근 `[x]` 항목에 근거가 있는지.

## 하지 말 것
- 파일 수정 · 커밋 · 테스트 · 빌드 · 에디터 플레이.
- 기록에 없는 것을 추정해 사실처럼 쓰기. 다음 작업을 스스로 시작하기.

## 결과 형식
사람 말로 10줄 이내:
- 지금 상태 — 마지막 작업 · 막힌 곳 · 사용자가 할 일
- 기록과 실제가 어긋난 점(없으면 "없음")
- 다음 할 일 3개 — `Docs/Task.md` 순서 기준, 각각 예상 실행 방식(Direct / Inline Plan / Full Plan)

## 다음 단계
- 사용자가 고른 일부터 시작한다.
