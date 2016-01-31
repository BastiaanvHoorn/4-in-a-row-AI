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
            int[] ratings = field.rate_columns(player, depth);

            byte high_score_index = 0;

            for (byte i = 1; i < ratings.Length; i++)
            {

                if (player == players.Alice)
                {
                    if (ratings[i] > ratings[high_score_index])
                    {
                        high_score_index = i;
                    }
                }
                else if (player == players.Bob)
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
