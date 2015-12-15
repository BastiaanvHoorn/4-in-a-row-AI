using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;
using Util;

namespace connect4
{

    public class GUI_player : IPlayer
    {
        private readonly game_interface gui;
        public players player { get; }
        public GUI_player(game_interface gui, players player)
        {
            this.gui = gui;
            this.player = player;
        }

        public byte get_turn(Field field, log_modes log_mode)
        {
            while (true)
            {
                if (wait_for_button(gui))
                {
                    return gui.get_numeric(player);
                }
            }
        }
        /// <summary>
        /// waits 5 miliseconds and then returns if the button from the given player is pressed
        /// </summary>
        /// <param name="gui"></param>
        /// <returns></returns>
        private bool wait_for_button(game_interface gui)
        {
            System.Threading.Thread.Sleep(5);
            bool button = gui.get_button_pressed(player);
            return button;
        }
    }
}
