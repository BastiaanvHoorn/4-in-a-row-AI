using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public class game_history_util
    {
        /// <summary>
        /// Converts the linear game history representation (passed through network connection) into a parallel representation (which can be read by the process_game_history function).
        /// </summary>
        /// <param name="history"></param>
        /// <returns>An array of game histories</returns>
        public static byte[][] linear_to_parrallel_game_history(List<byte> history)
        {
            //Remove header and everything after the footer from the data
            history = history.SkipWhile(b => b == Network_codes.game_history_array).TakeWhile(b => b != Network_codes.end_of_stream).ToList();
            history.Add(Network_codes.end_of_stream);
            //Count the amount of games that is in this byte-array
            int game_counter = history.Count(b => b == Network_codes.game_history_alice || b == Network_codes.game_history_bob);

            //Create an array of arrays with the count of games
            byte[][] game_history = new byte[game_counter][];
            int index = 0;
            for (int game = 0; game < game_history.Length; game++)
            {
                for (int turn = 1+  index; turn < history.Count; turn++)
                {
                    if (history[turn] == Network_codes.game_history_alice ||
                        history[turn] == Network_codes.game_history_bob ||
                        history[turn] == Network_codes.end_of_stream)
                    {

                        game_history[game] = new byte[turn-index];
                        break;
                    }
                }
                for (int turn = 0; turn < game_history[game].Count(); turn++)
                {
                    game_history[game][turn] = history[turn+index];
                }
                index += game_history[game].Count();
            }
            return game_history;
        }
    }
}
