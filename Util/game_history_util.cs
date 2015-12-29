using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util
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

            history = history.SkipWhile(b => b == Network_codes.game_history_array).TakeWhile(b => b != Network_codes.end_of_stream).ToList();
            history.Add(Network_codes.end_of_stream);
            //Count the amount of games that is in this byte-array
            int game_counter = history.Count(b => b == Network_codes.game_history_alice || b == Network_codes.game_history_bob);

            //Create an array of arrays with the count of games
            byte[][] game_history = new byte[game_counter][];
            for (int game = 0; game < game_history.Length; game++)
            {
                for (int turn = 1; turn < history.Count; turn++)
                {
                    if (history[turn] == Network_codes.game_history_alice ||
                        history[turn] == Network_codes.game_history_bob ||
                        history[turn] == Network_codes.end_of_stream)
                    {

                        game_history[game] = new byte[turn];
                        break;
                    }
                }
                for (int turn = 0; turn < game_history[game].Count(); turn++)
                {
                    game_history[game][turn] = history[turn];
                }
                IEnumerable<byte> _history = history.Skip(game_history[game].Count());
                history = _history.ToList();
            }
            return game_history;
        }
    }
}
