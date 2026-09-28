---
name: qa-scene
description: 보드 씬(Assets/Scenes/Board.unity) 셋업 전수 실측 — 읽기 전용. 계층 · 컴포넌트 · 직렬화 참조 누락 · Canvas Scaler · EventSystem 입력 모듈 · 프리팹 · 에셋 · 빌드 씬 목록을 MCP로 조회해 표로 보고한다. 씬을 수정하지 않는다. 씬 배선 뒤, 빌드 전, "인스펙터가 비어 있는지" 의심될 때 쓴다.
---

# /qa-scene — 보드 씬 셋업 실측 (읽기 전용)

Task.md DoD 3조 "코드는 맞는데 인스펙터가 비어 있음"을 잡는 절차. **아무것도 고치지 않는다** — 문제는 표로 보고하고, 수정은 사용자 승인 뒤 `AgentScripts/BoardSceneBuilder.cs`(다시 실행해도 같은 결과, 폰트가 바뀌었으면 `Phase2Font.cs` 먼저) 또는 MCP 배선 도구로 한다.

## 0. 전제
- `editor_status`: `ready`, `playMode: stopped`, `projectPath`가 이 프로젝트. Play Mode면 멈추라고 알리고 중단(플레이 중 값은 저장 안 됨).
- EditMode `run_tests`와 동시에 하지 않는다(테스트 러너의 임시 씬이 잡힘 — CLAUDE.md §8).
- 활성 씬이 `Assets/Scenes/Board.unity`가 아니면 보고하고 중단(`list_open_scenes`).

## 1. 계층 (`get_scene_hierarchy`)
기대값 — 루트 5개. UI는 모두 `/Canvas/SafeArea` 아래(이하 `…` = `/Canvas/SafeArea`):

| 경로 | 컴포넌트 | 활성 |
|---|---|---|
| `/Main Camera` | Camera(직교 · 단색 #D9DFDC) | O |
| `/EventSystem` | EventSystem · **InputSystemUIInputModule**(StandaloneInputModule이면 실패 — New Input System 전용) | O |
| `/Canvas` | Canvas · CanvasScaler · GraphicRaycaster | O |
| `/Canvas/SafeArea` | SafeAreaFitter | O |
| `…/StageName` · `…/MoveCounter` | Label(TextMeshProUGUI) | O |
| `…/TargetView` | BoardView(목표 썸네일) | O |
| `…/MixTable` | HorizontalLayoutGroup · MixTableView | O |
| `…/BoardArea` | Image(투명, raycastTarget) · BoardView | O |
| `…/ClearBanner` · `…/StuckBanner`(안에 UndoButton) | Image · Label | **X**(처음엔 꺼짐) |
| `…/UndoButton` · `…/RestartButton` · `…/SymbolsButton` · `…/SoundButton` | Image · Button · Label | O |
| `/Sound` | SoundController | O |
| `/Puzzle` | PuzzleController | O |

씬이 `isDirty: true`면 저장 안 된 변경이 있다고 보고한다.

## 2. 설정값 (`get_component_properties` / `get_serialized_fields`, format=value)
- `/Canvas` Canvas: renderMode = Screen Space - Overlay
- `/Canvas` CanvasScaler: uiScaleMode = Scale With Screen Size · referenceResolution = 1080×1920 · screenMatchMode = Expand
- `…/BoardArea` Image: color.a = 0 · raycastTarget = true
- `…/BoardArea` BoardView: `_showTarget` false · `_fitMargin` 1.8 / `…/TargetView` BoardView: `_showTarget` true · `_fitMargin` < 1. 둘 다 `_cellPrefab` · `_directionButtonPrefab` null 아님
- 모든 TextMeshProUGUI의 font = `Assets/Art/Fonts/Pretendard SDF.asset`(한글이 □로 나오면 실패)

## 3. 직렬화 참조 — null이 하나라도 있으면 실패
- `/Puzzle` PuzzleController: `_stageCode` · `_queryStages`(요소 모두) · `_palette` · `_boardView` · `_targetView` · `_mixTable` · `_stageName` · `_moveCounter` · `_undoButton` · `_restartButton` · `_symbolsButton` · `_stuckUndoButton` · `_clearBanner` · `_stuckBanner`
- `/Sound` SoundController: `_toggleButton` · `_toggleLabel`
- `…/MixTable` MixTableView: `_chipSprite`
- 프리팹 `Assets/Prefabs/Cell.prefab` CellView: `_fill` · `_marker` · `_markerFill` · `_deadRing` · `_selectRing` · `_ghost` · `_symbol`
- 프리팹 `Assets/Prefabs/DirectionButton.prefab` Button: targetGraphic

## 4. 에셋
- `_stageCode`가 가리키는 JSON이 `Stage.Parse`로 읽히는지: `eval`로 `ColoringBoot.Core.Stage.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(경로).text).Cells.Count` (포도 = 10)
- `_palette`의 `_colors` 길이 7, 알파 모두 1
- 스프라이트 `Assets/Art/Sprites/*.png`(`get_import_settings`): textureType Sprite · spriteImportMode Single
- `Assets/TextMesh Pro/Resources/TMP Settings.asset` 존재 · 기본 폰트 = Pretendard SDF
- `Pretendard SDF`: 고정(Static) 아틀라스 · `m_SourceFontFile` null(원본 TTF가 빌드에 딸려 가지 않게) · 씬 · 코드의 화면 문구 글자가 모두 들어 있는지(`Phase2Font.Build` 결과의 빠진 글자 0)
- `_queryStages`의 JSON도 `Stage.Parse`로 읽히는지(큰 벌집 = 37칸)

## 5. 빌드 씬 목록 (`get_build_settings`)
- 활성 씬 목록 = `Assets/Scenes/Board.unity` 하나, enabled

## 6. 보고
항목 · 기대 · 실측 · 판정(통과/실패) 표 하나. 실패만 따로 모아 원인 추정과 수정 방법(어느 빌더 · 어느 MCP 명령)을 적되 **실행하지 않는다**. 모두 통과면 한 줄로 "보드 씬 셋업 전수 통과(N항목)".
