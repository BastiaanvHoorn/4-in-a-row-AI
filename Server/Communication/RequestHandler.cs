using Engine;
using System;
using Server.Properties;
using System.Collections.Generic;

namespace Server
{
    public static class RequestHandler
    {
        static Random rnd = new Random();
        /// <summary>
        /// Returns the move (column) that suits best with the given field (given situation). If the field is not included in the database a random column is returned.
        /// </summary>
        /// <param name="field">Current field</param>
        /// <returns>The best move (column) to do</returns>
        public static byte get_column(Field field)
        {
            Settings.Default.Reload();
            Database db = new Database(Settings.Default.DbPath);
            DatabaseLocation location;
            if (db.fieldExists(field, out location))
            {
                FieldData fieldData = db.readFieldData(location);

                float bestChance = 0;
                byte bestColumn = 0;

                for (byte i = 0; i < 7; i++)
                {
                    float chance = fieldData.getWinningChance(i);
                    if (chance > bestChance)
                    {
                        bestChance = chance;
                        bestColumn = i;
                    }
                }

                return bestColumn;
            }
            else
            {
                return (byte)rnd.Next(7); //Returns a value between 0 and 6
            }
        }

        /// <summary>
        /// Updates the field data of the specifield field. The data changes are based on the move that has been done and if the player has won the game.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="moveColumn"></param>
        /// <param name="winning"></param>
        public static void update_field_data(DatabaseLocation fieldLocation, FieldData newData)
        {
            Settings.Default.Reload();
            Database db = new Database(Settings.Default.DbPath);
            FieldData fieldData = db.readFieldData(fieldLocation);  // Reads the old field data from the database.

            // Edits the field data to the wanted values.
            fieldData += newData;

            db.writeFieldData(fieldLocation, fieldData);            // Writes the field data to the database.
        }

        public static void receive_game_history(byte[][] gameHistories)
        {
            Settings.Default.Reload();  // Gets the settings from the settings file so we can ask for database paths.

            Database db = new Database(Settings.Default.DbPath);

            Dictionary<Field, FieldData> history = new Dictionary<Field, FieldData>();
            
            for (int i = 0; i < gameHistories.Length; i++)
            {
                byte[] h = gameHistories[i];
                Field f = new Field(db.DbProperties.FieldWidth, db.DbProperties.FieldHeight);
                players winner = h[0] == 253 ? players.Alice : players.Bob;
                players turn = players.Alice;

                for (int j = 0; j < h.Length; j++)
                {
                    byte column = h[j];

                    if (history.ContainsKey(f))
                        history.Add(new Field(f), new FieldData());

                    history[f].TotalCounts[column]++;
                    if (turn == winner)
                        history[f].WinningCounts[column]++;

                    f.doMove(column, turn);

                    turn = turn == players.Alice ? players.Bob : players.Alice;
                }
            }

            foreach (Field field in history.Keys)
            {
                DatabaseLocation dbLoc;
                if (!db.fieldExists(field, out dbLoc))      // If the field doesn't exist it has to be added to the database.
                {
                    dbLoc = db.addDatabaseItem(field);
                }

                update_field_data(dbLoc, history[field]);   // Applies the new data to the field data database.
            }
        }
    }
}
