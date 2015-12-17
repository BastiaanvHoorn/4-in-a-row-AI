using Engine;
using System;
using Server.Properties;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Util;
using System.Diagnostics;
using NLog;

namespace Server
{
    public static class RequestHandler
    {
        static Random rnd = new Random();
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /*public static byte get_column(Field field)
        {
            Settings.Default.Reload();
            using (Database db = new Database(Settings.Default.DbPath))
            {
                return get_column(field, db);
            }
        }*/

        /// <summary>
        /// Returns the move (column) that suits best with the given field (given situation). If the field is not included in the database a random column is returned.
        /// </summary>
        /// <param name="field">Current field</param>
        /// <returns>The best move (column) to do</returns>
        public static byte get_column(Field field, Database db)
        {
            if (db.isBusy())
            {
                logger.Info("Can't get column, because database is busy! Waiting...");
                while (db.isBusy())
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

                float[] chances = new float[field.Width];

                for (byte i = 0; i < field.Width; i++)
                {
                    chances[i] = fieldData.getWinningChance(i);
                }

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

                return bestColumn;
            }
            else
            {
                db.setBusy(false);
                return (byte)rnd.Next(7); //Returns a value between 0 and 6
            }
        }

        public static void update_field_data(DatabaseLocation fieldLocation, FieldData newData)
        {
            Settings.Default.Reload();
            using (Database db = new Database(Settings.Default.DbPath))
            {
                update_field_data(fieldLocation, newData, db);
            }
        }

        /// <summary>
        /// Updates the field data of the specifield field. The data changes are based on the move that has been done and if the player has won the game.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="moveColumn"></param>
        /// <param name="winning"></param>
        public static void update_field_data(DatabaseLocation fieldLocation, FieldData newData, Database db)
        {
            FieldData fieldData = db.readFieldData(fieldLocation);  // Reads the old field data from the database.

            // Edits the field data to the wanted values.
            fieldData += newData;

            db.writeFieldData(fieldLocation, fieldData);            // Writes the field data to the database.
        }

        /*public static int receive_game_history(byte[][] gameHistories)
        {
            Settings.Default.Reload();
            using (Database db = new Database(Settings.Default.DbPath))
            {
                return receive_game_history(gameHistories, db);
            }
        }*/

        public static int receive_game_history(byte[][] gameHistories, Database db)
        {
            logger.Info($"Processing {gameHistories.Length} games...");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Dictionary<Field, FieldData> history = new Dictionary<Field, FieldData>();

            for (int i = 0; i < gameHistories.Length; i++)
            {
                byte[] h = gameHistories[i];
                Field f = new Field(db.DbProperties.FieldWidth, db.DbProperties.FieldHeight);
                players turn = players.Alice;
                players winner = players.Empty;

                if (h[0] == (byte)Util.network_codes.game_history_alice)
                    winner = players.Alice;
                else if (h[0] == (byte)Util.network_codes.game_history_bob)
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
                logger.Info("Can't process game history, because database is busy! Waiting...");
                while (db.isBusy())
                    Thread.Sleep(100);
            }

            sw.Start();

            db.setBusy(true);
            db.processGameHistory(history);
            db.setBusy(false);

            sw.Stop();

            string deltaTime = sw.Elapsed.Minutes + "m and " + sw.Elapsed.Seconds + "s";
            logger.Info($"{history.Count} fields processed in {deltaTime}");

            return history.Count;
        }
    }
}
