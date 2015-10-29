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
    /// This class makes it easier to do operations related to the database.
    /// </summary>
    public static class DatabaseHandler
    {
        /// <summary>
        /// Returns whether the given location is an existing (valid to use) location, depending on the field data database filestream. WARNING: Do NOT use the field database filestream (Fields.db), only the field DATA database filestream (Fielddata.db).
        /// </summary>
        /// <param name="location">The location to check</param>
        /// <param name="fieldDataFs">The field data database filestream</param>
        /// <returns></returns>
        public static bool locationExists(int location, FileStream fieldDataFs)
        {
            int length = getDatabaseLength(fieldDataFs);
            return location >= 0 && location < length;
        }

        /// <summary>
        /// Returns the database length according to the length of the field data filestream. WARNING: Do NOT use the field database filestream (Fields.db), only the field DATA database filestream (Fielddata.db).
        /// </summary>
        /// <param name="fs">Field data database filestream</param>
        /// <returns></returns>
        public static int getDatabaseLength(FileStream fieldDataFs)
        {
            return (int)fieldDataFs.Length / 56;
        }

        /// <summary>
        /// Returns the position (in bytes) in the field data database where the data corresponding to the given field location is stored.
        /// </summary>
        /// <param name="fieldLocation">The location of the field</param>
        /// <param name="fieldDataFs">The field data database stream</param>
        /// <returns></returns>
        public static int getSeekPosition(int location, FileStream fieldDataFs)
        {
            if (!locationExists(location, fieldDataFs))
                throw new DatabaseException("Can't calculate seek position for field location -1, because this location doesn't exist");

            return location * 56;         // 56 bytes per 'field data' -> 14 uints = 56 bytes
        }

        /// <summary>
        /// Reads the field data from of the given field the database.
        /// </summary>
        /// <param name="field">Field to read the data from</param>
        /// <returns></returns>
        public static FieldData readFieldData(this Field field)
        {
            string fieldFilePath = Settings.Default.FieldsDBPath;
            using (FileStream fieldDataFs = new FileStream(fieldFilePath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                int location = findField(field, fieldDataFs);
                return readFieldData(location);
            }
        }

        /// <summary>
        /// Reads the field data from the database at the specified (field)location. (Not the location in bytes)
        /// </summary>
        /// <param name="location">Field location</param>
        /// <returns></returns>
        public static FieldData readFieldData(int location)
        {
            string fieldDataFilePath = Settings.Default.FieldDataDBPath;

            using (FileStream fieldDataFs = new FileStream(fieldDataFilePath, FileMode.OpenOrCreate, FileAccess.Read)) // Opens the field data database filestream in read mode.
            {
                if (!locationExists(location, fieldDataFs))
                    throw new DatabaseException("Can't read field data at field location -1, because this location doesn't exist.");

                int seekPosition = getSeekPosition(location, fieldDataFs);

                uint[] storage = new uint[14];
                using (BinaryReader br = new BinaryReader(fieldDataFs))
                {
                    fieldDataFs.Seek(seekPosition, SeekOrigin.Begin);   // Sets the reading position to the wanted byte (uint in our case) database.

                    for (byte i = 0; i < 14; i++)   // We read the uints one by one from the database.
                    {
                        storage[i] = br.ReadUInt32();
                    }
                }

                return new FieldData(storage);
            }
        }

        /// <summary>
        /// Writes the given field data to the database for the specified field.
        /// </summary>
        /// <param name="field">The field to save the data for</param>
        /// <param name="fieldData">The field data to write</param>
        public static void writeFieldData(this Field field, FieldData fieldData)
        {
            string fieldFilePath = Settings.Default.FieldsDBPath;
            using (FileStream fieldDataFs = new FileStream(fieldFilePath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                int location = findField(field, fieldDataFs);
                writeFieldData(location, fieldData);
            }
        }

        /// <summary>
        /// Writes the given field data to the database at the specified (field)location. (Not the location in bytes)
        /// </summary>
        /// <param name="location">Field location</param>
        /// <param name="fieldData">Data to be written</param>
        public static void writeFieldData(int location, FieldData fieldData)
        {
            string fieldDataFilePath = Settings.Default.FieldDataDBPath;

            using (FileStream fieldDataFs = new FileStream(fieldDataFilePath, FileMode.OpenOrCreate, FileAccess.Write)) // Opens the field data database filestream in write mode.
            {
                if (!locationExists(location, fieldDataFs))
                    throw new DatabaseException("Can't write field data at field location -1, because this location doesn't exist.");

                int seekPosition = getSeekPosition(location, fieldDataFs);

                using (BinaryWriter bw = new BinaryWriter(fieldDataFs))  // We use a BinaryWriter to be able to write uints directly to the stream.
                {
                    uint[] storage = fieldData.getStorage();

                    fieldDataFs.Seek(seekPosition, SeekOrigin.Begin);   // Sets the writing position to the wanted byte (uint in our case) in the database.
                    for (byte i = 0; i < 14; i++)                       // We write each uint of the storage to the database stream.
                    {
                        bw.Write(storage[i]);
                    }
                }
            }
        }
        
        /// <summary>
        /// Adds the given field to the database. WARNING: It's your own responsibility to check for the existance of a field in the database. Always AVOID adding fields that are already included in the database.
        /// </summary>
        /// <param name="field">Field to be added</param>
        public static void addDatabaseItem(this Field field)
        {
            string fieldFilePath = Settings.Default.FieldsDBPath;

            using (FileStream fieldFs = new FileStream(fieldFilePath, FileMode.OpenOrCreate, FileAccess.Write))     // Opens the field database filestream in write mode.
            {
                byte[] compressed = field.compressField();          // Gets the compressed field.
                fieldFs.Seek(0, SeekOrigin.End);                    // Sets the writing position to the end of the database.
                fieldFs.Write(compressed, 0, compressed.Length);    // Writes the bytes of the compressed field to the database.
            }

            string fieldDataPath = Settings.Default.FieldDataDBPath;

            using (FileStream fieldDataFs = new FileStream(fieldDataPath, FileMode.OpenOrCreate, FileAccess.Write)) // Opens the field data database filestream in write mode.
            {
                fieldDataFs.Seek(0, SeekOrigin.End);
                fieldDataFs.Write(new byte[56], 0, 56);
            }
        }

        /// <summary>
        /// Clears the database item (the field and its corresponding data) of the specified field from the database(s). This function doesn't delete the item, but sets all its values to zero.
        /// </summary>
        /// <param name="field">The field to be cleared</param>
        public static void clearDataBaseItem(this Field field)
        {
            int byteIndex;
            int fieldLength;
            int location = field.findField(out byteIndex, out fieldLength);

            string fieldDataFilePath = Settings.Default.FieldDataDBPath;
            using (FileStream fieldDataFs = new FileStream(fieldDataFilePath, FileMode.OpenOrCreate, FileAccess.Write)) // Opens the field data database filestream in write mode.
            {
                if (!locationExists(location, fieldDataFs))
                    throw new DatabaseException("Can't clear database item at field location -1, because this location doesn't exist.");

                fieldDataFs.Seek(getSeekPosition(location, fieldDataFs), SeekOrigin.Begin);
                fieldDataFs.Write(new byte[56], 0, 56);
            }

            string fieldFilePath = Settings.Default.FieldsDBPath;
            using (FileStream fieldFs = new FileStream(fieldFilePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fieldFs.Seek(byteIndex, SeekOrigin.Begin);
                fieldFs.Write(new byte[fieldLength], 0, fieldLength);
            }
        }

        /// <summary>
        /// Returns whether it's possible to add the given field to the stream. WARNING: In most cases it's better to do this check yourself, because the findField function could be quite expensive as the database grows.
        /// </summary>
        /// <param name="field">Field to check</param>
        /// <param name="s">Stream to get the data from</param>
        public static bool fieldExists(this Field field, Stream s)
        {
            return findField(field, s) == -1;   // findField returns -1 when the field is not included in the database. That's the value we want to be returned if we want to add the given field.
        }

        /// <summary>
        /// Returns where the given field is located in the field database. Return value -1 means the specified field is not included in the database.
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static int findField(this Field field)
        {
            string fieldFilePath = Settings.Default.FieldsDBPath;

            using (FileStream fieldFs = new FileStream(fieldFilePath, FileMode.OpenOrCreate, FileAccess.Read)) //  Gets the stream from the database file in read mode.
            {
                return findField(field, fieldFs);
            }
        }

        /// <summary>
        /// Returns where the given field is located in the field database. Return value -1 means the specified field is not included the database. out byteIndex represents the byte where the field storage starts.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int findField(this Field field, out int byteIndex, out int fieldLength)
        {
            string fieldFilePath = Settings.Default.FieldsDBPath;

            using (FileStream fieldFs = new FileStream(fieldFilePath, FileMode.OpenOrCreate, FileAccess.Read)) //  Gets the stream from the database file in read mode.
            {
                return findField(field, fieldFs, out byteIndex, out fieldLength);
            }
        }

        /// <summary>
        /// Returns where the given field is located in the specified stream. Return value -1 means the specified field is not included in the stream.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int findField(this Field field, Stream s)
        {
            int byteIndex;
            int fieldLength;
            return findField(field, s, out byteIndex, out fieldLength);
        }

        /// <summary>
        /// Returns where the given field is located in the specified stream. Return value -1 means the specified field is not included in the stream. out byteIndex represents the byte where the field storage starts.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        internal static int findField(this Field field, Stream s, out int byteIndex, out int fieldLength)
        {
            byte[] compressed = compressField(field);   // Gets the compressed version of the given field to compare it with database items.
            int fieldCounter = 0;                       // Counts how many fields we have found. Used as return value.
            int byteCounter = 0;

            int b = 0;                                  // The byte we're reading.
            int column = 0;                             // Current column
            int row = 0;                                // Current row
            byte bitPos = 0;                            // The bit we are reading from the current byte.
            byte storageCount = 0;                      // The amount of bytes needed for the current field.
            byte[] fieldStorage = new byte[11];         // Array to store the field in we're currently reading.

            #region Read bytes one by one
            while (s.Position != s.Length)              // Checks whether we've finished searching the database. If (b == -1) we know we've reached the end of the database.
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
                byteCounter++;
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
                        fieldLength = compressed.Length;
                        byteIndex = byteCounter - fieldLength;
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

            byteIndex = (int)s.Length;
            fieldLength = compressed.Length;
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
