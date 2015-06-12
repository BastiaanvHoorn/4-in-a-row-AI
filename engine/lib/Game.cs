using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace lib
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class Game : IGame
    {
        private byte[][] field = new byte[9][]; //9 colums with 7 rows
        private byte height = 9;
        private byte winning { get; set; } //0 for noone, 1 for alice and 2 for bob
        /// <summary>
        /// A check if the specified person has won
        /// </summary>
        /// <param name="alice">Check if Alice(true) or Bob(false) has won</param>
        public bool has_won(bool alice)
        {
            if (alice && winning == (byte)1 || !alice && winning == (byte)2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Initializes the field on 0;
        /// </summary>
        public Game()
        {
            for (int i = 0; i < field.Length; i++)
            {
                field[i] = new byte[height];
                for (int j = 0; j < field[i].Length; j++)
                {
                    Console.WriteLine("X: " + i + ", Y: " + j);
                    field[i][j] = (byte)0;

                }
            }
            winning = 0;
        }

        public byte[][] get_field()
        {
            return field;
        }
        /// <summary>
        /// Adds a stone on top of the specified row
        /// </summary>
        /// <param name="row">The row that the stone must be added to. Minimum of 0 and Maximum of the height of the field</param>
        /// <param name="alice">If this stone is for Alice(true) or for Bob(false)</param>
        /// <returns>If the stone could be placed in that row</returns>
        public bool add_stone(byte row, bool alice)
        {
            if (row >= field[row].Length)
            {
                return false;
            }
            else
            {

                for (byte i = 0; i < height; i++)
                {

                    if (field[row][i] == 0)
                    {
                        field[row][i] = alice ? (byte)1 : (byte)2; //1 for Alice(true), 2 for Bob(false)
                        Console.WriteLine("Dropped a stone for " + (alice ? "alice" : "bob") + " at " + row + ", " + i);
                        //TODO: clear check_for_win of errors
                        check_for_win(row, i, alice);
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Checks if someone has won
        /// </summary>
        /// <p>
        /// Checks in each direction from the given stone if it can make a whole row. If so, the variable winning will be changed
        /// </p>
        /// <param name="x">The x of the given stone</param>
        /// <param name="y">The y of the given stone</param>
        /// <param name="alice">If the player that laid the stone is alice (false if it was bob)</param>
        private void check_for_win(byte x, byte y, bool alice)
        {
            byte ab = alice ? (byte)1 : (byte)2;
            //Checks from botleft to topright
            byte counter = 1;
            counter += count_for_win_direction(x, y, -1, 1, ab);
            counter += count_for_win_direction(x, y, 1, -1, ab);
            if (counter >= 4)
            {
                winning = ab;
            }

            //checks from topleft to botright
            counter = 1;
            counter += count_for_win_direction(x, y, 1, 1, ab);
            counter += count_for_win_direction(x, y, -1, -1, ab);
            if (counter >= 4)
            {
                winning = ab;
            }

            //checks horizontal
            counter = 1;
            counter += count_for_win_direction(x, y, 0, 1, ab);
            counter += count_for_win_direction(x, y, 0, -1, ab);
            if (counter >= 4)
            {
                winning = ab;
            }

            //checks vertical
            counter = 1;
            counter += count_for_win_direction(x, y, -1, 0, ab);
            counter += count_for_win_direction(x, y, 1, 0, ab);
            if (counter >= 4)
            {
                winning = ab;
            }
        }
        private byte count_for_win_direction(byte x, byte y, sbyte dx, sbyte dy, byte ab)
        {
            byte counter = 0;
            sbyte _x = (sbyte)x;
            sbyte _y = (sbyte)y;
            while (true)
            {
                _x += dx;
                _y += dy;
                if (_x < 0 || _x >= field.Length || _y < 0 || _y >= field[0].Length)
                {
                    break;
                }
                else if(field[_x][_y] != ab)
                {
                    break;
                }
                else
                {
                    counter++;
                }
            }
            return counter;
        }
    }
}
