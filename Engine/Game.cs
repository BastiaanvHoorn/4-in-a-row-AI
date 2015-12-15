using System;
using System.CodeDom;
using System.Collections.Generic;

namespace Engine
{
    public class Game
    {
        public byte height;
        public byte width;
        public byte[] history { get { return game_history.ToArray(); } }
        private List<byte> game_history { get; }
        public byte stones_count { get; private set; } //If this is equal to the width times the height, then it is a tie
        private readonly Field field;
        public players next_player { get; private set; }
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
            game_history = new List<byte>();
            stones_count = 0;
            height = _height;
            width = _width;
            field = new Field(_width, _height);
            next_player = players.Alice;
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
            if (stones_count == width*height)
            {
                info = "The field is full, check for a tie";
            }
            string player_name = Enum.GetName(typeof(players), player);
            if (next_player != player)
            {
                info = $"It's not {player_name}'s turn";
                return false;
            }
            if (column >= field.Width)
            {
                info = $"specified invalid column ({column})";
                return false;
            }
            byte empty_cell = field.getEmptyCell(column); //Get the x-coordinate for the first empty cell in the given column
            if (empty_cell < 6) //If there is still room in this column, place a stone
            {
                field.doMove(column,player);
                next_player = (player == players.Alice ? players.Bob : players.Alice);
                info = $"{player_name} dropped a stone at {column}, {empty_cell}";
                if (field.check_for_win(column, empty_cell, player))
                    winning = player;

                stones_count++;
                game_history.Add(column);
                return true;
            }

            info = $"column {column} is already full";
            return false;
        }
    }
}
