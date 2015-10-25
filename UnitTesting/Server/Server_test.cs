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

        [TestMethod]
        public void Compression_Test_2()
        {
            Field f = new Field();

            for (int i = 0; i < 6; i++)
            {
                f.setCell(2, i, (player)(i % 2 + 1));    //Column 3 (zero-based 2) with content (A = Alice, B = Bob): ABABAB
            }

            f.setCell(1, 0, player.Bob);
            f.setCell(3, 0, player.Alice);
            f.setCell(3, 1, player.Bob);
            f.setCell(4, 0, player.Bob);
            f.setCell(4, 1, player.Alice);
            f.setCell(5, 0, player.Bob);
            f.setCell(5, 1, player.Alice);
            f.setCell(5, 2, player.Alice);

            byte[] actual = DatabaseHandler.compressField(f);

            byte[] expected = new byte[] { 2 + 4 + 16 + 64 + 128, 1 + 4 + 8 + 16 + 64 + 128, 1 + 4 + 8 + 32 + 64 + 128, 4 + 8 + 16 + 64 };

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Compression_Test_3()
        {
            Field f = new Field();

            for (int x = 0; x < 7; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    f.setCell(x, y, (player)(y % 2 + 1));
                }
            }

            byte[] actual = DatabaseHandler.compressField(f);

            byte[] expected = new byte[11];
            for (int i = 0; i < 10; i++)
            {
                expected[i] = 1 + 4 + 8 + 16 + 64 + 128;
            }

            expected[10] = 1 + 4 + 8;

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
