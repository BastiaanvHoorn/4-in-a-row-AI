using System;
using System.CodeDom;

namespace Engine
{
    public enum players { Empty, Alice, Bob }
    public class Game
    {
        private static byte height;
        private static byte width;
        private readonly Field field;

        //0 for noone, 1 for alice and 2 for bob
        public players next_players { get; private set; }
        private players winning { get; set; }
        /// <summary>
        /// A check if the specified person has won
        /// </summary>
        /// <param name="player">1 for alice, 2 for bob</param>
        public bool has_won(players player)
        {
            if (player == winning)
            {
                return true;
            }
            return false;

        }
        /// <summary>
        /// Initializes the field on 0;
        /// </summary>
        public Game(byte _width, byte _height)
        {
            height = _height;
            width = _width;
            field = new Field(_width, _height);
            next_players = players.Alice;
            winning = 0;
        }

        public Field get_field()
        {
            return field;
        }

        /// <summary>
        /// Adds a stone on top of the specified column
        /// </summary>
        /// <param name="column">The column that the stone must be added to. Minimum of 0 and Maximum of the width of the field minus 1</param>
        /// <param name="player">1 for Alice, 2 for Bob</param>
        /// <param name="info"></param>
        /// <returns>If the stone could be placed in that column</returns>
        public bool add_stone(byte column, players player, ref string info)
        {
            string player_name = Enum.GetName(typeof(players), player);
            if (next_players != player)
            {
                info = $"It's not {player_name}'s turn";
                return false;
            }
            if (column >= field.Width)
            {
                info = $"specified invalid column ({column})";
                return false;
            }
            byte empty_cell = (byte)field.getEmptyCell(column); //Get the x-coordinate for the first empty cell in the given column
            if (empty_cell < 6) //If there is still room in this column, place a stone
            {
                field.doMove(column,player);
                next_players = (player == players.Alice ? players.Bob : players.Alice);
                Console.WriteLine($"{player_name} dropped a stone at {column}, {empty_cell}");
                check_for_win(column, empty_cell, player);
                return true;
            }

            info = "column " + column + " is already full";
            return false;
        }
        #region check_for_win
        /// <summary>
        /// Checks if someone has won
        /// </summary>
        /// <p>
        /// Checks in each direction from the given stone if it can make a whole row. If so, the variable winning will be changed
        /// </p>
        /// <param name="x">The x of the given stone</param>
        /// <param name="y">The y of the given stone</param>
        /// <param name="player">1 for alice, 2 for bob</param>
        private void check_for_win(byte x, byte y, players player)
        {
            //Checks from botleft to topright
            byte counter = 1;
            counter += count_for_win_direction(x, y, -1, 1, player);
            counter += count_for_win_direction(x, y, 1, -1, player);
            if (counter >= 4)
            {
                winning = player;
            }

            //checks from topleft to botright
            counter = 1;
            counter += count_for_win_direction(x, y, 1, 1, player);
            counter += count_for_win_direction(x, y, -1, -1, player);
            if (counter >= 4)
            {
                winning = player;
            }

            //checks horizontal
            counter = 1;
            counter += count_for_win_direction(x, y, 0, 1, player);
            counter += count_for_win_direction(x, y, 0, -1, player);
            if (counter >= 4)
            {
                winning = player;
            }

            //checks vertical
            counter = 1;
            counter += count_for_win_direction(x, y, -1, 0, player);
            counter += count_for_win_direction(x, y, 1, 0, player);
            if (counter >= 4)
            {
                winning = player;
            }
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
        private byte count_for_win_direction(byte x, byte y, sbyte dx, sbyte dy, players player)
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
        #endregion
    }
}
