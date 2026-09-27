using System;
using NUnit.Framework;

namespace ColoringBoot.Core.Tests
{
    public class HexCoordTests
    {
        private static readonly HexDirection[] _allDirections = (HexDirection[])Enum.GetValues(typeof(HexDirection));

        // CLAUDE.md §3 방향 표
        [TestCase(HexDirection.Clock1, 1, -1)]
        [TestCase(HexDirection.Clock3, 1, 0)]
        [TestCase(HexDirection.Clock5, 0, 1)]
        [TestCase(HexDirection.Clock7, -1, 1)]
        [TestCase(HexDirection.Clock9, -1, 0)]
        [TestCase(HexDirection.Clock11, 0, -1)]
        public void Delta_MatchesDirectionTable(HexDirection dir, int dq, int dr)
        {
            Assert.AreEqual(new HexCoord(dq, dr), dir.Delta());
        }

        // 반대 방향 쌍(1↔7, 3↔9, 5↔11)은 좌표 변화가 반대이고 같은 축이다
        [TestCase(HexDirection.Clock1, HexDirection.Clock7)]
        [TestCase(HexDirection.Clock3, HexDirection.Clock9)]
        [TestCase(HexDirection.Clock5, HexDirection.Clock11)]
        public void OppositePair_HasReversedDeltaAndSameAxis(HexDirection dir, HexDirection opposite)
        {
            Assert.AreEqual(new HexCoord(-dir.Delta().Q, -dir.Delta().R), opposite.Delta());
            Assert.AreEqual(dir.Axis(), opposite.Axis());
        }

        // 같은 줄: 줄의 축 방향으로는 몇 칸을 가도 키가 같고, 다른 축 방향으로 가면 달라진다
        [Test]
        public void LineKey_SameOnlyAlongItsAxis()
        {
            var origin = new HexCoord(2, -1);
            foreach (HexDirection dir in _allDirections)
            {
                foreach (HexDirection step in _allDirections)
                {
                    var moved = new HexCoord(origin.Q + step.Delta().Q * 3, origin.R + step.Delta().R * 3);
                    bool sameLine = origin.LineKey(dir) == moved.LineKey(dir);
                    Assert.AreEqual(step.Axis() == dir.Axis(), sameLine, $"{dir} 줄에서 {step} 쪽으로 3칸");
                }
            }
        }

        // 포도(GDD §3)의 1시·7시 줄: (1,3) (2,2) (3,1) (4,0)
        [Test]
        public void LineKey_GrapeClock1Line()
        {
            int key = new HexCoord(3, 1).LineKey(HexDirection.Clock1);
            Assert.AreEqual(key, new HexCoord(1, 3).LineKey(HexDirection.Clock7));
            Assert.AreEqual(key, new HexCoord(2, 2).LineKey(HexDirection.Clock1));
            Assert.AreEqual(key, new HexCoord(4, 0).LineKey(HexDirection.Clock1));
            Assert.AreNotEqual(key, new HexCoord(3, 2).LineKey(HexDirection.Clock1));
        }

        // dir 쪽으로 한 칸 가면 Along(dir)이 커진다 → 오름차순이 반대편 끝에서 dir 끝으로 가는 순서
        [Test]
        public void Along_GrowsTowardDirection()
        {
            var cell = new HexCoord(2, -1);
            foreach (HexDirection dir in _allDirections)
            {
                var next = new HexCoord(cell.Q + dir.Delta().Q, cell.R + dir.Delta().R);
                Assert.Greater(next.Along(dir), cell.Along(dir), dir.ToString());
            }
        }
    }
}
