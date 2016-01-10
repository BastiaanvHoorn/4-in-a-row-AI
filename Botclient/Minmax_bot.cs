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

    public class game_state
    {
        public game_state[] children { get; }
        public game_state parent { get; }
        public Field field;
        public players player;
        public int depth;
        public int rating { get; }

        public game_state(Field _field, players _player, int _depth, game_state _parent = null)
        {
            field = _field;
            player = _player;
            depth = _depth;
            if (_parent != null)
            {
                parent = _parent;
            }
            if (depth > 0)
            {
                children = set_children(field, player);
            }
            else
            {
                
            }
        }

        private game_state[] set_children(Field field, players _player)
        {
            byte width = field.Width;
            game_state[] children = new game_state[field.get_total_empty_columns()];
            int child_index = 0;
            if (_player == players.Alice)
                _player = players.Bob;
            else
                _player = players.Alice;
            for (int i = 0; i < width; i++)
            {
                if (field.getEmptyCell(i) < field.Height)
                {
                    Field _field = field;
                    _field.doMove(i, _player);

                    children[child_index] = new game_state(_field, _player, depth - 1, this);
                }
            }
            return children;
        }

    }
}
