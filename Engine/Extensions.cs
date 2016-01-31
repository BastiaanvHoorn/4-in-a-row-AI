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
        /// Gets the first empty cell from the bottom of the given column.
        /// </summary>
        /// <param name="column"></param>
        /// <returns>Returns the Y-coord of the empty cell</returns>
        public static byte getEmptyCell(this Field field, int column)
        {
            if (field.Height == 6)
            {
                int value = 0;

                if ((column & 1) == 0)    //% 2. Even or odd.
                {
                    int startByte = column / 2 * 3;
                    value = field.Storage[startByte] + 256 * (field.Storage[startByte + 1] & 15); //    We need all cells (4) that are stored in the start byte and the first 2 cells that are stored in the next byte, to get the total column value.
                }
                else
                {
                    int startByte = column / 2 * 3 + 1;
                    value = ((field.Storage[startByte] & 240) >> 4) + 16 * (field.Storage[startByte + 1]); // We need the 2 last cells stored in the first byte and all cells that are stored in the next byte, to get the total column value.
                }

                byte cell = 0;
                while (value > 0) //    Every iteration in the while loop we shift value with 2 bits. When value is 0, we know that every bit in value is 0 and all remaining cells in the column are empty.
                {
                    cell++;      //Every iteration row is increased by 1. Cell represents how many bitshifts were necessary to make value 0. This means that cell is the first empty cell in the given column.
                    value >>= 2; // column2 /= 4. Each bitshift a cell is wiped, and only the cells above remain.
                }

                return cell;
            }
            throw new NotImplementedException("No support for fields with a heights other than 6");
        }

        public static byte get_total_empty_columns(this Field field)
        {
            byte empty_cols = 0;
            for (int i = 0; i < field.Width; i++)
            {
                if (field.getEmptyCell(i) < field.Height)
                    empty_cols++;
            }
            return empty_cols;
        }

        public static byte[] get_empty_column_indices(this Field field)
        {
            byte[] indices = new byte[field.get_total_empty_columns()];
            int col = 0;
            for (byte i = 0; i < field.Width; i++)
            {
                if (field.getEmptyCell(i) < field.Height)
                {
                    indices[col] = i;
                    col++;
                }
            }
            return indices;
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

            return column;
        }

        /// <summary>
        /// count the amount of consecutive stones of one player in the given direction
        /// </summary>
        /// <param name="x">the x-coordinate of the start</param>
        /// <param name="y">the y-cooordinate of the start</param>
        /// <param name="dx">the direction in x (can only be -1, 0 or 1)</param>
        /// <param name="dy">the direction in y (can only be -1, 0 or 1)</param>
        /// <param name="ab">the player of which the stones should be counted (1 for alice, 2 for bob)</param>
        /// <returns>The amount of stones from player ab found, not counting the starting stone</returns>
        public static byte count_stones_direction(this Field field, byte x, byte y, sbyte dx, sbyte dy, players player, bool count_empty = false)
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
                if (field[_x, _y] == (player == players.Alice ? players.Bob : players.Alice) || // If the current cell is occupied by the other player
                    (!count_empty && field[_x, _y] == players.Empty)) // Or if the occupied cell is empty but we're not counting empty ones
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
        /// Checks in each direction from the given stone if it can make a whole row.
        /// </p>
        /// <param name="x">The x of the given stone</param>
        /// <param name="y">The y of the given stone</param>
        /// <param name="player">1 for alice, 2 for bob</param>
        public static bool check_for_win(this Field field, byte x, byte y, players player)
        {
            //Checks from botleft to topright
            byte counter = 1;
            counter += field.count_stones_direction(x, y, -1, 1, player);
            counter += field.count_stones_direction(x, y, 1, -1, player);
            if (counter >= 4)
            {
                return true;
            }

            //checks from topleft to botright
            counter = 1;
            counter += field.count_stones_direction(x, y, 1, 1, player);
            counter += field.count_stones_direction(x, y, -1, -1, player);
            if (counter >= 4)
            {
                return true;
            }

            //checks horizontal
            counter = 1;
            counter += field.count_stones_direction(x, y, 0, 1, player);
            counter += field.count_stones_direction(x, y, 0, -1, player);
            if (counter >= 4)
            {
                return true;
            }

            //checks vertical
            counter = 1;
            counter += field.count_stones_direction(x, y, -1, 0, player);
            counter += field.count_stones_direction(x, y, 1, 0, player);
            if (counter >= 4)
            {
                return true;
            }

            return false;
        }

        private static int rate_row(this Field field, players player, int x, int y, int dx, int dy)
        {
            int rating = 0;
            int counter = 0;
            int empty = 0;
            while (x >= 0 && x < field.Width && y >= 0 && y < field.Height)
            {
                if (field[x, y] == player)
                {
                    counter++;
                }
                else if (field[x, y] == players.Empty)
                {
                    empty++;
                }
                else
                {
                    if (counter + empty >= 4)
                    {
                        rating += counter;
                    }
                    counter = 0;
                    empty = 0;
                }
                x += dx;
                y += dy;
            }
            if (counter + empty >= 4)
            {
                rating += counter;
            }
            return rating;
        }

        public static int rate_field(this Field field)
        {
            int rating = 0;
            // Vertical
            for (int x = 0; x < field.Width; x++)
            {
                rating += field.rate_row(players.Alice, x, 0, 0, 1);
                rating -= field.rate_row(players.Bob, x, 0, 0, 1);
            }
            // Horizontal
            for (int y = 0; y < field.Height; y++)
            {
                rating += field.rate_row(players.Alice, 0, y, 1, 0);
                rating -= field.rate_row(players.Bob, 0, y, 1, 0);
            }
            // Diagonals starting starting at the leftside
            for (int y = 0; y < field.Height - 3; y++)
            {
                // Upwards
                rating += field.rate_row(players.Alice, 0, y, 1, 1);
                rating -= field.rate_row(players.Bob, 0, y, 1, 1);
                // Downwards
                rating += field.rate_row(players.Alice, 0, field.Height - 1 - y, 1, -1);
                rating -= field.rate_row(players.Bob, 0, field.Height - 1 - y, 1, -1);
            }
            // Diagonals starting from the bottom and from the top
            for (int x = 1; x < field.Width - 3; x++)
            {
                // Upwards
                rating += field.rate_row(players.Alice, x, 0, 1, 1);
                rating -= field.rate_row(players.Bob, x, 0, 1, 1);
                // Downwards
                rating += field.rate_row(players.Alice, x, field.Height - 1, 1, -1);
                rating -= field.rate_row(players.Bob, x, field.Height - 1, 1, -1);
            }


            return rating;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="player">The player that will perform the next move</param>
        /// <returns></returns>
        public static int[] rate_columns(this Field field, players player, int depth)
        {
            int[] score = new int[field.Width];

            for (int i = 0; i < field.Width; i++)
            {

                // If it is impossible to make a move in this column, set the score to the lowest possible
                if (field.getEmptyCell(i) >= field.Height)
                {
                    if (player == players.Alice)
                        score[i] = int.MinValue;
                    else if (player == players.Bob)
                        score[i] = int.MaxValue;
                    continue;
                }

                Field _field = new Field(field);
                _field.doMove(i, player);

                // Check if the placed stone wins the game, if so, the score is maximum
                if (_field.check_for_win((byte)i, (byte)(field.getEmptyCell(i)), player))
                {
                    if (player == players.Alice)
                    {
                        score[i] = int.MaxValue-1;
                    }
                    else if (player == players.Bob)
                    {
                        score[i] = int.MinValue+1;
                    }
                    continue;
                }

                // If the game is still going on, check if we reached the maximum depth
                if (depth > 0)
                {
                    // Go deeper to get scores on which we can base the score of this field
                    int[] ratings = _field.rate_columns(player == players.Alice ? players.Bob : players.Alice,
                        depth - 1);

                    int high_score = ratings[0];

                    // Get the lowest (or the highest in Bob's case) score of the score array
                    // That move will probably be the move that our oponent will make in reaction to our move
                    for (int j = 1; j < ratings.Length; j++)
                    {

                        if (player == players.Alice)
                        {
                            if (ratings[j] < high_score)
                            {
                                high_score = ratings[j];
                            }
                        }
                        else if (player == players.Bob)
                        {
                            if (ratings[j] > high_score)
                            {
                                high_score = ratings[j];
                            }
                        }

                    }
                    score[i] = high_score;
                }
                // if we reached the maximum depth, rate the field
                else
                {
                    score[i] = _field.rate_field();
                }
            }
            return score;
        }
    }
}
