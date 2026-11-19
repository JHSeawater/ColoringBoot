using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ColoringBoot.Core.Tests
{
    // 테스트에 쓰는 스테이지 코드
    internal static class TestStages
    {
        // GDD §3 포도 — 원문 그대로
        public const string Grape = @"{""name"":""포도"",""cells"":[[4,0,0,6],[3,1,6,6],[2,2,0,5],[3,2,0,5],[1,3,0,5],[2,3,0,5],[3,3,5,5],[1,4,0,5],[2,4,0,5],[1,5,0,5]]}";

        // 프로토타입 스테이지 9개의 회귀 기준 파일 — 게임 데이터(Assets/Data/Chapters/Chapter1/Stages)와 분리해 둔 사본(점검 F6).
        // 칸 순서만 바뀌어도 탐색 수 · 순서 민감도 기대값이 달라지므로 이 파일은 고치지 않는다
        public static string ReadPrototype(string file) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/PrototypeStages", file + ".json"));

        // 챕터 4 호박(2026-11-17 사본) — 힌트 탐색이 무거운 판. 게임 파일(AutumnPumpkin.json)은 출시 전까지 바뀔 수 있어 여기 고정해 둔다
        public const string Pumpkin = @"{""name"":""호박"",""cells"":[[2,0,0,3],[3,0,0,6],[1,1,0,3],[2,1,0,0],[3,1,4,6],[4,1,0,3],[-1,2,0,3],[0,2,0,3],[1,2,0,3],[2,2,0,3],[3,2,0,3],[4,2,0,0],[-1,3,0,3],[0,3,0,3],[1,3,0,3],[2,3,1,3],[3,3,2,3],[-1,4,2,3],[0,4,0,3],[1,4,0,3],[2,4,0,0]],""palette"":""autumn"",""minMoves"":8}";

        // 게임에 들어가는 스테이지 폴더(Assets/Data 아래 JSON: 챕터 · 따라 하기 · 시험 목록)
        public static string DataPath => Path.Combine(Application.dataPath, "Data");

        // 게임 스테이지 파일 전부 — 테스트 이름에 보이도록 Data 기준 상대 경로
        public static IEnumerable<string> GameStageFiles() =>
            Directory.GetFiles(DataPath, "*.json", SearchOption.AllDirectories)
                .Select(path => path.Substring(DataPath.Length + 1).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal);

        public static Stage ReadGameStage(string file) => Stage.Parse(File.ReadAllText(Path.Combine(DataPath, file)));

        // 무한 모드 묶음(Assets/Data/Endless/<난이도>.txt — 한 줄에 스테이지 코드 하나)의 줄들. 빈 줄은 뺀다
        public static string[] ReadEndlessPool(string tier) =>
            File.ReadAllLines(Path.Combine(DataPath, "Endless", tier + ".txt")).Where(line => line.Length > 0).ToArray();
    }
}
