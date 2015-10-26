using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Engine;
using Server;
using System.IO;
using System.Collections.Generic;

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

            byte[] expected = new byte[] { 1 + 2 + 8 + 16 + 64 + 128, 1 + 8, 0 };

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

            byte[] expected = new byte[] { 2 + 4 + 16 + 64 + 128, 1 + 4 + 8 + 16 + 64 + 128, 1 + 4 + 8 + 32 + 64 + 128, 4 + 8 + 16 + 64, 0 };

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

        [TestMethod]
        public void equalFields_Test_1()
        {
            byte[] f1 = new byte[] { 3, 5, 3, 2 };
            byte[] f2 = new byte[] { 3, 5, 3, 2 };

            bool actual = DatabaseHandler.equalFields(f1, f2);
            bool expected = true;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void equalFields_Test_2()
        {
            byte[] f1 = new byte[] { 3, 5, 3 };
            byte[] f2 = new byte[] { 3, 5, 3, 2 };

            bool actual = DatabaseHandler.equalFields(f1, f2);
            bool expected = false;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void equalFields_Test_3()
        {
            byte[] f1 = new byte[] { 3, 5, 1, 2 };
            byte[] f2 = new byte[] { 3, 5, 3, 2 };

            bool actual = DatabaseHandler.equalFields(f1, f2);
            bool expected = false;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void findField_Test_1()
        {
            using (var ms = new MemoryStream(new byte[] { 0 }))
            {
                Field f = new Field(new byte[11]);
                long actual = DatabaseHandler.findField(f, ms);
                long expected = 0;
                Assert.AreEqual(expected, actual);
            }
        }

        private static Field f0 = new Field(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        private static Field f1 = new Field(new byte[] { 5, 80, 5, 21, 80, 0, 0, 0, 0, 0, 0 });
        private static Field f2 = new Field(new byte[] { 5, 16, 0, 21, 80, 85, 1, 80, 1, 1, 0 });
        private static Field f3 = new Field(new byte[] { 85, 80, 5, 85, 80, 5, 85, 80, 5, 85, 0 });

        [TestMethod]
        public void findField_Test_2()
        {
            List<byte> memory = new List<byte>();
            memory.AddRange(f0.compressField());
            memory.AddRange(f1.compressField());
            memory.AddRange(f2.compressField());
            memory.AddRange(f3.compressField());
            
            using (var ms = new MemoryStream(memory.ToArray()))
            {
                long actual = DatabaseHandler.findField(f1, ms);
                long expected = 1;
                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void findField_Test_3()
        {
            List<byte> memory = new List<byte>();
            memory.AddRange(f0.compressField());
            memory.AddRange(f1.compressField());
            memory.AddRange(f2.compressField());
            memory.AddRange(f3.compressField());

            using (var ms = new MemoryStream(memory.ToArray()))
            {
                long actual = DatabaseHandler.findField(f2, ms);
                long expected = 2;
                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void findField_Test_4()
        {
            List<byte> memory = new List<byte>();
            memory.AddRange(f0.compressField());
            memory.AddRange(f1.compressField());
            memory.AddRange(f2.compressField());
            memory.AddRange(f3.compressField());

            using (var ms = new MemoryStream(memory.ToArray()))
            {
                long actual = DatabaseHandler.findField(f3, ms);
                long expected = 3;
                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void canAddField_Test_1()
        {
            Field newField = new Field(new byte[] { 0, 80, 0, 1, 0, 0, 21, 16, 0, 0, 0 });

            List<byte> memory = new List<byte>();
            memory.AddRange(f0.compressField());
            memory.AddRange(f1.compressField());
            memory.AddRange(f2.compressField());
            memory.AddRange(f3.compressField());

            using (var ms = new MemoryStream(memory.ToArray()))
            {
                bool actual = DatabaseHandler.fieldExists(newField, ms);
                bool expected = true;

                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void canAddField_Test_2()
        {
            Field newField = new Field(f1);

            List<byte> memory = new List<byte>();
            memory.AddRange(f0.compressField());
            memory.AddRange(f1.compressField());
            memory.AddRange(f2.compressField());
            memory.AddRange(f3.compressField());

            using (var ms = new MemoryStream(memory.ToArray()))
            {
                bool actual = DatabaseHandler.fieldExists(newField, ms);
                bool expected = false;

                Assert.AreEqual(expected, actual);
            }
        }
    }
}
