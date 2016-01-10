using System;
using Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTesting
{
    [TestClass]
    public class Field_test
    {
        [TestMethod]
        public void TestMethod1_getCell()
        {
            Field f = new Field(new byte[] { 0, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            players actual = f[1, 1];

            players expected = players.Alice;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestMethod1_setCell()
        {
            Field actual = new Field(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            actual[1, 1] = players.Alice;

            Field expected = new Field(new byte[] { 0, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            //Assert.AreEqual(expected.Storage[1], actual.Storage[1]);
            CollectionAssert.AreEqual(expected.Storage, actual.Storage);
        }

        [TestMethod]
        public void TestMethod2_setCell()
        {
            Field actual = new Field(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            actual[1, 1] = players.Bob;

            Field expected = new Field(new byte[] { 0, 128, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            //Assert.AreEqual(expected.Storage[1], actual.Storage[1]);
            CollectionAssert.AreEqual(expected.Storage, actual.Storage);
        }

        [TestMethod, ExpectedException(typeof(InvalidMoveException))]
        public void TestMethod3_setCell_invalid()
        {
            Field actual = new Field(new byte[] { 0, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            actual[1, 1] = players.Bob;
        }

        [TestMethod]
        public void TestMethod1_getEmptyCell()
        {
            Field f = new Field();
            f[1, 0] = players.Alice;
            f[1, 1] = players.Bob;

            int actual = f.getEmptyCell(1);
            int expected = 2;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void test_method2_getEmptyCell()
        {
            Field f = new Field();
            f.doMove(0, players.Alice);
            f.doMove(0, players.Bob);
            f.doMove(0, players.Alice);
            f.doMove(0, players.Bob);
            f.doMove(0, players.Alice);
            f.doMove(0, players.Bob);
            int actual = f.getEmptyCell(0);
            int expected = f.Height;
            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public void TestMethod1_doMove_1()
        {
            Field actual = new Field();

            Field expected = new Field(actual);
            expected[1, 0] = players.Alice;

            actual.doMove(1, players.Alice);

            CollectionAssert.AreEqual(expected.Storage, actual.Storage);
        }

        [TestMethod]
        public void TestMethod1_doMove_2()
        {
            Field actual = new Field();
            actual[1, 0] = players.Alice;
            actual[1, 1] = players.Bob;

            Field expected = new Field(actual);
            expected[1, 2] = players.Alice;

            actual.doMove(1, players.Alice);

            CollectionAssert.AreEqual(expected.Storage, actual.Storage);
        }

        [TestMethod, ExpectedException(typeof(InvalidMoveException))]
        public void TestMethod1_doMove_3()
        {
            Field actual = new Field();
            for (int i = 0; i < 6; i++)
            {
                actual[1, i] = players.Alice;
            }

            actual.doMove(1, players.Alice);
        }

        [TestMethod, ExpectedException(typeof(InvalidMoveException))]
        public void TestMethod1_doMove_4()
        {
            Field actual = new Field();

            actual.doMove(7, players.Alice);
        }

        [TestMethod]
        public void Field_Hashcode_test_1()
        {
            Field f = new Field(new byte[] { 1, 0, 0, 2, 0, 0, 3, 0, 0, 4, 0, 0 });

            Assert.AreEqual(67305985, f.GetHashCode());
        }

        [TestMethod]
        public void Field_Hashcode_test_2()
        {
            Field f = new Field(new byte[] { 255, 0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0 });

            Assert.AreEqual(-1, f.GetHashCode());
        }
    }
}
