using Engine;
using System;
using Server.Properties;

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
        public static void update_field_data(DatabaseLocation fieldLocation, byte moveColumn, bool winning)
        {
            Settings.Default.Reload();
            Database db = new Database(Settings.Default.DbPath);
            FieldData fieldData = db.readFieldData(fieldLocation);  // Reads the old field data from the database.
            
            // Edits the field data to the wanted values.
            fieldData.TotalCounts[moveColumn]++;
            if (winning)
                fieldData.WinningCounts[moveColumn]++;

            db.writeFieldData(fieldLocation, fieldData);            // Writes the field data to the database.
        }

        /*public static void receive_game(Field field)
        {
            Settings.Default.Reload();  // Gets the settings from the settings file so we can ask for database paths.
            string fieldFilePath = Settings.Default.FieldsDBPath;

            Database db = new Database();

            if (!Directory.GetParent(fieldFilePath).Exists) // If the directory of the database doesn't exist we create it.
            {
                Directory.CreateDirectory(Directory.GetParent(fieldFilePath).FullName);
            }

            int fieldLocation = db.findField(field);  // Gets the location of the field in the field database. (You could also call it the field index)

            if (fieldLocation == -1)    // Means that field doesn't exist and has to be added to the database.
            {
                db.addDatabaseItem(field);
            }

            update_field_data(fieldLocation, moveColumn, winning);    // Applies the new data to the field data database.
        }*/
    }
}
