using System.Globalization;
using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    public class StageWriterTests
    {
        // 프로토타입 스테이지 기준 파일 9개: 읽고 다시 쓰면 파일 내용과 글자 하나까지 같다(끝 줄바꿈 제외)
        [TestCase("Grape")]
        [TestCase("TwoColors")]
        [TestCase("BrushChanges")]
        [TestCase("Honeycomb")]
        [TestCase("Crossing")]
        [TestCase("Stain")]
        [TestCase("MakeBlack")]
        [TestCase("Hive")]
        [TestCase("LastStroke")]
        public void RoundTrip_StageFiles(string file)
        {
            string text = TestStages.ReadPrototype(file).TrimEnd();
            Assert.AreEqual(text, StageWriter.ToJson(Stage.Parse(text)));
        }

        // 따옴표 · 역슬래시 · 제어 문자가 든 이름과 팔레트도 다시 읽힌다
        [Test]
        public void RoundTrip_EscapesAndOptionalFields()
        {
            string name = "a\"b" + (char)92 + "c" + (char)9 + "포도";
            var stage = new Stage(name, new[] { new StageCell(new HexCoord(-3, 2), PaintColor.Red, PaintColor.Black) }, "pastel", 4);

            Stage read = Stage.Parse(StageWriter.ToJson(stage));

            Assert.AreEqual(name, read.Name);
            Assert.AreEqual("pastel", read.Palette);
            Assert.AreEqual(4, read.MinMoves);
            Assert.AreEqual(new HexCoord(-3, 2), read.Cells[0].Coord);
            Assert.AreEqual(PaintColor.Black, read.Cells[0].Target);
        }

        // 음수 부호가 '-'가 아닌 문화권(U+2212)에서 써도 숫자는 Invariant — Stage.Parse가 다시 읽는다
        [Test]
        public void ToJson_IgnoresCurrentCultureNegativeSign()
        {
            var stage = new Stage("t", new[] { new StageCell(new HexCoord(-3, -2), PaintColor.Red, PaintColor.Red) }, null, null);
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NegativeSign = ((char)0x2212).ToString();
            CultureInfo previous = CultureInfo.CurrentCulture;
            string json;
            try
            {
                CultureInfo.CurrentCulture = culture;
                json = StageWriter.ToJson(stage);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }

            Assert.AreEqual(new HexCoord(-3, -2), Stage.Parse(json).Cells[0].Coord);
        }
    }
}
