# TDD — ColoringBoot 기술 스펙

> 지금 코드가 어떻게 되어 있는지 적은 구현 지도다. 지킬 규칙은 `CLAUDE.md`, 기획(무엇 · 왜)은 `GDD.md`, 결정과 이유는 `DECISIONS.md`. 이 문서와 코드가 어긋나면 코드를 확인하고 이 문서를 고친다.
> 2026-11-07 `CLAUDE.md` §3 · §4 · §8에서 옮겨 왔다(내용 그대로 — 날짜는 그 내용을 정한 때).

---

## 1. 코드 구조

네임스페이스 = 어셈블리 이름(하위 폴더는 네임스페이스를 나누지 않는다). 2026-09-24 확정 · 2026-10-04 · 10-05 정리.

| 위치 | 내용 |
|---|---|
| `Assets/Scripts/Core/` | `ColoringBoot.Core` — 순수 로직(`noEngineReferences: true`). `Rules/`(좌표 · 색 · 보드 · 붓질 · 판정 · 세션) · `Stages/`(스테이지 코드 읽기 · 쓰기) · `Solver/`(솔버 · 생성기 · 난이도 지표 `StageMetrics` · 그림 맵 시작 칸 찾기 `SeedSearch`) · `Records/`(진행 · 플레이 기록) |
| `Assets/Scripts/Game/` | `ColoringBoot.Game` — 표현 계층. `Board/`(한 판 — `PuzzleController` · `BoardView` · 칸 · 조합표) · `Screens/`(`GameFlow` · 화면 · 패널 · 전환) · `Data/`(ScriptableObject 타입) · `Platform/`(저장 · 광고 · 소리 · 주소 — 앱인토스 연동 때 바꿀 곳). `BoardView`는 partial 파일 4개: `BoardView.cs`(필드 · 칸 만들기 · 그리기 · 배치 · 선택 — 필드는 모두 여기) · `.Input`(끌기 · 탭 · 키보드) · `.Preview`(미리보기) · `.Motion`(연출) |
| `Assets/Scripts/LevelEditor/` | `ColoringBoot.LevelEditor` — `defineConstraints: UNITY_EDITOR`(빌드에 안 들어가고 씬에는 붙음) + `Assets/Scenes/LevelEditor.unity`(빌드 목록 제외, 플레이해서 쓴다 · 조작 패널은 IMGUI) |
| `Assets/Tests/EditMode/` | `ColoringBoot.Core.Tests` — 프로토타입 스테이지 회귀 기준은 게임 데이터와 분리한 사본 `PrototypeStages/`(고치지 않는다) |
| `Assets/Data/` | 챕터마다 `Chapters/ChapterN/`(`Stages/*.json` · `StageCatalog` · `ChapterArt` · `Chapter` — 제목 · 부제 · 목록 · 그림) · 따라 하기 `Tutorial/`(`Stages/` · `TutorialCatalog`) · 시험 목록 `Lab/`(`Stages/` · `LabCatalog` — `?lab`) · 무한 모드 퍼즐 묶음 `Endless/{Easy,Normal,Hard}.txt`(한 줄에 스테이지 코드 — `EndlessPoolBuilder`) · 팔레트 `Palettes/`(+ `PaletteCatalog`) · `Settings/`(디자인 값 `UiTheme` · 연출 값 `MotionSettings`) — 2026-10-07 챕터 단위로 옮김(GUID · 파일 이름 그대로) |
| `Assets/Art/` | 챕터 그림(게임 크기) · 폰트 · 스프라이트 — 빌더가 만든다 |
| `AgentScripts/` | `Assets/` 밖의 `run_script` 빌더(`Build/`) · QA(`QA/`) · 도구(`Tools/`) · 한 번 적용한 설정 기록(`Setup/`) — 무엇을 언제 다시 돌리는지는 `AgentScripts/README.md` |
| `ArtSource/` | 그림 원본(`ChapterN/` — `ArtSpec.md` 규격, `Assets/` 밖). `_archive/` = 안 쓰는 원본 보관(임시 도안 `Chapter1-temp`) |

## 2. 화면과 기능

* **화면**: 보드는 uGUI(Screen Space - Overlay, 2026-09-28 사용자 결정 — 렌더 스케일 0.8의 영향을 받지 않고 입력이 UI와 한 체계). 화면 흐름 = `GameFlow`(Phase 4.3): 씬 하나에서 타이틀(`TitleScreen`, Phase 7.3 — 주소에 `?stage=`가 있으면 건너뜀) · 선택 화면(`StageSelectView`) · 보드(`BoardScreen` — `PuzzleController.Open`으로 스테이지를 바꿔 연다) · 옵션(`OptionsView`) · 기록(`StatsView`) 패널을 켜고 끈다(씬 다시 불러오기 없음). 화면 · 패널은 켜질 때 0.2초 동안 나타난다(`ScreenFade` — 투명도만, 입력은 막지 않음). 저장소 · `SaveData`(진행 · 기록 · 설정) · 광고 자리는 `GameFlow`가 한 번 만들어 넘긴다.
* **여러 챕터**(2026-10-11 사용자 결정): 목록 화면의 작은 그림 양옆 ◀ ▶로 챕터를 넘긴다(`StageSelectView.ChapterStepped` — 넘길 챕터가 없으면 숨김). 잠긴 챕터는 제목 "챕터 N · 잠김" · 버튼 모두 잠김 · 누르면 "앞 챕터 그림을 완성하면 열려요"(해금은 Core `Progress.IsChapterUnlocked`). 그림을 완성하면 그림 화면 버튼이 [다음 챕터] → 다음 챕터 목록. 타이틀 [시작]은 아직 완성하지 않은 첫 챕터(`Progress.CurrentChapter`). 지금 챕터 2는 임시(옛 판 사본 `Temp01~11` + 임시 도안, `AgentScripts/Setup/Phase8TempChapter2.cs`)이고 주소 `?chapters`일 때만 들어간다.
* **따라 하기 · 힌트**(2026-10-07, GDD §6): 따라 하기 = 처음 [시작] 때(봤는지는 `SaveData.TutorialSeen`) · 옵션 "규칙 다시 보기" → `PuzzleController.OpenLesson` — 레슨 i = `TutorialCatalog`의 i번째 스테이지 + `TutorialLessons`(그을 획 · 한 줄 문구, 정해진 줄 · 방향만 받음 — 마지막 복습은 혼자 풀기: 아무 획 · 힌트 사용 가능). 힌트 = Core `PuzzleSession.FindHint`(다음 한 수 · 몇 수 되돌릴지, 탐색 상한) → `BoardView.ShowGuide`(손가락 표시 + 미리보기) · 안내 띠, 보상형 광고 자리(`IAdService.ShowRewarded`)를 거친다. "완벽"은 힌트와 무관.
* **무한 모드**(2026-10-07, GDD §5): 타이틀 · 목록 아래 [무한 모드](따라 하기 뒤) → `EndlessView`(난이도) → `GameFlow.OpenEndless` — 퍼즐 i = 묶음의 i번째 줄, 기록 키 `Endless{난이도}{번호}`(퍼즐마다), 진행은 Core `EndlessProgress`(지금 번호 · 푼 수 · 건너뛴 수). 풀린 판을 떠날 때 다음 번호로. 위 가운데 [건너뛰기](안 풀린 판은 힌트처럼 보상형 광고 자리 `IAdService.ShowRewarded`를 거침, 2026-10-17 — 지나간 판은 다시 하지 않음: 묶음을 다 돌면 처음부터). 그림 보상 없음.
* **보드 연출**(Phase 7.4) = `BoardView.PlayStroke` · `PlayClear` · `PlayShake`(값은 `MotionSettings`) — 세션 상태는 즉시 바뀌고 화면만 늦게 따라간다(경로는 Core `Trace`). `Render`가 진행 중인 연출을 끝난 모습으로 정리한다.
* **챕터 그림**(Phase 5) = `ChapterArt`(선화 + 단계 조각, 단계 i ↔ 목록의 i번째 스테이지) · `ChapterView`(선택 화면 위 작은 그림 · `ChapterScreen` 큰 그림): 처음 클리어 → 클리어 띠 "다음" → 그림 화면에서 그 단계 칠하기(다 칠하면 "그림 완성!") → 다음 스테이지. 목록 버튼으로 나가면 작은 그림에서 칠한다. 다시 클리어하면 그림 화면을 거치지 않는다(`PuzzleController.FirstClear`). 그림을 바꿀 땐 `ChapterArtExport.py` → `ChapterArtBuilder`를 차례로 실행한다(`AgentScripts/README.md`).
* **화면 디자인 기준**(Phase 7.1, 시안 A "종이와 갈색 선") = `UiTheme`(`Assets/Data/Settings/UiTheme.asset` — 바탕 · 글자/선 갈색 · 버튼 바탕 등. 퍼즐 칸 색은 팔레트라 여기 없음): `BoardSceneBuilder`가 이 값으로 씬 · 프리팹을 만든다(값을 바꾸면 BuildPrefabs → BuildScene). 버튼 모양 = 9-slice 둥근 사각형 `RoundFill` · `RoundRing` · 그림 테두리 `FrameRing` · 스테이지 버튼 외곽선 `HexLine`(`SpriteBuilder.BuildRound`).

## 3. 팔레트

* 구현(2026-09-30): `ColorPalette._id`(스테이지 JSON의 이름 — `default` · `pastel`) · `PaletteCatalog`(`Assets/Data/Palettes/PaletteCatalog.asset`, 첫 칸 = 기본). 이름이 없으면 기본, 모르는 이름이면 기본 + 경고. 기호 글자색은 칸 색의 밝기(`Color.grayscale` > 0.6이면 어두운 글자).

## 4. 스테이지 데이터 · 저장 형식

* 프로토타입 포맷 `{"name": ..., "cells": [[q, r, 시작 색, 목표 색], ...]}` (GDD §3) + 선택 필드 `palette`(팔레트 이름, 없으면 기본 팔레트) · `minMoves`(최소 수, 솔버가 채움, 없으면 미표시). 로직은 `palette`를 이름(문자열)으로만 들고, 팔레트 에셋을 고르는 일은 표현 계층이 한다. 프로토타입에 들어 있는 스테이지 9개(포도 포함)는 이 포맷 그대로 옮겨 쓸 수 있다. 읽기는 `Stage.Parse` — Core 전용 파서다(`JsonUtility`는 UnityEngine이고 중첩 배열을 못 읽음, 2026-09-28 사용자 결정). `name` · `cells` 필수, 모르는 키 · 0~7 밖의 색 · 좌표 중복은 `FormatException`.
* **저장 형식 (2026-09-29 사용자 결정)**: 스테이지 = `Assets/Data/Chapters/ChapterN/Stages/<영문 이름>.json` TextAsset(이 포맷 그대로 — 프로토타입 코드와 복사 · 붙여넣기로 오감, git 차이가 읽힘). `minMoves`는 레벨 에디터가 저장할 때 솔버로 채운다. 순서 민감도는 저장하지 않고 에디터에만 표시. 게임이 여는 스테이지 목록은 `StageCatalog`(ScriptableObject, JSON TextAsset 순서 목록 — Phase 4 스테이지 선택 화면도 이것을 쓴다). 챕터는 `Chapter` 에셋(제목 · 부제 · 목록 · 그림, 2026-10-07)이 묶고 `GameFlow._chapters`(차례 — 앞 챕터 그림을 완성하면 다음 챕터, 2026-10-11)가 연다 — 파일 이름은 진행 기록의 키라 챕터 · 따라 하기 · 시험 목록 모두에서 겹치면 안 된다(`ContentRegressionTests`).

## 5. 저장 · 플랫폼 서비스

* 저장은 `PlayerPrefs`를 직접 부르지 않고 인터페이스 뒤에 둔다(앱인토스에서 네이티브 저장소로 교체). 광고는 인터페이스 자리만. 사운드 켜고 끄기와 백그라운드 전환 시 정지는 처음부터 구조에 넣는다. 구현(2026-09-30): `IKeyValueStore` · `PlayerPrefsStore`(쓸 때마다 `PlayerPrefs.Save()` — WebGL은 그래야 IndexedDB에 남음), 키 `ColoringBoot.Progress`(클리어 · 최고 기록, Core `Progress`) · `ColoringBoot.PlayStats`(플레이테스트 기록, Core `PlayStats`) — 저장 형식은 머리줄(`progress 1` · `stats 2` — 2026-10-07 힌트 칸, `stats 1`도 읽음) + 탭 구분 줄, 깨지면 빈 기록으로 시작. 따라 하기를 봤는지는 `ColoringBoot.Tutorial`, 무한 모드 진행은 `ColoringBoot.Endless`(`endless 1`, 2026-10-07) — `?reset`이면 함께 지움. 스테이지는 파일 이름으로 구분. 광고는 `IAdService` · `NoAdService`. 주소 쿼리(테스트 주소 — GitHub Pages · localhost · 127.0.0.1 — 에서만, `UrlQuery.IsQaHost`, 2026-10-07): `?stage=<파일 이름>` · `?stats`(기록 패널 — 지금 목록 + 따라 하기) · `?reset`(진행 · 기록 지우기) · `?palette=<이름>`(모든 스테이지를 그 팔레트로 — 색 확인용) · `?lab`(시험 목록 11개 — 소재 맵 시험 · 챕터 1에서 빠진 옛 판, 모두 열림 · 그림 화면 없음, 2026-10-06 · 10-11 · 기믹 판은 10-12에 뺌) · `?chapters`(임시 챕터 `GameFlow._testChapters`를 뒤에 붙임 — 여러 챕터 흐름 확인, 2026-10-11).

## 6. WebGL 설정

* WebGL Build Support 설치 · 활성 빌드 타깃 WebGL 확인(`list_build_targets` · `get_build_settings`). WebGL은 품질 레벨 `Mobile`(→ `Mobile_RPAsset`, URP)을 쓴다. 적용된 설정: 압축 Brotli + Decompression Fallback · Managed Stripping High · IL2CPP OptimizeSize · 기본 캔버스 540×960 · 데이터 캐싱 · 스레드 끔 · 스플래시 끔(Phase 4.1) · 전용 템플릿 `PROJECT:ColoringBoot` · 제품 이름 "컬러링붓" · Name Files As Hashes(Phase 7.6, `AgentScripts/Setup/Phase7WebTemplate.cs`). 텍스처 압축: 챕터 그림만 ASTC(선화 4×4 · 색칠 단계 6×6, `ChapterArtBuilder`), UI 스프라이트는 압축하지 않음(2026-10-11 사용자 결정 — 휴대폰이 그대로 써 메모리가 줄고, PC 브라우저는 불러올 때 푼다).
* 설정 이유: GitHub Pages는 `Content-Encoding: br` 헤더를 주지 못해 Brotli 파일이 그대로는 로드에 실패한다 → Decompression Fallback. 빌드 파일 이름이 매번 같으면 브라우저가 GitHub Pages 캐시(10분)의 옛 파일과 새 파일을 섞어 `RangeError: Maximum call stack size exceeded`로 멈춘다(2026-10-03) → Name Files As Hashes · 템플릿 CSS는 `index.html` 안에.
* 빌드 크기 · 첫 로딩 · 배포 이력은 `Docs/BuildHistory.md`.

## 7. 한글 폰트

* TMP 기본 폰트(LiberationSans SDF)에는 한글 글리프가 없다(Labyrinth 2026-09-16 선례 — 이 프로젝트는 2026-09-30에 LiberationSans를 지웠다. TMP Essential Resources를 다시 임포트하면 되살아나 빌드에 들어간다). 한글 폰트는 용량이 커서 첫 로딩 10초에 영향을 주므로, 필요한 글자 범위와 방식을 정해서 넣는다 → Pretendard SemiBold 고정 아틀라스(쓰는 글자만, 2026-09-28 사용자 결정 — 1024² 한 장, 샘플링 56: 64에서 254자에 두 장이 되어 2026-09-30에 낮춤. `FontBuilder.Build` 결과가 두 장이 되면 용량과 함께 보고한다 — 2026-10-11 294자에서 두 장이 됨: 2번째 장은 글자 5개뿐이라 빌드 +664바이트 · 휴대폰 메모리 +1 MiB, WebGL 표시 정상). **화면 문구나 스테이지 이름을 새로 쓰면 `AgentScripts/Build/FontBuilder.cs`를 다시 실행**하고(폰트 에셋이 새로 만들어지므로) `BoardSceneBuilder`의 BuildPrefabs → BuildScene도 다시 돌린다. 문구는 문자열 리터럴로 써야 수집된다(로그 · 예외 메시지 · `[Tooltip]` · `[Header]` 줄은 제외 — 2026-10-02부터 인스펙터 설명 글자를 빼서 278 → 199자, 2026-10-05부터 머리글도). 빌더는 `Assets/Scripts/Game` · `Assets/Data`(모든 스테이지 JSON)를 하위 폴더까지 읽고(2026-10-05) 챕터 에셋의 제목 · 부제도 모으며(2026-10-07), `QaScene`이 씬 글자 · 스테이지 이름이 모두 폰트에 있는지 점검한다. 폰트를 다시 만들지 않고 모을 글자만 보려면 `FontBuilder.Preview`. 주의: 이 TMP 버전은 `AtlasPopulationMode.Static`을 폐기 예정(obsolete)으로 표시한다 — 지금은 빌드 · 표시 정상(2026-09-29). TMP를 올릴 때 Dynamic 방식으로 옮길지(용량 · 원본 폰트 포함 여부) 다시 정한다.
