using System;
using Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTesting
{
    [TestClass]
    public class Rating_tests
    {
        [TestMethod]
        public void test_no_depth()
        {
            Field field = new Field();
            int[] actual = field.rate_columns(players.Alice, 0);
            int[] expected = { 3, 3, 3, 4, 3, 3, 3 };
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void test_depth()
        {
            Field field = new Field();
            int[] actual = field.rate_columns(players.Alice, 1);
            int[] expected = { -1, -1, -2, -1, -2, -1, -1 };
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
