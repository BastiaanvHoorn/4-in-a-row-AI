using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _4_in_a_row_;

namespace UnitTesting
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1_getPlayer()
        {
            Field f = new Field(new byte[] { 0, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            player actual = f.getPlayer(1, 1);

            player expected = player.Alice;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestMethod1_setPlayer()
        {
            Field actual = new Field(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            actual.setPlayer(1, 1, player.Alice);

            Field expected = new Field(new byte[] { 0, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            //Assert.AreEqual(expected.Storage[1], actual.Storage[1]);
            CollectionAssert.AreEqual(expected.Storage, actual.Storage);
        }

        [TestMethod]
        public void TestMethod2_setPlayer()
        {
            Field actual = new Field(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            actual.setPlayer(1, 1, player.Bob);

            Field expected = new Field(new byte[] { 0, 128, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            //Assert.AreEqual(expected.Storage[1], actual.Storage[1]);
            CollectionAssert.AreEqual(expected.Storage, actual.Storage);
        }

        [TestMethod, ExpectedException(typeof(InvalidMoveException))]
        public void TestMethod3_setPlayer_invalid()
        {
            Field actual = new Field(new byte[] { 0, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            actual.setPlayer(1, 1, player.Bob);
        }
    }
}
