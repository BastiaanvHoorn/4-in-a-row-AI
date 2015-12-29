using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Util;
using System.Diagnostics;
using NLog;
using System.IO;
using Util;

namespace Server
{
    /// <summary>
    /// This class is a bridge between Server.cs and Database.cs.
    /// </summary>
    public static class RequestHandler
    {
        static Random rnd = new Random();
        private static Logger logger = LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// Returns the move (column) that suits best with the given field (given situation). If the field is not included in the database a random column is returned.
        /// </summary>
        /// <param name="field">Current field</param>
        /// <returns>The best move (column) to perform</returns>
        public static byte get_column(this Database db, Field field)
        {
            if (db.isBusy() || db.BufferMgr.isProcessing())
            {
                logger.Debug("Can't get column, because database is busy! Waiting...");
                while (db.isBusy() || db.BufferMgr.isProcessing())
                    Thread.Sleep(100);
            }

            db.setBusy(true);

            DatabaseLocation location;
            if (db.fieldExists(field, out location))
            {
                FieldData fieldData = db.readFieldData(location);

                db.setBusy(false);

                float bestChance = -1;
                byte bestColumn = 0;

                for (byte i = 0; i < field.Width; i++)
                {
                    if (field.getEmptyCell(i) < field.Height)
                    {
                        float chance = fieldData.getWinningChance(i);
                        if (chance > bestChance)
                        {
                            bestChance = chance;
                            bestColumn = i;
                        }
                    }
                }

                logger.Debug("Returning column based on database");

                return bestColumn;
            }
            else
            {
                db.setBusy(false);

                logger.Debug("Returning random column");

                return field.getRandomColumn();
            }
        }

        /// <summary>
        /// Receives the game history and adds it to the buffer.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="gameHistories"></param>
        /// <returns>The filepath of the new buffer item</returns>
        public static string receive_game_history(this Database db, byte[] gameHistories)
        {
            return db.BufferMgr.addToBuffer(gameHistories);
        }

        /// <summary>
        /// Processes the gamehistory of the buffer file at the specified path.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="bufferPath"></param>
        /// <returns>The amount of fields processed</returns>
        public static int process_game_history(this Database db, string bufferPath)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            byte[] rawHistory = db.BufferMgr.getBufferContent(bufferPath);
            byte[][] gameHistories = game_history_util.linear_to_parrallel_game_history(rawHistory.ToList());

            string bufferName = Path.GetFileName(bufferPath);

            Dictionary<Field, FieldData> history = new Dictionary<Field, FieldData>();

            for (int i = 0; i < gameHistories.Length; i++)
            {
                byte[] h = gameHistories[i];
                Field f = new Field(db.DbProperties.FieldWidth, db.DbProperties.FieldHeight);
                players turn = players.Alice;
                players winner = players.Empty;

                if (h[0] == (byte)Util.Network_codes.game_history_alice)
                    winner = players.Alice;
                else if (h[0] == (byte)Util.Network_codes.game_history_bob)
                    winner = players.Bob;

                for (int j = 1; j < h.Length; j++)
                {
                    byte column = h[j];

                    FieldData fd = null;

                    if (!history.ContainsKey(f))
                    {
                        fd = new FieldData();
                        history.Add(new Field(f), fd);
                    }
                    else
                    {
                        fd = history[f];
                    }

                    fd.TotalCounts[column]++;
                    if (turn == winner)
                        fd.WinningCounts[column]++;

                    f.doMove(column, turn);

                    turn = turn == players.Alice ? players.Bob : players.Alice;
                }
            }

            if (db.isBusy())
            {
                sw.Stop();
                logger.Debug("Can't process game history, because database is busy! Waiting...");
                while (db.isBusy())
                    Thread.Sleep(100);
            }

            sw.Start();

            db.setBusy(true);
            db.processGameHistory(history);
            db.setBusy(false);

            sw.Stop();

            string deltaTime = sw.Elapsed.Minutes + "m and " + sw.Elapsed.Seconds + "s";
            logger.Info($"Processed \t{gameHistories.Length} games \t{history.Count} fields \t in {deltaTime}");

            return history.Count;
        }

        
    }
}
