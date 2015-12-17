using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Engine
{
    public static class Extensions
    {
        private static Random rnd = new Random();

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
            s.Seek(0, SeekOrigin.Begin);

            byte[] fStorage = field.compressField();

            byte[] bytes = new byte[s.Length];
            s.Read(bytes, 0, (int)s.Length);

            return fStorage.getFieldLocation(bytes);
        }

        /// <summary>
        /// Returns the location (in fields) in the specified byte array.
        /// </summary>
        /// <param name="field">Storage of the field</param>
        /// <param name="s">Stream to read from</param>
        /// <returns>Zero-based location</returns>
        public static int getFieldLocation(this byte[] field, byte[] bytes)
        {
            int result = -1;
            int fieldLength = field.Length;

            Parallel.For(0, bytes.Length / fieldLength, (i, loopState) =>
            {
                bool found = true;

                for (int j = 0; j < fieldLength; j++)
                {
                    if (bytes[i * fieldLength + j] != field[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    result = i;
                    loopState.Break();
                }
            });

            return result;
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
        public static bool equalFields(byte[] field1, byte[] field2)
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
        public static byte[] compressField(this Field field)
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

        public static Field decompressField(this byte[] storage, byte width = 7, byte height = 6)
        {
            Field f = new Field(width, height);
            byte row = 0;
            byte column = 0;

            BitReader br = new BitReader(storage);
            int bit = br.readBit();

            while (bit != -1 && column < width)
            {
                if (bit == 0)
                {
                    row = 0;
                    column++;
                }
                else
                {
                    row++;
                    f.doMove(column, (players)(br.readBit() + 1));

                    if (row == height)
                    {
                        row = 0;
                        column++;
                    }
                }

                bit = br.readBit();
            }

            return f;
        }

        /// <summary>
        /// Returns a valid, random move. (No moves that don't fit the fields height)
        /// </summary>
        /// <param name="field"></param>
        /// <returns>Valid random column</returns>
        public static byte getRandomColumn(this Field field)
        {
            List<byte> columns = new List<byte>();

            for (byte i = 0; i < field.Width; i++)
            {
                if (field.getEmptyCell(i) < field.Height)
                    columns.Add(i);
            }

            int random = rnd.Next(columns.Count);
            byte column = columns[random];
            
            return columns[random];
        }

        /// <summary>
        /// count the amount of consecutive stones of one player in the given direction
        /// </summary>
        /// <param name="x">the x-coordinate of the start</param>
        /// <param name="y">the y-cooordinate of the start</param>
        /// <param name="dx">the direction in x (can only be -1, 0, or -1)</param>
        /// <param name="dy">the direction in y (can only be -1, 0, or -1)</param>
        /// <param name="ab">the player of which the stones should be counted (1 for alice, 2 for bob)</param>
        /// <returns>The amount of stones from player ab found, not counting the starting stone</returns>
        public static byte count_for_win_direction(this Field field, byte x, byte y, sbyte dx, sbyte dy, players player)
        {
            byte counter = 0;
            sbyte _x = (sbyte)x;
            sbyte _y = (sbyte)y;
            while (true)
            {
                _x += dx;
                _y += dy;
                if (_x < 0 || _x >= field.Width || _y < 0 || _y >= field.Height)
                {
                    break;
                }
                if (field.getCellPlayer(_x, _y) != player)
                {
                    break;
                }
                counter++;
            }
            return counter;
        }

        /// <summary>
        /// Checks if someone has won
        /// </summary>
        /// <p>
        /// Checks in each direction from the given stone if it can make a whole row. If so, the variable winning will be changed
        /// </p>
        /// <param name="x">The x of the given stone</param>
        /// <param name="y">The y of the given stone</param>
        /// <param name="player">1 for alice, 2 for bob</param>
        public static bool check_for_win(this Field field, byte x, byte y, players player)
        {
            //Checks from botleft to topright
            byte counter = 1;
            counter += field.count_for_win_direction(x, y, -1, 1, player);
            counter += field.count_for_win_direction(x, y, 1, -1, player);
            if (counter >= 4)
            {
                return true;
            }

            //checks from topleft to botright
            counter = 1;
            counter += field.count_for_win_direction(x, y, 1, 1, player);
            counter += field.count_for_win_direction(x, y, -1, -1, player);
            if (counter >= 4)
            {
                return true;
            }

            //checks horizontal
            counter = 1;
            counter += field.count_for_win_direction(x, y, 0, 1, player);
            counter += field.count_for_win_direction(x, y, 0, -1, player);
            if (counter >= 4)
            {
                return true;
            }

            //checks vertical
            counter = 1;
            counter += field.count_for_win_direction(x, y, -1, 0, player);
            counter += field.count_for_win_direction(x, y, 1, 0, player);
            if (counter >= 4)
            {
                return true;
            }

            return false;
        }
    }
}
