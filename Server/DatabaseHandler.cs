using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;

namespace Server
{
    public static class DatabaseHandler
    {
        private static byte[] Bitmask = new byte[] { 3, 12, 48, 192 }; //This array is used for bit operations to read pairs of two bits. Binary values: 00000011, 00001100, 00110000, 11000000

        /*public static bool addField(Field field)
        {
            if (fieldExists(field))
            {
                throw new DatabaseException("Can't add field to database, because it already exists. Field content: " + field.ToString());
            }
            else
            {
                byte[] storage = field.getStorage();
            }
        }*/

        internal static bool fieldExists(Field field)
        {
            bool exists = false;



            return exists;
        }

        /// <summary>
        /// Compresses the given field by not storing empty cells in the field. WARNING: Don't try to compare a regular fields storage with a compressed fields storage! The format is totally different.
        /// </summary>
        /// <param name="field">The field to be compressed</param>
        /// <returns>The compressed field as a byte array</returns>
        internal static byte[] compressField(Field field)
        {
            BitWriter bw = new BitWriter();

            for (int column = 0; column < field.Width; column++)
            {
                int cellValue = 0;
                int row = 0;

                do
                {
                    cellValue = (byte)field.getCellValue(column, row);

                    if (cellValue > 0)
                    {
                        bw.append(1);               // 1 means that the cell is taken by a player.
                        bw.append(cellValue - 1);   // This value represents which player has taken the cell.
                    }
                    else
                    {
                        bw.append(0);               // 0 means that the cell is empty.
                        break;                      // We don't have to save additional information about the empty cell. Having said that it's empty in the previous step is enough.
                    }

                    row++;
                }
                while (row < field.Height && cellValue > 0);
            }

            return bw.getStorage();
        }
    }
}
