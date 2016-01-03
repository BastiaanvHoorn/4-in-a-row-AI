//using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Engine;
//using Server;
//using System.IO;
//using System.Collections.Generic;
//using System.Diagnostics;

//namespace UnitTesting.ServerTest
//{
//    [TestClass]
//    public class Server_test
//    {
//        [TestMethod]
//        public void Compression_Test_1()
//        {
//            Field f = new Field();
//            f.setCell(0, 0, players.Bob);
//            f.setCell(1, 0, players.Bob);
//            f.setCell(2, 0, players.Bob);
//            f.setCell(2, 1, players.Alice);
//            f.setCell(3, 0, players.Alice);

//            byte[] actual = f.compressField();

//            byte[] expected = new byte[] { 1 + 2 + 8 + 16 + 64 + 128, 1 + 8, 0 };

//            CollectionAssert.AreEqual(expected, actual);
//        }

//        [TestMethod]
//        public void Compression_Test_2()
//        {
//            Field f = new Field();

//            for (int i = 0; i < 6; i++)
//            {
//                f.setCell(2, i, (players)(i % 2 + 1));    //Column 3 (zero-based 2) with content (A = Alice, B = Bob): ABABAB
//            }

//            f.setCell(1, 0, players.Bob);
//            f.setCell(3, 0, players.Alice);
//            f.setCell(3, 1, players.Bob);
//            f.setCell(4, 0, players.Bob);
//            f.setCell(4, 1, players.Alice);
//            f.setCell(5, 0, players.Bob);
//            f.setCell(5, 1, players.Alice);
//            f.setCell(5, 2, players.Alice);

//            byte[] actual = f.compressField();

//            byte[] expected = new byte[] { 2 + 4 + 16 + 64 + 128, 1 + 4 + 8 + 16 + 64 + 128, 1 + 4 + 8 + 32 + 64 + 128, 4 + 8 + 16 + 64, 0 };

//            CollectionAssert.AreEqual(expected, actual);
//        }

//        [TestMethod]
//        public void Compression_Test_3()
//        {
//            Field f = new Field();

//            for (int x = 0; x < 7; x++)
//            {
//                for (int y = 0; y < 6; y++)
//                {
//                    f.setCell(x, y, (players)(y % 2 + 1));
//                }
//            }

//            byte[] actual = f.compressField();

//            byte[] expected = new byte[11];
//            for (int i = 0; i < 10; i++)
//            {
//                expected[i] = 1 + 4 + 8 + 16 + 64 + 128;
//            }

//            expected[10] = 1 + 4 + 8;

//            CollectionAssert.AreEqual(expected, actual);
//        }

//        [TestMethod]
//        public void equalFields_Test_1()
//        {
//            byte[] f1 = new byte[] { 3, 5, 3, 2 };
//            byte[] f2 = new byte[] { 3, 5, 3, 2 };

//            bool actual = Extensions.equalFields(f1, f2);
//            bool expected = true;
//            Assert.AreEqual(expected, actual);
//        }

//        [TestMethod]
//        public void equalFields_Test_2()
//        {
//            byte[] f1 = new byte[] { 3, 5, 3 };
//            byte[] f2 = new byte[] { 3, 5, 3, 2 };

//            bool actual = Extensions.equalFields(f1, f2);
//            bool expected = false;
//            Assert.AreEqual(expected, actual);
//        }

//        [TestMethod]
//        public void equalFields_Test_3()
//        {
//            byte[] f1 = new byte[] { 3, 5, 1, 2 };
//            byte[] f2 = new byte[] { 3, 5, 3, 2 };

//            bool actual = Extensions.equalFields(f1, f2);
//            bool expected = false;
//            Assert.AreEqual(expected, actual);
//        }

//        [TestMethod]
//        public void findField_Test_1()
//        {
//            using (var ms = new MemoryStream(new byte[] { 0 }))
//            {
//                Field f = new Field(new byte[11]);
//                long actual = f.getFieldLocation(ms);
//                long expected = 0;
//                Assert.AreEqual(expected, actual);
//            }
//        }

//        private static Field f0 = new Field(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
//        private static Field f1 = new Field(new byte[] { 5, 80, 5, 21, 80, 0, 0, 0, 0, 0, 0 });
//        private static Field f2 = new Field(new byte[] { 5, 16, 0, 21, 80, 85, 1, 80, 1, 1, 0 });
//        private static Field f3 = new Field(new byte[] { 85, 80, 5, 85, 80, 5, 85, 80, 5, 85, 0 });
//        private static Field f4 = new Field(new byte[] { 85, 80, 5, 85, 80, 5, 85, 80, 5, 21, 0 });
//        private static Field f5 = new Field(new byte[] { 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 5 });

//        // These test are designed for the old db format.
//        /*[TestMethod]
//        public void findField_Test_2()
//        {
//            List<byte> memory = new List<byte>();
//            memory.AddRange(f0.compressField());
//            memory.AddRange(f1.compressField());
//            memory.AddRange(f2.compressField());
//            memory.AddRange(f3.compressField());
            
//            using (var ms = new MemoryStream(memory.ToArray()))
//            {
//                long actual = f1.getFieldLocation(ms);
//                long expected = 1;
//                Assert.AreEqual(expected, actual);
//            }
//        }

//        [TestMethod]
//        public void findField_Test_3()
//        {
//            List<byte> memory = new List<byte>();
//            memory.AddRange(f0.compressField());
//            memory.AddRange(f1.compressField());
//            memory.AddRange(f2.compressField());
//            memory.AddRange(f3.compressField());

//            using (var ms = new MemoryStream(memory.ToArray()))
//            {
//                long actual = f2.getFieldLocation(ms);
//                long expected = 2;
//                Assert.AreEqual(expected, actual);
//            }
//        }

//        [TestMethod]
//        public void findField_Test_4()
//        {
//            List<byte> memory = new List<byte>();
//            memory.AddRange(f0.compressField());
//            memory.AddRange(f1.compressField());
//            memory.AddRange(f2.compressField());
//            memory.AddRange(f3.compressField());

//            using (var ms = new MemoryStream(memory.ToArray()))
//            {
//                long actual = f3.getFieldLocation(ms);
//                long expected = 3;
//                Assert.AreEqual(expected, actual);
//            }
//        }

//        [TestMethod]
//        public void canAddField_Test_1()
//        {
//            Field newField = new Field(new byte[] { 0, 80, 0, 1, 0, 0, 21, 16, 0, 0, 0 });

//            List<byte> memory = new List<byte>();
//            memory.AddRange(f0.compressField());
//            memory.AddRange(f1.compressField());
//            memory.AddRange(f2.compressField());
//            memory.AddRange(f3.compressField());

//            using (var ms = new MemoryStream(memory.ToArray()))
//            {
//                bool actual = !newField.fieldExists(ms);
//                bool expected = true;

//                Assert.AreEqual(expected, actual);
//            }
//        }

//        [TestMethod]
//        public void canAddField_Test_2()
//        {
//            Field newField = new Field(f1);

//            List<byte> memory = new List<byte>();
//            memory.AddRange(f0.compressField());
//            memory.AddRange(f1.compressField());
//            memory.AddRange(f2.compressField());
//            memory.AddRange(f3.compressField());

//            using (var ms = new MemoryStream(memory.ToArray()))
//            {
//                bool actual = !newField.fieldExists(ms);
//                bool expected = false;

//                Assert.AreEqual(expected, actual);
//            }
//        }

//        [TestMethod]
//        public void database_addDatabaseItem_speedTest_1()
//        {
//            var dbProps = new DatabaseProperties(@"C:\Connect Four\db speed test 1", 7, 6);
//            var db = new Database(dbProps);

//            for (int i = 0; i < 1000; i++)
//                db.addDatabaseItem(f3);

//            db.addDatabaseItem(f2);
//        }

//        [TestMethod]
//        public void database_findField_speedTest_1()
//        {
//            var db = new Database(@"C:\Connect Four\db speed test 1");
//            int actual = db.findField(f2).Location;
//            int expected = 8001;
//            Assert.AreEqual(expected, actual);
//        }

//        [TestMethod]
//        public void database_addDatabaseItem_speedTest_2()  // ========== Run test finished: 1 run (0:01:52,7662723) ==========
//        {
//            var dbProps = new DatabaseProperties(@"C:\Connect Four\db speed test 2", 7, 6);
//            var db = new Database(dbProps);

//            for (int i = 0; i < 100000; i++)
//                db.addDatabaseItem(f3);

//            db.addDatabaseItem(f2);
//        }

//        [TestMethod]
//        public void database_findField_speedTest_2()
//        {
//            var db = new Database(@"C:\Connect Four\db speed test 2");
//            int actual = db.findField(f2).Location;
//            int expected = 800001;
//            Assert.AreEqual(expected, actual);
//        }*/

//        // The compressed lengths of f3 and f4 are the same. Necessary for these two tests.
//        /*[TestMethod]
//        public void database_addDatabaseItem_speedTest_3()  // ========== Run test finished: 1 run (0:04:46,4096759) ==========
//        {
//            var dbProps = new DatabaseProperties(@"C:\Connect Four\db speed test 3", 7, 6);
//            //var db = new Database(dbProps);

//            using (FileStream fieldStream = new FileStream(dbProps.getFieldDirPath(8) + "\\Fields.db", FileMode.OpenOrCreate, FileAccess.Write))
//            {
//                int bytes = 8 * 100000000;
//                fieldStream.Write(new byte[bytes], 0, bytes);
//            }
//        }*/

//        //  Speed test results
//        //  _______________________________________________
//        //  Scanned items (* 8 bytes)       Processing time
//        //  100,000                         70  - 80    ms
//        //  1,000,000                       100 - 110   ms
//        //  10,000,000                      600 - 700   ms
//        //  100,000,000                     ~ 5 - 6     s
//        //  1,000,000,000                   Not supported!

//        /*[TestMethod]
//        public void database_findField_speedTest_3()
//        {
//            var db = new Database(@"C:\Connect Four\db speed test 3");
//            int actual = db.findField(f3).Location;
//            int expected = 100000000;
//            Assert.AreEqual(expected, actual);
//        }

//        [TestMethod]
//        public void database_newFormat_test_2()
//        {
//            var db = new Database(new DatabaseProperties(@"C:\Connect Four\Database", 7, 6, 134217728));

//            for (int i = 0; i < 1000; i++)
//            {
//                db.addDatabaseItem(f3);
//            }

//            db.addDatabaseItem(f4);
//        }*/

//        [TestMethod]
//        public void gameHistory_processing_test_1()
//        {
//            string dbDir = @"C:\Connect Four\Simulation Learning";
//            if (!Directory.Exists(dbDir))
//            {
//                var dbProps = new DatabaseProperties(dbDir, 7, 6, 134217728);
//                Database.prepareNew(dbProps);
//            }

//            using (var db = new Database(dbDir))
//            {
//                Stopwatch sw = new Stopwatch();

//                int times = 0;
//                int dbLength = db.DbProperties.getTotalLength();

//                int gameCount = 1000;
//                int batchCount = 1;
//                var iterationTime = new TimeSpan();

//                while (times < 100)//DateTime.Now.Add(iterationTime) < new DateTime(2015, 12, 12, 18, 10, 0))
//                {
//                    sw.Restart();

//                    int aliceWon = 0;
//                    int bobWon = 0;
//                    var histories = new byte[gameCount * batchCount][];
//                    Random rnd = new Random();

//                    for (int j = 0; j < batchCount; j++)
//                    {
//                        for (int i = 0; i < gameCount; i++)
//                        {
//                            Game g = new Game(7, 6);

//                            while (g.has_won(players.Empty) && g.stones_count != 42)
//                            {
//                                string info = "";
//                                byte column = 0;
//                                //if (g.next_player == players.Alice)
//                                //{
//                                if (rnd.NextDouble() <= 0.8)
//                                {
//                                    column = RequestHandler.get_column(g.get_field(), db);
//                                }
//                                else
//                                {
//                                    column = (byte)rnd.Next(0, 7);
//                                }
//                                //}
//                                //else
//                                //{
//                                //    column = (byte)rnd.Next(0, 7);
//                                //}

//                                g.add_stone(column, g.next_player, ref info);
//                            }

//                            byte[] data = new byte[g.stones_count + 1];

//                            if (g.has_won(players.Alice))
//                            {
//                                aliceWon++;
//                                data[0] = (byte)Args_parser.Network_codes.game_history_alice;
//                            }
//                            else
//                            {
//                                bobWon++;
//                                data[0] = (byte)Args_parser.Network_codes.game_history_bob;
//                            }

//                            data[0] = g.has_won(players.Alice) ? (byte)Args_parser.Network_codes.game_history_alice : (byte)Args_parser.Network_codes.game_history_bob;
//                            Buffer.BlockCopy(g.history, 0, data, 1, g.stones_count);
//                            histories[j * gameCount + i] = data;
//                        }
//                    }

//                    var simulationTime = sw.Elapsed;
//                    sw.Restart();

//                    int fieldsProc = RequestHandler.receive_game_history(histories, db);

//                    var processingTime = sw.Elapsed;
//                    iterationTime = simulationTime + processingTime;

//                    times++;
//                    int newLength = db.DbProperties.getTotalLength();

//                    Trace.WriteLine("________________________________________________");
//                    Trace.WriteLine("Iteration " + times + " took " + iterationTime.Minutes + " m " + iterationTime.Seconds + " s");
//                    Trace.WriteLine("Batch count " + batchCount);
//                    Trace.WriteLine("Batch size " + gameCount);
//                    Trace.WriteLine("Games in iteration " + batchCount * gameCount);

//                    double percAlice = Math.Round((double)aliceWon / (batchCount * gameCount) * 100, 1);
//                    double percBob = Math.Round((double)bobWon / (batchCount * gameCount) * 100, 1);
//                    double ties = Math.Round(100 - percAlice - percBob, 1);
//                    Trace.WriteLine($"Game results | Alice won {percAlice}% | Bob won {percBob}% | Ties {ties}%");

//                    double avgSimSpeed = batchCount * gameCount / simulationTime.TotalMinutes;
//                    avgSimSpeed = Math.Round(avgSimSpeed, Math.Max(0, 2 - (int)Math.Log10(avgSimSpeed)));
//                    Trace.WriteLine("Average simulation speed " + avgSimSpeed + " games/minute");

//                    double avgProcSpeed = batchCount * gameCount / processingTime.TotalMinutes;
//                    avgProcSpeed = Math.Round(avgProcSpeed, Math.Max(0, 2 - (int)Math.Log10(avgProcSpeed)));
//                    Trace.WriteLine("Average processing speed " + avgProcSpeed + " games/minute");

//                    Trace.WriteLine("Fields processed " + fieldsProc);

//                    double avgFieldSpeed = fieldsProc / processingTime.TotalMinutes;
//                    avgFieldSpeed = Math.Round(avgFieldSpeed, Math.Max(0, 2 - (int)Math.Log10(avgFieldSpeed)));
//                    Trace.WriteLine("Average field processing speed " + avgFieldSpeed + " fields/minute");

//                    Trace.WriteLine("New fields added " + (newLength - dbLength));
//                    Trace.WriteLine("Currently " + newLength + " stored in database");
//                    Trace.WriteLine("Total games processed (since creation of database) " + db.readFieldData(f0).getOccuranceCount());

//                    dbLength = newLength;

//                    //if (DateTime.Now.Add(iterationTime) > new DateTime(2015, 12, 11, 6, 45, 0))
//                    //    break;
//                }
//            }
//        }
//    }
//}
