# 저장 값을 읽지 못했을 때 조용히 지우지 않기

> 작성 2026-11-18 · 상태: 검수 1라운드 반영(2026-11-19) · 승인(2026-11-19 "계획대로 작업을 시작하라") · 구현 0~4단계 완료(2026-11-19 — DevelopLog) · 방식: Full Plan(저장 처리 — `CLAUDE.md` §2 · §5 "저장 형식")
> 근거: `Docs/Task.md` 155 · `/code-audit all`(2026-11-12) 지적

## 원래 요청(그대로) · 목표

- `Task.md` 155: "저장 값을 읽지 못하면 조용히 비우고 다음 저장에서 덮어쓴다 — `Progress` · `PlayStats` · `EndlessProgress.Deserialize`가 머리줄이 다르거나 깨진 값이면 빈 기록을 돌려주고 `SaveData`는 알 수 없다(예: 새 빌드의 `progress 2`를 캐시된 옛 빌드가 읽으면 진행이 지워짐). 버렸다는 표시를 돌려주고 경고 로그 · 원본 보존. 저장 처리라 착수 때 방식(Inline · Full Plan)을 정한다"
- 사용자: (다음 할 일 추천 "155 저장 값 처리 — `/plan`으로 계획부터") "진행하라"
- 목표: 저장 값을 읽다가 버린 것이 있으면 (1) 게임은 지금처럼 시작하되 (2) 버렸다는 사실이 로그로 드러나고 (3) 원래 값이 사라지지 않는다. 정상 값 · 첫 실행(값 없음)의 동작은 지금과 같다.

## 지금 구조(확인한 것)

- `SaveData`(`Assets/Scripts/Game/Platform/SaveData.cs`) 생성자가 키 셋을 읽는다: `ColoringBoot.Progress` → `Progress.Deserialize` · `ColoringBoot.PlayStats` → `PlayStats.Deserialize` · `ColoringBoot.Endless` → `EndlessProgress.Deserialize`. 생성은 `GameFlow` 93줄 한 곳(`new SaveData(new PlayerPrefsStore())`).
- 세 `Deserialize`(Core `Records/`)는 모두 같은 꼴이다: 값이 `null` · 빈 문자열 · 머리줄이 다르면 빈 기록, 머리줄이 맞으면 줄마다 읽고 읽을 수 없는 줄은 건너뛴다. 버렸는지는 돌려주지 않는다.
  - 머리줄: `progress 1` · `stats 2`(옛 `stats 1`도 읽음) · `endless 1`.
  - `Serialize`는 마지막 줄 뒤에도 `\n`을 붙인다 → 읽을 때 끝에 빈 줄 하나가 생긴다(지금은 필드 수가 안 맞아 조용히 건너뜀).
- 저장은 덮어쓰기다: 클리어 때 `SaveProgress` · 붓질 등 기록 때 `SaveStats` · 무한 모드 판을 넘길 때 `SaveEndless`가 메모리의 기록 전체를 그 키에 쓴다. 그래서 읽을 때 버린 내용은 다음 저장에서 영영 사라진다.
- `?reset`(`SaveData.ClearRecords`)은 진행 · 기록 · 따라 하기 · 무한 모드 키를 지운다(설정은 남김).
- 테스트 어셈블리(`ColoringBoot.Core.Tests`)는 지금 Core만 참조한다 — `SaveData`(Game)는 EditMode 테스트로 직접 못 부른다. 이 계획에서 `ColoringBoot.Game` 참조를 더한다(검수 지적 1 · 사용자 2026-11-19).
- `SaveData` 생성(`GameFlow.cs:93`, Awake)과 `?reset`의 `ClearRecords`(`GameFlow.cs:223`, Start)는 같은 실행 안이다.
- `/health-check`는 시작할 때 저장 키 6개를 지운다(`.claude/skills/health-check/SKILL.md:33`).
- 기존 테스트: `ProgressTests.Deserialize_UnreadableText_GivesEmptyProgress` · `Deserialize_SkipsBrokenLines` · `PlayStatsTests.Deserialize_BrokenText_SkipsOrEmpties` · `EndlessTests`(garbage · 깨진 줄) — 지금의 "빈 기록 · 줄 건너뛰기" 결과를 본다. 이 결과는 바꾸지 않는다.

## 요구사항

1. 세 `Deserialize`가 읽은 결과와 함께 상태를 돌려준다: **정상**(값 없음 · 빈 값 포함 — 첫 실행) / **일부 줄 버림**(머리줄은 맞고 읽을 수 없는 줄이 있음 — 공백뿐인 줄은 어디에 있든 세지 않는다, 검수 지적 5) / **못 읽음**(값이 있는데 머리줄을 모름).
2. "못 읽음"은 둘로 나눈다(물을 것 1): **새 버전**(머리줄 이름은 같고 번호가 지금이 아는 것보다 큼 — 예 `progress 2`) / **깨짐**(그 밖).
3. 읽은 기록 · 건너뛰는 줄은 지금과 같다(기존 테스트 그대로 통과).
4. `SaveData`는 정상이 아니면 키 · 상태를 넣어 `Debug.LogWarning`을 1회 남긴다(Core는 로그를 남기지 않는다 — `CLAUDE.md` §5).
5. "깨짐" · "일부 줄 버림"이면(다음 저장이 원본을 덮는 두 경우) 원래 값을 백업 키(`<키>.Unread`)에 복사한다. 이미 백업이 있으면 덮어쓰지 않는다(처음 버린 원본을 지킨다). "새 버전"은 백업하지 않는다 — 그 키에 쓰지 않아 원본이 그대로 남고, 백업을 만들면 나중에 진짜 깨진 원본이 백업되지 못한다(검수 지적 3).
6. "새 버전"이면 이번 실행 동안 그 키에 쓰지 않는다 — 옛 빌드가 새 형식 값을 덮어쓰지 않게(이번 실행의 그 기록은 메모리에만, 막힌 저장마다 로그를 더 남기지 않음). "깨짐" · "일부 줄 버림"은 백업 뒤 지금처럼 이어서 저장한다(물을 것 1 · 2).
7. `?reset`(`ClearRecords`)은 백업 키도 지우고 쓰기 막기도 푼다 — 지운 뒤에는 그 실행에서도 정상 저장(물을 것 4 · 검수 지적 2).
8. 화면 안내는 넣지 않는다 — 로그만(물을 것 5).
9. 저장 값의 형식(머리줄 · 줄)은 바꾸지 않는다 — 새 키가 셋 늘 뿐이다.

## 수정 대상

기존 파일
- `Assets/Scripts/Core/Records/Progress.cs` · `PlayStats.cs` · `EndlessProgress.cs` — `Deserialize(string text, out RecordReadStatus status)` 오버로드. 지금의 `Deserialize(string)`은 그것을 불러 상태를 버린다(테스트 · 도구 호출 그대로).
- `Assets/Scripts/Game/Platform/SaveData.cs` — 상태 확인 · 경고 로그 · 백업 · 새 버전 키 쓰기 막기 · `ClearRecords`의 백업 삭제.
- `Assets/Tests/EditMode/ProgressTests.cs` · `PlayStatsTests.cs` · `EndlessTests.cs` — 상태 테스트 추가(기존 테스트는 고치지 않는다).
- `Assets/Tests/EditMode/ColoringBoot.Core.Tests.asmdef` — `references`에 `ColoringBoot.Game` 추가(검수 지적 1 · 사용자 2026-11-19). 어셈블리 이름은 그대로 둔다.
- `Docs/TDD.md` §1(테스트 어셈블리가 Game도 참조) · §5(저장) · `Docs/DECISIONS.md` · `Docs/Task.md` · `Docs/DevelopLog.md` · `CLAUDE.md` §5 코드 위치 표의 테스트 줄(Game 참조) · `.claude/skills/health-check/SKILL.md` 33줄(지우는 키에 `.Unread` 셋 추가 — 검수 지적 4).

새 파일
- `Assets/Scripts/Core/Records/RecordReadStatus.cs` — 상태 열거형(`Ok` · `LinesDropped` · `NewerVersion` · `Unreadable`)과 머리줄 판정 도우미 하나(세 클래스가 같이 씀 — "이름 같고 번호가 더 큼" 판정).
- `Assets/Tests/EditMode/SaveDataTests.cs` — 테스트 안의 메모리 저장소(`IKeyValueStore` 구현)로 `SaveData`를 만들어 상태별 백업 · 쓰기 막기 · `ClearRecords`를 본다. 경고 로그는 `LogAssert.Expect(LogType.Warning, …)`로(정상 값은 `LogAssert.NoUnexpectedReceived`). 별도 QA 스크립트(`SaveQa`)는 만들지 않는다.

다시 돌릴 빌더: 없음(씬 · 폰트 · 그림 · 목록 변화 없음 — 화면 문구를 더하지 않는다).

## 구현 단계

### 0단계 — 상태 열거형 · 머리줄 판정
- 목표: `RecordReadStatus`와 머리줄 판정 도우미.
- 수정 대상: 새 `RecordReadStatus.cs`.
- 선행: 없음.
- 완료 기준: 컴파일 통과 · Core가 `UnityEngine`을 참조하지 않음(`noEngineReferences`).
- 검증: 컴파일 · `SourceRuleTests`.

### 1단계 — 세 `Deserialize`가 상태를 돌려줌
- 목표: 요구사항 1 · 2 · 3.
- 수정 대상: `Progress.cs` · `PlayStats.cs` · `EndlessProgress.cs` · 세 테스트 파일.
- 선행: 0단계.
- 완료 기준: 클래스마다 테스트 — `null` · `""` → Ok / `Serialize()` 결과 → Ok(끝 빈 줄 포함) / 중간 · 끝의 공백뿐인 줄 · `\r\n` 줄 끝 → Ok / 깨진 줄 하나 → LinesDropped(읽은 기록은 기존 테스트와 같음) / `progress 2`(`stats 3` · `endless 2`) → NewerVersion / `garbage` · `progress x` · 다른 기록의 머리줄 → Unreadable / `stats 1` → Ok. 기존 테스트 전부 그대로 통과.
- 검증: EditMode 전체(먼저 새 테스트가 실패하는 것을 본다).

### 2단계 — `SaveData` 경고 · 백업 · 쓰기 막기 · `?reset`
- 목표: 요구사항 4 · 5 · 6 · 7.
- 수정 대상: `SaveData.cs` · 테스트 asmdef · 새 `SaveDataTests.cs`.
- 선행: 1단계.
- 완료 기준(`SaveDataTests`로): 키마다 — 정상 · 값 없음이면 지금과 같음(로그 · 백업 없음). LinesDropped · Unreadable → 경고 1회 · 백업 키에 원본(이미 있으면 그대로) · 다음 `Save*`가 정상 저장. NewerVersion → 경고 1회 · 백업 없음 · `Save*`가 그 키에 쓰지 않음(나머지 키는 정상 저장). `ClearRecords`가 백업 키 셋을 지우고, NewerVersion으로 막혔던 키도 그 뒤 `Save*`가 정상 저장.
- 검증: 컴파일 · EditMode 전체(먼저 새 테스트가 실패하는 것을 본다 · asmdef 참조 추가 뒤 기존 테스트 수가 그대로인지).

### 3단계 — 에디터 플레이 확인
- 목표: 실제 `PlayerPrefsStore` 경로에서 같은 동작.
- 수정 대상: 없음(QA만).
- 선행: 2단계.
- 완료 기준: (a) `ColoringBoot.Progress`를 `progress 2\n…`로 넣고 플레이 → 콘솔 경고 · 백업 키 없음 · 1번 판을 깨도 `ColoringBoot.Progress`가 그대로. (b) `garbage`로 넣고 플레이 → 경고 · 백업 키 생김 · 1번 판을 깨면 새 진행이 저장됨. (c) 정상 값 → 경고 없음.
- 저장 키 복원(검수 지적 4): 시작 전에 `eval`로 저장 키 6개(`health-check` 목록)의 값을 읽어 기록해 두고, 값을 넣을 때도 `eval`(`PlayerPrefs.SetString` → `Save`)로 한다. 끝나면 `eval`로 6개를 기록한 값으로 되돌리고(없던 키는 `DeleteKey`) `.Unread` 셋을 지운다. 새 스크립트는 만들지 않는다.
- 검증: 에디터 플레이(`BoardQa`) · 콘솔 · `eval`로 키 값 조회.

### 4단계 — 문서
- 목표: 바뀐 저장 동작을 기록.
- 수정 대상: `TDD.md` §1(테스트 어셈블리 참조) · §5(백업 키 · 상태 · 새 버전 쓰기 막기 · `?reset`) · `DECISIONS.md`(물을 것 답 · 테스트 어셈블리가 Game을 참조하게 된 이유) · `CLAUDE.md` §5 코드 위치 표 테스트 줄 · `.claude/skills/health-check/SKILL.md` 지우는 키 목록 · `Task.md` 155 · `DevelopLog.md`.
- 선행: 3단계.
- 완료 기준: 문서의 키 이름 · 동작이 코드와 같다.
- 검증: 대조.

WebGL 빌드 · 배포는 이 계획에 넣지 않는다(동작이 플랫폼과 무관 — `PlayerPrefsStore` 그대로). 다음 빌드 때 함께 나가며, 그때 콘솔 경고가 없는지(정상 값) 본다.

## 위험

- **회귀**: 규칙 · 솔버 · 스테이지를 건드리지 않는다 → 포도 · 프로토타입 · 콘텐츠 회귀 테스트는 영향 없음(EditMode 전체로 확인).
- **저장 데이터 호환**: 머리줄 · 줄 형식 · 진행 기록 키(파일 이름)는 그대로라 지금 플레이어의 기록은 그대로 읽힌다. 새 키 셋(`.Unread`)이 생길 수 있다 — 값이 깨졌을 때만.
- **오탐(정상 값을 "버림"으로 봄)**: 끝의 빈 줄 · `\r\n` · `stats 1`을 "버림"으로 세면 정상 플레이어마다 경고 · 백업이 생긴다 → 1단계 테스트에 넣는다. `PlayStats`는 같은 이름 줄이 두 번이면 뒤 줄이 이기고 `Progress`는 작은 수가 이긴다 — 버림으로 세지 않는다(지금 동작 유지).
- **새 버전 쓰기 막기의 대가**: 옛 빌드로 플레이한 그 실행의 진행은 저장되지 않는다(메모리에만). 새 빌드가 다시 열리면 새 형식 값이 그대로 있다. 이 때문에 "새 버전" 판정은 좁게(이름 같고 번호가 정수로 더 큼) 한다 — 넓으면 깨진 값에서도 영영 저장이 안 된다. 이 보호는 이 코드가 들어간 빌드부터 작동한다(출시 전이라 출시 빌드에는 모두 들어감).
- **테스트 어셈블리가 Game을 참조**: Game의 의존(UI · TMP · Input System)이 테스트 컴파일에 함께 들어온다. 테스트 이름 · 기존 테스트 수가 그대로인지 2단계에서 본다. `SourceRuleTests` · 콘텐츠 테스트는 파일을 읽으므로 영향 없음(추정 — 2단계 EditMode 전체로 확인).
- **앱인토스 네이티브 저장소(`Task.md` 217)**: 백업 키도 `IKeyValueStore`로만 쓰므로 교체에 영향 없음. 저장소의 키 이름 규칙(마침표 허용 등)은 연동 때 확인 — 지금 키도 마침표를 쓴다.
- **용량 · WebGL**: 코드 몇 줄 · 키 셋. 빌드 용량 영향 무시할 수준. 스레드 · 파일 시스템 사용 없음.

## 후보 기획(GDD §7)을 불가능하게 만드는지

- 이동 횟수 제한 · 흰색은 스테이지 형식 · 규칙의 일이고 이 계획은 기록 읽기만 바꾼다 → 막지 않는다. 오히려 나중에 기록 형식을 올릴 때(`progress 2`) 옛 빌드가 새 값을 지우지 않게 해 준다.

## 사용자에게 물은 것 — 답(2026-11-19, 모두 권장대로)

1. **"못 읽음"을 둘로 나눠 다르게 처리** — 예. 새 버전(`progress 2` 등)은 이번 실행 동안 그 키에 쓰지 않음(백업 없음 — 검수 지적 3), 깨진 값은 백업 + 빈 기록에서 이어서 저장.
2. **일부 줄만 버린 경우도 백업** — 예, 원본 그대로.
3. **백업 키 이름** — `ColoringBoot.Progress.Unread` · `ColoringBoot.PlayStats.Unread` · `ColoringBoot.Endless.Unread`, 이미 있으면 덮어쓰지 않음(처음 원본을 지킴).
4. **`?reset`이 백업 키도 지움** — 예(쓰기 막기도 풂 — 검수 지적 2).
5. **플레이어에게 화면으로 알림** — 아니오, 로그만.

## 검수 기록

- 1라운드(2026-11-19, `/review-plan`): 높음 없음. 반영한 지적(사용자가 모두 고름):
  1. 보통 — `SaveData` 동작에 회귀 테스트가 없음(테스트 asmdef가 Core만 참조, `SaveQa`는 health-check 밖) → 테스트 asmdef에 Game 참조 + `SaveDataTests`(사용자 (a) 선택), `SaveQa` 뺌.
  2. 낮음 — `?reset`이 쓰기 막기를 풀지 않으면 그 실행 동안 저장 안 됨(`GameFlow.cs:93` → `:223` 같은 실행) → 요구 7 · 2단계 완료 기준.
  3. 낮음 — "새 버전" 백업은 필요 없고 나중의 진짜 깨진 원본 백업을 막음 → 요구 5 · 6.
  4. 낮음 — 3단계 저장 키 복원 수단 없음 · health-check 키 목록에 `.Unread` 없음 → 3단계 · 4단계 · 수정 대상.
  5. 낮음(단순화) — 빈 줄 기준을 "끝의 빈 줄" → "공백뿐인 줄은 어디서든"으로 → 요구 1 · 1단계.
- 높음이 없어 검수는 1라운드로 닫는다.
