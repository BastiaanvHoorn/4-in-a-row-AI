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
            while (true)
            {
                Task<bool> task = Task.Factory.StartNew(() => wait_for_button(gui));
                await task;
                if (task.Result)
                {
                    return gui.get_numeric(player);
                }
            }
        }

        private bool wait_for_button(game_interface gui)
        {
            bool button = gui.get_button_pressed(player);
            System.Threading.Thread.Sleep(5);
            return button;
        }
    }
}
