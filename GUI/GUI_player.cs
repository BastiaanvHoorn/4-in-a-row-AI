using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;
using Utility;

namespace connect4
{

    public class GUI_player : IPlayer
    {
        private readonly MainWindow gui;
        public players player { get; }
        public GUI_player(MainWindow gui, players player)
        {
            this.gui = gui;
            this.player = player;
        }

        public byte get_turn(Field field)
        {
            while (true)
            {
                byte? b = gui.col_clicked;
                if (b != null)
                {
                    return (byte)b;
                }
                System.Threading.Thread.Sleep(5);
            }
        }
    }
}
