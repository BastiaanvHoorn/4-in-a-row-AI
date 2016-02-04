using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Botclient
{
    public class Random_bot : IPlayer
    {
        private players Player;

        public Random_bot(players player)
        {
            Player = player;
        }

        public players player
        {
            get { return Player; }
        }

        public byte get_turn(Field field)
        {
            return field.getRandomColumn();
        }
    }
}
