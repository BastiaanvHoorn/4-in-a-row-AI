using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4_in_a_row_
{
    public class Field
    {
		private static byte[] Bitmask = new byte[] { 0x03, 0x0C, 0x30, 0xC0 }; //This array is used for bit operations. The items represent the following binary values: 00000011, 00001100, 00110000, 11000000.
		internal byte[] Storage; //The actual array that stores the field.
        
		public Field(byte[] input)
		{
			Storage = input;
		}

        /// <summary>
        /// Gets which player owns the cell at the specified coordinates
        /// </summary>
        /// <param name="x">X-coordinate (row)</param>
        /// <param name="y">Y-coordinate (column)</param>
        /// <returns>The player who owns the specified cell</returns>
		public player getPlayer(int x, int y)
		{
			int cellIndex = 6 * x + y;              //The cell position represented as an index in a one-dimensional row instead of a coordinate representation (two-dimensional)
			int byteNumber = cellIndex >> 2;		//cellIndex / 4     Each byte can store 4 cells (two bits per cell). With this operation we determine which byte contains the needed information.
			int bitNumber = cellIndex & 3;			//cellIndex % 4     The position of the bit (and with that the position of the cell) within the selected byte.
			int bits = Storage[byteNumber] & Bitmask[bitNumber];    //Gets the bits that represent the wanted cell.
			return (player)(bits >> (2 * bitNumber));		//bits / 4^bitNumber    Returns the player by converting int ´bits´ to enum ´player´. This requires a bitshift like this: 00110000 -> 00000011 or: 11000000 -> 00000011 to make sure that we return a value of 0 (00), 1 (01) or 2 (10).
		}

        /// <summary>
        /// Stores the given player at the given cell coordinates
        /// </summary>
        /// <param name="x">X-coordinate</param>
        /// <param name="y">Y-coordinate</param>
        /// <param name="player">The player that needs to be stored</param>
		public void setPlayer(int x, int y, player player)
		{
            int cellIndex = 6 * x + y;
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