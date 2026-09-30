using System.IO;
using UnityEngine;

namespace ColoringBoot.Core.Tests
{
    // 테스트에 쓰는 스테이지 코드
    internal static class TestStages
    {
        // GDD §3 포도 — 원문 그대로
        public const string Grape = @"{""name"":""포도"",""cells"":[[4,0,0,6],[3,1,6,6],[2,2,0,5],[3,2,0,5],[1,3,0,5],[2,3,0,5],[3,3,5,5],[1,4,0,5],[2,4,0,5],[1,5,0,5]]}";

        // 프로토타입 스테이지 9개의 회귀 기준 파일 — 게임 데이터(Assets/Data/Stages)와 분리해 둔 사본(점검 F6).
        // 칸 순서만 바뀌어도 탐색 수 · 순서 민감도 기대값이 달라지므로 이 파일은 고치지 않는다
        public static string ReadPrototype(string file) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Tests/EditMode/PrototypeStages", file + ".json"));
    }
}
