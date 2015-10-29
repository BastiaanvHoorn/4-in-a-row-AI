using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;

namespace connect4
{

    public class GUI_player : IPlayer
    {
        private readonly game_interface gui;
        public player player { get; }
        public GUI_player(game_interface gui, player player)
        {
            this.gui = gui;
            this.player = player;
        }

        public async Task<byte> get_turn(Field field)
        {
            return await Task.Factory.StartNew(() => gui.await_button(player)).Result;
        }
    }
}
