using System;
using System.IO;
using Engine;

namespace Server
{
    public static class Extensions
    {
        /// <summary>
        /// fieldExists function for a specific stream, instead of a database Stream. (For unittesting)
        /// </summary>
        /// <param name="field">The field</param>
        /// <param name="s">The stream to read from</param>
        /// <returns></returns>
        internal static bool fieldExists(this Field field, Stream s)
        {
            int location = field.getFieldLocation(s);
            return location >= 0;
        }
        
        /// <summary>
        /// Returns the location (in fields) in the specified stream.
        /// </summary>
        /// <param name="field">The field</param>
        /// <param name="s">Stream to read from</param>
        /// <returns>Zero-based location</returns>
        public static int getFieldLocation(this Field field, Stream s)
        {
            return field.compressField().getFieldLocation(s);
        }

        /// <summary>
        /// Returns the location (in fields) in the specified stream.
        /// </summary>
        /// <param name="field">Storage of the field</param>
        /// <param name="s">Stream to read from</param>
        /// <returns>Zero-based location</returns>
        public static int getFieldLocation(this byte[] field, Stream s)
        {
            int fieldLength = field.Length;
            byte[] fieldStorage = new byte[fieldLength];

            byte[] bytes = new byte[s.Length];
            s.Read(bytes, 0, (int)s.Length);
            bool found;

            for (int i = 0; i < bytes.Length; i += fieldLength)
            {
                found = true;

                for (int j = 0; j < fieldLength; j++)
                {
                    if (bytes[i + j] != field[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                    return i / fieldLength;
            }

            return -1;
        }

        /// <summary>
        /// Returns the maximum storage size (in bytes) that could be needed for a field with the given dimensions.
        /// </summary>
        /// <param name="width">Field width</param>
        /// <param name="height">Field height</param>
        /// <returns>Max bytes needed per field</returns>
        public static byte getMaxStorageSize(byte width, byte height)
        {
            return (byte)Math.Ceiling((double)(width * height) / 4);
        }

        /// <summary>
        /// Returns the maximum storage size (in bytes) that could be needed for the given field.
        /// </summary>
        /// <param name="field"></param>
        /// <returns>Max bytes needed per field</returns>
        public static int getMaxStorageSize(this Field field)
        {
            return getMaxStorageSize(field.Width, field.Height);
        }

        /// <summary>
        /// Returns whether the given field storages are equal to eachother.
        /// </summary>
        /// <param name="field1"></param>
        /// <param name="field2"></param>
        /// <returns>Equality of fields</returns>
        internal static bool equalFields(byte[] field1, byte[] field2)
        {
            if (field1.Length != field2.Length)
                return false;

            for (byte i = 0; i < field1.Length; i++)
            {
                if (field1[i] != field2[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Compresses the given field by not storing empty cells in the field. WARNING: Don't try to compare a regular fields storage with a compressed fields storage! The format is totally different.
        /// </summary>
        /// <param name="field">The field to be compressed</param>
        /// <returns>The compressed field as a byte array</returns>
        internal static byte[] compressField(this Field field)
        {
            BitWriter bw = new BitWriter(field.getMaxStorageSize());

            for (int column = 0; column < field.Width; column++)
            {
                int cellValue = field.getCellValue(column, 0);
                int row = 0;

                while (row < field.Height && cellValue != 0)
                {
                    bw.append(1);               // 1 means that the cell is taken by a player.
                    bw.append(cellValue >> 1);  // This value represents which player has taken the cell.

                    row++;
                    cellValue = field.getCellValue(column, row);
                }

                if (row != field.Height)
                    bw.append(0);
            }

            return bw.getStorage();
        }
    }
}
