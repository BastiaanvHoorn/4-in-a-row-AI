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
    class Minmax_bot : IPlayer
    {
        public players player { get; }
        public uint depth { get; }
        public bool search2 { get; }
        public Minmax_bot(players _player, uint _depth, bool _search2)
        {
            player = _player;
            depth = _depth;
            search2 = _search2;
        }

        public byte get_turn(Field field)
        {
            throw new NotImplementedException();
        }

        public byte rate_field(Field field)
        {
            throw new NotImplementedException();
        }
    }
}
