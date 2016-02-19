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
        /// Returns the move (column) that suits best with the given field (given situation) and supports dynamic learning. If the field is not included in the database a random column is returned.
        /// </summary>
        /// <param name="field">Current field</param>
        /// <returns>The best move (column) to perform</returns>
        public static byte get_column(this Database db, Field field, uint dynamLearning = 0)
        {
            DatabaseLocation location;
            if (db.fieldExists(field, out location))    // Checks if the given field exists in the database.
            {
                FieldData fieldData = db.readFieldData(location);   // Gets the field data from the database.
                byte bestColumn = 0;
                bool executeDynam = false;  // A boolean that indicates whether a random column needs to be returned in order to use dynamic learning or a column based on the database.

                if (dynamLearning != 0)
                {
                    // Calculates the reliability of the given field.
                    double reliability = fieldData.getReliability(dynamLearning);

                    if (rnd.NextDouble() > reliability) // The reliability represents the chance that a random column is chosen, instead of a 'smart' column according to the database.
                        executeDynam = true;
                }

                if (!executeDynam)  // Not dynamic means based on database.
                {
                    float bestChance = -1;

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
                }
                else    // Dynamic means random.
                {
                    bestColumn = field.getRandomColumn();

                    logger.Debug($"Returning column based on dynamic learning ({bestColumn})");
                }

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
        /// Returns the reliablitiy of the fielddata, based on the given 'completeness' constant.
        /// </summary>
        /// <param name="fd"></param>
        /// <param name="completenessConst">This constant defines the occurancecount that we call reliable.</param>
        /// <returns></returns>
        public static double getReliability(this FieldData fd, uint completenessConst)
        {
            uint reliability = 0;
            int fieldWidth = fd.TotalCounts.Length;

            for (byte i = 0; i < fieldWidth; i++)
            {
                reliability += fd.getOccuranceCount(i);
            }

            return (double)reliability / (double)(fieldWidth * completenessConst);
        }

        /// <summary>
        /// Processes the given gamehistory into a buffer item and adds it to the Buffer folder.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="bufferPath"></param>
        /// <returns>The amount of fields processed</returns>
        public static int preprocess_game_history(this Database db, byte[] rawHistory)
        {
            logger.Info("Starting preprocessing game history");
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

            db.DbProperties.increaseCycles();
            db.DbProperties.increaseGames(gameHistories.Length);
            db.DbProperties.writeProperties();

            int fieldCount = history.Sum(h => h.Count);
            logger.Info($"Preprocessed \t{gameHistories.Length} games \t{fieldCount} fields \t in {deltaTime}");

            for (int i = 1; i <= maxFS; i++)
                db.BufferMgr.addBuffer(i, history[i - 1]);

            return fieldCount;
        }
    }
}
