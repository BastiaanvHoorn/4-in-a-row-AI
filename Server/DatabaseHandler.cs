using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Engine;
using Server.Properties;

namespace Server
{
    /// <summary>
    /// This class makes it easier to do operations related to the database
    /// </summary>
    public static class DatabaseHandler
    {
        /// <summary>
        /// Adds the given field to the database.
        /// </summary>
        /// <param name="field">Field to be added</param>
        public static void addField(Field field)
        {
            Settings.Default.Reload();
            using (FileStream fs = new FileStream(Settings.Default.FieldsDBPath, FileMode.OpenOrCreate, FileAccess.ReadWrite)) //  Gets the stream from the database file in read mode.
            {
                addField(field, fs);
            }
        }

        /// <summary>
        /// Adds the given field to the database.
        /// </summary>
        /// <param name="field">Field to be added</param>
        public static void addField(Field field, Stream s)
        {
            long fieldLocation = findField(field, s);  // Gets the location of field in database.

            if (fieldLocation > -1) // Means field is not found in database and we can add it to the database.
            {
                //byte[] compressed = compressField(field);
            }
            else                    // Means field already exists and can't be added again.
            {
                throw new DatabaseException("Can't add field to database, because it already exists. Field content: " + field.ToString());
            }
        }

        /*/// <summary>
        /// Returns where the given field is located in the database. Return value -1 means the specified field is not present in the database.
        /// </summary>
        /// <param name="field"></param>
        /// <returns>Field position (zero-based) in database</returns>
        internal static long findField(Field field)
        {
            Settings.Default.Reload();
            using (FileStream fs = new FileStream(Settings.Default.FieldsDBPath, FileMode.Open)) //  Gets the stream from the database file in read mode.
            {
                return findField(field, fs);
            }
        }*/

        /// <summary>
        /// Returns where the given field is located in the specified stream. Return value -1 means the specified field is not present in the stream.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        internal static long findField(Field field, Stream s)
        {
            byte[] compressed = compressField(field);   // Gets the compressed version of the given field to compare it with database items.
            long fieldCounter = 0;                      // Counts how many fields we have found. Used as return value.

            int b = 0;                              // The byte we're reading.
            int column = 0;                         // Current column
            int row = 0;                            // Current row
            byte bitPos = 0;                        // The bit we are reading from the current byte.
            byte storageCount = 0;                  // The amount of bytes needed for the current field.
            byte[] fieldStorage = new byte[11];     // Array to store the field in we're currently reading.

            #region Read bytes one by one
            while (s.Position != s.Length)                  // Checks whether we've finished searching the database. If (b == -1) we know we've reached the end of the database.
            {
                #region Here we read one byte
                b = s.ReadByte();                           // Read the next byte from the stream
                byte[] bits = BitReader.getBits((byte)b);   // Gets the individual bits from the current byte.
                while (bitPos < 8 && column < field.Width)  // Loops until we have to go on to the next byte. If (column < field.width) we know the current byte is the last byte of the current field.
                {
                    if (bits[bitPos] == 0)                  // If the bit is 0 it means we've reached the end of the column.
                    {
                        bitPos++;                           // The next relevant bit is just the next one.
                        column++;                           // We move on to the next column.
                        row = 0;                            // We reset the row counter to 0, since we move on to the next column
                    }
                    else                                    // If the bit is 1 it means we're reading a cell value (not the end of a column).
                    {
                        bitPos += 2;                        // The next relevant bit is located two bits beyond, because the next bit holds the information about which player owns the cell.
                        row++;                              // We move on to the next row.
                        if (row == field.Height)            // Checks whether we've reached the end of the current column.
                        {
                            column++;                       // If we have reached the end of the column, we move on to the next one.
                            row = 0;                        // A new column means we start at row 0 again.
                        }
                    }

                    if (column == field.Width)          // If (column == width) we know we've reached the end of the current field.
                    {
                        break;
                    }
                }

                fieldStorage[storageCount] = (byte)b;   // Stores the current byte in the current fieldStorage.
                storageCount++;                         // This counter is used to know how many bytes the current field consists till now.
                #endregion

                #region Here we check whether we've reached the end of a field
                if (column < field.Width)                   // This means there is more field data to come.
                {
                    bitPos -= 8;                            // We've just finished reading the current byte and the bitposition needs to be set in order to start reading the next byte at the right bit.
                }
                else                                        // This means the current field ends here.
                {                     // We know the storage of the current field ends here. We know that the next field is stored in a new byte, so bitPosition is reset to 0.
                    Array.Resize(ref fieldStorage, storageCount);   // At the time when we declare array fieldStorage we set its length to the maximum amount of bytes that could be needed (11 in a 6x7 field). Here we resize it to the actual (compressed) length.

                    if (equalFields(compressed, fieldStorage))      // Checks whether the given field and the field we've just found are equal.
                    {
                        return fieldCounter;                // If we find the specified field we return the fields zero-based location in the database.
                    }

                    bitPos = 0;                             // Reset bitPos
                    column = 0;                             // Reset column
                    row = 0;                                // Reset row
                    fieldStorage = new byte[11];            // Reset fieldStorage (create new instance)
                    storageCount = 0;                       // Reset storageCount

                    fieldCounter++;                         // To be able to return the given fields location in the database (if present), we have to count how many fields we've found before.
                }
                #endregion

                //byteCounter++;
            }
            #endregion

            return -1;          //This return statement is only used when the field is not present in the database. So -1 means not found.
        }

        /// <summary>
        /// Returns whether the given field storages are equal to eachother.
        /// </summary>
        /// <param name="field1"></param>
        /// <param name="field2"></param>
        /// <returns>Equality of fields</returns>
        internal static bool equalFields(byte[] field1, byte[] field2)
        {
            bool result = true; // Result is set to false if it turns out that the fields are not equal.

            if (field1.Length != field2.Length)
                return false;

            for (byte i = 0; i < field1.Length; i++)
            {
                if (field1[i] != field2[i])
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Compresses the given field by not storing empty cells in the field. WARNING: Don't try to compare a regular fields storage with a compressed fields storage! The format is totally different.
        /// </summary>
        /// <param name="field">The field to be compressed</param>
        /// <returns>The compressed field as a byte array</returns>
        internal static byte[] compressField(this Field field)
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
