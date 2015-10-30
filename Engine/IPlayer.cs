using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public interface IPlayer
    {
        log_modes log_mode { get; }
        players player { get; }
        byte get_turn(Field field);
    }
}
