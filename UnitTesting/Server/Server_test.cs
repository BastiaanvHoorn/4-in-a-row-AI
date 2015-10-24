using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Engine;
using Server;

namespace UnitTesting.Server
{
    [TestClass]
    public class Server_test
    {
        [TestMethod]
        public void Compression_Test_1()
        {
            Field f = new Field();
            f.setCell(0, 0, player.Bob);
            f.setCell(1, 0, player.Bob);
            f.setCell(2, 0, player.Bob);
            f.setCell(2, 1, player.Alice);
            f.setCell(3, 0, player.Alice);

            byte[] actual = DatabaseHandler.compressField(f);

            byte[] expected = new byte[] { 1 + 2 + 8 + 16 + 64 + 128, 1 + 8 };

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
