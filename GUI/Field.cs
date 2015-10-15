using System;

namespace GUI
{
    public class Field
    {
        private static byte[] Bitmask = new byte[] { 3, 12, 48, 192 }; //This array is used for bit operations to read pairs of two bits. Binary values: 00000011, 00001100, 00110000, 11000000
        private static byte[] Bitmask2 = new byte[] { 255, 252, 240, 192 }; //This array is used for bit operations to read several pairs of two bits. Binary values: 11111111, 11111100, 11110000, 11000000
        private static byte[] Bitmask3 = new byte[] { 3, 15, 63, 255 }; //Same as Bitmask2. Binary values: 11111111, 00111111, 00001111, 00000011

        internal byte[] Storage; //The actual array that stores the field.
        byte Width;
        byte Height;
        
		public Field(byte[] input, byte width = 7, byte height = 6)
		{
			Storage = input;
            this.Width = width;
            this.Height = height;
        }

        /// <summary>
        /// Empty constructor
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Field(byte width = 7, byte height = 6)
        {
            int totalBytes = (int)Math.Ceiling((double)(width * height) / 4);
            Storage = new byte[totalBytes];
            this.Width = width;
            this.Height = height;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="f">Field to be copied</param>
        public Field(Field f)
        {
            Storage = new byte[f.Storage.Length];
            Buffer.BlockCopy(f.Storage, 0, this.Storage, 0, f.Storage.Length);
            this.Width = f.Width;
            this.Height = f.Height;
        }

        /// <summary>
        /// Gets which player owns the cell at the specified coordinates
        /// </summary>
        /// <param name="x">X-coordinate (row)</param>
        /// <param name="y">Y-coordinate (column)</param>
        /// <returns>The player who owns the specified cell</returns>
		public player getCell(int x, int y)
		{
			int cellIndex = Height * x + y;             //The cell position represented as an index in a one-dimensional row instead of a coordinate representation (two-dimensional)
			int byteNumber = cellIndex >> 2;		    //cellIndex / 4     Each byte can store 4 cells (two bits per cell). With this operation we determine which byte contains the needed information.
			int bitNumber = cellIndex & 3;			    //cellIndex % 4     The position of the bit (and with that the position of the cell) within the selected byte.
			int bits = Storage[byteNumber] & Bitmask[bitNumber];    //Gets the bits that represent the wanted cell.
			return (player)(bits >> (2 * bitNumber));		//bits / 4^bitNumber    Returns the player by converting int ´bits´ to enum ´player´. This requires a bitshift like this: 00110000 -> 00000011 or: 11000000 -> 00000011 to make sure that we return a value of 0 (00), 1 (01) or 2 (10).
		}

        /// <summary>
        /// Gets the first empty cell from the bottom of the given column.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public int getEmptyCell(int column)
        {
            if (Height == 6)
            {
                int value = 0;

                if ((column & 1) == 0)    //% 2. Even or odd.
                {
                    int startByte = column / 2 * 3;
                    value = Storage[startByte] + 256 * (Storage[startByte + 1] & 15); //    We need all cells (4) that are stored in the start byte and the first 2 cells that are stored in the next byte, to get the total column value.
                }
                else
                {
                    int startByte = column / 2 * 3 + 1;
                    value = ((Storage[startByte] & 240) >> 4) + 16 * (Storage[startByte + 1]); // We need the 2 last cells stored in the first byte and all cells that are stored in the next byte, to get the total column value.
                }

                int cell = 0;
                while (value > 0) //    Every iteration in the while loop we shift value with 2 bits. When value is 0, we know that every bit in value is 0 and all remaining cells in the column are empty.
                {
                    cell++;      //Every iteration row is increased by 1. Cell represents how many bitshifts were necessary to make value 0. This means that cell is the first empty cell in the given column.
                    value >>= 2; // column2 /= 4. Each bitshift a cell is wiped, and only the cells above remain.
                }

                return cell;
            }
            else
            {
                throw new NotImplementedException("No support for fields with a heights other than 6");
            }
        }

        /// <summary>
        /// Stores the given player at the given cell coordinates
        /// </summary>
        /// <param name="x">X-coordinate</param>
        /// <param name="y">Y-coordinate</param>
        /// <param name="player">The player that needs to be stored in the cell</param>
		internal void setCell(int x, int y, player player)
		{
            int cellIndex = Height * x + y;
            int byteNumber = cellIndex >> 2;
            int bitNumber = cellIndex & 3;
            int playerNumber = (int)player << (2 * bitNumber);

            if ((Storage[byteNumber] & Bitmask[bitNumber]) == 0)
            {
                Storage[byteNumber] |= (byte)playerNumber;
            }
            else
            {
                throw new InvalidMoveException(string.Format("The cell at ({0}, {1}) is already taken", x, y));
            }
		}

        /*private void clearCell(int x, int y)
        {
            if (x > Width - 1)
            {
                throw new InvalidMoveException(String.Format("Column {0} doesn't exist. There are just {1} columns available", x + 1, Width));
            }
            else if (y > Height - 1)
            {
                throw new InvalidMoveException(String.Format("Cell {0} doesn't exist. Ther are just {1} cells per column", y + 1, Height));
            }


        }*/

        /// <summary>
        /// Performs a move at the given column for the given player
        /// </summary>
        /// <param name="column">The column of the players choice</param>
        /// <param name="player">The player who performs the turn</param>
        public void doMove(int column, player player)
        {
            if (column > Width - 1)
            {
                throw new InvalidMoveException(String.Format("Column {0} doesn't exist. There are just {1} columns available", column + 1, Width));
            }
            else if (player == player.Empty)
            {
                throw new InvalidMoveException("Only real players, like Alice and Bob can do a move. player.Empty isn't able to do that");
            }

            int emptyCell = getEmptyCell(column);

            if (emptyCell > Height - 1)
            {
                throw new InvalidMoveException(String.Format("{0} tries to add a stone to column {1}, but that column is already filled. (Max {2} stones per column)", player, column + 1, Height));
            }
            else
            {
                setCell(column, emptyCell, player);
            }
        }

		//public void foo(int value, int bit)
		//{
		//	//Get
		//	bool b = ((value & Bitmask[bit]) != 0);

		//	//Turn on bit
		//	value |= Bitmask[bit];

		//	//Turn off bit
		//	value |= ~Bitmask[bit];
		//}
	}
}