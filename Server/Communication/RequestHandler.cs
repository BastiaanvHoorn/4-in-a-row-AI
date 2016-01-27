using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Utility;
using System.Diagnostics;
using NLog;
using System.IO;

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
            DatabaseLocation location;
            if (db.fieldExists(field, out location))
            {
                FieldData fieldData = db.readFieldData(location);

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

                logger.Debug($"Returning column based on database ({bestColumn})");

                return bestColumn;
            }
            else
            {
                byte column = field.getRandomColumn();
                logger.Debug($"Returning random column ({column})");

                return column;
            }
        }

        /// <summary>
        /// Processes the gamehistory of the buffer file at the specified path.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="bufferPath"></param>
        /// <returns>The amount of fields processed</returns>
        public static int preprocess_game_history(this Database db, byte[] rawHistory)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            byte[][] gameHistories = Game_history.linear_to_parrallel_game_history(rawHistory.ToList());

            byte maxFS = db.DbProperties.MaxFieldStorageSize;
            Dictionary<Field, FieldData>[] history = new Dictionary<Field, FieldData>[maxFS];
            for (byte i = 0; i < maxFS; i++)
            {
                history[i] = new Dictionary<Field, FieldData>();
            }

            for (int i = 0; i < gameHistories.Length; i++)
            {
                byte[] h = gameHistories[i];
                Field f = new Field(db.DbProperties.FieldWidth, db.DbProperties.FieldHeight);
                players turn = players.Alice;
                players winner = players.Empty;

                if (h[0] == Network_codes.game_history_alice)
                    winner = players.Alice;
                else if (h[0] == Network_codes.game_history_bob)
                    winner = players.Bob;

                for (int j = 1; j < h.Length; j++)
                {
                    byte column = h[j];

                    int fieldLength = f.compressField().Length;

                    FieldData fd = null;

                    if (!history[fieldLength - 1].ContainsKey(f))
                    {
                        fd = new FieldData();
                        history[fieldLength - 1].Add(new Field(f), fd);
                    }
                    else
                    {
                        fd = history[fieldLength - 1][f];
                    }

                    fd.TotalCounts[column]++;
                    if (turn == winner)
                        fd.WinningCounts[column]++;

                    f.doMove(column, turn);

                    turn = turn == players.Alice ? players.Bob : players.Alice;
                }
            }

            sw.Stop();

            string deltaTime = sw.Elapsed.Minutes + "m and " + sw.Elapsed.Seconds + "s";
            int fieldCount = history.Sum(h => h.Count);
            logger.Info($"Preprocessed \t{gameHistories.Length} games \t{fieldCount} fields \t in {deltaTime}");

            for (int i = 1; i <= maxFS; i++)
                db.BufferMgr.addBuffer(i, history[i - 1]);

            return fieldCount;
        }


    }
}
