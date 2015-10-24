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
            player actual = f.getCellPlayer(1, 1);

            player expected = player.Alice;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestMethod1_setCell()
        {
            Field actual = new Field(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            actual.setCell(1, 1, player.Alice);

            Field expected = new Field(new byte[] { 0, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            //Assert.AreEqual(expected.Storage[1], actual.Storage[1]);
            CollectionAssert.AreEqual(expected.Storage, actual.Storage);
        }

        [TestMethod]
        public void TestMethod2_setCell()
        {
            Field actual = new Field(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            actual.setCell(1, 1, player.Bob);

            Field expected = new Field(new byte[] { 0, 128, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            //Assert.AreEqual(expected.Storage[1], actual.Storage[1]);
            CollectionAssert.AreEqual(expected.Storage, actual.Storage);
        }

        [TestMethod, ExpectedException(typeof(InvalidMoveException))]
        public void TestMethod3_setCell_invalid()
        {
            Field actual = new Field(new byte[] { 0, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            actual.setCell(1, 1, player.Bob);
        }

        [TestMethod]
        public void TestMethod1_getEmptyCell_1()
        {
            Field f = new Field();
            f.setCell(1, 0, player.Alice);
            f.setCell(1, 1, player.Bob);

            int actual = f.getEmptyCell(1);
            int expected = 2;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestMethod1_doMove_1()
        {
            Field actual = new Field();

            Field expected = new Field(actual);
            expected.setCell(1, 0, player.Alice);

            actual.doMove(1, player.Alice);

            CollectionAssert.AreEqual(expected.Storage, actual.Storage);
        }

        [TestMethod]
        public void TestMethod1_doMove_2()
        {
            Field actual = new Field();
            actual.setCell(1, 0, player.Alice);
            actual.setCell(1, 1, player.Bob);

            Field expected = new Field(actual);
            expected.setCell(1, 2, player.Alice);

            actual.doMove(1, player.Alice);

            CollectionAssert.AreEqual(expected.Storage, actual.Storage);
        }

        [TestMethod, ExpectedException(typeof(InvalidMoveException))]
        public void TestMethod1_doMove_3()
        {
            Field actual = new Field();
            for (int i = 0; i < 6; i++)
            {
                actual.setCell(1, i, player.Alice);
            }

            actual.doMove(1, player.Alice);
        }

        [TestMethod, ExpectedException(typeof(InvalidMoveException))]
        public void TestMethod1_doMove_4()
        {
            Field actual = new Field();
            
            actual.doMove(7, player.Alice);
        }
    }
}
