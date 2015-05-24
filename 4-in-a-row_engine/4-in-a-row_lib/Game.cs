using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace _4_in_a_row_lib
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class Game : IGame
    {
        private byte[,] field = new byte[9,9]; //9 colums with 7 rows
        private byte winning { get; set; } //0 for noone, 1 for alice and 2 for bob
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
        Game()
        {
            for(int i = 0; i <field.GetLength(1); i++)
            {
                for (int j = 0; i < field.GetLength(2); i++)
                {
                    field[i, j] = 0;
                }
            }
            winning = 0;
        }

        public byte[,] get_field()
        {
            return field;
        }

        public void add_stone(byte row, bool alice)
        {
            for (byte i = 0; i <= field.GetLength(2); i++)
            {
                if(field[row, i] == 0)
                {
                    field[i, row] = alice?(byte)1:(byte)2; //1 for Alice(true), 2 for Bob(false)
                    check_for_win(row, i, alice);
                }
            }
        }

        private void check_for_win(byte x, byte y, bool alice)
        {
            byte ab = alice ? (byte)1 : (byte)2;
            byte counter = 1;
            int _x = x;
            int _y = y;
            counter += count_for_win_direction(x, y, -1, 1, ab);
            counter += count_for_win_direction(x, y, 1, -1, ab);
            if (counter >= 4)
            {
                winning = ab;
            }

            counter += count_for_win_direction(x, y, 1, 1, ab);
            counter += count_for_win_direction(x, y, -1, -1, ab);
            if (counter >= 4)
            {
                winning = ab;
            }
            counter += count_for_win_direction(x, y, 0, 1, ab);
            counter += count_for_win_direction(x, y, 0, -1, ab);
            if (counter >= 4)
            {
                winning = ab;
            }
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
                if (field[_x, _y] != ab || _x < 0 || _x > field.GetLength(1) || _y < 0 || _y > field.GetLength(2))
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
