using System;
using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    public class StageParserTests
    {
        [Test]
        public void Parse_GrapeCode()
        {
            Stage stage = Stage.Parse(TestStages.Grape);

            Assert.AreEqual("포도", stage.Name);
            Assert.AreEqual(10, stage.Cells.Count);
            Assert.AreEqual(new HexCoord(4, 0), stage.Cells[0].Coord);
            Assert.AreEqual(PaintColor.Empty, stage.Cells[0].Start);
            Assert.AreEqual(PaintColor.Green, stage.Cells[0].Target);
            Assert.AreEqual(new HexCoord(3, 3), stage.Cells[6].Coord);
            Assert.AreEqual(PaintColor.Purple, stage.Cells[6].Start);
            Assert.IsNull(stage.Palette);
            Assert.IsNull(stage.MinMoves);
        }

        [Test]
        public void Parse_OptionalFields()
        {
            Stage stage = Stage.Parse(@"{""name"":""t"",""cells"":[[0,0,1,1]],""palette"":""pastel"",""minMoves"":5}");

            Assert.AreEqual("pastel", stage.Palette);
            Assert.AreEqual(5, stage.MinMoves);
        }

        // 줄바꿈 · 들여쓰기 · 음수 좌표 · 문자열 이스케이프 (Python json.dumps는 한글을 유니코드 이스케이프로 쓴다)
        // 역슬래시는 %로 적고 바꾼다 — 소스에 역슬래시 + u + 16진 4자리를 쓰면 편집 도구가 그 글자로 바꿔 저장한다(2026-09-28)
        [Test]
        public void Parse_WhitespaceNegativeCoordsAndEscapes()
        {
            Stage stage = Stage.Parse(@"{
                ""name"": ""a%""b%uD3EC%uB3C4"",
                ""cells"": [ [-3, 2, 0, 7] ]
            }".Replace('%', '\\'));

            Assert.AreEqual(@"a""b포도", stage.Name);
            Assert.AreEqual(new HexCoord(-3, 2), stage.Cells[0].Coord);
            Assert.AreEqual(PaintColor.Black, stage.Cells[0].Target);
        }

        [TestCase(@"{""name"":""t"",""cells"":[[0,0,8,0]]}")]              // 색 범위 밖
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,0,-1]]}")]             // 음수 색
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,1,1],[0,0,0,1]]}")]    // 좌표 중복
        [TestCase(@"{""cells"":[[0,0,1,1]]}")]                              // name 없음
        [TestCase(@"{""name"":""t""}")]                                      // cells 없음
        [TestCase(@"{""name"":""t"",""cells"":[]}")]                        // 칸 없음
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,1]]}")]                 // 칸 값 3개
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,1,1,1]]}")]             // 칸 값 5개
        [TestCase(@"{""name"":""t"",""cells"":[[0.5,0,1,1]]}")]             // 정수 아님
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,1,1]],""size"":3}")]    // 모르는 키
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,1,1]],""minMoves"":0}")] // 최소 수 0
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,1,1]]} x")]             // 뒤에 남은 내용
        [TestCase(@"{""name"":""t"",""cells"":[[0,0,1,1]]")]                // 닫히지 않음
        [TestCase("포도")]
        [TestCase("")]
        public void Parse_InvalidCode_ThrowsFormatException(string json)
        {
            Assert.Throws<FormatException>(() => Stage.Parse(json));
        }

        [Test]
        public void Parse_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Stage.Parse(null));
        }
    }
}
