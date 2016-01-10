using System;
using System.Text;

namespace Engine
{
    public class Field
    {
        private static byte[] Bitmask = new byte[] { 3, 12, 48, 192 }; //This array is used for bit operations to read pairs of two bits. Binary values: 00000011, 00001100, 00110000, 11000000
        private static byte[] Bitmask2 = new byte[] { 255, 252, 240, 192 }; //This array is used for bit operations to read several pairs of two bits. Binary values: 11111111, 11111100, 11110000, 11000000
        private static byte[] Bitmask3 = new byte[] { 3, 15, 63, 255 }; //Same as Bitmask2. Binary values: 11111111, 00111111, 00001111, 00000011

        internal byte[] Storage; //The actual array that stores the field.
        readonly public byte Width;
        readonly public byte Height;

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

        public players this[int x, int y]
        {
            get { return getCellPlayer(x, y); }
            internal set { setCell(x, y, value); }
        }

        /// <summary>
        /// Gets the value of the cell at the specified coordinates
        /// </summary>
        /// <param name="x">X-coordinate (row)</param>
        /// <param name="y">Y-coordinate (column)</param>
        /// <returns>Cell value</returns>
		public int getCellValue(int x, int y)
        {
            int cellIndex = Height * x + y;             //The cell position represented as an index in a one-dimensional row instead of a coordinate representation (two-dimensional)
            int byteNumber = cellIndex >> 2;            //cellIndex / 4     Each byte can store 4 cells (two bits per cell). With this operation we determine which byte contains the needed information.
            int bitNumber = cellIndex & 3;              //cellIndex % 4     The position of the bit (and with that the position of the cell) within the selected byte.
            int value = Storage[byteNumber] & Bitmask[bitNumber];   //Gets the bits that represent the wanted cell.
            return value >> (2 * bitNumber);       //value / 4^bitNumber     Returns value. This requires a bitshift like this: 00110000 -> 00000011 or: 11000000 -> 00000011 to make sure that we return a value of 0 (00), 1 (01) or 2 (10).
        }

        /// <summary>
        /// Gets which player owns the cell at the specified coordinates
        /// </summary>
        /// <param name="x">X-coordinate (row)</param>
        /// <param name="y">Y-coordinate (column)</param>
        /// <returns>The player who owns the specified cell</returns>
		public players getCellPlayer(int x, int y)
        {
            return (players)(getCellValue(x, y));    //Converts the cellValue directly into the player enum.
        }


        /// <summary>
        /// Stores the given player at the given cell coordinates
        /// </summary>
        /// <param name="x">X-coordinate</param>
        /// <param name="y">Y-coordinate</param>
        /// <param name="player">The player that needs to be stored in the cell</param>
		internal void setCell(int x, int y, players player)
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
                throw new InvalidMoveException($"The cell at ({x}, {y}) is already taken");
            }
        }

        /// <summary>
        /// Performs a move at the given column for the given player
        /// </summary>
        /// <param name="column">The column of the player choice</param>
        /// <param name="player">The player who performs the turn</param>
        public void doMove(int column, players player)
        {
            if (column > Width - 1)
            {
                throw new InvalidMoveException($"Column {column + 1} doesn't exist. There are just {Width} columns available");
            }
            if (player == players.Empty)
            {
                throw new InvalidMoveException("Only real player, like Alice and Bob can do a move. player.Empty isn't able to do that");
            }

            byte emptyCell = this.getEmptyCell(column);

            if (emptyCell > Height - 1)
            {
                throw new InvalidMoveException($"{player} tries to add a stone to column {column + 1}, but that column is already filled. (Max {Height} stones per column)");
            }

            this[column, emptyCell] = player;

        }

        /// <summary>
        /// Returns byte array Storage. IMPORTANT: Only use this function to GET Storage. Don't use it to manipulate the array by yourself.
        /// </summary>
        /// <returns>Storage that contains the field data</returns>
        public byte[] getStorage()
        {
            return Storage;
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Field p = obj as Field;
            if ((System.Object)p == null)
            {
                return false;
            }

            if (Storage.Length != p.Storage.Length)
                return false;

            for (int i = 0; i < Storage.Length; i++)
            {
                if (Storage[i] != p.Storage[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int code = 0;
            code = Storage[0];
            code += Storage[3] << 8;
            code += Storage[6] << 16;
            code += Storage[9] << 24;
            return code;
        }

        /// <summary>
        /// Returns a concatenation of the hexadecimal presentations of the bytes in byte[] Storage.
        /// </summary>
        /// <returns>String representation of Storage</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (byte b in Storage)
            {
                for (byte i = 0; i < 4; i++)
                {
                    sb.AppendFormat("{0:x2}", (b & Bitmask[i]) >> (i << 1));   //Appends byte b to StringBuilder sb. Format: x -> Hexadecimal representation, 2 -> Consists of at least two digits. Example: 5 -> 05, 20 -> 14
                    sb.Append(" ");
                }
            }

            return sb.ToString();
        }
    }
}