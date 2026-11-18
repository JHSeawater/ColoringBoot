# 컬러링붓

육각형 보드 위에 붓을 한 줄씩 그어 색을 칠하고 섞으며, 정해진 그림을 완성하는 퍼즐 게임입니다.

- **플레이(테스트 배포)**: https://jhseawater.github.io/ColoringBoot/ — 정식 출시 전 시험판이라 내용이 바뀔 수 있습니다. 휴대폰 세로 화면 기준이고 PC 브라우저에서도 됩니다.
- **상태**: 개발 중. Android 앱으로 원스토어에 먼저 내고, 앱인토스(웹) · 플레이스토어에 이어서 낼 예정입니다.

## 규칙

- 칸을 누른 채 한 방향으로 끌면, 그 칸이 있는 줄 전체에 붓이 지나갑니다. 붓은 지나가며 만난 색을 묻혀 다음 칸을 칠합니다.
- 기본색은 빨강 · 노랑 · 파랑이고, 섞이면 주황 · 초록 · 보라 · 검정이 됩니다. 한 번 섞인 색은 빠지지 않습니다.
- 모든 칸이 목표 그림과 같아지면 성공입니다. 목표에 없는 색이 한 칸에라도 들어가면 막힙니다(되돌리기 · 처음부터 · 힌트로 다시 풉니다).
- 챕터마다 판을 하나 깰 때마다 그림의 소재 하나가 칠해지고, 모두 깨면 그림 한 장이 완성됩니다.

## 만든 방식

- Unity 6.6 · URP. 한 프로젝트에서 WebGL(앱인토스 · 브라우저)과 Android 빌드를 만듭니다.
- 퍼즐 규칙 · 판정 · 솔버는 Unity를 참조하지 않는 순수 C#(`Assets/Scripts/Core`)이고, 화면 · 입력 · 소리는 그 위의 표현 계층(`Assets/Scripts/Game`)입니다.
- 스테이지는 JSON 파일입니다(`{"name": ..., "cells": [[q, r, 시작 색, 목표 색], ...]}`). 최소 수는 레벨 에디터가 솔버로 채웁니다.

## 폴더

| 위치 | 내용 |
|---|---|
| `Assets/Scripts/Core/` | 규칙 · 스테이지 읽기 · 솔버 · 기록(순수 C#) |
| `Assets/Scripts/Game/` | 화면 · 보드 그리기 · 입력 · 저장 · 소리 |
| `Assets/Scripts/LevelEditor/` | 에디터 전용 레벨 에디터 |
| `Assets/Tests/EditMode/` | 규칙 · 콘텐츠 회귀 테스트 |
| `Assets/Data/` | 챕터 · 스테이지 JSON · 팔레트 · 무한 모드 묶음 |
| `Assets/Art/` | 챕터 그림 · UI 그림 · 폰트 |
| `ArtSource/` | 챕터 그림 원본과 나누기 설정 |
| `AgentScripts/` | 씬 · 데이터 빌더, QA · 도구 스크립트(`Assets/` 밖) |
| `Prototype/` | 규칙의 참조 구현인 웹 프로토타입 |
| `Docs/` | 기획 · 기술 문서와 작업 기록 |

## 문서

| 문서 | 내용 |
|---|---|
| `Docs/GDD.md` | 기획서 — 게임 규칙의 정본 |
| `Docs/TDD.md` | 기술 문서 — 코드 구조 · 데이터 · 저장 · WebGL 설정 |
| `Docs/Task.md` | 지금 상태와 작업 목록 |
| `Docs/DECISIONS.md` | 결정과 그 이유 |
| `Docs/DevelopLog.md` | 작업 기록 |
| `Docs/BuildHistory.md` | WebGL 빌드 크기 · 배포 이력 |
| `Docs/ArtSpec.md` | 챕터 그림 전달 규격 |
| `CLAUDE.md` · `AgentScripts/README.md` | 개발 작업 규칙 · 도구 사용법 |
| `CREDITS.md` | 폰트 · 소리 · 그림 출처 |

## 저장소를 받아 열 때

- 효과음 파일(`Assets/Sound/`)은 라이선스 때문에 저장소에 넣지 않았습니다. 받은 저장소에서는 씬 빌더(`BoardSceneBuilder`)가 음원을 찾지 못해 멈춥니다 — 같은 경로에 음원과 `.meta`가 있어야 합니다.
- 테스트는 Unity Test Runner의 EditMode로 돌립니다.
