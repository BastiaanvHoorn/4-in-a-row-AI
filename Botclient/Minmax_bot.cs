using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;
using Utility;
using NLog;
using Logger = NLog.Logger;

namespace Botclient
{
    public class Minmax_bot : IPlayer
    {
        public players player { get; }
        public int depth { get; }
        public Minmax_bot(players _player, int _depth)
        {
            player = _player;
            depth = _depth;
        }

        public byte get_turn(Field field)
        {
            // Get the array of column ratings
            int[] ratings = field.rate_columns(player, depth);

            byte high_score_index = 0;

            // Get maximum value in case of Alice
            if (player == players.Alice)
            {
                for (byte i = 1; i < ratings.Length; i++)
                {

                    if (ratings[i] > ratings[high_score_index])
                    {
                        high_score_index = i;
                    }

                }
            }
            // Get the minimum value in the case of Bob
            else if (player == players.Bob)
            {
                for (byte i = 1; i < ratings.Length; i++)
                {

                    if (ratings[i] < ratings[high_score_index])
                    {
                        high_score_index = i;
                    }


                }
            }


            return high_score_index;
        }
    }
}
