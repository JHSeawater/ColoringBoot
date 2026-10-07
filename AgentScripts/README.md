# AgentScripts — 빌더 · QA · 도구

`Assets/` 밖에 두는 작업 스크립트다. C#은 MCP `run_script`(또는 CLI `unity command run_script`)로 **한 파일씩** 메모리에서 컴파일해 실행한다 — 임포트 · 도메인 리로드가 없다. 경로는 모두 프로젝트 루트 기준이다.

```
run_script(file=AgentScripts/Build/FontBuilder.cs, entry=FontBuilder.Build)
unity command run_script --file AgentScripts/QA/QaScene.cs --entry QaScene.Check
python AgentScripts/Build/ArtCheck.py ArtSource/Chapter1
```

## 규칙

- **스크립트끼리 서로 참조할 수 없다**(한 파일씩 컴파일). 공통 코드가 필요하면 각 파일에 둔다.
- **이 빌더들이 정본이다** — 보드 씬 · 레벨 에디터 씬 · 프리팹 · 스프라이트 · 폰트 · 팔레트 · 챕터 그림 에셋. 에디터나 MCP로 직접 고친 값은 다음 빌드 때 사라진다 → 빌더를 고쳐 다시 실행한다(급히 직접 고쳤다면 빌더에도 같은 수정).
- **에셋을 옮기거나 이름을 바꾸면 빌더의 경로 상수도 함께 바꾼다.** `UiTheme` · `MotionSettings` · 파스텔 팔레트는 없으면 기본값으로 새로 만들므로(사람이 고친 값 유지용), 경로가 어긋나면 기본값 에셋이 조용히 하나 더 생긴다.
- **폰트 빌더는 `Assets/Scripts/Game` · `Assets/Data`(모든 스테이지 JSON)를 하위 폴더까지 읽고 챕터 에셋(`Chapter`)의 제목 · 부제도 모으며, 로그 · 예외 메시지 · `[Tooltip]` · `[Header]` 줄은 뺀다**(2026-10-05). 화면 글자가 폰트에 없으면 게임에 □로 나온다 — `QaScene`이 씬 글자 · 스테이지 이름을 점검한다.
- Python: `pip install -r AgentScripts/requirements.txt`. Windows 표준 출력은 cp949라 스크립트가 `sys.stdout.reconfigure(encoding='utf-8')`를 부른다.

## Build/ — 다시 돌리는 빌더

| 파일 | 만드는 것 | 언제 다시 실행 | entry |
|---|---|---|---|
| `BoardSceneBuilder.cs` | 칸 · 방향 버튼 프리팹, 보드 씬(`Board.unity`), 레벨 에디터 씬, 디자인 · 연출 값 에셋(없을 때만) | 화면 구성 · `UiTheme` 값을 바꿨을 때, 폰트를 다시 만든 뒤 | `BuildPrefabs` → `BuildScene` · `BuildEditorScene` · `BuildCatalog`(프로토타입 9개 중 빠진 것만 목록에) |
| `SpriteBuilder.cs` | `Assets/Art/Sprites` 흰색 스프라이트(색은 코드가 입힘) | 모양을 바꿀 때 | `Build`(칸 · 테두리 · 원 · 화살표) · `BuildIcons`(자물쇠 · 별) · `BuildRound`(버튼 · 그림 테두리 · 육각 외곽선) |
| `FontBuilder.cs` | `Pretendard SDF` 고정 아틀라스(쓰는 글자만) | 화면 문구 · 스테이지 이름을 바꿨을 때 → 뒤에 `BuildPrefabs` → `BuildScene` | `Build` · `Preview`(읽기 전용 — 모을 글자만 보여 줌) |
| `PaletteBuilder.cs` | 기본 팔레트 이름 · 파스텔 팔레트 · `PaletteCatalog` | 파스텔 색 · 팔레트 목록을 바꿀 때(기본 팔레트 색은 `Setup/Phase1Assets.CreatePalette`) | `Build` |
| `StageOrder.cs` | 챕터 N의 `StageCatalog` 순서 · 시험 목록 `LabCatalog`(`?lab`) · 따라 하기 `TutorialCatalog`(레슨 i ↔ `TutorialLessons`) — 시험 · 따라 하기 목록 에셋은 없으면 만듦 | 순서를 바꾸거나 스테이지를 더할 때 — 파일 안의 목록을 고친 뒤 | `SetOrder`(args=[챕터 번호]) · `SetLabOrder` · `SetTutorialOrder` |
| `EndlessPoolBuilder.cs` | 무한 모드 퍼즐 묶음 `Assets/Data/Endless/{Easy,Normal,Hard}.txt`(한 줄에 스테이지 코드, 같은 시드면 같은 묶음) — 난이도별 임시 기준은 파일 안 | 기준을 바꾸거나 개수를 늘릴 때 → 뒤에 `BuildScene`(묶음 연결은 그대로라 파일만 바뀌면 다시 안 돌려도 됨) | `Build`(args=["Easy", 300, 1] — 난이도 · 개수 · 시드) |
| `ArtCheck.py` | (읽기 전용) 그림 파일 규격 점검 — `ArtSpec.md` §6 | 그림을 받았을 때 | `python … [원본 폴더]`(없으면 `ArtSource/Chapter1`) |
| `ChapterArtExport.py` | 점검 → 절반 크기 · 여백 자르기 → `Assets/Art/Chapters/Chapter1` + `layout.json` | 챕터 그림을 바꿀 때(다음 줄과 차례로) | `python … [원본 폴더] [출력 폴더]` |
| `ChapterArtBuilder.cs` | 스프라이트 설정 · `Assets/Data/Chapters/ChapterN/ChapterArt.asset`(그림 조각은 `Assets/Art/Chapters/ChapterN/`) | `ChapterArtExport.py` 바로 뒤 | `Build`(args=[챕터 번호]) |

## QA/ — 점검(읽기 전용이거나 에디터 플레이 중에만)

| 파일 | 하는 일 | entry |
|---|---|---|
| `QaScene.cs` | 보드 씬 셋업 전수 점검(`/qa-scene`) — 참조 누락 · 폰트(씬 글자 · 스테이지 이름이 폰트에 있는지 포함) · 팔레트 · 스테이지가 풀리는지 · 그림 단계 수 | `Check` |
| `BoardQa.cs` | 에디터 플레이 보드 QA — 실제 포인터 이벤트로 탭 · 끌기 · 버튼 · 흐름(CLAUDE.md §2) | `Flow` · `ChooseStage` · `Press` · `SolveByDrag` · `Drag` · `State` · `Lab`(시험 목록 열기) 등 |
| `LevelEditorQa.cs` | 레벨 에디터 씬 플레이 QA — 칠하기 · 획 기록 · 저장 · 생성 조건 · 그림 → 시작 칸 찾기 | `Paint` · `Record` · `Check` · `Save` · `GenerateWith` · `FindSeeds` 등 |
| `ConsoleDump.cs` | 콘솔 창의 에러를 직접 읽음(MCP 버퍼가 놓친 것까지) | `Errors` |

## Tools/

| 파일 | 하는 일 |
|---|---|
| `Refresh.cs` | 디스크에 직접 쓴 에셋을 임포트(`Refresh.All`) — 빌더가 참조하기 전에 |
| `RemoveStage.cs` | 스테이지를 그 챕터 목록 · 파일에서 뺌(`RemoveStage.Remove`, args=`["파일 이름"]` — 챕터 폴더에서 찾음) → 뒤에 `FontBuilder.Build` → `BuildPrefabs` → `BuildEditorScene` → `BuildScene` |
| `deploy-pages.sh` | `Builds/WebGL`을 gh-pages에 배포(`bash AgentScripts/Tools/deploy-pages.sh "메시지"`) — gh-pages는 커밋 하나만 두고 강제 push(`--force-with-lease`), 메시지에 빌드 바이트를 붙인다 |

## Setup/ — 한 번 적용한 설정 기록

새 환경 · Unity 업그레이드 · 설정이 의심될 때 다시 본다. 이름은 적용한 Phase 그대로 둔다.

| 파일 | 적용한 것 |
|---|---|
| `Phase0ClearVolumeProfile.cs` | URP 에셋 2개의 볼륨 프로필 참조 해제(2026-09-24) |
| `Phase0WebGLSettings.cs` | WebGL Player 설정 — Decompression Fallback · Stripping High · 캔버스 540×960 · 남은 심볼 정리(`Preview` → `Apply`) |
| `Phase1Assets.cs` | TMP Essential Resources 임포트(대화상자 없이) · 기본 팔레트 색(`CreatePalette` — 다시 실행하면 덮어씀) |
| `Phase4BuildSize.cs` | 스플래시 끄기 · 안 쓰는 LiberationSans 삭제(2026-09-30) |
| `Phase7WebTemplate.cs` | 전용 웹 템플릿 · 제품 이름 "컬러링붓" · 파일 이름 해시(2026-10-03) |
| `Phase8ChapterFolders.cs` | 데이터를 챕터 단위 폴더로 옮김 · 챕터 1 `Chapter` 에셋 만듦(2026-10-07, `Preview` → `Apply`) |
| `Phase8ProjectWideActions.cs` | 안 쓰는 프로젝트 전체 입력(`InputSystem_Actions`) 등록 해제 · 삭제(2026-10-07, `Preview` → `Apply`) |
| `TempChapterArt.py` | 임시 도안 생성 → `ArtSource/_archive/Chapter1-temp`(보관용) |

## 옛 이름 (2026-10-04 정리 전에는 모두 `AgentScripts/` 바로 아래)

Task.md · DevelopLog.md의 지난 기록은 옛 이름 그대로다.

| 옛 이름 | 지금 |
|---|---|
| `Phase1Sprites.cs` | `Build/SpriteBuilder.cs` |
| `Phase2Font.cs` | `Build/FontBuilder.cs` |
| `Phase4Palettes.cs` | `Build/PaletteBuilder.cs` |
| `Phase4Stages.cs` | `Build/StageOrder.cs` |
| 그 밖의 파일 | 이름 그대로 `Build/` · `QA/` · `Tools/` · `Setup/` 중 한 곳 |
| `ArtSource/chapter1_test/` | `ArtSource/Chapter1/`(지금 챕터 1 그림) |
| `ArtSource/Chapter1/`(임시 도안) | `ArtSource/_archive/Chapter1-temp/` |
